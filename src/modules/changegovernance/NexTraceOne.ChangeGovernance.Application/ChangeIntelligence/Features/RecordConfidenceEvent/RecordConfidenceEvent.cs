using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Abstractions;
using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Entities;
using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Enums;
using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Errors;

namespace NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.RecordConfidenceEvent;

/// <summary>
/// Feature: RecordConfidenceEvent — regista um evento na timeline de confiança de uma release.
/// O handler obtém o score atual (ou 50 como default) e persiste o novo evento append-only.
/// Estrutura VSA: Command + Validator + Handler + Response em um único arquivo.
/// </summary>
public static class RecordConfidenceEvent
{
    /// <summary>Comando para registar um evento de confiança numa release.</summary>
    public sealed record Command(
        Guid ReleaseId,
        ConfidenceEventType EventType,
        int ConfidenceAfter,
        string Reason,
        string? Details,
        string Source) : ICommand<Response>;

    /// <summary>Valida a entrada do comando de evento de confiança.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.ReleaseId).NotEmpty();
            RuleFor(x => x.EventType).IsInEnum();
            RuleFor(x => x.ConfidenceAfter).InclusiveBetween(0, 100);
            RuleFor(x => x.Reason).NotEmpty().MaximumLength(2000);
            RuleFor(x => x.Details).MaximumLength(8000);
            RuleFor(x => x.Source).NotEmpty().MaximumLength(500);
        }
    }

    /// <summary>
    /// Handler que obtém o score actual da release, cria o evento de confiança
    /// e persiste no repositório append-only.
    /// </summary>
    public sealed class Handler(
        IReleaseRepository releaseRepository,
        IChangeConfidenceEventRepository confidenceEventRepository,
        IUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider) : ICommandHandler<Command, Response>
    {
        private const int DefaultConfidenceScore = 50;

        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var releaseId = ReleaseId.From(request.ReleaseId);
            var release = await releaseRepository.GetByIdAsync(releaseId, cancellationToken);

            if (release is null)
                return ChangeIntelligenceErrors.ReleaseNotFound(request.ReleaseId.ToString());

            var latest = await confidenceEventRepository.GetLatestByReleaseAsync(releaseId, cancellationToken);
            var confidenceBefore = latest?.ConfidenceAfter ?? DefaultConfidenceScore;

            var now = dateTimeProvider.UtcNow;

            var confidenceEvent = ChangeConfidenceEvent.Create(
                releaseId,
                request.EventType,
                confidenceBefore,
                request.ConfidenceAfter,
                request.Reason,
                request.Details,
                request.Source,
                now);

            await confidenceEventRepository.AddAsync(confidenceEvent, cancellationToken);
            await unitOfWork.CommitAsync(cancellationToken);

            return new Response(
                confidenceEvent.Id.Value,
                release.Id.Value,
                request.EventType,
                confidenceBefore,
                request.ConfidenceAfter,
                request.Reason,
                now);
        }
    }

    /// <summary>Resposta do registo de evento de confiança.</summary>
    public sealed record Response(
        Guid EventId,
        Guid ReleaseId,
        ConfidenceEventType EventType,
        int ConfidenceBefore,
        int ConfidenceAfter,
        string Reason,
        DateTimeOffset OccurredAt);
}
