using NexTraceOne.BuildingBlocks.Application.Abstractions;

namespace NexTraceOne.AuditCompliance.Application.Abstractions;

/// <summary>
/// Unit of work específico do módulo AuditCompliance para garantir commit no AuditDbContext.
/// </summary>
public interface IAuditComplianceUnitOfWork : IUnitOfWork;
