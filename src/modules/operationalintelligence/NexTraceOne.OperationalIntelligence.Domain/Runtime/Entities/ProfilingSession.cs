using Ardalis.GuardClauses;
using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;
using NexTraceOne.OperationalIntelligence.Domain.Runtime.Enums;

namespace NexTraceOne.OperationalIntelligence.Domain.Runtime.Entities;

/// <summary>
/// Aggregate Root que representa uma sessão de profiling contínuo de um serviço.
///
/// Armazena os dados de profiling ingestados a partir de dotnet-trace, pprof, async-profiler
/// ou outros formatos, contextualizados por serviço, ambiente e janela temporal.
///
/// A análise de hotspots (TopFrames) é um resumo computado no momento da ingestão
/// a partir dos raw frames. O payload completo (RawDataUri) é guardado externamente
/// (blob storage, S3, etc.) para não sobrecarregar o banco de dados relacional.
///
/// Wave D backlog — Continuous Profiling ingest contextualizado por serviço.
/// </summary>
public sealed class ProfilingSession : AuditableEntity<ProfilingSessionId>
{
    private const int MaxServiceNameLength = 200;
    private const int MaxEnvironmentLength = 100;

    private ProfilingSession() { }

    /// <summary>Identificador do tenant ao qual a sessão pertence.</summary>
    public string TenantId { get; private set; } = string.Empty;

    /// <summary>Nome do serviço perfilado.</summary>
    public string ServiceName { get; private set; } = string.Empty;

    /// <summary>Ambiente de execução (dev, staging, prod).</summary>
    public string Environment { get; private set; } = string.Empty;

    /// <summary>Tipo de frame de profiling — determina o parser de análise.</summary>
    public ProfilingFrameType FrameType { get; private set; }

    /// <summary>Início da janela de profiling em UTC.</summary>
    public DateTimeOffset WindowStart { get; private set; }

    /// <summary>Fim da janela de profiling em UTC.</summary>
    public DateTimeOffset WindowEnd { get; private set; }

    /// <summary>Duração total em segundos (calculado).</summary>
    public int DurationSeconds { get; private set; }

    /// <summary>Número total de amostras de CPU capturadas na sessão.</summary>
    public long TotalCpuSamples { get; private set; }

    /// <summary>Uso máximo de memória observado em MB durante a sessão.</summary>
    public decimal PeakMemoryMb { get; private set; }

    /// <summary>
    /// Resumo dos top N frames por CPU (hotspots) em JSON.
    /// Formato: [{"method":"...", "module":"...", "sampleCount": N, "percentage": X.X}]
    /// </summary>
    public string? TopFramesJson { get; private set; }

    /// <summary>URI externo para o raw profiling data (blob storage, S3, etc.).</summary>
    public string? RawDataUri { get; private set; }

    /// <summary>SHA-256 do payload raw para verificação de integridade.</summary>
    public string? RawDataHash { get; private set; }

    /// <summary>Versão da release associada (opcional — correlação com deployment).</summary>
    public string? ReleaseVersion { get; private set; }

    /// <summary>SHA do commit associado (opcional — correlação com deployment).</summary>
    public string? CommitSha { get; private set; }

    /// <summary>Se true, anomalias foram detectadas nesta sessão (e.g., GC pressure, hot path).</summary>
    public bool HasAnomalies { get; private set; }

    /// <summary>Número de threads ativos no pico da sessão.</summary>
    public int PeakThreadCount { get; private set; }

    /// <summary>
    /// Inicia uma nova sessão de profiling.
    /// WindowEnd deve ser posterior a WindowStart.
    /// TotalCpuSamples e PeakMemoryMb devem ser não-negativos.
    /// </summary>
    public static ProfilingSession Start(
        string tenantId,
        string serviceName,
        string environment,
        ProfilingFrameType frameType,
        DateTimeOffset windowStart,
        DateTimeOffset windowEnd,
        long totalCpuSamples,
        decimal peakMemoryMb,
        int peakThreadCount,
        DateTimeOffset capturedAt)
    {
        Guard.Against.NullOrWhiteSpace(tenantId);
        Guard.Against.NullOrWhiteSpace(serviceName);
        Guard.Against.NullOrWhiteSpace(environment);
        Guard.Against.InvalidInput(windowEnd, nameof(windowEnd), v => v > windowStart,
            "WindowEnd must be after WindowStart.");
        Guard.Against.Negative(totalCpuSamples);

        var durationSeconds = (int)(windowEnd - windowStart).TotalSeconds;

        return new ProfilingSession
        {
            Id = ProfilingSessionId.New(),
            TenantId = tenantId,
            ServiceName = serviceName[..Math.Min(serviceName.Length, MaxServiceNameLength)],
            Environment = environment[..Math.Min(environment.Length, MaxEnvironmentLength)],
            FrameType = frameType,
            WindowStart = windowStart,
            WindowEnd = windowEnd,
            DurationSeconds = durationSeconds,
            TotalCpuSamples = totalCpuSamples,
            PeakMemoryMb = Math.Max(0, peakMemoryMb),
            PeakThreadCount = Math.Max(0, peakThreadCount),
            HasAnomalies = false
        };
    }

    /// <summary>Anexa o resumo de hotspots (top frames) em JSON à sessão.</summary>
    public void AttachTopFrames(string topFramesJson)
    {
        Guard.Against.NullOrWhiteSpace(topFramesJson);
        TopFramesJson = topFramesJson[..Math.Min(topFramesJson.Length, 50000)];
    }

    /// <summary>Regista a referência para os dados raw externos.</summary>
    public void AttachRawDataReference(string rawDataUri, string? rawDataHash)
    {
        Guard.Against.NullOrWhiteSpace(rawDataUri);
        RawDataUri = rawDataUri[..Math.Min(rawDataUri.Length, 2000)];
        RawDataHash = rawDataHash;
    }

    /// <summary>Associa a sessão de profiling a uma release específica.</summary>
    public void LinkToRelease(string releaseVersion, string? commitSha)
    {
        Guard.Against.NullOrWhiteSpace(releaseVersion);
        ReleaseVersion = releaseVersion[..Math.Min(releaseVersion.Length, 50)];
        CommitSha = commitSha;
    }

    /// <summary>Marca a sessão como contendo anomalias detectadas.</summary>
    public void MarkAsHavingAnomalies()
    {
        HasAnomalies = true;
    }
}

/// <summary>Strongly-typed ID para ProfilingSession.</summary>
public sealed record ProfilingSessionId(Guid Value) : TypedIdBase(Value)
{
    public static ProfilingSessionId New() => new(Guid.NewGuid());
    public static ProfilingSessionId From(Guid id) => new(id);
}
