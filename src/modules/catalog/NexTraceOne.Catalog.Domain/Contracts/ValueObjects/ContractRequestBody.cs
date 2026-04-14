namespace NexTraceOne.Catalog.Domain.Contracts.ValueObjects;

/// <summary>
/// Value object que representa o corpo de requisição normalizado de uma operação REST.
/// Contém o content type, flag de obrigatoriedade e os campos do schema do body.
/// Permite ao preview e ao diff semântico apresentar e comparar request bodies.
/// </summary>
public sealed record ContractRequestBody(
    /// <summary>Content type do body (ex: "application/json").</summary>
    string ContentType,
    /// <summary>Indica se o body é obrigatório na operação.</summary>
    bool IsRequired,
    /// <summary>Propriedades/campos do schema do body, quando resolvidos inline.</summary>
    IReadOnlyList<ContractSchemaElement> Properties,
    /// <summary>Referência ao schema quando não resolvido inline (ex: "#/components/schemas/CreateUserRequest").</summary>
    string? SchemaRef = null);
