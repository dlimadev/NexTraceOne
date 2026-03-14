using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Identity.Application.Abstractions;
using NexTraceOne.Identity.Domain.Entities;
using NexTraceOne.Identity.Domain.Errors;

namespace NexTraceOne.Identity.Application.Features.GetUserProfile;

/// <summary>
/// Feature: GetUserProfile — obtém o perfil completo de um usuário.
/// </summary>
public static class GetUserProfile
{
    /// <summary>Query de obtenção de perfil por identificador.</summary>
    public sealed record Query(Guid UserId) : IQuery<Response>;

    /// <summary>Valida a entrada da query de perfil.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.UserId).NotEmpty();
        }
    }

    /// <summary>Handler que obtém dados do usuário e seus vínculos.</summary>
    public sealed class Handler(
        IUserRepository userRepository,
        ITenantMembershipRepository membershipRepository,
        IRoleRepository roleRepository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var user = await userRepository.GetByIdAsync(UserId.From(request.UserId), cancellationToken);
            if (user is null)
            {
                return IdentityErrors.UserNotFound(request.UserId);
            }

            var memberships = await membershipRepository.ListByUserAsync(user.Id, cancellationToken);
            var membershipResponses = new List<MembershipResponse>(memberships.Count);

            foreach (var membership in memberships)
            {
                var role = await roleRepository.GetByIdAsync(membership.RoleId, cancellationToken);
                membershipResponses.Add(new MembershipResponse(
                    membership.TenantId.Value,
                    membership.RoleId.Value,
                    role?.Name ?? string.Empty,
                    membership.IsActive));
            }

            return new Response(
                user.Id.Value,
                user.Email.Value,
                user.FullName.FirstName,
                user.FullName.LastName,
                user.IsActive,
                user.LastLoginAt,
                membershipResponses);
        }
    }

    /// <summary>Resposta do perfil do usuário.</summary>
    public sealed record Response(
        Guid Id,
        string Email,
        string FirstName,
        string LastName,
        bool IsActive,
        DateTimeOffset? LastLoginAt,
        IReadOnlyList<MembershipResponse> Memberships);

    /// <summary>Resumo de vínculo do usuário com tenants.</summary>
    public sealed record MembershipResponse(Guid TenantId, Guid RoleId, string RoleName, bool IsActive);
}
