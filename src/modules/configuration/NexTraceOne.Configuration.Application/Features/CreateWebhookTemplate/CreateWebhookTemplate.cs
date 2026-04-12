using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Configuration.Application.Abstractions;
using NexTraceOne.Configuration.Domain.Entities;

namespace NexTraceOne.Configuration.Application.Features.CreateWebhookTemplate;

/// <summary>Feature: CreateWebhookTemplate — cria um novo template de payload personalizado para webhooks do tenant.</summary>
public static class CreateWebhookTemplate
{
    private static readonly string[] ValidEventTypes =
        ["change.created", "incident.opened", "contract.published", "approval.expired"];

    public sealed record Command(
        string Name,
        string EventType,
        string PayloadTemplate,
        string? HeadersJson) : ICommand<Response>;

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
            RuleFor(x => x.EventType).NotEmpty()
                .Must(e => ValidEventTypes.Contains(e, StringComparer.OrdinalIgnoreCase))
                .WithMessage($"EventType must be one of: {string.Join(", ", ValidEventTypes)}");
            RuleFor(x => x.PayloadTemplate).NotEmpty();
        }
    }

    public sealed class Handler(
        IWebhookTemplateRepository repository,
        ICurrentTenant currentTenant,
        ICurrentUser currentUser,
        IDateTimeProvider clock) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            if (!currentUser.IsAuthenticated)
                return Error.Unauthorized("User.NotAuthenticated", "User must be authenticated.");

            var template = WebhookTemplate.Create(
                currentTenant.Id.ToString(),
                request.Name,
                request.EventType,
                request.PayloadTemplate,
                request.HeadersJson,
                clock.UtcNow);

            await repository.AddAsync(template, cancellationToken);

            return new Response(template.Id.Value, template.Name, template.EventType, template.IsEnabled, template.CreatedAt);
        }
    }

    public sealed record Response(Guid TemplateId, string Name, string EventType, bool IsEnabled, DateTimeOffset CreatedAt);
}
