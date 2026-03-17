using NexTraceOne.Catalog.Domain.Contracts.Enums;

namespace NexTraceOne.Catalog.Domain.Contracts.ValueObjects;

/// <summary>
/// Value object que define o perfil de interoperabilidade de um contrato.
/// Indica quais formatos de exportação são suportados, regras de compatibilidade
/// aplicáveis e capacidades de conversão entre protocolos.
/// Utilizado para avaliar readiness de migração e integração entre sistemas heterogêneos.
/// </summary>
public sealed record InteroperabilityProfile(
    /// <summary>Protocolo original do contrato.</summary>
    ContractProtocol SourceProtocol,
    /// <summary>Formatos de exportação suportados (ex: "openapi-json", "openapi-yaml", "wsdl-xml").</summary>
    IReadOnlyList<string> SupportedExportFormats,
    /// <summary>Protocolos para os quais conversão é possível (ex: OpenAPI → AsyncAPI).</summary>
    IReadOnlyList<ContractProtocol> ConvertibleTo,
    /// <summary>Indica se o contrato suporta validação bidirecional (import→export→import sem perda).</summary>
    bool SupportsRoundTrip,
    /// <summary>Indica se possui binding ativo com Schema Registry.</summary>
    bool HasSchemaRegistryBinding,
    /// <summary>Capabilities disponíveis para este protocolo.</summary>
    IReadOnlyList<string> Capabilities);
