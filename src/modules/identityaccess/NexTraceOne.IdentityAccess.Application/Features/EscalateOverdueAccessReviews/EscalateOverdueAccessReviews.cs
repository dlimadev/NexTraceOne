using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.IdentityAccess.Application.Abstractions;
using NexTraceOne.IdentityAccess.Domain.Entities;
using NexTraceOne.Notifications.Contracts.ServiceInterfaces;

namespace NexTraceOne.IdentityAccess.Application.Features.EscalateOverdueAccessReviews;

/// <summary>
/// Feature: EscalateOverdueAccessReviews — identifica campanhas de Access Review
/// próximas do prazo e envia notificações de escalação para os reviewers.
/// Chamado periodicamente pelo BackgroundWorker para alertar sobre revisões em atraso.
/// Idempotente por campanha: a escalação é controlada pelo campo EscalatedAt.
/// Wave C.2 — Compliance &amp; Access Governance.
/// </summary>
public static class EscalateOverdueAccessReviews
{
    public sealed record Command(int DaysBeforeDeadline = 3) : ICommand<Response>;

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator() =>
            RuleFor(x => x.DaysBeforeDeadline).InclusiveBetween(1, 30);
    }

    public sealed class Handler(
        IAccessReviewRepository reviewRepository,
        INotificationModule notificationModule,
        IDateTimeProvider clock) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var now = clock.UtcNow;
            var escalationWindowEnd = now.AddDays(request.DaysBeforeDeadline);

            var campaigns = await reviewRepository.ListOpenApproachingDeadlineAsync(
                now, escalationWindowEnd, cancellationToken);

            var escalated = 0;

            foreach (var campaign in campaigns)
            {
                var pendingCount = campaign.Items.Count(i =>
                    i.Decision == AccessReviewDecision.Pending);

                if (pendingCount == 0)
                    continue;

                var result = await notificationModule.SubmitAsync(new NotificationRequest
                {
                    EventType = "AccessReviewEscalation",
                    Category = "Security",
                    Severity = "Warning",
                    Title = $"Access review '{campaign.Name}' expires in {request.DaysBeforeDeadline} day(s)",
                    Message = $"Access review campaign '{campaign.Name}' has {pendingCount} pending item(s) and expires at {campaign.Deadline:yyyy-MM-dd HH:mm} UTC. Please complete the review to avoid auto-revocation.",
                    SourceModule = "IdentityAccess",
                    SourceEntityType = "AccessReviewCampaign",
                    SourceEntityId = campaign.Id.Value.ToString(),
                    RequiresAction = true,
                    TenantId = campaign.TenantId.Value,
                    ActionUrl = $"/admin/access-reviews/{campaign.Id.Value}",
                }, cancellationToken);

                if (result.Success)
                    escalated++;
            }

            return Result<Response>.Success(new Response(
                EscalatedCount: escalated,
                ReviewedAt: now));
        }
    }

    public sealed record Response(int EscalatedCount, DateTimeOffset ReviewedAt);
}
