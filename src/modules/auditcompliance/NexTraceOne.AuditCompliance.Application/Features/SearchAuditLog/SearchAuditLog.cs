using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.AuditCompliance.Application.Abstractions;
using NexTraceOne.AuditCompliance.Domain.Entities;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.AuditCompliance.Application.Features.SearchAuditLog;

/// <summary>
/// Feature: SearchAuditLog — pesquisa eventos de auditoria com filtros.
/// P7.4 — enriquecido com filtros de correlação (CorrelationId) e ResourceType/ResourceId
/// para suporte a lookup por fluxo e recurso auditável.
/// </summary>
public static class SearchAuditLog
{
    /// <summary>Query de pesquisa de eventos de auditoria.</summary>
    public sealed record Query(
        string? SourceModule,
        string? ActionType,
        string? CorrelationId,
        DateTimeOffset? From,
        DateTimeOffset? To,
        int Page,
        int PageSize,
        string? ResourceType = null,
        string? ResourceId = null) : IQuery<Response>;

    /// <summary>Valida a entrada da query.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
            RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
            RuleFor(x => x.CorrelationId).MaximumLength(100)
                .When(x => !string.IsNullOrWhiteSpace(x.CorrelationId));
        }
    }

    /// <summary>Handler que pesquisa eventos de auditoria.</summary>
    public sealed class Handler(IAuditEventRepository auditEventRepository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            IReadOnlyList<AuditEvent> events;
            int totalCount;

            // Use the enriched search when ResourceType/ResourceId is provided
            if (request.ResourceType is not null || request.ResourceId is not null)
            {
                events = await auditEventRepository.SearchWithResourceAsync(
                    request.SourceModule, request.ActionType,
                    request.CorrelationId,
                    request.ResourceType, request.ResourceId,
                    request.From, request.To,
                    request.Page, request.PageSize, cancellationToken);

                totalCount = await auditEventRepository.CountWithResourceAsync(
                    request.SourceModule, request.ActionType,
                    request.CorrelationId,
                    request.ResourceType, request.ResourceId,
                    request.From, request.To, cancellationToken);
            }
            else
            {
                events = await auditEventRepository.SearchAsync(
                    request.SourceModule, request.ActionType, request.CorrelationId,
                    request.From, request.To,
                    request.Page, request.PageSize, cancellationToken);

                totalCount = await auditEventRepository.CountAsync(
                    request.SourceModule, request.ActionType, request.CorrelationId,
                    request.From, request.To, cancellationToken);
            }

            var items = events
                .Select(e => new AuditLogEntry(
                    e.Id.Value, e.SourceModule, e.ActionType,
                    e.ResourceType, e.ResourceId,
                    e.PerformedBy, e.OccurredAt, e.TenantId,
                    e.Payload,
                    e.CorrelationId,
                    e.ChainLink?.CurrentHash,
                    e.ChainLink?.PreviousHash,
                    e.ChainLink?.SequenceNumber))
                .ToArray();

            var totalPages = request.PageSize > 0
                ? (int)Math.Ceiling((double)totalCount / request.PageSize)
                : 0;

            return new Response(items, totalCount, request.Page, request.PageSize, totalPages);
        }
    }

    /// <summary>Resposta da pesquisa de auditoria.</summary>
    public sealed record Response(
        IReadOnlyList<AuditLogEntry> Items,
        int TotalCount,
        int Page,
        int PageSize,
        int TotalPages);

    /// <summary>Entrada do log de auditoria.</summary>
    public sealed record AuditLogEntry(
        Guid EventId,
        string SourceModule,
        string ActionType,
        string ResourceType,
        string ResourceId,
        string PerformedBy,
        DateTimeOffset OccurredAt,
        Guid TenantId,
        string? Payload,
        string? CorrelationId,
        string? ChainHash,
        string? PreviousHash,
        long? SequenceNumber);
}
