using Microsoft.EntityFrameworkCore.Diagnostics;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using System.Data;
using System.Data.Common;

namespace NexTraceOne.BuildingBlocks.Infrastructure.Interceptors;

/// <summary>
/// Interceptor EF Core que configura o parâmetro app.current_tenant_id no PostgreSQL
/// antes de cada comando SQL, ativando o Row-Level Security para isolamento multi-tenant.
///
/// Segurança: o tenant ID é sempre passado via parâmetro SQL ($1), nunca por interpolação
/// de string, prevenindo SQL injection mesmo em cenários de comprometimento do tenant resolver.
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

    /// <summary>
    /// Injeta o tenant ID como parâmetro SQL parametrizado para prevenir SQL injection.
    /// O valor é passado via DbParameter, garantindo que nunca será interpretado como SQL.
    /// </summary>
    private void ApplyTenantContext(DbCommand command)
    {
        if (currentTenant.Id == Guid.Empty)
        {
            return;
        }

        var tenantParam = command.CreateParameter();
        tenantParam.ParameterName = "__tenantId";
        tenantParam.Value = currentTenant.Id.ToString();
        tenantParam.DbType = DbType.String;
        command.Parameters.Add(tenantParam);

        command.CommandText = $"select set_config('app.current_tenant_id', @__tenantId, true); {command.CommandText}";
    }
}
