using Ardalis.GuardClauses;

using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;
using NexTraceOne.Catalog.Domain.Contracts.Enums;

namespace NexTraceOne.Catalog.Domain.Contracts.Entities;

/// <summary>
/// Entidade que representa uma simulação de impacto de dependências entre serviços.
/// Permite avaliar cenários what-if (remoção de endpoint, indisponibilidade, migração de contrato,
/// alteração de schema) para estimar serviços afetados, consumidores que quebrariam,
/// profundidade de cascata transitiva, nível de risco e recomendações de mitigação.
/// Suporta os pilares de Service Governance e Change Intelligence do NexTraceOne.
/// </summary>
public sealed class ImpactSimulation : AuditableEntity<ImpactSimulationId>
{
    private ImpactSimulation() { }

    /// <summary>Nome do serviço alvo da simulação de impacto.</summary>
    public string ServiceName { get; private set; } = string.Empty;

    /// <summary>Tipo de cenário simulado (EndpointRemoval, ServiceUnavailability, etc.).</summary>
    public ImpactSimulationScenario Scenario { get; private set; }

    /// <summary>Descrição textual do cenário what-if em linguagem natural.</summary>
    public string ScenarioDescription { get; private set; } = string.Empty;

    /// <summary>Lista de serviços afetados pela simulação (JSONB).</summary>
    public string? AffectedServices { get; private set; }

    /// <summary>Lista de consumidores que quebrariam com o cenário simulado (JSONB).</summary>
    public string? BrokenConsumers { get; private set; }

    /// <summary>Profundidade máxima da cascata transitiva de impacto.</summary>
    public int TransitiveCascadeDepth { get; private set; }

    /// <summary>Percentual de risco estimado (0 a 100).</summary>
    public int RiskPercent { get; private set; }

    /// <summary>Recomendações de mitigação geradas por IA (JSONB).</summary>
    public string? MitigationRecommendations { get; private set; }

    /// <summary>Momento em que a simulação foi executada.</summary>
    public DateTimeOffset SimulatedAt { get; private set; }

    /// <summary>Identificador do tenant (multi-tenancy).</summary>
    public string? TenantId { get; private set; }

    /// <summary>Token de concorrência otimista (PostgreSQL xmin).</summary>
    public uint RowVersion { get; set; }

    /// <summary>
    /// Cria uma nova simulação de impacto de dependências entre serviços,
    /// validando todos os parâmetros obrigatórios e limites de negócio.
    /// </summary>
    public static ImpactSimulation Simulate(
        string serviceName,
        ImpactSimulationScenario scenario,
        string scenarioDescription,
        string? affectedServices,
        string? brokenConsumers,
        int transitiveCascadeDepth,
        int riskPercent,
        string? mitigationRecommendations,
        DateTimeOffset simulatedAt,
        string? tenantId = null)
    {
        Guard.Against.NullOrWhiteSpace(serviceName);
        Guard.Against.StringTooLong(serviceName, 200);
        Guard.Against.EnumOutOfRange(scenario);
        Guard.Against.NullOrWhiteSpace(scenarioDescription);
        Guard.Against.StringTooLong(scenarioDescription, 4000);
        Guard.Against.Negative(transitiveCascadeDepth, nameof(transitiveCascadeDepth));
        Guard.Against.OutOfRange(riskPercent, nameof(riskPercent), 0, 100);

        return new ImpactSimulation
        {
            Id = ImpactSimulationId.New(),
            ServiceName = serviceName.Trim(),
            Scenario = scenario,
            ScenarioDescription = scenarioDescription.Trim(),
            AffectedServices = affectedServices,
            BrokenConsumers = brokenConsumers,
            TransitiveCascadeDepth = transitiveCascadeDepth,
            RiskPercent = riskPercent,
            MitigationRecommendations = mitigationRecommendations,
            SimulatedAt = simulatedAt,
            TenantId = tenantId?.Trim()
        };
    }
}

/// <summary>Identificador fortemente tipado de ImpactSimulation.</summary>
public sealed record ImpactSimulationId(Guid Value) : TypedIdBase(Value)
{
    /// <summary>Cria novo Id com Guid gerado automaticamente.</summary>
    public static ImpactSimulationId New() => new(Guid.NewGuid());

    /// <summary>Cria Id a partir de Guid existente.</summary>
    public static ImpactSimulationId From(Guid id) => new(id);
}
