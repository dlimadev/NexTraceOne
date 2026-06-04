using FluentValidation;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using NexTraceOne.Governance.Application.AuditCompliance.Features.ConfigureRetention;
using NexTraceOne.Governance.Application.AuditCompliance.Features.ExportAuditReport;
using NexTraceOne.Governance.Application.AuditCompliance.Features.GetAuditTrail;
using NexTraceOne.Governance.Application.AuditCompliance.Features.RecordAuditEvent;
using NexTraceOne.Governance.Application.AuditCompliance.Features.CreateAuditCampaign;
using NexTraceOne.Governance.Application.AuditCompliance.Features.CreateCompliancePolicy;
using NexTraceOne.Governance.Application.AuditCompliance.Features.GetAuditCampaign;
using NexTraceOne.Governance.Application.AuditCompliance.Features.GetCompliancePolicy;
using NexTraceOne.Governance.Application.AuditCompliance.Features.RecordComplianceResult;
using NexTraceOne.Governance.Application.AuditCompliance.Features.SearchAuditLog;
using NexTraceOne.BuildingBlocks.Application;

namespace NexTraceOne.Governance.Application.AuditCompliance;

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
