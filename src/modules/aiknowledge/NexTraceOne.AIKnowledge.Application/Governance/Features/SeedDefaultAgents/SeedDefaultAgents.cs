using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.AIKnowledge.Domain.Governance.Entities;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.AIKnowledge.Application.Governance.Features.SeedDefaultAgents;

/// <summary>
/// Feature: SeedDefaultAgents — popula a tabela ai_agents com os agents oficiais
/// do <see cref="DefaultAgentCatalog"/> quando ainda não existem agents com o
/// mesmo nome no catálogo.
///
/// Objetivo de produto: permitir que o PlatformAdmin inicialize o catálogo de
/// agents com agents oficiais pré-configurados para operação imediata,
/// cobrindo os principais domínios do NexTraceOne (serviços, contratos, mudanças,
/// incidentes, testes, documentação, segurança, eventos).
///
/// Comportamento idempotente: agents já existentes (por nome) não são duplicados.
/// </summary>
public static class SeedDefaultAgents
{
    /// <summary>Comando sem parâmetros — a seed é determinística a partir do catálogo.</summary>
    public sealed record Command : ICommand<Response>;

    /// <summary>Resposta com contagem de agents criados.</summary>
    public sealed record Response(int AgentsSeeded, int TotalInCatalog);

    /// <summary>Handler que popula agents oficiais do catálogo para nomes ainda não registados.</summary>
    public sealed class Handler(
        IAiAgentRepository agentRepository) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            var catalog = DefaultAgentCatalog.GetAll();
            var existing = await agentRepository.ListAsync(
                isActive: null, isOfficial: null, cancellationToken);

            var existingNames = new HashSet<string>(
                existing.Select(a => a.Name),
                StringComparer.OrdinalIgnoreCase);

            var seeded = 0;

            foreach (var def in catalog)
            {
                if (existingNames.Contains(def.Name))
                    continue;

                var agent = AiAgent.Register(
                    name: def.Name,
                    displayName: def.DisplayName,
                    description: def.Description,
                    category: def.Category,
                    isOfficial: true,
                    systemPrompt: def.SystemPrompt,
                    capabilities: def.Capabilities,
                    targetPersona: def.TargetPersona,
                    icon: def.Icon,
                    sortOrder: def.SortOrder);

                await agentRepository.AddAsync(agent, cancellationToken);
                seeded++;
            }

            return new Response(seeded, catalog.Count);
        }
    }
}
