using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Abstractions;

namespace NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.GetPromotionReadinessDelta;

/// <summary>
/// Feature: GetPromotionReadinessDelta — devolve deltas relativos de comportamento
/// runtime (erro, latência, throughput, custo, incidentes) entre dois ambientes
/// para um serviço, prontos para alimentar a UX do ReleaseTrain e um gate de
/// promoção consciente do risco.
///
/// Esta feature é dona da *decisão* (ChangeGovernance) e depende da superfície
/// <see cref="IRuntimeComparisonReader"/> para obter dados comparativos. Por
/// defeito o reader é o null-honest que marca o resultado como simulado; uma
/// bridge real p/ OperationalIntelligence pode ser registada na composition
/// root sem quebrar este bounded context.
/// </summary>
public static class GetPromotionReadinessDelta
{
    /// <summary>Janela máxima (em dias) aceite pela query.</summary>
    public const int MaxWindowDays = 60;

    /// <summary>Janela default quando o chamador não informa <c>WindowDays</c>.</summary>
    public const int DefaultWindowDays = 7;

    /// <summary>Query de consulta do delta de prontidão para promoção.</summary>
    public sealed record Query(
        string ServiceName,
        string EnvironmentFrom,
        string EnvironmentTo,
        int? WindowDays) : IQuery<Response>;

    /// <summary>Valida a entrada da query.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.ServiceName).NotEmpty().MaximumLength(200);
            RuleFor(x => x.EnvironmentFrom).NotEmpty().MaximumLength(100);
            RuleFor(x => x.EnvironmentTo).NotEmpty().MaximumLength(100);
            RuleFor(x => x.EnvironmentTo)
                .NotEqual(x => x.EnvironmentFrom)
                .WithMessage("EnvironmentTo must differ from EnvironmentFrom.");
            RuleFor(x => x.WindowDays!.Value)
                .InclusiveBetween(1, MaxWindowDays)
                .When(x => x.WindowDays.HasValue);
        }
    }

    /// <summary>Handler que devolve o snapshot comparativo normalizado.</summary>
    public sealed class Handler(
        IRuntimeComparisonReader reader,
        ICurrentTenant currentTenant) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var window = request.WindowDays ?? DefaultWindowDays;

            var snapshot = await reader.CompareAsync(
                currentTenant.Id,
                request.ServiceName,
                request.EnvironmentFrom,
                request.EnvironmentTo,
                window,
                cancellationToken);

            var readiness = ComputeReadiness(snapshot);

            return new Response(
                ServiceName: snapshot.ServiceName,
                EnvironmentFrom: snapshot.EnvironmentFrom,
                EnvironmentTo: snapshot.EnvironmentTo,
                WindowDays: snapshot.WindowDays,
                ErrorRateDelta: snapshot.ErrorRateDelta,
                LatencyP95DeltaMs: snapshot.LatencyP95DeltaMs,
                ThroughputDelta: snapshot.ThroughputDelta,
                CostDelta: snapshot.CostDelta,
                IncidentsDelta: snapshot.IncidentsDelta,
                DataQuality: snapshot.DataQuality,
                Readiness: readiness,
                SimulatedNote: snapshot.SimulatedNote);
        }

        /// <summary>
        /// Heurística defensiva: quando não há dados suficientes o resultado
        /// é sempre <c>Unknown</c>, nunca um falso positivo de "pronto".
        /// </summary>
        private static PromotionReadinessLevel ComputeReadiness(RuntimeComparisonSnapshot snapshot)
        {
            if (snapshot.DataQuality <= 0m)
                return PromotionReadinessLevel.Unknown;

            var error = snapshot.ErrorRateDelta;
            var latency = snapshot.LatencyP95DeltaMs;
            var incidents = snapshot.IncidentsDelta;

            if ((error.HasValue && error.Value > 0.05m)
                || (latency.HasValue && latency.Value > 250m)
                || (incidents.HasValue && incidents.Value > 0))
            {
                return PromotionReadinessLevel.Blocked;
            }

            if ((error.HasValue && error.Value > 0.01m)
                || (latency.HasValue && latency.Value > 75m))
            {
                return PromotionReadinessLevel.Review;
            }

            return PromotionReadinessLevel.Ready;
        }
    }

    /// <summary>Resposta normalizada para a UX de ReleaseTrain/Promotion Gate.</summary>
    public sealed record Response(
        string ServiceName,
        string EnvironmentFrom,
        string EnvironmentTo,
        int WindowDays,
        decimal? ErrorRateDelta,
        decimal? LatencyP95DeltaMs,
        decimal? ThroughputDelta,
        decimal? CostDelta,
        int? IncidentsDelta,
        decimal DataQuality,
        PromotionReadinessLevel Readiness,
        string? SimulatedNote);

    /// <summary>Classificação de readiness para promoção.</summary>
    public enum PromotionReadinessLevel
    {
        /// <summary>Sem dados suficientes para decidir.</summary>
        Unknown = 0,
        /// <summary>Pronto para promover.</summary>
        Ready = 1,
        /// <summary>Promoção requer revisão humana.</summary>
        Review = 2,
        /// <summary>Promoção deve ser bloqueada.</summary>
        Blocked = 3
    }
}
