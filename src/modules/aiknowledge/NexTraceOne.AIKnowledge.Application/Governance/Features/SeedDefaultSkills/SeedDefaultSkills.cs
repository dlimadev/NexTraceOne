using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.AIKnowledge.Domain.Governance.Entities;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.AIKnowledge.Application.Governance.Features.SeedDefaultSkills;

/// <summary>
/// Feature: SeedDefaultSkills — popula a tabela de skills com as skills oficiais
/// do <see cref="DefaultSkillCatalog"/> quando ainda não existem para o tenant.
///
/// Comportamento idempotente: skills já existentes (por nome) não são duplicadas.
/// </summary>
public static class SeedDefaultSkills
{
    /// <summary>Comando de seed com identificador do tenant alvo.</summary>
    public sealed record Command(Guid TenantId) : ICommand<Response>;

    /// <summary>Resposta com contagem de skills criadas.</summary>
    public sealed record Response(int SeededCount);

    /// <summary>Handler que popula skills oficiais do catálogo para nomes ainda não registados.</summary>
    public sealed class Handler(
        IAiSkillRepository skillRepository) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            var catalog = DefaultSkillCatalog.GetAll();

            var existing = await skillRepository.ListAsync(
                status: null,
                ownershipType: null,
                tenantId: null,
                ct: cancellationToken);

            var existingNames = new HashSet<string>(
                existing.Select(s => s.Name),
                StringComparer.OrdinalIgnoreCase);

            var seeded = 0;

            foreach (var skill in catalog)
            {
                if (existingNames.Contains(skill.Name))
                    continue;

                skillRepository.Add(skill);
                seeded++;
            }

            return new Response(seeded);
        }
    }
}
