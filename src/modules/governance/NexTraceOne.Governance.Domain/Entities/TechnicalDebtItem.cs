using Ardalis.GuardClauses;
using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;

namespace NexTraceOne.Governance.Domain.Entities;

/// <summary>
/// Identificador fortemente tipado para a entidade TechnicalDebtItem.
/// </summary>
public sealed record TechnicalDebtItemId(Guid Value) : TypedIdBase(Value);

/// <summary>
/// Representa um item de dívida técnica registado para um serviço.
/// Inclui classificação por tipo, severidade, esforço estimado e score de risco.
/// Aggregate root com ciclo de vida próprio.
/// </summary>
public sealed class TechnicalDebtItem : Entity<TechnicalDebtItemId>
{
    private static readonly string[] ValidDebtTypes =
        ["architecture", "code-quality", "security", "dependency", "documentation", "testing", "performance", "infrastructure"];

    private static readonly string[] ValidSeverities =
        ["critical", "high", "medium", "low"];

    /// <summary>Nome do serviço associado a este item de dívida técnica.</summary>
    public string ServiceName { get; private init; } = string.Empty;

    /// <summary>Tipo de dívida técnica (ex: "architecture", "security").</summary>
    public string DebtType { get; private init; } = string.Empty;

    /// <summary>Título descritivo do item de dívida técnica (máx. 200 caracteres).</summary>
    public string Title { get; private init; } = string.Empty;

    /// <summary>Descrição detalhada do item (máx. 1000 caracteres).</summary>
    public string Description { get; private set; } = string.Empty;

    /// <summary>Severidade do item: critical, high, medium, low.</summary>
    public string Severity { get; private init; } = string.Empty;

    /// <summary>Esforço estimado em dias para resolução.</summary>
    public int EstimatedEffortDays { get; private set; }

    /// <summary>Score de risco calculado com base na severidade.</summary>
    public int DebtScore { get; private init; }

    /// <summary>Tags opcionais para classificação adicional.</summary>
    public string? Tags { get; private set; }

    /// <summary>Identificador do tenant proprietário.</summary>
    public string TenantId { get; private init; } = string.Empty;

    /// <summary>Data/hora UTC de criação.</summary>
    public DateTimeOffset CreatedAt { get; private init; }

    /// <summary>Data/hora UTC da última modificação.</summary>
    public DateTimeOffset UpdatedAt { get; private set; }

    /// <summary>Token de concorrência otimista (PostgreSQL xmin).</summary>
    public uint RowVersion { get; set; }

    /// <summary>Construtor privado para EF Core.</summary>
    private TechnicalDebtItem() { }

    /// <summary>
    /// Cria um novo item de dívida técnica com validação de invariantes.
    /// O score é calculado automaticamente a partir da severidade.
    /// </summary>
    public static TechnicalDebtItem Create(
        string serviceName,
        string debtType,
        string title,
        string description,
        string severity,
        int estimatedEffortDays,
        string? tags,
        string tenantId,
        DateTimeOffset now)
    {
        Guard.Against.NullOrWhiteSpace(serviceName, nameof(serviceName));
        Guard.Against.StringTooLong(serviceName, 200, nameof(serviceName));
        Guard.Against.NullOrWhiteSpace(title, nameof(title));
        Guard.Against.StringTooLong(title, 200, nameof(title));
        Guard.Against.NullOrWhiteSpace(description, nameof(description));
        Guard.Against.StringTooLong(description, 1000, nameof(description));
        Guard.Against.NullOrWhiteSpace(tenantId, nameof(tenantId));

        if (!ValidDebtTypes.Contains(debtType))
            throw new ArgumentException($"DebtType must be one of: {string.Join(", ", ValidDebtTypes)}", nameof(debtType));

        if (!ValidSeverities.Contains(severity))
            throw new ArgumentException($"Severity must be one of: {string.Join(", ", ValidSeverities)}", nameof(severity));

        Guard.Against.OutOfRange(estimatedEffortDays, nameof(estimatedEffortDays), 1, 999);

        return new TechnicalDebtItem
        {
            Id = new TechnicalDebtItemId(Guid.NewGuid()),
            ServiceName = serviceName.Trim(),
            DebtType = debtType,
            Title = title.Trim(),
            Description = description.Trim(),
            Severity = severity,
            EstimatedEffortDays = estimatedEffortDays,
            DebtScore = ComputeDebtScore(severity),
            Tags = tags?.Trim(),
            TenantId = tenantId,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    /// <summary>
    /// Atualiza a descrição, esforço estimado e tags do item.
    /// </summary>
    public void Update(string description, int estimatedEffortDays, string? tags, DateTimeOffset now)
    {
        Guard.Against.NullOrWhiteSpace(description, nameof(description));
        Guard.Against.StringTooLong(description, 1000, nameof(description));
        Guard.Against.OutOfRange(estimatedEffortDays, nameof(estimatedEffortDays), 1, 999);

        Description = description.Trim();
        EstimatedEffortDays = estimatedEffortDays;
        Tags = tags?.Trim();
        UpdatedAt = now;
    }

    private static int ComputeDebtScore(string severity) => severity switch
    {
        "critical" => 40,
        "high" => 25,
        "medium" => 10,
        "low" => 5,
        _ => 0
    };
}
