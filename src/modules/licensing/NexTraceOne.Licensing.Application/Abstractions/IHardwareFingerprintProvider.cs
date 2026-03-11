namespace NexTraceOne.Licensing.Application.Abstractions;

/// <summary>
/// Serviço responsável por obter a impressão digital do hardware atual.
/// </summary>
public interface IHardwareFingerprintProvider
{
    /// <summary>Gera ou recupera o fingerprint do hardware atual.</summary>
    string Generate();
}
