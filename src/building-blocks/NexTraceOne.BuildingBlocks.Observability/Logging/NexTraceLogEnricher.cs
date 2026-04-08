using System.Diagnostics;
using Serilog.Core;
using Serilog.Events;

namespace NexTraceOne.BuildingBlocks.Observability.Logging;

/// <summary>
/// Enricher Serilog que adiciona contexto de tracing distribuído (TraceId, SpanId)
/// e contexto de plataforma (TenantId, EnvironmentId) a cada evento de log.
///
/// Correlação logs ↔ traces: cada log emitido dentro de um span OpenTelemetry
/// ganha automaticamente as propriedades TraceId e SpanId, permitindo pesquisa
/// cruzada no Elasticsearch (nextraceone-logs-* + nextraceone-traces-*).
///
/// Prefixo "nexttrace." nos atributos de span é lido das tags da Activity atual
/// (preenchidas via TelemetryContextEnricher). Aqui os valores são promovidos
/// para propriedades de log Serilog para que apareçam nos documentos do Elastic.
/// </summary>
public sealed class NexTraceLogEnricher : ILogEventEnricher
{
    /// <summary>Enriquece cada evento de log com contexto de tracing e plataforma.</summary>
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        var activity = Activity.Current;

        if (activity is not null)
        {
            var traceId = activity.TraceId.ToString();
            if (traceId != "00000000000000000000000000000000")
            {
                logEvent.AddPropertyIfAbsent(
                    propertyFactory.CreateProperty("TraceId", traceId));
            }

            var spanId = activity.SpanId.ToString();
            if (spanId != "0000000000000000")
            {
                logEvent.AddPropertyIfAbsent(
                    propertyFactory.CreateProperty("SpanId", spanId));
            }

            // Promover atributos de plataforma do span para propriedades do log
            var tenantId = activity.GetTagItem(Telemetry.TelemetryContextEnricher.TenantIdAttribute);
            if (tenantId is not null)
            {
                logEvent.AddPropertyIfAbsent(
                    propertyFactory.CreateProperty("TenantId", tenantId.ToString()));
            }

            var environmentId = activity.GetTagItem(Telemetry.TelemetryContextEnricher.EnvironmentIdAttribute);
            if (environmentId is not null)
            {
                logEvent.AddPropertyIfAbsent(
                    propertyFactory.CreateProperty("EnvironmentId", environmentId.ToString()));
            }

            var correlationId = activity.GetTagItem(Telemetry.TelemetryContextEnricher.CorrelationIdAttribute);
            if (correlationId is not null)
            {
                logEvent.AddPropertyIfAbsent(
                    propertyFactory.CreateProperty("CorrelationId", correlationId.ToString()));
            }

            var serviceOrigin = activity.GetTagItem(Telemetry.TelemetryContextEnricher.ServiceOriginAttribute);
            if (serviceOrigin is not null)
            {
                logEvent.AddPropertyIfAbsent(
                    propertyFactory.CreateProperty("ServiceOrigin", serviceOrigin.ToString()));
            }

            var userId = activity.GetTagItem(Telemetry.TelemetryContextEnricher.UserIdAttribute);
            if (userId is not null)
            {
                logEvent.AddPropertyIfAbsent(
                    propertyFactory.CreateProperty("UserId", userId.ToString()));
            }
        }
    }
}
