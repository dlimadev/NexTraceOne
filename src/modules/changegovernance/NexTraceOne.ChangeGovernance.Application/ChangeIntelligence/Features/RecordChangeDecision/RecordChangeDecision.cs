using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.ChangeIntelligence.Application.Abstractions;
using NexTraceOne.ChangeIntelligence.Domain.Entities;
using NexTraceOne.ChangeIntelligence.Domain.Errors;

namespace NexTraceOne.ChangeIntelligence.Application.Features.RecordChangeDecision;

/// <summary>
/// Feature: RecordChangeDecision — regista uma decisão de governança sobre uma mudança
/// (aprovação, rejeição ou aprovação condicional) como evento no histórico da release.
/// Estrutura VSA: Command + Validator + Handler + Response em um único arquivo.
/// </summary>
public static class RecordChangeDecision
{
    /// <summary>Comando para registar uma decisão de governança sobre uma mudança.</summary>
    public sealed record Command(
        Guid ReleaseId,
        string Decision,
        string DecidedBy,
        string Rationale,
        string? Conditions) : ICommand<Response>;

    /// <summary>Valida a entrada do comando de decisão.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        private static readonly string[] AllowedDecisions = ["Approved", "Rejected", "ApprovedConditionally"];

        public Validator()
        {
            RuleFor(x => x.ReleaseId).NotEmpty();
            RuleFor(x => x.Decision)
                .NotEmpty()
                .Must(d => AllowedDecisions.Contains(d))
                .WithMessage("Decision must be one of: Approved, Rejected, ApprovedConditionally.");
            RuleFor(x => x.DecidedBy).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Rationale).NotEmpty().MaximumLength(2000);
            RuleFor(x => x.Conditions).MaximumLength(2000);
        }
    }

    /// <summary>
    /// Handler que regista a decisão como ChangeEvent associado à release,
    /// persistindo através do IChangeEventRepository.
    /// </summary>
    public sealed class Handler(
        IReleaseRepository releaseRepository,
        IChangeEventRepository changeEventRepository,
        IUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var releaseId = ReleaseId.From(request.ReleaseId);
            var release = await releaseRepository.GetByIdAsync(releaseId, cancellationToken);

            if (release is null)
                return ChangeIntelligenceErrors.ReleaseNotFound(request.ReleaseId.ToString());

            var now = dateTimeProvider.UtcNow;

            var description = string.IsNullOrWhiteSpace(request.Conditions)
                ? $"Decision: {request.Decision}. Rationale: {request.Rationale}"
                : $"Decision: {request.Decision}. Rationale: {request.Rationale}. Conditions: {request.Conditions}";

            var changeEvent = ChangeEvent.Create(
                releaseId,
                $"governance_decision_{request.Decision.ToLowerInvariant()}",
                description,
                request.DecidedBy,
                now);

            changeEventRepository.Add(changeEvent);
            await unitOfWork.CommitAsync(cancellationToken);

            return new Response(
                changeEvent.Id.Value,
                release.Id.Value,
                request.Decision,
                request.DecidedBy,
                now);
        }
    }

    /// <summary>Resposta do registo de decisão de governança.</summary>
    public sealed record Response(
        Guid DecisionId,
        Guid ReleaseId,
        string Decision,
        string DecidedBy,
        DateTimeOffset DecidedAt);
}
