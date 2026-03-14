using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.EngineeringGraph.Application.Abstractions;
using NexTraceOne.EngineeringGraph.Domain.Entities;
using NexTraceOne.EngineeringGraph.Domain.Errors;

namespace NexTraceOne.EngineeringGraph.Application.Features.RegisterServiceAsset;

/// <summary>
/// Feature: RegisterServiceAsset — registra um novo serviço no grafo de engenharia.
/// Estrutura VSA: Command + Validator + Handler + Response em um único arquivo.
/// </summary>
public static class RegisterServiceAsset
{
    /// <summary>Comando de registo de um ativo de serviço.</summary>
    public sealed record Command(string Name, string Domain, string TeamName) : ICommand<Response>;

    /// <summary>Valida a entrada do comando de registo de serviço.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Domain).NotEmpty().MaximumLength(200);
            RuleFor(x => x.TeamName).NotEmpty().MaximumLength(200);
        }
    }

    /// <summary>Handler que regista um novo ativo de serviço no grafo.</summary>
    public sealed class Handler(
        IServiceAssetRepository serviceAssetRepository,
        IUnitOfWork unitOfWork) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var existing = await serviceAssetRepository.GetByNameAsync(request.Name, cancellationToken);
            if (existing is not null)
            {
                return EngineeringGraphErrors.ServiceAssetAlreadyExists(request.Name);
            }

            var serviceAsset = ServiceAsset.Create(request.Name, request.Domain, request.TeamName);
            serviceAssetRepository.Add(serviceAsset);

            await unitOfWork.CommitAsync(cancellationToken);

            return new Response(serviceAsset.Id.Value, serviceAsset.Name, serviceAsset.Domain, serviceAsset.TeamName);
        }
    }

    /// <summary>Resposta do registo do ativo de serviço.</summary>
    public sealed record Response(Guid ServiceAssetId, string Name, string Domain, string TeamName);
}
