using NexTraceOne.IdentityAccess.Domain.Entities;

namespace NexTraceOne.IdentityAccess.Application.Abstractions;

public interface IAccountActivationTokenRepository
{
    Task<AccountActivationToken?> FindByHashAsync(string tokenHash, CancellationToken ct);
    Task DeleteByUserIdAsync(UserId userId, CancellationToken ct);
    void Add(AccountActivationToken token);
}
