using Microsoft.AspNetCore.Builder;
using NexTraceOne.BuildingBlocks.Security.Authorization;

namespace NexTraceOne.BuildingBlocks.Security.Extensions;

/// <summary>
/// Extensões para aplicar autorização baseada em permissão ou módulo/página/ação a endpoints Minimal API.
///
/// Dois modelos disponíveis:
/// - <c>RequirePermission("ai:runtime:write")</c> — modelo plano (legacy, totalmente funcional).
///   Resolve via cascata: JWT claims → DB RolePermission → DB ModuleAccessPolicy → JIT grants.
/// - <c>RequireModuleAccess("AI", "Runtime", "Write")</c> — modelo granular (novo, preferencial).
///   Resolve directamente contra a tabela ModuleAccessPolicy, sem necessidade de mapeamento intermediário.
///
/// Ambos os modelos coexistem e podem ser migrados gradualmente por módulo.
/// </summary>
public static class EndpointAuthorizationExtensions
{
    /// <summary>
    /// Exige que o usuário possua a permissão especificada para acessar o endpoint.
    /// Combina autenticação obrigatória com verificação granular de permissão.
    ///
    /// Cascata de resolução: JWT → DB RolePermission → DB ModuleAccessPolicy → JIT.
    /// </summary>
    /// <param name="builder">Builder do endpoint Minimal API.</param>
    /// <param name="permission">Código da permissão exigida (ex.: "identity:users:write").</param>
    /// <returns>O builder com a policy de autorização aplicada.</returns>
    public static TBuilder RequirePermission<TBuilder>(this TBuilder builder, string permission)
        where TBuilder : IEndpointConventionBuilder
    {
        return builder.RequireAuthorization($"{PermissionPolicyProvider.PolicyPrefix}{permission}");
    }

    /// <summary>
    /// Exige que o utilizador tenha acesso ao módulo/página/ação especificado.
    /// Usa o modelo granular de autorização baseado em ModuleAccessPolicy (BD).
    ///
    /// Preferencial para novos endpoints. Resolve directamente contra a tabela
    /// iam_module_access_policies com suporte a personalização por tenant e wildcard.
    /// </summary>
    /// <param name="builder">Builder do endpoint Minimal API.</param>
    /// <param name="module">Módulo da plataforma (ex.: "AI", "Catalog", "Operations").</param>
    /// <param name="page">Página ou sub-área (ex.: "Runtime", "ServiceCatalog"). Use "*" para wildcard.</param>
    /// <param name="action">Ação granular (ex.: "Read", "Write", "Approve"). Use "*" para wildcard.</param>
    /// <returns>O builder com a policy de autorização aplicada.</returns>
    public static TBuilder RequireModuleAccess<TBuilder>(this TBuilder builder, string module, string page, string action)
        where TBuilder : IEndpointConventionBuilder
    {
        return builder.RequireAuthorization($"{PermissionPolicyProvider.ModuleAccessPrefix}{module}:{page}:{action}");
    }
}
