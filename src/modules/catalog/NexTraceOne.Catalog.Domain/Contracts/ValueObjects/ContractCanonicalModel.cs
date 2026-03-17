using NexTraceOne.Catalog.Domain.Contracts.Enums;

namespace NexTraceOne.Catalog.Domain.Contracts.ValueObjects;

/// <summary>
/// Modelo canônico interno que normaliza qualquer contrato (REST, SOAP, eventos) para uma
/// representação unificada. Permite raciocinar sobre interface, operações, schemas, metadados
/// e segurança de forma independente do formato original.
/// Ponto central para diff semântico cross-protocolo, scorecard técnico e análise de impacto.
/// </summary>
public sealed record ContractCanonicalModel(
    /// <summary>Protocolo original do contrato.</summary>
    ContractProtocol Protocol,
    /// <summary>Título/nome do contrato ou API.</summary>
    string Title,
    /// <summary>Versão da especificação (ex: "3.1.0" para OpenAPI, "2.0" para AsyncAPI).</summary>
    string SpecVersion,
    /// <summary>Descrição geral da API ou serviço.</summary>
    string? Description,
    /// <summary>Lista normalizada de operações extraídas do contrato.</summary>
    IReadOnlyList<ContractOperation> Operations,
    /// <summary>Lista de schemas/tipos globais definidos no contrato.</summary>
    IReadOnlyList<ContractSchemaElement> GlobalSchemas,
    /// <summary>Metadados de segurança extraídos (esquemas de autenticação/autorização).</summary>
    IReadOnlyList<string> SecuritySchemes,
    /// <summary>Servidores/endpoints base definidos no contrato.</summary>
    IReadOnlyList<string> Servers,
    /// <summary>Tags globais para categorização.</summary>
    IReadOnlyList<string> Tags,
    /// <summary>Total de operações no contrato.</summary>
    int OperationCount,
    /// <summary>Total de schemas/tipos definidos.</summary>
    int SchemaCount,
    /// <summary>Indica se o contrato define esquemas de segurança.</summary>
    bool HasSecurityDefinitions,
    /// <summary>Indica se o contrato contém exemplos nas operações.</summary>
    bool HasExamples,
    /// <summary>Indica se o contrato contém descrições nas operações.</summary>
    bool HasDescriptions);
