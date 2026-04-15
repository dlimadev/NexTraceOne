namespace NexTraceOne.AIKnowledge.Domain.Governance.Enums;

/// <summary>
/// Categoria funcional de um guardrail de IA.
/// Define o domínio de proteção que o guardrail cobre.
/// </summary>
public enum GuardrailCategory
{
    /// <summary>Protecção de segurança (ex: injeção de prompt, jailbreak).</summary>
    Security,

    /// <summary>Protecção de privacidade (ex: deteção de PII, dados pessoais).</summary>
    Privacy,

    /// <summary>Protecção de compliance (ex: dados regulamentados, LGPD/GDPR).</summary>
    Compliance,

    /// <summary>Controlo de qualidade (ex: respostas inadequadas, linguagem ofensiva).</summary>
    Quality
}
