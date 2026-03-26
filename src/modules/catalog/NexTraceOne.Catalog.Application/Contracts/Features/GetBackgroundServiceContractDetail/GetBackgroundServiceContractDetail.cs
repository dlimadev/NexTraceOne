using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.Contracts.Abstractions;
using NexTraceOne.Catalog.Domain.Contracts.Entities;
using NexTraceOne.Catalog.Domain.Contracts.Errors;

namespace NexTraceOne.Catalog.Application.Contracts.Features.GetBackgroundServiceContractDetail;

/// <summary>
/// Feature: GetBackgroundServiceContractDetail — consulta os metadados específicos de um Background Service Contract.
/// Retorna informações do processo: nome, categoria, trigger, schedule, inputs/outputs, side effects, concurrency.
/// Estrutura VSA: Query + Validator + Handler + Response em ficheiro único.
/// </summary>
public static class GetBackgroundServiceContractDetail
{
    /// <summary>Query para obter os detalhes de Background Service de uma versão de contrato.</summary>
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
    /// Handler que consulta o BackgroundServiceContractDetail associado a uma versão de contrato.
    /// Retorna erro quando a versão não tem detalhe de background service associado.
    /// </summary>
    public sealed class Handler(
        IBackgroundServiceContractDetailRepository repository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var versionId = ContractVersionId.From(request.ContractVersionId);
            var detail = await repository.GetByContractVersionIdAsync(versionId, cancellationToken);

            if (detail is null)
                return ContractsErrors.BackgroundServiceDetailNotFound(request.ContractVersionId.ToString());

            return new Response(
                DetailId: detail.Id.Value,
                ContractVersionId: detail.ContractVersionId.Value,
                ServiceName: detail.ServiceName,
                Category: detail.Category,
                TriggerType: detail.TriggerType,
                ScheduleExpression: detail.ScheduleExpression,
                TimeoutExpression: detail.TimeoutExpression,
                AllowsConcurrency: detail.AllowsConcurrency,
                InputsJson: detail.InputsJson,
                OutputsJson: detail.OutputsJson,
                SideEffectsJson: detail.SideEffectsJson);
        }
    }

    /// <summary>Resposta com os detalhes do Background Service Contract.</summary>
    public sealed record Response(
        Guid DetailId,
        Guid ContractVersionId,
        string ServiceName,
        string Category,
        string TriggerType,
        string? ScheduleExpression,
        string? TimeoutExpression,
        bool AllowsConcurrency,
        string InputsJson,
        string OutputsJson,
        string SideEffectsJson);
}
