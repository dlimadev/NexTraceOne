using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Abstractions;
using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Entities;
using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Errors;

namespace NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.GetFeatureFlagAwareness;

/// <summary>
/// Feature: GetFeatureFlagAwareness — obtém o estado de feature flags activas para uma release
/// e calcula um sinal de risco baseado na presença e criticidade das flags.
///
/// Lógica de risco:
///   - CriticalFlagCount >= 3  → High   (muitas flags críticas aumentam o blast radius)
///   - CriticalFlagCount >= 1  → Medium (alguma flag crítica activa)
///   - ActiveFlagCount >= 5    → Low    (muitas flags activas, mas nenhuma crítica)
///   - else                    → Minimal
///
/// Valor: incorporar a presença de feature flags como fator de confiança,
/// já que releases com flags críticas activas têm maior risco operacional.
/// Estrutura VSA: Query + Validator + Handler + Response em um único arquivo.
/// </summary>
public static class GetFeatureFlagAwareness
{
    /// <summary>Query para obter a consciência de feature flags de uma release.</summary>
    public sealed record Query(Guid ReleaseId) : IQuery<Response>;

    /// <summary>Valida a entrada da query.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.ReleaseId).NotEmpty();
        }
    }

    /// <summary>
    /// Handler que consulta o estado de feature flags e determina o nível de risco.
    /// </summary>
    public sealed class Handler(
        IReleaseRepository releaseRepository,
        IFeatureFlagStateRepository flagStateRepository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var releaseId = ReleaseId.From(request.ReleaseId);
            var release = await releaseRepository.GetByIdAsync(releaseId, cancellationToken);
            if (release is null)
                return ChangeIntelligenceErrors.ReleaseNotFound(request.ReleaseId.ToString());

            var state = await flagStateRepository.GetLatestByReleaseIdAsync(releaseId, cancellationToken);

            if (state is null)
            {
                return new Response(
                    ReleaseId: request.ReleaseId,
                    HasData: false,
                    ActiveFlagCount: 0,
                    CriticalFlagCount: 0,
                    NewFeatureFlagCount: 0,
                    FlagProvider: null,
                    RiskLevel: "Unknown",
                    RiskRationale: "No feature flag state has been recorded for this release.",
                    RecordedAt: null);
            }

            var (riskLevel, rationale) = DetermineRisk(state);

            return new Response(
                ReleaseId: request.ReleaseId,
                HasData: true,
                ActiveFlagCount: state.ActiveFlagCount,
                CriticalFlagCount: state.CriticalFlagCount,
                NewFeatureFlagCount: state.NewFeatureFlagCount,
                FlagProvider: state.FlagProvider,
                RiskLevel: riskLevel,
                RiskRationale: rationale,
                RecordedAt: state.RecordedAt);
        }

        private static (string RiskLevel, string Rationale) DetermineRisk(ReleaseFeatureFlagState state)
        {
            if (state.CriticalFlagCount >= 3)
                return ("High",
                    $"{state.CriticalFlagCount} critical feature flags active. High risk of unexpected blast radius.");

            if (state.CriticalFlagCount >= 1)
                return ("Medium",
                    $"{state.CriticalFlagCount} critical feature flag(s) active out of {state.ActiveFlagCount} total. " +
                    "Monitor closely for unexpected behaviours.");

            if (state.ActiveFlagCount >= 5)
                return ("Low",
                    $"{state.ActiveFlagCount} feature flags active (none critical). " +
                    "Low risk but high flag density may complicate rollback.");

            return ("Minimal",
                $"{state.ActiveFlagCount} feature flag(s) active, {state.CriticalFlagCount} critical. " +
                "Feature flag exposure is minimal for this release.");
        }
    }

    /// <summary>Resposta da consciência de feature flags da release.</summary>
    public sealed record Response(
        Guid ReleaseId,
        bool HasData,
        int ActiveFlagCount,
        int CriticalFlagCount,
        int NewFeatureFlagCount,
        string? FlagProvider,
        string RiskLevel,
        string RiskRationale,
        DateTimeOffset? RecordedAt);
}
