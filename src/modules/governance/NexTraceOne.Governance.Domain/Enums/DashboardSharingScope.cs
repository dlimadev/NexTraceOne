namespace NexTraceOne.Governance.Domain.Enums;

/// <summary>
/// Âmbito de partilha de um CustomDashboard (V3.1 — Dashboard Intelligence Foundation).
/// </summary>
public enum DashboardSharingScope
{
    /// <summary>Apenas o criador pode ver/editar.</summary>
    Private = 0,

    /// <summary>Visível à equipa do criador (TeamId).</summary>
    Team = 1,

    /// <summary>Visível a todos os utilizadores do tenant.</summary>
    Tenant = 2,

    /// <summary>Link público assinado com expiração (V3.6 feature — configurável por policy).</summary>
    PublicLink = 3
}
