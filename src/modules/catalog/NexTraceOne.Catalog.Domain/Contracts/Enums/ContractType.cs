namespace NexTraceOne.Contracts.Domain.Enums;

/// <summary>
/// Tipo de contrato suportado pelo Contract Studio.
/// Categoriza contratos conforme sua natureza funcional.
/// </summary>
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
