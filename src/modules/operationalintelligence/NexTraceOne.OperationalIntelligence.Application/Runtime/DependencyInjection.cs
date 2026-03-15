using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NexTraceOne.BuildingBlocks.Application;
using NexTraceOne.OperationalIntelligence.Application.Reliability;
using NexTraceOne.RuntimeIntelligence.Application.Features.CompareReleaseRuntime;
using NexTraceOne.RuntimeIntelligence.Application.Features.ComputeObservabilityDebt;
using NexTraceOne.RuntimeIntelligence.Application.Features.DetectRuntimeDrift;
using NexTraceOne.RuntimeIntelligence.Application.Features.GetDriftFindings;
using NexTraceOne.RuntimeIntelligence.Application.Features.GetObservabilityScore;
using NexTraceOne.RuntimeIntelligence.Application.Features.GetReleaseHealthTimeline;
using NexTraceOne.RuntimeIntelligence.Application.Features.GetRuntimeHealth;
using NexTraceOne.RuntimeIntelligence.Application.Features.IngestRuntimeSnapshot;

namespace NexTraceOne.RuntimeIntelligence.Application;

/// <summary>
/// Registra serviços da camada Application do módulo RuntimeIntelligence.
/// Inclui: MediatR handlers, FluentValidation validators.
/// Compõe também o subdomínio Reliability (Team-owned Service Reliability).
/// </summary>
public static class DependencyInjection
{
    /// <summary>Adiciona os serviços da camada Application do módulo RuntimeIntelligence ao contêiner de DI.</summary>
    public static IServiceCollection AddRuntimeIntelligenceApplication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddBuildingBlocksApplication(configuration);
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));

        // ── Runtime Intelligence validators ─────────────────────────
        services.AddTransient<IValidator<IngestRuntimeSnapshot.Command>, IngestRuntimeSnapshot.Validator>();
        services.AddTransient<IValidator<GetRuntimeHealth.Query>, GetRuntimeHealth.Validator>();
        services.AddTransient<IValidator<GetObservabilityScore.Query>, GetObservabilityScore.Validator>();
        services.AddTransient<IValidator<ComputeObservabilityDebt.Command>, ComputeObservabilityDebt.Validator>();
        services.AddTransient<IValidator<DetectRuntimeDrift.Command>, DetectRuntimeDrift.Validator>();
        services.AddTransient<IValidator<GetDriftFindings.Query>, GetDriftFindings.Validator>();
        services.AddTransient<IValidator<GetReleaseHealthTimeline.Query>, GetReleaseHealthTimeline.Validator>();
        services.AddTransient<IValidator<CompareReleaseRuntime.Query>, CompareReleaseRuntime.Validator>();

        // ── Reliability (Team-owned Service Reliability) validators ──
        services.AddReliabilityApplication(configuration);

        return services;
    }
}
