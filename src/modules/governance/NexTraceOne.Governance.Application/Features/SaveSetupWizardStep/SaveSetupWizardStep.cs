using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Governance.Application.Abstractions;
using NexTraceOne.Governance.Domain.Entities;

namespace NexTraceOne.Governance.Application.Features.SaveSetupWizardStep;

/// <summary>
/// Feature: SaveSetupWizardStep — persiste ou actualiza a configuração de um passo do SetupWizard.
/// Cria o registo se não existir; actualiza se já existir.
/// F-04 — SetupWizard Persistence.
/// </summary>
public static class SaveSetupWizardStep
{
    public sealed record Command(
        string TenantId,
        string StepId,
        string DataJson) : ICommand<Response>;

    public sealed record Response(
        string StepId,
        DateTimeOffset SavedAt,
        bool IsNew);

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.TenantId).NotEmpty();
            RuleFor(x => x.StepId).NotEmpty().MaximumLength(50);
            RuleFor(x => x.DataJson).NotEmpty().MaximumLength(10_000);
        }
    }

    public sealed class Handler(
        ISetupWizardRepository repository,
        IGovernanceUnitOfWork unitOfWork,
        IDateTimeProvider clock) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            var now = clock.UtcNow;
            var existing = await repository.GetByStepIdAsync(
                request.TenantId, request.StepId, cancellationToken);

            if (existing is null)
            {
                var step = SetupWizardStep.Create(request.TenantId, request.StepId, request.DataJson, now);
                await repository.AddAsync(step, cancellationToken);
                await unitOfWork.CommitAsync(cancellationToken);

                return Result<Response>.Success(new Response(request.StepId, now, IsNew: true));
            }

            existing.Update(request.DataJson, now);
            await repository.UpdateAsync(existing, cancellationToken);
            await unitOfWork.CommitAsync(cancellationToken);

            return Result<Response>.Success(new Response(request.StepId, now, IsNew: false));
        }
    }
}
