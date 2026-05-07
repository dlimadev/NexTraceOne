namespace NexTraceOne.IdentityAccess.Application.Abstractions;

/// <summary>
/// Port para envio de emails transacionais do módulo Identity.
/// A implementação real delega ao módulo Notifications via integration events.
/// </summary>
public interface IIdentityNotifier
{
    Task SendAccountActivationAsync(string email, string fullName, string rawToken, CancellationToken ct);
    Task SendPasswordResetAsync(string email, string fullName, string rawToken, CancellationToken ct);
}
