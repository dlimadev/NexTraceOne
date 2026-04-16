using Microsoft.Extensions.Configuration;

namespace NexTraceOne.ApiHost.Preflight.Checks;

/// <summary>
/// Verifica se o JWT Secret está configurado e tem comprimento mínimo de 32 caracteres.
/// Check obrigatório — falha bloqueia o startup.
/// </summary>
public sealed class JwtSecretPreflightCheck(IConfiguration configuration) : IPreflightCheck
{
    private const string CheckName = "JWT Secret";
    private const int MinJwtSecretLength = 32;

    public Task<IReadOnlyList<PreflightCheckResult>> RunAsync(CancellationToken ct = default)
    {
        var secret = configuration["Jwt:Secret"];

        if (string.IsNullOrWhiteSpace(secret))
        {
            return Task.FromResult<IReadOnlyList<PreflightCheckResult>>([
                new PreflightCheckResult(
                    CheckName, PreflightCheckStatus.Error,
                    "Jwt:Secret is not configured.",
                    "Set the Jwt__Secret environment variable (minimum 32 characters). Generate with: openssl rand -base64 48")
            ]);
        }

        if (secret.Length < MinJwtSecretLength)
        {
            return Task.FromResult<IReadOnlyList<PreflightCheckResult>>([
                new PreflightCheckResult(
                    CheckName, PreflightCheckStatus.Error,
                    $"Jwt:Secret is {secret.Length} characters — below the minimum of {MinJwtSecretLength}.",
                    "Generate a stronger JWT secret with: openssl rand -base64 48")
            ]);
        }

        return Task.FromResult<IReadOnlyList<PreflightCheckResult>>([
            new PreflightCheckResult(
                CheckName, PreflightCheckStatus.Ok,
                $"JWT Secret configured — {secret.Length} characters.")
        ]);
    }
}
