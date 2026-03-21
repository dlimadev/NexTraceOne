using NexTraceOne.BuildingBlocks.Application.Abstractions;

namespace NexTraceOne.BuildingBlocks.Application.Context;

/// <summary>
/// Contexto distribuído de execução — snapshot imutável do contexto operacional
/// no momento de uma operação, evento ou job.
///
/// Serve como envelope de contexto para:
/// - propagar TenantId + EnvironmentId em eventos de integração
/// - enriquecer traces e logs com contexto operacional
/// - vincular background jobs e workers ao contexto que os originou
/// - permitir correlação posterior por tenant/ambiente
///
/// Diferença de ICurrentTenant/ICurrentEnvironment (scoped à requisição HTTP atual, live, mutável):
/// DistributedExecutionContext é um snapshot imutável criado em momento específico
/// para ser transportado em mensagens, eventos e jobs assíncronos.
///
/// SEGURANÇA: Nunca use este contexto para autorização — apenas para correlação e enriquecimento.
/// </summary>
public sealed record DistributedExecutionContext
{
    /// <summary>
    /// Cria contexto distribuído com os valores fornecidos.
    /// Todos os campos são opcionais para máxima compatibilidade.
    /// </summary>
    public DistributedExecutionContext(
        Guid? tenantId = null,
        Guid? environmentId = null,
        string? correlationId = null,
        string? userId = null,
        string? serviceOrigin = null)
    {
        TenantId = tenantId;
        EnvironmentId = environmentId;
        CorrelationId = correlationId ?? Guid.NewGuid().ToString("N");
        UserId = userId;
        ServiceOrigin = serviceOrigin ?? "NexTraceOne";
        CapturedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>Identificador do tenant no contexto desta operação.</summary>
    public Guid? TenantId { get; init; }

    /// <summary>Identificador do ambiente no contexto desta operação.</summary>
    public Guid? EnvironmentId { get; init; }

    /// <summary>
    /// Identificador de correlação distribuída.
    /// Gerado automaticamente se não fornecido.
    /// Propagado em todas as operações downstream para rastreabilidade ponta-a-ponta.
    /// </summary>
    public string CorrelationId { get; init; }

    /// <summary>Identificador do usuário que originou a operação. Pode ser "system" para jobs.</summary>
    public string? UserId { get; init; }

    /// <summary>Nome do serviço/módulo de origem desta operação.</summary>
    public string ServiceOrigin { get; init; }

    /// <summary>Momento em que este contexto foi capturado.</summary>
    public DateTimeOffset CapturedAt { get; init; }

    /// <summary>Indica se o contexto tem TenantId válido (não nulo e não vazio).</summary>
    public bool IsOperational => TenantId.HasValue && TenantId != Guid.Empty;

    /// <summary>
    /// Cria um DistributedExecutionContext a partir das abstrações de contexto atual.
    /// Método factory conveniente para uso em handlers e services.
    /// </summary>
    public static DistributedExecutionContext From(
        ICurrentTenant tenant,
        ICurrentEnvironment environment,
        string? correlationId = null,
        string? userId = null,
        string? serviceOrigin = null)
        => new(
            tenantId: tenant.Id != Guid.Empty ? tenant.Id : null,
            environmentId: environment.IsResolved ? environment.EnvironmentId : null,
            correlationId: correlationId,
            userId: userId,
            serviceOrigin: serviceOrigin);
}
