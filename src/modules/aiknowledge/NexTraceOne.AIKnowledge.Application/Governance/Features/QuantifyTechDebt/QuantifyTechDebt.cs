using FluentValidation;
using NexTraceOne.AIKnowledge.Domain.Governance.ValueObjects;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.AIKnowledge.Application.Governance.Features.QuantifyTechDebt;

/// <summary>
/// Feature: QuantifyTechDebt — quantifica dívida técnica e seu impacto financeiro em serviços.
/// Estrutura VSA: Query + Validator + Handler + Response num único ficheiro.
/// </summary>
public static class QuantifyTechDebt
{
    public sealed record Query(
        string ServiceName,
        Guid TenantId,
        int IncidentCountLast90Days,
        double TestCoveragePercent,
        int CircularDependencies,
        double AveragePrSizeLines,
        double AverageMttrMinutes,
        decimal HourlyEngineeringRate) : IQuery<Response>;

    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.ServiceName).NotEmpty();
            RuleFor(x => x.TenantId).NotEmpty();
            RuleFor(x => x.TestCoveragePercent).InclusiveBetween(0, 100);
            RuleFor(x => x.HourlyEngineeringRate).GreaterThan(0);
        }
    }

    public sealed class Handler(IDateTimeProvider clock) : IQueryHandler<Query, Response>
    {
        public Task<Result<Response>> Handle(Query request, CancellationToken ct)
        {
            var items = new List<TechDebtItem>();

            if (request.CircularDependencies > 0)
            {
                var monthlyCost = (decimal)(request.AverageMttrMinutes * 0.1 * (double)request.HourlyEngineeringRate * 12);
                items.Add(new TechDebtItem(
                    "architecture",
                    $"{request.CircularDependencies} circular dependência(s) detectada(s)",
                    request.CircularDependencies >= 3 ? "Critical" : "High",
                    monthlyCost,
                    Math.Min(0.9, request.CircularDependencies * 0.12),
                    request.CircularDependencies * 5,
                    Math.Round(monthlyCost > 0 ? (request.CircularDependencies * 5 * (double)request.HourlyEngineeringRate / 8) / (double)monthlyCost : 0, 1)));
            }

            if (request.TestCoveragePercent < 60)
            {
                var incidentCostPerMonth = (decimal)(request.IncidentCountLast90Days / 3.0) * 800m;
                items.Add(new TechDebtItem(
                    "quality",
                    $"Cobertura de testes baixa ({request.TestCoveragePercent:F0}%)",
                    request.TestCoveragePercent < 30 ? "Critical" : "High",
                    incidentCostPerMonth * 0.5m,
                    Math.Min(0.8, (100 - request.TestCoveragePercent) / 100.0 * 0.5),
                    (int)Math.Ceiling((60 - request.TestCoveragePercent) / 5),
                    incidentCostPerMonth > 0 ? Math.Round((double)((int)Math.Ceiling((60 - request.TestCoveragePercent) / 5) * (double)request.HourlyEngineeringRate * 8) / (double)incidentCostPerMonth * 0.5, 1) : 0));
            }

            if (request.IncidentCountLast90Days > 5)
            {
                var incidentCost = (decimal)(request.IncidentCountLast90Days / 3.0 * request.AverageMttrMinutes / 60.0) * request.HourlyEngineeringRate;
                items.Add(new TechDebtItem(
                    "operations",
                    $"{request.IncidentCountLast90Days} incidentes nos últimos 90 dias (MTTR médio: {request.AverageMttrMinutes:F0} min)",
                    request.IncidentCountLast90Days > 15 ? "Critical" : "High",
                    incidentCost,
                    Math.Min(0.85, request.IncidentCountLast90Days / 20.0),
                    10,
                    incidentCost > 0 ? Math.Round(10.0 * (double)request.HourlyEngineeringRate * 8 / (double)incidentCost, 1) : 0));
            }

            if (request.AveragePrSizeLines > 500)
            {
                items.Add(new TechDebtItem(
                    "velocity",
                    $"PRs grandes (média: {request.AveragePrSizeLines:F0} linhas) — revisão lenta e risco alto",
                    "Medium",
                    request.HourlyEngineeringRate * 5,
                    0.15,
                    3,
                    3.0));
            }

            var totalMonthlyCost = items.Sum(i => i.MonthlyCostEstimate);

            return Task.FromResult(Result<Response>.Success(new Response(
                ServiceName: request.ServiceName,
                Items: items,
                TotalMonthlyCostEstimate: totalMonthlyCost,
                TotalAnnualCostEstimate: totalMonthlyCost * 12,
                HighestPriorityItem: items.OrderByDescending(i => i.MonthlyCostEstimate).FirstOrDefault()?.Description,
                AnalysedAt: clock.UtcNow)));
        }
    }

    public sealed record Response(
        string ServiceName,
        IReadOnlyList<TechDebtItem> Items,
        decimal TotalMonthlyCostEstimate,
        decimal TotalAnnualCostEstimate,
        string? HighestPriorityItem,
        DateTimeOffset AnalysedAt);
}
