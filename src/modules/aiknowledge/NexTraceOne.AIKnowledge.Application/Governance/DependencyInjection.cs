using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NexTraceOne.AiGovernance.Application.Features.CreatePolicy;
using NexTraceOne.AiGovernance.Application.Features.RegisterModel;
using NexTraceOne.AiGovernance.Application.Features.SendAssistantMessage;
using NexTraceOne.AiGovernance.Application.Features.UpdateBudget;
using NexTraceOne.AiGovernance.Application.Features.UpdateModel;
using NexTraceOne.AiGovernance.Application.Features.UpdatePolicy;
using NexTraceOne.BuildingBlocks.Application;

namespace NexTraceOne.AiGovernance.Application;

/// <summary>
/// Registra serviços da camada Application do módulo AiGovernance.
/// Inclui: MediatR handlers, FluentValidation validators para todas as features
/// de governança de IA — Model Registry, Access Policies, Budgets, Audit e Assistant.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddAiGovernanceApplication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddBuildingBlocksApplication(configuration);
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));

        // ── Model Registry ───────────────────────────────────────────────
        services.AddTransient<IValidator<RegisterModel.Command>, RegisterModel.Validator>();
        services.AddTransient<IValidator<UpdateModel.Command>, UpdateModel.Validator>();

        // ── Access Policies ──────────────────────────────────────────────
        services.AddTransient<IValidator<CreatePolicy.Command>, CreatePolicy.Validator>();
        services.AddTransient<IValidator<UpdatePolicy.Command>, UpdatePolicy.Validator>();

        // ── Budgets ──────────────────────────────────────────────────────
        services.AddTransient<IValidator<UpdateBudget.Command>, UpdateBudget.Validator>();

        // ── AI Assistant ─────────────────────────────────────────────────
        services.AddTransient<IValidator<SendAssistantMessage.Command>, SendAssistantMessage.Validator>();

        return services;
    }
}
