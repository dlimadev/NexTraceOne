using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace NexTraceOne.Contracts.Application;

/// <summary>
/// Registra serviços da camada Application do módulo Contracts.
/// Inclui: MediatR handlers, FluentValidation validators.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddContractsApplication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // TODO: Registrar MediatR handlers e validators deste módulo
        return services;
    }
}
