using Ardalis.GuardClauses;
using FluentValidation;
using MediatR;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.EngineeringGraph.Application.Abstractions;
using NexTraceOne.EngineeringGraph.Domain.Entities;
using NexTraceOne.EngineeringGraph.Domain.Errors;

namespace NexTraceOne.EngineeringGraph.Application.Features.UpdateAssetMetadata;

/// <summary>
/// Feature: UpdateAssetMetadata — atualiza metadados de um ativo de API existente.
/// </summary>
public static class UpdateAssetMetadata
{
    /// <summary>Comando de atualização de metadados do ativo.</summary>
    public sealed record Command(
        Guid ApiAssetId,
        string Name,
        string RoutePattern,
        string Version,
        string Visibility) : ICommand;

    /// <summary>Valida a entrada do comando de atualização.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.ApiAssetId).NotEmpty();
            RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
            RuleFor(x => x.RoutePattern).NotEmpty().MaximumLength(500);
            RuleFor(x => x.Version).NotEmpty().MaximumLength(50);
            RuleFor(x => x.Visibility).NotEmpty().MaximumLength(50);
        }
    }

    /// <summary>Handler que atualiza metadados de um ativo de API.</summary>
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

            var result = apiAsset.UpdateMetadata(request.Name, request.RoutePattern, request.Version, request.Visibility);
            if (result.IsFailure)
            {
                return result.Error;
            }

            await unitOfWork.CommitAsync(cancellationToken);
            return Unit.Value;
        }
    }
}
