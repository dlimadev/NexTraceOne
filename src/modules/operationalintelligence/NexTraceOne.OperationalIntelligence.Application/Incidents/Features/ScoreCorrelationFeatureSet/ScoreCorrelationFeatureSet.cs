using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.OperationalIntelligence.Application.Incidents.Abstractions;
using NexTraceOne.OperationalIntelligence.Domain.Incidents.Errors;

namespace NexTraceOne.OperationalIntelligence.Application.Incidents.Features.ScoreCorrelationFeatureSet;

/// <summary>
/// Feature: ScoreCorrelationFeatureSet — calcula um conjunto multidimensional de features
/// de correlação entre um incidente e uma mudança (change/release).
///
/// Dimensões calculadas:
///   - TemporalProximityScore: proximidade temporal entre a detecção do incidente e o deploy
///   - ServiceMatchScore: grau de correspondência de serviço (exacta=1.0, parcial=0.6, nenhuma=0.0)
///   - OwnershipAlignmentScore: alinhamento de equipa responsável (ownerTeam string comparison)
///   - WeightedTotalScore: soma ponderada das três dimensões acima
///   - ConfidenceLabel: classificação textual do score total (High≥0.7, Medium≥0.4, Low&lt;0.4)
///
/// Pesos por omissão: temporal=0.4, service=0.4, ownership=0.2.
/// Configurable via platform config keys oi.correlation.feature.*.
/// Estrutura VSA: Query + Validator + Handler + Response em ficheiro único.
/// </summary>
public static class ScoreCorrelationFeatureSet
{
    private const double DefaultTemporalWeight = 0.4;
    private const double DefaultServiceWeight = 0.4;
    private const double DefaultOwnershipWeight = 0.2;
    private const double TemporalWindowHours = 24.0;

    /// <summary>Query para calcular o feature score entre um incidente e uma mudança.</summary>
    public sealed record Query(Guid IncidentId, Guid ChangeId) : IQuery<Response>;

    /// <summary>Valida os parâmetros da query.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.IncidentId).NotEmpty();
            RuleFor(x => x.ChangeId).NotEmpty();
        }
    }

    /// <summary>Handler que computa o feature score de correlação multidimensional.</summary>
    public sealed class Handler(
        IIncidentStore incidentStore,
        ICorrelationFeatureReader featureReader) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            var context = incidentStore.GetIncidentCorrelationContext(request.IncidentId.ToString());
            if (context is null)
                return IncidentErrors.IncidentNotFound(request.IncidentId.ToString());

            var changeDetails = await featureReader.GetChangeDetailsAsync(request.ChangeId, cancellationToken);
            if (changeDetails is null)
            {
                return Result<Response>.Success(new Response(
                    request.IncidentId,
                    request.ChangeId,
                    TemporalProximityScore: 0.0,
                    ServiceMatchScore: 0.0,
                    OwnershipAlignmentScore: 0.0,
                    WeightedTotalScore: 0.0,
                    ConfidenceLabel: "Low",
                    Explanation: "Change details not available. Feature scoring requires accessible change data."));
            }

            var temporalScore = ComputeTemporalScore(context.DetectedAtUtc, changeDetails.DeployedAt);
            var serviceScore = ComputeServiceScore(context.ServiceId, context.ServiceDisplayName, changeDetails.ServiceName);
            var ownershipScore = ComputeOwnershipScore(context.ServiceDisplayName, changeDetails.OwnerTeam);
            var weighted = (temporalScore * DefaultTemporalWeight)
                         + (serviceScore * DefaultServiceWeight)
                         + (ownershipScore * DefaultOwnershipWeight);

            var label = weighted >= 0.7 ? "High" : weighted >= 0.4 ? "Medium" : "Low";

            return Result<Response>.Success(new Response(
                request.IncidentId,
                request.ChangeId,
                Math.Round(temporalScore, 4),
                Math.Round(serviceScore, 4),
                Math.Round(ownershipScore, 4),
                Math.Round(weighted, 4),
                label,
                Explanation: null));
        }

        internal static double ComputeTemporalScore(DateTimeOffset incidentDetectedAt, DateTimeOffset deployedAt)
        {
            var diffHours = Math.Abs((incidentDetectedAt - deployedAt).TotalHours);
            if (diffHours > TemporalWindowHours) return 0.0;
            return 1.0 - (diffHours / TemporalWindowHours);
        }

        internal static double ComputeServiceScore(string serviceId, string serviceDisplayName, string changeServiceName)
        {
            if (changeServiceName.Equals(serviceId, StringComparison.OrdinalIgnoreCase)
                || changeServiceName.Equals(serviceDisplayName, StringComparison.OrdinalIgnoreCase))
                return 1.0;

            if (changeServiceName.Contains(serviceId, StringComparison.OrdinalIgnoreCase)
                || changeServiceName.Contains(serviceDisplayName, StringComparison.OrdinalIgnoreCase)
                || serviceId.Contains(changeServiceName, StringComparison.OrdinalIgnoreCase)
                || serviceDisplayName.Contains(changeServiceName, StringComparison.OrdinalIgnoreCase))
                return 0.6;

            return 0.0;
        }

        internal static double ComputeOwnershipScore(string incidentServiceDisplay, string changeOwnerTeam)
        {
            if (string.IsNullOrWhiteSpace(incidentServiceDisplay) || string.IsNullOrWhiteSpace(changeOwnerTeam))
                return 0.0;

            return incidentServiceDisplay.Equals(changeOwnerTeam, StringComparison.OrdinalIgnoreCase) ? 1.0 : 0.0;
        }
    }

    /// <summary>Resposta com o feature score multidimensional de correlação.</summary>
    public sealed record Response(
        Guid IncidentId,
        Guid ChangeId,
        double TemporalProximityScore,
        double ServiceMatchScore,
        double OwnershipAlignmentScore,
        double WeightedTotalScore,
        string ConfidenceLabel,
        string? Explanation);
}
