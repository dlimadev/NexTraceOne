namespace NexTraceOne.Governance.Application.Abstractions;

/// <summary>
/// Política de assinatura digital que define validade, algoritmos e requisitos de compliance.
/// </summary>
public interface ISignaturePolicy
{
    /// <summary>Duração de validade da assinatura (ex: 1 ano).</summary>
    TimeSpan SignatureValidity { get; }

    /// <summary>Algoritmo de hash usado para checksum (ex: SHA256).</summary>
    string HashAlgorithm { get; }

    /// <summary>Requer entrada no transparency log (Rekor)?</summary>
    bool RequireTransparencyLog { get; }

    /// <summary>Requer SBOM anexado à assinatura?</summary>
    bool RequireSbom { get; }
}
