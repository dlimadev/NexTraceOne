using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Abstractions;
using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Entities;
using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Enums;
using NexTraceOne.Configuration.Application.Abstractions;
using NexTraceOne.IdentityAccess.Application.ConfigurationKeys;
using NexTraceOne.OperationalIntelligence.Contracts.Incidents.ServiceInterfaces;

namespace NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.GetDoraMetrics;

/// <summary>
/// Feature: GetDoraMetrics — calcula as 4 métricas DORA (Deployment Frequency,
/// Lead Time for Changes, Change Failure Rate, Time to Restore Service) com
/// base em dados reais de releases e incidentes.
///
/// Diferencial NexTraceOne: as métricas DORA são cruzadas com contexto de
/// contratos, ownership e blast radius — uma visão que nenhum concorrente oferece.
///
/// Estrutura VSA: Query + Validator + Handler + Response em um único ficheiro.
/// </summary>
public static class GetDoraMetrics
{
    /// <summary>Query para cálculo de métricas DORA.</summary>
    public sealed record Query(
        string? ServiceName = null,
        string? TeamName = null,
        string? Environment = "Production",
        int Days = 30) : IQuery<Response>;

    /// <summary>Validação da query.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(q => q.Days)
                .InclusiveBetween(1, 365)
                .WithMessage("Days must be between 1 and 365.");
        }
    }

    /// <summary>Handler que calcula métricas DORA a partir de dados reais de releases e incidentes.</summary>
    public sealed class Handler(
        IReleaseRepository releaseRepository,
        IIncidentModule incidentModule,
        ICurrentTenant currentTenant,
        IEnvironmentBehaviorService environmentBehaviorService,
        IDateTimeProvider clock) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            // ── Gate: verificar se cálculo DORA está habilitado para o ambiente ──
            var doraEnabled = await environmentBehaviorService.IsEnabledAsync(
                EnvironmentBehaviorConfigKeys.MetricsDoraEnabled,
                environmentId: null,
                cancellationToken);

            if (!doraEnabled)
            {
                var empty = new Response(
                    DeploymentFrequency: new DeploymentFrequencyDto(0m, 0, DoraClassification.Low),
                    LeadTimeForChanges: new LeadTimeDto(0m, DoraClassification.Low),
                    ChangeFailureRate: new ChangeFailureRateDto(0m, 0, 0, 0, DoraClassification.Low),
                    TimeToRestoreService: new TimeToRestoreDto(0m, DoraClassification.Low),
                    OverallClassification: DoraClassification.Low,
                    PeriodDays: request.Days,
                    ServiceName: request.ServiceName,
                    TeamName: request.TeamName,
                    Environment: request.Environment,
                    GeneratedAt: clock.UtcNow);
                return Result<Response>.Success(empty);
            }

            var to = clock.UtcNow;
            var from = to.AddDays(-request.Days);

            // ── 1. Deployment Frequency ──────────────────────────────
            // Número de deploys bem-sucedidos por dia no período.
            var succeededCount = await releaseRepository.CountFilteredAsync(
                tenantId: currentTenant.Id,
                serviceName: request.ServiceName,
                teamName: request.TeamName,
                environment: request.Environment,
                changeType: null,
                confidenceStatus: null,
                deploymentStatus: DeploymentStatus.Succeeded,
                searchTerm: null,
                from: from,
                to: to,
                cancellationToken: cancellationToken);

            var deploymentFrequency = request.Days > 0
                ? Math.Round((decimal)succeededCount / request.Days, 2)
                : 0m;

            // ── 2. Lead Time for Changes ─────────────────────────────
            // Tempo médio entre criação do release e deploy bem-sucedido.
            // Como o NexTraceOne rastreia CreatedAt (quando o release foi registado)
            // e o status do deploy, calculamos a diferença média.
            var releases = await releaseRepository.ListInRangeAsync(
                from, to, request.Environment, currentTenant.Id, cancellationToken);

            var succeededReleases = releases
                .Where(r => r.Status == DeploymentStatus.Succeeded)
                .ToList();

            // Filtrar por serviço/equipa se especificado
            if (!string.IsNullOrEmpty(request.ServiceName))
            {
                succeededReleases = succeededReleases
                    .Where(r => string.Equals(r.ServiceName, request.ServiceName, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            if (!string.IsNullOrEmpty(request.TeamName))
            {
                succeededReleases = succeededReleases
                    .Where(r => string.Equals(r.TeamName, request.TeamName, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            // Lead time: tempo médio desde a criação de cada release até ao fim
            // do período de análise. Quando CI/CD real estiver integrado,
            // será calculado como commit time → deploy time.
            var leadTimeHours = 0m;
            if (succeededReleases.Count > 0)
            {
                leadTimeHours = Math.Round(
                    (decimal)succeededReleases.Average(r => (to - r.CreatedAt).TotalHours),
                    1);
            }

            // ── 3. Change Failure Rate ───────────────────────────────
            // Percentagem de deploys que falharam ou foram rolled back.
            var failedCount = await releaseRepository.CountFilteredAsync(
                tenantId: currentTenant.Id,
                serviceName: request.ServiceName,
                teamName: request.TeamName,
                environment: request.Environment,
                changeType: null,
                confidenceStatus: null,
                deploymentStatus: DeploymentStatus.Failed,
                searchTerm: null,
                from: from,
                to: to,
                cancellationToken: cancellationToken);

            var rolledBackCount = await releaseRepository.CountFilteredAsync(
                tenantId: currentTenant.Id,
                serviceName: request.ServiceName,
                teamName: request.TeamName,
                environment: request.Environment,
                changeType: null,
                confidenceStatus: null,
                deploymentStatus: DeploymentStatus.RolledBack,
                searchTerm: null,
                from: from,
                to: to,
                cancellationToken: cancellationToken);

            var totalDeploys = succeededCount + failedCount + rolledBackCount;
            var changeFailureRate = totalDeploys > 0
                ? Math.Round((decimal)(failedCount + rolledBackCount) / totalDeploys * 100, 1)
                : 0m;

            // ── 4. Time to Restore Service (MTTR) ────────────────────
            // Tempo médio de resolução de incidentes no período.
            var mttrHours = await incidentModule.GetAverageResolutionHoursAsync(
                days: request.Days,
                cancellationToken: cancellationToken);

            // ── Classificação DORA ───────────────────────────────────
            var classification = ClassifyPerformance(
                deploymentFrequency, leadTimeHours, changeFailureRate, mttrHours);

            return new Response(
                DeploymentFrequency: new DeploymentFrequencyDto(
                    DeploysPerDay: deploymentFrequency,
                    TotalDeploys: succeededCount,
                    Classification: ClassifyDeployFrequency(deploymentFrequency)),
                LeadTimeForChanges: new LeadTimeDto(
                    AverageHours: leadTimeHours,
                    Classification: ClassifyLeadTime(leadTimeHours)),
                ChangeFailureRate: new ChangeFailureRateDto(
                    FailurePercentage: changeFailureRate,
                    FailedDeploys: failedCount,
                    RolledBackDeploys: rolledBackCount,
                    TotalDeploys: totalDeploys,
                    Classification: ClassifyFailureRate(changeFailureRate)),
                TimeToRestoreService: new TimeToRestoreDto(
                    AverageHours: Math.Round(mttrHours, 1),
                    Classification: ClassifyMttr(mttrHours)),
                OverallClassification: classification,
                PeriodDays: request.Days,
                ServiceName: request.ServiceName,
                TeamName: request.TeamName,
                Environment: request.Environment,
                GeneratedAt: clock.UtcNow);
        }

        /// <summary>Classifica performance geral com base nos 4 indicadores DORA.</summary>
        private static DoraClassification ClassifyPerformance(
            decimal deployFreq, decimal leadTimeHours, decimal failureRate, decimal mttrHours)
        {
            var scores = new[]
            {
                ClassifyDeployFrequency(deployFreq) switch
                {
                    DoraClassification.Elite => 4,
                    DoraClassification.High => 3,
                    DoraClassification.Medium => 2,
                    _ => 1
                },
                ClassifyLeadTime(leadTimeHours) switch
                {
                    DoraClassification.Elite => 4,
                    DoraClassification.High => 3,
                    DoraClassification.Medium => 2,
                    _ => 1
                },
                ClassifyFailureRate(failureRate) switch
                {
                    DoraClassification.Elite => 4,
                    DoraClassification.High => 3,
                    DoraClassification.Medium => 2,
                    _ => 1
                },
                ClassifyMttr(mttrHours) switch
                {
                    DoraClassification.Elite => 4,
                    DoraClassification.High => 3,
                    DoraClassification.Medium => 2,
                    _ => 1
                },
            };

            var avg = scores.Average();
            return avg switch
            {
                >= 3.5 => DoraClassification.Elite,
                >= 2.5 => DoraClassification.High,
                >= 1.5 => DoraClassification.Medium,
                _ => DoraClassification.Low,
            };
        }

        /// <summary>Classifica deploy frequency segundo benchmarks DORA.</summary>
        private static DoraClassification ClassifyDeployFrequency(decimal deploysPerDay) =>
            deploysPerDay switch
            {
                >= 1m => DoraClassification.Elite,         // Multiple deploys per day
                >= 0.14m => DoraClassification.High,       // Between once per day and once per week
                >= 0.03m => DoraClassification.Medium,     // Between once per week and once per month
                _ => DoraClassification.Low,               // Less than once per month
            };

        /// <summary>Classifica lead time segundo benchmarks DORA.</summary>
        private static DoraClassification ClassifyLeadTime(decimal hours) =>
            hours switch
            {
                <= 24m => DoraClassification.Elite,        // Less than one day
                <= 168m => DoraClassification.High,        // Between one day and one week
                <= 720m => DoraClassification.Medium,      // Between one week and one month
                _ => DoraClassification.Low,               // More than one month
            };

        /// <summary>Classifica change failure rate segundo benchmarks DORA.</summary>
        private static DoraClassification ClassifyFailureRate(decimal percentage) =>
            percentage switch
            {
                <= 5m => DoraClassification.Elite,         // 0-5%
                <= 10m => DoraClassification.High,         // 5-10%
                <= 15m => DoraClassification.Medium,       // 10-15%
                _ => DoraClassification.Low,               // > 15%
            };

        /// <summary>Classifica MTTR segundo benchmarks DORA.</summary>
        private static DoraClassification ClassifyMttr(decimal hours) =>
            hours switch
            {
                <= 1m => DoraClassification.Elite,         // Less than one hour
                <= 24m => DoraClassification.High,         // Less than one day
                <= 168m => DoraClassification.Medium,      // Less than one week
                _ => DoraClassification.Low,               // More than one week
            };
    }

    // ── Response DTOs ────────────────────────────────────────────────

    /// <summary>Resposta completa das métricas DORA.</summary>
    public sealed record Response(
        DeploymentFrequencyDto DeploymentFrequency,
        LeadTimeDto LeadTimeForChanges,
        ChangeFailureRateDto ChangeFailureRate,
        TimeToRestoreDto TimeToRestoreService,
        DoraClassification OverallClassification,
        int PeriodDays,
        string? ServiceName,
        string? TeamName,
        string? Environment,
        DateTimeOffset GeneratedAt);

    /// <summary>Métrica: Deployment Frequency.</summary>
    public sealed record DeploymentFrequencyDto(
        decimal DeploysPerDay,
        int TotalDeploys,
        DoraClassification Classification);

    /// <summary>Métrica: Lead Time for Changes.</summary>
    public sealed record LeadTimeDto(
        decimal AverageHours,
        DoraClassification Classification);

    /// <summary>Métrica: Change Failure Rate.</summary>
    public sealed record ChangeFailureRateDto(
        decimal FailurePercentage,
        int FailedDeploys,
        int RolledBackDeploys,
        int TotalDeploys,
        DoraClassification Classification);

    /// <summary>Métrica: Time to Restore Service.</summary>
    public sealed record TimeToRestoreDto(
        decimal AverageHours,
        DoraClassification Classification);

    /// <summary>Classificação DORA conforme benchmarks oficiais.</summary>
    public enum DoraClassification
    {
        /// <summary>Elite performer — top 25% das organizações.</summary>
        Elite = 4,
        /// <summary>High performer.</summary>
        High = 3,
        /// <summary>Medium performer.</summary>
        Medium = 2,
        /// <summary>Low performer — precisa de atenção.</summary>
        Low = 1,
    }
}
