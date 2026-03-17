using FluentValidation;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using NexTraceOne.BuildingBlocks.Application;
using NexTraceOne.ChangeGovernance.Application.Workflow.Features.AddObservation;
using NexTraceOne.ChangeGovernance.Application.Workflow.Features.ApproveStage;
using NexTraceOne.ChangeGovernance.Application.Workflow.Features.CreateWorkflowTemplate;
using NexTraceOne.ChangeGovernance.Application.Workflow.Features.EscalateSlaViolation;
using NexTraceOne.ChangeGovernance.Application.Workflow.Features.ExportEvidencePackPdf;
using NexTraceOne.ChangeGovernance.Application.Workflow.Features.GenerateEvidencePack;
using NexTraceOne.ChangeGovernance.Application.Workflow.Features.GetEvidencePack;
using NexTraceOne.ChangeGovernance.Application.Workflow.Features.GetWorkflowStatus;
using NexTraceOne.ChangeGovernance.Application.Workflow.Features.InitiateWorkflow;
using NexTraceOne.ChangeGovernance.Application.Workflow.Features.ListPendingApprovals;
using NexTraceOne.ChangeGovernance.Application.Workflow.Features.RejectWorkflow;
using NexTraceOne.ChangeGovernance.Application.Workflow.Features.RequestChanges;

namespace NexTraceOne.ChangeGovernance.Application.Workflow;

/// <summary>
/// Registra serviços da camada Application do módulo Workflow.
/// Inclui: MediatR handlers, FluentValidation validators.
/// </summary>
public static class DependencyInjection
{
    /// <summary>Adiciona os serviços da camada Application do módulo Workflow ao contêiner de DI.</summary>
    public static IServiceCollection AddWorkflowApplication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddBuildingBlocksApplication(configuration);
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));

        services.AddTransient<IValidator<CreateWorkflowTemplate.Command>, CreateWorkflowTemplate.Validator>();
        services.AddTransient<IValidator<InitiateWorkflow.Command>, InitiateWorkflow.Validator>();
        services.AddTransient<IValidator<ApproveStage.Command>, ApproveStage.Validator>();
        services.AddTransient<IValidator<RejectWorkflow.Command>, RejectWorkflow.Validator>();
        services.AddTransient<IValidator<RequestChanges.Command>, RequestChanges.Validator>();
        services.AddTransient<IValidator<AddObservation.Command>, AddObservation.Validator>();
        services.AddTransient<IValidator<GenerateEvidencePack.Command>, GenerateEvidencePack.Validator>();
        services.AddTransient<IValidator<EscalateSlaViolation.Command>, EscalateSlaViolation.Validator>();
        services.AddTransient<IValidator<GetWorkflowStatus.Query>, GetWorkflowStatus.Validator>();
        services.AddTransient<IValidator<ListPendingApprovals.Query>, ListPendingApprovals.Validator>();
        services.AddTransient<IValidator<GetEvidencePack.Query>, GetEvidencePack.Validator>();
        services.AddTransient<IValidator<ExportEvidencePackPdf.Query>, ExportEvidencePackPdf.Validator>();

        return services;
    }
}
