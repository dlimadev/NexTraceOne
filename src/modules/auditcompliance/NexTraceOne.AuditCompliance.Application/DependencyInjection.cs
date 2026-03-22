using FluentValidation;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using NexTraceOne.AuditCompliance.Application.Features.ConfigureRetention;
using NexTraceOne.AuditCompliance.Application.Features.ExportAuditReport;
using NexTraceOne.AuditCompliance.Application.Features.GetAuditTrail;
using NexTraceOne.AuditCompliance.Application.Features.RecordAuditEvent;
using NexTraceOne.AuditCompliance.Application.Features.CreateAuditCampaign;
using NexTraceOne.AuditCompliance.Application.Features.CreateCompliancePolicy;
using NexTraceOne.AuditCompliance.Application.Features.GetAuditCampaign;
using NexTraceOne.AuditCompliance.Application.Features.GetCompliancePolicy;
using NexTraceOne.AuditCompliance.Application.Features.RecordComplianceResult;
using NexTraceOne.AuditCompliance.Application.Features.SearchAuditLog;
using NexTraceOne.BuildingBlocks.Application;

namespace NexTraceOne.AuditCompliance.Application;

/// <summary>
/// Registra serviços da camada Application do módulo Audit.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddAuditApplication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddBuildingBlocksApplication(configuration);
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));

        services.AddTransient<IValidator<RecordAuditEvent.Command>, RecordAuditEvent.Validator>();
        services.AddTransient<IValidator<GetAuditTrail.Query>, GetAuditTrail.Validator>();
        services.AddTransient<IValidator<SearchAuditLog.Query>, SearchAuditLog.Validator>();
        services.AddTransient<IValidator<ExportAuditReport.Query>, ExportAuditReport.Validator>();
        services.AddTransient<IValidator<ConfigureRetention.Command>, ConfigureRetention.Validator>();
        services.AddTransient<IValidator<CreateCompliancePolicy.Command>, CreateCompliancePolicy.Validator>();
        services.AddTransient<IValidator<GetCompliancePolicy.Query>, GetCompliancePolicy.Validator>();
        services.AddTransient<IValidator<CreateAuditCampaign.Command>, CreateAuditCampaign.Validator>();
        services.AddTransient<IValidator<GetAuditCampaign.Query>, GetAuditCampaign.Validator>();
        services.AddTransient<IValidator<RecordComplianceResult.Command>, RecordComplianceResult.Validator>();

        return services;
    }
}
