using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Abstractions;
using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Entities;
using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Enums;
using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Errors;

namespace NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.RegisterExternalMarker;

/// <summary>
/// Feature: RegisterExternalMarker — registra um marcador externo de ferramenta CI/CD na timeline da release.
/// Estrutura VSA: Command + Validator + Handler + Response em um único arquivo.
/// </summary>
public static class RegisterExternalMarker
{
    /// <summary>Comando para registrar um marcador externo de ferramenta CI/CD.</summary>
    public sealed record Command(
        Guid ReleaseId,
        MarkerType MarkerType,
        string SourceSystem,
        string ExternalId,
        string? Payload,
        DateTimeOffset OccurredAt) : ICommand<Response>;

    /// <summary>Valida a entrada do comando de registro de marcador externo.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.ReleaseId).NotEmpty();
            RuleFor(x => x.SourceSystem).NotEmpty().MaximumLength(200);
            RuleFor(x => x.ExternalId).NotEmpty().MaximumLength(500);
            RuleFor(x => x.MarkerType).IsInEnum();
        }
    }

    /// <summary>
    /// Handler que registra um marcador externo na timeline da release.
    /// Enriquece a timeline com eventos reais do ciclo de deploy.
    /// </summary>
    public sealed class Handler(
        IReleaseRepository releaseRepository,
        IExternalMarkerRepository markerRepository,
        IChangeIntelligenceUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var releaseId = ReleaseId.From(request.ReleaseId);
            var release = await releaseRepository.GetByIdAsync(releaseId, cancellationToken);

            if (release is null)
                return ChangeIntelligenceErrors.ReleaseNotFound(request.ReleaseId.ToString());

            var marker = ExternalMarker.Create(
                releaseId,
                request.MarkerType,
                request.SourceSystem,
                request.ExternalId,
                request.Payload,
                request.OccurredAt,
                dateTimeProvider.UtcNow);

            markerRepository.Add(marker);
            await unitOfWork.CommitAsync(cancellationToken);

            return new Response(
                marker.Id.Value,
                release.Id.Value,
                marker.MarkerType,
                marker.SourceSystem,
                marker.OccurredAt);
        }
    }

    /// <summary>Resposta do registro de marcador externo.</summary>
    public sealed record Response(
        Guid MarkerId,
        Guid ReleaseId,
        MarkerType MarkerType,
        string SourceSystem,
        DateTimeOffset OccurredAt);
}
