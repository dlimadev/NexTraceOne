using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Governance.Application.Abstractions;
using NexTraceOne.Governance.Domain.Enums;

namespace NexTraceOne.Governance.Application.Features.ListExecutiveBriefings;

/// <summary>
/// Feature: ListExecutiveBriefings — lista briefings executivos com filtros opcionais.
/// Suporta filtro por frequência e/ou status.
///
/// Owner: módulo Governance.
/// Pilar: Operational Intelligence — visão panorâmica de briefings executivos.
/// Persona principal: Executive, Tech Lead, Auditor.
/// </summary>
public static class ListExecutiveBriefings
{
    /// <summary>Query para listar executive briefings com filtros opcionais.</summary>
    public sealed record Query(
        BriefingFrequency? Frequency = null,
        BriefingStatus? Status = null) : IQuery<Response>;

    /// <summary>Validação da query.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.Frequency).IsInEnum().When(x => x.Frequency.HasValue);
            RuleFor(x => x.Status).IsInEnum().When(x => x.Status.HasValue);
        }
    }

    /// <summary>Handler que lista briefings executivos com filtros opcionais.</summary>
    public sealed class Handler(
        IExecutiveBriefingRepository repository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            var briefings = await repository.ListAsync(request.Frequency, request.Status, cancellationToken);

            var items = briefings
                .Select(b => new ExecutiveBriefingItemDto(
                    BriefingId: b.Id.Value,
                    Title: b.Title,
                    Status: b.Status,
                    Frequency: b.Frequency,
                    PeriodStart: b.PeriodStart,
                    PeriodEnd: b.PeriodEnd,
                    GeneratedAt: b.GeneratedAt,
                    GeneratedByAgent: b.GeneratedByAgent,
                    PublishedAt: b.PublishedAt,
                    ArchivedAt: b.ArchivedAt))
                .ToList();

            return Result<Response>.Success(new Response(
                Items: items,
                TotalCount: items.Count,
                FilteredFrequency: request.Frequency,
                FilteredStatus: request.Status));
        }
    }

    /// <summary>Resposta com a lista de briefings executivos.</summary>
    public sealed record Response(
        IReadOnlyList<ExecutiveBriefingItemDto> Items,
        int TotalCount,
        BriefingFrequency? FilteredFrequency,
        BriefingStatus? FilteredStatus);

    /// <summary>DTO resumido de um executive briefing para listagem.</summary>
    public sealed record ExecutiveBriefingItemDto(
        Guid BriefingId,
        string Title,
        BriefingStatus Status,
        BriefingFrequency Frequency,
        DateTimeOffset PeriodStart,
        DateTimeOffset PeriodEnd,
        DateTimeOffset GeneratedAt,
        string GeneratedByAgent,
        DateTimeOffset? PublishedAt,
        DateTimeOffset? ArchivedAt);
}
