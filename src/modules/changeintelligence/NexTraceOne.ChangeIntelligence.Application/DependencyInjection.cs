using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NexTraceOne.BuildingBlocks.Application;
using NexTraceOne.ChangeIntelligence.Application.Features.AttachWorkItemContext;
using NexTraceOne.ChangeIntelligence.Application.Features.CalculateBlastRadius;
using NexTraceOne.ChangeIntelligence.Application.Features.ClassifyChangeLevel;
using NexTraceOne.ChangeIntelligence.Application.Features.ComputeChangeScore;
using NexTraceOne.ChangeIntelligence.Application.Features.GetBlastRadiusReport;
using NexTraceOne.ChangeIntelligence.Application.Features.GetChangeScore;
using NexTraceOne.ChangeIntelligence.Application.Features.GetRelease;
using NexTraceOne.ChangeIntelligence.Application.Features.GetReleaseHistory;
using NexTraceOne.ChangeIntelligence.Application.Features.ListReleases;
using NexTraceOne.ChangeIntelligence.Application.Features.NotifyDeployment;
using NexTraceOne.ChangeIntelligence.Application.Features.RegisterRollback;
using NexTraceOne.ChangeIntelligence.Application.Features.SyncJiraWorkItems;
using NexTraceOne.ChangeIntelligence.Application.Features.UpdateDeploymentState;

namespace NexTraceOne.ChangeIntelligence.Application;

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

        return services;
    }
}

