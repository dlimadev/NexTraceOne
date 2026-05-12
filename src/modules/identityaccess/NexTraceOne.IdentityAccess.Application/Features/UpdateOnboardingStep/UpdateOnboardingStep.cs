using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.IdentityAccess.Application.Abstractions;
using NexTraceOne.IdentityAccess.Domain.Entities;
using NexTraceOne.IdentityAccess.Domain.Enums;

namespace NexTraceOne.IdentityAccess.Application.Features.UpdateOnboardingStep;

/// <summary>Feature: UpdateOnboardingStep — marca um passo do wizard como concluído.</summary>
public static class UpdateOnboardingStep
{
    /// <summary>Comando para marcar um passo do wizard como concluído.</summary>
    public sealed record Command(string Step) : ICommand<Response>;

    /// <summary>Validação do comando.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Step).NotEmpty()
                .Must(s => Enum.TryParse<OnboardingStep>(s, true, out _))
                .WithMessage("Step inválido. Valores aceites: Install, FirstSignal, RegisterService, AddContract, SetupSlo.");
        }
    }

    /// <summary>Resposta indicando se o passo foi avançado e se o onboarding está completo.</summary>
    public sealed record Response(bool Advanced, bool IsCompleted);

    internal sealed class Handler(
        IOnboardingProgressRepository repository,
        IUnitOfWork unitOfWork,
        ICurrentTenant currentTenant,
        IDateTimeProvider clock) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var step = Enum.Parse<OnboardingStep>(request.Step, ignoreCase: true);
            var progress = await repository.GetByTenantAsync(currentTenant.Id, cancellationToken);

            // Cria registo de progresso se ainda não existir para este tenant
            if (progress is null)
            {
                progress = OnboardingProgress.Create(currentTenant.Id);
                await repository.AddAsync(progress, cancellationToken);
            }

            progress.AdvanceStep(step, clock.UtcNow);

            await repository.UpdateAsync(progress, cancellationToken);
            await unitOfWork.CommitAsync(cancellationToken);

            return new Response(true, progress.IsCompleted);
        }
    }
}
