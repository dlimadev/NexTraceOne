using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.OperationalIntelligence.Application.Cost.Abstractions;

namespace NexTraceOne.OperationalIntelligence.Application.Cost.Features.GetFocusExport;

/// <summary>
/// Feature: GetFocusExport — exportação de dados de custo no formato FOCUS (FinOps Open Cost and Usage Specification).
/// Gera um payload compatível com FOCUS 1.0 para integração com ferramentas de FinOps externas.
/// Pilar: FinOps contextual. Owner: OI Cost.
/// </summary>
public static class GetFocusExport
{
    public sealed record Query(
        string Period,
        string? ServiceName = null,
        string? TeamName = null) : IQuery<Response>;

    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.Period).NotEmpty().MaximumLength(20).Matches(@"^\d{4}-\d{2}$")
                .WithMessage("Period must be in YYYY-MM format.");
        }
    }

    public sealed class Handler(ICostRecordRepository repo) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            var records = await repo.ListByPeriodAsync(request.Period, cancellationToken);

            var filtered = records
                .Where(r => request.ServiceName is null || r.ServiceName.Equals(request.ServiceName, StringComparison.OrdinalIgnoreCase))
                .Where(r => request.TeamName is null || (r.Team != null && r.Team.Equals(request.TeamName, StringComparison.OrdinalIgnoreCase)))
                .ToList();

            // FOCUS 1.0 columns mapping
            var rows = filtered.Select(r => new FocusRowDto(
                BillingPeriodStart: DateTimeOffset.TryParse($"{request.Period}-01", out var start) ? start : DateTimeOffset.UtcNow,
                BillingPeriodEnd: DateTimeOffset.TryParse($"{request.Period}-01", out var s2) ? s2.AddMonths(1).AddDays(-1) : DateTimeOffset.UtcNow,
                ServiceName: r.ServiceName,
                ServiceCategory: r.Domain ?? "UnknownDomain",
                RegionName: r.Environment ?? "unknown",
                SubAccountId: r.Team ?? "unassigned",
                Tags: r.Team is not null ? new Dictionary<string, string> { ["team"] = r.Team } : new Dictionary<string, string>(),
                BilledCost: r.TotalCost,
                EffectiveCost: r.TotalCost,
                Currency: r.Currency,
                InvoiceIssuerName: "NexTraceOne-FinOps",
                Source: r.Source)).ToList();

            return Result<Response>.Success(new Response(
                SchemaVersion: "FOCUS_1.0",
                Period: request.Period,
                TotalRows: rows.Count,
                TotalCost: Math.Round(filtered.Sum(r => r.TotalCost), 2),
                Currency: filtered.FirstOrDefault()?.Currency ?? "USD",
                Rows: rows));
        }
    }

    public sealed record Response(
        string SchemaVersion,
        string Period,
        int TotalRows,
        decimal TotalCost,
        string Currency,
        IReadOnlyList<FocusRowDto> Rows);

    public sealed record FocusRowDto(
        DateTimeOffset BillingPeriodStart,
        DateTimeOffset BillingPeriodEnd,
        string ServiceName,
        string ServiceCategory,
        string RegionName,
        string SubAccountId,
        Dictionary<string, string> Tags,
        decimal BilledCost,
        decimal EffectiveCost,
        string Currency,
        string InvoiceIssuerName,
        string Source);
}
