using NexTraceOne.BuildingBlocks.Application.Abstractions;

namespace NexTraceOne.IdentityAccess.Application.Abstractions;

/// <summary>
/// Unit of work específico do módulo IdentityAccess para garantir commit no IdentityDbContext.
/// </summary>
public interface IIdentityAccessUnitOfWork : IUnitOfWork;
