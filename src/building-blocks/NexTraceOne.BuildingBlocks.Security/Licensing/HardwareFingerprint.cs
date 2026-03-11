namespace NexTraceOne.BuildingBlocks.Security.Licensing;

/// <summary>
/// Gera impressão digital do hardware: SHA-256(CPU ID | Motherboard UUID | MAC).
/// Usada para binding de licença ao hardware registrado.
/// Em ambientes virtualizados, usa identificadores do hypervisor.
/// </summary>
public sealed class HardwareFingerprint
{
    /// <summary>Gera fingerprint. Retorna hex 64 chars (SHA-256).</summary>
    public static string Generate()
    {
        // TODO: Implementar coleta de CPU ID, Motherboard UUID e MAC
        throw new NotImplementedException();
    }
}
