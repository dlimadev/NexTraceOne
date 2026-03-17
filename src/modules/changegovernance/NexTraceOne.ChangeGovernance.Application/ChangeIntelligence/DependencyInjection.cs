using FluentValidation;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using NexTraceOne.BuildingBlocks.Application;
using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.AttachWorkItemContext;
using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.CalculateBlastRadius;
using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.ClassifyChangeLevel;
using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.ComputeChangeScore;
using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.GetBlastRadiusReport;
using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.GetChangeAdvisory;
using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.GetChangeDecisionHistory;
using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.GetChangeScore;
using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.GetRelease;
using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.GetReleaseHistory;
using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.ListReleases;
using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.NotifyDeployment;
using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.RecordChangeDecision;
using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.RegisterRollback;
using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.SyncJiraWorkItems;
using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.UpdateDeploymentState;

namespace NexTraceOne.ChangeGovernance.Application.ChangeIntelligence;

/// <summary>
/// Registra serviços da camada Application do módulo ChangeIntelligence.
/// Inclui: MediatR handlers, FluentValidation validators.
/// </summary>
public static class DependencyInjection
{
    /// <summary>Adiciona os serviços da camada Application do módulo ChangeIntelligence ao contêiner de DI.</summary>
    public static IServiceCollection AddChangeIntelligenceApplication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddBuildingBlocksApplication(configuration);
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));

        services.AddTransient<IValidator<NotifyDeployment.Command>, NotifyDeployment.Validator>();
        services.AddTransient<IValidator<ClassifyChangeLevel.Command>, ClassifyChangeLevel.Validator>();
        services.AddTransient<IValidator<CalculateBlastRadius.Command>, CalculateBlastRadius.Validator>();
        services.AddTransient<IValidator<ComputeChangeScore.Command>, ComputeChangeScore.Validator>();
        services.AddTransient<IValidator<GetRelease.Query>, GetRelease.Validator>();
        services.AddTransient<IValidator<GetReleaseHistory.Query>, GetReleaseHistory.Validator>();
        services.AddTransient<IValidator<ListReleases.Query>, ListReleases.Validator>();
        services.AddTransient<IValidator<UpdateDeploymentState.Command>, UpdateDeploymentState.Validator>();
        services.AddTransient<IValidator<RegisterRollback.Command>, RegisterRollback.Validator>();
        services.AddTransient<IValidator<GetBlastRadiusReport.Query>, GetBlastRadiusReport.Validator>();
        services.AddTransient<IValidator<GetChangeScore.Query>, GetChangeScore.Validator>();
        services.AddTransient<IValidator<AttachWorkItemContext.Command>, AttachWorkItemContext.Validator>();
        services.AddTransient<IValidator<SyncJiraWorkItems.Command>, SyncJiraWorkItems.Validator>();
        services.AddTransient<IValidator<GetChangeAdvisory.Query>, GetChangeAdvisory.Validator>();
        services.AddTransient<IValidator<RecordChangeDecision.Command>, RecordChangeDecision.Validator>();
        services.AddTransient<IValidator<GetChangeDecisionHistory.Query>, GetChangeDecisionHistory.Validator>();

        return services;
    }
}

