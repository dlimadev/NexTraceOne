namespace NexTraceOne.Ingestion.Api.Models;

/// <summary>Request para eventos de deployment de pipelines CI/CD.</summary>
public sealed record DeploymentEventRequest(
    string Provider,
    string? Source,
    string? CorrelationId,
    string? ServiceName,
    string? Environment,
    string? Version,
    string? CommitSha);

/// <summary>Request para eventos de promoção entre ambientes.</summary>
public sealed record PromotionEventRequest(
    string? CorrelationId,
    string? ServiceName,
    string? FromEnvironment,
    string? ToEnvironment,
    string? Version);

/// <summary>Request para sinais de runtime de serviços monitorados.</summary>
public sealed record RuntimeSignalRequest(
    string? ServiceName,
    string? SignalType,
    string? Message,
    Dictionary<string, string>? Tags);

/// <summary>Request para sincronização de consumidores e dependências.</summary>
public sealed record ConsumerSyncRequest(
    string? ServiceName,
    List<string>? Consumers,
    List<string>? Dependencies);

/// <summary>Request para sincronização de contratos de fontes externas.</summary>
public sealed record ContractSyncRequest(
    string? Provider,
    List<ContractItem>? Contracts);

/// <summary>Item de contrato individual para sincronização.</summary>
public sealed record ContractItem(
    string Name,
    string Type,
    string? Version,
    string? Content);
