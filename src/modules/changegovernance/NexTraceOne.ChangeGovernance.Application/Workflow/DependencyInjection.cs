using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NexTraceOne.BuildingBlocks.Application;
using NexTraceOne.Workflow.Application.Features.AddObservation;
using NexTraceOne.Workflow.Application.Features.ApproveStage;
using NexTraceOne.Workflow.Application.Features.CreateWorkflowTemplate;
using NexTraceOne.Workflow.Application.Features.EscalateSlaViolation;
using NexTraceOne.Workflow.Application.Features.ExportEvidencePackPdf;
using NexTraceOne.Workflow.Application.Features.GenerateEvidencePack;
using NexTraceOne.Workflow.Application.Features.GetEvidencePack;
using NexTraceOne.Workflow.Application.Features.GetWorkflowStatus;
using NexTraceOne.Workflow.Application.Features.InitiateWorkflow;
using NexTraceOne.Workflow.Application.Features.ListPendingApprovals;
using NexTraceOne.Workflow.Application.Features.RejectWorkflow;
using NexTraceOne.Workflow.Application.Features.RequestChanges;

namespace NexTraceOne.Workflow.Application;

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
