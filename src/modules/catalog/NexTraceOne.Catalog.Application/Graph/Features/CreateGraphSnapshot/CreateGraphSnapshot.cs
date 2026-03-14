using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.EngineeringGraph.Application.Abstractions;
using NexTraceOne.EngineeringGraph.Domain.Entities;
using System.Text.Json;

namespace NexTraceOne.EngineeringGraph.Application.Features.CreateGraphSnapshot;

/// <summary>
/// Feature: CreateGraphSnapshot — materializa o estado atual do grafo como snapshot temporal.
/// Permite consultas históricas, diff entre dois instantes e baseline para comparação.
/// Estratégia de armazenamento: JSON serializado dos nós e arestas.
/// Estrutura VSA: Command + Validator + Handler + Response em um único arquivo.
/// </summary>
public static class CreateGraphSnapshot
{
    /// <summary>Comando para criar um snapshot do estado atual do grafo.</summary>
    public sealed record Command(string Label) : ICommand<Response>;

    /// <summary>Valida os parâmetros do snapshot.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Label).NotEmpty().MaximumLength(200);
        }
    }

    /// <summary>
    /// Handler que materializa o grafo atual como snapshot.
    /// Serializa todos os nós e arestas em JSON, persiste e retorna o identificador.
    /// </summary>
    public sealed class Handler(
        IApiAssetRepository apiAssetRepository,
        IServiceAssetRepository serviceAssetRepository,
        IGraphSnapshotRepository snapshotRepository,
        ICurrentUser currentUser,
        IDateTimeProvider clock,
        IUnitOfWork unitOfWork) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var allApis = await apiAssetRepository.ListAllAsync(cancellationToken);
            var allServices = await serviceAssetRepository.ListAllAsync(cancellationToken);

            var nodesPayload = allServices.Select(s => new
            {
                s.Id.Value,
                Type = "Service",
                s.Name,
                s.Domain,
                s.TeamName
            }).Concat<object>(allApis.Select(a => new
            {
                a.Id.Value,
                Type = "Api",
                a.Name,
                a.RoutePattern,
                a.Version,
                a.Visibility,
                OwnerServiceId = a.OwnerService.Id.Value
            })).ToList();

            var edgesPayload = allApis.SelectMany(a =>
                a.ConsumerRelationships.Select(r => new
                {
                    ApiAssetId = a.Id.Value,
                    ConsumerId = r.ConsumerAssetId.Value,
                    r.ConsumerName,
                    r.SourceType,
                    r.ConfidenceScore
                })).ToList();

            var nodesJson = JsonSerializer.Serialize(nodesPayload);
            var edgesJson = JsonSerializer.Serialize(edgesPayload);

            var snapshot = GraphSnapshot.Create(
                request.Label,
                clock.UtcNow,
                nodesJson,
                edgesJson,
                nodesPayload.Count,
                edgesPayload.Count,
                currentUser.Id);

            snapshotRepository.Add(snapshot);
            await unitOfWork.CommitAsync(cancellationToken);

            return new Response(snapshot.Id.Value, snapshot.Label, snapshot.CapturedAt, snapshot.NodeCount, snapshot.EdgeCount);
        }
    }

    /// <summary>Resposta com os metadados do snapshot criado.</summary>
    public sealed record Response(Guid SnapshotId, string Label, DateTimeOffset CapturedAt, int NodeCount, int EdgeCount);
}
