using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.ChangeIntelligence.Application.Abstractions;
using NexTraceOne.ChangeIntelligence.Domain.Entities;
using NexTraceOne.ChangeIntelligence.Domain.Errors;

namespace NexTraceOne.ChangeIntelligence.Application.Features.CalculateBlastRadius;

/// <summary>
/// Feature: CalculateBlastRadius — calcula o blast radius de uma Release a partir das listas de consumidores.
/// Estrutura VSA: Command + Validator + Handler + Response em um único arquivo.
/// </summary>
public static class CalculateBlastRadius
{
    /// <summary>Comando de cálculo de blast radius para uma Release.</summary>
    public sealed record Command(
        Guid ReleaseId,
        IReadOnlyList<string> DirectConsumers,
        IReadOnlyList<string> TransitiveConsumers) : ICommand<Response>;

    /// <summary>Valida a entrada do comando de cálculo de blast radius.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.ReleaseId).NotEmpty();
            RuleFor(x => x.DirectConsumers).NotNull();
            RuleFor(x => x.TransitiveConsumers).NotNull();
        }
    }

    /// <summary>Handler que calcula e persiste o blast radius de uma Release.</summary>
    public sealed class Handler(
        IReleaseRepository releaseRepository,
        IBlastRadiusRepository blastRadiusRepository,
        IUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var release = await releaseRepository.GetByIdAsync(ReleaseId.From(request.ReleaseId), cancellationToken);
            if (release is null)
                return ChangeIntelligenceErrors.ReleaseNotFound(request.ReleaseId.ToString());

            var report = BlastRadiusReport.Calculate(
                release.Id,
                release.ApiAssetId,
                request.DirectConsumers,
                request.TransitiveConsumers,
                dateTimeProvider.UtcNow);

            blastRadiusRepository.Add(report);
            await unitOfWork.CommitAsync(cancellationToken);

            return new Response(
                release.Id.Value,
                report.Id.Value,
                report.TotalAffectedConsumers,
                report.DirectConsumers,
                report.TransitiveConsumers,
                report.CalculatedAt);
        }
    }

    /// <summary>Resposta do cálculo de blast radius da Release.</summary>
    public sealed record Response(
        Guid ReleaseId,
        Guid BlastRadiusReportId,
        int TotalAffectedConsumers,
        IReadOnlyList<string> DirectConsumers,
        IReadOnlyList<string> TransitiveConsumers,
        DateTimeOffset CalculatedAt);
}
