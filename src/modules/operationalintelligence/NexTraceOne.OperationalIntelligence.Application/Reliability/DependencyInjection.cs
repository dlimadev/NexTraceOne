using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NexTraceOne.OperationalIntelligence.Application.Reliability.Features.GetDomainReliabilitySummary;
using NexTraceOne.OperationalIntelligence.Application.Reliability.Features.GetServiceReliabilityCoverage;
using NexTraceOne.OperationalIntelligence.Application.Reliability.Features.GetServiceReliabilityDetail;
using NexTraceOne.OperationalIntelligence.Application.Reliability.Features.GetServiceReliabilityTrend;
using NexTraceOne.OperationalIntelligence.Application.Reliability.Features.GetTeamReliabilitySummary;
using NexTraceOne.OperationalIntelligence.Application.Reliability.Features.GetTeamReliabilityTrend;
using NexTraceOne.OperationalIntelligence.Application.Reliability.Features.ListServiceReliability;

namespace NexTraceOne.OperationalIntelligence.Application.Reliability;

/// <summary>
/// Registra serviços da camada Application do subdomínio Reliability.
/// Inclui: validators para todas as features de confiabilidade.
/// Os handlers são registados automaticamente via MediatR assembly scanning.
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

        return services;
    }
}
