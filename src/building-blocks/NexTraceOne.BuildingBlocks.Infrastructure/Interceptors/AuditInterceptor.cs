using Microsoft.EntityFrameworkCore.Diagnostics;

namespace NexTraceOne.BuildingBlocks.Infrastructure.Interceptors;

/// <summary>
/// Interceptor que preenche CreatedAt/By e UpdatedAt/By automaticamente
/// em todas as AuditableEntity antes do SaveChanges.
/// </summary>
public sealed class AuditInterceptor : SaveChangesInterceptor
{
    // TODO: Implementar SavingChangesAsync para preencher campos de auditoria
}
