using Microsoft.EntityFrameworkCore;
using NexTraceOne.BuildingBlocks.Infrastructure.Persistence;
using NexTraceOne.Identity.Application.Abstractions;
using NexTraceOne.Identity.Domain.Entities;
using NexTraceOne.Identity.Domain.ValueObjects;

namespace NexTraceOne.Identity.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repositório de usuários persistidos via EF Core.
/// Busca por email usa o Value do VO para tradução correta em SQL.
/// </summary>
internal sealed class UserRepository(IdentityDbContext context)
    : RepositoryBase<User, UserId>(context), IUserRepository
{
    /// <inheritdoc />
    public async Task<User?> GetByEmailAsync(Email email, CancellationToken cancellationToken)
        => await context.Users.SingleOrDefaultAsync(x => x.Email.Value == email.Value, cancellationToken);

    /// <inheritdoc />
    public async Task<User?> GetByFederatedIdentityAsync(string provider, string externalId, CancellationToken cancellationToken)
        => await context.Users.SingleOrDefaultAsync(
            x => x.FederationProvider == provider && x.ExternalId == externalId,
            cancellationToken);

    /// <inheritdoc />
    public Task<bool> ExistsAsync(Email email, CancellationToken cancellationToken)
        => context.Users.AnyAsync(x => x.Email.Value == email.Value, cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<UserId, User>> GetByIdsAsync(IReadOnlyCollection<UserId> ids, CancellationToken cancellationToken)
        => await context.Users
            .Where(x => ids.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, cancellationToken);
}
