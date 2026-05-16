using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using NexTraceOne.AIKnowledge.Application.Runtime.Abstractions;

namespace NexTraceOne.AIKnowledge.Infrastructure.Runtime.Services;

/// <summary>
/// Implementação de redação de PII para grounding de IA.
/// Remove ou mascara dados sensíveis antes de envio ao LLM,
/// conforme requisito de segurança documentado em SECURITY-ARCHITECTURE.md.
/// </summary>
public sealed partial class PiiRedactionService : IPiiRedactionService
{
    private readonly ILogger<PiiRedactionService> _logger;

    // Compilados uma única vez para performance
    private static readonly Regex ConnectionStringRegex = GetConnectionStringRegex();
    private static readonly Regex BearerTokenRegex = GetBearerTokenRegex();
    private static readonly Regex PrivateKeyRegex = GetPrivateKeyRegex();
    private static readonly Regex ApiKeyRegex = GetApiKeyRegex();
    private static readonly Regex EmailRegex = GetEmailRegex();
    private static readonly Regex IpAddressRegex = GetIpAddressRegex();
    private static readonly Regex SsnRegex = GetSsnRegex();
    private static readonly Regex CreditCardRegex = GetCreditCardRegex();

    public PiiRedactionService(ILogger<PiiRedactionService> logger)
    {
        _logger = logger;
    }

    public string Redact(string text)
    {
        if (string.IsNullOrEmpty(text))
            return text;

        var originalLength = text.Length;
        var redacted = text;

        redacted = ConnectionStringRegex.Replace(redacted, "[REDACTED-CONNECTION-STRING]");
        redacted = BearerTokenRegex.Replace(redacted, "[REDACTED-BEARER-TOKEN]");
        redacted = PrivateKeyRegex.Replace(redacted, "[REDACTED-PRIVATE-KEY]");
        redacted = ApiKeyRegex.Replace(redacted, "[REDACTED-API-KEY]");
        redacted = EmailRegex.Replace(redacted, "[REDACTED-EMAIL]");
        redacted = IpAddressRegex.Replace(redacted, "[REDACTED-IP]");
        redacted = SsnRegex.Replace(redacted, "[REDACTED-SSN]");
        redacted = CreditCardRegex.Replace(redacted, "[REDACTED-CREDIT-CARD]");

        if (redacted.Length != originalLength)
        {
            _logger.LogDebug("PII redaction applied: {OriginalLength} -> {RedactedLength} chars", originalLength, redacted.Length);
        }

        return redacted;
    }

    public bool ContainsSensitiveData(string text)
    {
        if (string.IsNullOrEmpty(text))
            return false;

        return ConnectionStringRegex.IsMatch(text)
            || BearerTokenRegex.IsMatch(text)
            || PrivateKeyRegex.IsMatch(text)
            || ApiKeyRegex.IsMatch(text)
            || EmailRegex.IsMatch(text)
            || IpAddressRegex.IsMatch(text)
            || SsnRegex.IsMatch(text)
            || CreditCardRegex.IsMatch(text);
    }

    // Generated regexes for AOT compatibility and compile-time safety
    [GeneratedRegex("""(Server|Data Source|Host|Port|Database|Initial Catalog|User Id|Password|Integrated Security|SSL Mode)=[^;\s"']+""", RegexOptions.IgnoreCase | RegexOptions.Compiled, "en-US")]
    private static partial Regex GetConnectionStringRegex();

    [GeneratedRegex(@"Bearer\s+[A-Za-z0-9\-_]+\.[A-Za-z0-9\-_]+\.[A-Za-z0-9\-_]+", RegexOptions.Compiled, "en-US")]
    private static partial Regex GetBearerTokenRegex();

    [GeneratedRegex(@"-----BEGIN\s+(RSA\s+)?PRIVATE\s+KEY-----[\s\S]*?-----END\s+(RSA\s+)?PRIVATE\s+KEY-----", RegexOptions.Compiled, "en-US")]
    private static partial Regex GetPrivateKeyRegex();

    [GeneratedRegex("""(api[_-]?key|apikey|secret|token)\s*[:=]\s*["']?[a-zA-Z0-9\-_]{16,}["']?""", RegexOptions.IgnoreCase | RegexOptions.Compiled, "en-US")]
    private static partial Regex GetApiKeyRegex();

    [GeneratedRegex(@"[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}", RegexOptions.Compiled, "en-US")]
    private static partial Regex GetEmailRegex();

    [GeneratedRegex(@"\b(?:\d{1,3}\.){3}\d{1,3}\b|\b(?:[0-9a-fA-F]{1,4}:){7}[0-9a-fA-F]{1,4}\b", RegexOptions.Compiled, "en-US")]
    private static partial Regex GetIpAddressRegex();

    [GeneratedRegex(@"\b\d{3}[-.]?\d{2}[-.]?\d{4}\b", RegexOptions.Compiled, "en-US")]
    private static partial Regex GetSsnRegex();

    [GeneratedRegex(@"\b(?:\d{4}[-\s]?){3}\d{4}\b", RegexOptions.Compiled, "en-US")]
    private static partial Regex GetCreditCardRegex();
}
