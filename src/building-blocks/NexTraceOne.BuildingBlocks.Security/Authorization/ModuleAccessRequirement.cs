using Microsoft.AspNetCore.Authorization;

namespace NexTraceOne.BuildingBlocks.Security.Authorization;

/// <summary>
/// Requisito de autorização baseado no modelo granular módulo/página/ação.
///
/// Usado pelos endpoints que adoptam o novo modelo <c>RequireModuleAccess("AI", "Runtime", "Write")</c>
/// em vez do modelo plano <c>RequirePermission("ai:runtime:write")</c>.
///
/// Avaliado pelo <see cref="ModuleAccessAuthorizationHandler"/>.
///
/// Benefícios face ao modelo plano:
/// - Alinhamento directo com a entidade ModuleAccessPolicy da base de dados.
/// - Suporte nativo a wildcard ("*") para página e ação.
/// - Sem necessidade de mapeamento intermediário.
/// - Permite administração sem redeploy quando as políticas são geridas na BD.
/// </summary>
public sealed class ModuleAccessRequirement(string module, string page, string action) : IAuthorizationRequirement
{
    /// <summary>Módulo da plataforma (ex.: "AI", "Catalog", "Operations").</summary>
    public string Module { get; } = module;

    /// <summary>Página ou sub-área do módulo (ex.: "Runtime", "ServiceCatalog"). Use "*" para wildcard.</summary>
    public string Page { get; } = page;

    /// <summary>Ação granular (ex.: "Read", "Write", "Approve", "Delete"). Use "*" para wildcard.</summary>
    public string Action { get; } = action;
}
