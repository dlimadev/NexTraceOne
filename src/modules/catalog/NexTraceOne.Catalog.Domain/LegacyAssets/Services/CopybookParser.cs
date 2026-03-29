using System.Globalization;
using System.Text.RegularExpressions;

namespace NexTraceOne.Catalog.Domain.LegacyAssets.Services;

/// <summary>
/// Parser de copybook COBOL — converte texto COBOL em campos estruturados.
/// Suporta Fase 1: PIC clauses (9, X, A, S, V, COMP, COMP-3), groups, OCCURS, REDEFINES, 88-levels.
/// Implementado como serviço de domínio puro (sem I/O, sem estado).
/// </summary>
public static class CopybookParser
{
    private static readonly Regex PicPattern = new(
        @"PIC\s+(.+?)(?:\s+COMP(?:-[1-5])?|\s+BINARY|\s+PACKED-DECIMAL|\s*\.|\s+OCCURS|\s+REDEFINES|\s*$)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex OccursPattern = new(
        @"OCCURS\s+(\d+)\s+TIMES?",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex RedefinesPattern = new(
        @"REDEFINES\s+([A-Za-z0-9_-]+)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex ValuePattern = new(
        @"VALUE\s+(.+?)(?:\s*\.|\s*$)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex UsagePattern = new(
        @"\b(COMP-[1-5]|COMP|BINARY|PACKED-DECIMAL)\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex PicExpandPattern = new(
        @"([9XASV])\((\d+)\)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    /// <summary>
    /// Faz o parse de texto COBOL de um copybook e retorna o layout estruturado.
    /// </summary>
    /// <param name="copybookText">Texto fonte do copybook COBOL.</param>
    /// <returns>Layout estruturado com campos, offsets e metadados.</returns>
    public static CopybookParsedLayout Parse(string copybookText)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(copybookText);

        var logicalLines = BuildLogicalLines(copybookText);
        var rawItems = ParseLogicalLines(logicalLines);
        var fields = ComputeOffsetsAndLengths(rawItems);

        var copybookName = rawItems.FirstOrDefault(r => r.Level == 1)?.Name ?? "UNKNOWN";
        var totalLength = ComputeTotalLength(fields);

        return new CopybookParsedLayout(copybookName, fields.AsReadOnly(), totalLength, "FB");
    }

    /// <summary>
    /// Constrói linhas lógicas a partir do texto raw COBOL, tratando continuações e comentários.
    /// Colunas COBOL: 1-6 (sequence), 7 (indicator), 8-72 (source area).
    /// </summary>
    private static List<string> BuildLogicalLines(string text)
    {
        var rawLines = text.Split('\n');
        var logicalLines = new List<string>();
        var currentLine = string.Empty;

        foreach (var rawLine in rawLines)
        {
            if (string.IsNullOrWhiteSpace(rawLine))
                continue;

            var line = rawLine.TrimEnd('\r');
            var fullTrimmed = line.Trim();

            // Linhas curtas: tratar como free-form
            if (line.Length < 7)
            {
                if (!string.IsNullOrEmpty(fullTrimmed) && !fullTrimmed.StartsWith('*'))
                {
                    if (!string.IsNullOrEmpty(currentLine))
                    {
                        logicalLines.Add(currentLine);
                        currentLine = string.Empty;
                    }
                    logicalLines.Add(fullTrimmed);
                }
                continue;
            }

            var indicator = line[6];

            // Linhas de comentário — fixed-format (indicador '*' ou '/' na col 7)
            if (indicator is '*' or '/')
                continue;

            // Comentário free-form: linha começa com '*' (após trim)
            if (fullTrimmed.StartsWith('*'))
                continue;

            // Continuação (indicador '-' na col 7)
            if (indicator == '-')
            {
                var contSource = line.Length > 7
                    ? (line.Length > 72 ? line[7..72] : line[7..])
                    : string.Empty;
                var continuation = contSource.TrimStart();
                if (continuation.StartsWith('\'') || continuation.StartsWith('"'))
                    continuation = continuation[1..];
                currentLine += continuation;
                continue;
            }

            // Extrair área de source (colunas 8-72 para fixed-format)
            var sourceArea = line.Length > 7
                ? (line.Length > 72 ? line[7..72] : line[7..])
                : string.Empty;
            sourceArea = sourceArea.TrimEnd();

            // Se a source area começa com nível COBOL, usar fixed-format.
            // Caso contrário, tentar free-form (linha inteira trimmed).
            var trimmedSource = sourceArea.TrimStart();
            string content;

            if (trimmedSource.Length > 0 && char.IsDigit(trimmedSource[0]))
            {
                content = trimmedSource;
            }
            else if (fullTrimmed.Length > 0 && char.IsDigit(fullTrimmed[0]))
            {
                content = fullTrimmed;
            }
            else
            {
                content = trimmedSource;
            }

            if (string.IsNullOrWhiteSpace(content))
                continue;

            // Linha normal: flush anterior e iniciar nova
            if (!string.IsNullOrEmpty(currentLine))
                logicalLines.Add(currentLine);

            currentLine = content;
        }

        if (!string.IsNullOrEmpty(currentLine))
            logicalLines.Add(currentLine);

        return logicalLines;
    }

    /// <summary>
    /// Extrai itens raw (nível, nome, clauses) de cada linha lógica COBOL.
    /// </summary>
    private static List<RawItem> ParseLogicalLines(List<string> logicalLines)
    {
        var items = new List<RawItem>();

        foreach (var line in logicalLines)
        {
            var trimmed = line.Trim();
            if (string.IsNullOrEmpty(trimmed))
                continue;

            // Remove trailing period
            if (trimmed.EndsWith('.'))
                trimmed = trimmed[..^1].TrimEnd();

            // Split into tokens
            var parts = trimmed.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2)
                continue;

            // First token must be a level number
            if (!int.TryParse(parts[0], CultureInfo.InvariantCulture, out var level))
                continue;

            // Skip RENAMES (66-level)
            if (level == 66)
                continue;

            var name = parts[1];
            var restOfLine = trimmed[(trimmed.IndexOf(name, StringComparison.Ordinal) + name.Length)..].Trim();

            // Extract clauses
            string? picClause = null;
            string? usage = null;
            int? occursCount = null;
            bool isRedefines = false;
            string? redefinesTarget = null;
            List<string>? conditionValues = null;

            // PIC clause
            var picMatch = PicPattern.Match(restOfLine);
            if (picMatch.Success)
                picClause = picMatch.Groups[1].Value.Trim().TrimEnd('.');

            // USAGE / COMP
            var usageMatch = UsagePattern.Match(restOfLine);
            if (usageMatch.Success)
                usage = usageMatch.Groups[1].Value.ToUpperInvariant();

            // OCCURS
            var occursMatch = OccursPattern.Match(restOfLine);
            if (occursMatch.Success)
                occursCount = int.Parse(occursMatch.Groups[1].Value, CultureInfo.InvariantCulture);

            // REDEFINES
            var redefinesMatch = RedefinesPattern.Match(restOfLine);
            if (redefinesMatch.Success)
            {
                isRedefines = true;
                redefinesTarget = redefinesMatch.Groups[1].Value;
            }

            // VALUE (for 88-level condition names)
            if (level == 88)
            {
                var valueMatch = ValuePattern.Match(restOfLine);
                if (valueMatch.Success)
                {
                    var valueText = valueMatch.Groups[1].Value.Trim().TrimEnd('.');
                    conditionValues = ParseConditionValues(valueText);
                }
            }

            items.Add(new RawItem(
                level, name, picClause, usage,
                occursCount, isRedefines, redefinesTarget, conditionValues));
        }

        return items;
    }

