namespace NexTraceOne.Catalog.Contracts.Graph.DTOs;

/// <summary>DTO de serviço associado a uma equipa, exposto pelo módulo Catalog Graph.</summary>
public sealed record TeamServiceInfo(
    string ServiceId,
    string Name,
    string Domain,
    string Criticality,
    string OwnershipType);
