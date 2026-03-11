using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Application.Pagination;
using NexTraceOne.BuildingBlocks.Domain.Results;
using NexTraceOne.Identity.Application.Abstractions;
using NexTraceOne.Identity.Domain.Entities;

namespace NexTraceOne.Identity.Application.Features.ListTenantUsers;

/// <summary>
/// Feature: ListTenantUsers — lista usuários vinculados a um tenant com paginação.
/// </summary>
public static class ListTenantUsers
{
    /// <summary>Query paginada para listagem de usuários por tenant.</summary>
    public sealed record Query(Guid TenantId, string? Search, int Page = 1, int PageSize = 20)
        : IQuery<PagedList<UserSummaryResponse>>, IPagedQuery;

    /// <summary>Valida a entrada da query paginada.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.TenantId).NotEmpty();
            RuleFor(x => x.Page).GreaterThan(0);
            RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        }
    }

    /// <summary>Handler que monta a listagem paginada de usuários do tenant.</summary>
    public sealed class Handler(
        ITenantMembershipRepository membershipRepository,
        IUserRepository userRepository,
        IRoleRepository roleRepository) : IQueryHandler<Query, PagedList<UserSummaryResponse>>
    {
        public async Task<Result<PagedList<UserSummaryResponse>>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var (memberships, totalCount) = await membershipRepository.ListByTenantAsync(
                TenantId.From(request.TenantId),
                request.Search,
                request.Page,
                request.PageSize,
                cancellationToken);

            var usersById = await userRepository.GetByIdsAsync(
                memberships.Select(x => x.UserId).Distinct().ToArray(),
                cancellationToken);

            var items = new List<UserSummaryResponse>(memberships.Count);

            foreach (var membership in memberships)
            {
                if (!usersById.TryGetValue(membership.UserId, out var user))
                {
                    continue;
                }

                var role = await roleRepository.GetByIdAsync(membership.RoleId, cancellationToken);
                items.Add(new UserSummaryResponse(
                    user.Id.Value,
                    user.Email.Value,
                    user.FullName.Value,
                    user.IsActive,
                    role?.Name ?? string.Empty));
            }

            return PagedList<UserSummaryResponse>.Create(items, totalCount, request.Page, request.PageSize);
        }
    }

    /// <summary>Resumo paginado de usuário do tenant.</summary>
    public sealed record UserSummaryResponse(Guid UserId, string Email, string FullName, bool IsActive, string RoleName);
}
