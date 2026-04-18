namespace NexTraceOne.BuildingBlocks.Observability.Observability.Models;

/// <summary>
/// Constrói cláusulas WHERE SQL para filtro por ServiceKind no ClickHouse.
///
/// Como ServiceKind é inferido em runtime pelo SpanKindResolver a partir dos atributos OTel,
/// a filtragem por ServiceKind é feita traduzindo o valor de volta para condições SQL
/// sobre a coluna SpanAttributes (Map(String, String)) do ClickHouse.
///
/// Em ClickHouse, SpanAttributes['key'] retorna string vazia quando a chave não existe.
/// A lógica espelha as regras do SpanKindResolver para garantir consistência.
/// </summary>
public static class ClickHouseServiceKindFilter
{
    /// <summary>
    /// Retorna a cláusula WHERE SQL correspondente ao ServiceKind solicitado, ou null
    /// quando o ServiceKind é desconhecido ou não requer filtragem adicional.
    /// </summary>
    /// <param name="serviceKind">Um dos valores definidos em <see cref="ServiceKindValues"/>.</param>
    /// <returns>Fragmento SQL pronto para concatenar com AND, ou null.</returns>
    public static string? Build(string serviceKind)
    {
        return serviceKind switch
        {
            ServiceKindValues.Rest =>
                "(SpanAttributes['http.method'] != '' OR SpanAttributes['http.request.method'] != '')",

            ServiceKindValues.Kafka =>
                "SpanAttributes['messaging.system'] = 'kafka'",

            ServiceKindValues.Soap =>
                "SpanAttributes['rpc.system'] = 'soap'",

            ServiceKindValues.GRpc =>
                "SpanAttributes['rpc.system'] = 'grpc'",

            ServiceKindValues.Db =>
                "SpanAttributes['db.system'] != ''",

            ServiceKindValues.Messaging =>
                "(SpanAttributes['messaging.system'] != '' AND SpanAttributes['messaging.system'] != 'kafka')",

            ServiceKindValues.Background =>
                "(SpanKind = 'Internal' AND SpanAttributes['http.method'] = '' AND SpanAttributes['http.request.method'] = '' AND SpanAttributes['messaging.system'] = '' AND SpanAttributes['db.system'] = '' AND SpanAttributes['rpc.system'] = '')",

            _ => null
        };
    }
}
