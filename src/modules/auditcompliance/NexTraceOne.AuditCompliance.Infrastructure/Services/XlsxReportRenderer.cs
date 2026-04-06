using System.Text.Json;

using ClosedXML.Excel;

using NexTraceOne.AuditCompliance.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Abstractions;

namespace NexTraceOne.AuditCompliance.Infrastructure.Services;

/// <summary>
/// Renderiza relatórios de auditoria no formato XLSX usando ClosedXML.
///
/// A estrutura do relatório é achatada num formato tabular:
///   - Propriedades escalares → uma única sheet "Report" com chave/valor
///   - Arrays de objetos → sheets adicionais com colunas para cada propriedade
///
/// Persona: Auditor, Executive.
/// </summary>
public sealed class XlsxReportRenderer(IDateTimeProvider dateTimeProvider) : IReportRenderer
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = false,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <inheritdoc />
    public Task<RenderedReport> RenderAsync(
        object report,
        string format,
        CancellationToken cancellationToken = default)
    {
        var jsonElement = JsonSerializer.SerializeToElement(report, SerializerOptions);

        using var workbook = new XLWorkbook();

        // ── Main "Report" sheet with scalar properties ──
        var mainSheet = workbook.AddWorksheet("Report");
        StyleHeader(mainSheet, 1, "Property", "Value");

        var row = 2;
        var arrayProperties = new List<(string Name, JsonElement Element)>();

        if (jsonElement.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in jsonElement.EnumerateObject())
            {
                if (property.Value.ValueKind == JsonValueKind.Array)
                {
                    arrayProperties.Add((property.Name, property.Value));
                }
                else if (property.Value.ValueKind == JsonValueKind.Object)
                {
                    // Flatten nested objects with dot notation
                    FlattenObject(mainSheet, ref row, property.Name, property.Value);
                }
                else
                {
                    mainSheet.Cell(row, 1).Value = FormatKey(property.Name);
                    mainSheet.Cell(row, 2).Value = FormatValue(property.Value);
                    row++;
                }
            }
        }

        mainSheet.Columns().AdjustToContents(5.0, 60.0);

        // ── Additional sheets for array properties ──
        foreach (var (name, arrayElement) in arrayProperties)
        {
            var sheetName = SanitizeSheetName(FormatKey(name));
            var sheet = workbook.AddWorksheet(sheetName);
            RenderArraySheet(sheet, arrayElement);
        }

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        var bytes = stream.ToArray();

        var now = dateTimeProvider.UtcNow;

        return Task.FromResult(new RenderedReport(
            bytes,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"audit-report-{now:yyyyMMdd-HHmmss}.xlsx"));
    }

    private static void FlattenObject(
        IXLWorksheet sheet,
        ref int row,
        string prefix,
        JsonElement element)
    {
        foreach (var property in element.EnumerateObject())
        {
            var key = $"{prefix}.{property.Name}";

            if (property.Value.ValueKind == JsonValueKind.Object)
            {
                FlattenObject(sheet, ref row, key, property.Value);
            }
            else if (property.Value.ValueKind != JsonValueKind.Array)
            {
                sheet.Cell(row, 1).Value = FormatKey(key);
                sheet.Cell(row, 2).Value = FormatValue(property.Value);
                row++;
            }
        }
    }

    private static void RenderArraySheet(IXLWorksheet sheet, JsonElement arrayElement)
    {
        // Collect all distinct column names from array items
        var columns = new List<string>();
        var columnSet = new HashSet<string>();

        foreach (var item in arrayElement.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.Object) continue;
            foreach (var property in item.EnumerateObject())
            {
                if (columnSet.Add(property.Name))
                    columns.Add(property.Name);
            }
        }

        if (columns.Count == 0)
        {
            // Simple array — just render values in a single column
            StyleHeader(sheet, 1, "Value");
            var row = 2;
            foreach (var item in arrayElement.EnumerateArray())
            {
                sheet.Cell(row, 1).Value = FormatValue(item);
                row++;
            }
            sheet.Columns().AdjustToContents(5.0, 60.0);
            return;
        }

        // Header row
        for (var c = 0; c < columns.Count; c++)
        {
            var headerCell = sheet.Cell(1, c + 1);
            headerCell.Value = FormatKey(columns[c]);
            headerCell.Style.Font.Bold = true;
            headerCell.Style.Fill.BackgroundColor = XLColor.FromHtml("#1E3A5F");
            headerCell.Style.Font.FontColor = XLColor.White;
        }

        // Data rows
        var dataRow = 2;
        foreach (var item in arrayElement.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.Object) continue;
            for (var c = 0; c < columns.Count; c++)
            {
                if (item.TryGetProperty(columns[c], out var value))
                    sheet.Cell(dataRow, c + 1).Value = FormatValue(value);
            }
            dataRow++;
        }

        sheet.Columns().AdjustToContents(5.0, 60.0);
        sheet.SheetView.FreezeRows(1);
    }

    private static void StyleHeader(IXLWorksheet sheet, int row, params string[] headers)
    {
        for (var c = 0; c < headers.Length; c++)
        {
            var cell = sheet.Cell(row, c + 1);
            cell.Value = headers[c];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#1E3A5F");
            cell.Style.Font.FontColor = XLColor.White;
        }
        sheet.SheetView.FreezeRows(1);
    }

    private static string FormatKey(string key)
    {
        if (string.IsNullOrEmpty(key)) return key;
        var result = new System.Text.StringBuilder();
        result.Append(char.ToUpper(key[0], System.Globalization.CultureInfo.InvariantCulture));
        for (var i = 1; i < key.Length; i++)
        {
            if (char.IsUpper(key[i]) && !char.IsUpper(key[i - 1]))
                result.Append(' ');
            result.Append(key[i]);
        }
        return result.ToString();
    }

    private static string FormatValue(JsonElement el) => el.ValueKind switch
    {
        JsonValueKind.String => el.GetString() ?? "",
        JsonValueKind.Number => el.GetRawText(),
        JsonValueKind.True => "Yes",
        JsonValueKind.False => "No",
        JsonValueKind.Null => "—",
        _ => el.GetRawText()
    };

    private static string SanitizeSheetName(string name)
    {
        // Excel sheet names: max 31 chars, no special chars
        var sanitized = name
            .Replace("[", "").Replace("]", "")
            .Replace("*", "").Replace("?", "")
            .Replace("/", "-").Replace("\\", "-")
            .Replace(":", "-");
        return sanitized.Length > 31 ? sanitized[..31] : sanitized;
    }
}
