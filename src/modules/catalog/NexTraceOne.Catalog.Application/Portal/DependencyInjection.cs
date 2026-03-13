using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NexTraceOne.BuildingBlocks.Application;
using NexTraceOne.DeveloperPortal.Application.Features.CreateSubscription;
using NexTraceOne.DeveloperPortal.Application.Features.DeleteSubscription;
using NexTraceOne.DeveloperPortal.Application.Features.ExecutePlayground;
using NexTraceOne.DeveloperPortal.Application.Features.GenerateCode;
using NexTraceOne.DeveloperPortal.Application.Features.GetApiConsumers;
using NexTraceOne.DeveloperPortal.Application.Features.GetApiDetail;
using NexTraceOne.DeveloperPortal.Application.Features.GetApiHealth;
using NexTraceOne.DeveloperPortal.Application.Features.GetApisIConsume;
using NexTraceOne.DeveloperPortal.Application.Features.GetAssetTimeline;
using NexTraceOne.DeveloperPortal.Application.Features.GetMyApis;
using NexTraceOne.DeveloperPortal.Application.Features.GetPlaygroundHistory;
using NexTraceOne.DeveloperPortal.Application.Features.GetPortalAnalytics;
using NexTraceOne.DeveloperPortal.Application.Features.GetSubscriptions;
using NexTraceOne.DeveloperPortal.Application.Features.RecordAnalyticsEvent;
using NexTraceOne.DeveloperPortal.Application.Features.RenderOpenApiContract;
using NexTraceOne.DeveloperPortal.Application.Features.SearchCatalog;

namespace NexTraceOne.DeveloperPortal.Application;

/// <summary>
/// Registra serviços da camada Application do módulo DeveloperPortal.
/// Inclui: MediatR handlers via assembly scanning e FluentValidation validators.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Adiciona serviços de aplicação do DeveloperPortal ao contentor de DI.
    /// Regista handlers MediatR, validators FluentValidation e behaviors de pipeline.
    /// </summary>
    public static IServiceCollection AddDeveloperPortalApplication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddBuildingBlocksApplication(configuration);

        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));

        services.AddTransient<IValidator<CreateSubscription.Command>, CreateSubscription.Validator>();
        services.AddTransient<IValidator<DeleteSubscription.Command>, DeleteSubscription.Validator>();
        services.AddTransient<IValidator<ExecutePlayground.Command>, ExecutePlayground.Validator>();
        services.AddTransient<IValidator<GenerateCode.Command>, GenerateCode.Validator>();
        services.AddTransient<IValidator<GetApiConsumers.Query>, GetApiConsumers.Validator>();
        services.AddTransient<IValidator<GetApiDetail.Query>, GetApiDetail.Validator>();
        services.AddTransient<IValidator<GetApiHealth.Query>, GetApiHealth.Validator>();
        services.AddTransient<IValidator<GetApisIConsume.Query>, GetApisIConsume.Validator>();
        services.AddTransient<IValidator<GetAssetTimeline.Query>, GetAssetTimeline.Validator>();
        services.AddTransient<IValidator<GetMyApis.Query>, GetMyApis.Validator>();
        services.AddTransient<IValidator<GetPlaygroundHistory.Query>, GetPlaygroundHistory.Validator>();
        services.AddTransient<IValidator<GetPortalAnalytics.Query>, GetPortalAnalytics.Validator>();
        services.AddTransient<IValidator<GetSubscriptions.Query>, GetSubscriptions.Validator>();
        services.AddTransient<IValidator<RecordAnalyticsEvent.Command>, RecordAnalyticsEvent.Validator>();
        services.AddTransient<IValidator<RenderOpenApiContract.Query>, RenderOpenApiContract.Validator>();
        services.AddTransient<IValidator<SearchCatalog.Query>, SearchCatalog.Validator>();

        return services;
    }
}
