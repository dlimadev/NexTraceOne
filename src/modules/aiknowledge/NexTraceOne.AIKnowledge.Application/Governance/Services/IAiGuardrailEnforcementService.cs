namespace NexTraceOne.AIKnowledge.Application.Governance.Services;

/// <summary>
/// Resultado da avaliação de um guardrail.
/// </summary>
public sealed record GuardrailEvaluationResult(
    bool IsBlocked,
    string? ViolationReason,
    string? ViolatedPattern,
    string? Severity,
    string? UserMessage)
{
    /// <summary>Resultado de passagem (sem violação).</summary>
    public static GuardrailEvaluationResult Passed()
        => new(false, null, null, null, null);

    /// <summary>Resultado de bloqueio com detalhes da violação.</summary>
    public static GuardrailEvaluationResult Blocked(string reason, string pattern, string severity, string? userMessage = null)
        => new(true, reason, pattern, severity, userMessage);
}

/// <summary>
/// Serviço de enforcement de guardrails de IA.
/// Avalia inputs e outputs contra guardrails ativos — regras de proteção contra
/// prompt injection, dados sensíveis, conteúdo proibido e violações de política.
/// </summary>
public interface IAiGuardrailEnforcementService
{
    /// <summary>
    /// Avalia o input do utilizador antes de enviar ao LLM.
    /// Verifica comprimento, injeção de prompt, dados sensíveis e padrões bloqueados.
    /// </summary>
    /// <param name="input">Texto de input a avaliar.</param>
    /// <param name="tenantId">Identificador do tenant para políticas específicas.</param>
    /// <param name="persona">Persona do utilizador para contexto de avaliação.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Resultado da avaliação: passagem ou bloqueio com razão.</returns>
    Task<GuardrailEvaluationResult> EvaluateInputAsync(
        string input,
        Guid tenantId,
        string persona,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Avalia o output do LLM antes de retornar ao utilizador.
    /// Verifica padrões sensíveis, PII e conteúdo proibido no output.
    /// </summary>
    /// <param name="output">Texto de output a avaliar.</param>
    /// <param name="tenantId">Identificador do tenant para políticas específicas.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Resultado da avaliação: passagem ou bloqueio com razão.</returns>
    Task<GuardrailEvaluationResult> EvaluateOutputAsync(
        string output,
        Guid tenantId,
        CancellationToken cancellationToken = default);
}
