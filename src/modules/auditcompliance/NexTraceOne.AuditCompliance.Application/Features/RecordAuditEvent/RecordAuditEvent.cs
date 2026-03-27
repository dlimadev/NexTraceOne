using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.AuditCompliance.Application.Abstractions;
using NexTraceOne.AuditCompliance.Domain.Entities;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.AuditCompliance.Application.Features.RecordAuditEvent;

/// <summary>
/// Feature: RecordAuditEvent — registra um evento de auditoria com hash chain SHA-256.
/// </summary>
public static class RecordAuditEvent
{
    /// <summary>Comando de registo de evento de auditoria.</summary>
    public sealed record Command(
        string SourceModule,
        string ActionType,
        string ResourceId,
        string ResourceType,
        string PerformedBy,
        Guid TenantId,
        string? Payload = null,
        string? CorrelationId = null) : ICommand<Response>;

    /// <summary>Valida a entrada do comando.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.SourceModule).NotEmpty().MaximumLength(200);
            RuleFor(x => x.ActionType).NotEmpty().MaximumLength(200);
            RuleFor(x => x.ResourceId).NotEmpty().MaximumLength(500);
            RuleFor(x => x.ResourceType).NotEmpty().MaximumLength(200);
            RuleFor(x => x.PerformedBy).NotEmpty().MaximumLength(200);
            RuleFor(x => x.TenantId).NotEmpty();
            RuleFor(x => x.CorrelationId).MaximumLength(100)
                .When(x => !string.IsNullOrWhiteSpace(x.CorrelationId));
        }
    }

    /// <summary>Handler que registra o evento e o vincula à cadeia de hash.</summary>
    public sealed class Handler(
        IAuditEventRepository auditEventRepository,
        IAuditChainRepository auditChainRepository,
        IDateTimeProvider dateTimeProvider,
        IUnitOfWork unitOfWork) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var now = dateTimeProvider.UtcNow;
            var correlationId = string.IsNullOrWhiteSpace(request.CorrelationId)
                ? System.Diagnostics.Activity.Current?.TraceId.ToString()
                : request.CorrelationId;
            var auditEvent = AuditEvent.Record(
                request.SourceModule,
                request.ActionType,
                request.ResourceId,
                request.ResourceType,
                request.PerformedBy,
                now,
                request.TenantId,
                request.Payload,
                correlationId);

            var latestLink = await auditChainRepository.GetLatestLinkAsync(cancellationToken);
            var previousHash = latestLink?.CurrentHash ?? string.Empty;
            var sequenceNumber = (latestLink?.SequenceNumber ?? 0) + 1;

            var chainLink = AuditChainLink.Create(auditEvent, sequenceNumber, previousHash, now);
            auditEvent.LinkToChain(chainLink);

            auditEventRepository.Add(auditEvent);
            auditChainRepository.Add(chainLink);

            await unitOfWork.CommitAsync(cancellationToken);

            return new Response(auditEvent.Id.Value, chainLink.CurrentHash, chainLink.SequenceNumber);
        }
    }

    /// <summary>Resposta do registo de evento de auditoria.</summary>
    public sealed record Response(Guid AuditEventId, string ChainHash, long SequenceNumber);
}
