using Ardalis.GuardClauses;

using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;
using NexTraceOne.Catalog.Domain.Contracts.Enums;

namespace NexTraceOne.Catalog.Domain.Contracts.Entities;

/// <summary>
/// Entidade que representa a execução de um pipeline automatizado de geração de código
/// a partir de um contrato (API Asset). Regista os estágios solicitados, resultados,
/// artefactos gerados e estado de conclusão para rastreabilidade completa.
/// </summary>
public sealed class PipelineExecution : AuditableEntity<PipelineExecutionId>
{
    private PipelineExecution() { }

    /// <summary>Identificador do API Asset (contrato) processado.</summary>
    public Guid ApiAssetId { get; private set; }

    /// <summary>Nome do contrato para exibição.</summary>
    public string ContractName { get; private set; } = string.Empty;

    /// <summary>Versão do contrato processada.</summary>
    public string ContractVersion { get; private set; } = string.Empty;

    /// <summary>Lista de estágios solicitados (serializado JSONB).</summary>
    public string RequestedStages { get; private set; } = string.Empty;

    /// <summary>Resultados por estágio (serializado JSONB).</summary>
    public string? StageResults { get; private set; }

    /// <summary>Lista de artefactos gerados (serializado JSONB).</summary>
    public string? GeneratedArtifacts { get; private set; }

    /// <summary>Linguagem alvo da geração (e.g. "csharp", "typescript").</summary>
    public string TargetLanguage { get; private set; } = string.Empty;

    /// <summary>Framework alvo da geração (e.g. "aspnet", "express").</summary>
    public string? TargetFramework { get; private set; }

    /// <summary>Estado atual da execução do pipeline.</summary>
    public PipelineExecutionStatus Status { get; private set; }

    /// <summary>Número total de estágios solicitados.</summary>
    public int TotalStages { get; private set; }

    /// <summary>Número de estágios concluídos com sucesso.</summary>
    public int CompletedStages { get; private set; }

    /// <summary>Número de estágios que falharam.</summary>
    public int FailedStages { get; private set; }

    /// <summary>Momento de início da execução.</summary>
    public DateTimeOffset StartedAt { get; private set; }

    /// <summary>Momento de conclusão da execução.</summary>
    public DateTimeOffset? CompletedAt { get; private set; }

    /// <summary>Duração total da execução em milissegundos.</summary>
    public long? DurationMs { get; private set; }

    /// <summary>Mensagem de erro quando a execução falha.</summary>
    public string? ErrorMessage { get; private set; }

    /// <summary>Identificador do utilizador que iniciou a execução.</summary>
    public string InitiatedByUserId { get; private set; } = string.Empty;

    /// <summary>Token de concorrência otimista (PostgreSQL xmin).</summary>
    public uint RowVersion { get; set; }

    /// <summary>
    /// Cria uma nova execução de pipeline com estado Running.
    /// </summary>
    public static PipelineExecution Create(
        Guid apiAssetId,
        string contractName,
        string contractVersion,
        string requestedStages,
        string targetLanguage,
        string? targetFramework,
        int totalStages,
        string initiatedByUserId,
        DateTimeOffset startedAt)
    {
        Guard.Against.Default(apiAssetId);
        Guard.Against.NullOrWhiteSpace(contractName);
        Guard.Against.NullOrWhiteSpace(contractVersion);
        Guard.Against.NullOrWhiteSpace(requestedStages);
        Guard.Against.NullOrWhiteSpace(targetLanguage);
        Guard.Against.NegativeOrZero(totalStages);
        Guard.Against.NullOrWhiteSpace(initiatedByUserId);

        return new PipelineExecution
        {
            Id = PipelineExecutionId.New(),
            ApiAssetId = apiAssetId,
            ContractName = contractName,
            ContractVersion = contractVersion,
            RequestedStages = requestedStages,
            TargetLanguage = targetLanguage,
            TargetFramework = targetFramework,
            Status = PipelineExecutionStatus.Running,
            TotalStages = totalStages,
            CompletedStages = 0,
            FailedStages = 0,
            StartedAt = startedAt,
            InitiatedByUserId = initiatedByUserId
        };
    }

    /// <summary>
    /// Marca a execução como concluída com sucesso.
    /// </summary>
    public void Complete(
        string? stageResults,
        string? generatedArtifacts,
        int completedStages,
        DateTimeOffset completedAt)
    {
        Guard.Against.Negative(completedStages);

        Status = PipelineExecutionStatus.Completed;
        StageResults = stageResults;
        GeneratedArtifacts = generatedArtifacts;
        CompletedStages = completedStages;
        FailedStages = 0;
        CompletedAt = completedAt;
        DurationMs = (long)(completedAt - StartedAt).TotalMilliseconds;
    }

    /// <summary>
    /// Marca a execução como falhada ou parcialmente concluída.
    /// Se algum estágio completou mas outros falharam, o estado é PartiallyCompleted.
    /// </summary>
    public void Fail(
        string? errorMessage,
        string? stageResults,
        int completedStages,
        int failedStages,
        DateTimeOffset completedAt)
    {
        Guard.Against.Negative(completedStages);
        Guard.Against.Negative(failedStages);

        Status = completedStages > 0
            ? PipelineExecutionStatus.PartiallyCompleted
            : PipelineExecutionStatus.Failed;

        ErrorMessage = errorMessage;
        StageResults = stageResults;
        CompletedStages = completedStages;
        FailedStages = failedStages;
        CompletedAt = completedAt;
        DurationMs = (long)(completedAt - StartedAt).TotalMilliseconds;
    }
}

/// <summary>Identificador fortemente tipado de PipelineExecution.</summary>
public sealed record PipelineExecutionId(Guid Value) : TypedIdBase(Value)
{
    /// <summary>Cria novo Id com Guid gerado automaticamente.</summary>
    public static PipelineExecutionId New() => new(Guid.NewGuid());

    /// <summary>Cria Id a partir de Guid existente.</summary>
    public static PipelineExecutionId From(Guid id) => new(id);
}
