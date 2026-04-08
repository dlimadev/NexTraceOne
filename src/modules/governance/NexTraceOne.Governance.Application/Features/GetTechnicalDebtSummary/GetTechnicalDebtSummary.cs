using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Governance.Application.Abstractions;

namespace NexTraceOne.Governance.Application.Features.GetTechnicalDebtSummary;

/// <summary>
/// Feature: GetTechnicalDebtSummary — retorna resumo agregado de dívida técnica por serviço ou equipa.
/// Consulta a base de dados real via ITechnicalDebtRepository.
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

    /// <summary>Handler que consulta itens de dívida técnica da base de dados e agrega resultados.</summary>
    public sealed class Handler(ITechnicalDebtRepository repository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            var items = await repository.ListAsync(
                request.ServiceName,
                debtType: null,
                topN: request.TopN,
                cancellationToken);

            var debtItems = items
                .Select(i => new DebtItemDto(
                    DebtId: i.Id.Value,
                    ServiceName: i.ServiceName,
                    DebtType: i.DebtType,
                    Title: i.Title,
                    Severity: i.Severity,
                    EstimatedEffortDays: i.EstimatedEffortDays,
                    DebtScore: i.DebtScore))
                .ToList();

            var totalScore = debtItems.Sum(i => i.DebtScore);

            var byType = debtItems
                .GroupBy(i => i.DebtType)
                .Select(g => new DebtByTypeDto(
                    DebtType: g.Key,
                    Count: g.Count(),
                    TotalScore: g.Sum(i => i.DebtScore)))
                .OrderByDescending(x => x.TotalScore)
                .ToList();

            var highestRiskService = debtItems
                .GroupBy(i => i.ServiceName)
                .OrderByDescending(g => g.Sum(i => i.DebtScore))
                .FirstOrDefault()?.Key ?? request.ServiceName ?? "N/A";

            var recommendedAction = totalScore switch
            {
                >= 80 => "Prioritize security and architecture debt immediately to reduce production risk.",
                >= 40 => "Address high-severity items before next release cycle.",
                > 0 => "Continue tracking and schedule effort in upcoming sprints.",
                _ => "No technical debt items found. Keep monitoring."
            };

            return Result<Response>.Success(new Response(
                TotalDebtScore: totalScore,
                DebtItems: debtItems,
                ByType: byType,
                HighestRiskService: highestRiskService,
                RecommendedAction: recommendedAction));
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
