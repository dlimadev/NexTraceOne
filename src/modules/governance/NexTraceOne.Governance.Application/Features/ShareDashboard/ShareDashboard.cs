using FluentValidation;
using MediatR;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Governance.Application.Abstractions;
using NexTraceOne.Governance.Domain.Entities;
using NexTraceOne.Governance.Domain.Enums;

namespace NexTraceOne.Governance.Application.Features.ShareDashboard;

/// <summary>
/// Feature: ShareDashboard — define a política de partilha granular de um dashboard.
/// Substitui o legacy SetShared(bool) com controlo fino de âmbito e permissões.
/// V3.1 — Dashboard Intelligence Foundation.
/// </summary>
public static class ShareDashboard
{
    /// <summary>Comando para partilhar um dashboard com política granular.</summary>
    public sealed record Command(
        Guid DashboardId,
        string TenantId,
        string UserId,
        DashboardSharingScope Scope,
        DashboardSharingPermission Permission,
        DateTimeOffset? SignedLinkExpiresAt = null) : ICommand<Response>;

    /// <summary>Resposta com a política de partilha aplicada.</summary>
    public sealed record Response(
        Guid DashboardId,
        DashboardSharingScope Scope,
        DashboardSharingPermission Permission,
        bool IsVisible,
        DateTimeOffset? SignedLinkExpiresAt);

    /// <summary>Validação do comando.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.DashboardId).NotEmpty();
            RuleFor(x => x.TenantId).NotEmpty();
            RuleFor(x => x.UserId).NotEmpty();
            RuleFor(x => x.Scope).IsInEnum();
            RuleFor(x => x.Permission).IsInEnum();
            RuleFor(x => x.SignedLinkExpiresAt)
                .Must(exp => exp is null || exp > DateTimeOffset.UtcNow)
                .When(x => x.Scope == DashboardSharingScope.PublicLink)
                .WithMessage("PublicLink expiry must be in the future.");
        }
    }

    /// <summary>Handler que define a política de partilha.</summary>
    public sealed class Handler(
        ICustomDashboardRepository repository,
        IGovernanceUnitOfWork unitOfWork,
        IDateTimeProvider clock) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            var dashboard = await repository.GetByIdAsync(
                new CustomDashboardId(request.DashboardId), cancellationToken);

            if (dashboard is null)
                return Error.NotFound(
                    "CustomDashboard.NotFound",
                    "Custom dashboard with ID '{0}' was not found.",
                    request.DashboardId);

            if (dashboard.TenantId != request.TenantId)
                return Error.Forbidden(
                    "CustomDashboard.Forbidden",
                    "Access to dashboard '{0}' is not allowed.",
                    request.DashboardId);

            if (dashboard.IsSystem && request.Scope == DashboardSharingScope.PublicLink)
                return Error.Business(
                    "CustomDashboard.SystemDashboardPublicLinkNotAllowed",
                    "System dashboards cannot be shared via public links.");

            var policy = new SharingPolicy(
                request.Scope,
                request.Permission,
                request.SignedLinkExpiresAt);

            dashboard.SetSharingPolicy(policy, clock.UtcNow);

            await repository.UpdateAsync(dashboard, cancellationToken);
            await unitOfWork.CommitAsync(cancellationToken);

            return Result<Response>.Success(new Response(
                request.DashboardId,
                policy.Scope,
                policy.Permission,
                policy.IsVisible,
                policy.SignedLinkExpiresAt));
        }
    }
}
