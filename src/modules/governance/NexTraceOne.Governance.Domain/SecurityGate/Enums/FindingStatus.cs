namespace NexTraceOne.Governance.Domain.SecurityGate.Enums;

/// <summary>Estado de um achado de segurança.</summary>
public enum FindingStatus
{
    /// <summary>Aberto — requer ação.</summary>
    Open = 0,

    /// <summary>Reconhecido — equipa está ciente e a trabalhar nele.</summary>
    Acknowledged = 1,

    /// <summary>Mitigado — corrigido ou contramedida aplicada.</summary>
    Mitigated = 2,

    /// <summary>Falso positivo — confirmado que não é um problema real.</summary>
    FalsePositive = 3
}
