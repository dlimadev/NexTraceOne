using System.Text.Json.Serialization;

namespace NexTraceOne.Catalog.Domain.Contracts.Enums;

/// <summary>
/// Protocolo de comunicação do contrato. Define o tipo de especificação
/// suportado pelo módulo Contracts, permitindo governança multi-protocolo
/// com parsing, diff semântico e rulesets específicos para cada formato.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ContractProtocol
{
    /// <summary>OpenAPI 3.0.x / 3.1.x — linha principal REST/HTTP.</summary>
    OpenApi = 0,

    /// <summary>Swagger 2.0 — legado importável, migração assistida para OpenAPI.</summary>
    Swagger = 1,

    /// <summary>WSDL 1.1 / 2.0 — contratos SOAP/Web Services enterprise.</summary>
    Wsdl = 2,

    /// <summary>AsyncAPI 2.6 / 3.x — contratos event-driven (mensageria, streaming).</summary>
    AsyncApi = 3,

    /// <summary>
    /// Protocol Buffers (.proto) — capacidade evolutiva para gRPC.
    /// RESERVADO: sem parser, diff semântico ou UI implementados nesta versão.
    /// </summary>
    Protobuf = 4,

    /// <summary>
    /// GraphQL SDL — capacidade evolutiva para APIs GraphQL.
    /// RESERVADO: sem parser, diff semântico ou UI implementados nesta versão.
    /// </summary>
    GraphQl = 5,

    /// <summary>Worker/Background Service — processo em background declarado por metadados estruturados (trigger, inputs, outputs, side effects).</summary>
    WorkerService = 6,

    // ── Novos protocolos para contratos legacy / mainframe ──

    /// <summary>Copybook COBOL — definição de layout de dados com PIC clauses.</summary>
    Copybook = 7,

    /// <summary>MQ Message Descriptor — formato de mensagem IBM MQ.</summary>
    MqMessageDescriptor = 8,

    /// <summary>Fixed Record Layout — ficheiro com posições fixas (flat file).</summary>
    FixedRecordLayout = 9,

    /// <summary>CICS COMMAREA — área de comunicação para transações CICS.</summary>
    CicsCommarea = 10,

    /// <summary>IMS Segment — segmento de dados IMS/DB.</summary>
    ImsSegment = 11
}
