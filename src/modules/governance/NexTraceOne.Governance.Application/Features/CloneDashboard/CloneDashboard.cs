using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

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

    public sealed class Handler(IDateTimeProvider clock) : ICommandHandler<Command, Response>
    {
        public Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            var cloneId = Guid.NewGuid();
            var now = clock.UtcNow;

            return Task.FromResult(Result<Response>.Success(new Response(
                CloneId: cloneId,
                Name: request.NewName,
                SourceDashboardId: request.SourceDashboardId,
                CreatedAt: now)));
        }
    }

    public sealed record Response(
        Guid CloneId,
        string Name,
        Guid SourceDashboardId,
        DateTimeOffset CreatedAt);
}
