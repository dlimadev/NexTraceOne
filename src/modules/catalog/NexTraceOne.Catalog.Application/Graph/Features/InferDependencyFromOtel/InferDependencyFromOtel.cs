using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Domain.Results;
using NexTraceOne.EngineeringGraph.Application.Abstractions;
using NexTraceOne.EngineeringGraph.Domain.Entities;
using NexTraceOne.EngineeringGraph.Domain.Errors;

namespace NexTraceOne.EngineeringGraph.Application.Features.InferDependencyFromOtel;

/// <summary>
/// Feature: InferDependencyFromOtel — infere dependência a partir de telemetria OpenTelemetry.
/// </summary>
public static class InferDependencyFromOtel
{
    /// <summary>Comando de inferência de dependência via OTel.</summary>
    public sealed record Command(
        Guid ApiAssetId,
        string ConsumerName,
        string Environment,
        string ExternalReference,
        decimal ConfidenceScore) : ICommand<Response>;

    /// <summary>Valida a entrada do comando de inferência.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.ApiAssetId).NotEmpty();
            RuleFor(x => x.ConsumerName).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Environment).NotEmpty().MaximumLength(100);
            RuleFor(x => x.ExternalReference).NotEmpty().MaximumLength(500);
            RuleFor(x => x.ConfidenceScore).InclusiveBetween(0.01m, 1.0m);
        }
    }

    /// <summary>Handler que infere dependência a partir de dados de telemetria.</summary>
    public sealed class Handler(
        IApiAssetRepository apiAssetRepository,
        IDateTimeProvider dateTimeProvider,
        IUnitOfWork unitOfWork) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var apiAssetId = ApiAssetId.From(request.ApiAssetId);
            var apiAsset = await apiAssetRepository.GetByIdAsync(apiAssetId, cancellationToken);
            if (apiAsset is null)
            {
                return EngineeringGraphErrors.ApiAssetNotFound(request.ApiAssetId);
            }

            var result = apiAsset.InferDependencyFromOtel(
                request.ConsumerName,
                request.Environment,
                request.ExternalReference,
                dateTimeProvider.UtcNow,
                request.ConfidenceScore);

            if (result.IsFailure)
            {
                return result.Error;
            }

            await unitOfWork.CommitAsync(cancellationToken);

            var relationship = result.Value;
            return new Response(relationship.Id.Value, relationship.ConsumerName, relationship.SourceType, relationship.ConfidenceScore);
        }
    }

    /// <summary>Resposta da inferência de dependência.</summary>
    public sealed record Response(Guid RelationshipId, string ConsumerName, string SourceType, decimal ConfidenceScore);
}