    /// <summary>
    /// Extrai valores de condições 88-level (VALUE 'A' ou VALUE 'A' 'B' 'C').
    /// </summary>
    private static List<string> ParseConditionValues(string valueText)
    {
        var values = new List<string>();
        var inQuote = false;
        var quoteChar = '\'';
        var current = string.Empty;

        foreach (var ch in valueText)
        {
            if (!inQuote && (ch == '\'' || ch == '"'))
            {
                inQuote = true;
                quoteChar = ch;
                current = string.Empty;
            }
            else if (inQuote && ch == quoteChar)
            {
                inQuote = false;
                values.Add(current);
            }
            else if (inQuote)
            {
                current += ch;
            }
        }

        // Handle unquoted values (numeric or keywords)
        if (values.Count == 0)
        {
            var parts = valueText.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            foreach (var part in parts)
            {
                if (!string.Equals(part, "THRU", StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(part, "THROUGH", StringComparison.OrdinalIgnoreCase))
                    values.Add(part.Trim('\'', '"'));
            }
        }

        return values;
    }

    /// <summary>
    /// Calcula offsets e comprimentos para todos os campos, tratando hierarquia de grupos,
    /// REDEFINES e OCCURS.
    /// </summary>
    private static List<CopybookParsedField> ComputeOffsetsAndLengths(List<RawItem> rawItems)
    {
        var fields = new List<CopybookParsedField>();
        // Stack: (index in fields, level) for tracking group hierarchy
        var groupStack = new Stack<(int FieldIndex, int Level)>();
        var currentOffset = 0;

        for (var i = 0; i < rawItems.Count; i++)
        {
            var item = rawItems[i];

            // 88-level: condition names, no physical storage
            if (item.Level == 88)
            {
                // Find parent field (last non-88 field)
                fields.Add(new CopybookParsedField(
                    Level: 88,
                    Name: item.Name,
                    PicClause: null,
                    DataType: "condition",
                    Offset: 0,
                    Length: 0,
                    DecimalPositions: null,
                    IsGroup: false,
                    OccursCount: null,
                    IsRedefines: false,
                    RedefinesTarget: null,
                    ConditionValues: item.ConditionValues?.AsReadOnly()));
                continue;
            }

            // Pop groups from stack that are at same or higher level
            while (groupStack.Count > 0 && groupStack.Peek().Level >= item.Level)
                groupStack.Pop();

            var isGroup = item.PicClause is null && item.Level is >= 1 and <= 49;
            var (dataType, length, decimalPositions) = isGroup
                ? ("group", 0, (int?)null)
                : ComputePicDetails(item.PicClause, item.Usage);

            var fieldOffset = item.IsRedefines
                ? FindRedefinesOffset(fields, item.RedefinesTarget)
                : currentOffset;

            var occursCount = item.OccursCount;
            var effectiveLength = occursCount.HasValue && !isGroup ? length * occursCount.Value : length;

            var field = new CopybookParsedField(
                Level: item.Level,
                Name: item.Name,
                PicClause: item.PicClause,
                DataType: dataType,
                Offset: fieldOffset,
                Length: length,
                DecimalPositions: decimalPositions,
                IsGroup: isGroup,
                OccursCount: occursCount,
                IsRedefines: item.IsRedefines,
                RedefinesTarget: item.RedefinesTarget,
                ConditionValues: null);

            fields.Add(field);

            if (isGroup)
            {
                groupStack.Push((fields.Count - 1, item.Level));
            }
            else if (!item.IsRedefines)
            {
                currentOffset += effectiveLength;
            }
        }

        // Second pass: compute group lengths from children
        ComputeGroupLengths(fields);

        return fields;
    }

    /// <summary>
    /// Calcula o offset de um campo REDEFINES procurando o campo alvo na lista.
    /// </summary>
    private static int FindRedefinesOffset(List<CopybookParsedField> fields, string? targetName)
    {
        if (targetName is null)
            return 0;

        for (var i = fields.Count - 1; i >= 0; i--)
        {
            if (string.Equals(fields[i].Name, targetName, StringComparison.OrdinalIgnoreCase))
                return fields[i].Offset;
        }

        return 0;
    }

    /// <summary>
    /// Segunda passagem: calcula comprimento de campos grupo somando os filhos.
    /// </summary>
    private static void ComputeGroupLengths(List<CopybookParsedField> fields)
    {
        for (var i = fields.Count - 1; i >= 0; i--)
        {
            if (!fields[i].IsGroup)
                continue;

            var groupLevel = fields[i].Level;
            var groupLength = 0;

            for (var j = i + 1; j < fields.Count; j++)
            {
                var child = fields[j];

                // Stop at same or lower level (end of group)
                if (child.Level <= groupLevel && child.Level != 88)
                    break;

                // Skip 88-level conditions
                if (child.Level == 88)
                    continue;

                // Skip REDEFINES fields (they overlay, don't add)
                if (child.IsRedefines)
                    continue;

                // Only count immediate children (direct or nested)
                if (!child.IsGroup)
                {
                    var childLength = child.OccursCount.HasValue
                        ? child.Length * child.OccursCount.Value
                        : child.Length;
                    groupLength += childLength;
                }
            }

            // Apply OCCURS at group level
            var effectiveLength = fields[i].OccursCount.HasValue
                ? groupLength * fields[i].OccursCount.Value
                : groupLength;

            fields[i] = fields[i] with { Length = effectiveLength };
        }
    }

    /// <summary>
    /// Interpreta a PIC clause e o USAGE para determinar o tipo de dados, comprimento em bytes
    /// e posições decimais.
    /// </summary>
    private static (string DataType, int Length, int? DecimalPositions) ComputePicDetails(
        string? picClause, string? usage)
    {
        if (picClause is null)
            return ("unknown", 0, null);

        // Expand PIC shorthand: X(10) → XXXXXXXXXX for counting
        var expanded = ExpandPic(picClause);

        var intDigits = CountChar(expanded, '9');
        var alphaCount = CountChar(expanded, 'X');
        var alphabeticCount = CountChar(expanded, 'A');
        var hasSigned = expanded.Contains('S', StringComparison.OrdinalIgnoreCase);
        var hasDecimal = expanded.Contains('V', StringComparison.OrdinalIgnoreCase);
        var decimalDigits = hasDecimal ? CountDigitsAfterV(expanded) : 0;
        // intDigits already includes ALL 9s (before and after V), so totalDigits = intDigits
        var totalDigits = intDigits;

        // COMP-1 / COMP-2: fixed sizes regardless of PIC
        if (string.Equals(usage, "COMP-1", StringComparison.OrdinalIgnoreCase))
            return ("float", 4, null);

        if (string.Equals(usage, "COMP-2", StringComparison.OrdinalIgnoreCase))
            return ("double", 8, null);

        // COMP-3 / PACKED-DECIMAL
        if (usage is not null &&
            (usage.Equals("COMP-3", StringComparison.OrdinalIgnoreCase) ||
             usage.Equals("PACKED-DECIMAL", StringComparison.OrdinalIgnoreCase)))
        {
            var packedLen = (totalDigits + 1 + 1) / 2; // +1 for sign nibble, then rounded up
            var type = hasDecimal ? "signed-decimal" : hasSigned ? "signed-numeric" : "packed-decimal";
            return (type, packedLen, hasDecimal ? decimalDigits : null);
        }

        // COMP / COMP-4 / BINARY
        if (usage is not null &&
            (usage.Equals("COMP", StringComparison.OrdinalIgnoreCase) ||
             usage.Equals("COMP-4", StringComparison.OrdinalIgnoreCase) ||
             usage.Equals("COMP-5", StringComparison.OrdinalIgnoreCase) ||
             usage.Equals("BINARY", StringComparison.OrdinalIgnoreCase)))
        {
            var binaryLen = totalDigits switch
            {
                <= 4 => 2,
                <= 9 => 4,
                _ => 8
            };
            var type = hasDecimal ? "binary-decimal" : hasSigned ? "binary-signed" : "binary";
            return (type, binaryLen, hasDecimal ? decimalDigits : null);
        }

        // Alphanumeric: PIC X
        if (alphaCount > 0 && intDigits == 0)
            return ("alphanumeric", alphaCount, null);

        // Alphabetic: PIC A
        if (alphabeticCount > 0 && intDigits == 0)
            return ("alphabetic", alphabeticCount, null);

        // Display numeric — intDigits already includes decimal digits
        var displayLen = intDigits + (hasSigned ? 1 : 0);

        if (hasDecimal && hasSigned)
            return ("signed-decimal", displayLen, decimalDigits);
        if (hasDecimal)
            return ("decimal", displayLen, decimalDigits);
        if (hasSigned)
            return ("signed-numeric", displayLen, null);

        return ("numeric", displayLen, null);
    }

    /// <summary>
    /// Expande notação PIC abreviada: 9(5) → 99999, X(10) → XXXXXXXXXX.
    /// </summary>
    private static string ExpandPic(string pic)
    {
        return PicExpandPattern.Replace(pic, match =>
        {
            var ch = match.Groups[1].Value;
            var count = int.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture);
            return new string(ch[0], count);
        });
    }

