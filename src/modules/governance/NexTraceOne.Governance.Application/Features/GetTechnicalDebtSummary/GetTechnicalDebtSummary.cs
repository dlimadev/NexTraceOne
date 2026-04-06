using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.Governance.Application.Features.GetTechnicalDebtSummary;

/// <summary>
/// Feature: GetTechnicalDebtSummary — retorna resumo agregado de dívida técnica por serviço ou equipa.
/// Inclui scoring total, breakdown por tipo e recomendação de ação prioritária.
///
/// Owner: módulo Governance.
/// Pilar: Service Governance — tracking de dívida técnica com scoring e correlação com incidentes.
/// </summary>
public static class GetTechnicalDebtSummary
{
    /// <summary>Query de resumo de dívida técnica com filtros opcionais por serviço ou equipa.</summary>
    public sealed record Query(
        string? ServiceName = null,
        string? TeamName = null,
        int TopN = 10) : IQuery<Response>;

    /// <summary>Validação da query de resumo de dívida técnica.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.TopN).InclusiveBetween(1, 50);
        }
    }

    /// <summary>Handler que retorna dados de demonstração representativos de dívida técnica acumulada.</summary>
    public sealed class Handler : IQueryHandler<Query, Response>
    {
        public Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            var serviceName = string.IsNullOrWhiteSpace(request.ServiceName)
                ? "order-service"
                : request.ServiceName;

            var items = new List<DebtItemDto>
            {
                new(
                    DebtId: new Guid("22222222-0000-0000-0000-000000000001"),
                    ServiceName: serviceName,
                    DebtType: "architecture",
                    Title: "Monolithic domain boundaries need decomposition",
                    Severity: "high",
                    EstimatedEffortDays: 15,
                    DebtScore: 25),
                new(
                    DebtId: new Guid("22222222-0000-0000-0000-000000000002"),
                    ServiceName: serviceName,
                    DebtType: "security",
                    Title: "Outdated JWT validation library",
                    Severity: "critical",
                    EstimatedEffortDays: 3,
                    DebtScore: 40),
                new(
                    DebtId: new Guid("22222222-0000-0000-0000-000000000003"),
                    ServiceName: "payment-service",
                    DebtType: "code-quality",
                    Title: "Missing unit tests on payment processor",
                    Severity: "medium",
                    EstimatedEffortDays: 5,
                    DebtScore: 10),
                new(
                    DebtId: new Guid("22222222-0000-0000-0000-000000000004"),
                    ServiceName: "notification-service",
                    DebtType: "dependency",
                    Title: "Legacy email client with no maintenance",
                    Severity: "high",
                    EstimatedEffortDays: 8,
                    DebtScore: 25),
                new(
                    DebtId: new Guid("22222222-0000-0000-0000-000000000005"),
                    ServiceName: serviceName,
                    DebtType: "testing",
                    Title: "Integration tests not covering async flows",
                    Severity: "medium",
                    EstimatedEffortDays: 6,
                    DebtScore: 10),
            };

            var topItems = items.Take(request.TopN).ToList();
            var totalScore = topItems.Sum(i => i.DebtScore);

            var byType = topItems
                .GroupBy(i => i.DebtType)
                .Select(g => new DebtByTypeDto(
                    DebtType: g.Key,
                    Count: g.Count(),
                    TotalScore: g.Sum(i => i.DebtScore)))
                .OrderByDescending(x => x.TotalScore)
                .ToList();

            var highestRiskService = topItems
                .GroupBy(i => i.ServiceName)
                .OrderByDescending(g => g.Sum(i => i.DebtScore))
                .FirstOrDefault()?.Key ?? serviceName;

            var recommendedAction = totalScore switch
            {
                >= 80 => "Prioritize security and architecture debt immediately to reduce production risk.",
                >= 40 => "Address high-severity items before next release cycle.",
                _ => "Continue tracking and schedule effort in upcoming sprints."
            };

            return Task.FromResult(Result<Response>.Success(new Response(
                TotalDebtScore: totalScore,
                DebtItems: topItems,
                ByType: byType,
                HighestRiskService: highestRiskService,
                RecommendedAction: recommendedAction)));
        }
    }

    /// <summary>Resposta com resumo agregado de dívida técnica.</summary>
    public sealed record Response(
        int TotalDebtScore,
        IReadOnlyList<DebtItemDto> DebtItems,
        IReadOnlyList<DebtByTypeDto> ByType,
        string HighestRiskService,
        string RecommendedAction);

    /// <summary>Item individual de dívida técnica no resumo.</summary>
    public sealed record DebtItemDto(
        Guid DebtId,
        string ServiceName,
        string DebtType,
        string Title,
        string Severity,
        int EstimatedEffortDays,
        int DebtScore);

    /// <summary>Agrupamento de dívida técnica por tipo.</summary>
    public sealed record DebtByTypeDto(
        string DebtType,
        int Count,
        int TotalScore);
}
