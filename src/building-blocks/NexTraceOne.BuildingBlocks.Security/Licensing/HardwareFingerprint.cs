using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

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
        var components = new[]
        {
            Environment.MachineName,
            RuntimeInformation.OSDescription,
            RuntimeInformation.OSArchitecture.ToString(),
            Environment.ProcessorCount.ToString(),
            string.Join('|', GetMacAddresses())
        };

        var payload = string.Join('|', components);
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(payload));
        return Convert.ToHexString(hash);
    }

    private static IReadOnlyList<string> GetMacAddresses()
        => NetworkInterface.GetAllNetworkInterfaces()
            .Where(ni => ni.NetworkInterfaceType != NetworkInterfaceType.Loopback)
            .Select(ni => ni.GetPhysicalAddress().ToString())
            .Where(address => !string.IsNullOrWhiteSpace(address))
            .OrderBy(address => address, StringComparer.Ordinal)
            .ToArray();
}
