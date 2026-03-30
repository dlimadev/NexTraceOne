using Ardalis.GuardClauses;
using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;

namespace NexTraceOne.Catalog.Domain.Graph.Entities;

/// <summary>
/// Registo de uma execução do job de discovery automático.
/// Cada run captura: quando correu, quantos serviços encontrou,
/// fonte de dados e estado final.
/// Garante auditabilidade completa do processo de descoberta.
/// </summary>
public sealed class DiscoveryRun : Entity<DiscoveryRunId>
{
    private DiscoveryRun() { }

    /// <summary>Data/hora UTC de início da execução.</summary>
    public DateTimeOffset StartedAt { get; private set; }

    /// <summary>Data/hora UTC de fim da execução (null se ainda em curso).</summary>
    public DateTimeOffset? CompletedAt { get; private set; }

    /// <summary>Fonte de dados consultada (ex: OpenTelemetry, LogAnalytics).</summary>
    public string Source { get; private set; } = string.Empty;

    /// <summary>Ambiente alvo da discovery (ex: production, staging).</summary>
    public string Environment { get; private set; } = string.Empty;

    /// <summary>Total de serviços distintos encontrados nesta execução.</summary>
    public int ServicesFound { get; private set; }

    /// <summary>Total de serviços novos (não vistos antes).</summary>
    public int NewServicesFound { get; private set; }

    /// <summary>Total de erros durante a execução.</summary>
    public int ErrorCount { get; private set; }

    /// <summary>Estado resumido: Completed, Failed, PartialSuccess.</summary>
    public string Status { get; private set; } = string.Empty;

    /// <summary>Mensagem de erro ou resumo (quando aplicável).</summary>
    public string? ErrorMessage { get; private set; }

    // ── Factory ───────────────────────────────────────────────────────

    /// <summary>Cria um novo registo de execução de discovery.</summary>
    public static DiscoveryRun Start(string source, string environment, DateTimeOffset startedAt)
    {
        return new DiscoveryRun
        {
            Id = DiscoveryRunId.New(),
            Source = Guard.Against.NullOrWhiteSpace(source),
            Environment = Guard.Against.NullOrWhiteSpace(environment),
            StartedAt = startedAt,
            Status = "Running"
        };
    }

    // ── Mutations ─────────────────────────────────────────────────────

    /// <summary>Marca a execução como concluída com sucesso.</summary>
    public void Complete(DateTimeOffset completedAt, int servicesFound, int newServicesFound)
    {
        CompletedAt = completedAt;
        ServicesFound = servicesFound;
        NewServicesFound = newServicesFound;
        Status = "Completed";
    }

    /// <summary>Marca a execução como falhada.</summary>
    public void Fail(DateTimeOffset completedAt, string errorMessage, int errorCount)
    {
        CompletedAt = completedAt;
        ErrorMessage = errorMessage;
        ErrorCount = errorCount;
        Status = "Failed";
    }

    /// <summary>Marca a execução como parcialmente bem-sucedida.</summary>
    public void PartialSuccess(DateTimeOffset completedAt, int servicesFound, int newServicesFound, int errorCount, string? errorMessage)
    {
        CompletedAt = completedAt;
        ServicesFound = servicesFound;
        NewServicesFound = newServicesFound;
        ErrorCount = errorCount;
        ErrorMessage = errorMessage;
        Status = "PartialSuccess";
    }
}

/// <summary>Identificador fortemente tipado de DiscoveryRun.</summary>
public sealed record DiscoveryRunId(Guid Value) : TypedIdBase(Value)
{
    /// <summary>Cria novo Id com Guid gerado automaticamente.</summary>
    public static DiscoveryRunId New() => new(Guid.NewGuid());

    /// <summary>Cria a partir de Guid existente.</summary>
    public static DiscoveryRunId From(Guid value) => new(value);
}
