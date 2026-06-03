using FluentValidation;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using NexTraceOne.BuildingBlocks.Application;
using NexTraceOne.OperationalIntelligence.Application.FinOps.Features.GetCostPerReleaseReport;
using NexTraceOne.OperationalIntelligence.Application.FinOps.Features.GetEnvironmentCostComparisonReport;
using NexTraceOne.OperationalIntelligence.Application.FinOps.Features.GetFinOpsInsights;
using NexTraceOne.OperationalIntelligence.Application.FinOps.Features.GetFinOpsTrendReport;
using NexTraceOne.OperationalIntelligence.Application.FinOps.Features.GetFinOpsWasteAnalysisReport;
using NexTraceOne.OperationalIntelligence.Application.FinOps.Features.GetServiceCostAllocationReport;
using NexTraceOne.OperationalIntelligence.Application.FinOps.Features.IngestServiceCostRecord;

namespace NexTraceOne.OperationalIntelligence.Application.FinOps;

/// <summary>
/// Registra serviços da camada Application do subdomínio FinOps.
/// FinOps Contextual: alocação de custo por serviço, trend reports, waste analysis, insights.
/// </summary>
public static class DependencyInjection
{
    /// <summary>Adiciona os serviços de Application do subdomínio FinOps ao contêiner de DI.</summary>
    public static IServiceCollection AddFinOpsApplication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddBuildingBlocksApplication(configuration);
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));

        services.AddTransient<IValidator<IngestServiceCostRecord.Command>, IngestServiceCostRecord.Validator>();
        services.AddTransient<IValidator<GetServiceCostAllocationReport.Query>, GetServiceCostAllocationReport.Validator>();
        services.AddTransient<IValidator<GetFinOpsTrendReport.Query>, GetFinOpsTrendReport.Validator>();
        services.AddTransient<IValidator<GetFinOpsInsights.Query>, GetFinOpsInsights.Validator>();
        services.AddTransient<IValidator<GetFinOpsWasteAnalysisReport.Query>, GetFinOpsWasteAnalysisReport.Validator>();
        services.AddTransient<IValidator<GetEnvironmentCostComparisonReport.Query>, GetEnvironmentCostComparisonReport.Validator>();
        services.AddTransient<IValidator<GetCostPerReleaseReport.Query>, GetCostPerReleaseReport.Validator>();

        return services;
    }
}
