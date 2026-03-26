namespace NexTraceOne.ApiHost;

/// <summary>
/// Configuração parametrizada das políticas de rate limiting da plataforma.
/// Todos os valores são lidos da secção "RateLimiting" do appsettings.
/// Os defaults inline preservam os valores originais auditados como baseline seguro.
///
/// Ajustar por ambiente via appsettings.{Environment}.json ou variáveis de ambiente
/// (ex: RateLimiting__Auth__PermitLimit=50).
///
/// Regra de segurança: não reduzir limites abaixo dos valores baseline sem validação
/// operacional — limites muito restritivos podem afetar utilizadores legítimos.
/// </summary>
public sealed class RateLimitingOptions
{
    public const string SectionName = "RateLimiting";

    /// <summary>
    /// Política global aplicada a todos os pedidos, particionada por IP.
    /// Clientes com IP não resolvido recebem o limite mais restritivo <see cref="GlobalPolicyOptions.UnresolvedIpPermitLimit"/>.
    /// </summary>
    public GlobalPolicyOptions Global { get; init; } = new();

    /// <summary>
    /// Política para endpoints de autenticação (login, refresh, federated, OIDC).
    /// Protege contra brute force e credential stuffing.
    /// </summary>
    public PolicyOptions Auth { get; init; } = new() { PermitLimit = 20, QueueLimit = 2 };

    /// <summary>
    /// Política para operações de autenticação sensíveis (register, OIDC start, cookie session).
    /// Limite mais restritivo que Auth para reduzir superfície de abuso.
    /// </summary>
    public PolicyOptions AuthSensitive { get; init; } = new() { PermitLimit = 10, QueueLimit = 2 };

    /// <summary>
    /// Política para endpoints de IA (chat, geração, retrieval, análise).
    /// Limita consumo de recursos computacionalmente custosos com custo financeiro associado.
    /// </summary>
    public PolicyOptions Ai { get; init; } = new() { PermitLimit = 30, QueueLimit = 3 };

    /// <summary>
    /// Política para endpoints de dados intensivos (catálogo, analytics, runtime queries, relatórios).
    /// Previne scraping e overload de queries pesadas.
    /// </summary>
    public PolicyOptions DataIntensive { get; init; } = new() { PermitLimit = 50, QueueLimit = 3 };

    /// <summary>
    /// Política para endpoints operacionais (incidentes, automação, observabilidade, health).
    /// Previne abuso de operações administrativas.
    /// </summary>
    public PolicyOptions Operations { get; init; } = new() { PermitLimit = 40, QueueLimit = 3 };
}

/// <summary>
/// Configuração de uma política de rate limiting de janela fixa por IP.
/// </summary>
public sealed class PolicyOptions
{
    /// <summary>
    /// Número máximo de pedidos permitidos por janela temporal, por IP.
    /// </summary>
    public int PermitLimit { get; init; } = 100;

    /// <summary>
    /// Duração da janela de rate limiting em minutos.
    /// </summary>
    public int WindowMinutes { get; init; } = 1;

    /// <summary>
    /// Número máximo de pedidos que ficam em fila quando o limite é atingido.
    /// </summary>
    public int QueueLimit { get; init; } = 5;
}

/// <summary>
/// Configuração da política global de rate limiting.
/// Extende <see cref="PolicyOptions"/> com controlo para IPs não resolvidos
/// (clientes atrás de proxy sem X-Forwarded-For configurado).
/// </summary>
public sealed class GlobalPolicyOptions
{
    /// <summary>
    /// Número máximo de pedidos permitidos por janela temporal, por IP resolvido.
    /// </summary>
    public int PermitLimit { get; init; } = 100;

    /// <summary>
    /// Limite mais restritivo aplicado quando o IP do cliente não pode ser resolvido.
    /// Mitiga bypass de rate limiting via proxies mal configurados.
    /// </summary>
    public int UnresolvedIpPermitLimit { get; init; } = 20;

    /// <summary>
    /// Duração da janela de rate limiting em minutos.
    /// </summary>
    public int WindowMinutes { get; init; } = 1;

    /// <summary>
    /// Número máximo de pedidos que ficam em fila quando o limite é atingido.
    /// </summary>
    public int QueueLimit { get; init; } = 5;
}
