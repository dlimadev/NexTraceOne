using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Notifications.Application.Abstractions;
using NexTraceOne.Notifications.Domain.Enums;

namespace NexTraceOne.Notifications.Application.Features.ListNotificationTemplates;

/// <summary>
/// Feature: ListNotificationTemplates — lista os templates de notificação disponíveis para o tenant.
/// </summary>
public static class ListNotificationTemplates
{
    /// <summary>Query de listagem de templates com filtros opcionais.</summary>
    public sealed record Query(
        string? EventType,
        string? Channel,
        bool? IsActive) : IQuery<Response>;

    /// <summary>Valida a entrada da query.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.Channel)
                .Must(c => c is null || Enum.TryParse<DeliveryChannel>(c, ignoreCase: true, out _))
                .WithMessage("Invalid delivery channel.");
        }
    }

    /// <summary>Handler que lista templates do tenant autenticado.</summary>
    public sealed class Handler(
        INotificationTemplateStore store,
        ICurrentTenant tenant) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            DeliveryChannel? channel = null;
            if (request.Channel is not null)
                channel = Enum.Parse<DeliveryChannel>(request.Channel, ignoreCase: true);

            var templates = await store.ListAsync(
                tenant.Id,
                request.EventType,
                channel,
                request.IsActive,
                cancellationToken);

            var items = templates
                .Select(t => new TemplateDto(
                    t.Id.Value,
                    t.EventType,
                    t.Name,
                    t.SubjectTemplate,
                    t.BodyTemplate,
                    t.PlainTextTemplate,
                    t.Channel?.ToString(),
                    t.Locale,
                    t.IsActive,
                    t.IsBuiltIn,
                    t.CreatedAt,
                    t.UpdatedAt))
                .ToList();

            return new Response(items);
        }
    }

    /// <summary>Resposta com a lista de templates.</summary>
    public sealed record Response(IReadOnlyList<TemplateDto> Items);

    /// <summary>DTO de template de notificação.</summary>
    public sealed record TemplateDto(
        Guid Id,
        string EventType,
        string Name,
        string SubjectTemplate,
        string BodyTemplate,
        string? PlainTextTemplate,
        string? Channel,
        string Locale,
        bool IsActive,
        bool IsBuiltIn,
        DateTimeOffset CreatedAt,
        DateTimeOffset UpdatedAt);
}
