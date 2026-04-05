using FluentValidation;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using NexTraceOne.BuildingBlocks.Application;
using NexTraceOne.Catalog.Application.Templates.Features.CreateServiceTemplate;
using NexTraceOne.Catalog.Application.Templates.Features.GetServiceTemplate;
using NexTraceOne.Catalog.Application.Templates.Features.ListServiceTemplates;
using NexTraceOne.Catalog.Application.Templates.Features.ScaffoldServiceFromTemplate;

namespace NexTraceOne.Catalog.Application.Templates;

/// <summary>
/// Registra serviços da camada de aplicação do módulo Templates.
/// Inclui MediatR handlers e validadores FluentValidation para as features de templates.
/// </summary>
public static class DependencyInjection
{
    /// <summary>Adiciona os serviços de aplicação do módulo Templates ao container DI.</summary>
    public static IServiceCollection AddCatalogTemplatesApplication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddBuildingBlocksApplication(configuration);
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));

        // ── Validators dos Templates ──────────────────────────────────────
        services.AddTransient<IValidator<CreateServiceTemplate.Command>, CreateServiceTemplate.Validator>();
        services.AddTransient<IValidator<GetServiceTemplate.Query>, GetServiceTemplate.Validator>();
        services.AddTransient<IValidator<ListServiceTemplates.Query>, ListServiceTemplates.Validator>();
        services.AddTransient<IValidator<ScaffoldServiceFromTemplate.Command>, ScaffoldServiceFromTemplate.Validator>();

        return services;
    }
}
