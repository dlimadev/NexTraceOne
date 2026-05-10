using Microsoft.Extensions.Logging;
using NexTraceOne.IdentityAccess.Application.Abstractions;
using NexTraceOne.Notifications.Contracts.ServiceInterfaces;

namespace NexTraceOne.IdentityAccess.Infrastructure.Services;

/// <summary>
/// Implementação real de IIdentityNotifier que delega ao módulo Notifications via INotificationModule.
/// Usada quando Smtp:Host está configurado — garante que emails de activação e reset são entregues.
/// </summary>
internal sealed class NotificationsIdentityNotifier(
    INotificationModule notificationModule,
    ILogger<NotificationsIdentityNotifier> logger) : IIdentityNotifier
{
    public async Task SendAccountActivationAsync(
        string email, string fullName, string rawToken, CancellationToken ct)
    {
        logger.LogInformation("Sending account activation email to {Email} via Notifications module", email);

        var result = await notificationModule.SubmitAsync(new NotificationRequest
        {
            EventType = "Identity.AccountActivation",
            Category = "Security",
            Severity = "Info",
            Title = "Activate your NexTraceOne account",
            Message = $"Hello {fullName}, your activation token is: {rawToken}",
            SourceModule = "IdentityAccess",
            SourceEntityType = "User",
            RequiresAction = true,
            RecipientRoles = null,
            PayloadJson = $"{{\"email\":\"{email}\",\"fullName\":\"{fullName}\",\"token\":\"{rawToken}\"}}"
        }, ct);

        if (!result.Success)
            logger.LogWarning("Account activation notification submission failed for {Email}: {Error}", email, result.ErrorMessage);
    }

    public async Task SendPasswordResetAsync(
        string email, string fullName, string rawToken, CancellationToken ct)
    {
        logger.LogInformation("Sending password reset email to {Email} via Notifications module", email);

        var result = await notificationModule.SubmitAsync(new NotificationRequest
        {
            EventType = "Identity.PasswordReset",
            Category = "Security",
            Severity = "Warning",
            Title = "Reset your NexTraceOne password",
            Message = $"Hello {fullName}, your password reset token is: {rawToken}",
            SourceModule = "IdentityAccess",
            SourceEntityType = "User",
            RequiresAction = true,
            RecipientRoles = null,
            PayloadJson = $"{{\"email\":\"{email}\",\"fullName\":\"{fullName}\",\"token\":\"{rawToken}\"}}"
        }, ct);

        if (!result.Success)
            logger.LogWarning("Password reset notification submission failed for {Email}: {Error}", email, result.ErrorMessage);
    }
}
