using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.Portal.Abstractions;
using NexTraceOne.Catalog.Domain.Portal.Entities;

namespace NexTraceOne.Catalog.Application.Portal.Features.SetRateLimitPolicy;

/// <summary>Feature: SetRateLimitPolicy — cria ou atualiza política de rate limiting para uma API.</summary>
public static class SetRateLimitPolicy
{
    public sealed record Command(
        Guid ApiAssetId,
        int RequestsPerMinute,
        int RequestsPerHour,
        int RequestsPerDay,
        int BurstLimit,
        bool IsEnabled,
        string CreatedBy,
        string? Notes) : ICommand<Response>;

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.ApiAssetId).NotEmpty();
            RuleFor(x => x.RequestsPerMinute).GreaterThan(0);
            RuleFor(x => x.RequestsPerHour).GreaterThan(0);
            RuleFor(x => x.RequestsPerDay).GreaterThan(0);
            RuleFor(x => x.BurstLimit).GreaterThanOrEqualTo(1);
            RuleFor(x => x.CreatedBy).NotEmpty().MaximumLength(500);
            RuleFor(x => x.Notes).MaximumLength(1000).When(x => x.Notes != null);
        }
    }

    public sealed class Handler(
        IApiRateLimitPolicyRepository repository,
        IUnitOfWork unitOfWork,
        IDateTimeProvider clock) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            var existing = await repository.GetByApiAssetIdAsync(request.ApiAssetId, cancellationToken);

            if (existing is not null)
            {
                existing.Update(
                    request.RequestsPerMinute,
                    request.RequestsPerHour,
                    request.RequestsPerDay,
                    request.BurstLimit,
                    request.IsEnabled,
                    request.Notes,
                    clock.UtcNow);
                repository.Update(existing);
            }
            else
            {
                var createResult = RateLimitPolicy.Create(
                    request.ApiAssetId,
                    request.RequestsPerMinute,
                    request.RequestsPerHour,
                    request.RequestsPerDay,
                    request.BurstLimit,
                    request.Notes,
                    request.CreatedBy,
                    clock.UtcNow);

                if (createResult.IsFailure)
                    return createResult.Error;

                repository.Add(createResult.Value);
            }

            await unitOfWork.CommitAsync(cancellationToken);

            return new Response(
                request.ApiAssetId,
                request.RequestsPerMinute,
                request.RequestsPerHour,
                request.RequestsPerDay,
                request.BurstLimit,
                request.IsEnabled);
        }
    }

    public sealed record Response(
        Guid ApiAssetId,
        int RequestsPerMinute,
        int RequestsPerHour,
        int RequestsPerDay,
        int BurstLimit,
        bool IsEnabled);
}
