using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.Contracts.Abstractions;
using NexTraceOne.Catalog.Domain.Contracts.Entities;
using NexTraceOne.Catalog.Domain.Contracts.Enums;
using NexTraceOne.Catalog.Domain.Contracts.Errors;

namespace NexTraceOne.Catalog.Application.Contracts.Features.GetPipelineExecution;

/// <summary>
/// Feature: GetPipelineExecution — obtém os detalhes de uma execução de pipeline pelo seu ID.
/// Permite consultar o estado, estágios, artefactos e erros de uma execução específica.
/// Estrutura VSA: Query + Validator + Handler + Response em arquivo único.
/// </summary>
public static class GetPipelineExecution
{
    /// <summary>Query para obter uma execução de pipeline.</summary>
    public sealed record Query(Guid ExecutionId) : IQuery<Response>;

    /// <summary>Valida a entrada da query de execução de pipeline.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.ExecutionId).NotEmpty();
        }
    }

    /// <summary>
    /// Handler que obtém os detalhes de uma execução de pipeline persistida.
    /// </summary>
    public sealed class Handler(IPipelineExecutionRepository repository)
        : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var execution = await repository.GetByIdAsync(
                PipelineExecutionId.From(request.ExecutionId), cancellationToken);

            if (execution is null)
                return ContractsErrors.PipelineExecutionNotFound(request.ExecutionId.ToString());

            return new Response(
                execution.Id.Value,
                execution.ApiAssetId,
                execution.ContractName,
                execution.ContractVersion,
                execution.RequestedStages,
                execution.StageResults,
                execution.GeneratedArtifacts,
                execution.TargetLanguage,
                execution.TargetFramework,
                execution.Status,
                execution.TotalStages,
                execution.CompletedStages,
                execution.FailedStages,
                execution.StartedAt,
                execution.CompletedAt,
                execution.DurationMs,
                execution.ErrorMessage,
                execution.InitiatedByUserId);
        }
    }

    /// <summary>Resposta completa de uma execução de pipeline.</summary>
    public sealed record Response(
        Guid ExecutionId,
        Guid ApiAssetId,
        string ContractName,
        string ContractVersion,
        string RequestedStages,
        string? StageResults,
        string? GeneratedArtifacts,
        string TargetLanguage,
        string? TargetFramework,
        PipelineExecutionStatus Status,
        int TotalStages,
        int CompletedStages,
        int FailedStages,
        DateTimeOffset StartedAt,
        DateTimeOffset? CompletedAt,
        long? DurationMs,
        string? ErrorMessage,
        string InitiatedByUserId);
}
