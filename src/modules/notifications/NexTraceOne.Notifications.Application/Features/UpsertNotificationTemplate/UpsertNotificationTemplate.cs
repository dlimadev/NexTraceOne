using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Notifications.Application.Abstractions;
using NexTraceOne.Notifications.Domain.Entities;
using NexTraceOne.Notifications.Domain.Enums;
using NexTraceOne.Notifications.Domain.StronglyTypedIds;

namespace NexTraceOne.Notifications.Application.Features.UpsertNotificationTemplate;

/// <summary>
/// Feature: UpsertNotificationTemplate — cria ou atualiza um template de notificação persistido.
/// </summary>
public static class UpsertNotificationTemplate
{
    /// <summary>Comando para criar ou atualizar um template.</summary>
    public sealed record Command(
        Guid? Id,
        string EventType,
        string Name,
        string SubjectTemplate,
        string BodyTemplate,
        string? PlainTextTemplate,
        string? Channel,
        string Locale = "en") : ICommand<Response>;

    /// <summary>Valida a entrada do comando.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.EventType).NotEmpty().MaximumLength(300);
            RuleFor(x => x.Name).NotEmpty().MaximumLength(500);
            RuleFor(x => x.SubjectTemplate).NotEmpty().MaximumLength(1000);
            RuleFor(x => x.BodyTemplate).NotEmpty();
            RuleFor(x => x.PlainTextTemplate).MaximumLength(8000).When(x => x.PlainTextTemplate is not null);
            RuleFor(x => x.Locale).NotEmpty().MaximumLength(10);
            RuleFor(x => x.Channel)
                .Must(c => c is null || Enum.TryParse<DeliveryChannel>(c, ignoreCase: true, out _))
                .WithMessage("Invalid delivery channel.");
        }
    }

    /// <summary>Handler que cria ou atualiza o template.</summary>
    public sealed class Handler(
        INotificationTemplateStore store,
        ICurrentTenant tenant) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            DeliveryChannel? channel = request.Channel is not null
                ? Enum.Parse<DeliveryChannel>(request.Channel, ignoreCase: true)
                : null;

            // Atualizar template existente
            if (request.Id.HasValue)
            {
                var existing = await store.GetByIdAsync(
                    new NotificationTemplateId(request.Id.Value),
                    cancellationToken);

                if (existing is null)
                    return Error.NotFound(
                        "NotificationTemplate.NotFound",
                        "Notification template {0} not found.",
                        request.Id.Value.ToString());

                if (existing.TenantId != tenant.Id)
                    return Error.Forbidden(
                        "NotificationTemplate.Forbidden",
                        "Access denied to notification template {0}.",
                        request.Id.Value.ToString());

                existing.Update(
                    request.Name,
                    request.SubjectTemplate,
                    request.BodyTemplate,
                    request.PlainTextTemplate);

                await store.SaveChangesAsync(cancellationToken);
                return new Response(existing.Id.Value, false);
            }

            // Criar novo template
            var template = NotificationTemplate.Create(
                tenant.Id,
                request.EventType,
                request.Name,
                request.SubjectTemplate,
                request.BodyTemplate,
                request.PlainTextTemplate,
                channel,
                request.Locale);

            await store.AddAsync(template, cancellationToken);
            await store.SaveChangesAsync(cancellationToken);

            return new Response(template.Id.Value, true);
        }
    }

    /// <summary>Resposta do comando de upsert de template.</summary>
    public sealed record Response(Guid Id, bool Created);
}
