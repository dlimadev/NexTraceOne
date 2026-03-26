namespace NexTraceOne.BuildingBlocks.Application.Context;

/// <summary>
/// Constantes dos cabeçalhos HTTP usados para propagação de contexto distribuído
/// na plataforma NexTraceOne.
///
/// Convenção:
/// - X-Tenant-Id: identificador do tenant (Guid) — contexto de tenant na requisição
/// - X-Environment-Id: identificador do ambiente (Guid) — obrigatório em requisições operacionais
/// - X-Correlation-Id: identificador de correlação distribuída — gerado automaticamente se ausente
/// - X-Request-Id: identificador único da requisição — diferente do correlation, não propagado downstream
///
/// REGRA DE SEGURANÇA — X-Tenant-Id:
/// O header X-Tenant-Id é aceito pelo TenantResolutionMiddleware APENAS quando o pedido
/// está autenticado (context.User.Identity?.IsAuthenticated == true).
/// Pedidos não autenticados com este header têm o valor ignorado.
/// A fonte principal de tenant é sempre o JWT claim "tenant_id".
/// O header serve como fallback controlado para casos em que o JWT autenticado não
/// contém o claim tenant_id (situação de transição controlada).
///
/// REGRA DE SEGURANÇA — geral:
/// Nenhum cabeçalho de contexto é suficiente para autorizar operações por si só.
/// TenantId é sempre validado contra o JWT. EnvironmentId é validado contra o TenantId.
/// Estes headers servem para propagar contexto, não para substituir autenticação.
/// </summary>
public static class ContextPropagationHeaders
{
    /// <summary>Header com o TenantId ativo. Valor: Guid como string.</summary>
    public const string TenantId = "X-Tenant-Id";

    /// <summary>Header com o EnvironmentId ativo. Valor: Guid como string.</summary>
    public const string EnvironmentId = "X-Environment-Id";

    /// <summary>
    /// Header de correlação distribuída. Propagado em todas as chamadas downstream,
    /// mensagens de fila e background jobs. Permite rastrear uma operação de ponta a ponta.
    /// </summary>
    public const string CorrelationId = "X-Correlation-Id";

    /// <summary>Identificador único de cada requisição individual (não propagado downstream).</summary>
    public const string RequestId = "X-Request-Id";

    /// <summary>
    /// Header opcional com o ServiceName de origem, para identificação em chamadas inter-módulo.
    /// </summary>
    public const string ServiceOrigin = "X-Service-Origin";

    /// <summary>
    /// Array com todos os headers de contexto que devem ser propagados downstream.
    /// Usado por HttpClients para forwarding automático de contexto.
    /// </summary>
    public static readonly string[] PropagatedHeaders =
    [
        TenantId,
        EnvironmentId,
        CorrelationId,
        ServiceOrigin
    ];
}
