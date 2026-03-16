using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NexTraceOne.OperationalIntelligence.Application.Incidents.Abstractions;

namespace NexTraceOne.OperationalIntelligence.Infrastructure.Incidents;

/// <summary>
/// Registra serviços de infraestrutura do subdomínio Incidents.
/// Nesta fase usa InMemoryIncidentStore como singleton; será substituído
/// por persistência EF Core em fase futura.
/// </summary>
public static class DependencyInjection
{
    /// <summary>Adiciona os serviços de infraestrutura de Incidents ao container DI.</summary>
    public static IServiceCollection AddIncidentsInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddSingleton<IIncidentStore, InMemoryIncidentStore>();
        return services;
    }
}
