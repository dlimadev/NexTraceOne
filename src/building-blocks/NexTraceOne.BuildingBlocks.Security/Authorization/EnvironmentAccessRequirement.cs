using Microsoft.AspNetCore.Authorization;

namespace NexTraceOne.BuildingBlocks.Security.Authorization;

/// <summary>
/// Requirement de autorização que exige que o usuário tenha acesso ao ambiente
/// resolvido na requisição atual.
///
/// Avaliado pelo EnvironmentAccessAuthorizationHandler.
/// Usado em endpoints operacionais que requerem contexto de ambiente válido.
/// </summary>
public sealed class EnvironmentAccessRequirement : IAuthorizationRequirement
{
    /// <summary>Instância padrão do requirement.</summary>
    public static readonly EnvironmentAccessRequirement Instance = new();
}

/// <summary>
/// Requirement que exige contexto operacional completo: tenant + ambiente + usuário autenticado.
/// Mais restritivo que EnvironmentAccessRequirement — exige que TODOS os três estejam resolvidos.
/// Usado em endpoints que operam sobre dados operacionais (observabilidade, incidentes, IA).
/// </summary>
public sealed class OperationalContextRequirement : IAuthorizationRequirement
{
    /// <summary>Instância padrão do requirement.</summary>
    public static readonly OperationalContextRequirement Instance = new();
}
