using NexTraceOne.IdentityAccess.Domain.Entities;

namespace NexTraceOne.IdentityAccess.Application.Abstractions;

public interface IPasswordResetTokenRepository
{
    Task<PasswordResetToken?> FindByHashAsync(string tokenHash, CancellationToken ct);
    Task DeleteByUserIdAsync(UserId userId, CancellationToken ct);
    void Add(PasswordResetToken token);
}
