using System.Text.Json;

namespace NexTraceOne.BuildingBlocks.Observability.Observability.Models;

/// <summary>
/// Constrói condições de query Elasticsearch para filtro por ServiceKind.
///
/// Como ServiceKind é inferido em runtime pelo SpanKindResolver a partir dos atributos OTel,
/// a filtragem por ServiceKind é feita traduzindo o valor de volta para condições Elasticsearch
/// baseadas nos atributos subjacentes do span que determinam aquele ServiceKind.
///
/// As condições retornadas são objectos anónimos compatíveis com Elasticsearch Query DSL
/// (bool/must/should/must_not/exists/term) e serializáveis via System.Text.Json.
/// </summary>
public static class ElasticServiceKindFilter
{
    /// <summary>
    /// Retorna a condição Query DSL do Elasticsearch correspondente ao ServiceKind solicitado,
    /// ou null quando o ServiceKind é desconhecido ou não requer filtragem adicional.
    /// </summary>
    /// <param name="serviceKind">Um dos valores definidos em <see cref="ServiceKindValues"/>.</param>
    /// <returns>Objecto Query DSL pronto para adicionar ao array must, ou null.</returns>
    public static object? Build(string serviceKind)
    {
        return serviceKind switch
        {
            ServiceKindValues.Rest =>
                new
                {
                    @bool = new
                    {
                        should = new object[]
                        {
                            new { exists = new { field = "attributes.http.method" } },
                            new { exists = new { field = "attributes.http.request.method" } }
                        },
                        minimum_should_match = 1
                    }
                },

            ServiceKindValues.Kafka =>
                new { term = new Dictionary<string, object> { ["attributes.messaging.system"] = "kafka" } },

            ServiceKindValues.Soap =>
                new { term = new Dictionary<string, object> { ["attributes.rpc.system"] = "soap" } },

            ServiceKindValues.GRpc =>
                new { term = new Dictionary<string, object> { ["attributes.rpc.system"] = "grpc" } },

            ServiceKindValues.Db =>
                new { @bool = new { must = new object[] { new { exists = new { field = "attributes.db.system" } } } } },

            ServiceKindValues.Messaging =>
                new
                {
                    @bool = new
                    {
                        must = new object[] { new { exists = new { field = "attributes.messaging.system" } } },
                        must_not = new object[]
                        {
                            new { term = new Dictionary<string, object> { ["attributes.messaging.system"] = "kafka" } }
                        }
                    }
                },

            ServiceKindValues.Background =>
                new
                {
                    @bool = new
                    {
                        must = new object[] { new { term = new Dictionary<string, object> { ["span_kind"] = "Internal" } } },
                        must_not = new object[]
                        {
                            new { exists = new { field = "attributes.http.method" } },
                            new { exists = new { field = "attributes.http.request.method" } },
                            new { exists = new { field = "attributes.messaging.system" } },
                            new { exists = new { field = "attributes.db.system" } },
                            new { exists = new { field = "attributes.rpc.system" } }
                        }
                    }
                },

            _ => null
        };
    }
}
