using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.AIKnowledge.Domain.Governance.Entities;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.AIKnowledge.Application.Governance.Features.RegisterPromptAsset;

/// <summary>
/// Feature: RegisterPromptAsset — regista um novo PromptAsset com versão inicial.
/// O slug é imutável após criação e deve ser único por tenant.
/// Estrutura VSA: Command + Validator + Handler + Response.
/// </summary>
public static class RegisterPromptAsset
{
    public sealed record Command(
        string Slug,
        string Name,
        string Description,
        string Category,
        string InitialContent,
        string Variables,
        string Tags,
        Guid? TenantId,
        string CreatedBy) : ICommand<Response>;

    public sealed class Validator : AbstractValidator<Command>
    {
        private static readonly string[] ValidCategories =
            ["system", "few-shot", "rag", "instruction", "chain-of-thought"];

        public Validator()
        {
            RuleFor(x => x.Slug)
                .NotEmpty()
                .MaximumLength(200)
                .Matches("^[a-z0-9][a-z0-9\\-]*[a-z0-9]$")
                .WithMessage("Slug must be lowercase alphanumeric with hyphens (e.g. 'incident-root-cause').");
            RuleFor(x => x.Name).NotEmpty().MaximumLength(300);
            RuleFor(x => x.Category)
                .NotEmpty()
                .Must(c => ValidCategories.Contains(c, StringComparer.OrdinalIgnoreCase))
                .WithMessage($"Category must be one of: {string.Join(", ", ValidCategories)}.");
            RuleFor(x => x.InitialContent).NotEmpty();
            RuleFor(x => x.CreatedBy).NotEmpty().MaximumLength(200);
        }
    }

    public sealed class Handler(
        IPromptAssetRepository repository) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            if (await repository.SlugExistsAsync(request.Slug, request.TenantId, cancellationToken))
                return Error.Business(
                    "AiGovernance.PromptAsset.SlugAlreadyExists",
                    $"A PromptAsset with slug '{request.Slug}' already exists.");

            var asset = PromptAsset.Create(
                slug: request.Slug,
                name: request.Name,
                description: request.Description,
                category: request.Category,
                initialContent: request.InitialContent,
                variables: request.Variables,
                tags: request.Tags,
                tenantId: request.TenantId,
                createdBy: request.CreatedBy);

            await repository.AddAsync(asset, cancellationToken);

            return new Response(asset.Id.Value, asset.Slug, asset.Name, asset.CurrentVersionNumber);
        }
    }

    public sealed record Response(Guid AssetId, string Slug, string Name, int VersionNumber);
}
