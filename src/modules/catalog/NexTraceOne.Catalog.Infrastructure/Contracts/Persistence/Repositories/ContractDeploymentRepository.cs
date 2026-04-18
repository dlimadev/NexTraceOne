using Microsoft.EntityFrameworkCore;

using NexTraceOne.Catalog.Application.Contracts.Abstractions;
using NexTraceOne.Catalog.Domain.Contracts.Entities;
using NexTraceOne.Catalog.Domain.Contracts.Enums;

namespace NexTraceOne.Catalog.Infrastructure.Contracts.Persistence.Repositories;

/// <summary>
/// Repositório de deployments de versões de contrato.
/// Implementa consultas específicas de rastreabilidade de mudanças por ambiente.
/// </summary>
internal sealed class ContractDeploymentRepository(ContractsDbContext context)
    : IContractDeploymentRepository
{
    /// <summary>Busca um deployment pelo seu identificador.</summary>
    public async Task<ContractDeployment?> GetByIdAsync(ContractDeploymentId id, CancellationToken ct = default)
        => await context.ContractDeployments
            .SingleOrDefaultAsync(d => d.Id == id, ct);

    /// <summary>Lista todos os deployments de uma versão de contrato, do mais recente para o mais antigo.</summary>
    public async Task<IReadOnlyList<ContractDeployment>> ListByContractVersionAsync(
        ContractVersionId contractVersionId, CancellationToken ct = default)
        => await context.ContractDeployments
            .Where(d => d.ContractVersionId == contractVersionId)
            .OrderByDescending(d => d.DeployedAt)
            .ToListAsync(ct);

    /// <summary>Adiciona um novo deployment ao repositório via change tracking do EF Core.</summary>
    public void Add(ContractDeployment deployment)
        => context.ContractDeployments.Add(deployment);

    /// <summary>
    /// Retorna o deployment mais recente bem-sucedido por ambiente para um ativo de API.
    /// Agrupa por Environment e selecciona o mais recente de cada grupo.
    /// </summary>
    public async Task<IReadOnlyDictionary<string, ContractDeployment>> GetLatestSuccessfulByEnvironmentAsync(
        Guid apiAssetId, CancellationToken ct = default)
    {
        var deployments = await context.ContractDeployments
            .Where(d => d.ApiAssetId == apiAssetId
                        && d.Status == ContractDeploymentStatus.Success)
            .OrderByDescending(d => d.DeployedAt)
            .ToListAsync(ct);

        // Mantém apenas o deployment mais recente por ambiente
        return deployments
            .GroupBy(d => d.Environment, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                g => g.Key,
                g => g.First(),
                StringComparer.OrdinalIgnoreCase);
    }
}
