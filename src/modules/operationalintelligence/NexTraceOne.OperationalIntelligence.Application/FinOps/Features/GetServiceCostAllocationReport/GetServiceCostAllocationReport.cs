using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.OperationalIntelligence.Application.FinOps.Abstractions;
using NexTraceOne.OperationalIntelligence.Domain.FinOps.Enums;

namespace NexTraceOne.OperationalIntelligence.Application.FinOps.Features.GetServiceCostAllocationReport;

/// <summary>
/// Feature: GetServiceCostAllocationReport — relatório de alocação de custo por serviço e categoria.
///
/// Agrega custos por serviço no período, com subtotais por categoria e ambiente.
/// Adequado para Tech Lead, FinOps e Executive persona views.
///
/// Wave I.2 — FinOps Contextual por Serviço (OperationalIntelligence).
/// </summary>
public static class GetServiceCostAllocationReport
{
    public sealed record Query(
        string TenantId,
        int Days = 30,
        string? ServiceName = null,
        string? Environment = null,
        CostCategory? Category = null) : IQuery<Response>;

    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.TenantId).NotEmpty().MaximumLength(100);
            RuleFor(x => x.Days).InclusiveBetween(1, 365);
        }
    }

    public sealed class Handler(
        IServiceCostAllocationRepository repository,
        IDateTimeProvider clock) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var now = clock.UtcNow;
            var since = now.AddDays(-request.Days);

            var records = await repository.ListByTenantAsync(
                request.TenantId, since, now,
                request.Environment, request.Category, cancellationToken);

            var filtered = request.ServiceName is not null
                ? records.Where(r => r.ServiceName.Equals(request.ServiceName, StringComparison.OrdinalIgnoreCase)).ToList()
                : records.ToList();

            // Agrega por serviço
            var byService = filtered
                .GroupBy(r => r.ServiceName)
                .Select(g =>
                {
                    var byCategory = g
                        .GroupBy(r => r.Category)
                        .Select(c => new CategoryBreakdown(c.Key, Math.Round(c.Sum(r => r.AmountUsd), 2)))
                        .OrderByDescending(c => c.AmountUsd)
                        .ToList();

                    return new ServiceCostSummary(
                        ServiceName: g.Key,
                        TotalAmountUsd: Math.Round(g.Sum(r => r.AmountUsd), 2),
                        RecordCount: g.Count(),
                        ByCategory: byCategory);
                })
                .OrderByDescending(s => s.TotalAmountUsd)
                .ToList();

            var grandTotal = Math.Round(filtered.Sum(r => r.AmountUsd), 2);

            return Result<Response>.Success(new Response(
                GeneratedAt: now,
                PeriodDays: request.Days,
                TenantId: request.TenantId,
                ServiceFilter: request.ServiceName,
                EnvironmentFilter: request.Environment,
                CategoryFilter: request.Category,
                GrandTotalUsd: grandTotal,
                TotalRecords: filtered.Count,
                Services: byService));
        }
    }

    public sealed record CategoryBreakdown(CostCategory Category, decimal AmountUsd);

    public sealed record ServiceCostSummary(
        string ServiceName,
        decimal TotalAmountUsd,
        int RecordCount,
        IReadOnlyList<CategoryBreakdown> ByCategory);

    public sealed record Response(
        DateTimeOffset GeneratedAt,
        int PeriodDays,
        string TenantId,
        string? ServiceFilter,
        string? EnvironmentFilter,
        CostCategory? CategoryFilter,
        decimal GrandTotalUsd,
        int TotalRecords,
        IReadOnlyList<ServiceCostSummary> Services);
}
