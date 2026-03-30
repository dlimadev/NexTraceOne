using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.Contracts.Abstractions;
using NexTraceOne.Catalog.Domain.Contracts.Entities;

namespace NexTraceOne.Catalog.Application.Contracts.Features.GetContractDeployments;

/// <summary>
/// Feature: GetContractDeployments — lista os deployments registados de uma versão de contrato.
/// Suporta rastreabilidade de mudanças por ambiente para Change Intelligence.
/// Estrutura VSA: Query + Validator + Handler + Response em um único arquivo.
/// </summary>
public static class GetContractDeployments
{
    /// <summary>Query para listar deployments de uma versão de contrato.</summary>
    public sealed record Query(Guid ContractVersionId) : IQuery<Response>;

    /// <summary>Valida os parâmetros da consulta.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.ContractVersionId).NotEmpty();
        }
    }

    /// <summary>Handler que retorna todos os deployments de uma versão de contrato ordenados por data.</summary>
    public sealed class Handler(IContractDeploymentRepository repository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var deployments = await repository.ListByContractVersionAsync(
                ContractVersionId.From(request.ContractVersionId), cancellationToken);

            var items = deployments.Select(d => new DeploymentDto(
                d.Id.Value,
                d.ContractVersionId.Value,
                d.ApiAssetId,
                d.Environment,
                d.SemVer,
                d.Status.ToString(),
                d.DeployedAt,
                d.DeployedBy,
                d.SourceSystem,
                d.Notes)).ToList();

            return new Response(items);
        }
    }

    /// <summary>DTO de deployment de contrato para listagem.</summary>
    public sealed record DeploymentDto(
        Guid DeploymentId,
        Guid ContractVersionId,
        Guid ApiAssetId,
        string Environment,
        string SemVer,
        string Status,
        DateTimeOffset DeployedAt,
        string DeployedBy,
        string SourceSystem,
        string? Notes);

    /// <summary>Resposta com lista de deployments da versão de contrato.</summary>
    public sealed record Response(IReadOnlyList<DeploymentDto> Deployments);
}
