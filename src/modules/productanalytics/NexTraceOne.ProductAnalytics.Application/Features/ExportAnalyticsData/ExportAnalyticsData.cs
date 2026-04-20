using System.Text;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Configuration.Application.Abstractions;
using NexTraceOne.Configuration.Domain.Enums;
using NexTraceOne.ProductAnalytics.Application.Abstractions;
using NexTraceOne.ProductAnalytics.Application.ConfigurationKeys;
using NexTraceOne.ProductAnalytics.Application.Constants;
using NexTraceOne.ProductAnalytics.Domain.Enums;

namespace NexTraceOne.ProductAnalytics.Application.Features.ExportAnalyticsData;

/// <summary>
/// Exporta dados de analytics num formato estruturado (CSV ou JSON).
/// Resposta síncrona para conjuntos de dados até MaxExportRows (configurável).
/// Para volumes maiores, a resposta inclui <c>IsTruncated = true</c> como indicação.
/// </summary>
public static class ExportAnalyticsData
{
    /// <summary>Formatos de exportação suportados.</summary>
    public enum ExportFormat
    {
        /// <summary>Comma-separated values — adequado para Excel/BI tools.</summary>
        Csv,
        /// <summary>JSON estruturado — adequado para integrações programáticas.</summary>
        Json
    }

    /// <summary>Tipos de dados exportáveis.</summary>
    public enum ExportDataType
    {
        /// <summary>Eventos individuais de analytics da sessão.</summary>
        Events,
        /// <summary>Resumo consolidado de métricas no período.</summary>
        Summary
    }

    /// <summary>Query para exportação de dados de analytics.</summary>
    public sealed record Query(
        ExportDataType DataType,
        ExportFormat Format,
        string? Persona,
        string? Module,
        string? TeamId,
        string? Range) : IQuery<Response>;

    /// <summary>Handler que gera o ficheiro de exportação a partir dos dados reais.</summary>
    public sealed class Handler(
        IAnalyticsEventRepository repository,
        IDateTimeProvider clock,
        IConfigurationResolutionService configService) : IQueryHandler<Query, Response>
    {
        private const int DefaultMaxExportRows = 10_000;

        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            var maxRangeCfg = await configService.ResolveEffectiveValueAsync(AnalyticsConfigKeys.MaxRangeDays, ConfigurationScope.System, null, cancellationToken);
            var maxRangeDays = int.TryParse(maxRangeCfg?.EffectiveValue, out var mrd) ? mrd : AnalyticsConstants.MaxRangeDays;

            var (from, to, periodLabel) = ResolveRange(clock.UtcNow, request.Range, maxRangeDays);

            var moduleFilter = request.Module is not null &&
                Enum.TryParse<ProductModule>(request.Module, true, out var parsedModule)
                ? parsedModule
                : (ProductModule?)null;

            return request.DataType switch
            {
                ExportDataType.Events => await ExportEventsAsync(request, from, to, periodLabel, moduleFilter, cancellationToken),
                ExportDataType.Summary => await ExportSummaryAsync(request, from, to, periodLabel, cancellationToken),
                _ => Error.Validation("analytics.export.unknown_type", "Unknown export data type.")
            };
        }

        private async Task<Result<Response>> ExportEventsAsync(
            Query request,
            DateTimeOffset from,
            DateTimeOffset to,
            string periodLabel,
            ProductModule? moduleFilter,
            CancellationToken cancellationToken)
        {
            var rows = await repository.ListSessionEventsAsync(
                request.Persona, request.TeamId, from, to, cancellationToken);

            var filtered = moduleFilter.HasValue
                ? rows.Where(r => r.EventType.ToString().Contains(moduleFilter.Value.ToString(), StringComparison.OrdinalIgnoreCase)).ToList()
                : rows.ToList();

            var isTruncated = filtered.Count > DefaultMaxExportRows;
            var exportRows = isTruncated ? filtered.Take(DefaultMaxExportRows).ToList() : filtered;

            var (content, contentType, fileName) = request.Format switch
            {
                ExportFormat.Csv => BuildEventsCsv(exportRows, periodLabel),
                _ => BuildEventsJson(exportRows, periodLabel)
            };

            return Result<Response>.Success(new Response(
                Content: content,
                ContentType: contentType,
                FileName: fileName,
                RowCount: exportRows.Count,
                TotalAvailable: filtered.Count,
                IsTruncated: isTruncated,
                PeriodLabel: periodLabel));
        }

        private async Task<Result<Response>> ExportSummaryAsync(
            Query request,
            DateTimeOffset from,
            DateTimeOffset to,
            string periodLabel,
            CancellationToken cancellationToken)
        {
            var totalEvents = await repository.CountAsync(
                request.Persona, null, request.TeamId, null, from, to, cancellationToken);
            var uniqueUsers = await repository.CountUniqueUsersAsync(
                request.Persona, null, request.TeamId, null, from, to, cancellationToken);
            var topModules = await repository.GetTopModulesAsync(
                request.Persona, request.TeamId, null, from, to, AnalyticsConstants.TopModulesLimit, cancellationToken);

            var valueEventCount = 0L;
            foreach (var et in AnalyticsConstants.ValueEventTypes)
                valueEventCount += await repository.CountByEventTypeAsync(et, request.Persona, from, to, cancellationToken);

            var frictionEventCount = 0L;
            foreach (var et in AnalyticsConstants.FrictionEventTypes)
                frictionEventCount += await repository.CountByEventTypeAsync(et, request.Persona, from, to, cancellationToken);

            var valueScore = totalEvents > 0 ? Math.Round((valueEventCount / (decimal)totalEvents) * 100m, 2) : 0m;
            var frictionScore = totalEvents > 0 ? Math.Round((frictionEventCount / (decimal)totalEvents) * 100m, 2) : 0m;

            var (content, contentType, fileName) = request.Format switch
            {
                ExportFormat.Csv => BuildSummaryCsv(totalEvents, uniqueUsers, valueScore, frictionScore, topModules, periodLabel),
                _ => BuildSummaryJson(totalEvents, uniqueUsers, valueScore, frictionScore, topModules, periodLabel)
            };

            return Result<Response>.Success(new Response(
                Content: content,
                ContentType: contentType,
                FileName: fileName,
                RowCount: 1,
                TotalAvailable: 1,
                IsTruncated: false,
                PeriodLabel: periodLabel));
        }

