namespace NexTraceOne.IdentityAccess.Domain.ValueObjects;

/// <summary>
/// Resultado imutável de uma avaliação de política no Policy Studio.
/// Returned by PolicyDefinition.Evaluate(contextJson).
/// </summary>
public sealed record PolicyEvaluationResult(
    /// <summary>True quando a política permite a acção (todos os predicados satisfeitos ou ação = Allow).</summary>
    bool Passed,
    /// <summary>Acção resultante: "Allow", "Warn" ou "Block".</summary>
    string Action,
    /// <summary>Mensagem explicativa proveniente do ActionJson da política.</summary>
    string? Message,
    /// <summary>Identificador da regra que despoletou o resultado (para debugging).</summary>
    string? RuleTriggered);
