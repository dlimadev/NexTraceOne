namespace NexTraceOne.Identity.Domain.Entities;

/// <summary>
/// Catálogo centralizado que mapeia papéis do sistema às suas permissões padrão.
/// Extraído de <see cref="Role"/> para respeitar o Princípio de Responsabilidade Única (SRP):
/// a entidade Role define identidade e ciclo de vida do papel, enquanto este catálogo
/// concentra a política de atribuição de permissões por papel.
/// Esta classe é a fonte única de verdade para mapeamentos papel→permissões.
/// </summary>
public static class RolePermissionCatalog
{
    /// <summary>
    /// Retorna as permissões padrão atribuídas a um papel conhecido do sistema.
    /// Papéis não reconhecidos recebem uma lista vazia, evitando concessão acidental de acesso.
    /// As constantes de nome de papel são definidas em <see cref="Role"/> pois fazem parte
    /// da identidade do aggregate.
    /// </summary>
    /// <param name="roleName">
    /// Nome do papel a consultar. Deve corresponder a uma das constantes definidas em <see cref="Role"/>
    /// (ex.: <see cref="Role.PlatformAdmin"/>, <see cref="Role.TechLead"/>).
    /// </param>
    /// <returns>
    /// Lista imutável de strings de permissão no formato "módulo:recurso:ação".
    /// Retorna lista vazia quando o papel não é reconhecido.
    /// </returns>
    public static IReadOnlyList<string> GetPermissionsForRole(string roleName)
        => roleName switch
        {
            Role.PlatformAdmin => [
                "identity:users:read",
                "identity:users:write",
                "identity:roles:read",
                "identity:roles:assign",
                "identity:sessions:read",
                "identity:sessions:revoke",
                "identity:permissions:read",
                "catalog:assets:read",
                "catalog:assets:write",
                "contracts:read",
                "contracts:write",
                "contracts:import",
                "developer-portal:read",
                "developer-portal:write",
                "change-intelligence:releases:read",
                "change-intelligence:releases:write",
                "change-intelligence:blast-radius:read",
                "workflow:read",
                "workflow:write",
                "workflow:approve",
                "promotion:read",
                "promotion:write",
                "promotion:promote",
                "ruleset-governance:read",
                "ruleset-governance:write",
                "audit:read",
                "audit:export",
                "licensing:read",
                "licensing:write",
                "licensing:vendor:license:create",
                "licensing:vendor:license:revoke",
                "licensing:vendor:license:rehost",
                "licensing:vendor:license:read",
                "licensing:vendor:key:generate",
                "licensing:vendor:trial:extend",
                "licensing:vendor:activation:issue",
                "licensing:vendor:tenant:manage",
                "licensing:vendor:telemetry:view",
                "licensing:vendor:plan:create",
                "licensing:vendor:plan:read",
                "licensing:vendor:featurepack:create",
                "licensing:vendor:featurepack:read",
                "licensing:vendor:license:manage",
                "platform:settings:read",
                "platform:settings:write"],
            Role.TechLead => [
                "identity:users:read",
                "identity:roles:read",
                "identity:sessions:read",
                "catalog:assets:read",
                "catalog:assets:write",
                "contracts:read",
                "contracts:write",
                "contracts:import",
                "developer-portal:read",
                "developer-portal:write",
                "change-intelligence:releases:read",
                "change-intelligence:releases:write",
                "change-intelligence:blast-radius:read",
                "workflow:read",
                "workflow:write",
                "workflow:approve",
                "promotion:read",
                "promotion:write",
                "promotion:promote",
                "ruleset-governance:read",
                "licensing:read",
                "audit:read",
                "audit:export"],
            Role.Developer => [
                "identity:users:read",
                "catalog:assets:read",
                "contracts:read",
                "contracts:write",
                "contracts:import",
                "developer-portal:read",
                "developer-portal:write",
                "change-intelligence:releases:read",
                "change-intelligence:releases:write",
                "change-intelligence:blast-radius:read",
                "workflow:read",
                "promotion:read",
                "ruleset-governance:read",
                "audit:read"],
            Role.Viewer => [
                "identity:users:read",
                "catalog:assets:read",
                "contracts:read",
                "developer-portal:read",
                "change-intelligence:releases:read",
                "change-intelligence:blast-radius:read",
                "workflow:read",
                "promotion:read",
                "audit:read"],
            Role.Auditor => [
                "identity:users:read",
                "identity:sessions:read",
                "catalog:assets:read",
                "contracts:read",
                "developer-portal:read",
                "change-intelligence:releases:read",
                "change-intelligence:blast-radius:read",
                "workflow:read",
                "promotion:read",
                "ruleset-governance:read",
                "audit:read",
                "audit:export"],
            Role.SecurityReview => [
                "identity:users:read",
                "identity:roles:read",
                "identity:sessions:read",
                "identity:sessions:revoke",
                "catalog:assets:read",
                "contracts:read",
                "developer-portal:read",
                "change-intelligence:releases:read",
                "change-intelligence:blast-radius:read",
                "workflow:read",
                "workflow:approve",
                "promotion:read",
                "ruleset-governance:read",
                "ruleset-governance:write",
                "audit:read",
                "audit:export"],
            Role.ApprovalOnly => [
                "workflow:read",
                "workflow:approve",
                "change-intelligence:releases:read",
                "change-intelligence:blast-radius:read",
                "promotion:read",
                "audit:read"],
            _ => []
        };
}
