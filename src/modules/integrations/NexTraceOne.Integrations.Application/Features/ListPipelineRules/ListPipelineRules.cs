using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Integrations.Application.Abstractions;
using NexTraceOne.Integrations.Domain.Enums;

namespace NexTraceOne.Integrations.Application.Features.ListPipelineRules;

/// <summary>
/// Feature: ListPipelineRules — lista regras de pipeline de um tenant com filtros e paginação.
/// Ownership: módulo Integrations (Pipeline).
/// </summary>
public static class ListPipelineRules
{
    /// <summary>Query para listar regras de pipeline.</summary>
    public sealed record Query(
        string TenantId,
        PipelineRuleType? RuleType = null,
        PipelineSignalType? SignalType = null,
        bool? IsEnabled = null,
        int Page = 1,
        int PageSize = 20) : IQuery<Response>;

    /// <summary>Validador da query.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.TenantId).NotEmpty();
            RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
            RuleFor(x => x.PageSize).InclusiveBetween(1, 50);
        }
    }

    /// <summary>Handler que retorna a lista paginada de regras de pipeline.</summary>
    public sealed class Handler(ITenantPipelineRuleRepository repository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            var (rules, totalCount) = await repository.ListAsync(
                request.RuleType,
                request.SignalType,
                request.IsEnabled,
                request.Page,
                request.PageSize,
                cancellationToken);

            var items = rules.Select(r => new PipelineRuleDto(
                RuleId: r.Id.Value,
                Name: r.Name,
                RuleType: r.RuleType,
                SignalType: r.SignalType,
                ConditionJson: r.ConditionJson,
                ActionJson: r.ActionJson,
                Priority: r.Priority,
                IsEnabled: r.IsEnabled,
                Description: r.Description,
                CreatedAt: r.CreatedAt,
                UpdatedAt: r.UpdatedAt)).ToList();

            return Result<Response>.Success(new Response(items, totalCount));
        }
    }

    /// <summary>Resposta da query ListPipelineRules.</summary>
    public sealed record Response(
        IReadOnlyList<PipelineRuleDto> Items,
        int TotalCount);

    /// <summary>DTO de uma regra de pipeline.</summary>
    public sealed record PipelineRuleDto(
        Guid RuleId,
        string Name,
        PipelineRuleType RuleType,
        PipelineSignalType SignalType,
        string ConditionJson,
        string ActionJson,
        int Priority,
        bool IsEnabled,
        string? Description,
        DateTimeOffset CreatedAt,
        DateTimeOffset? UpdatedAt);
}
