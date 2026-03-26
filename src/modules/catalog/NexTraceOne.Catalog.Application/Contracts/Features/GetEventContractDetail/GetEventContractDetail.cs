using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.Contracts.Abstractions;
using NexTraceOne.Catalog.Domain.Contracts.Entities;
using NexTraceOne.Catalog.Domain.Contracts.Errors;

namespace NexTraceOne.Catalog.Application.Contracts.Features.GetEventContractDetail;

/// <summary>
/// Feature: GetEventContractDetail — consulta os metadados AsyncAPI específicos de uma versão de contrato.
/// Retorna informações extraídas da spec AsyncAPI: título, versão, channels, mensagens, servidores, content type.
/// Estrutura VSA: Query + Validator + Handler + Response em ficheiro único.
/// </summary>
public static class GetEventContractDetail
{
    /// <summary>Query para obter os detalhes AsyncAPI de uma versão de contrato.</summary>
    public sealed record Query(Guid ContractVersionId) : IQuery<Response>;

    /// <summary>Valida a entrada da query.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.ContractVersionId).NotEmpty();
        }
    }

    /// <summary>
    /// Handler que consulta o EventContractDetail associado a uma versão de contrato.
    /// Retorna erro quando a versão não tem detalhe de evento associado.
    /// </summary>
    public sealed class Handler(
        IEventContractDetailRepository repository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var versionId = ContractVersionId.From(request.ContractVersionId);
            var detail = await repository.GetByContractVersionIdAsync(versionId, cancellationToken);

            if (detail is null)
                return ContractsErrors.EventDetailNotFound(request.ContractVersionId.ToString());

            return new Response(
                EventDetailId: detail.Id.Value,
                ContractVersionId: detail.ContractVersionId.Value,
                Title: detail.Title,
                AsyncApiVersion: detail.AsyncApiVersion,
                DefaultContentType: detail.DefaultContentType,
                ChannelsJson: detail.ChannelsJson,
                MessagesJson: detail.MessagesJson,
                ServersJson: detail.ServersJson);
        }
    }

    /// <summary>Resposta com os detalhes AsyncAPI extraídos da versão de contrato.</summary>
    public sealed record Response(
        Guid EventDetailId,
        Guid ContractVersionId,
        string Title,
        string AsyncApiVersion,
        string DefaultContentType,
        string ChannelsJson,
        string MessagesJson,
        string ServersJson);
}
