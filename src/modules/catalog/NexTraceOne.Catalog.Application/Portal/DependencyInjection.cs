using FluentValidation;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using NexTraceOne.BuildingBlocks.Application;
using NexTraceOne.Catalog.Application.Portal.Features.CreateSubscription;
using NexTraceOne.Catalog.Application.Portal.Features.DeleteSubscription;
using NexTraceOne.Catalog.Application.Portal.Features.GenerateCode;
using NexTraceOne.Catalog.Application.Portal.Features.GetApiConsumers;
using NexTraceOne.Catalog.Application.Portal.Features.GetApiDetail;
using NexTraceOne.Catalog.Application.Portal.Features.GetApiHealth;
using NexTraceOne.Catalog.Application.Portal.Features.GetApisIConsume;
using NexTraceOne.Catalog.Application.Portal.Features.GetAssetTimeline;
using NexTraceOne.Catalog.Application.Portal.Features.GetContractPublicationStatus;
using NexTraceOne.Catalog.Application.Portal.Features.GetMyApis;
using NexTraceOne.Catalog.Application.Portal.Features.GetPlaygroundHistory;
using NexTraceOne.Catalog.Application.Portal.Features.GetPortalAnalytics;
using NexTraceOne.Catalog.Application.Portal.Features.GetPublicationCenterEntries;
using NexTraceOne.Catalog.Application.Portal.Features.GetSubscriptions;
using NexTraceOne.Catalog.Application.Portal.Features.PublishContractToPortal;
using NexTraceOne.Catalog.Application.Portal.Features.RecordAnalyticsEvent;
using NexTraceOne.Catalog.Application.Portal.Features.RenderOpenApiContract;
using NexTraceOne.Catalog.Application.Portal.Features.SearchCatalog;
using NexTraceOne.Catalog.Application.Portal.Features.WithdrawContractFromPortal;

namespace NexTraceOne.Catalog.Application.Portal;

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

        // Publication Center — workflow de publicação de contratos no Developer Portal
        services.AddTransient<IValidator<PublishContractToPortal.Command>, PublishContractToPortal.Validator>();
        services.AddTransient<IValidator<WithdrawContractFromPortal.Command>, WithdrawContractFromPortal.Validator>();
        services.AddTransient<IValidator<GetPublicationCenterEntries.Query>, GetPublicationCenterEntries.Validator>();
        services.AddTransient<IValidator<GetContractPublicationStatus.Query>, GetContractPublicationStatus.Validator>();

        return services;
    }
}
