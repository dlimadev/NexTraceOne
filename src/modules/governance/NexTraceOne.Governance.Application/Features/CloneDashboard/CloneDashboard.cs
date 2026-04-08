using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Governance.Application.Abstractions;
using NexTraceOne.Governance.Domain.Entities;

namespace NexTraceOne.Governance.Application.Features.CloneDashboard;

/// <summary>
/// Feature: CloneDashboard — cria uma cópia independente de um dashboard existente.
/// A cópia não tem relação com o original; alterações são completamente independentes.
/// </summary>
public static class CloneDashboard
{
    public sealed record Command(
        Guid SourceDashboardId,
        string NewName,
        string TenantId,
        string UserId) : ICommand<Response>;

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.SourceDashboardId).NotEmpty();
            RuleFor(x => x.NewName).NotEmpty().MaximumLength(100);
            RuleFor(x => x.TenantId).NotEmpty();
            RuleFor(x => x.UserId).NotEmpty();
        }
    }

    public sealed class Handler(
        ICustomDashboardRepository repository,
        IUnitOfWork unitOfWork,
        IDateTimeProvider clock) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            var source = await repository.GetByIdAsync(
                new CustomDashboardId(request.SourceDashboardId), cancellationToken);

            if (source is null)
                return Error.NotFound(
                    "CustomDashboard.NotFound",
                    "Source dashboard with ID '{0}' was not found.",
                    request.SourceDashboardId);

            var now = clock.UtcNow;
            var clone = source.Clone(request.NewName, request.UserId, now);

            await repository.AddAsync(clone, cancellationToken);
            await unitOfWork.CommitAsync(cancellationToken);

            return Result<Response>.Success(new Response(
                CloneId: clone.Id.Value,
                Name: clone.Name,
                SourceDashboardId: request.SourceDashboardId,
                CreatedAt: clone.CreatedAt));
        }
    }

    public sealed record Response(
        Guid CloneId,
        string Name,
        Guid SourceDashboardId,
        DateTimeOffset CreatedAt);
}
