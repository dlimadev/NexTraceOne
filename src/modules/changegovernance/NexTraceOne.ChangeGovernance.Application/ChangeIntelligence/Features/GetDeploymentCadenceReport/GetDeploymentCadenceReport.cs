using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Abstractions;

namespace NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.GetDeploymentCadenceReport;

/// <summary>
/// Feature: GetDeploymentCadenceReport — relatório de cadência de deployment por serviço.
///
/// Classifica cada serviço numa das 4 categorias DORA de cadência de deployment:
/// - HighPerformer:  média ≥ 1 deploy/dia (múltiplas vezes por dia ou ≥ 7/semana)
/// - Medium:         média ≥ 1 deploy/semana (≥ 1/semana mas &lt; 1/dia)
/// - LowPerformer:   ≥ 1 deploy no período mas &lt; 1/semana na média
/// - Insufficient:   sem deploys no período
///
/// Alimenta dashboards de Tech Lead, CTO e Executive com visibilidade de maturidade DevOps
/// e serve de contexto complementar ao Change Frequency Heatmap.
///
/// Wave K.3b — Deployment Cadence Report (ChangeGovernance).
/// </summary>
public static class GetDeploymentCadenceReport
{
    public sealed record Query(
        int Days = 30,
        string? TeamName = null,
        string? Environment = null,
        int MaxServices = 50) : IQuery<Response>;

    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.Days).InclusiveBetween(7, 90);
            RuleFor(x => x.MaxServices).InclusiveBetween(1, 200);
            RuleFor(x => x.TeamName).MaximumLength(200).When(x => x.TeamName is not null);
            RuleFor(x => x.Environment).MaximumLength(100).When(x => x.Environment is not null);
        }
    }

    public sealed class Handler(
        IReleaseRepository releaseRepository,
        ICurrentTenant currentTenant,
        IDateTimeProvider clock) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var now = clock.UtcNow;
            var since = now.AddDays(-request.Days);

            var releases = await releaseRepository.ListInRangeAsync(since, now, request.Environment, currentTenant.Id, cancellationToken);

            // Agrupar por serviço e calcular frequência de deployment
            var byService = releases
                .GroupBy(r => r.ServiceName)
                .Take(request.MaxServices)
                .Select(g =>
                {
                    var deploysPerDay = (decimal)g.Count() / request.Days;
                    var cadence = ClassifyCadence(deploysPerDay);
                    return new ServiceCadence(
                        ServiceName: g.Key,
                        TotalDeploys: g.Count(),
                        DeploysPerDay: Math.Round(deploysPerDay, 3),
                        DeploysPerWeek: Math.Round(deploysPerDay * 7, 2),
                        Cadence: cadence);
                })
                .OrderByDescending(s => s.DeploysPerDay)
                .ToList();

            // Adicionar serviços sem deploys no período se houver TeamName filter (não temos essa info aqui facilmente)
            // Computar distribuição geral
            var distribution = new CadenceDistribution(
                HighPerformer: byService.Count(s => s.Cadence == DeploymentCadence.HighPerformer),
                Medium: byService.Count(s => s.Cadence == DeploymentCadence.Medium),
                LowPerformer: byService.Count(s => s.Cadence == DeploymentCadence.LowPerformer),
                Insufficient: byService.Count(s => s.Cadence == DeploymentCadence.Insufficient));

            var totalDeploys = releases.Count;
            var overallCadence = byService.Count > 0
                ? ClassifyCadence((decimal)totalDeploys / (byService.Count * request.Days))
                : DeploymentCadence.Insufficient;

            return Result<Response>.Success(new Response(
                GeneratedAt: now,
                PeriodDays: request.Days,
                TeamFilter: request.TeamName,
                EnvironmentFilter: request.Environment,
                TotalServices: byService.Count,
                TotalDeploys: totalDeploys,
                OverallCadence: overallCadence,
                Distribution: distribution,
                Services: byService));
        }

        private static DeploymentCadence ClassifyCadence(decimal deploysPerDay)
        {
            if (deploysPerDay >= 1.0m) return DeploymentCadence.HighPerformer;
            if (deploysPerDay >= 1.0m / 7) return DeploymentCadence.Medium;
            if (deploysPerDay > 0) return DeploymentCadence.LowPerformer;
            return DeploymentCadence.Insufficient;
        }
    }

    public enum DeploymentCadence
    {
        HighPerformer,
        Medium,
        LowPerformer,
        Insufficient
    }

    public sealed record ServiceCadence(
        string ServiceName,
        int TotalDeploys,
        decimal DeploysPerDay,
        decimal DeploysPerWeek,
        DeploymentCadence Cadence);

    public sealed record CadenceDistribution(
        int HighPerformer,
        int Medium,
        int LowPerformer,
        int Insufficient);

    public sealed record Response(
        DateTimeOffset GeneratedAt,
        int PeriodDays,
        string? TeamFilter,
        string? EnvironmentFilter,
        int TotalServices,
        int TotalDeploys,
        DeploymentCadence OverallCadence,
        CadenceDistribution Distribution,
        IReadOnlyList<ServiceCadence> Services);
}
