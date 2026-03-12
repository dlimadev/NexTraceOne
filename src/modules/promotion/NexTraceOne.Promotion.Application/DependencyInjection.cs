using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NexTraceOne.BuildingBlocks.Application;
using NexTraceOne.Promotion.Application.Features.ApprovePromotion;
using NexTraceOne.Promotion.Application.Features.BlockPromotion;
using NexTraceOne.Promotion.Application.Features.ConfigureEnvironment;
using NexTraceOne.Promotion.Application.Features.CreatePromotionRequest;
using NexTraceOne.Promotion.Application.Features.EvaluatePromotionGates;
using NexTraceOne.Promotion.Application.Features.GetGateEvaluation;
using NexTraceOne.Promotion.Application.Features.GetPromotionStatus;
using NexTraceOne.Promotion.Application.Features.ListPromotionRequests;
using NexTraceOne.Promotion.Application.Features.OverrideGateWithJustification;

namespace NexTraceOne.Promotion.Application;

/// <summary>
/// Registra serviços da camada Application do módulo Promotion.
/// Inclui: MediatR handlers, FluentValidation validators.
/// </summary>
public static class DependencyInjection
{
    /// <summary>Adiciona os serviços da camada Application do módulo Promotion ao contêiner de DI.</summary>
    public static IServiceCollection AddPromotionApplication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddBuildingBlocksApplication(configuration);
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));

        services.AddTransient<IValidator<CreatePromotionRequest.Command>, CreatePromotionRequest.Validator>();
        services.AddTransient<IValidator<EvaluatePromotionGates.Command>, EvaluatePromotionGates.Validator>();
        services.AddTransient<IValidator<ApprovePromotion.Command>, ApprovePromotion.Validator>();
        services.AddTransient<IValidator<BlockPromotion.Command>, BlockPromotion.Validator>();
        services.AddTransient<IValidator<ConfigureEnvironment.Command>, ConfigureEnvironment.Validator>();
        services.AddTransient<IValidator<GetGateEvaluation.Query>, GetGateEvaluation.Validator>();
        services.AddTransient<IValidator<GetPromotionStatus.Query>, GetPromotionStatus.Validator>();
        services.AddTransient<IValidator<ListPromotionRequests.Query>, ListPromotionRequests.Validator>();
        services.AddTransient<IValidator<OverrideGateWithJustification.Command>, OverrideGateWithJustification.Validator>();

        return services;
    }
}
