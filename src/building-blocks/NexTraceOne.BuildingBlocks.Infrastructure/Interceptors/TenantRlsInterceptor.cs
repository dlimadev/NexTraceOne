using Microsoft.EntityFrameworkCore.Diagnostics;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using System.Data.Common;

namespace NexTraceOne.BuildingBlocks.Infrastructure.Interceptors;

/// <summary>
/// Interceptor EF Core que executa SET app.current_tenant_id antes de cada query.
/// Ativa o Row-Level Security do PostgreSQL para isolamento de dados multi-tenant.
/// </summary>
public sealed class TenantRlsInterceptor(ICurrentTenant currentTenant) : DbCommandInterceptor
{
    public override InterceptionResult<DbDataReader> ReaderExecuting(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<DbDataReader> result)
    {
        ApplyTenantContext(command);
        return base.ReaderExecuting(command, eventData, result);
    }

    public override InterceptionResult<int> NonQueryExecuting(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<int> result)
    {
        ApplyTenantContext(command);
        return base.NonQueryExecuting(command, eventData, result);
    }

    public override InterceptionResult<object> ScalarExecuting(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<object> result)
    {
        ApplyTenantContext(command);
        return base.ScalarExecuting(command, eventData, result);
    }

    private void ApplyTenantContext(DbCommand command)
    {
        if (currentTenant.Id == Guid.Empty)
        {
            return;
        }

        command.CommandText = $"select set_config('app.current_tenant_id', '{currentTenant.Id}', true); {command.CommandText}";
    }
}