        private static (string Content, string ContentType, string FileName) BuildEventsCsv(
            IReadOnlyList<SessionEventRow> rows, string periodLabel)
        {
            var sb = new StringBuilder();
            sb.AppendLine("session_id,event_type,occurred_at");
            foreach (var r in rows)
                sb.AppendLine($"{EscapeCsvField(r.SessionId)},{r.EventType},{r.OccurredAt:O}");

            return (sb.ToString(), "text/csv", $"analytics_events_{periodLabel}_{DateTimeOffset.UtcNow:yyyyMMdd}.csv");
        }

        private static (string Content, string ContentType, string FileName) BuildEventsJson(
            IReadOnlyList<SessionEventRow> rows, string periodLabel)
        {
            var sb = new StringBuilder();
            sb.Append('[');
            for (var i = 0; i < rows.Count; i++)
            {
                if (i > 0) sb.Append(',');
                var r = rows[i];
                sb.Append($"{{\"sessionId\":\"{r.SessionId}\",\"eventType\":\"{r.EventType}\",\"occurredAt\":\"{r.OccurredAt:O}\"}}");
            }
            sb.Append(']');
            return (sb.ToString(), "application/json", $"analytics_events_{periodLabel}_{DateTimeOffset.UtcNow:yyyyMMdd}.json");
        }

        private static (string Content, string ContentType, string FileName) BuildSummaryCsv(
            long totalEvents, int uniqueUsers, decimal valueScore, decimal frictionScore,
            IReadOnlyList<ModuleUsageRow> topModules, string periodLabel)
        {
            var sb = new StringBuilder();
            sb.AppendLine("metric,value");
            sb.AppendLine($"total_events,{totalEvents}");
            sb.AppendLine($"unique_users,{uniqueUsers}");
            sb.AppendLine($"value_score_percent,{valueScore}");
            sb.AppendLine($"friction_score_percent,{frictionScore}");
            sb.AppendLine($"period,{periodLabel}");
            foreach (var m in topModules)
                sb.AppendLine($"top_module_{m.Module},{m.EventCount}");

            return (sb.ToString(), "text/csv", $"analytics_summary_{periodLabel}_{DateTimeOffset.UtcNow:yyyyMMdd}.csv");
        }

        private static (string Content, string ContentType, string FileName) BuildSummaryJson(
            long totalEvents, int uniqueUsers, decimal valueScore, decimal frictionScore,
            IReadOnlyList<ModuleUsageRow> topModules, string periodLabel)
        {
            var topModulesJson = string.Join(",", topModules.Select(m =>
                $"{{\"module\":\"{m.Module}\",\"eventCount\":{m.EventCount},\"uniqueUsers\":{m.UniqueUsers}}}"));

            var content = $"{{" +
                $"\"totalEvents\":{totalEvents}," +
                $"\"uniqueUsers\":{uniqueUsers}," +
                $"\"valueScorePercent\":{valueScore}," +
                $"\"frictionScorePercent\":{frictionScore}," +
                $"\"periodLabel\":\"{periodLabel}\"," +
                $"\"topModules\":[{topModulesJson}]" +
                $"}}";

            return (content, "application/json", $"analytics_summary_{periodLabel}_{DateTimeOffset.UtcNow:yyyyMMdd}.json");
        }

        private static string EscapeCsvField(string value)
        {
            if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
                return $"\"{value.Replace("\"", "\"\"")}\"";
            return value;
        }

        private static (DateTimeOffset From, DateTimeOffset To, string Label) ResolveRange(DateTimeOffset utcNow, string? range, int maxDays = AnalyticsConstants.MaxRangeDays)
        {
            var label = string.IsNullOrWhiteSpace(range) ? "last_30d" : range;
            var days = label switch
            {
                "last_7d" => 7,
                "last_1d" => 1,
                "last_90d" => 90,
                _ => 30
            };
            if (days > maxDays) days = maxDays;
            return (utcNow.AddDays(-days), utcNow, label);
        }
    }

    /// <summary>Resposta da exportação de dados de analytics.</summary>
    public sealed record Response(
        /// <summary>Conteúdo do ficheiro em formato texto (CSV ou JSON).</summary>
        string Content,
        /// <summary>Content-Type HTTP do ficheiro gerado.</summary>
        string ContentType,
        /// <summary>Nome sugerido para o ficheiro de exportação.</summary>
        string FileName,
        /// <summary>Número de linhas exportadas nesta resposta.</summary>
        int RowCount,
        /// <summary>Total de registos disponíveis antes de truncagem.</summary>
        int TotalAvailable,
        /// <summary>Indica se os dados foram truncados por atingir o limite máximo.</summary>
        bool IsTruncated,
        /// <summary>Período coberto pelos dados exportados.</summary>
        string PeriodLabel);
}
