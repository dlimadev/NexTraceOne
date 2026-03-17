using System.Text.Json.Serialization;

namespace NexTraceOne.Catalog.Domain.Contracts.Enums;

/// <summary>
/// Tipo de contrato suportado pelo Contract Studio.
/// Categoriza contratos conforme sua natureza funcional.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ContractType
{
    /// <summary>Contrato de API REST (OpenAPI, Swagger).</summary>
    RestApi = 0,

    /// <summary>Contrato de serviço SOAP (WSDL/XSD).</summary>
    Soap = 1,

    /// <summary>Contrato de evento (Kafka, AsyncAPI).</summary>
    Event = 2,

    /// <summary>Contrato de serviço em background.</summary>
    BackgroundService = 3,

    /// <summary>Schema canónico partilhado entre serviços.</summary>
    SharedSchema = 4
}
