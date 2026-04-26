namespace NexTraceOne.BuildingBlocks.Application.Nql;

/// <summary>
/// Parser da NexTraceOne Query Language (NQL).
///
/// Gramática suportada (case-insensitive, sem dependências externas):
/// <code>
///   FROM &lt;module.entity&gt;
///   [WHERE &lt;field&gt; &lt;op&gt; '&lt;value&gt;' [AND &lt;field&gt; &lt;op&gt; '&lt;value&gt;']*]
///   [GROUP BY &lt;field&gt;[, &lt;field&gt;]*]
///   [ORDER BY &lt;field&gt; [ASC|DESC]]
///   [LIMIT &lt;n&gt;]
///   [RENDER AS &lt;hint&gt;]
/// </code>
/// Operadores: = != &gt; &lt; &gt;= &lt;= LIKE
///
/// Exemplos:
///   FROM catalog.services WHERE tier = 'Critical' ORDER BY name ASC LIMIT 50
///   FROM operations.incidents WHERE status = 'open' GROUP BY service ORDER BY count DESC LIMIT 20
/// </summary>
public static class NqlParser
{
    private const int DefaultLimit = 100;
    private const int MaxLimit = 1000;

    /// <summary>Analisa a query e retorna um <see cref="NqlValidationResult"/>.</summary>
    public static NqlValidationResult Parse(string? query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return NqlValidationResult.Fail("Query cannot be empty.");

        var tokens = Tokenize(query);
        if (tokens.Count == 0)
            return NqlValidationResult.Fail("Query is empty after tokenization.");

        return ParseTokens(tokens);
    }

    // ─── Tokenizer ──────────────────────────────────────────────────────────

    private static List<string> Tokenize(string query)
    {
        var tokens = new List<string>();
        var i = 0;
        var span = query.AsSpan();

        while (i < span.Length)
        {
            // Skip whitespace
            if (char.IsWhiteSpace(span[i])) { i++; continue; }

            // Quoted string
            if (span[i] == '\'' || span[i] == '"')
            {
                var quote = span[i];
                i++;
                var start = i;
                while (i < span.Length && span[i] != quote) i++;
                tokens.Add(span[start..i].ToString());
                if (i < span.Length) i++; // skip closing quote
                continue;
            }

            // Two-char operators
            if (i + 1 < span.Length)
            {
                var two = span.Slice(i, 2).ToString();
                if (two is ">=" or "<=" or "!=")
                {
                    tokens.Add(two);
                    i += 2;
                    continue;
                }
            }

            // Single-char operators / punctuation
            if (span[i] is '>' or '<' or '=' or ',')
            {
                tokens.Add(span[i].ToString());
                i++;
                continue;
            }

            // Word / identifier (including module.entity dots)
            var wordStart = i;
            while (i < span.Length && !char.IsWhiteSpace(span[i]) && span[i] is not '\'' and not '"' and not ',' and not '>' and not '<' and not '=')
                i++;
            var word = span[wordStart..i].ToString();
            if (!string.IsNullOrEmpty(word))
                tokens.Add(word);
        }

        return tokens;
    }

    // ─── Parser ─────────────────────────────────────────────────────────────

