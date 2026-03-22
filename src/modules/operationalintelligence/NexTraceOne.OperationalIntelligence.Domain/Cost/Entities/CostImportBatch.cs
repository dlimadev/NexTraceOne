using Ardalis.GuardClauses;

using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;

namespace NexTraceOne.OperationalIntelligence.Domain.Cost.Entities;

/// <summary>
/// Aggregate Root que representa um lote de importação de registos de custo.
/// Rastreia a origem, período, estado e resultado de cada importação em massa,
/// garantindo auditabilidade e controlo sobre o pipeline de ingestão de custos.
///
/// Invariantes:
/// - Source e Period são obrigatórios.
/// - Status transita apenas de Pending → Completed ou Pending → Failed.
/// - RecordCount é atualizado somente quando o batch é completado.
///
/// REGRA DDD: A validação de invariantes ocorre no factory method — não há estado inválido.
/// </summary>
public sealed class CostImportBatch : AuditableEntity<CostImportBatchId>
{
    /// <summary>Status inicial de um batch recém-criado.</summary>
    public const string StatusPending = "Pending";

    /// <summary>Status de um batch processado com sucesso.</summary>
    public const string StatusCompleted = "Completed";

    /// <summary>Status de um batch que falhou durante o processamento.</summary>
    public const string StatusFailed = "Failed";

    private CostImportBatch() { }

    /// <summary>Fonte dos dados de custo importados (ex: "AWS CUR", "Azure Cost Management").</summary>
    public string Source { get; private set; } = string.Empty;

    /// <summary>Período ao qual os dados de custo se referem (ex: "2026-03").</summary>
    public string Period { get; private set; } = string.Empty;

    /// <summary>Moeda dos valores de custo (padrão: USD).</summary>
    public string Currency { get; private set; } = "USD";

    /// <summary>Número de registos importados neste batch.</summary>
    public int RecordCount { get; private set; }

    /// <summary>Estado atual do batch: Pending, Completed ou Failed.</summary>
    public string Status { get; private set; } = StatusPending;

    /// <summary>Mensagem de erro caso o batch tenha falhado. Null se bem-sucedido.</summary>
    public string? Error { get; private set; }

    /// <summary>Data/hora UTC em que a importação foi iniciada.</summary>
    public DateTimeOffset ImportedAt { get; private set; }

    /// <summary>
    /// Factory method para criação de um novo batch de importação de custo validado.
    /// Garante todas as invariantes do aggregate — não existe CostImportBatch em estado inválido.
    /// </summary>
    public static Result<CostImportBatch> Create(
        string source,
        string period,
        DateTimeOffset importedAt,
        string currency = "USD")
    {
        Guard.Against.NullOrWhiteSpace(source);
        Guard.Against.NullOrWhiteSpace(period);
        Guard.Against.NullOrWhiteSpace(currency);

        return new CostImportBatch
        {
            Id = CostImportBatchId.New(),
            Source = source,
            Period = period,
            Currency = currency,
            Status = StatusPending,
            RecordCount = 0,
            ImportedAt = importedAt
        };
    }

    /// <summary>
    /// Marca o batch como completo após processamento bem-sucedido de todos os registos.
    /// Atualiza o número de registos importados.
    /// </summary>
    /// <param name="recordCount">Número total de registos importados no batch.</param>
    public void Complete(int recordCount)
    {
        Guard.Against.Negative(recordCount);
        RecordCount = recordCount;
        Status = StatusCompleted;
    }

    /// <summary>
    /// Marca o batch como falhado, registando a mensagem de erro para diagnóstico.
    /// </summary>
    /// <param name="error">Descrição do erro que impediu o processamento do batch.</param>
    public void Fail(string error)
    {
        Guard.Against.NullOrWhiteSpace(error);
        Error = error;
        Status = StatusFailed;
    }
}

/// <summary>Identificador fortemente tipado de CostImportBatch.</summary>
public sealed record CostImportBatchId(Guid Value) : TypedIdBase(Value)
{
    /// <summary>Cria novo Id com Guid gerado automaticamente.</summary>
    public static CostImportBatchId New() => new(Guid.NewGuid());

    /// <summary>Cria Id a partir de Guid existente.</summary>
    public static CostImportBatchId From(Guid id) => new(id);
}
