using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.OperationalIntelligence.Domain.Automation.Enums;
using NexTraceOne.OperationalIntelligence.Domain.Incidents.Enums;

namespace NexTraceOne.OperationalIntelligence.Application.Automation.Features.ListAutomationActions;

/// <summary>
/// Feature: ListAutomationActions — lista todas as ações de automação disponíveis no catálogo.
/// Retorna o catálogo completo de ações com tipos, risco, personas permitidas e pré-condições.
/// Estrutura VSA: Query + Validator + Handler + Response em um único arquivo.
///
/// Nota: nesta fase os dados são simulados até integração completa entre módulos.
/// </summary>
public static class ListAutomationActions
{
    /// <summary>Query para listar ações de automação com filtro opcional.</summary>
    public sealed record Query(string? Filter) : IQuery<Response>;

    /// <summary>Valida os parâmetros de consulta — filtro é opcional.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.Filter).MaximumLength(200).When(x => x.Filter is not null);
        }
    }

    /// <summary>Handler que retorna o catálogo de ações de automação disponíveis.</summary>
    public sealed class Handler : IQueryHandler<Query, Response>
    {
        public Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            var catalog = AutomationActionCatalog.GetAll();

            IReadOnlyList<ActionItem> items = catalog
                .Select(a => new ActionItem(
                    a.ActionId, a.Name, a.DisplayName, a.Description,
                    a.ActionType, a.RiskLevel, a.RequiresApproval,
                    a.AllowedPersonas, a.AllowedEnvironments,
                    a.PreconditionTypes, a.HasPostValidation))
                .ToList();

            if (!string.IsNullOrWhiteSpace(request.Filter))
            {
                items = items
                    .Where(a =>
                        a.Name.Contains(request.Filter, StringComparison.OrdinalIgnoreCase) ||
                        a.DisplayName.Contains(request.Filter, StringComparison.OrdinalIgnoreCase) ||
                        a.Description.Contains(request.Filter, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            return Task.FromResult(Result<Response>.Success(new Response(items)));
        }
    }

    /// <summary>Item do catálogo de ações de automação.</summary>
    public sealed record ActionItem(
        string ActionId,
        string Name,
        string DisplayName,
        string Description,
        AutomationActionType ActionType,
        RiskLevel RiskLevel,
        bool RequiresApproval,
        IReadOnlyList<string> AllowedPersonas,
        IReadOnlyList<string> AllowedEnvironments,
        IReadOnlyList<PreconditionType> PreconditionTypes,
        bool HasPostValidation);

    /// <summary>Resposta com a lista de ações de automação do catálogo.</summary>
    public sealed record Response(IReadOnlyList<ActionItem> Items);
}
