using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Abstractions;
using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Entities;
using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Enums;

namespace NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.RequestExternalApproval;

/// <summary>
/// Feature: RequestExternalApproval — cria um pedido de aprovação de release e,
/// quando o tipo é ExternalWebhook, envia um webhook outbound para o sistema externo.
///
/// O fluxo é: NexTraceOne cria ReleaseApprovalRequest → gera CallbackToken → envia webhook outbound
/// → sistema externo responde via POST /api/v1/releases/{id}/approvals/{token}/respond.
///
/// Estrutura VSA: Command + Validator + Handler + Response num único ficheiro.
/// </summary>
public static class RequestExternalApproval
{
    /// <summary>Comando para criar um pedido de aprovação de release.</summary>
    public sealed record Command(
        Guid ReleaseId,
        string ApprovalType,
        string TargetEnvironment,
        string? ExternalSystem = null,
        string? WebhookUrl = null,
        int TokenExpiryHours = 48) : ICommand<Response>;

    /// <summary>Valida a entrada do comando.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.ReleaseId).NotEmpty();
            RuleFor(x => x.ApprovalType).NotEmpty();
            RuleFor(x => x.TargetEnvironment).NotEmpty().MaximumLength(200);
            RuleFor(x => x.TokenExpiryHours).InclusiveBetween(1, 168); // max 1 semana
            RuleFor(x => x.WebhookUrl)
                .NotEmpty()
                .Must(url => Uri.TryCreate(url, UriKind.Absolute, out _))
                .When(x => x.ApprovalType == "ExternalWebhook")
                .WithMessage("A valid webhook URL is required for ExternalWebhook approval type.");
        }
    }

    /// <summary>
    /// Handler que cria o ReleaseApprovalRequest, gera o callback token
    /// e envia o webhook outbound quando o tipo for ExternalWebhook.
    /// </summary>
    public sealed class Handler(
        IReleaseRepository releaseRepository,
        IApprovalRequestRepository approvalRepository,
        IExternalApprovalWebhookSender webhookSender,
        ICurrentTenant currentTenant,
        IChangeIntelligenceUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var releaseId = ReleaseId.From(request.ReleaseId);
            var release = await releaseRepository.GetByIdAsync(releaseId, cancellationToken);
            if (release is null)
                return Error.NotFound("RELEASE_NOT_FOUND", $"Release '{request.ReleaseId}' not found.");

            if (!Enum.TryParse<ExternalApprovalType>(request.ApprovalType, ignoreCase: true, out var approvalType))
                return Error.Validation("INVALID_APPROVAL_TYPE",
                    $"Unknown approval type '{request.ApprovalType}'. Valid values: Internal, ExternalWebhook, ServiceNow, AutoApprove.");

            var tenantId = currentTenant.Id;
            var now = dateTimeProvider.UtcNow;

            // Gera token UUID v4 e armazena apenas o hash SHA-256
            var callbackToken = Guid.NewGuid().ToString("N");
            var tokenHash = ComputeSha256(callbackToken);
            var expiresAt = now.AddHours(request.TokenExpiryHours);

            var approvalRequest = ReleaseApprovalRequest.Create(
                tenantId: tenantId,
                releaseId: releaseId,
                approvalType: approvalType,
                targetEnvironment: request.TargetEnvironment,
                callbackTokenHash: tokenHash,
                callbackTokenExpiresAt: expiresAt,
                requestedAt: now,
                externalSystem: request.ExternalSystem,
                outboundWebhookUrl: request.WebhookUrl);

            approvalRepository.Add(approvalRequest);
            await unitOfWork.CommitAsync(cancellationToken);

            bool webhookSent = false;
            if (approvalType == ExternalApprovalType.ExternalWebhook && !string.IsNullOrWhiteSpace(request.WebhookUrl))
            {
                var callbackUrl = $"/api/v1/releases/{request.ReleaseId}/approvals/{callbackToken}/respond";
                var payload = new ApprovalWebhookPayload(
                    CallbackUrl: callbackUrl,
                    ApprovalRequestId: approvalRequest.Id.Value.ToString(),
                    ReleaseId: request.ReleaseId.ToString(),
                    ServiceName: release.ServiceName,
                    Version: release.Version,
                    TargetEnvironment: request.TargetEnvironment,
                    RiskScore: (double)release.ChangeScore,
                    ImpactSummary: $"Service: {release.ServiceName} v{release.Version} → {request.TargetEnvironment}",
                    ExpiresAt: expiresAt);

                webhookSent = await webhookSender.SendAsync(request.WebhookUrl, payload, cancellationToken);
            }

            // Para AutoApprove: aprova imediatamente
            if (approvalType == ExternalApprovalType.AutoApprove)
            {
                approvalRequest.Respond(ApprovalRequestStatus.Approved, "system:auto", now, "Auto-approved by policy.");
                await unitOfWork.CommitAsync(cancellationToken);
            }

            return new Response(
                ApprovalRequestId: approvalRequest.Id.Value,
                ReleaseId: request.ReleaseId,
                Status: approvalRequest.Status.ToString(),
                CallbackTokenPlainText: approvalType == ExternalApprovalType.ExternalWebhook ? callbackToken : null,
                ExpiresAt: expiresAt,
                WebhookSent: webhookSent);
        }

        private static string ComputeSha256(string input)
        {
            var bytes = System.Text.Encoding.UTF8.GetBytes(input);
            var hash = System.Security.Cryptography.SHA256.HashData(bytes);
            return Convert.ToHexString(hash).ToLowerInvariant();
        }
    }

    /// <summary>Resposta da criação do pedido de aprovação.</summary>
    public sealed record Response(
        Guid ApprovalRequestId,
        Guid ReleaseId,
        string Status,
        /// <summary>Plain text token retornado apenas para integração external webhook. Não armazenado.</summary>
        string? CallbackTokenPlainText,
        DateTimeOffset ExpiresAt,
        bool WebhookSent);
}