    private static int CountChar(string expanded, char target)
    {
        var count = 0;
        foreach (var ch in expanded)
        {
            if (char.ToUpperInvariant(ch) == target)
                count++;
        }
        return count;
    }

    /// <summary>
    /// Conta dígitos decimais após o ponto decimal implícito (V) na PIC expandida.
    /// </summary>
    private static int CountDigitsAfterV(string expanded)
    {
        var afterV = false;
        var count = 0;
        foreach (var ch in expanded)
        {
            if (char.ToUpperInvariant(ch) == 'V')
            {
                afterV = true;
                continue;
            }
            if (afterV && char.ToUpperInvariant(ch) == '9')
                count++;
        }
        return count;
    }

    /// <summary>
    /// Calcula o comprimento total do registo somando campos de primeiro nível (excluindo REDEFINES e 88).
    /// </summary>
    private static int ComputeTotalLength(List<CopybookParsedField> fields)
    {
        var total = 0;
        var rootLevel = fields.FirstOrDefault(f => f.Level != 88 && !f.IsRedefines)?.Level ?? 1;

        foreach (var field in fields)
        {
            if (field.Level == 88 || field.IsRedefines)
                continue;

            // Only count fields at the root children level or standalone (77)
            if (field.Level == rootLevel)
            {
                total = field.Offset + field.Length;
            }
        }

        return total;
    }

    /// <summary>
    /// Item intermédio do parsing antes do cálculo de offsets e comprimentos.
    /// </summary>
    private sealed record RawItem(
        int Level,
        string Name,
        string? PicClause,
        string? Usage,
        int? OccursCount,
        bool IsRedefines,
        string? RedefinesTarget,
        List<string>? ConditionValues);
}
