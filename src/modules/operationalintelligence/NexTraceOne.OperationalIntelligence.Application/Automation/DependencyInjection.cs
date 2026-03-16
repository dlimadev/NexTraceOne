using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NexTraceOne.OperationalIntelligence.Application.Automation.Features.CreateAutomationWorkflow;
using NexTraceOne.OperationalIntelligence.Application.Automation.Features.EvaluatePreconditions;
using NexTraceOne.OperationalIntelligence.Application.Automation.Features.GetAutomationAction;
using NexTraceOne.OperationalIntelligence.Application.Automation.Features.GetAutomationAuditTrail;
using NexTraceOne.OperationalIntelligence.Application.Automation.Features.GetAutomationValidation;
using NexTraceOne.OperationalIntelligence.Application.Automation.Features.GetAutomationWorkflow;
using NexTraceOne.OperationalIntelligence.Application.Automation.Features.ListAutomationActions;
using NexTraceOne.OperationalIntelligence.Application.Automation.Features.ListAutomationWorkflows;
using NexTraceOne.OperationalIntelligence.Application.Automation.Features.RecordAutomationValidation;
using NexTraceOne.OperationalIntelligence.Application.Automation.Features.UpdateAutomationWorkflowAction;

namespace NexTraceOne.OperationalIntelligence.Application.Automation;

/// <summary>
/// Registra serviços da camada Application do subdomínio Automation.
/// Inclui: validators para todas as features de automação operacional,
/// workflows, pré-condições, validação pós-execução e trilha de auditoria.
/// Os handlers são registados automaticamente via MediatR assembly scanning.
/// </summary>
public static class DependencyInjection
{
    /// <summary>Adiciona os validadores do subdomínio Automation ao container DI.</summary>
    public static IServiceCollection AddAutomationApplication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Automation action catalog features
        services.AddTransient<IValidator<ListAutomationActions.Query>, ListAutomationActions.Validator>();
        services.AddTransient<IValidator<GetAutomationAction.Query>, GetAutomationAction.Validator>();

        // Automation workflow features
        services.AddTransient<IValidator<CreateAutomationWorkflow.Command>, CreateAutomationWorkflow.Validator>();
        services.AddTransient<IValidator<GetAutomationWorkflow.Query>, GetAutomationWorkflow.Validator>();
        services.AddTransient<IValidator<UpdateAutomationWorkflowAction.Command>, UpdateAutomationWorkflowAction.Validator>();
        services.AddTransient<IValidator<ListAutomationWorkflows.Query>, ListAutomationWorkflows.Validator>();

        // Precondition and validation features
        services.AddTransient<IValidator<EvaluatePreconditions.Command>, EvaluatePreconditions.Validator>();
        services.AddTransient<IValidator<RecordAutomationValidation.Command>, RecordAutomationValidation.Validator>();
        services.AddTransient<IValidator<GetAutomationValidation.Query>, GetAutomationValidation.Validator>();

        // Audit trail features
        services.AddTransient<IValidator<GetAutomationAuditTrail.Query>, GetAutomationAuditTrail.Validator>();

        return services;
    }
}
