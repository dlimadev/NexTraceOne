namespace NexTraceOne.AIKnowledge.Domain.Governance.Entities;

/// <summary>
/// Catálogo estático de guardrails oficiais da plataforma.
/// Utilizado pela feature SeedDefaultGuardrails para garantir que o sistema
/// contenha as proteções mínimas para operação segura de IA.
///
/// Inclui guardrails para deteção de PII, injeção de prompt, dados sensíveis,
/// limites de output e conformidade com políticas organizacionais.
///
/// O catálogo é determinístico e idempotente — não cria guardrails duplicados.
/// </summary>
public static class DefaultGuardrailCatalog
{
    /// <summary>Definição de um guardrail para seed.</summary>
    public sealed record GuardrailDefinition(
        string Name,
        string DisplayName,
        string Description,
        string Category,
        string GuardType,
        string Pattern,
        string PatternType,
        string Severity,
        string Action,
        string? UserMessage,
        int Priority);

    /// <summary>
    /// Retorna a lista completa de guardrails oficiais da plataforma.
    /// </summary>
    public static IReadOnlyList<GuardrailDefinition> GetAll() => Guardrails;

    private static readonly IReadOnlyList<GuardrailDefinition> Guardrails = new[]
    {
        // ── Security ────────────────────────────────────────────────────
        new GuardrailDefinition(
            Name: "prompt-injection-detection",
            DisplayName: "Prompt Injection Detection",
            Description: "Detects and blocks common prompt injection patterns that attempt to override system instructions or extract sensitive data.",
            Category: "security",
            GuardType: "input",
            Pattern: @"(?i)(ignore\s+(previous|all|above)\s+(instructions?|prompts?|rules?))|(you\s+are\s+now\s+)|(system\s*:\s*you)|(pretend\s+you\s+are)|(act\s+as\s+if)|(reveal\s+(your|the)\s+(system|instructions?|prompt))|(what\s+are\s+your\s+(instructions?|rules?))|(output\s+your\s+(system|initial)\s+prompt)",
            PatternType: "regex",
            Severity: "critical",
            Action: "block",
            UserMessage: "Your message was blocked because it contains patterns that may attempt to manipulate system behavior. Please rephrase your request.",
            Priority: 1),

        new GuardrailDefinition(
            Name: "credential-leak-prevention",
            DisplayName: "Credential Leak Prevention",
            Description: "Prevents AI responses from including credentials, API keys, tokens, or connection strings.",
            Category: "security",
            GuardType: "output",
            Pattern: @"(?i)(api[_-]?key\s*[:=]\s*\S{10,})|(password\s*[:=]\s*\S{6,})|(bearer\s+[a-zA-Z0-9\-._~+/]+=*)|(-----BEGIN\s+(RSA\s+)?PRIVATE\s+KEY-----)|(connectionstring\s*[:=])|(secret\s*[:=]\s*\S{10,})",
            PatternType: "regex",
            Severity: "critical",
            Action: "sanitize",
            UserMessage: "The response was sanitized to remove potentially sensitive credentials.",
            Priority: 2),

        // ── Privacy ─────────────────────────────────────────────────────
        new GuardrailDefinition(
            Name: "pii-email-detection",
            DisplayName: "PII Email Detection",
            Description: "Detects email addresses in AI inputs and outputs to prevent PII exposure.",
            Category: "privacy",
            GuardType: "both",
            Pattern: @"[a-zA-Z0-9._%+\-]+@[a-zA-Z0-9.\-]+\.[a-zA-Z]{2,}",
            PatternType: "regex",
            Severity: "high",
            Action: "warn",
            UserMessage: "Email addresses detected in the content. Ensure PII handling policies are followed.",
            Priority: 10),

        new GuardrailDefinition(
            Name: "pii-phone-detection",
            DisplayName: "PII Phone Number Detection",
            Description: "Detects phone numbers in AI inputs and outputs to prevent PII exposure.",
            Category: "privacy",
            GuardType: "both",
            Pattern: @"(\+?\d{1,3}[-.\s]?)?\(?\d{2,4}\)?[-.\s]?\d{3,4}[-.\s]?\d{3,4}",
            PatternType: "regex",
            Severity: "medium",
            Action: "warn",
            UserMessage: "Phone numbers detected in the content. Ensure PII handling policies are followed.",
            Priority: 11),

        // ── Compliance ──────────────────────────────────────────────────
        new GuardrailDefinition(
            Name: "sensitive-data-classification",
            DisplayName: "Sensitive Data Classification",
            Description: "Detects references to sensitive data classifications and ensures proper handling.",
            Category: "compliance",
            GuardType: "both",
            Pattern: @"(?i)(confidential|top\s*secret|restricted|internal\s+only|classified|not\s+for\s+distribution)",
            PatternType: "regex",
            Severity: "high",
            Action: "log",
            UserMessage: null,
            Priority: 20),

        new GuardrailDefinition(
            Name: "gdpr-personal-data-reference",
            DisplayName: "GDPR Personal Data Reference",
            Description: "Detects references to GDPR-relevant personal data categories in AI interactions.",
            Category: "compliance",
            GuardType: "both",
            Pattern: @"(?i)(social\s+security\s+number|passport\s+number|national\s+id|tax\s+id|health\s+record|biometric\s+data|genetic\s+data|political\s+opinion|religious\s+belief|trade\s+union\s+membership)",
            PatternType: "regex",
            Severity: "high",
            Action: "warn",
            UserMessage: "GDPR-relevant personal data detected. Ensure compliance with data protection policies.",
            Priority: 21),

        // ── Quality ─────────────────────────────────────────────────────
        new GuardrailDefinition(
            Name: "excessive-output-length",
            DisplayName: "Excessive Output Length Guard",
            Description: "Flags AI responses that exceed reasonable length, indicating potential hallucination or repetition.",
            Category: "quality",
            GuardType: "output",
            Pattern: @".{15000,}",
            PatternType: "regex",
            Severity: "medium",
            Action: "warn",
            UserMessage: "The AI response is unusually long. Please verify the output for accuracy and relevance.",
            Priority: 30),

        new GuardrailDefinition(
            Name: "hallucination-indicator",
            DisplayName: "Hallucination Indicator",
            Description: "Detects common AI hallucination patterns such as fabricated URLs, fake references, or invented statistics.",
            Category: "quality",
            GuardType: "output",
            Pattern: @"(?i)(according\s+to\s+my\s+(training|knowledge))|(as\s+of\s+my\s+last\s+update)|(I\s+don'?t\s+have\s+access\s+to\s+real-?time)|(here\s+is\s+a\s+hypothetical)|(I'?m\s+making\s+this\s+up)",
            PatternType: "regex",
            Severity: "medium",
            Action: "log",
            UserMessage: null,
            Priority: 31),
    };
}
