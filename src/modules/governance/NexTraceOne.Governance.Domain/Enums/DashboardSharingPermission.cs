namespace NexTraceOne.Governance.Domain.Enums;

/// <summary>
/// Permissão de acesso a um CustomDashboard partilhado (V3.1 — Dashboard Intelligence Foundation).
/// </summary>
public enum DashboardSharingPermission
{
    /// <summary>Apenas leitura — o destinatário pode ver mas não editar.</summary>
    Read = 0,

    /// <summary>Leitura e edição — o destinatário pode editar o dashboard.</summary>
    Edit = 1
}
