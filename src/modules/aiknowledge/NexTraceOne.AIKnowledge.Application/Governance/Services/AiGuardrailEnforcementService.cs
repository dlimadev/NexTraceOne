using System.Text.RegularExpressions;

using Microsoft.Extensions.Logging;

using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;

namespace NexTraceOne.AIKnowledge.Application.Governance.Services;

/// <summary>
/// Implementação do serviço de enforcement de guardrails.
/// Combina guardrails ativos do repositório com verificações built-in de segurança:
/// comprimento máximo, injeção de prompt, PII e padrões sensíveis configuráveis.
/// </summary>
public sealed class AiGuardrailEnforcementService(
    IAiGuardrailRepository guardrailRepository,
    ILogger<AiGuardrailEnforcementService> logger) : IAiGuardrailEnforcementService
{
    // Comprimento máximo permitido para inputs (100K caracteres ~ 25K tokens)
    private const int MaxInputLength = 100_000;

    // Padrões de injeção de prompt — detetam tentativas de override do system prompt
    private static readonly string[] PromptInjectionPatterns =
    [
        @"ignore\s+(?:all\s+)?previous\s+instructions?",
        @"disregard\s+(?:all\s+)?previous\s+instructions?",
        @"forget\s+(?:all\s+)?previous\s+instructions?",
        @"you\s+are\s+now\s+(?:a\s+)?(?:dan|jailbroken|unrestricted)",
        @"do\s+anything\s+now\s*[:\-]?\s*dan",
        @"bypass\s+(?:your\s+)?(?:safety|ethics|content)\s+(?:filters?|guidelines?|policies?)",
        @"(?:print|output|reveal|show|display|tell\s+me)\s+(?:your\s+)?(?:system\s+prompt|instructions?)",
        @"\[system\]|\<system\>|##\s*system",
    ];

    private static readonly Regex[] PromptInjectionRegexes =
        PromptInjectionPatterns.Select(p =>
            new Regex(p, RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.CultureInvariant))
        .ToArray();

    // Padrões de PII / dados sensíveis no output — focados em padrões concretos e de alta confiança
    private static readonly (string Name, Regex Pattern)[] PiiOutputPatterns =
    [
        ("CONNECTION_STRING", new Regex(@"(?:Server|Data Source|Initial Catalog|Password|Pwd)\s*=", RegexOptions.IgnoreCase | RegexOptions.Compiled)),
        ("BEARER_TOKEN", new Regex(@"Bearer\s+[A-Za-z0-9\-_.~+/]+=*", RegexOptions.Compiled)),
        ("PRIVATE_KEY_HEADER", new Regex(@"-----BEGIN\s+(?:RSA\s+)?PRIVATE\s+KEY-----", RegexOptions.Compiled)),
    ];

    public async Task<GuardrailEvaluationResult> EvaluateInputAsync(
        string input,
        Guid tenantId,
        string persona,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(input))
            return GuardrailEvaluationResult.Passed();

        // 1. Length check (built-in, always active)
        if (input.Length > MaxInputLength)
        {
            logger.LogWarning(
                "Input guardrail triggered: length {Length} exceeds maximum {Max} for tenant {TenantId}",
                input.Length, MaxInputLength, tenantId);

            return GuardrailEvaluationResult.Blocked(
                reason: "Input exceeds maximum allowed length",
                pattern: "max_length",
                severity: "high",
                userMessage: $"Your message is too long. Please reduce it to under {MaxInputLength:N0} characters.");
        }

        // 2. Prompt injection detection (built-in)
        foreach (var regex in PromptInjectionRegexes)
        {
            if (regex.IsMatch(input))
            {
                logger.LogWarning(
                    "Prompt injection attempt detected for tenant {TenantId}, pattern={Pattern}",
                    tenantId, regex);

                return GuardrailEvaluationResult.Blocked(
                    reason: "Prompt injection attempt detected",
                    pattern: "prompt_injection",
                    severity: "critical",
                    userMessage: "Your message contains patterns that are not allowed. Please rephrase your request.");
            }
        }

        // 3. Active guardrails from repository (input + both)
        var activeGuardrails = await guardrailRepository.GetByGuardTypeAsync("input", cancellationToken);
        var bothGuardrails = await guardrailRepository.GetByGuardTypeAsync("both", cancellationToken);
        var inputGuardrails = activeGuardrails.Concat(bothGuardrails).Where(g => g.IsActive).ToList();

        foreach (var guardrail in inputGuardrails)
        {
            if (!TryMatchGuardrail(guardrail.PatternType, guardrail.Pattern, input, out var matchedText))
                continue;

            logger.LogWarning(
                "Guardrail '{GuardrailName}' triggered for tenant {TenantId}: matched '{MatchedText}'",
                guardrail.Name, tenantId, matchedText);

            if (string.Equals(guardrail.Action, "block", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(guardrail.Action, "warn", StringComparison.OrdinalIgnoreCase))
            {
                return GuardrailEvaluationResult.Blocked(
                    reason: $"Guardrail '{guardrail.DisplayName}' triggered",
                    pattern: guardrail.Name,
                    severity: guardrail.Severity,
                    userMessage: guardrail.UserMessage ?? "Your request was blocked by content policy.");
            }
        }

        return GuardrailEvaluationResult.Passed();
    }

    public async Task<GuardrailEvaluationResult> EvaluateOutputAsync(
        string output,
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(output))
            return GuardrailEvaluationResult.Passed();

        // 1. Built-in PII / sensitive data detection in output
        foreach (var (name, pattern) in PiiOutputPatterns)
        {
            if (pattern.IsMatch(output))
            {
                logger.LogWarning(
                    "Output guardrail triggered: PII pattern '{PatternName}' detected for tenant {TenantId}",
                    name, tenantId);

                return GuardrailEvaluationResult.Blocked(
                    reason: $"Output contains sensitive pattern: {name}",
                    pattern: $"pii_{name.ToLowerInvariant()}",
                    severity: "high",
                    userMessage: "The response was blocked because it may contain sensitive information.");
            }
        }

        // 2. Active guardrails from repository (output + both)
        var outputGuardrails = await guardrailRepository.GetByGuardTypeAsync("output", cancellationToken);
        var bothGuardrails = await guardrailRepository.GetByGuardTypeAsync("both", cancellationToken);
        var evaluableGuardrails = outputGuardrails.Concat(bothGuardrails).Where(g => g.IsActive).ToList();

        foreach (var guardrail in evaluableGuardrails)
        {
            if (!TryMatchGuardrail(guardrail.PatternType, guardrail.Pattern, output, out var matchedText))
                continue;

            logger.LogWarning(
                "Output guardrail '{GuardrailName}' triggered for tenant {TenantId}",
                guardrail.Name, tenantId);

            if (string.Equals(guardrail.Action, "block", StringComparison.OrdinalIgnoreCase))
            {
                return GuardrailEvaluationResult.Blocked(
                    reason: $"Output guardrail '{guardrail.DisplayName}' triggered",
                    pattern: guardrail.Name,
                    severity: guardrail.Severity,
                    userMessage: guardrail.UserMessage ?? "The response was blocked by output policy.");
            }
        }

        return GuardrailEvaluationResult.Passed();
    }

    private static bool TryMatchGuardrail(
        string patternType,
        string pattern,
        string text,
        out string? matchedText)
    {
        matchedText = null;

        if (string.IsNullOrWhiteSpace(pattern))
            return false;

        try
        {
            if (string.Equals(patternType, "regex", StringComparison.OrdinalIgnoreCase))
            {
                var match = Regex.Match(text, pattern,
                    RegexOptions.IgnoreCase | RegexOptions.CultureInvariant,
                    TimeSpan.FromMilliseconds(200));

                if (match.Success)
                {
                    matchedText = match.Value;
                    return true;
                }

                return false;
            }

            if (string.Equals(patternType, "keyword", StringComparison.OrdinalIgnoreCase))
            {
                var keywords = pattern.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                foreach (var kw in keywords)
                {
                    if (text.Contains(kw, StringComparison.OrdinalIgnoreCase))
                    {
                        matchedText = kw;
                        return true;
                    }
                }

                return false;
            }

            return false;
        }
        catch (RegexMatchTimeoutException)
        {
            // Timeout — treat as no match to avoid blocking legitimate requests
            return false;
        }
        catch (ArgumentException)
        {
            // Invalid regex — skip gracefully
            return false;
        }
    }
}
