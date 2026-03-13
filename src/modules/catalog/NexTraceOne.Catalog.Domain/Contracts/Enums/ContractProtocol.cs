namespace NexTraceOne.Contracts.Domain.Enums;

/// <summary>
/// Protocolo de comunicação do contrato. Define o tipo de especificação
/// suportado pelo módulo Contracts, permitindo governança multi-protocolo
/// com parsing, diff semântico e rulesets específicos para cada formato.
/// </summary>
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

    /// <summary>Protocol Buffers (.proto) — capacidade evolutiva para gRPC.</summary>
    Protobuf = 4,

    /// <summary>GraphQL SDL — capacidade evolutiva para APIs GraphQL.</summary>
    GraphQl = 5
}
