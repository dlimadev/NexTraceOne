using Microsoft.Extensions.Logging;
using NexTraceOne.IdentityAccess.Application.Abstractions;

namespace NexTraceOne.IdentityAccess.Infrastructure.Services;

/// <summary>
/// Null implementation do IIdentityNotifier para ambientes sem email configurado.
/// Regista o token em log de Warning para facilitar testes locais/desenvolvimento.
/// Substituir por implementação real que integre com o módulo Notifications.
/// </summary>
internal sealed class NullIdentityNotifier(ILogger<NullIdentityNotifier> logger) : IIdentityNotifier
{
    public Task SendAccountActivationAsync(string email, string fullName, string rawToken, CancellationToken ct)
    {
        logger.LogWarning(
            "[NullIdentityNotifier] Account activation email NOT sent to {Email}. " +
            "Token (DEV ONLY — never log in production): {Token}",
            email, rawToken);
        return Task.CompletedTask;
    }

    public Task SendPasswordResetAsync(string email, string fullName, string rawToken, CancellationToken ct)
    {
        logger.LogWarning(
            "[NullIdentityNotifier] Password reset email NOT sent to {Email}. " +
            "Token (DEV ONLY — never log in production): {Token}",
            email, rawToken);
        return Task.CompletedTask;
    }
}
