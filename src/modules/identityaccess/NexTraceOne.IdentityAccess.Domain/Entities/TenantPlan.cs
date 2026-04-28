namespace NexTraceOne.IdentityAccess.Domain.Entities;

/// <summary>
/// Plano de licenciamento SaaS do tenant.
/// Determina quais capabilities estão disponíveis.
/// </summary>
public enum TenantPlan
{
    /// <summary>Plano trial temporário (14 dias) — acesso completo limitado.</summary>
    Trial = 0,

    /// <summary>Plano Starter — catálogo de serviços e governança básica.</summary>
    Starter = 1,

    /// <summary>Plano Professional — + AI governance, Contract Studio, FinOps, compliance básica.</summary>
    Professional = 2,

    /// <summary>Plano Enterprise — todas as capabilities + compliance avançada, multi-região.</summary>
    Enterprise = 3,
}
