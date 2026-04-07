using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Configuration.Application.Abstractions;
using NexTraceOne.Configuration.Domain.Entities;

namespace NexTraceOne.Configuration.Application.Features.ToggleWebhookTemplate;

/// <summary>Feature: ToggleWebhookTemplate — activa ou desactiva um template de webhook.</summary>
public static class ToggleWebhookTemplate
{
    public sealed record Command(Guid TemplateId, bool Enabled) : ICommand<bool>;

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.TemplateId).NotEmpty();
        }
    }

    public sealed class Handler(
        IWebhookTemplateRepository repository,
        ICurrentUser currentUser) : ICommandHandler<Command, bool>
    {
        public async Task<Result<bool>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            if (!currentUser.IsAuthenticated)
                return Error.Unauthorized("User.NotAuthenticated", "User must be authenticated.");

            var template = await repository.GetByIdAsync(new WebhookTemplateId(request.TemplateId), cancellationToken);
            if (template is null)
                return Error.NotFound("WebhookTemplate.NotFound", $"Webhook template '{request.TemplateId}' not found.");

            template.Toggle(request.Enabled);
            await repository.UpdateAsync(template, cancellationToken);
            return true;
        }
    }
}
