using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.OperationalIntelligence.Application.Runtime.Abstractions;
using NexTraceOne.OperationalIntelligence.Domain.Runtime.Entities;
using NexTraceOne.OperationalIntelligence.Domain.Runtime.Errors;

namespace NexTraceOne.OperationalIntelligence.Application.Runtime.Features.GetOperationalPlaybook;

/// <summary>
/// Feature: GetOperationalPlaybook — obtém um playbook operacional pelo identificador.
/// Retorna todos os campos incluindo passos, ligações, estado e histórico de execução.
/// </summary>
public static class GetOperationalPlaybook
{
    /// <summary>Query para obter playbook por identificador.</summary>
    public sealed record Query(Guid PlaybookId) : IQuery<Response>;

    /// <summary>Validação da query.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.PlaybookId).NotEmpty();
        }
    }

    /// <summary>Handler que obtém o playbook operacional.</summary>
    public sealed class Handler(
        IOperationalPlaybookRepository repository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var playbook = await repository.GetByIdAsync(
                OperationalPlaybookId.From(request.PlaybookId),
                cancellationToken);

            if (playbook is null)
                return RuntimeIntelligenceErrors.PlaybookNotFound(request.PlaybookId.ToString());

            return Result<Response>.Success(new Response(
                playbook.Id.Value,
                playbook.Name,
                playbook.Description,
                playbook.Version,
                playbook.Steps,
                playbook.Status.ToString(),
                playbook.LinkedServiceIds,
                playbook.LinkedRunbookIds,
                playbook.Tags,
                playbook.ApprovedByUserId,
                playbook.ApprovedAt,
                playbook.DeprecatedAt,
                playbook.ExecutionCount,
                playbook.LastExecutedAt,
                playbook.CreatedAt));
        }
    }

    /// <summary>Resposta completa do playbook operacional.</summary>
    public sealed record Response(
        Guid PlaybookId,
        string Name,
        string? Description,
        int Version,
        string Steps,
        string Status,
        string? LinkedServiceIds,
        string? LinkedRunbookIds,
        string? Tags,
        string? ApprovedByUserId,
        DateTimeOffset? ApprovedAt,
        DateTimeOffset? DeprecatedAt,
        int ExecutionCount,
        DateTimeOffset? LastExecutedAt,
        DateTimeOffset CreatedAt);
}
