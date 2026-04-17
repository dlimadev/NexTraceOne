using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.Governance.Application.Features.GetSupportBundles;

/// <summary>
/// Feature: GetSupportBundles — geração e listagem de bundles de suporte.
/// Geração de ficheiro real é pendente. Retorna entrada sintética com SimulatedNote.
/// </summary>
public static class GetSupportBundles
{
    /// <summary>Query sem parâmetros — lista bundles de suporte disponíveis.</summary>
    public sealed record Query() : IQuery<SupportBundleListResponse>;

    /// <summary>Comando para gerar um novo bundle de suporte.</summary>
    public sealed record GenerateSupportBundle(
        bool IncludesLogs,
        bool IncludesConfig,
        bool IncludesDb) : ICommand<SupportBundleEntry>;

    /// <summary>Handler de listagem de bundles de suporte.</summary>
    public sealed class Handler : IQueryHandler<Query, SupportBundleListResponse>
    {
        public Task<Result<SupportBundleListResponse>> Handle(Query request, CancellationToken cancellationToken)
        {
            var response = new SupportBundleListResponse(
                Bundles: [],
                Total: 0,
                SimulatedNote: "Real bundle storage integration pending. Generated bundles are synthetic entries only.");

            return Task.FromResult(Result<SupportBundleListResponse>.Success(response));
        }
    }

    /// <summary>Handler de geração de bundle de suporte.</summary>
    public sealed class GenerateHandler : ICommandHandler<GenerateSupportBundle, SupportBundleEntry>
    {
        public Task<Result<SupportBundleEntry>> Handle(GenerateSupportBundle request, CancellationToken cancellationToken)
        {
            var entry = new SupportBundleEntry(
                Id: Guid.NewGuid().ToString(),
                RequestedAt: DateTimeOffset.UtcNow,
                Status: "Pending",
                DownloadUrl: null,
                SizeMb: null,
                IncludesLogs: request.IncludesLogs,
                IncludesConfig: request.IncludesConfig,
                IncludesDb: request.IncludesDb,
                SimulatedNote: "Bundle generation queued. Real file generation is pending.");

            return Task.FromResult(Result<SupportBundleEntry>.Success(entry));
        }
    }

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
}
