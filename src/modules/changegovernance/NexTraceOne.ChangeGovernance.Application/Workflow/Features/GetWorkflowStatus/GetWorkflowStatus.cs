using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Workflow.Application.Abstractions;
using NexTraceOne.Workflow.Domain.Entities;
using NexTraceOne.Workflow.Domain.Errors;

namespace NexTraceOne.Workflow.Application.Features.GetWorkflowStatus;

/// <summary>
/// Feature: GetWorkflowStatus — retorna o status atual de uma instância de workflow com seus estágios.
/// Estrutura VSA: Query + Validator + Handler + Response em um único arquivo.
/// </summary>
public static class GetWorkflowStatus
{
    /// <summary>Query para obter o status de uma instância de workflow.</summary>
    public sealed record Query(Guid InstanceId) : IQuery<Response>;

    /// <summary>Valida a entrada da query de status de workflow.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.InstanceId).NotEmpty();
        }
    }

    /// <summary>Handler que consulta a instância e seus estágios para retornar o status consolidado.</summary>
    public sealed class Handler(
        IWorkflowInstanceRepository instanceRepository,
        IWorkflowStageRepository stageRepository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var instance = await instanceRepository.GetByIdAsync(
                WorkflowInstanceId.From(request.InstanceId), cancellationToken);

            if (instance is null)
                return WorkflowErrors.InstanceNotFound(request.InstanceId.ToString());

            var stages = await stageRepository.ListByInstanceIdAsync(
                instance.Id, cancellationToken);

            var stageResponses = stages
                .Select(s => new StageResponse(
                    s.Id.Value,
                    s.Name,
                    s.StageOrder,
                    s.Status.ToString(),
                    s.RequiredApprovers,
                    s.CurrentApprovals,
                    s.IsComplete))
                .ToList();

            return new Response(
                instance.Id.Value,
                instance.Status.ToString(),
                instance.CurrentStageIndex,
                instance.SubmittedBy,
                instance.SubmittedAt,
                instance.CompletedAt,
                stageResponses);
        }
    }

    /// <summary>Dados de um estágio na resposta de status.</summary>
    public sealed record StageResponse(
        Guid StageId,
        string Name,
        int StageOrder,
        string Status,
        int RequiredApprovers,
        int CurrentApprovals,
        bool IsComplete);

    /// <summary>Resposta do status consolidado de uma instância de workflow.</summary>
    public sealed record Response(
        Guid InstanceId,
        string Status,
        int CurrentStageIndex,
        string SubmittedBy,
        DateTimeOffset SubmittedAt,
        DateTimeOffset? CompletedAt,
        IReadOnlyList<StageResponse> Stages);
}
