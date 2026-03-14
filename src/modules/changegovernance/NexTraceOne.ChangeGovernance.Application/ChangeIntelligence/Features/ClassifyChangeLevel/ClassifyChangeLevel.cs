using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Enums;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.ChangeIntelligence.Application.Abstractions;
using NexTraceOne.ChangeIntelligence.Domain.Entities;
using NexTraceOne.ChangeIntelligence.Domain.Errors;

namespace NexTraceOne.ChangeIntelligence.Application.Features.ClassifyChangeLevel;

/// <summary>
/// Feature: ClassifyChangeLevel — classifica o nível de mudança de uma Release.
/// Estrutura VSA: Command + Validator + Handler + Response em um único arquivo.
/// </summary>
public static class ClassifyChangeLevel
{
    /// <summary>Comando de classificação do nível de mudança de uma Release.</summary>
    public sealed record Command(
        Guid ReleaseId,
        ChangeLevel ChangeLevel) : ICommand<Response>;

    /// <summary>Valida a entrada do comando de classificação de nível de mudança.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.ReleaseId).NotEmpty();
            RuleFor(x => x.ChangeLevel).IsInEnum();
        }
    }

    /// <summary>Handler que classifica o nível de mudança de uma Release.</summary>
    public sealed class Handler(
        IReleaseRepository repository,
        IUnitOfWork unitOfWork) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var release = await repository.GetByIdAsync(ReleaseId.From(request.ReleaseId), cancellationToken);
            if (release is null)
                return ChangeIntelligenceErrors.ReleaseNotFound(request.ReleaseId.ToString());

            release.Classify(request.ChangeLevel);
            await unitOfWork.CommitAsync(cancellationToken);

            return new Response(release.Id.Value, release.ChangeLevel);
        }
    }

    /// <summary>Resposta da classificação do nível de mudança da Release.</summary>
    public sealed record Response(Guid ReleaseId, ChangeLevel ChangeLevel);
}
