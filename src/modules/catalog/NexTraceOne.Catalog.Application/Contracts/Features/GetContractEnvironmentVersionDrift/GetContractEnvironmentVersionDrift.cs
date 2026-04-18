using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.Contracts.Abstractions;
using NexTraceOne.Catalog.Domain.Contracts.Errors;

namespace NexTraceOne.Catalog.Application.Contracts.Features.GetContractEnvironmentVersionDrift;

/// <summary>
/// Feature: GetContractEnvironmentVersionDrift — detecta drift de versão de contrato entre ambientes.
/// Compara o deployment mais recente bem-sucedido em cada ambiente registado para um ativo de API.
/// Identifica quando staging e produção estão em versões diferentes sem promoção formal.
///
/// Resolve gap INOVACAO-ROADMAP.md §1.1 — Contract Drift Detection entre ambientes.
/// Estrutura VSA: Query + Validator + Handler + Response em arquivo único.
/// </summary>
public static class GetContractEnvironmentVersionDrift
{
    /// <summary>Query para detecção de drift entre ambientes.</summary>
    public sealed record Query(Guid ApiAssetId) : IQuery<Response>;

    /// <summary>Valida a entrada da query.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.ApiAssetId).NotEmpty();
        }
    }

    /// <summary>
    /// Handler que analisa os deployments mais recentes por ambiente e detecta divergências de versão.
    /// Considera drift quando ambientes de maior maturidade (staging, production) têm versões distintas.
    /// </summary>
    public sealed class Handler(
        IContractDeploymentRepository deploymentRepository,
        IContractVersionRepository versionRepository) : IQueryHandler<Query, Response>
    {
        // Ambientes considerados de "alto impacto" para análise de drift
        private static readonly HashSet<string> HighImpactEnvironments =
            new(StringComparer.OrdinalIgnoreCase) { "production", "prod", "staging", "pre-production", "preprod" };

        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var latestByEnv = await deploymentRepository.GetLatestSuccessfulByEnvironmentAsync(
                request.ApiAssetId, cancellationToken);

            if (latestByEnv.Count == 0)
                return ContractsErrors.ContractVersionNotFound(request.ApiAssetId.ToString());

            // Obtém a versão mais recente publicada do contrato como referência canónica
            var latestVersion = await versionRepository.GetLatestByApiAssetAsync(
                request.ApiAssetId, cancellationToken);

            var environmentStates = latestByEnv
                .Select(kvp => new EnvironmentVersionState(
                    Environment: kvp.Key,
                    ActiveSemVer: kvp.Value.SemVer,
                    DeployedAt: kvp.Value.DeployedAt,
                    DeployedBy: kvp.Value.DeployedBy,
                    SourceSystem: kvp.Value.SourceSystem,
                    IsHighImpact: HighImpactEnvironments.Contains(kvp.Key)))
                .OrderByDescending(e => e.IsHighImpact)
                .ThenBy(e => e.Environment)
                .ToList()
                .AsReadOnly();

            // Detecta drift: ambientes com versões diferentes entre si
            var distinctVersions = environmentStates
                .Select(e => e.ActiveSemVer)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var hasDrift = distinctVersions.Count > 1;

            // Identifica ambientes que divergem da versão canónica mais recente
            var latestPublished = latestVersion?.SemVer;
            var laggingEnvironments = latestPublished is not null
                ? environmentStates
                    .Where(e => !string.Equals(e.ActiveSemVer, latestPublished, StringComparison.OrdinalIgnoreCase))
                    .Select(e => e.Environment)
                    .ToList()
                    .AsReadOnly()
                : (IReadOnlyList<string>)[];

            var driftStatus = (hasDrift, laggingEnvironments.Count) switch
            {
                (false, 0) => "Synchronized",
                (false, _) => "BehindLatest",
                (true, _) when environmentStates.Any(e =>
                    e.IsHighImpact &&
                    distinctVersions.Any(v => !string.Equals(v, e.ActiveSemVer, StringComparison.OrdinalIgnoreCase)))
                    => "CriticalDrift",
                _ => "Drift"
            };

            return new Response(
                ApiAssetId: request.ApiAssetId,
                LatestPublishedSemVer: latestPublished,
                HasDrift: hasDrift,
                DriftStatus: driftStatus,
                EnvironmentStates: environmentStates,
                LaggingEnvironments: laggingEnvironments,
                DistinctVersionCount: distinctVersions.Count);
        }
    }

    /// <summary>Estado de versão de contrato num ambiente específico.</summary>
    public sealed record EnvironmentVersionState(
        string Environment,
        string ActiveSemVer,
        DateTimeOffset DeployedAt,
        string DeployedBy,
        string SourceSystem,
        bool IsHighImpact);

    /// <summary>
    /// Resposta da detecção de drift de versão entre ambientes.
    /// DriftStatus: Synchronized | BehindLatest | Drift | CriticalDrift.
    /// </summary>
    public sealed record Response(
        Guid ApiAssetId,
        string? LatestPublishedSemVer,
        bool HasDrift,
        string DriftStatus,
        IReadOnlyList<EnvironmentVersionState> EnvironmentStates,
        IReadOnlyList<string> LaggingEnvironments,
        int DistinctVersionCount);
}
