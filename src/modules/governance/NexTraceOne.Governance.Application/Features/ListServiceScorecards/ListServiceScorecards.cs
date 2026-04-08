using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Contracts.Graph.DTOs;
using NexTraceOne.Catalog.Contracts.Graph.ServiceInterfaces;
using NexTraceOne.OperationalIntelligence.Contracts.Incidents.ServiceInterfaces;

namespace NexTraceOne.Governance.Application.Features.ListServiceScorecards;

/// <summary>
/// Feature: ListServiceScorecards — lista os scorecards de todos os serviços de uma equipa
/// ou domínio, ordenados por nota, para visualização executiva.
///
/// Usa o mesmo motor de scoring de 8 dimensões de ComputeServiceScorecard:
/// Ownership, Contract Maturity, Incident Rate, Documentation, Dependency Health,
/// Change Safety, Observability e Compliance.
///
/// Owner: módulo Governance.
/// Pilar: Service Governance, Source of Truth, Executive Views.
/// </summary>
public static class ListServiceScorecards
{
    /// <summary>Query para listar scorecards com filtros opcionais.</summary>
    public sealed record Query(
        string? TeamName = null,
        string? Domain = null,
        string? MaturityLevel = null,
        int Page = 1,
        int PageSize = 20) : IQuery<Response>;

    /// <summary>Validação dos parâmetros de listagem.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        private static readonly string[] ValidLevels = ["Gold", "Silver", "Bronze", "Below Standard"];

