using Ardalis.GuardClauses;

using FluentValidation;
using System.Text.Json;

using MediatR;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.IdentityAccess.Application.Abstractions;
using NexTraceOne.IdentityAccess.Domain.Entities;
using NexTraceOne.IdentityAccess.Domain.Errors;

namespace NexTraceOne.IdentityAccess.Application.Features.DecideAccessReviewItem;

/// <summary>
/// Feature: DecideAccessReviewItem — registra a decisão de um reviewer sobre um item de revisão de acesso.
///
/// Fluxo:
/// 1. Reviewer recebe lista de itens pendentes da campanha.
/// 2. Para cada item, confirma o acesso (Confirmed) ou revoga (Revoked) com comentário opcional.
/// 3. Após decisão de todos os itens, campanha pode ser fechada automaticamente.
/// 4. Decisão de revogação gera SecurityEvent para trilha de auditoria.
/// 5. Se todos os itens foram revisados, a campanha é encerrada automaticamente.
///
/// Restrições de segurança:
/// - Apenas o reviewer designado pode decidir sobre o item.
/// - Itens já decididos não podem ser re-decididos.
/// - Decisão de revogação real do acesso (ajuste de role) é feita fora desta feature
///   pelo workflow de aprovação — esta feature registra apenas a intenção.
/// </summary>
public static class DecideAccessReviewItem
{
    /// <summary>Comando com a decisão do reviewer sobre o item.</summary>
    public sealed record Command(
        Guid CampaignId,
        Guid ItemId,
        bool Approve,
        string? Comment = null) : ICommand;

    /// <summary>Valida os parâmetros do comando de decisão.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.CampaignId).NotEmpty();
            RuleFor(x => x.ItemId).NotEmpty();
            RuleFor(x => x.Comment).MaximumLength(500).When(x => x.Comment is not null);
        }
    }

    /// <summary>
    /// Handler que registra a decisão sobre o item de revisão.
    /// Valida que o caller é o reviewer designado e que o item ainda está pendente.
    /// </summary>
    public sealed class Handler(
        ICurrentUser currentUser,
        ICurrentTenant currentTenant,
        IAccessReviewRepository accessReviewRepository,
        ISecurityEventRepository securityEventRepository,
        ISecurityEventTracker securityEventTracker,
        IDateTimeProvider dateTimeProvider) : ICommandHandler<Command>
    {
        public async Task<Result<Unit>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            if (!currentUser.IsAuthenticated || !Guid.TryParse(currentUser.Id, out var reviewerId))
            {
                return IdentityErrors.NotAuthenticated();
            }

            var campaign = await accessReviewRepository.GetByIdWithItemsAsync(
                AccessReviewCampaignId.From(request.CampaignId),
                cancellationToken);

            if (campaign is null)
            {
                return IdentityErrors.AccessReviewCampaignNotFound(request.CampaignId);
            }

            // Garantia de isolamento por tenant
            if (campaign.TenantId.Value != currentTenant.Id)
            {
                return IdentityErrors.AccessReviewCampaignNotFound(request.CampaignId);
            }

            var item = campaign.Items.FirstOrDefault(i => i.Id.Value == request.ItemId);
            if (item is null)
            {
                return IdentityErrors.AccessReviewItemNotFound(request.ItemId);
            }

            // Verifica que o caller é o reviewer designado para este item
            if (item.ReviewerId.Value != reviewerId)
            {
                return IdentityErrors.Forbidden();
            }

            // Item já decidido não pode ser re-decidido para preservar a integridade da trilha
            if (item.Decision != AccessReviewDecision.Pending)
            {
                return IdentityErrors.AccessReviewItemAlreadyDecided(request.ItemId);
            }

            var reviewerUserId = UserId.From(reviewerId);
            var now = dateTimeProvider.UtcNow;

            if (request.Approve)
            {
                item.Confirm(reviewerUserId, request.Comment, now);

                // Acesso confirmado — evento informativo de baixo risco
                RecordReviewEvent(
                    campaign.TenantId,
                    item.UserId,
                    request.ItemId,
                    SecurityEventType.AccessReviewItemApproved,
                    $"Access review item approved for user '{item.UserId.Value}' with role '{item.RoleName}'. Reviewer: '{reviewerId}'.",
                    riskScore: 5);
            }
            else
            {
                item.Revoke(reviewerUserId, request.Comment, now);

                // Acesso revogado — evento de risco moderado pois altera os privilégios do usuário
                RecordReviewEvent(
                    campaign.TenantId,
                    item.UserId,
                    request.ItemId,
                    SecurityEventType.AccessReviewItemRevoked,
                    $"Access review item revoked for user '{item.UserId.Value}' with role '{item.RoleName}'. Reviewer: '{reviewerId}'.",
                    riskScore: 25);
            }

            // Verifica se a campanha pode ser encerrada após esta decisão
            campaign.TryComplete(now);

            return Unit.Value;
        }

        private void RecordReviewEvent(
            TenantId tenantId,
            UserId affectedUserId,
            Guid itemId,
            string eventType,
            string description,
            int riskScore)
        {
            var securityEvent = SecurityEvent.Create(
                tenantId,
                affectedUserId,
                sessionId: null,
                eventType,
                description,
                riskScore,
                ipAddress: null,
                userAgent: null,
                JsonSerializer.Serialize(new { itemId }),
                dateTimeProvider.UtcNow);
            securityEventRepository.Add(securityEvent);
            securityEventTracker.Track(securityEvent);
        }
    }
}
