using FluentValidation;
using MediatR;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Governance.Application.Abstractions;
using NexTraceOne.Governance.Domain.Entities;
using NexTraceOne.Governance.Domain.Errors;

namespace NexTraceOne.Governance.Application.Features.PublishExecutiveBriefing;

/// <summary>
/// Feature: PublishExecutiveBriefing — publica um briefing executivo em estado Draft.
/// Transiciona o estado de Draft para Published.
///
/// Owner: módulo Governance.
/// Pilar: Operational Intelligence — publicação governada de briefings executivos.
/// Persona principal: Executive, Tech Lead.
/// </summary>
public static class PublishExecutiveBriefing
{
    /// <summary>Comando para publicar um executive briefing.</summary>
    public sealed record Command(Guid BriefingId) : ICommand<Unit>;

    /// <summary>Validação do comando de publicação.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.BriefingId).NotEmpty();
        }
    }

    /// <summary>Handler que publica o executive briefing (Draft → Published).</summary>
    public sealed class Handler(
        IExecutiveBriefingRepository repository,
        IUnitOfWork unitOfWork,
        IDateTimeProvider clock) : ICommandHandler<Command, Unit>
    {
        public async Task<Result<Unit>> Handle(Command request, CancellationToken cancellationToken)
        {
            var briefing = await repository.GetByIdAsync(
                new ExecutiveBriefingId(request.BriefingId), cancellationToken);

            if (briefing is null)
                return GovernanceBriefingErrors.BriefingNotFound(request.BriefingId.ToString());

            var publishResult = briefing.Publish(clock.UtcNow);
            if (!publishResult.IsSuccess)
                return publishResult;

            await repository.UpdateAsync(briefing, cancellationToken);
            await unitOfWork.CommitAsync(cancellationToken);

            return Result<Unit>.Success(Unit.Value);
        }
    }
}
