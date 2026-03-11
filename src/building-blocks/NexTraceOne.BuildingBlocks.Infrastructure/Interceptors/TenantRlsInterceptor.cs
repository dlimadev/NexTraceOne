using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Data.Common;

namespace NexTraceOne.BuildingBlocks.Infrastructure.Interceptors;

/// <summary>
/// Interceptor EF Core que executa SET app.current_tenant_id antes de cada query.
/// Ativa o Row-Level Security do PostgreSQL para isolamento de dados multi-tenant.
/// </summary>
public sealed class TenantRlsInterceptor : DbCommandInterceptor
{
    // TODO: Implementar ReaderExecutingAsync para SET app.current_tenant_id
}
