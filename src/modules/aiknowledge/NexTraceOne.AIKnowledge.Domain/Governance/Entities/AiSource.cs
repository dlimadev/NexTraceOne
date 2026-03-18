using Ardalis.GuardClauses;

using MediatR;

using NexTraceOne.AIKnowledge.Domain.Governance.Enums;
using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;

namespace NexTraceOne.AIKnowledge.Domain.Governance.Entities;

/// <summary>
/// Representa uma fonte de dados registada para grounding e retrieval de IA.
/// Cada fonte possui tipo, classificação, política de acesso e estado de saúde
/// monitorizado pela plataforma.
///
/// Invariantes:
/// - Nome e SourceType são obrigatórios e imutáveis após registo.
/// - OwnerTeam é obrigatório — toda fonte tem um responsável.
/// - HealthStatus inicia como "Unknown" no registo.
/// - Fonte inicia sempre com IsEnabled = true.
/// </summary>
public sealed class AiSource : AuditableEntity<AiSourceId>
{
    private AiSource() { }

    /// <summary>Nome técnico da fonte (ex: "confluence-eng", "pg-contracts").</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>Nome de exibição amigável para a interface do utilizador.</summary>
    public string DisplayName { get; private set; } = string.Empty;

    /// <summary>Tipo da fonte — determina o mecanismo de conexão e indexação.</summary>
    public AiSourceType SourceType { get; private set; }

    /// <summary>Descrição operacional da fonte.</summary>
    public string Description { get; private set; } = string.Empty;

    /// <summary>Indica se a fonte está ativa e disponível para consultas de IA.</summary>
    public bool IsEnabled { get; private set; }

    /// <summary>Informação de conexão (connection string, URL, path) — tratada como sensível.</summary>
    public string ConnectionInfo { get; private set; } = string.Empty;

    /// <summary>Escopo da política de acesso (ex: "public", "team:platform", "role:architect").</summary>
    public string AccessPolicyScope { get; private set; } = string.Empty;

    /// <summary>Classificação de sensibilidade dos dados (ex: "internal", "confidential", "public").</summary>
    public string Classification { get; private set; } = string.Empty;

    /// <summary>Equipa responsável pela fonte de dados.</summary>
    public string OwnerTeam { get; private set; } = string.Empty;

    /// <summary>Estado de saúde da fonte (ex: "Healthy", "Degraded", "Unavailable", "Unknown").</summary>
    public string HealthStatus { get; private set; } = string.Empty;

    /// <summary>Data/hora UTC em que a fonte foi registada na plataforma.</summary>
    public DateTimeOffset RegisteredAt { get; private set; }

    /// <summary>
    /// Regista uma nova fonte de dados para grounding de IA com validações de invariantes.
    /// A fonte inicia ativa e com HealthStatus "Unknown".
    /// </summary>
    public static AiSource Register(
        string name,
        string displayName,
        AiSourceType sourceType,
        string description,
        string connectionInfo,
        string accessPolicyScope,
        string classification,
        string ownerTeam,
        DateTimeOffset registeredAt)
    {
        Guard.Against.NullOrWhiteSpace(name);
        Guard.Against.NullOrWhiteSpace(displayName);
        Guard.Against.NullOrWhiteSpace(connectionInfo);
        Guard.Against.NullOrWhiteSpace(ownerTeam);

        return new AiSource
        {
            Id = AiSourceId.New(),
            Name = name,
            DisplayName = displayName,
            SourceType = sourceType,
            Description = description ?? string.Empty,
            IsEnabled = true,
            ConnectionInfo = connectionInfo,
            AccessPolicyScope = accessPolicyScope ?? string.Empty,
            Classification = classification ?? string.Empty,
            OwnerTeam = ownerTeam,
            HealthStatus = "Unknown",
            RegisteredAt = registeredAt
        };
    }

    /// <summary>
    /// Atualiza os detalhes configuráveis da fonte.
    /// Permite ajustar nome de exibição, descrição, conexão, política de acesso e classificação.
    /// </summary>
    public Result<Unit> UpdateDetails(
        string displayName,
        string description,
        string connectionInfo,
        string accessPolicyScope,
        string classification)
    {
        Guard.Against.NullOrWhiteSpace(displayName);
        Guard.Against.NullOrWhiteSpace(connectionInfo);

        DisplayName = displayName;
        Description = description ?? string.Empty;
        ConnectionInfo = connectionInfo;
        AccessPolicyScope = accessPolicyScope ?? string.Empty;
        Classification = classification ?? string.Empty;
        return Unit.Value;
    }

    /// <summary>
    /// Ativa a fonte, tornando-a disponível para consultas de IA.
    /// Operação idempotente — não retorna erro se já ativa.
    /// </summary>
    public Result<Unit> Enable()
    {
        IsEnabled = true;
        return Unit.Value;
    }

    /// <summary>
    /// Desativa a fonte, removendo-a do pool de fontes disponíveis.
    /// Operação idempotente.
    /// </summary>
    public Result<Unit> Disable()
    {
        IsEnabled = false;
        return Unit.Value;
    }

    /// <summary>
    /// Atualiza o estado de saúde da fonte com base em verificação ou monitorização.
    /// </summary>
    public Result<Unit> UpdateHealth(string healthStatus)
    {
        Guard.Against.NullOrWhiteSpace(healthStatus);

        HealthStatus = healthStatus;
        return Unit.Value;
    }
}

/// <summary>Identificador fortemente tipado de AiSource.</summary>
public sealed record AiSourceId(Guid Value) : TypedIdBase(Value)
{
    /// <summary>Cria novo Id com Guid gerado automaticamente.</summary>
    public static AiSourceId New() => new(Guid.NewGuid());

    /// <summary>Cria Id a partir de Guid existente.</summary>
    public static AiSourceId From(Guid id) => new(id);
}
