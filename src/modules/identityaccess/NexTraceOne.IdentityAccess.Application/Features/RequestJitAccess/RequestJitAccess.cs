using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Identity.Application.Abstractions;
using NexTraceOne.Identity.Domain.Entities;
using NexTraceOne.Identity.Domain.Errors;

namespace NexTraceOne.Identity.Application.Features.RequestJitAccess;

/// <summary>
/// Feature: RequestJitAccess — solicita acesso privilegiado temporário (Just-in-Time).
///
/// O solicitante indica a permissão desejada, o escopo específico e a justificativa.
/// A solicitação fica pendente até aprovação por um responsável ou expiração do prazo.
/// </summary>
public static class RequestJitAccess
{
    /// <summary>Comando para solicitação de acesso JIT.</summary>
    public sealed record Command(
        string PermissionCode,
        string Scope,
        string Justification) : ICommand<Response>;

    /// <summary>Valida a entrada da solicitação JIT.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.PermissionCode).NotEmpty().MaximumLength(256);
            RuleFor(x => x.Scope).NotEmpty().MaximumLength(1000);
            RuleFor(x => x.Justification).NotEmpty().MinimumLength(10).MaximumLength(2000);
        }
    }

    /// <summary>Handler que cria a solicitação de acesso JIT.</summary>
    public sealed class Handler(
        IJitAccessRepository jitAccessRepository,
        ISecurityEventRepository securityEventRepository,
        ICurrentUser currentUser,
        ICurrentTenant currentTenant,
        IDateTimeProvider dateTimeProvider) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            if (string.IsNullOrWhiteSpace(currentUser.Id))
                return IdentityErrors.NotAuthenticated();

            var userId = UserId.From(Guid.Parse(currentUser.Id));
            var tenantId = TenantId.From(currentTenant.Id);
            var now = dateTimeProvider.UtcNow;

            var jitRequest = JitAccessRequest.Create(
                userId,
                tenantId,
                request.PermissionCode,
                request.Scope,
                request.Justification,
                now);

            jitAccessRepository.Add(jitRequest);

            var securityEvent = SecurityEvent.Create(
                tenantId,
                userId,
                sessionId: null,
                SecurityEventType.JitAccessRequested,
                $"JIT access requested for permission '{request.PermissionCode}' by user {userId.Value}.",
                riskScore: 40,
                ipAddress: null,
                userAgent: null,
                metadataJson: null,
                now);

            securityEventRepository.Add(securityEvent);

            return new Response(
                jitRequest.Id.Value,
                jitRequest.ApprovalDeadline);
        }
    }

    /// <summary>Resposta da criação de solicitação JIT.</summary>
    public sealed record Response(
        Guid RequestId,
        DateTimeOffset ApprovalDeadline);
}
