using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.IdentityAccess.Application.Abstractions;
using NexTraceOne.IdentityAccess.Domain.Entities;
using NexTraceOne.IdentityAccess.Domain.Errors;

namespace NexTraceOne.IdentityAccess.Application.Features.StartAccessReviewCampaign;

/// <summary>
/// Feature: StartAccessReviewCampaign — cria uma campanha de recertificação de acessos para um tenant.
///
/// Fluxo:
/// 1. Admin ou processo automático inicia a campanha com nome e janela de revisão.
/// 2. Para cada membro ativo do tenant, adiciona um item de revisão com o reviewer designado.
/// 3. Gera SecurityEvent de auditoria registrando o início da campanha.
/// 4. O job periódico irá checar prazos e auto-revogar itens não revisados.
///
/// Pré-condições:
/// - Tenant deve ter ao menos 1 membro ativo.
/// - O nome da campanha deve ser único dentro do tenant (não validado aqui para simplicidade).
/// - Prazo mínimo de 1 dia, máximo de 90 dias.
/// </summary>
public static class StartAccessReviewCampaign
{
    /// <summary>Comando para iniciar uma campanha de revisão de acessos.</summary>
    public sealed record Command(
        string Name,
        int ReviewWindowDays = 14,
        Guid? ReviewerId = null) : ICommand<Response>;

    /// <summary>Resposta com os dados da campanha criada.</summary>
    public sealed record Response(
        Guid CampaignId,
        string Name,
        DateTimeOffset Deadline,
        int ItemCount);

    /// <summary>Valida os dados da campanha.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
            RuleFor(x => x.ReviewWindowDays).InclusiveBetween(1, 90);
        }
    }

    /// <summary>
    /// Handler que cria a campanha e adiciona itens para todos os membros ativos do tenant.
    /// Usa o caller como reviewer padrão se nenhum reviewer específico for designado.
    /// </summary>
    public sealed class Handler(
        ICurrentUser currentUser,
        ICurrentTenant currentTenant,
        ITenantMembershipRepository membershipRepository,
        IRoleRepository roleRepository,
        IAccessReviewRepository accessReviewRepository,
        ISecurityEventRepository securityEventRepository,
        ISecurityEventTracker securityEventTracker,
        IDateTimeProvider dateTimeProvider) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            if (!currentUser.IsAuthenticated || !Guid.TryParse(currentUser.Id, out var initiatorId))
            {
                return IdentityErrors.NotAuthenticated();
            }

            if (currentTenant.Id == Guid.Empty)
            {
                return IdentityErrors.TenantContextRequired();
            }

            var tenantId = TenantId.From(currentTenant.Id);
            var initiatorUserId = UserId.From(initiatorId);

            // Carrega todos os vínculos ativos do tenant para popular os itens da campanha
            var activeMemberships = await membershipRepository.ListAllActiveByTenantAsync(tenantId, cancellationToken);

            var campaign = AccessReviewCampaign.Create(
                tenantId,
                request.Name,
                initiatorUserId,
                dateTimeProvider.UtcNow,
                TimeSpan.FromDays(request.ReviewWindowDays));

            // Designa o caller como reviewer padrão ou usa o reviewer específico passado
            var defaultReviewerId = request.ReviewerId.HasValue
                ? UserId.From(request.ReviewerId.Value)
                : initiatorUserId;

            foreach (var membership in activeMemberships)
            {
                var role = await roleRepository.GetByIdAsync(membership.RoleId, cancellationToken);
                var roleName = role?.Name ?? membership.RoleId.Value.ToString();

                campaign.AddItem(
                    membership.UserId,
                    membership.RoleId,
                    roleName,
                    defaultReviewerId);
            }

            accessReviewRepository.Add(campaign);

            // Registra evento de segurança para trilha de auditoria obrigatória
            var securityEvent = SecurityEvent.Create(
                tenantId,
                initiatorUserId,
                sessionId: null,
                SecurityEventType.AccessReviewStarted,
                $"Access review campaign '{request.Name}' started with {activeMemberships.Count} items. Deadline: {campaign.Deadline:O}.",
                riskScore: 5,
                ipAddress: null,
                userAgent: null,
                $"{{\"campaignId\":\"{campaign.Id.Value}\",\"itemCount\":{activeMemberships.Count},\"windowDays\":{request.ReviewWindowDays}}}",
                dateTimeProvider.UtcNow);
            securityEventRepository.Add(securityEvent);
            securityEventTracker.Track(securityEvent);

            return new Response(
                campaign.Id.Value,
                campaign.Name,
                campaign.Deadline,
                activeMemberships.Count);
        }
    }
}
