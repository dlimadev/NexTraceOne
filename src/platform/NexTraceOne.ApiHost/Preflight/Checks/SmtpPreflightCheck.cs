using Microsoft.Extensions.Configuration;

namespace NexTraceOne.ApiHost.Preflight.Checks;

/// <summary>
/// Verifica se o SMTP está configurado para envio de notificações por email.
/// Check de aviso — não bloqueia o startup. Notificações ficam desactivadas se não configurado.
/// </summary>
public sealed class SmtpPreflightCheck(IConfiguration configuration) : IPreflightCheck
{
    private const string CheckName = "SMTP (Email)";

    public Task<IReadOnlyList<PreflightCheckResult>> RunAsync(CancellationToken ct = default)
    {
        var smtpHost = configuration["Smtp:Host"];

        if (string.IsNullOrWhiteSpace(smtpHost))
        {
            return Task.FromResult<IReadOnlyList<PreflightCheckResult>>([
                new PreflightCheckResult(
                    CheckName, PreflightCheckStatus.Warning,
                    "SMTP not configured — email notifications are disabled.",
                    "Set Smtp__Host, Smtp__Port, Smtp__Username, Smtp__Password and Smtp__From to enable notifications.",
                    IsRequired: false)
            ]);
        }

        return Task.FromResult<IReadOnlyList<PreflightCheckResult>>([
            new PreflightCheckResult(
                CheckName, PreflightCheckStatus.Ok,
                $"SMTP configured — host: {smtpHost}.")
        ]);
    }
}
