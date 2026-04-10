using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.Contracts.Abstractions;
using NexTraceOne.Catalog.Domain.Contracts.Entities;
using NexTraceOne.Catalog.Domain.Contracts.Enums;
using NexTraceOne.Catalog.Domain.Contracts.Errors;

namespace NexTraceOne.Catalog.Application.Contracts.Features.VerifyContractCompliance;

/// <summary>
/// Feature: VerifyContractCompliance — recebe uma especificação de CI/CD, compara com
/// a versão aprovada/bloqueada mais recente, computa diff estrutural e retorna resultado
/// de verificação. Persiste o registo quando não é DryRun.
/// Estrutura VSA: Command + Validator + Handler + Response em arquivo único.
/// </summary>
public static class VerifyContractCompliance
{
    /// <summary>Comando de verificação de compliance contratual a partir de CI/CD.</summary>
    public sealed record Command(
        string ApiAssetId,
        string ServiceName,
        string SpecContent,
        string SpecFormat,
        string SourceSystem,
        string? SourceBranch,
        string? CommitSha,
        string? PipelineId,
        string? EnvironmentName,
        bool DryRun) : ICommand<Response>;

    /// <summary>Valida os parâmetros do comando de verificação de compliance contratual.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.ApiAssetId).NotEmpty().MaximumLength(200);
            RuleFor(x => x.ServiceName).NotEmpty().MaximumLength(300);
            RuleFor(x => x.SpecContent).NotEmpty();
            RuleFor(x => x.SpecFormat).NotEmpty();
            RuleFor(x => x.SourceSystem).NotEmpty().MaximumLength(100);
        }
    }

    /// <summary>
    /// Handler que orquestra a verificação de compliance contratual.
    /// Compara a especificação submetida com a versão aprovada/bloqueada mais recente,
    /// extrai operações para diff estrutural e persiste o resultado quando não é DryRun.
    /// </summary>
    public sealed class Handler(
        IContractVersionRepository contractVersionRepository,
        IContractVerificationRepository verificationRepository,
        IContractsUnitOfWork unitOfWork,
        IDateTimeProvider clock,
        ICurrentTenant currentTenant,
        ICurrentUser currentUser) : ICommandHandler<Command, Response>
    {
        private static readonly string[] HttpMethods = ["get", "post", "put", "delete", "patch", "head", "options"];

        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var now = clock.UtcNow;
            var specHash = ComputeSha256(request.SpecContent);

            if (!Guid.TryParse(request.ApiAssetId, out var apiAssetGuid))
            {
                var errorVerification = await PersistErrorVerificationAsync(
                    request, specHash, now,
                    "Invalid ApiAssetId format — expected a valid GUID.",
                    cancellationToken);

                return new Response(
                    errorVerification?.Id.Value ?? Guid.Empty,
                    VerificationStatus.Error.ToString(),
                    null, null,
                    0, 0, 0,
                    [], [],
                    "Invalid ApiAssetId format — expected a valid GUID.",
                    now);
            }

            var latestVersion = await contractVersionRepository.GetLatestByApiAssetAsync(
                apiAssetGuid, cancellationToken);

            if (latestVersion is null)
            {
                var errorVerification = await PersistErrorVerificationAsync(
                    request, specHash, now,
                    $"No approved or locked contract version found for API asset '{request.ApiAssetId}'.",
                    cancellationToken);

                return new Response(
                    errorVerification?.Id.Value ?? Guid.Empty,
                    VerificationStatus.Error.ToString(),
                    null, null,
                    0, 0, 0,
                    [], [],
                    $"No approved or locked contract version found for API asset '{request.ApiAssetId}'.",
                    now);
            }

            var submittedOps = ExtractOperations(request.SpecContent);
            var existingOps = ExtractOperations(latestVersion.SpecContent);

            var existingSet = new HashSet<string>(
                existingOps.Select(NormalizeOp), StringComparer.OrdinalIgnoreCase);
            var submittedSet = new HashSet<string>(
                submittedOps.Select(NormalizeOp), StringComparer.OrdinalIgnoreCase);

            var removedEndpoints = existingOps
                .Where(o => !submittedSet.Contains(NormalizeOp(o)))
                .Select(o => $"{o.Method} {o.Path}")
                .ToList();

            var newEndpoints = submittedOps
                .Where(o => !existingSet.Contains(NormalizeOp(o)))
                .Select(o => $"{o.Method} {o.Path}")
                .ToList();

            var breakingCount = removedEndpoints.Count;
            var additiveCount = newEndpoints.Count;
            var nonBreakingCount = submittedOps.Count - additiveCount;
            if (nonBreakingCount < 0) nonBreakingCount = 0;

            var status = breakingCount > 0
                ? VerificationStatus.Block
                : additiveCount > 0 || nonBreakingCount > 0
                    ? VerificationStatus.Warn
                    : VerificationStatus.Pass;

            if (status == VerificationStatus.Warn && additiveCount == 0 && breakingCount == 0)
                status = VerificationStatus.Pass;

            var message = status switch
            {
                VerificationStatus.Pass => "Contract verification passed — no changes detected.",
                VerificationStatus.Warn => $"Contract verification passed with warnings — {additiveCount} new endpoint(s) detected.",
                VerificationStatus.Block => $"Contract verification blocked — {breakingCount} breaking change(s) detected ({string.Join(", ", removedEndpoints.Take(5))}).",
                _ => "Contract verification completed."
            };

            var diffDetails = JsonSerializer.Serialize(new
            {
                removedEndpoints,
                newEndpoints,
                submittedOperationsCount = submittedOps.Count,
                existingOperationsCount = existingOps.Count
            });

            Guid verificationId = Guid.Empty;

            if (!request.DryRun)
            {
                var verification = ContractVerification.Create(
                    tenantId: currentTenant.Id.ToString(),
                    apiAssetId: request.ApiAssetId,
                    serviceName: request.ServiceName,
                    contractVersionId: latestVersion.Id.Value,
                    specContentHash: specHash,
                    status: status,
                    breakingChangesCount: breakingCount,
                    nonBreakingChangesCount: nonBreakingCount,
                    additiveChangesCount: additiveCount,
                    diffDetails: diffDetails,
                    complianceViolations: "[]",
                    sourceSystem: request.SourceSystem,
                    sourceBranch: request.SourceBranch,
                    commitSha: request.CommitSha,
                    pipelineId: request.PipelineId,
                    environmentName: request.EnvironmentName,
                    verifiedAt: now,
                    createdAt: now,
                    createdBy: currentUser.Id);

                await verificationRepository.AddAsync(verification, cancellationToken);
                await unitOfWork.CommitAsync(cancellationToken);
                verificationId = verification.Id.Value;
            }

            return new Response(
                verificationId,
                status.ToString(),
                latestVersion.Id.Value,
                latestVersion.SemVer,
                breakingCount,
                nonBreakingCount,
                additiveCount,
                removedEndpoints.AsReadOnly(),
                newEndpoints.AsReadOnly(),
                message,
                now);
        }

        /// <summary>
        /// Persiste verificação com status Error quando não é DryRun.
        /// </summary>
        private async Task<ContractVerification?> PersistErrorVerificationAsync(
            Command request,
            string specHash,
            DateTimeOffset now,
            string errorMessage,
            CancellationToken cancellationToken)
        {
            if (request.DryRun)
                return null;

            var verification = ContractVerification.Create(
                tenantId: currentTenant.Id.ToString(),
                apiAssetId: request.ApiAssetId,
                serviceName: request.ServiceName,
                contractVersionId: null,
                specContentHash: specHash,
                status: VerificationStatus.Error,
                breakingChangesCount: 0,
                nonBreakingChangesCount: 0,
                additiveChangesCount: 0,
                diffDetails: "{}",
                complianceViolations: JsonSerializer.Serialize(new[] { errorMessage }),
                sourceSystem: request.SourceSystem,
                sourceBranch: request.SourceBranch,
                commitSha: request.CommitSha,
                pipelineId: request.PipelineId,
                environmentName: request.EnvironmentName,
                verifiedAt: now,
                createdAt: now,
                createdBy: currentUser.Id);

            await verificationRepository.AddAsync(verification, cancellationToken);
            await unitOfWork.CommitAsync(cancellationToken);
            return verification;
        }

        /// <summary>
        /// Extrai operações HTTP (método + caminho) da especificação usando parsing simples de string.
        /// Limitação MVP: assume formato JSON OpenAPI com paths entre aspas e verbos como chaves JSON.
        /// Formatos com indentação YAML pura, multi-line ou strings escapadas podem não ser detetados.
        /// Reutiliza a mesma abordagem do DetectContractDrift para consistência no módulo.
        /// Evolução futura: substituir por parser OpenAPI dedicado (ex: Microsoft.OpenApi).
        /// </summary>
        private static List<OperationInfo> ExtractOperations(string specContent)
        {
            var results = new List<OperationInfo>();
            var lines = specContent.Split('\n');
            string? currentPath = null;

            foreach (var rawLine in lines)
            {
                var line = rawLine.Trim();

                if (line.StartsWith("\"/", StringComparison.Ordinal) && line.Contains("\":"))
                {
                    var end = line.IndexOf("\":", StringComparison.Ordinal);
                    if (end > 1)
                        currentPath = line[1..end];
                }

                if (currentPath is null) continue;

                var searchOffset = line.StartsWith("\"/", StringComparison.Ordinal)
                    ? (line.IndexOf("\":", StringComparison.Ordinal) + 2)
                    : 0;

                foreach (var method in HttpMethods)
                {
                    var pattern = $"\"{method}\":";
                    if (line.IndexOf(pattern, searchOffset, StringComparison.OrdinalIgnoreCase) >= 0)
                        results.Add(new OperationInfo(method.ToUpperInvariant(), currentPath));
                }
            }

            return results;
        }

        private static string NormalizeOp(OperationInfo op)
            => $"{op.Method.ToUpperInvariant()} {op.Path.TrimEnd('/')}";

        private static string ComputeSha256(string content)
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(content));
            return Convert.ToHexStringLower(bytes);
        }
    }

    /// <summary>Informação de uma operação HTTP extraída da especificação.</summary>
    private sealed record OperationInfo(string Method, string Path);

    /// <summary>Resposta da verificação de compliance contratual.</summary>
    public sealed record Response(
        Guid VerificationId,
        string Status,
        Guid? ContractVersionId,
        string? ContractVersionSemVer,
        int BreakingChangesCount,
        int NonBreakingChangesCount,
        int AdditiveChangesCount,
        IReadOnlyList<string> RemovedEndpoints,
        IReadOnlyList<string> NewEndpoints,
        string Message,
        DateTimeOffset VerifiedAt);
}
