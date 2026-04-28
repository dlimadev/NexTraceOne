using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.IdentityAccess.Application.Abstractions;
using NexTraceOne.IdentityAccess.Domain.Entities;

namespace NexTraceOne.IdentityAccess.Application.Features.ResolveAlert;

/// <summary>
/// SaaS-08: Resolve ou silencia manualmente um alerta disparado.
/// </summary>
public static class ResolveAlert
{
    public sealed record Command(
        Guid RecordId,
        string Action,
        string? Reason) : ICommand<Response>;

    public sealed record Response(Guid RecordId, string Status);

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.RecordId).NotEmpty();
            RuleFor(x => x.Action).NotEmpty()
                .Must(a => a is "resolve" or "silence")
                .WithMessage("Action must be 'resolve' or 'silence'.");
        }
    }

    public sealed class Handler(
        IAlertFiringRecordRepository repository,
        IDateTimeProvider dateTimeProvider,
        IIdentityAccessUnitOfWork unitOfWork) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);
            var now = dateTimeProvider.UtcNow;

            var record = await repository.GetByIdAsync(AlertFiringRecordId.From(request.RecordId), cancellationToken);
            if (record is null)
                return Result.Failure<Response>("Alert firing record not found.");

            if (request.Action == "silence")
                record.Silence(now);
            else
                record.Resolve(request.Reason, now);

            repository.Update(record);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return Result.Success(new Response(record.Id.Value, record.Status.ToString()));
        }
    }
}
