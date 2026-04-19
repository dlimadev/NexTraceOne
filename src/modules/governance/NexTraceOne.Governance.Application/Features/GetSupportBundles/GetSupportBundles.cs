using System.IO.Compression;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Governance.Application.Abstractions;
using NexTraceOne.Governance.Domain.Entities;

namespace NexTraceOne.Governance.Application.Features.GetSupportBundles;

/// <summary>
/// Feature: GetSupportBundles — geração e listagem de bundles de suporte.
/// A geração produz um ZIP real em memória com:
/// 1) resumo da plataforma (versão, ambiente, timestamp)
/// 2) config summary sanitizado (sem segredos)
/// 3) resumo de governança (equipas, domínios, pacotes, dados operacionais)
/// O ZIP é persistido inline na base de dados (gov_support_bundles).
/// </summary>
public static class GetSupportBundles
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <summary>Query sem parâmetros — lista bundles de suporte disponíveis.</summary>
    public sealed record Query() : IQuery<SupportBundleListResponse>;

    /// <summary>Comando para gerar um novo bundle de suporte.</summary>
    public sealed record GenerateSupportBundle(
        bool IncludesLogs,
        bool IncludesConfig,
        bool IncludesDb) : ICommand<SupportBundleEntry>;

    /// <summary>Handler de listagem de bundles de suporte.</summary>
    public sealed class Handler(ISupportBundleRepository repository) : IQueryHandler<Query, SupportBundleListResponse>
    {
        public async Task<Result<SupportBundleListResponse>> Handle(Query request, CancellationToken cancellationToken)
        {
            var bundles = await repository.ListAsync(tenantId: null, cancellationToken);

            var entries = bundles
                .Select(b => new SupportBundleEntry(
                    Id: b.Id.Value.ToString(),
                    RequestedAt: b.RequestedAt,
                    Status: b.Status,
                    DownloadUrl: b.Status == "Ready" ? $"/api/v1/platform/support-bundles/{b.Id.Value}/download" : null,
                    SizeMb: b.SizeMb,
                    IncludesLogs: b.IncludesLogs,
                    IncludesConfig: b.IncludesConfig,
                    IncludesDb: b.IncludesDb,
                    SimulatedNote: string.Empty))
                .ToList();

            return Result<SupportBundleListResponse>.Success(
                new SupportBundleListResponse(entries, entries.Count, string.Empty));
        }
    }

    /// <summary>Handler de geração de bundle de suporte.</summary>
    public sealed class GenerateHandler(
        ISupportBundleRepository repository,
        IGovernanceUnitOfWork unitOfWork,
        ITeamRepository teamRepository,
        IGovernanceDomainRepository domainRepository,
        IGovernancePackRepository packRepository,
        IConfiguration configuration,
        IDateTimeProvider clock) : ICommandHandler<GenerateSupportBundle, SupportBundleEntry>
    {
        public async Task<Result<SupportBundleEntry>> Handle(GenerateSupportBundle request, CancellationToken cancellationToken)
        {
            var now = clock.UtcNow;
            var bundle = SupportBundle.Create(
                includesLogs: request.IncludesLogs,
                includesConfig: request.IncludesConfig,
                includesDb: request.IncludesDb,
                tenantId: null,
                now: now);

            await repository.AddAsync(bundle, cancellationToken);
            await unitOfWork.CommitAsync(cancellationToken);

            bundle.MarkGenerating(now);
            repository.Update(bundle);
            await unitOfWork.CommitAsync(cancellationToken);

            try
            {
                var zipContent = await BuildZipAsync(request, teamRepository, domainRepository, packRepository, configuration, now, cancellationToken);
                bundle.MarkReady(zipContent, clock.UtcNow);
            }
            catch (Exception)
            {
                bundle.MarkFailed(clock.UtcNow);
            }

            repository.Update(bundle);
            await unitOfWork.CommitAsync(cancellationToken);

            var downloadUrl = bundle.Status == "Ready"
                ? $"/api/v1/platform/support-bundles/{bundle.Id.Value}/download"
                : null;

            return Result<SupportBundleEntry>.Success(new SupportBundleEntry(
                Id: bundle.Id.Value.ToString(),
                RequestedAt: bundle.RequestedAt,
                Status: bundle.Status,
                DownloadUrl: downloadUrl,
                SizeMb: bundle.SizeMb,
                IncludesLogs: bundle.IncludesLogs,
                IncludesConfig: bundle.IncludesConfig,
                IncludesDb: bundle.IncludesDb,
                SimulatedNote: string.Empty));
        }

        private static async Task<byte[]> BuildZipAsync(
            GenerateSupportBundle request,
            ITeamRepository teamRepository,
            IGovernanceDomainRepository domainRepository,
            IGovernancePackRepository packRepository,
            IConfiguration configuration,
            DateTimeOffset now,
            CancellationToken ct)
        {
            using var ms = new MemoryStream();
            using (var zip = new ZipArchive(ms, ZipArchiveMode.Create, leaveOpen: true))
            {
                // 1 — Platform summary (always included)
                await WriteJsonEntryAsync(zip, "platform-summary.json", new
                {
                    generatedAt = now,
                    version = typeof(GetSupportBundles).Assembly.GetName().Version?.ToString() ?? "unknown",
                    environment = configuration["ASPNETCORE_ENVIRONMENT"] ?? "Production",
                    clrVersion = Environment.Version.ToString(),
                    machineName = Environment.MachineName,
                    processorCount = Environment.ProcessorCount
                }, ct);

                // 2 — Config summary (sanitized, no secrets)
                if (request.IncludesConfig)
                {
                    var configKeys = configuration.AsEnumerable()
                        .Where(kv => kv.Key is not null && !IsSecret(kv.Key))
                        .OrderBy(kv => kv.Key)
                        .ToDictionary(kv => kv.Key!, kv => kv.Value);

                    await WriteJsonEntryAsync(zip, "config-summary.json", configKeys, ct);
                }

                // 3 — Governance summary (teams, domains, packs)
                if (request.IncludesDb)
                {
                    var teams = await teamRepository.ListAsync(null, ct);
                    var domains = await domainRepository.ListAsync(null, ct);
                    var packs = await packRepository.ListAsync(null, null, ct);

                    await WriteJsonEntryAsync(zip, "governance-summary.json", new
                    {
                        teams = teams.Select(t => new { t.Id.Value, name = t.Name }),
                        domains = domains.Select(d => new { d.Id.Value, name = d.Name }),
                        packCount = packs.Count
                    }, ct);
                }
            }

            return ms.ToArray();
        }

        private static async Task WriteJsonEntryAsync(ZipArchive zip, string entryName, object data, CancellationToken ct)
        {
            var entry = zip.CreateEntry(entryName, CompressionLevel.Optimal);
            using var stream = entry.Open();
            var json = JsonSerializer.SerializeToUtf8Bytes(data, JsonOptions);
            await stream.WriteAsync(json, ct);
        }

        private static bool IsSecret(string key) =>
            key.Contains("password", StringComparison.OrdinalIgnoreCase) ||
            key.Contains("secret", StringComparison.OrdinalIgnoreCase) ||
            key.Contains("connectionstring", StringComparison.OrdinalIgnoreCase) ||
            key.Contains("apikey", StringComparison.OrdinalIgnoreCase) ||
            key.Contains("token", StringComparison.OrdinalIgnoreCase) ||
            key.Contains("certificate", StringComparison.OrdinalIgnoreCase) ||
            key.Contains("privatekey", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Handler de download de bundle de suporte.</summary>
    public sealed class DownloadHandler(ISupportBundleRepository repository) : IQueryHandler<DownloadBundle, BundleDownload>
    {
        public async Task<Result<BundleDownload>> Handle(DownloadBundle request, CancellationToken cancellationToken)
        {
            var bundle = await repository.GetByIdAsync(new SupportBundleId(request.BundleId), cancellationToken);

            if (bundle is null)
                return Error.NotFound("SupportBundle.NotFound", "Support bundle not found.");

            if (bundle.Status != "Ready" || bundle.ZipContent is null)
                return Error.Conflict("SupportBundle.NotReady", "Support bundle is not ready for download.");

            return Result<BundleDownload>.Success(new BundleDownload(
                Content: bundle.ZipContent,
                FileName: $"support-bundle-{bundle.Id.Value:N}-{bundle.RequestedAt:yyyyMMdd}.zip"));
        }
    }

    /// <summary>Query para download de bundle de suporte.</summary>
    public sealed record DownloadBundle(Guid BundleId) : IQuery<BundleDownload>;

    /// <summary>Resposta com lista de bundles de suporte.</summary>
    public sealed record SupportBundleListResponse(
        IReadOnlyList<SupportBundleEntry> Bundles,
        int Total,
        string SimulatedNote);

    /// <summary>Entrada de bundle de suporte.</summary>
    public sealed record SupportBundleEntry(
        string Id,
        DateTimeOffset RequestedAt,
        string Status,
        string? DownloadUrl,
        double? SizeMb,
        bool IncludesLogs,
        bool IncludesConfig,
        bool IncludesDb,
        string SimulatedNote);

    /// <summary>Conteúdo para download do bundle.</summary>
    public sealed record BundleDownload(byte[] Content, string FileName);
}
