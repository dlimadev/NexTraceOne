namespace NexTraceOne.Contracts.Domain.ValueObjects;

/// <summary>
/// Value object que representa um elemento de schema normalizado (campo, parâmetro, tipo).
/// Permite raciocinar sobre a estrutura de dados de qualquer contrato, independentemente
/// do formato original (JSON Schema, XSD, Avro, Protobuf).
/// Usado dentro de ContractOperation e ContractCanonicalModel para análise de impacto.
/// </summary>
public sealed record ContractSchemaElement(
    /// <summary>Nome do campo ou parâmetro (ex: "userId", "email", "body").</summary>
    string Name,
    /// <summary>Tipo de dados normalizado (ex: "string", "integer", "object", "array").</summary>
    string DataType,
    /// <summary>Indica se o campo é obrigatório.</summary>
    bool IsRequired,
    /// <summary>Descrição do campo, se disponível na spec original.</summary>
    string? Description = null,
    /// <summary>Formato adicional do tipo (ex: "date-time", "email", "int64").</summary>
    string? Format = null,
    /// <summary>Valor padrão quando definido na spec.</summary>
    string? DefaultValue = null,
    /// <summary>Indica se o campo está marcado como deprecated.</summary>
    bool IsDeprecated = false,
    /// <summary>Elementos filhos para tipos compostos (object, array de objects).</summary>
    IReadOnlyList<ContractSchemaElement>? Children = null);
