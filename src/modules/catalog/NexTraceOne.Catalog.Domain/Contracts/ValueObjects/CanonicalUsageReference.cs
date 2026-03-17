namespace NexTraceOne.Catalog.Domain.Contracts.ValueObjects;

/// <summary>
/// Referência de uso de uma entidade Canonical num contrato específico.
/// Permite rastrear onde e como as entidades Canonical são utilizadas,
/// calcular impacto de alterações e validar aderência a políticas de reutilização.
/// </summary>
public sealed record CanonicalUsageReference(
    /// <summary>Identificador da entidade Canonical utilizada.</summary>
    Guid CanonicalEntityId,
    /// <summary>Nome da entidade Canonical.</summary>
    string CanonicalEntityName,
    /// <summary>Identificador da versão de contrato que usa a entidade.</summary>
    Guid ContractVersionId,
    /// <summary>Identificador do asset de API.</summary>
    Guid ApiAssetId,
    /// <summary>Caminho no contrato onde a entidade é referenciada (ex: "#/components/schemas/Customer").</summary>
    string ReferencePath,
    /// <summary>Tipo de uso: "request-body", "response-body", "event-payload", "parameter", "schema-ref".</summary>
    string UsageType,
    /// <summary>Indica se o uso está conforme com a entidade Canonical (schema compatível).</summary>
    bool IsConformant,
    /// <summary>Mensagem de conformidade, quando não conforme.</summary>
    string? ConformanceMessage = null);
