namespace NexTraceOne.Catalog.Application.Contracts.Generation;

/// <summary>
/// Modelo neutro (independente de linguagem) de um contrato OpenAPI, usado como entrada
/// para os geradores de código determinísticos. O parser de OpenAPI produz este modelo;
/// os geradores por stack (.NET, Java, …) consomem-no.
///
/// Manter neutro permite suportar múltiplas stacks sem reparsear o contrato.
/// </summary>
public sealed record OpenApiContractModel(
    string Title,
    IReadOnlyList<SchemaModel> Schemas,
    IReadOnlyList<OperationModel> Operations);

/// <summary>Schema (componente reutilizável) do contrato — origem dos DTOs.</summary>
public sealed record SchemaModel(
    string Name,
    IReadOnlyList<PropertyModel> Properties);

/// <summary>
/// Propriedade de um schema. <paramref name="NeutralType"/> usa um vocabulário neutro:
/// "string", "integer", "long", "number", "boolean", "date-time", "date", "uuid",
/// "object", "ref:&lt;SchemaName&gt;" ou "array:&lt;itemNeutralType&gt;".
/// </summary>
public sealed record PropertyModel(
    string Name,
    string NeutralType,
    bool Required);

/// <summary>Operação HTTP do contrato — origem dos endpoints.</summary>
public sealed record OperationModel(
    string Method,
    string Path,
    string OperationId,
    string? Tag,
    string? RequestSchemaName,
    string? ResponseSchemaName,
    string? Summary);
