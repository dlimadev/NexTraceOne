using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.Configuration.Application.Features.ExportData;

/// <summary>
/// Feature: ExportData — exporta dados de uma entidade suportada nos formatos CSV ou JSON.
/// Estrutura VSA: Command + Validator + Handler + Result em um único arquivo.
/// </summary>
public static class ExportData
{
    /// <summary>Comando para exportar dados de uma entidade.</summary>
    public sealed record Command(
        string Entity,
        string Format,
        string[]? Columns) : ICommand<ExportResult>;

    /// <summary>Valida o comando de exportação de dados.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        private static readonly string[] SupportedEntities = ["contracts", "scheduled_reports", "audit_events"];
        private static readonly string[] SupportedFormats = ["csv", "json"];

        public Validator()
        {
            RuleFor(x => x.Entity)
                .NotEmpty()
                .Must(e => SupportedEntities.Contains(e, StringComparer.OrdinalIgnoreCase))
                .WithMessage("Entity must be one of: contracts, scheduled_reports, audit_events.");

            RuleFor(x => x.Format)
                .NotEmpty()
                .Must(f => SupportedFormats.Contains(f, StringComparer.OrdinalIgnoreCase))
                .WithMessage("Format must be one of: csv, json.");
        }
    }

    /// <summary>Handler que gera o ficheiro de exportação no formato solicitado.</summary>
    public sealed class Handler(IExportDataRepository repository) : ICommandHandler<Command, ExportResult>
    {
        public async Task<Result<ExportResult>> Handle(Command request, CancellationToken cancellationToken)
        {
            var rows = await repository.GetExportRowsAsync(request.Entity, request.Columns, cancellationToken);

            byte[] content;
            string contentType;
            string fileName = $"{request.Entity}_{DateTimeOffset.UtcNow:yyyyMMdd_HHmmss}";

            if (request.Format.Equals("json", StringComparison.OrdinalIgnoreCase))
            {
                content = System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(rows);
                contentType = "application/json";
                fileName += ".json";
            }
            else
            {
                content = BuildCsv(rows);
                contentType = "text/csv";
                fileName += ".csv";
            }

            return Result<ExportResult>.Success(new ExportResult(content, contentType, fileName));
        }

        private static byte[] BuildCsv(IReadOnlyList<IReadOnlyDictionary<string, object?>> rows)
        {
            if (rows.Count == 0) return System.Text.Encoding.UTF8.GetBytes(string.Empty);

            var sb = new System.Text.StringBuilder();
            var headers = rows[0].Keys.ToList();
            sb.AppendLine(string.Join(",", headers.Select(EscapeCsv)));

            foreach (var row in rows)
            {
                sb.AppendLine(string.Join(",", headers.Select(h => EscapeCsv(row.TryGetValue(h, out var v) ? v?.ToString() ?? "" : ""))));
            }

            return System.Text.Encoding.UTF8.GetBytes(sb.ToString());
        }

        private static string EscapeCsv(string? value)
        {
            if (value is null) return "\"\"";
            value = value.Replace("\"", "\"\"");
            return value.Contains(',') || value.Contains('"') || value.Contains('\n') ? $"\"{value}\"" : value;
        }
    }

    /// <summary>Resultado da exportação contendo o conteúdo binário, tipo e nome do ficheiro.</summary>
    public sealed record ExportResult(byte[] Content, string ContentType, string FileName);
}
