using System.Net.Sockets;

namespace NexTraceOne.ApiHost.Preflight.Checks;

/// <summary>
/// Verifica se as portas 8080 e 8090 estão disponíveis para uso pelo NexTraceOne.
/// Check de aviso — não bloqueia o startup. Retorna um resultado por porta verificada.
/// </summary>
public sealed class PortsPreflightCheck : IPreflightCheck
{
    private static readonly int[] PortsToCheck = [8080, 8090];

    public Task<IReadOnlyList<PreflightCheckResult>> RunAsync(CancellationToken ct = default)
    {
        IReadOnlyList<PreflightCheckResult> results = PortsToCheck
            .Select(port =>
            {
                var inUse = IsPortInUse(port);
                return inUse
                    ? new PreflightCheckResult(
                        $"Port {port}", PreflightCheckStatus.Warning,
                        $"Port {port} appears to be in use by another process.",
                        $"Ensure port {port} is available or reconfigure NexTraceOne to use a different port.",
                        IsRequired: false)
                    : new PreflightCheckResult(
                        $"Port {port}", PreflightCheckStatus.Ok,
                        $"Port {port} is available.");
            })
            .ToList();

        return Task.FromResult(results);
    }

    private static bool IsPortInUse(int port)
    {
        try
        {
            using var listener = new TcpListener(System.Net.IPAddress.Loopback, port);
            listener.Start();
            listener.Stop();
            return false;
        }
        catch
        {
            return true;
        }
    }
}
