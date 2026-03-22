using FluentValidation;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using NexTraceOne.BuildingBlocks.Application;
using NexTraceOne.OperationalIntelligence.Application.Cost.Features.AlertCostAnomaly;
using NexTraceOne.OperationalIntelligence.Application.Cost.Features.AttributeCostToService;
using NexTraceOne.OperationalIntelligence.Application.Cost.Features.ComputeCostTrend;
using NexTraceOne.OperationalIntelligence.Application.Cost.Features.GetCostByRelease;
using NexTraceOne.OperationalIntelligence.Application.Cost.Features.GetCostByRoute;
using NexTraceOne.OperationalIntelligence.Application.Cost.Features.GetCostDelta;
using NexTraceOne.OperationalIntelligence.Application.Cost.Features.GetCostReport;
using NexTraceOne.OperationalIntelligence.Application.Cost.Features.ImportCostBatch;
using NexTraceOne.OperationalIntelligence.Application.Cost.Features.IngestCostSnapshot;

namespace NexTraceOne.OperationalIntelligence.Application.Cost;

/// <summary>
/// Registra serviços da camada Application do módulo CostIntelligence.
/// Inclui: MediatR handlers, FluentValidation validators.
/// </summary>
public static class DependencyInjection
{
    /// <summary>Adiciona os serviços da camada Application do módulo CostIntelligence ao contêiner de DI.</summary>
    public static IServiceCollection AddCostIntelligenceApplication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddBuildingBlocksApplication(configuration);
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));

        services.AddTransient<IValidator<IngestCostSnapshot.Command>, IngestCostSnapshot.Validator>();
        services.AddTransient<IValidator<GetCostReport.Query>, GetCostReport.Validator>();
        services.AddTransient<IValidator<GetCostByRelease.Query>, GetCostByRelease.Validator>();
        services.AddTransient<IValidator<GetCostByRoute.Query>, GetCostByRoute.Validator>();
        services.AddTransient<IValidator<GetCostDelta.Query>, GetCostDelta.Validator>();
        services.AddTransient<IValidator<AttributeCostToService.Command>, AttributeCostToService.Validator>();
        services.AddTransient<IValidator<ComputeCostTrend.Command>, ComputeCostTrend.Validator>();
        services.AddTransient<IValidator<AlertCostAnomaly.Command>, AlertCostAnomaly.Validator>();
        services.AddTransient<IValidator<ImportCostBatch.Command>, ImportCostBatch.Validator>();

        return services;
    }
}
