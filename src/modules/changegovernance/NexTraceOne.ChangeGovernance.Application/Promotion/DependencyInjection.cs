using FluentValidation;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using NexTraceOne.BuildingBlocks.Application;
using NexTraceOne.ChangeGovernance.Application.Promotion.Features.ApprovePromotion;
using NexTraceOne.ChangeGovernance.Application.Promotion.Features.BlockPromotion;
using NexTraceOne.ChangeGovernance.Application.Promotion.Features.ConfigureEnvironment;
using NexTraceOne.ChangeGovernance.Application.Promotion.Features.CreatePromotionRequest;
using NexTraceOne.ChangeGovernance.Application.Promotion.Features.EvaluatePromotionGates;
using NexTraceOne.ChangeGovernance.Application.Promotion.Features.GetGateEvaluation;
using NexTraceOne.ChangeGovernance.Application.Promotion.Features.GetPromotionStatus;
using NexTraceOne.ChangeGovernance.Application.Promotion.Features.ListPromotionRequests;
using NexTraceOne.ChangeGovernance.Application.Promotion.Features.OverrideGateWithJustification;

namespace NexTraceOne.ChangeGovernance.Application.Promotion;

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
