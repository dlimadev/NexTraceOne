using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.AIKnowledge.Domain.Governance.Entities;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.AIKnowledge.Application.Governance.Features.SeedDefaultGuardrails;

/// <summary>
/// Seed dos guardrails oficiais da plataforma.
/// Idempotente: não duplica guardrails já existentes (comparação por nome, case-insensitive).
/// </summary>
public static class SeedDefaultGuardrails
{
    public sealed record Command : ICommand<Response>;

    public sealed record Response(int GuardrailsSeeded, int TotalInCatalog);

    internal sealed class Handler(IAiGuardrailRepository repository) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            var catalog = DefaultGuardrailCatalog.GetAll();
            var existing = await repository.GetAllActiveAsync(cancellationToken);

            var existingNames = new HashSet<string>(
                existing.Select(g => g.Name),
                StringComparer.OrdinalIgnoreCase);

            var seeded = 0;

            foreach (var definition in catalog)
            {
                if (existingNames.Contains(definition.Name))
                    continue;

                var guardrail = AiGuardrail.Create(
                    name: definition.Name,
                    displayName: definition.DisplayName,
                    description: definition.Description,
                    category: definition.Category,
                    guardType: definition.GuardType,
                    pattern: definition.Pattern,
                    patternType: definition.PatternType,
                    severity: definition.Severity,
                    action: definition.Action,
                    userMessage: definition.UserMessage,
                    isActive: true,
                    isOfficial: true,
                    agentId: null,
                    modelId: null,
                    priority: definition.Priority);

                await repository.AddAsync(guardrail, cancellationToken);
                seeded++;
            }

            return Result<Response>.Success(new Response(seeded, catalog.Count));
        }
    }
}
