using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NexTraceOne.BuildingBlocks.Application;
using NexTraceOne.Licensing.Application.Features.ActivateLicense;
using NexTraceOne.Licensing.Application.Features.AlertLicenseThreshold;
using NexTraceOne.Licensing.Application.Features.CheckCapability;
using NexTraceOne.Licensing.Application.Features.GetLicenseStatus;
using NexTraceOne.Licensing.Application.Features.TrackUsageMetric;
using NexTraceOne.Licensing.Application.Features.VerifyLicenseOnStartup;

namespace NexTraceOne.Licensing.Application;

/// <summary>
/// Registra serviços da camada Application do módulo Licensing.
/// Inclui: MediatR handlers, FluentValidation validators.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddLicensingApplication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddBuildingBlocksApplication(configuration);
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));
        services.AddTransient<IValidator<ActivateLicense.Command>, ActivateLicense.Validator>();
        services.AddTransient<IValidator<VerifyLicenseOnStartup.Query>, VerifyLicenseOnStartup.Validator>();
        services.AddTransient<IValidator<CheckCapability.Query>, CheckCapability.Validator>();
        services.AddTransient<IValidator<TrackUsageMetric.Command>, TrackUsageMetric.Validator>();
        services.AddTransient<IValidator<AlertLicenseThreshold.Query>, AlertLicenseThreshold.Validator>();
        services.AddTransient<IValidator<GetLicenseStatus.Query>, GetLicenseStatus.Validator>();

        return services;
    }
}
