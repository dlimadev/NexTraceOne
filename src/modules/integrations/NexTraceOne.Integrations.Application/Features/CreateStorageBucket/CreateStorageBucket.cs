using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Integrations.Application.Abstractions;
using NexTraceOne.Integrations.Domain.Entities;
using NexTraceOne.Integrations.Domain.Enums;

namespace NexTraceOne.Integrations.Application.Features.CreateStorageBucket;

/// <summary>
/// Feature: CreateStorageBucket — cria um novo bucket de routing de storage para o tenant.
/// Ownership: módulo Integrations (Pipeline).
/// </summary>
public static class CreateStorageBucket
{
    public sealed record Command(
        string TenantId,
        string BucketName,
        StorageBucketBackendType BackendType,
        int RetentionDays,
        string? FilterJson,
        int Priority,
        bool IsEnabled = true,
        bool IsFallback = false,
        string? Description = null) : ICommand<Response>;

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.TenantId).NotEmpty();
            RuleFor(x => x.BucketName).NotEmpty().MaximumLength(100);
            RuleFor(x => x.BackendType).IsInEnum();
            RuleFor(x => x.RetentionDays).GreaterThan(0).LessThanOrEqualTo(36500);
            RuleFor(x => x.Priority).GreaterThan(0).LessThanOrEqualTo(1000);
            RuleFor(x => x.FilterJson).MaximumLength(2000).When(x => x.FilterJson is not null);
            RuleFor(x => x.Description).MaximumLength(500).When(x => x.Description is not null);
        }
    }

    public sealed class Handler(
        IStorageBucketRepository repository,
        IUnitOfWork unitOfWork,
        IDateTimeProvider clock) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            var bucket = StorageBucket.Create(
                tenantId: request.TenantId,
                bucketName: request.BucketName,
                backendType: request.BackendType,
                retentionDays: request.RetentionDays,
                filterJson: request.FilterJson,
                priority: request.Priority,
                isEnabled: request.IsEnabled,
                isFallback: request.IsFallback,
                description: request.Description,
                utcNow: clock.UtcNow);

            await repository.AddAsync(bucket, cancellationToken);
            await unitOfWork.CommitAsync(cancellationToken);

            return Result<Response>.Success(new Response(
                BucketId: bucket.Id.Value,
                BucketName: bucket.BucketName,
                BackendType: bucket.BackendType,
                RetentionDays: bucket.RetentionDays,
                Priority: bucket.Priority,
                IsEnabled: bucket.IsEnabled,
                IsFallback: bucket.IsFallback,
                CreatedAt: bucket.CreatedAt));
        }
    }

    public sealed record Response(
        Guid BucketId,
        string BucketName,
        StorageBucketBackendType BackendType,
        int RetentionDays,
        int Priority,
        bool IsEnabled,
        bool IsFallback,
        DateTimeOffset CreatedAt);
}
