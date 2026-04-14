namespace NexTraceOne.Catalog.Domain.Contracts.ValueObjects;

/// <summary>
/// Value object que representa uma operação normalizada extraída de qualquer tipo de contrato.
/// Permite raciocinar sobre operações de forma unificada, independentemente do protocolo original
/// (REST path+method, SOAP operation, AsyncAPI channel+operation, gRPC method).
/// Essencial para diff semântico, scorecard e análise de impacto cross-protocolo.
/// </summary>
public sealed record ContractOperation(
    /// <summary>Identificador único da operação dentro do contrato (ex: "GET /users", "GetUser", "user/signedup:publish").</summary>
    string OperationId,
    /// <summary>Nome legível da operação (ex: "List Users", "GetUser", "UserSignedUp").</summary>
    string Name,
    /// <summary>Descrição da operação, se disponível na spec original.</summary>
    string? Description,
    /// <summary>Método HTTP (REST), tipo de operação (SOAP/AsyncAPI) ou "rpc" (gRPC).</summary>
    string Method,
    /// <summary>Caminho, canal ou nome do serviço (ex: "/users", "user/signedup", "UserService").</summary>
    string Path,
    /// <summary>Lista de parâmetros de entrada da operação.</summary>
    IReadOnlyList<ContractSchemaElement> InputParameters,
    /// <summary>Lista de campos do payload de resposta/saída.</summary>
    IReadOnlyList<ContractSchemaElement> OutputFields,
    /// <summary>Indica se a operação está marcada como deprecated na spec original.</summary>
    bool IsDeprecated = false,
    /// <summary>Tags/categorias associadas à operação para agrupamento lógico.</summary>
    IReadOnlyList<string>? Tags = null,
    /// <summary>Corpo de requisição da operação (REST POST/PUT/PATCH), quando aplicável.</summary>
    ContractRequestBody? RequestBody = null,
    /// <summary>Respostas HTTP da operação com status code, descrição e schema.</summary>
    IReadOnlyList<ContractOperationResponse>? Responses = null);
