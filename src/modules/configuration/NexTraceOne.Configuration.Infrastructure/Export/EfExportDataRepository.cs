using Microsoft.EntityFrameworkCore;

using NexTraceOne.Configuration.Application.Features.ExportData;
using NexTraceOne.Configuration.Infrastructure.Persistence;

namespace NexTraceOne.Configuration.Infrastructure.Export;

/// <summary>
/// Implementação EF Core do repositório de exportação de dados.
/// Consulta as tabelas disponíveis no ConfigurationDbContext e retorna linhas como dicionários coluna→valor.
/// </summary>
internal sealed class EfExportDataRepository(ConfigurationDbContext context) : IExportDataRepository
{
    /// <summary>Número máximo de linhas retornadas por exportação síncrona.</summary>
    private const int MaxExportRows = 5000;
    public async Task<IReadOnlyList<IReadOnlyDictionary<string, object?>>> GetExportRowsAsync(
        string entity,
        string[]? columns,
        CancellationToken cancellationToken = default)
    {
        return entity.ToLowerInvariant() switch
        {
            "audit_events" => await GetAuditEventsAsync(columns, cancellationToken),
            "contracts" => await GetContractTemplatesAsync(columns, cancellationToken),
            "scheduled_reports" => await GetScheduledReportsAsync(columns, cancellationToken),
            _ => []
        };
    }

    private async Task<IReadOnlyList<IReadOnlyDictionary<string, object?>>> GetAuditEventsAsync(
        string[]? columns,
        CancellationToken cancellationToken)
    {
        var rows = await context.AuditEntries
            .AsNoTracking()
            .OrderByDescending(e => e.ChangedAt)
            .Take(MaxExportRows)
            .Select(e => new
            {
                id = (object?)e.Id.Value,
                key = (object?)e.Key,
                scope = (object?)e.Scope.ToString(),
                action = (object?)e.Action,
                changed_by = (object?)e.ChangedBy,
                changed_at = (object?)e.ChangedAt.ToString("o"),
                previous_value = (object?)e.PreviousValue,
                new_value = (object?)e.NewValue
            })
            .ToListAsync(cancellationToken);

        return rows.Select(r => BuildRow(new Dictionary<string, object?>
        {
            ["id"] = r.id,
            ["key"] = r.key,
            ["scope"] = r.scope,
            ["action"] = r.action,
            ["changed_by"] = r.changed_by,
            ["changed_at"] = r.changed_at,
            ["previous_value"] = r.previous_value,
            ["new_value"] = r.new_value
        }, columns)).ToList();
    }

    private async Task<IReadOnlyList<IReadOnlyDictionary<string, object?>>> GetContractTemplatesAsync(
        string[]? columns,
        CancellationToken cancellationToken)
    {
        var rows = await context.ContractTemplates
            .AsNoTracking()
            .OrderByDescending(t => t.CreatedAt)
            .Take(MaxExportRows)
            .Select(t => new
            {
                id = (object?)t.Id.Value,
                name = (object?)t.Name,
                contract_type = (object?)t.ContractType,
                is_built_in = (object?)t.IsBuiltIn,
                created_by = (object?)t.TemplateCreatedBy,
                created_at = (object?)t.CreatedAt.ToString("o")
            })
            .ToListAsync(cancellationToken);

        return rows.Select(r => BuildRow(new Dictionary<string, object?>
        {
            ["id"] = r.id,
            ["name"] = r.name,
            ["contract_type"] = r.contract_type,
            ["is_built_in"] = r.is_built_in,
            ["created_by"] = r.created_by,
            ["created_at"] = r.created_at
        }, columns)).ToList();
    }

    private async Task<IReadOnlyList<IReadOnlyDictionary<string, object?>>> GetScheduledReportsAsync(
        string[]? columns,
        CancellationToken cancellationToken)
    {
        var rows = await context.ScheduledReports
            .AsNoTracking()
            .OrderByDescending(r => r.CreatedAt)
            .Take(MaxExportRows)
            .Select(r => new
            {
                id = (object?)r.Id.Value,
                name = (object?)r.Name,
                report_type = (object?)r.ReportType,
                schedule = (object?)r.Schedule,
                format = (object?)r.Format,
                is_enabled = (object?)r.IsEnabled,
                created_at = (object?)r.CreatedAt.ToString("o")
            })
            .ToListAsync(cancellationToken);

        return rows.Select(r => BuildRow(new Dictionary<string, object?>
        {
            ["id"] = r.id,
            ["name"] = r.name,
            ["report_type"] = r.report_type,
            ["schedule"] = r.schedule,
            ["format"] = r.format,
            ["is_enabled"] = r.is_enabled,
            ["created_at"] = r.created_at
        }, columns)).ToList();
    }

    private static IReadOnlyDictionary<string, object?> BuildRow(
        Dictionary<string, object?> all,
        string[]? columns)
    {
        if (columns is null || columns.Length == 0)
            return all;

        var filtered = new Dictionary<string, object?>(columns.Length);
        foreach (var col in columns)
        {
            if (all.TryGetValue(col, out var val))
                filtered[col] = val;
        }

        return filtered;
    }
}
