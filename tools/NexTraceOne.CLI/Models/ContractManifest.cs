namespace NexTraceOne.CLI.Models;

/// <summary>
/// Modelo local do manifesto de contrato para validação offline via CLI.
/// Representa a estrutura esperada de um ficheiro JSON de contrato/serviço.
/// </summary>
public sealed record ContractManifest
{
    public string? Name { get; init; }
    public string? Version { get; init; }
    public string? Type { get; init; }
    public string? Description { get; init; }
    public string? Owner { get; init; }
    public string? Team { get; init; }
    public ContractEndpoint[]? Endpoints { get; init; }
    public ContractSchema? Schema { get; init; }
}

/// <summary>
/// Endpoint individual dentro de um contrato REST/SOAP.
/// </summary>
public sealed record ContractEndpoint
{
    public string? Path { get; init; }
    public string? Method { get; init; }
    public string? Summary { get; init; }
}

/// <summary>
/// Schema associado ao contrato (OpenAPI, AsyncAPI, WSDL, etc.).
/// </summary>
public sealed record ContractSchema
{
    public string? Format { get; init; }
    public string? Content { get; init; }
}
