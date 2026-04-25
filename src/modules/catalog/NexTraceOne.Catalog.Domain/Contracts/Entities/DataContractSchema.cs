using Ardalis.GuardClauses;
using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;

namespace NexTraceOne.Catalog.Domain.Contracts.Entities;

/// <summary>
/// Identificador fortemente tipado para DataContractSchema.
/// </summary>
public sealed record DataContractSchemaId(Guid Value) : TypedIdBase(Value);

/// <summary>
/// Classificação de risco PII para colunas de um Data Contract.
/// </summary>
public enum PiiClassification
{
    /// <summary>Sem dados pessoais identificáveis.</summary>
    None = 0,
    /// <summary>Dados de baixo risco (ex: cidade, preferências gerais).</summary>
    Low = 1,
    /// <summary>Dados de risco médio (ex: nome, email genérico).</summary>
    Medium = 2,
    /// <summary>Dados de alto risco (ex: nome completo, email pessoal, telefone).</summary>
    High = 3,
    /// <summary>Dados críticos (ex: CPF, NIF, número de cartão de crédito, dados de saúde).</summary>
    Critical = 4
}

/// <summary>
/// Schema de um Data Contract — contrato de dados para tabelas/vistas/streams analíticos.
/// Define owner, SLA de frescura, schema de colunas com tipos e classificação PII, e sistema de origem.
///
/// Referência: CC-03, ADR-007.
/// Owner: módulo Catalog (Contracts).
/// </summary>
public sealed class DataContractSchema : Entity<DataContractSchemaId>
{
    /// <summary>Identificador da API Asset associada (contrato pai).</summary>
    public Guid ApiAssetId { get; private init; }

    /// <summary>Tenant proprietário.</summary>
    public string TenantId { get; private init; } = string.Empty;

    /// <summary>Owner do data contract (equipa ou indivíduo responsável pela qualidade).</summary>
    public string Owner { get; private set; } = string.Empty;

    /// <summary>SLA de frescura dos dados em horas (ex: 24 = dados atualizados diariamente).</summary>
    public int SlaFreshnessHours { get; private set; }

    /// <summary>Schema em JSON: array de colunas com {name, type, nullable, description, piiClassification}.</summary>
    public string SchemaJson { get; private set; } = "[]";

    /// <summary>Nível de classificação PII global do contrato (máximo das colunas).</summary>
    public PiiClassification PiiClassification { get; private set; }

    /// <summary>Sistema de origem dos dados (ex: "PostgreSQL", "Kafka", "Snowflake", "BigQuery").</summary>
    public string SourceSystem { get; private set; } = string.Empty;

    /// <summary>Número de colunas declaradas no schema.</summary>
    public int ColumnCount { get; private set; }

    /// <summary>Versão deste snapshot de schema.</summary>
    public int Version { get; private set; }

    /// <summary>Data/hora UTC em que o schema foi capturado/registado.</summary>
    public DateTimeOffset CapturedAt { get; private init; }

    /// <summary>Data/hora UTC de criação.</summary>
    public DateTimeOffset CreatedAt { get; private init; }

    private DataContractSchema() { }

    /// <summary>Cria um novo schema de Data Contract.</summary>
    public static DataContractSchema Create(
        Guid apiAssetId,
        string tenantId,
        string owner,
        int slaFreshnessHours,
        string schemaJson,
        PiiClassification piiClassification,
        string sourceSystem,
        int columnCount,
        int version,
        DateTimeOffset utcNow)
    {
        Guard.Against.Default(apiAssetId, nameof(apiAssetId));
        Guard.Against.NullOrWhiteSpace(tenantId, nameof(tenantId));
        Guard.Against.NullOrWhiteSpace(owner, nameof(owner));
        Guard.Against.StringTooLong(owner, 200, nameof(owner));
        Guard.Against.Negative(slaFreshnessHours, nameof(slaFreshnessHours));
        Guard.Against.NullOrWhiteSpace(schemaJson, nameof(schemaJson));
        Guard.Against.NullOrWhiteSpace(sourceSystem, nameof(sourceSystem));
        Guard.Against.StringTooLong(sourceSystem, 100, nameof(sourceSystem));
        Guard.Against.Negative(columnCount, nameof(columnCount));

        return new DataContractSchema
        {
            Id = new DataContractSchemaId(Guid.NewGuid()),
            ApiAssetId = apiAssetId,
            TenantId = tenantId,
            Owner = owner.Trim(),
            SlaFreshnessHours = slaFreshnessHours,
            SchemaJson = schemaJson,
            PiiClassification = piiClassification,
            SourceSystem = sourceSystem.Trim(),
            ColumnCount = columnCount,
            Version = version,
            CapturedAt = utcNow,
            CreatedAt = utcNow
        };
    }

    /// <summary>Verifica se o SLA de frescura foi violado com base no timestamp da última actualização.</summary>
    public bool IsFreshnessViolated(DateTimeOffset lastUpdatedAt, DateTimeOffset now)
        => (now - lastUpdatedAt).TotalHours > SlaFreshnessHours;
}
