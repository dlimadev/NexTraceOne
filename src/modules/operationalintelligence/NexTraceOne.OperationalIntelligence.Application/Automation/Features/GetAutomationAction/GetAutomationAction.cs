using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.OperationalIntelligence.Domain.Automation.Enums;
using NexTraceOne.OperationalIntelligence.Domain.Automation.Errors;
using NexTraceOne.OperationalIntelligence.Domain.Incidents.Enums;

namespace NexTraceOne.OperationalIntelligence.Application.Automation.Features.GetAutomationAction;

/// <summary>
/// Feature: GetAutomationAction — retorna os detalhes de uma ação de automação pelo identificador.
/// Consulta o catálogo estático para localizar a ação e retorna os detalhes completos.
/// Estrutura VSA: Query + Validator + Handler + Response em um único arquivo.
///
/// Nota: nesta fase os dados são simulados até integração completa entre módulos.
/// </summary>
public static class GetAutomationAction
{
    /// <summary>Query para obter uma ação de automação pelo identificador.</summary>
    public sealed record Query(string ActionId) : IQuery<Response>;

    /// <summary>Valida que o identificador da ação foi informado.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.ActionId).NotEmpty().MaximumLength(200);
        }
    }

    /// <summary>Handler que localiza a ação no catálogo e retorna os seus detalhes.</summary>
    public sealed class Handler : IQueryHandler<Query, Response>
    {
        public Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            var catalog = AutomationActionCatalog.GetAll();
            var action = catalog.FirstOrDefault(a =>
                a.ActionId.Equals(request.ActionId, StringComparison.OrdinalIgnoreCase));

            if (action is null)
                return Task.FromResult<Result<Response>>(AutomationErrors.ActionNotFound(request.ActionId));

            var response = new Response(
                action.ActionId,
                action.Name,
                action.DisplayName,
                action.Description,
                action.ActionType,
                action.RiskLevel,
                action.RequiresApproval,
                action.AllowedPersonas,
                action.AllowedEnvironments,
                action.PreconditionTypes,
                action.HasPostValidation);

            return Task.FromResult(Result<Response>.Success(response));
        }
    }

    /// <summary>Resposta com os detalhes de uma ação de automação.</summary>
    public sealed record Response(
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
}
