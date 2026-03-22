namespace NexTraceOne.Catalog.Contracts.Graph.DTOs;

/// <summary>DTO de dependência cross-team, exposto pelo módulo Catalog Graph.</summary>
public sealed record CrossTeamDependencyInfo(
    string DependencyId,
    string SourceServiceName,
    string TargetServiceName,
    string TargetTeamId,
    string TargetTeamName,
    string DependencyType);
