using MediatR;
using Microsoft.Extensions.Logging;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using System.Diagnostics;

namespace NexTraceOne.BuildingBlocks.Application.Behaviors;

/// <summary>
/// Pipeline behavior que enriquece o escopo de logging com TenantId + EnvironmentId
/// para cada request MediatR.
///
/// Complementa o LoggingBehavior adicionando propriedades de contexto operacional
/// disponíveis em todos os logs dentro do request via ILogger.BeginScope:
/// - TenantId
/// - EnvironmentId
/// - IsProductionLike
///
/// Também enriquece a Activity OpenTelemetry corrente com as mesmas propriedades.
///
/// SEGURANÇA: Não loga conteúdo do request — apenas metadata de contexto.
///
/// POSIÇÃO NA PIPELINE: Deve ser registrado ANTES de LoggingBehavior para que
/// as propriedades estejam disponíveis quando LoggingBehavior emite seus logs.
/// </summary>
public sealed class ContextualLoggingBehavior<TRequest, TResponse>(
    ICurrentTenant currentTenant,
    ICurrentEnvironment currentEnvironment,
    ILogger<ContextualLoggingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var tenantIdValue = currentTenant.Id != Guid.Empty ? currentTenant.Id.ToString() : "none";
        var environmentIdValue = currentEnvironment.IsResolved
            ? currentEnvironment.EnvironmentId.ToString()
            : "none";

        // ILogger.BeginScope é suportado por Serilog, Application Insights e outros providers.
        // As propriedades ficam disponíveis em todos os logs dentro do escopo.
        using var scope = logger.BeginScope(new Dictionary<string, object?>
        {
            ["TenantId"] = tenantIdValue,
            ["EnvironmentId"] = environmentIdValue,
            ["IsProductionLike"] = currentEnvironment.IsResolved ? currentEnvironment.IsProductionLike : (bool?)null,
        });

        // Enriquece a Activity OpenTelemetry corrente.
        if (Activity.Current is { } activity)
        {
            if (currentTenant.Id != Guid.Empty)
                activity.SetTag("nexttrace.tenant_id", currentTenant.Id.ToString());

            if (currentEnvironment.IsResolved)
            {
                activity.SetTag("nexttrace.environment_id", currentEnvironment.EnvironmentId.ToString());
                activity.SetTag("nexttrace.environment.is_production_like",
                    currentEnvironment.IsProductionLike.ToString().ToLowerInvariant());
            }
        }

        return await next();
    }
}
