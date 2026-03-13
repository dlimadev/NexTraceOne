using Ardalis.GuardClauses;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Domain.Results;
using NexTraceOne.Identity.Application.Abstractions;
using NexTraceOne.Identity.Domain.Entities;
using NexTraceOne.Identity.Domain.Errors;

namespace NexTraceOne.Identity.Application.Features.GetCurrentUser;

/// <summary>
/// Feature: GetCurrentUser — obtém o perfil do usuário autenticado atual (/me).
/// </summary>
public static class GetCurrentUser
{
    /// <summary>Query sem parâmetros; o usuário é resolvido do contexto.</summary>
    public sealed record Query : IQuery<Response>;

    /// <summary>Handler que obtém dados do usuário autenticado a partir do ICurrentUser.</summary>
    public sealed class Handler(
        ICurrentUser currentUser,
        IUserRepository userRepository,
        IRoleRepository roleRepository,
        ILoginResponseBuilder responseBuilder) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            if (!currentUser.IsAuthenticated || !Guid.TryParse(currentUser.Id, out var userId))
            {
                return IdentityErrors.NotAuthenticated();
            }

            var user = await userRepository.GetByIdAsync(UserId.From(userId), cancellationToken);
            if (user is null)
            {
                return IdentityErrors.UserNotFound(userId);
            }

            var membership = await responseBuilder.ResolveMembershipAsync(user.Id, cancellationToken);

            string roleName = string.Empty;
            IReadOnlyList<string> permissions = [];

            if (membership is not null)
            {
                var role = await roleRepository.GetByIdAsync(membership.RoleId, cancellationToken);
                if (role is not null)
                {
                    roleName = role.Name;
                    permissions = Role.GetPermissionsForRole(role.Name);
                }
            }

            return new Response(
                user.Id.Value,
                user.Email.Value,
                user.FullName.FirstName,
                user.FullName.LastName,
                user.FullName.Value,
                user.IsActive,
                user.LastLoginAt,
                membership?.TenantId.Value ?? Guid.Empty,
                roleName,
                permissions);
        }
    }

    /// <summary>Resposta do perfil do usuário autenticado.</summary>
    public sealed record Response(
        Guid Id,
        string Email,
        string FirstName,
        string LastName,
        string FullName,
        bool IsActive,
        DateTimeOffset? LastLoginAt,
        Guid TenantId,
        string RoleName,
        IReadOnlyList<string> Permissions);
}
