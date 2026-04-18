namespace NexTraceOne.BuildingBlocks.Observability.Observability.Models;

/// <summary>
/// Resolve o ServiceKind de um span com base nas convenções semânticas OpenTelemetry.
///
/// A inferência segue a ordem de prioridade abaixo, privilegiando os atributos
/// mais específicos primeiro:
///   1. messaging.system = kafka             → Kafka
///   2. messaging.system (qualquer outro)    → Messaging
///   3. rpc.system = soap                    → SOAP
///   4. rpc.system = grpc                    → gRPC
///   5. db.system                            → DB
///   6. http.method / http.request.method    → REST
///   7. SpanKind = Internal (ou ausente)     → Background
///   8. Fallback                             → Unknown
///
/// Referência: https://opentelemetry.io/docs/concepts/semantic-conventions/
/// </summary>
public static class SpanKindResolver
{
    /// <summary>
    /// Determina o ServiceKind a partir dos atributos do span e do SpanKind OTel.
    /// </summary>
    /// <param name="spanAttributes">Atributos do span (pode ser null).</param>
    /// <param name="spanKind">
    /// SpanKind OTel: Internal, Server, Client, Producer, Consumer.
    /// Pode ser null quando não disponível no storage.
    /// </param>
    /// <returns>Um dos valores definidos em <see cref="ServiceKindValues"/>.</returns>
    public static string Resolve(
        IReadOnlyDictionary<string, string>? spanAttributes,
        string? spanKind)
    {
        if (spanAttributes is null || spanAttributes.Count == 0)
            return ResolveFromSpanKindOnly(spanKind);

        // 1. Messaging — Kafka específico ou genérico
        if (spanAttributes.TryGetValue("messaging.system", out var messagingSystem))
        {
            return string.Equals(messagingSystem, "kafka", StringComparison.OrdinalIgnoreCase)
                ? ServiceKindValues.Kafka
                : ServiceKindValues.Messaging;
        }

        // 2. RPC (SOAP / gRPC)
        if (spanAttributes.TryGetValue("rpc.system", out var rpcSystem))
        {
            if (string.Equals(rpcSystem, "soap", StringComparison.OrdinalIgnoreCase))
                return ServiceKindValues.Soap;
            if (string.Equals(rpcSystem, "grpc", StringComparison.OrdinalIgnoreCase))
                return ServiceKindValues.GRpc;
        }

        // 3. Database
        if (spanAttributes.ContainsKey("db.system"))
            return ServiceKindValues.Db;

        // 4. HTTP / REST (suporta atributos legados e novos)
        if (spanAttributes.ContainsKey("http.method") || spanAttributes.ContainsKey("http.request.method"))
            return ServiceKindValues.Rest;

        // 5. SpanKind-based fallback
        return ResolveFromSpanKindOnly(spanKind);
    }

    private static string ResolveFromSpanKindOnly(string? spanKind)
    {
        // Producer e Consumer sem messaging.system inferem Messaging genérico
        if (string.Equals(spanKind, "Producer", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(spanKind, "Consumer", StringComparison.OrdinalIgnoreCase))
            return ServiceKindValues.Messaging;

        // Server e Client sem http/rpc/db attributes são indetermináveis
        if (string.Equals(spanKind, "Server", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(spanKind, "Client", StringComparison.OrdinalIgnoreCase))
            return ServiceKindValues.Unknown;

        // Internal sem outros indicadores → Background
        if (string.Equals(spanKind, "Internal", StringComparison.OrdinalIgnoreCase))
            return ServiceKindValues.Background;

        return ServiceKindValues.Unknown;
    }
}
