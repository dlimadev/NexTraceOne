using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.AIKnowledge.Domain.Governance.Entities;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.AIKnowledge.Application.Governance.Features.SeedDefaultPromptTemplates;

/// <summary>
/// Seed dos templates de prompt oficiais da plataforma.
/// Idempotente: não duplica templates já existentes (comparação por nome, case-insensitive).
/// </summary>
public static class SeedDefaultPromptTemplates
{
    public sealed record Command : ICommand<Response>;

    public sealed record Response(int TemplatesSeeded, int TotalInCatalog);

    internal sealed class Handler(IPromptTemplateRepository repository) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            var catalog = DefaultPromptTemplateCatalog.GetAll();
            var existing = await repository.GetAllActiveAsync(cancellationToken);

            var existingNames = new HashSet<string>(
                existing.Select(t => t.Name),
                StringComparer.OrdinalIgnoreCase);

            var seeded = 0;

            foreach (var definition in catalog)
            {
                if (existingNames.Contains(definition.Name))
                    continue;

                var template = PromptTemplate.Create(
                    name: definition.Name,
                    displayName: definition.DisplayName,
                    description: definition.Description,
                    category: definition.Category,
                    content: definition.Content,
                    variables: definition.Variables,
                    version: 1,
                    isActive: true,
                    isOfficial: true,
                    agentId: null,
                    targetPersonas: definition.TargetPersonas,
                    scopeHint: definition.ScopeHint,
                    relevance: definition.Relevance);

                await repository.AddAsync(template, cancellationToken);
                seeded++;
            }

            return Result<Response>.Success(new Response(seeded, catalog.Count));
        }
    }
}
