using NexTraceOne.BuildingBlocks.Application.Abstractions;

namespace NexTraceOne.OperationalIntelligence.Application.Incidents.Abstractions;

/// <summary>
/// Unit of work específico do módulo OperationalIntelligence — subdomain Incidents.
/// Garante commit no IncidentResponseDbContext.
/// </summary>
public interface IIncidentResponseUnitOfWork : IUnitOfWork;
