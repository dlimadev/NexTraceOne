using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Abstractions;
using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Entities;
using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Errors;

namespace NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.RecordReleaseBaseline;

/// <summary>
/// Feature: RecordReleaseBaseline — registra baseline de indicadores pré-release.
/// Estrutura VSA: Command + Validator + Handler + Response em um único arquivo.
/// </summary>
public static class RecordReleaseBaseline
{
    /// <summary>Comando para registrar baseline de indicadores pré-release.</summary>
    public sealed record Command(
        Guid ReleaseId,
        decimal RequestsPerMinute,
        decimal ErrorRate,
        decimal AvgLatencyMs,
        decimal P95LatencyMs,
        decimal P99LatencyMs,
        decimal Throughput,
        DateTimeOffset CollectedFrom,
        DateTimeOffset CollectedTo) : ICommand<Response>;

    /// <summary>Valida a entrada do comando de registro de baseline.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.ReleaseId).NotEmpty();
            RuleFor(x => x.RequestsPerMinute).GreaterThanOrEqualTo(0);
            RuleFor(x => x.ErrorRate).InclusiveBetween(0m, 1m);
            RuleFor(x => x.AvgLatencyMs).GreaterThanOrEqualTo(0);
            RuleFor(x => x.CollectedTo).GreaterThan(x => x.CollectedFrom)
                .WithMessage("Collection end must be after start.");
        }
    }

    /// <summary>
    /// Handler que registra baseline de indicadores antes do deploy.
    /// O baseline serve como referência para comparação before/after.
    /// </summary>
    public sealed class Handler(
        IReleaseRepository releaseRepository,
        IReleaseBaselineRepository baselineRepository,
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

            var baseline = ReleaseBaseline.Create(
                releaseId,
                request.RequestsPerMinute,
                request.ErrorRate,
                request.AvgLatencyMs,
                request.P95LatencyMs,
                request.P99LatencyMs,
                request.Throughput,
                request.CollectedFrom,
                request.CollectedTo,
                dateTimeProvider.UtcNow);

            baselineRepository.Add(baseline);
            await unitOfWork.CommitAsync(cancellationToken);

            return new Response(
                baseline.Id.Value,
                release.Id.Value,
                baseline.CapturedAt);
        }
    }

    /// <summary>Resposta do registro de baseline.</summary>
    public sealed record Response(
        Guid BaselineId,
        Guid ReleaseId,
        DateTimeOffset CapturedAt);
}
