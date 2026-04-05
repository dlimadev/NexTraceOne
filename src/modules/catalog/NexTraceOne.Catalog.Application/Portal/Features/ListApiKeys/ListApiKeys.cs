using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.Portal.Abstractions;

namespace NexTraceOne.Catalog.Application.Portal.Features.ListApiKeys;

/// <summary>Feature: ListApiKeys — lista API Keys de um proprietário sem expor o valor raw.</summary>
public static class ListApiKeys
{
    public sealed record Query(Guid OwnerId) : IQuery<IReadOnlyList<ApiKeyDto>>;

    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.OwnerId).NotEmpty();
        }
    }

    public sealed class Handler(IApiKeyRepository repository) : IQueryHandler<Query, IReadOnlyList<ApiKeyDto>>
    {
        public async Task<Result<IReadOnlyList<ApiKeyDto>>> Handle(Query request, CancellationToken cancellationToken)
        {
            var keys = await repository.GetByOwnerAsync(request.OwnerId, cancellationToken);

            var dtos = keys.Select(k => new ApiKeyDto(
                k.Id.Value,
                k.Name,
                k.KeyPrefix,
                k.IsActive,
                k.CreatedAt,
                k.ExpiresAt,
                k.LastUsedAt,
                k.RequestCount,
                k.ApiAssetId)).ToList();

            return Result<IReadOnlyList<ApiKeyDto>>.Success(dtos);
        }
    }

    public sealed record ApiKeyDto(
        Guid Id,
        string Name,
        string KeyPrefix,
        bool IsActive,
        DateTimeOffset CreatedAt,
        DateTimeOffset? ExpiresAt,
        DateTimeOffset? LastUsedAt,
        long RequestCount,
        Guid? ApiAssetId);
}
