namespace NexTraceOne.Catalog.Contracts.Graph.DTOs;

/// <summary>DTO de contrato associado a uma equipa, exposto pelo módulo Catalog Graph.</summary>
public sealed record TeamContractInfo(
    string ContractId,
    string Name,
    string Type,
    string Version,
    string Status);
