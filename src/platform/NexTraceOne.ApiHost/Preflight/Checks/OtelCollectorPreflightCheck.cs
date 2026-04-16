using System.Net.Sockets;
using Microsoft.Extensions.Configuration;

namespace NexTraceOne.ApiHost.Preflight.Checks;

/// <summary>
/// Verifica se o OpenTelemetry Collector está acessível na porta 4317.
/// Check de aviso — não bloqueia o startup. Distributed tracing fica desactivado se inacessível.
/// </summary>
public sealed class OtelCollectorPreflightCheck(IConfiguration configuration) : IPreflightCheck
{
    private const string CheckName = "OTel Collector";

    public async Task<IReadOnlyList<PreflightCheckResult>> RunAsync(CancellationToken ct = default)
        => [await ExecuteAsync(ct)];

    private async Task<PreflightCheckResult> ExecuteAsync(CancellationToken ct)
    {
        var otelEnabled = configuration.GetValue<bool?>("OpenTelemetry:Enabled") ?? true;
        if (!otelEnabled)
        {
            return new PreflightCheckResult(
                CheckName, PreflightCheckStatus.Ok,
                "OTel Collector disabled by configuration — skipped.",
                IsRequired: false, Suggestion: null);
        }

        try
        {
            using var tcp = new TcpClient();
            var connectTask = tcp.ConnectAsync("localhost", 4317, ct).AsTask();
            if (await Task.WhenAny(connectTask, Task.Delay(2000, ct)) == connectTask && !connectTask.IsFaulted)
            {
                return new PreflightCheckResult(
                    CheckName, PreflightCheckStatus.Ok,
                    "OTel Collector accessible at :4317.");
            }

            return new PreflightCheckResult(
                CheckName, PreflightCheckStatus.Warning,
                "OTel Collector not accessible at :4317 — distributed tracing disabled.",
                "Start an OpenTelemetry Collector or disable OTel in configuration.",
                IsRequired: false);
        }
        catch
        {
            return new PreflightCheckResult(
                CheckName, PreflightCheckStatus.Warning,
                "OTel Collector not accessible — distributed tracing disabled.",
                "Start an OpenTelemetry Collector or set OpenTelemetry__Enabled=false to suppress this warning.",
                IsRequired: false);
        }
    }
}
