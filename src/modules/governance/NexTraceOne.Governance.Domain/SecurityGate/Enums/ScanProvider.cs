namespace NexTraceOne.Governance.Domain.SecurityGate.Enums;

/// <summary>Provedor do scan de segurança.</summary>
public enum ScanProvider
{
    /// <summary>Scanner interno baseado em regras do NexTraceOne.</summary>
    Internal = 0,

    /// <summary>Semgrep.</summary>
    Semgrep = 1,

    /// <summary>SonarQube.</summary>
    SonarQube = 2
}
