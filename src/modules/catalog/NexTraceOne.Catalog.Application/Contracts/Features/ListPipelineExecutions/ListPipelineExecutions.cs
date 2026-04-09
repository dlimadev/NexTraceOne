using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.Contracts.Abstractions;
using NexTraceOne.Catalog.Domain.Contracts.Enums;

namespace NexTraceOne.Catalog.Application.Contracts.Features.ListPipelineExecutions;

/// <summary>
/// Feature: ListPipelineExecutions — lista execuções de pipeline com filtro opcional por API Asset.
/// Permite consultar o histórico de execuções para um contrato específico ou todos os contratos.
/// Estrutura VSA: Query + Validator + Handler + Response em arquivo único.
/// </summary>
public static class ListPipelineExecutions
{
    /// <summary>Query para listar execuções de pipeline.</summary>
    public sealed record Query(Guid? ApiAssetId) : IQuery<Response>;

    /// <summary>Valida a entrada da query de listagem de execuções de pipeline.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.ApiAssetId)
                .Must(id => id != Guid.Empty)
                .When(x => x.ApiAssetId.HasValue)
                .WithMessage("ApiAssetId must not be empty when provided.");
        }
    }

    /// <summary>
    /// Handler que lista execuções de pipeline com filtro opcional por API Asset.
    /// </summary>
    public sealed class Handler(IPipelineExecutionRepository repository)
        : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var executions = await repository.ListAsync(request.ApiAssetId, cancellationToken);

            var items = executions.Select(e => new ExecutionSummary(
                e.Id.Value,
                e.ApiAssetId,
                e.ContractName,
                e.ContractVersion,
                e.TargetLanguage,
                e.TargetFramework,
                e.Status,
                e.TotalStages,
                e.CompletedStages,
                e.FailedStages,
                e.StartedAt,
                e.CompletedAt,
                e.DurationMs,
                e.InitiatedByUserId)).ToList();

            return new Response(items);
        }
    }

    /// <summary>Resumo de uma execução de pipeline para listagem.</summary>
    public sealed record ExecutionSummary(
        Guid ExecutionId,
        Guid ApiAssetId,
        string ContractName,
        string ContractVersion,
        string TargetLanguage,
        string? TargetFramework,
        PipelineExecutionStatus Status,
        int TotalStages,
        int CompletedStages,
        int FailedStages,
        DateTimeOffset StartedAt,
        DateTimeOffset? CompletedAt,
        long? DurationMs,
        string InitiatedByUserId);

    /// <summary>Resposta da listagem de execuções de pipeline.</summary>
    public sealed record Response(IReadOnlyList<ExecutionSummary> Items);
}
