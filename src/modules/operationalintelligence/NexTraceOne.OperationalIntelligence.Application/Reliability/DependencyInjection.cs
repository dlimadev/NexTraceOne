using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NexTraceOne.OperationalIntelligence.Application.Reliability.Features.GetBurnRate;
using NexTraceOne.OperationalIntelligence.Application.Reliability.Features.GetDomainReliabilitySummary;
using NexTraceOne.OperationalIntelligence.Application.Reliability.Features.GetErrorBudget;
using NexTraceOne.OperationalIntelligence.Application.Reliability.Features.GetServiceReliabilityCoverage;
using NexTraceOne.OperationalIntelligence.Application.Reliability.Features.GetServiceReliabilityDetail;
using NexTraceOne.OperationalIntelligence.Application.Reliability.Features.GetServiceReliabilityTrend;
using NexTraceOne.OperationalIntelligence.Application.Reliability.Features.GetTeamReliabilitySummary;
using NexTraceOne.OperationalIntelligence.Application.Reliability.Features.GetTeamReliabilityTrend;
using NexTraceOne.OperationalIntelligence.Application.Reliability.Features.ListServiceReliability;
using NexTraceOne.OperationalIntelligence.Application.Reliability.Features.RegisterSlaDefinition;
using NexTraceOne.OperationalIntelligence.Application.Reliability.Features.RegisterSloDefinition;

namespace NexTraceOne.OperationalIntelligence.Application.Reliability;

/// <summary>
/// Registra serviços da camada Application do subdomínio Reliability.
/// Inclui: validators para todas as features de confiabilidade.
/// Os handlers são registados automaticamente via MediatR assembly scanning.
/// P6.1: adicionados validators para RegisterSloDefinition, RegisterSlaDefinition, GetErrorBudget e GetBurnRate.
/// </summary>
public static class DependencyInjection
{
    /// <summary>Adiciona os validadores do subdomínio Reliability ao container DI.</summary>
    public static IServiceCollection AddReliabilityApplication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddTransient<IValidator<ListServiceReliability.Query>, ListServiceReliability.Validator>();
        services.AddTransient<IValidator<GetServiceReliabilityDetail.Query>, GetServiceReliabilityDetail.Validator>();
        services.AddTransient<IValidator<GetTeamReliabilitySummary.Query>, GetTeamReliabilitySummary.Validator>();
        services.AddTransient<IValidator<GetDomainReliabilitySummary.Query>, GetDomainReliabilitySummary.Validator>();
        services.AddTransient<IValidator<GetServiceReliabilityTrend.Query>, GetServiceReliabilityTrend.Validator>();
        services.AddTransient<IValidator<GetTeamReliabilityTrend.Query>, GetTeamReliabilityTrend.Validator>();
        services.AddTransient<IValidator<GetServiceReliabilityCoverage.Query>, GetServiceReliabilityCoverage.Validator>();

        // P6.1 — SLO / SLA / ErrorBudget / BurnRate
        services.AddTransient<IValidator<RegisterSloDefinition.Command>, RegisterSloDefinition.Validator>();
        services.AddTransient<IValidator<RegisterSlaDefinition.Command>, RegisterSlaDefinition.Validator>();
        services.AddTransient<IValidator<GetErrorBudget.Query>, GetErrorBudget.Validator>();
        services.AddTransient<IValidator<GetBurnRate.Query>, GetBurnRate.Validator>();

        return services;
    }
}
