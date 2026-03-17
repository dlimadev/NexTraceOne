using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.AuditCompliance.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.AuditCompliance.Application.Features.GetAuditTrail;

/// <summary>
/// Feature: GetAuditTrail — obtém a trilha de auditoria de um recurso específico.
/// </summary>
public static class GetAuditTrail
{
    /// <summary>Query de trilha de auditoria por recurso.</summary>
    public sealed record Query(string ResourceType, string ResourceId) : IQuery<Response>;

    /// <summary>Valida a entrada da query.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.ResourceType).NotEmpty().MaximumLength(200);
            RuleFor(x => x.ResourceId).NotEmpty().MaximumLength(500);
        }
    }

    /// <summary>Handler que retorna a trilha de auditoria do recurso.</summary>
    public sealed class Handler(IAuditEventRepository auditEventRepository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var events = await auditEventRepository.GetTrailByResourceAsync(request.ResourceType, request.ResourceId, cancellationToken);

            var items = events
                .Select(e => new AuditTrailItem(e.Id.Value, e.SourceModule, e.ActionType, e.PerformedBy, e.OccurredAt, e.ChainLink?.CurrentHash))
                .ToArray();

            return new Response(items);
        }
    }

    /// <summary>Resposta da trilha de auditoria.</summary>
    public sealed record Response(IReadOnlyList<AuditTrailItem> Items);

    /// <summary>Item da trilha de auditoria.</summary>
    public sealed record AuditTrailItem(Guid EventId, string SourceModule, string ActionType, string PerformedBy, DateTimeOffset OccurredAt, string? ChainHash);
}
