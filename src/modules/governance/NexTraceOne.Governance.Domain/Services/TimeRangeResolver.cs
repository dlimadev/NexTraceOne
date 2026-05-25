using System.Globalization;
using System.Text.RegularExpressions;

namespace NexTraceOne.Governance.Domain.Services;

/// <summary>
/// Resolve expressões de intervalo de tempo no estilo Grafana/Kibana.
/// Suporta:
/// - Relativas simples: "1h", "6h", "24h", "7d", "30d", "90d"
/// - Expressões Grafana-like: "now-1h", "now-6h", "now-7d", "now-30d"
/// - Snapping: "now/d" (início do dia), "now/w" (início da semana), "now/M" (início do mês)
/// - Combinações: "now-1h/d", "now-7d/w"
/// - Absolutas: "abs:ISO|ISO"
/// - Palavras-chave: "today", "yesterday", "week", "month"
/// </summary>
public static class TimeRangeResolver
{
    private static readonly Regex RelativeRegex = new(
        @"^now(?<offset>[+-]\d+[hdwMy])?(?<snap>/[hdwMy])?$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    /// <summary>
    /// Resolve uma expressão de time range em um par de DateTimeOffset (from, to).
    /// </summary>
    public static (DateTimeOffset from, DateTimeOffset to) Resolve(string? expression, DateTimeOffset? now = null)
    {
        var anchor = now ?? DateTimeOffset.UtcNow;

        if (string.IsNullOrWhiteSpace(expression))
            return (anchor.AddHours(-24), anchor);

        // Absolute format: abs:ISO|ISO
        if (expression.StartsWith("abs:", StringComparison.OrdinalIgnoreCase))
        {
            var parts = expression[4..].Split('|', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 2 &&
                DateTimeOffset.TryParse(parts[0], CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var from) &&
                DateTimeOffset.TryParse(parts[1], CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var to))
            {
                return (from, to);
            }
            return (anchor.AddHours(-24), anchor);
        }

        // Simple relative: "1h", "6h", "24h", "7d", "30d", "90d"
        var simpleMatch = Regex.Match(expression, @"^(\d+)([hd])$", RegexOptions.IgnoreCase);
        if (simpleMatch.Success)
        {
            var amount = int.Parse(simpleMatch.Groups[1].Value);
            var unit = simpleMatch.Groups[2].Value.ToLowerInvariant();
            var from = unit == "h" ? anchor.AddHours(-amount) : anchor.AddDays(-amount);
            return (from, anchor);
        }

        // Keyword aliases
        var (keyFrom, keyTo) = ResolveKeyword(expression, anchor);
        if (keyFrom.HasValue && keyTo.HasValue)
            return (keyFrom.Value, keyTo.Value);

        // Grafana-like: now-1h, now-7d, now/d, now-1h/d
        var match = RelativeRegex.Match(expression);
        if (match.Success)
        {
            var offsetGroup = match.Groups["offset"];
            var snapGroup = match.Groups["snap"];

            var result = anchor;

            // Apply offset first
            if (offsetGroup.Success)
            {
                var offsetStr = offsetGroup.Value; // e.g., "-1h" or "+1d"
                result = ApplyOffset(result, offsetStr);
            }

            // Apply snapping
            if (snapGroup.Success)
            {
                var snapUnit = snapGroup.Value[1..]; // remove leading '/'
                result = SnapTo(result, snapUnit);
            }

            // For "now" without offset, from=result (now) and we need a default range
            // For expressions like "now-1h", from=result, to=anchor
            if (offsetGroup.Success)
            {
                return (result, anchor);
            }

            // Just "now" or "now/d" — default to last 24h from anchor
            return (anchor.AddHours(-24), anchor);
        }

        // Fallback
        return (anchor.AddHours(-24), anchor);
    }

    /// <summary>
    /// Formata um par (from, to) para a representação absoluta usada na URL/API.
    /// </summary>
    public static string ToAbsoluteExpression(DateTimeOffset from, DateTimeOffset to)
        => $"abs:{from:O}|{to:O}";

    /// <summary>
    /// Retorna um rótulo legível para a expressão.
    /// </summary>
    public static string GetDisplayLabel(string? expression)
    {
        if (string.IsNullOrWhiteSpace(expression))
            return "Last 24 hours";

        if (expression.StartsWith("abs:", StringComparison.OrdinalIgnoreCase))
        {
            var (_, to) = Resolve(expression);
            return to.ToString("MMM dd, HH:mm", CultureInfo.CurrentCulture);
        }

        return expression.ToLowerInvariant() switch
        {
            "1h" or "now-1h" => "Last 1 hour",
            "6h" or "now-6h" => "Last 6 hours",
            "24h" or "now-24h" => "Last 24 hours",
            "7d" or "now-7d" => "Last 7 days",
            "30d" or "now-30d" => "Last 30 days",
            "90d" or "now-90d" => "Last 90 days",
            "today" or "now/d" => "Today",
            "yesterday" or "now-1d/d" => "Yesterday",
            "week" or "now/w" => "This week",
            "month" or "now/M" => "This month",
            _ => expression
        };
    }

    /// <summary>
    /// Lista todas as quick ranges disponíveis para UI.
    /// </summary>
    public static IReadOnlyList<(string value, string label)> QuickRanges =>
    [
        ("now-1h", "Last 1 hour"),
        ("now-6h", "Last 6 hours"),
        ("now-24h", "Last 24 hours"),
        ("now-7d", "Last 7 days"),
        ("now-30d", "Last 30 days"),
        ("now-90d", "Last 90 days"),
        ("today", "Today"),
        ("yesterday", "Yesterday"),
        ("week", "This week"),
        ("month", "This month"),
    ];

    private static DateTimeOffset ApplyOffset(DateTimeOffset baseTime, string offset)
    {
        var sign = offset[0] == '+' ? 1 : -1;
        var amount = int.Parse(offset[1..^1]);
        var unit = offset[^1];

        return unit switch
        {
            'h' or 'H' => baseTime.AddHours(sign * amount),
            'd' or 'D' => baseTime.AddDays(sign * amount),
            'w' or 'W' => baseTime.AddDays(sign * amount * 7),
            'M' => baseTime.AddMonths(sign * amount),
            'y' or 'Y' => baseTime.AddYears(sign * amount),
            _ => baseTime
        };
    }

    private static DateTimeOffset SnapTo(DateTimeOffset time, string unit)
    {
        var local = time.DateTime;
        return unit.ToLowerInvariant() switch
        {
            "h" => new DateTimeOffset(local.Year, local.Month, local.Day, local.Hour, 0, 0, time.Offset),
            "d" => new DateTimeOffset(local.Date, time.Offset),
            "w" => new DateTimeOffset(local.Date.AddDays(-(int)local.DayOfWeek), time.Offset),
            "m" => new DateTimeOffset(local.Year, local.Month, 1, 0, 0, 0, time.Offset),
            "y" => new DateTimeOffset(local.Year, 1, 1, 0, 0, 0, time.Offset),
            _ => time
        };
    }

    private static (DateTimeOffset? from, DateTimeOffset? to) ResolveKeyword(string expression, DateTimeOffset now)
    {
        var local = now.DateTime;
        var date = local.Date;

        return expression.ToLowerInvariant() switch
        {
            "today" => (
                new DateTimeOffset(date, now.Offset),
                new DateTimeOffset(date.AddDays(1).AddTicks(-1), now.Offset)),
            "yesterday" => (
                new DateTimeOffset(date.AddDays(-1), now.Offset),
                new DateTimeOffset(date.AddTicks(-1), now.Offset)),
            "week" => (
                new DateTimeOffset(date.AddDays(-(int)date.DayOfWeek), now.Offset),
                new DateTimeOffset(date.AddDays(7 - (int)date.DayOfWeek).AddTicks(-1), now.Offset)),
            "month" => (
                new DateTimeOffset(new DateTime(date.Year, date.Month, 1), now.Offset),
                new DateTimeOffset(new DateTime(date.Year, date.Month, 1).AddMonths(1).AddTicks(-1), now.Offset)),
            _ => (null, null)
        };
    }
}
