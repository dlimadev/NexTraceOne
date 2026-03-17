using Microsoft.EntityFrameworkCore;

using NexTraceOne.BuildingBlocks.Infrastructure.Persistence;
using NexTraceOne.IdentityAccess.Application.Abstractions;
using NexTraceOne.IdentityAccess.Domain.Entities;
using NexTraceOne.IdentityAccess.Domain.ValueObjects;

namespace NexTraceOne.IdentityAccess.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repositório de usuários persistidos via EF Core.
/// Queries por email comparam o Value Object diretamente — o EF Core aplica
/// o ValueConverter registrado em HasConversion para gerar o parâmetro SQL
/// corretamente. Não usar .Value no LINQ (não traduz) nem EF.Property com
/// tipo divergente do converter (causa InvalidCastException no Sanitize).
/// </summary>
internal sealed class UserRepository(IdentityDbContext context)
    : RepositoryBase<User, UserId>(context), IUserRepository
{
    /// <inheritdoc />
    public async Task<User?> GetByEmailAsync(Email email, CancellationToken cancellationToken)
        => await context.Users.SingleOrDefaultAsync(x => x.Email == email, cancellationToken);

    /// <inheritdoc />
    public async Task<User?> GetByFederatedIdentityAsync(string provider, string externalId, CancellationToken cancellationToken)
        => await context.Users.SingleOrDefaultAsync(
            x => x.FederationProvider == provider && x.ExternalId == externalId,
            cancellationToken);

    /// <inheritdoc />
    public Task<bool> ExistsAsync(Email email, CancellationToken cancellationToken)
        => context.Users.AnyAsync(x => x.Email == email, cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<UserId, User>> GetByIdsAsync(IReadOnlyCollection<UserId> ids, CancellationToken cancellationToken)
        => await context.Users
            .Where(x => ids.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, cancellationToken);
}
