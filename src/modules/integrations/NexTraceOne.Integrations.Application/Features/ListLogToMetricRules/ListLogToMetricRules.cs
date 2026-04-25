using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Integrations.Application.Abstractions;
using NexTraceOne.Integrations.Domain.Enums;

namespace NexTraceOne.Integrations.Application.Features.ListLogToMetricRules;

/// <summary>
/// Feature: ListLogToMetricRules — lista regras de transformação log → métrica de um tenant.
/// Ownership: módulo Integrations (Pipeline).
/// </summary>
public static class ListLogToMetricRules
{
    public sealed record Query(
        string TenantId,
        bool? IsEnabled = null,
        int Page = 1,
        int PageSize = 20) : IQuery<Response>;

    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.TenantId).NotEmpty();
            RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
            RuleFor(x => x.PageSize).InclusiveBetween(1, 50);
        }
    }

    public sealed class Handler(ILogToMetricRuleRepository repository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            var (rules, totalCount) = await repository.ListAsync(
                request.IsEnabled,
                request.Page,
                request.PageSize,
                cancellationToken);

            var items = rules.Select(r => new LogToMetricRuleDto(
                RuleId: r.Id.Value,
                Name: r.Name,
                Pattern: r.Pattern,
                MetricName: r.MetricName,
                MetricType: r.MetricType,
                ValueExtractor: r.ValueExtractor,
                LabelsJson: r.LabelsJson,
                IsEnabled: r.IsEnabled,
                CreatedAt: r.CreatedAt,
                UpdatedAt: r.UpdatedAt)).ToList();

            return Result<Response>.Success(new Response(items, totalCount));
        }
    }

    public sealed record Response(IReadOnlyList<LogToMetricRuleDto> Items, int TotalCount);

    public sealed record LogToMetricRuleDto(
        Guid RuleId,
        string Name,
        string Pattern,
        string MetricName,
        MetricType MetricType,
        string? ValueExtractor,
        string LabelsJson,
        bool IsEnabled,
        DateTimeOffset CreatedAt,
        DateTimeOffset? UpdatedAt);
}
