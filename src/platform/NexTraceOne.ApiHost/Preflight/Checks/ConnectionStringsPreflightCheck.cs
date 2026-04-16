using Microsoft.Extensions.Configuration;

namespace NexTraceOne.ApiHost.Preflight.Checks;

/// <summary>
/// Verifica se pelo menos uma connection string está configurada.
/// Check obrigatório — falha bloqueia o startup.
/// </summary>
public sealed class ConnectionStringsPreflightCheck(IConfiguration configuration) : IPreflightCheck
{
    private const string CheckName = "Connection Strings";

    public Task<IReadOnlyList<PreflightCheckResult>> RunAsync(CancellationToken ct = default)
    {
        var connStrings = configuration.GetSection("ConnectionStrings");

        if (!connStrings.Exists())
        {
            return Task.FromResult<IReadOnlyList<PreflightCheckResult>>([
                new PreflightCheckResult(
                    CheckName, PreflightCheckStatus.Error,
                    "ConnectionStrings section is missing from configuration.",
                    "Add the ConnectionStrings section with at least the NexTraceOne connection string.")
            ]);
        }

        var configured = connStrings.GetChildren()
            .Count(c => !string.IsNullOrWhiteSpace(c.Value));

        var total = connStrings.GetChildren().Count();

        if (configured == 0)
        {
            return Task.FromResult<IReadOnlyList<PreflightCheckResult>>([
                new PreflightCheckResult(
                    CheckName, PreflightCheckStatus.Error,
                    "No connection strings are configured.",
                    "Configure at least ConnectionStrings__NexTraceOne.")
            ]);
        }

        return Task.FromResult<IReadOnlyList<PreflightCheckResult>>([
            new PreflightCheckResult(
                CheckName, PreflightCheckStatus.Ok,
                $"Connection strings: {configured}/{total} configured.")
        ]);
    }
}
