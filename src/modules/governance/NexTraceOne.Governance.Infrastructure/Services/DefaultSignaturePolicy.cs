using NexTraceOne.Governance.Application.Abstractions;

namespace NexTraceOne.Governance.Infrastructure.Services;

/// <summary>
/// Implementação padrão de ISignaturePolicy com configurações recomendadas para compliance.
/// </summary>
public class DefaultSignaturePolicy : ISignaturePolicy
{
    public TimeSpan SignatureValidity => TimeSpan.FromDays(365); // 1 ano
    
    public string HashAlgorithm => "SHA256";
    
    public bool RequireTransparencyLog => true;
    
    public bool RequireSbom => true;
}