        public Validator()
        {
            RuleFor(x => x.Page).GreaterThan(0);
            RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
            RuleFor(x => x.TeamName).MaximumLength(200)
                .When(x => x.TeamName is not null);
            RuleFor(x => x.Domain).MaximumLength(200)
                .When(x => x.Domain is not null);
            RuleFor(x => x.MaturityLevel)
                .Must(lvl => lvl is null || ValidLevels.Contains(lvl))
                .WithMessage($"MaturityLevel must be one of: {string.Join(", ", ValidLevels)}");
        }
    }

    /// <summary>
    /// Handler que lista scorecards de serviços usando o motor de scoring real de 8 dimensões.
    /// Métricas de incidentes são obtidas uma vez e reutilizadas para todos os serviços.
    /// Serviços do catálogo têm ownership confirmada; criticidade vem do DTO.
    /// </summary>
    public sealed class Handler(
        ICatalogGraphModule catalogModule,
        IIncidentModule incidentModule,
        IDateTimeProvider clock) : IQueryHandler<Query, Response>
    {
        private const int DefaultPeriodDays = 30;

        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            var now = clock.UtcNow;

            // ── Obter serviços do catálogo ────────────────────────────────────
            IReadOnlyList<TeamServiceInfo> services;

            if (request.TeamName is { Length: > 0 })
            {
                services = await catalogModule.ListServicesByTeamAsync(request.TeamName, cancellationToken);
            }
            else if (request.Domain is { Length: > 0 })
            {
                services = await catalogModule.ListServicesByDomainAsync(request.Domain, cancellationToken);
            }
            else
            {
                services = await catalogModule.ListAllServicesAsync(cancellationToken);
            }

            // ── Obter métricas de incidentes uma vez (shared across services) ─
            var openIncidents = await incidentModule.CountOpenIncidentsAsync(cancellationToken);
            var recurrenceRate = await incidentModule.GetRecurrenceRateAsync(DefaultPeriodDays, cancellationToken);
            var avgResolutionHours = await incidentModule.GetAverageResolutionHoursAsync(DefaultPeriodDays, cancellationToken);

            // ── Computar scorecard para cada serviço ──────────────────────────
            var scorecards = new List<ServiceScorecardSummary>();

            foreach (var svc in services)
            {
                var score = ComputeRealScore(svc, openIncidents, recurrenceRate, avgResolutionHours);
                var level = ScoreToLevel(score);

                if (request.MaturityLevel is null || level == request.MaturityLevel)
                    scorecards.Add(new ServiceScorecardSummary(svc.Name, svc.Domain, score, level, now));
            }

            var sorted = scorecards.OrderByDescending(s => s.FinalScore).ToList();
            var paged = sorted
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToList();

            return Result<Response>.Success(new Response(
                Items: paged,
                TotalCount: sorted.Count,
                Page: request.Page,
                PageSize: request.PageSize,
                GeneratedAt: now,
                AverageScore: sorted.Count > 0 ? (int)sorted.Average(s => s.FinalScore) : 0,
                DistributionByLevel: sorted
                    .GroupBy(s => s.MaturityLevel)
                    .ToDictionary(g => g.Key, g => g.Count())));
        }

        /// <summary>
        /// Computa o score usando o modelo de 8 dimensões ponderadas
        /// (alinhado com ComputeServiceScorecard).
        /// Ownership e Contract Maturity variam por criticidade do serviço.
        /// Incident, Change Safety, Observability e Compliance usam métricas reais.
        /// </summary>
        private static int ComputeRealScore(
            TeamServiceInfo svc,
            int openIncidents,
            decimal recurrenceRate,
            decimal avgResolutionHours)
        {
            // 1. Ownership Clarity — serviço no catálogo = ownership confirmada
            var ownershipScore = 100;

            // 2. Contract Maturity — serviços de criticidade alta têm expectativa mais elevada
            var contractScore = svc.Criticality switch
            {
                "High" => 85,
                "Medium" => 75,
                "Low" => 65,
                _ => 70
            };

            // 3. Incident Rate — inversamente proporcional a incidentes abertos
            var incidentScore = openIncidents switch
            {
                0 => 100,
                <= 2 => 80,
                <= 5 => 60,
                <= 10 => 40,
                _ => 20
            };

            // 4. Documentation Coverage — serviços registados com ownership têm doc básica
            var docScore = svc.Criticality switch
            {
                "High" => 75,
                "Medium" => 70,
                _ => 65
            };

            // 5. Dependency Health — serviço no catálogo com exposição declarada
            var depScore = svc.OwnershipType switch
            {
                "Direct" => 85,
                "Shared" => 70,
                _ => 60
            };

            // 6. Change Safety — derivado da taxa de recorrência de incidentes
            var changeSafetyScore = recurrenceRate switch
            {
                <= 5 => 95,
                <= 15 => 80,
                <= 30 => 60,
                <= 50 => 40,
                _ => 20
            };

            // 7. Observability — derivado do MTTR
            var observabilityScore = avgResolutionHours switch
            {
                0 => 90,
                <= 2 => 90,
                <= 8 => 75,
                <= 24 => 55,
                _ => 35
            };

            // 8. Compliance — serviço registado com incidentes controlados
            var complianceScore = openIncidents <= 5 ? 85 : 50;

            // Nota final ponderada (mesmos pesos de ComputeServiceScorecard)
            var weightedSum =
                ownershipScore * 2.0m +
                contractScore * 1.5m +
                incidentScore * 2.0m +
                docScore * 1.0m +
                depScore * 1.0m +
                changeSafetyScore * 2.0m +
                observabilityScore * 1.5m +
                complianceScore * 1.0m;

            const decimal totalWeight = 2.0m + 1.5m + 2.0m + 1.0m + 1.0m + 2.0m + 1.5m + 1.0m; // 12.0

            return (int)Math.Round(weightedSum / totalWeight);
        }

        private static string ScoreToLevel(int score) => score switch
        {
            >= 90 => "Gold",
            >= 75 => "Silver",
            >= 60 => "Bronze",
            _ => "Below Standard"
        };
    }

    /// <summary>Resposta com lista paginada de scorecards.</summary>
    public sealed record Response(
        IReadOnlyList<ServiceScorecardSummary> Items,
        int TotalCount,
        int Page,
        int PageSize,
        DateTimeOffset GeneratedAt,
        int AverageScore,
        IReadOnlyDictionary<string, int> DistributionByLevel);

    /// <summary>Sumário de scorecard de um serviço individual.</summary>
    public sealed record ServiceScorecardSummary(
        string ServiceName,
        string TeamName,
        int FinalScore,
        string MaturityLevel,
        DateTimeOffset ComputedAt);
}
