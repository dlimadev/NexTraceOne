using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.ChangeIntelligence.Application.Abstractions;
using NexTraceOne.ChangeIntelligence.Domain.Entities;
using NexTraceOne.ChangeIntelligence.Domain.Errors;

namespace NexTraceOne.ChangeIntelligence.Application.Features.UpdateDeploymentState;

/// <summary>
/// Feature: UpdateDeploymentState — atualiza o status de deployment de uma Release.
/// Estrutura VSA: Command + Validator + Handler + Response em um único arquivo.
/// </summary>
public static class UpdateDeploymentState
{
    /// <summary>Comando de atualização do status de deployment de uma Release.</summary>
    public sealed record Command(Guid ReleaseId, string Status) : ICommand<Response>;

    /// <summary>Valida a entrada do comando de atualização de status de deployment.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.ReleaseId).NotEmpty();
            RuleFor(x => x.Status).NotEmpty();
        }
    }

    /// <summary>Handler que atualiza o status de deployment de uma Release.</summary>
    public sealed class Handler(
        IReleaseRepository repository,
        IUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var release = await repository.GetByIdAsync(ReleaseId.From(request.ReleaseId), cancellationToken);
            if (release is null)
                return ChangeIntelligenceErrors.ReleaseNotFound(request.ReleaseId.ToString());

            if (!Enum.TryParse<DeploymentStatus>(request.Status, ignoreCase: true, out var deploymentStatus))
                return ChangeIntelligenceErrors.InvalidDeploymentStatus(request.Status);

            var result = release.UpdateStatus(deploymentStatus);
            if (result.IsFailure)
                return result.Error;

            await unitOfWork.CommitAsync(cancellationToken);

            return new Response(release.Id.Value, release.Status.ToString(), dateTimeProvider.UtcNow);
        }
    }

    /// <summary>Resposta da atualização do status de deployment da Release.</summary>
    public sealed record Response(Guid ReleaseId, string Status, DateTimeOffset UpdatedAt);
}
