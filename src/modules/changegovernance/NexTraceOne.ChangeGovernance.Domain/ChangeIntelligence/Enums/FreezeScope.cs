namespace NexTraceOne.ChangeIntelligence.Domain.Enums;

/// <summary>
/// Escopo de aplicação de uma janela de freeze.
/// Define a abrangência da restrição de mudanças.
/// </summary>
public enum FreezeScope
{
    /// <summary>Freeze aplicado a todos os tenants e ambientes.</summary>
    Global = 0,
    /// <summary>Freeze específico para um tenant.</summary>
    Tenant = 1,
    /// <summary>Freeze específico para um domínio/área de negócio.</summary>
    Domain = 2,
    /// <summary>Freeze específico para um ambiente (ex: produção).</summary>
    Environment = 3,
    /// <summary>Freeze específico para um serviço/API.</summary>
    Service = 4
}
