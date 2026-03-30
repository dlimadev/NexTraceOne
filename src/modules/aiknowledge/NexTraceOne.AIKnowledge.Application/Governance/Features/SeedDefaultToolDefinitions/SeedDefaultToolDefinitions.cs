using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.AIKnowledge.Domain.Governance.Entities;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.AIKnowledge.Application.Governance.Features.SeedDefaultToolDefinitions;

/// <summary>
/// Seed das definições de ferramentas oficiais da plataforma.
/// Idempotente: não duplica ferramentas já existentes (comparação por nome, case-insensitive).
/// </summary>
public static class SeedDefaultToolDefinitions
{
    public sealed record Command : ICommand<Response>;

    public sealed record Response(int ToolsSeeded, int TotalInCatalog);

    internal sealed class Handler(IAiToolDefinitionRepository repository) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            var catalog = DefaultToolDefinitionCatalog.GetAll();
            var existing = await repository.GetAllActiveAsync(cancellationToken);

            var existingNames = new HashSet<string>(
                existing.Select(t => t.Name),
                StringComparer.OrdinalIgnoreCase);

            var seeded = 0;

            foreach (var definition in catalog)
            {
                if (existingNames.Contains(definition.Name))
                    continue;

                var tool = AiToolDefinition.Create(
                    name: definition.Name,
                    displayName: definition.DisplayName,
                    description: definition.Description,
                    category: definition.Category,
                    parametersSchema: definition.ParametersSchema,
                    version: 1,
                    isActive: true,
                    requiresApproval: definition.RequiresApproval,
                    riskLevel: definition.RiskLevel,
                    isOfficial: true,
                    timeoutMs: definition.TimeoutMs);

                await repository.AddAsync(tool, cancellationToken);
                seeded++;
            }

            return Result<Response>.Success(new Response(seeded, catalog.Count));
        }
    }
}
