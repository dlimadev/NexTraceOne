using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Abstractions;
using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Enums;

namespace NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.RespondToApprovalRequest;

/// <summary>
/// Feature: RespondToApprovalRequest — processa a resposta de um sistema externo
/// ao pedido de aprovação de release.
///
/// O sistema externo chama POST /api/v1/releases/{id}/approvals/{token}/respond
/// com a decisão (Approved/Rejected) e o NexTraceOne valida o token e actualiza o estado.
///
/// Estrutura VSA: Command + Validator + Handler + Response num único ficheiro.
/// </summary>
public static class RespondToApprovalRequest
{
    /// <summary>Comando de resposta ao pedido de aprovação.</summary>
    public sealed record Command(
        /// <summary>Token de callback em plain text (recebido no URL).</summary>
        string CallbackToken,
        string Decision,
        string RespondedBy,
        string? Comments = null,
        string? ExternalChangeId = null) : ICommand<Response>;

    /// <summary>Valida a entrada do comando.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        private static readonly string[] ValidDecisions = ["Approved", "Rejected"];

        public Validator()
        {
            RuleFor(x => x.CallbackToken).NotEmpty();
            RuleFor(x => x.Decision).NotEmpty().Must(d => ValidDecisions.Contains(d, StringComparer.OrdinalIgnoreCase))
                .WithMessage("Decision must be 'Approved' or 'Rejected'.");
            RuleFor(x => x.RespondedBy).NotEmpty().MaximumLength(500);
            RuleFor(x => x.Comments).MaximumLength(2000).When(x => x.Comments is not null);
        }
    }

    /// <summary>
    /// Handler que valida o callback token, verifica expiração e regista a decisão.
    /// Operação idempotente: respostas duplicadas são ignoradas.
    /// </summary>
    public sealed class Handler(
        IApprovalRequestRepository repository,
        IChangeIntelligenceUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            // Deriva hash do token recebido para localização segura
            var tokenHash = ComputeSha256(request.CallbackToken);
            var approvalRequest = await repository.GetByCallbackTokenHashAsync(tokenHash, cancellationToken);

            if (approvalRequest is null)
                return Error.NotFound("APPROVAL_REQUEST_NOT_FOUND",
                    "No approval request found for the provided callback token.");

            var now = dateTimeProvider.UtcNow;

            // Verifica expiração
            if (approvalRequest.ExpireIfOverdue(now))
            {
                await unitOfWork.CommitAsync(cancellationToken);
                return Error.Validation("APPROVAL_TOKEN_EXPIRED",
                    "The callback token has expired. Please request a new approval.");
            }

            if (approvalRequest.Status != ApprovalRequestStatus.Pending)
                return new Response(approvalRequest.Id.Value, approvalRequest.Status.ToString(), false);

            var decision = request.Decision.Equals("Approved", StringComparison.OrdinalIgnoreCase)
                ? ApprovalRequestStatus.Approved
                : ApprovalRequestStatus.Rejected;

            approvalRequest.Respond(decision, request.RespondedBy, now, request.Comments, request.ExternalChangeId);
            await unitOfWork.CommitAsync(cancellationToken);

            return new Response(approvalRequest.Id.Value, approvalRequest.Status.ToString(), true);
        }

        private static string ComputeSha256(string input)
        {
            var bytes = System.Text.Encoding.UTF8.GetBytes(input);
            var hash = System.Security.Cryptography.SHA256.HashData(bytes);
            return Convert.ToHexString(hash).ToLowerInvariant();
        }
    }

    /// <summary>Resposta do processamento do callback de aprovação.</summary>
    public sealed record Response(
        Guid ApprovalRequestId,
        string Status,
        bool Updated);
}
