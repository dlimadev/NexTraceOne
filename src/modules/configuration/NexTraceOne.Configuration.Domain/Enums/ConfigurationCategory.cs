namespace NexTraceOne.Configuration.Domain.Enums;

/// <summary>
/// Categoria de uma configuração, determinando o seu ciclo de vida,
/// visibilidade e regras de gestão dentro da plataforma.
/// As configurações são divididas em três grupos para governança adequada.
/// </summary>
public enum ConfigurationCategory
{
    /// <summary>
    /// Configurações de arranque do sistema — carregadas antes de qualquer outro módulo.
    /// Incluem parâmetros essenciais como connection strings, feature flags de infraestrutura
    /// e configurações que condicionam a inicialização da plataforma.
    /// </summary>
    Bootstrap = 0,

    /// <summary>
    /// Configurações operacionais sensíveis — requerem controlo de acesso reforçado.
    /// Incluem segredos, chaves de API, tokens de integração e parâmetros que,
    /// se expostos ou alterados indevidamente, comprometem a segurança ou operação.
    /// </summary>
    SensitiveOperational = 1,

    /// <summary>
    /// Configurações funcionais — controlam comportamento de produto e experiência do utilizador.
    /// Incluem feature flags, preferências de UI, limites operacionais e parâmetros
    /// que equipas e administradores podem ajustar de forma controlada.
    /// </summary>
    Functional = 2
}
