using Ardalis.GuardClauses;
using FluentValidation;
using MediatR;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Domain.Results;
using NexTraceOne.EngineeringGraph.Application.Abstractions;
using NexTraceOne.EngineeringGraph.Domain.Entities;
using NexTraceOne.EngineeringGraph.Domain.Errors;

namespace NexTraceOne.EngineeringGraph.Application.Features.DecommissionAsset;

/// <summary>
/// Feature: DecommissionAsset — marca um ativo de API como descomissionado.
/// </summary>
public static class DecommissionAsset
{
    /// <summary>Comando de descomissionamento de ativo de API.</summary>
    public sealed record Command(Guid ApiAssetId) : ICommand;

    /// <summary>Valida a entrada do comando de descomissionamento.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.ApiAssetId).NotEmpty();
        }
    }

    /// <summary>Handler que descomissiona um ativo de API.</summary>
    public sealed class Handler(
        IApiAssetRepository apiAssetRepository,
        IUnitOfWork unitOfWork) : ICommandHandler<Command>
    {
        public async Task<Result<Unit>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var apiAssetId = ApiAssetId.From(request.ApiAssetId);
            var apiAsset = await apiAssetRepository.GetByIdAsync(apiAssetId, cancellationToken);
            if (apiAsset is null)
            {
                return EngineeringGraphErrors.ApiAssetNotFound(request.ApiAssetId);
            }

            var result = apiAsset.Decommission();
            if (result.IsFailure)
            {
                return result.Error;
            }

            await unitOfWork.CommitAsync(cancellationToken);
            return Unit.Value;
        }
    }
}
