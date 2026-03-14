using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Promotion.Application.Abstractions;
using NexTraceOne.Promotion.Domain.Entities;
using NexTraceOne.Promotion.Domain.Errors;

namespace NexTraceOne.Promotion.Application.Features.CreatePromotionRequest;

/// <summary>
/// Feature: CreatePromotionRequest — cria uma nova solicitação de promoção de release entre ambientes.
/// Estrutura VSA: Command + Validator + Handler + Response em um único arquivo.
/// </summary>
public static class CreatePromotionRequest
{
    /// <summary>Comando para criação de uma solicitação de promoção.</summary>
    public sealed record Command(
        Guid ReleaseId,
        Guid SourceEnvironmentId,
        Guid TargetEnvironmentId,
        string RequestedBy,
        string? Justification) : ICommand<Response>;

    /// <summary>Valida a entrada do comando de criação de solicitação de promoção.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.ReleaseId).NotEmpty();
            RuleFor(x => x.SourceEnvironmentId).NotEmpty();
            RuleFor(x => x.TargetEnvironmentId).NotEmpty();
            RuleFor(x => x.RequestedBy).NotEmpty().MaximumLength(500);
            RuleFor(x => x.Justification).MaximumLength(4000).When(x => x.Justification is not null);
        }
    }

    /// <summary>Handler que cria uma nova PromotionRequest e a persiste.</summary>
    public sealed class Handler(
        IPromotionRequestRepository requestRepository,
        IDeploymentEnvironmentRepository environmentRepository,
        IUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var sourceEnv = await environmentRepository.GetByIdAsync(
                DeploymentEnvironmentId.From(request.SourceEnvironmentId), cancellationToken);
            if (sourceEnv is null)
                return PromotionErrors.EnvironmentNotFound(request.SourceEnvironmentId.ToString());

            var targetEnv = await environmentRepository.GetByIdAsync(
                DeploymentEnvironmentId.From(request.TargetEnvironmentId), cancellationToken);
            if (targetEnv is null)
                return PromotionErrors.EnvironmentNotFound(request.TargetEnvironmentId.ToString());

            if (!targetEnv.IsActive)
                return PromotionErrors.EnvironmentNotFound(request.TargetEnvironmentId.ToString());

            var promotionRequest = PromotionRequest.Create(
                request.ReleaseId,
                sourceEnv.Id,
                targetEnv.Id,
                request.RequestedBy,
                dateTimeProvider.UtcNow);

            if (request.Justification is not null)
                promotionRequest.SetJustification(request.Justification);

            requestRepository.Add(promotionRequest);
            await unitOfWork.CommitAsync(cancellationToken);

            return new Response(promotionRequest.Id.Value, promotionRequest.Status.ToString());
        }
    }

    /// <summary>Resposta da criação da solicitação de promoção.</summary>
    public sealed record Response(Guid PromotionRequestId, string Status);
}