    private static NqlValidationResult ParseTokens(List<string> tokens)
    {
        var pos = 0;

        // FROM <entity>
        if (!Consume(tokens, ref pos, "FROM"))
            return NqlValidationResult.Fail("Query must start with FROM.");

        if (!TryPeek(tokens, pos, out var entityStr))
            return NqlValidationResult.Fail("Expected entity after FROM (e.g. catalog.services).");

        pos++;
        if (!NqlEntityMap.TryParse(entityStr, out var entity))
            return NqlValidationResult.Fail(
                $"Unknown entity '{entityStr}'. Valid sources: {string.Join(", ", NqlEntityMap.ValidSources)}.");

        // WHERE
        var filters = new List<NqlFilter>();
        if (ConsumeKeyword(tokens, ref pos, "WHERE"))
        {
            var filterResult = ParseFilters(tokens, ref pos);
            if (filterResult is null)
                return NqlValidationResult.Fail("Invalid WHERE clause.");
            filters.AddRange(filterResult);
        }

        // GROUP BY
        var groupBy = new List<string>();
        if (ConsumeKeyword(tokens, ref pos, "GROUP"))
        {
            if (!ConsumeKeyword(tokens, ref pos, "BY"))
                return NqlValidationResult.Fail("Expected BY after GROUP.");
            ParseCommaSeparatedFields(tokens, ref pos, groupBy);
        }

        // ORDER BY
        NqlOrderBy? orderBy = null;
        if (ConsumeKeyword(tokens, ref pos, "ORDER"))
        {
            if (!ConsumeKeyword(tokens, ref pos, "BY"))
                return NqlValidationResult.Fail("Expected BY after ORDER.");
            if (!TryPeek(tokens, pos, out var orderField))
                return NqlValidationResult.Fail("Expected field after ORDER BY.");
            pos++;
            var direction = NqlSortDirection.Asc;
            if (ConsumeKeyword(tokens, ref pos, "DESC"))
                direction = NqlSortDirection.Desc;
            else
                ConsumeKeyword(tokens, ref pos, "ASC");
            orderBy = new NqlOrderBy(orderField, direction);
        }

        // LIMIT
        var limit = DefaultLimit;
        if (ConsumeKeyword(tokens, ref pos, "LIMIT"))
        {
            if (!TryPeek(tokens, pos, out var limitStr) || !int.TryParse(limitStr, out var parsedLimit))
                return NqlValidationResult.Fail("Expected integer after LIMIT.");
            pos++;
            if (parsedLimit < 1 || parsedLimit > MaxLimit)
                return NqlValidationResult.Fail($"LIMIT must be between 1 and {MaxLimit}.");
            limit = parsedLimit;
        }

        // RENDER AS <hint>
        string? renderHint = null;
        if (ConsumeKeyword(tokens, ref pos, "RENDER"))
        {
            ConsumeKeyword(tokens, ref pos, "AS");
            if (TryPeek(tokens, pos, out var hint))
            {
                pos++;
                renderHint = hint.ToLowerInvariant();
            }
        }

        // Trailing tokens = syntax error
        if (pos < tokens.Count)
            return NqlValidationResult.Fail($"Unexpected token '{tokens[pos]}' at position {pos}.");

        return NqlValidationResult.Ok(new NqlPlan(
            entity,
            filters,
            groupBy,
            orderBy,
            limit,
            renderHint));
    }

    // ─── Clause helpers ─────────────────────────────────────────────────────

    private static List<NqlFilter>? ParseFilters(List<string> tokens, ref int pos)
    {
        var filters = new List<NqlFilter>();

        while (true)
        {
            if (!TryPeek(tokens, pos, out var field)) break;
            // Stop if we hit a keyword
            if (IsKeyword(field)) break;
            pos++;

            if (!TryPeek(tokens, pos, out var opStr)) return null;
            pos++;
            if (!TryParseOperator(opStr, out var op)) return null;

            if (!TryPeek(tokens, pos, out var value)) return null;
            pos++;

            filters.Add(new NqlFilter(field, op, value));

            // AND continues the chain
            if (!ConsumeKeyword(tokens, ref pos, "AND")) break;
        }

        return filters;
    }

    private static void ParseCommaSeparatedFields(List<string> tokens, ref int pos, List<string> result)
    {
        while (TryPeek(tokens, pos, out var field) && !IsKeyword(field))
        {
            pos++;
            result.Add(field);
            if (pos < tokens.Count && tokens[pos] == ",")
                pos++;
            else
                break;
        }
    }

    // ─── Token helpers ───────────────────────────────────────────────────────

    private static bool TryPeek(List<string> tokens, int pos, out string value)
    {
        if (pos < tokens.Count) { value = tokens[pos]; return true; }
        value = string.Empty;
        return false;
    }

    private static bool Consume(List<string> tokens, ref int pos, string expected)
    {
        if (pos < tokens.Count && string.Equals(tokens[pos], expected, StringComparison.OrdinalIgnoreCase))
        {
            pos++;
            return true;
        }
        return false;
    }

    private static bool ConsumeKeyword(List<string> tokens, ref int pos, string keyword) =>
        Consume(tokens, ref pos, keyword);

    private static readonly HashSet<string> Keywords = new(StringComparer.OrdinalIgnoreCase)
        { "FROM", "WHERE", "AND", "GROUP", "BY", "ORDER", "LIMIT", "ASC", "DESC", "RENDER", "AS" };

    private static bool IsKeyword(string token) => Keywords.Contains(token);

    private static bool TryParseOperator(string op, out NqlFilterOperator result)
    {
        result = op switch
        {
            "="    => NqlFilterOperator.Equals,
            "!="   => NqlFilterOperator.NotEquals,
            ">"    => NqlFilterOperator.GreaterThan,
            "<"    => NqlFilterOperator.LessThan,
            ">="   => NqlFilterOperator.GreaterThanOrEquals,
            "<="   => NqlFilterOperator.LessThanOrEquals,
            _      => NqlFilterOperator.Equals
        };

        if (string.Equals(op, "LIKE", StringComparison.OrdinalIgnoreCase))
        {
            result = NqlFilterOperator.Like;
            return true;
        }

        if (op is "=" or "!=" or ">" or "<" or ">=" or "<=")
            return true;

        return false;
    }
}
