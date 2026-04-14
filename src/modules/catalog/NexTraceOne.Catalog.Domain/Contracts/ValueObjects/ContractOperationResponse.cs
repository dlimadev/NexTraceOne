namespace NexTraceOne.Catalog.Domain.Contracts.ValueObjects;

/// <summary>
/// Value object que representa uma resposta normalizada de uma operação REST.
/// Contém o status code, descrição, content type e os campos do schema da resposta.
/// Permite ao preview apresentar a tabela de respostas estilo Swagger UI.
/// </summary>
public sealed record ContractOperationResponse(
    /// <summary>Código de status HTTP (ex: "200", "201", "404").</summary>
    string StatusCode,
    /// <summary>Descrição da resposta (ex: "Sucesso ao obter a lista.").</summary>
    string? Description,
    /// <summary>Content type da resposta (ex: "application/json").</summary>
    string? ContentType,
    /// <summary>Propriedades/campos do schema da resposta, quando resolvidos inline.</summary>
    IReadOnlyList<ContractSchemaElement> Properties,
    /// <summary>Referência ao schema quando não resolvido inline (ex: "#/components/schemas/User").</summary>
    string? SchemaRef = null);
