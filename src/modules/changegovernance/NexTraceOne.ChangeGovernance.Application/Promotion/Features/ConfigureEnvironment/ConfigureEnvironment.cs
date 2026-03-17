using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.ChangeGovernance.Application.Promotion.Abstractions;
using NexTraceOne.ChangeGovernance.Domain.Promotion.Entities;
using NexTraceOne.ChangeGovernance.Domain.Promotion.Errors;

namespace NexTraceOne.ChangeGovernance.Application.Promotion.Features.ConfigureEnvironment;

/// <summary>
/// Feature: ConfigureEnvironment — cria e configura um novo ambiente de deployment no pipeline de promoção.
/// Estrutura VSA: Command + Validator + Handler + Response em um único arquivo.
/// </summary>
public static class ConfigureEnvironment
{
    /// <summary>Comando para configuração de um ambiente de deployment.</summary>
    public sealed record Command(
        string Name,
        string Description,
        int Order,
        bool RequiresApproval,
        bool RequiresEvidencePack) : ICommand<Response>;

    /// <summary>Valida a entrada do comando de configuração de ambiente.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Description).NotNull().MaximumLength(2000);
            RuleFor(x => x.Order).GreaterThanOrEqualTo(0);
        }
    }

    /// <summary>Handler que cria um novo DeploymentEnvironment e o persiste.</summary>
    public sealed class Handler(
        IDeploymentEnvironmentRepository environmentRepository,
        IUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var existing = await environmentRepository.GetByNameAsync(request.Name, cancellationToken);
            if (existing is not null)
                return PromotionErrors.DuplicateEnvironmentName(request.Name);

            var environment = DeploymentEnvironment.Create(
                request.Name,
                request.Description,
                request.Order,
                request.RequiresApproval,
                request.RequiresEvidencePack,
                dateTimeProvider.UtcNow);

            environmentRepository.Add(environment);
            await unitOfWork.CommitAsync(cancellationToken);

            return new Response(environment.Id.Value, environment.Name, environment.Order, environment.IsActive);
        }
    }

    /// <summary>Resposta da configuração do ambiente de deployment.</summary>
    public sealed record Response(Guid EnvironmentId, string Name, int Order, bool IsActive);
}
