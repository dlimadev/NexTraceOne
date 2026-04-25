using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Integrations.Application.Abstractions;
using NexTraceOne.Integrations.Domain.Enums;

namespace NexTraceOne.Integrations.Application.Features.ListStorageBuckets;

/// <summary>
/// Feature: ListStorageBuckets — lista buckets de storage de um tenant.
/// Ownership: módulo Integrations (Pipeline).
/// </summary>
public static class ListStorageBuckets
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

    public sealed class Handler(IStorageBucketRepository repository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            var (buckets, totalCount) = await repository.ListAsync(
                request.IsEnabled,
                request.Page,
                request.PageSize,
                cancellationToken);

            var items = buckets.Select(b => new StorageBucketDto(
                BucketId: b.Id.Value,
                BucketName: b.BucketName,
                BackendType: b.BackendType,
                RetentionDays: b.RetentionDays,
                FilterJson: b.FilterJson,
                Priority: b.Priority,
                IsEnabled: b.IsEnabled,
                IsFallback: b.IsFallback,
                Description: b.Description,
                CreatedAt: b.CreatedAt,
                UpdatedAt: b.UpdatedAt)).ToList();

            return Result<Response>.Success(new Response(items, totalCount));
        }
    }

    public sealed record Response(IReadOnlyList<StorageBucketDto> Items, int TotalCount);

    public sealed record StorageBucketDto(
        Guid BucketId,
        string BucketName,
        StorageBucketBackendType BackendType,
        int RetentionDays,
        string? FilterJson,
        int Priority,
        bool IsEnabled,
        bool IsFallback,
        string? Description,
        DateTimeOffset CreatedAt,
        DateTimeOffset? UpdatedAt);
}
