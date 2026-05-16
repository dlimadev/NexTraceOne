namespace NexTraceOne.AIKnowledge.Application.Runtime.Abstractions;

/// <summary>
/// Serviço de redação (sanitização) de PII e dados sensíveis antes de envio ao LLM.
/// Implementa as regras de segurança de dados do grounding (SECURITY-ARCHITECTURE.md).
/// </summary>
public interface IPiiRedactionService
{
    /// <summary>
    /// Redige PII e dados sensíveis de um texto (connection strings, tokens, chaves, emails, etc.).
    /// Retorna o texto sanitizado com placeholders indicativos.
    /// </summary>
    string Redact(string text);

    /// <summary>
    /// Verifica se um texto contém dados sensíveis que deveriam ser redigidos.
    /// </summary>
    bool ContainsSensitiveData(string text);
}
