using FluentValidation;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using NexTraceOne.BuildingBlocks.Application;
using NexTraceOne.OperationalIntelligence.Application.Cost.Features.AlertCostAnomaly;
using NexTraceOne.OperationalIntelligence.Application.Cost.Features.AttributeCostToService;
using NexTraceOne.OperationalIntelligence.Application.Cost.Features.ComputeCostTrend;
using NexTraceOne.OperationalIntelligence.Application.Cost.Features.CreateServiceCostProfile;
using NexTraceOne.OperationalIntelligence.Application.Cost.Features.EnrichCostRecordWithRelease;
using NexTraceOne.OperationalIntelligence.Application.Cost.Features.GetCostByRelease;
using NexTraceOne.OperationalIntelligence.Application.Cost.Features.GetCostByRoute;
using NexTraceOne.OperationalIntelligence.Application.Cost.Features.GetCostDelta;
using NexTraceOne.OperationalIntelligence.Application.Cost.Features.GetCostRecordsByDomain;
using NexTraceOne.OperationalIntelligence.Application.Cost.Features.GetCostRecordsByRelease;
using NexTraceOne.OperationalIntelligence.Application.Cost.Features.GetCostRecordsByService;
using NexTraceOne.OperationalIntelligence.Application.Cost.Features.GetCostRecordsByTeam;
using NexTraceOne.OperationalIntelligence.Application.Cost.Features.GetCostReport;
using NexTraceOne.OperationalIntelligence.Application.Cost.Features.ImportCostBatch;
using NexTraceOne.OperationalIntelligence.Application.Cost.Features.IngestCostSnapshot;
using NexTraceOne.OperationalIntelligence.Application.Cost.Features.ListCostImportBatches;

namespace NexTraceOne.OperationalIntelligence.Application.Cost;

/// <summary>
/// Registra serviços da camada Application do módulo CostIntelligence.
/// Inclui: MediatR handlers, FluentValidation validators.
/// P6.3: adicionados validators para CreateServiceCostProfile, ListCostImportBatches,
/// GetCostRecordsByService. ComputeCostTrend corrigido para persistir CostTrend.
/// P6.4: adicionados validators para GetCostRecordsByTeam, GetCostRecordsByDomain,
/// GetCostRecordsByRelease, EnrichCostRecordWithRelease.
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

        // P6.3 — pipeline real de ingestão e consulta de custo
        services.AddTransient<IValidator<CreateServiceCostProfile.Command>, CreateServiceCostProfile.Validator>();
        services.AddTransient<IValidator<ListCostImportBatches.Query>, ListCostImportBatches.Validator>();
        services.AddTransient<IValidator<GetCostRecordsByService.Query>, GetCostRecordsByService.Validator>();

        // P6.4 — correlação contextual: team, domain, release
        services.AddTransient<IValidator<GetCostRecordsByTeam.Query>, GetCostRecordsByTeam.Validator>();
        services.AddTransient<IValidator<GetCostRecordsByDomain.Query>, GetCostRecordsByDomain.Validator>();
        services.AddTransient<IValidator<GetCostRecordsByRelease.Query>, GetCostRecordsByRelease.Validator>();
        services.AddTransient<IValidator<EnrichCostRecordWithRelease.Command>, EnrichCostRecordWithRelease.Validator>();

        return services;
    }
}
