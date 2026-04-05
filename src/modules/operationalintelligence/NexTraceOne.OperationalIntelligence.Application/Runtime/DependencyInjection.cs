using FluentValidation;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using NexTraceOne.BuildingBlocks.Application;
using NexTraceOne.OperationalIntelligence.Application.Automation;
using NexTraceOne.OperationalIntelligence.Application.Incidents;
using NexTraceOne.OperationalIntelligence.Application.Reliability;
using NexTraceOne.OperationalIntelligence.Application.Runtime.Features.CompareEnvironments;
using NexTraceOne.OperationalIntelligence.Application.Runtime.Features.CompareReleaseRuntime;
using NexTraceOne.OperationalIntelligence.Application.Runtime.Features.ComputeObservabilityDebt;
using NexTraceOne.OperationalIntelligence.Application.Runtime.Features.CorrelateTraceToChange;
using NexTraceOne.OperationalIntelligence.Application.Runtime.Features.DetectLogAnomaly;
using NexTraceOne.OperationalIntelligence.Application.Runtime.Features.DetectRuntimeDrift;
using NexTraceOne.OperationalIntelligence.Application.Runtime.Features.EstablishRuntimeBaseline;
using NexTraceOne.OperationalIntelligence.Application.Runtime.Features.GetDriftFindings;
using NexTraceOne.OperationalIntelligence.Application.Runtime.Features.GetObservabilityScore;
using NexTraceOne.OperationalIntelligence.Application.Runtime.Features.GetReleaseHealthTimeline;
using NexTraceOne.OperationalIntelligence.Application.Runtime.Features.GetRuntimeHealth;
using NexTraceOne.OperationalIntelligence.Application.Runtime.Features.IngestRuntimeSnapshot;

namespace NexTraceOne.OperationalIntelligence.Application.Runtime;

/// <summary>
/// Registra serviços da camada Application do módulo RuntimeIntelligence.
/// Inclui: MediatR handlers, FluentValidation validators.
/// Compõe também os subdomínios Reliability e Incidents.
/// P6.5: adicionados EstablishRuntimeBaseline e CompareEnvironments.
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

        // P6.5 — Operational Consistency: baseline establishment + cross-environment comparison
        services.AddTransient<IValidator<EstablishRuntimeBaseline.Command>, EstablishRuntimeBaseline.Validator>();
        services.AddTransient<IValidator<CompareEnvironments.Command>, CompareEnvironments.Validator>();

        // P5.4 — Observability Correlation Engine
        services.AddTransient<IValidator<CorrelateTraceToChange.Query>, CorrelateTraceToChange.Validator>();
        services.AddTransient<IValidator<DetectLogAnomaly.Command>, DetectLogAnomaly.Validator>();

        // ── Reliability (Team-owned Service Reliability) validators ──
        services.AddReliabilityApplication(configuration);

        // ── Incidents (Incident Correlation & Mitigation) validators ──
        services.AddIncidentsApplication(configuration);

        // ── Automation (Operational Automation Workflows) validators ──
        services.AddAutomationApplication(configuration);

        return services;
    }
}
