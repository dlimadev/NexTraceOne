using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Configuration.Application.Abstractions;
using NexTraceOne.Configuration.Domain.Entities;

namespace NexTraceOne.Configuration.Application.Features.DeleteWebhookTemplate;

/// <summary>Feature: DeleteWebhookTemplate — remove um template de webhook do tenant.</summary>
public static class DeleteWebhookTemplate
{
    public sealed record Command(Guid TemplateId) : ICommand<Response>;

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.TemplateId).NotEmpty();
        }
    }

    public sealed class Handler(
        IWebhookTemplateRepository repository,
        ICurrentUser currentUser) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            if (!currentUser.IsAuthenticated)
                return Error.Unauthorized("User.NotAuthenticated", "User must be authenticated.");

            var template = await repository.GetByIdAsync(new WebhookTemplateId(request.TemplateId), cancellationToken);
            if (template is null)
                return Error.NotFound("WebhookTemplate.NotFound", $"Webhook template '{request.TemplateId}' not found.");

            await repository.DeleteAsync(new WebhookTemplateId(request.TemplateId), cancellationToken);
            return new Response(request.TemplateId);
        }
    }

    public sealed record Response(Guid TemplateId);
}
