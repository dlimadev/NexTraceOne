using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.Contracts.Abstractions;
using NexTraceOne.Catalog.Domain.Contracts.Enums;
using NexTraceOne.Catalog.Domain.Contracts.Errors;

namespace NexTraceOne.Catalog.Application.Contracts.Features.GetContractHealthTimeline;

/// <summary>
/// Feature: GetContractHealthTimeline — exibe a evolução temporal do score de saúde de um contrato.
/// Para cada versão publicada, calcula um HealthScore baseado na qualidade da especificação
/// (presença de exemplos, referências canónicas, descrição, evolução semântica e estado de ciclo de vida).
/// Correlaciona mudanças de major version com eventos de breaking change.
/// Estrutura VSA: Query + Validator + Handler + HealthTimelinePoint + Response em arquivo único.
/// </summary>
public static class GetContractHealthTimeline
{
    /// <summary>Query para obter a timeline de saúde de um contrato.</summary>
    public sealed record Query(
        Guid ApiAssetId,
        int MaxVersions = 20) : IQuery<Response>;

    /// <summary>Valida a entrada da query de timeline de saúde.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.ApiAssetId).NotEmpty();
            RuleFor(x => x.MaxVersions).InclusiveBetween(1, 100);
        }
    }

    /// <summary>
    /// Handler que computa o HealthScore de cada versão e retorna a timeline ordenada da mais antiga para a mais recente.
    /// O score é calculado com base em: exemplos (+20), entidades canónicas (+20),
    /// não depreciado (+20), descrição presente (+20) e evolução além da versão inicial (+20).
    /// Breaking change é detectado por mudança de major version ou marcação explícita na especificação.
    /// </summary>
    public sealed class Handler(IContractVersionRepository repository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var versions = await repository.ListByApiAssetAsync(request.ApiAssetId, cancellationToken);

            if (versions.Count == 0)
                return ContractsErrors.ContractVersionNotFound(request.ApiAssetId.ToString());

            // Ordena da mais antiga para a mais recente e limita ao máximo solicitado
            var ordered = versions
                .OrderBy(v => v.CreatedAt)
                .Take(request.MaxVersions)
                .ToList();

            var points = new List<HealthTimelinePoint>(ordered.Count);
            string? previousSemVer = null;

            foreach (var version in ordered)
            {
                var score = ComputeHealthScore(version.SpecContent, version.LifecycleState, version.SemVer);
                var isBreaking = IsBreakingChange(version.SpecContent, version.SemVer, previousSemVer);

                points.Add(new HealthTimelinePoint(
                    SemVer: version.SemVer,
                    HealthScore: score,
                    CreatedAt: version.CreatedAt,
                    LifecycleState: version.LifecycleState.ToString(),
                    IsBreakingChange: isBreaking));

                previousSemVer = version.SemVer;
            }

            return new Response(request.ApiAssetId, points.AsReadOnly());
        }

        /// <summary>
        /// Calcula HealthScore de 0 a 100 com base em cinco critérios de qualidade da especificação.
        /// </summary>
        private static decimal ComputeHealthScore(string specContent, ContractLifecycleState lifecycleState, string semVer)
        {
            decimal score = 0;

            if (specContent.Contains("\"example\":", StringComparison.OrdinalIgnoreCase)
                || specContent.Contains("example:", StringComparison.OrdinalIgnoreCase))
                score += 20;

            if (specContent.Contains("#/components/schemas/", StringComparison.OrdinalIgnoreCase))
                score += 20;

            if (lifecycleState != ContractLifecycleState.Deprecated)
                score += 20;

            if (specContent.Contains("\"description\":", StringComparison.OrdinalIgnoreCase)
                || specContent.Contains("description:", StringComparison.OrdinalIgnoreCase))
                score += 20;

            if (semVer != "0.0.1" && semVer != "1.0.0")
                score += 20;

            return score;
        }

        /// <summary>
        /// Determina se a versão representa um breaking change.
        /// É considerado breaking change quando há mudança de major version ou a especificação
        /// contém a marcação explícita de breaking change.
        /// </summary>
        private static bool IsBreakingChange(string specContent, string semVer, string? previousSemVer)
        {
            if (specContent.Contains("\"breaking\":", StringComparison.OrdinalIgnoreCase))
                return true;

            if (previousSemVer is null)
                return false;

            var currentMajor = ExtractMajorVersion(semVer);
            var previousMajor = ExtractMajorVersion(previousSemVer);

            return currentMajor > previousMajor;
        }

        private static int ExtractMajorVersion(string semVer)
        {
            var dot = semVer.IndexOf('.', StringComparison.Ordinal);
            if (dot < 0) return 0;
            return int.TryParse(semVer[..dot], out var major) ? major : 0;
        }
    }

    /// <summary>Ponto de saúde na timeline de versões de um contrato.</summary>
    public sealed record HealthTimelinePoint(
        string SemVer,
        decimal HealthScore,
        DateTimeOffset CreatedAt,
        string LifecycleState,
        bool IsBreakingChange);

    /// <summary>Resposta da timeline de saúde de um contrato.</summary>
    public sealed record Response(
        Guid ApiAssetId,
        IReadOnlyList<HealthTimelinePoint> Points);
}
