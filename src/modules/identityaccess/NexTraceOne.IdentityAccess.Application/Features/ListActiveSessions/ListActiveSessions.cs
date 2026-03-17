using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.IdentityAccess.Application.Abstractions;
using NexTraceOne.IdentityAccess.Domain.Entities;
using NexTraceOne.IdentityAccess.Domain.Errors;

namespace NexTraceOne.IdentityAccess.Application.Features.ListActiveSessions;

/// <summary>
/// Feature: ListActiveSessions — lista as sessões ativas de um usuário.
/// Utilizada por administradores para visualizar e gerenciar sessões abertas.
/// </summary>
public static class ListActiveSessions
{
    /// <summary>Query para listar sessões ativas de um usuário.</summary>
    public sealed record Query(Guid UserId) : IQuery<IReadOnlyList<SessionResponse>>;

    /// <summary>Valida a entrada da query de sessões.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.UserId).NotEmpty();
        }
    }

    /// <summary>Handler que retorna sessões ativas do usuário.</summary>
    public sealed class Handler(
        IUserRepository userRepository,
        ISessionRepository sessionRepository) : IQueryHandler<Query, IReadOnlyList<SessionResponse>>
    {
        public async Task<Result<IReadOnlyList<SessionResponse>>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var user = await userRepository.GetByIdAsync(UserId.From(request.UserId), cancellationToken);
            if (user is null)
            {
                return IdentityErrors.UserNotFound(request.UserId);
            }

            var sessions = await sessionRepository.ListActiveByUserIdAsync(user.Id, cancellationToken);

            var result = sessions.Select(s => new SessionResponse(
                s.Id.Value,
                s.CreatedByIp,
                s.UserAgent,
                s.ExpiresAt)).ToList();

            return result;
        }
    }

    /// <summary>Resumo de uma sessão ativa.</summary>
    public sealed record SessionResponse(
        Guid SessionId,
        string IpAddress,
        string UserAgent,
        DateTimeOffset ExpiresAt);
}
