using Microsoft.Extensions.Configuration;

namespace NexTraceOne.ApiHost.Preflight.Checks;

/// <summary>
/// Verifica se estão configuradas CORS origins para permitir acesso a clientes browser externos.
/// Check de aviso — não bloqueia o startup.
/// </summary>
public sealed class CorsOriginsPreflightCheck(IConfiguration configuration) : IPreflightCheck
{
    private const string CheckName = "CORS Origins";

    public Task<IReadOnlyList<PreflightCheckResult>> RunAsync(CancellationToken ct = default)
    {
        var origins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];

        if (origins.Length == 0)
        {
            return Task.FromResult<IReadOnlyList<PreflightCheckResult>>([
                new PreflightCheckResult(
                    CheckName, PreflightCheckStatus.Warning,
                    "No CORS origins configured — browser clients from external domains will be blocked.",
                    "Set Cors__AllowedOrigins with the URL(s) of the NexTraceOne frontend (e.g., https://nextraceone.acme.com).",
                    IsRequired: false)
            ]);
        }

        return Task.FromResult<IReadOnlyList<PreflightCheckResult>>([
            new PreflightCheckResult(
                CheckName, PreflightCheckStatus.Ok,
                $"CORS configured — {origins.Length} origin(s): {string.Join(", ", origins)}.")
        ]);
    }
}
