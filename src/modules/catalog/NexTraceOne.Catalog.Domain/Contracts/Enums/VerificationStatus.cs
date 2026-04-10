namespace NexTraceOne.Catalog.Domain.Contracts.Enums;

/// <summary>
/// Estado do resultado de uma verificação de contrato.
/// Indica se a verificação passou, gerou aviso, foi bloqueada ou resultou em erro.
/// </summary>
public enum VerificationStatus
{
    /// <summary>A verificação passou sem problemas.</summary>
    Pass = 0,

    /// <summary>A verificação gerou avisos mas não bloqueou.</summary>
    Warn = 1,

    /// <summary>A verificação resultou em bloqueio.</summary>
    Block = 2,

    /// <summary>A verificação resultou em erro técnico.</summary>
    Error = 3
}
