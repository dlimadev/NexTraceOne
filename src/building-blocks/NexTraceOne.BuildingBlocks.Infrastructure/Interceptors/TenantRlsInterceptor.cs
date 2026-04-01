using Microsoft.EntityFrameworkCore.Diagnostics;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using System.Data;
using System.Data.Common;

namespace NexTraceOne.BuildingBlocks.Infrastructure.Interceptors;

/// <summary>
/// Interceptor EF Core que configura o parâmetro app.current_tenant_id no PostgreSQL
/// antes de cada comando SQL, ativando o Row-Level Security para isolamento multi-tenant.
///
/// O set_config é executado num comando separado na mesma conexão/transação para não
/// poluir o result set da query principal com o retorno text do set_config, o que causava
/// InvalidCastException ao ler colunas uuid/integer como text.
///
/// is_local = false (session-scoped) é usado porque o comando separado corre na sua
/// própria transação implícita (autocommit); com is_local = true a configuração seria
/// revertida antes da query principal executar.
///
/// Segurança: o tenant ID é sempre passado via parâmetro SQL, nunca por interpolação
/// de string, prevenindo SQL injection mesmo em cenários de comprometimento do tenant resolver.
/// </summary>
public sealed class TenantRlsInterceptor(ICurrentTenant currentTenant) : DbCommandInterceptor
{
    private const string SetConfigSql =
        "SELECT set_config('app.current_tenant_id', @__tenantId, false)";

    public override InterceptionResult<DbDataReader> ReaderExecuting(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<DbDataReader> result)
    {
        ApplyTenantContext(command);
        return base.ReaderExecuting(command, eventData, result);
    }

    public override async ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<DbDataReader> result,
        CancellationToken cancellationToken = default)
    {
        await ApplyTenantContextAsync(command, cancellationToken);
        return await base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
    }

    public override InterceptionResult<int> NonQueryExecuting(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<int> result)
    {
        ApplyTenantContext(command);
        return base.NonQueryExecuting(command, eventData, result);
    }

    public override async ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        await ApplyTenantContextAsync(command, cancellationToken);
        return await base.NonQueryExecutingAsync(command, eventData, result, cancellationToken);
    }

    public override InterceptionResult<object> ScalarExecuting(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<object> result)
    {
        ApplyTenantContext(command);
        return base.ScalarExecuting(command, eventData, result);
    }

    public override async ValueTask<InterceptionResult<object>> ScalarExecutingAsync(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<object> result,
        CancellationToken cancellationToken = default)
    {
        await ApplyTenantContextAsync(command, cancellationToken);
        return await base.ScalarExecutingAsync(command, eventData, result, cancellationToken);
    }

    /// <summary>
    /// Executa set_config num comando separado na mesma conexão (sync).
    /// </summary>
    private void ApplyTenantContext(DbCommand command)
    {
        if (currentTenant.Id == Guid.Empty)
        {
            return;
        }

        using var configCmd = CreateTenantCommand(command);

        if (command.CommandTimeout > 0)
        {
            configCmd.CommandTimeout = command.CommandTimeout;
        }

        try
        {
            configCmd.ExecuteNonQuery();
        }
        catch (OperationCanceledException)
        {
            // swallow cancellation here to avoid the interceptor bubbling the exception
            // to EF Core internals; the main operation will observe cancellation as needed.
        }
    }

    /// <summary>
    /// Executa set_config num comando separado na mesma conexão (async).
    /// Usar a variante async evita bloqueio de thread em pipelines EF Core assíncronos
    /// e respeita o CancellationToken da operação principal.
    /// </summary>
    private async Task ApplyTenantContextAsync(DbCommand command, CancellationToken cancellationToken)
    {
        if (currentTenant.Id == Guid.Empty)
        {
            return;
        }

        await using var configCmd = CreateTenantCommand(command);
        // align timeouts so the helper command doesn't outlive the main command
        if (command.CommandTimeout > 0)
        {
            configCmd.CommandTimeout = command.CommandTimeout;
        }

        try
        {
            await configCmd.ExecuteNonQueryAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            // cancellation requested for the overall operation — swallow here so the
            // interceptor doesn't mask upstream handling. The caller will observe
            // cancellation via its own token as appropriate.
        }
    }

    /// <summary>
    /// Cria o DbCommand para set_config reutilizando conexão e transação do comando principal.
    /// </summary>
    private DbCommand CreateTenantCommand(DbCommand command)
    {
        var configCmd = command.Connection!.CreateCommand();
        configCmd.Transaction = command.Transaction;
        configCmd.CommandText = SetConfigSql;

        var tenantParam = configCmd.CreateParameter();
        tenantParam.ParameterName = "__tenantId";
        tenantParam.Value = currentTenant.Id.ToString();
        tenantParam.DbType = DbType.String;
        configCmd.Parameters.Add(tenantParam);

        return configCmd;
    }
}
