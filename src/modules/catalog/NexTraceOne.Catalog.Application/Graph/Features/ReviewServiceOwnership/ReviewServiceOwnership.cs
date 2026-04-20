using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.Graph.Abstractions;
using NexTraceOne.Catalog.Domain.Graph.Entities;
using NexTraceOne.Catalog.Domain.Graph.Errors;

namespace NexTraceOne.Catalog.Application.Graph.Features.ReviewServiceOwnership;

/// <summary>
/// Feature: ReviewServiceOwnership — regista que o ownership do serviço foi revisto.
/// Atualiza LastOwnershipReviewAt para suprimir alertas de drift pelo período de threshold configurado.
/// Deve ser chamado quando um utilizador confirma/atualiza dados de ownership.
/// Estrutura VSA: Command + Validator + Handler + Response em um único arquivo.
/// </summary>
public static class ReviewServiceOwnership
{
    /// <summary>Comando para registar revisão de ownership.</summary>
    public sealed record Command(Guid ServiceId) : ICommand<Response>;

    /// <summary>Valida o comando ReviewServiceOwnership.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.ServiceId).NotEmpty();
        }
    }

    /// <summary>Handler que regista a revisão de ownership do serviço.</summary>
    public sealed class Handler(
        IServiceAssetRepository serviceAssetRepository,
        ICatalogGraphUnitOfWork unitOfWork,
        IDateTimeProvider clock) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var service = await serviceAssetRepository.GetByIdAsync(
                ServiceAssetId.From(request.ServiceId), cancellationToken);
            if (service is null)
                return CatalogGraphErrors.ServiceAssetNotFoundById(request.ServiceId);

            var reviewedAt = clock.UtcNow;
            service.RecordOwnershipReview(reviewedAt);
            await unitOfWork.CommitAsync(cancellationToken);

            return new Response(service.Id.Value, service.Name, reviewedAt);
        }
    }

    /// <summary>Resposta do comando ReviewServiceOwnership.</summary>
    public sealed record Response(Guid ServiceId, string ServiceName, DateTimeOffset ReviewedAt);
}
