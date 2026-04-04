namespace NexTraceOne.Catalog.Domain.Templates.Enums;

/// <summary>
/// Tipo de serviço gerado pelo template — define o padrão de contrato e scaffolding.
/// </summary>
public enum TemplateServiceType
{
    /// <summary>REST API com contratos OpenAPI.</summary>
    RestApi = 1,

    /// <summary>Serviço de eventos Kafka/AsyncAPI.</summary>
    EventDriven = 2,

    /// <summary>Background worker / job agendado.</summary>
    BackgroundWorker = 3,

    /// <summary>gRPC service com contratos Protobuf.</summary>
    Grpc = 4,

    /// <summary>SOAP service / WSDL.</summary>
    Soap = 5,

    /// <summary>Template genérico sem tipo definido.</summary>
    Generic = 6
}
