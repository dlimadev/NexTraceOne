using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.OperationalIntelligence.Application.Automation.Abstractions;
using NexTraceOne.OperationalIntelligence.Domain.Automation.Enums;
using NexTraceOne.OperationalIntelligence.Domain.Incidents.Enums;

namespace NexTraceOne.OperationalIntelligence.Application.Automation.Features.ListAutomationWorkflows;

/// <summary>
/// Feature: ListAutomationWorkflows — lista workflows de automação com filtros opcionais.
/// Retorna resumo dos workflows com status, risco, ação e serviço associado.
/// </summary>
public static class ListAutomationWorkflows
{
    /// <summary>Query para listar workflows de automação com filtros e paginação.</summary>
    public sealed record Query(
        string? ServiceId,
        string? Status,
        int Page = 1,
        int PageSize = 20) : IQuery<Response>;

    /// <summary>Valida os parâmetros de consulta de listagem.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
            RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
            RuleFor(x => x.ServiceId).MaximumLength(200).When(x => x.ServiceId is not null);
            RuleFor(x => x.Status).MaximumLength(200).When(x => x.Status is not null);
        }
    }

    /// <summary>Handler que retorna a lista paginada de workflows de automação.</summary>
    public sealed class Handler(IAutomationWorkflowRepository workflowRepository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            AutomationWorkflowStatus? statusFilter = null;
            if (!string.IsNullOrEmpty(request.Status) &&
                Enum.TryParse<AutomationWorkflowStatus>(request.Status, ignoreCase: true, out var parsedStatus))
                statusFilter = parsedStatus;

            var catalog = AutomationActionCatalog.GetAll();
            var workflows = await workflowRepository.ListAsync(
                request.ServiceId, statusFilter, request.Page, request.PageSize, cancellationToken);
            var total = await workflowRepository.CountAsync(request.ServiceId, statusFilter, cancellationToken);

            var items = workflows.Select(w =>
            {
                var action = catalog.FirstOrDefault(a =>
                    a.ActionId.Equals(w.ActionId, StringComparison.OrdinalIgnoreCase));
                return new WorkflowSummary(
                    WorkflowId: w.Id.Value,
                    ActionId: w.ActionId,
                    ActionDisplayName: action?.DisplayName ?? w.ActionId,
                    Status: w.Status,
                    RiskLevel: w.RiskLevel,
                    RequestedBy: w.RequestedBy,
                    ServiceId: w.ServiceId,
                    CreatedAt: w.CreatedAt);
            }).ToList();

            return Result<Response>.Success(new Response(items, total));
        }
    }

    /// <summary>Resumo de um workflow de automação na listagem.</summary>
    public sealed record WorkflowSummary(
        Guid WorkflowId,
        string ActionId,
        string ActionDisplayName,
        AutomationWorkflowStatus Status,
        RiskLevel RiskLevel,
        string RequestedBy,
        string? ServiceId,
        DateTimeOffset CreatedAt);

    /// <summary>Resposta paginada da listagem de workflows de automação.</summary>
    public sealed record Response(
        IReadOnlyList<WorkflowSummary> Items,
        int TotalCount);
}


/// <summary>
/// Feature: ListAutomationWorkflows — lista workflows de automação com filtros opcionais.
/// Retorna resumo dos workflows com status, risco, ação e serviço associado.
/// Estrutura VSA: Query + Validator + Handler + Response em um único arquivo.
///
/// Nota: nesta fase os dados são simulados até integração completa entre módulos.
/// </summary>
public static class ListAutomationWorkflows
{
    /// <summary>Query para listar workflows de automação com filtros e paginação.</summary>
    public sealed record Query(
        string? ServiceId,
        string? Status,
        int Page = 1,
        int PageSize = 20) : IQuery<Response>;

    /// <summary>Valida os parâmetros de consulta de listagem.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
            RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
            RuleFor(x => x.ServiceId).MaximumLength(200).When(x => x.ServiceId is not null);
            RuleFor(x => x.Status).MaximumLength(200).When(x => x.Status is not null);
        }
    }

    /// <summary>Handler que compõe a listagem de workflows de automação.</summary>
    public sealed class Handler : IQueryHandler<Query, Response>
    {
        public Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            return Task.FromResult<Result<Response>>(
                Error.Business(
                    "Operations.Automation.Workflows.PreviewOnly",
                    "Operational automation workflows remain a preview-only capability and are not backed by production workflow data in this release."));
        }
    }

    /// <summary>Resumo de um workflow de automação na listagem.</summary>
    public sealed record WorkflowSummary(
        Guid WorkflowId,
        string ActionId,
        string ActionDisplayName,
        AutomationWorkflowStatus Status,
        RiskLevel RiskLevel,
        string RequestedBy,
        string? ServiceId,
        DateTimeOffset CreatedAt);

    /// <summary>Resposta paginada da listagem de workflows de automação.</summary>
    public sealed record Response(
        IReadOnlyList<WorkflowSummary> Items,
        int TotalCount);
}
