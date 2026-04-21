namespace NexTraceOne.ChangeGovernance.Domain.Compliance.Enums;

/// <summary>Estado de consentimento de participação em benchmarks cross-tenant anonimizados.</summary>
public enum BenchmarkConsentStatus
{
    /// <summary>Consentimento não solicitado ainda.</summary>
    NotRequested = 0,

    /// <summary>Solicitação de consentimento pendente de resposta.</summary>
    Pending = 1,

    /// <summary>Consentimento concedido pelo tenant.</summary>
    Granted = 2,

    /// <summary>Consentimento revogado pelo tenant.</summary>
    Revoked = 3
}
