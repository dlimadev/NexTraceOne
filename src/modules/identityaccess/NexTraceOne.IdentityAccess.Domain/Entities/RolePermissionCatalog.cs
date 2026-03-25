namespace NexTraceOne.IdentityAccess.Domain.Entities;

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
                // Identity
                "identity:users:read",
                "identity:users:write",
                "identity:roles:read",
                "identity:roles:assign",
                "identity:sessions:read",
                "identity:sessions:revoke",
                "identity:permissions:read",

                // Catalog
                "catalog:assets:read",
                "catalog:assets:write",

                // Contracts
                "contracts:read",
                "contracts:write",
                "contracts:import",

                // Developer Portal
                "developer-portal:read",
                "developer-portal:write",

                // Change Intelligence
                "change-intelligence:read",
                "change-intelligence:write",

                // Operations
                "operations:incidents:read",
                "operations:incidents:write",
                "operations:mitigation:read",
                "operations:mitigation:write",
                "operations:runbooks:read",
                "operations:reliability:read",
                "operations:runtime:read",
                "operations:runtime:write",
                "operations:cost:read",
                "operations:cost:write",
                "operations:automation:read",
                "operations:automation:write",
                "operations:automation:execute",
                "operations:automation:approve",

                // Governance
                "governance:domains:read",
                "governance:domains:write",
                "governance:teams:read",
                "governance:teams:write",
                "governance:policies:read",
                "governance:controls:read",
                "governance:compliance:read",
                "governance:risk:read",
                "governance:evidence:read",
                "governance:waivers:read",
                "governance:waivers:write",
                "governance:packs:read",
                "governance:packs:write",
                "governance:reports:read",
                "governance:analytics:read",
                "governance:analytics:write",
                "governance:finops:read",
                "governance:admin:read",
                "governance:admin:write",

                // Promotion
                "promotion:requests:read",
                "promotion:requests:write",
                "promotion:environments:write",
                "promotion:gates:override",

                // Rulesets
                "rulesets:read",
                "rulesets:write",
                "rulesets:execute",

                // Audit
                "audit:trail:read",
                "audit:reports:read",
                "audit:compliance:read",
                "audit:compliance:write",
                "audit:events:write",

                // AI
                "ai:assistant:read",
                "ai:assistant:write",
                "ai:governance:read",
                "ai:governance:write",
                "ai:ide:read",
                "ai:ide:write",

                // Integrations
                "integrations:read",
                "integrations:write",

                // Platform
                "platform:admin:read",
                "platform:settings:read",
                "platform:settings:write",

                // Licensing
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

                // Notifications
                "notifications:inbox:read",
                "notifications:inbox:write",
                "notifications:preferences:read",
                "notifications:preferences:write",

                // Environment Management
                "env:environments:read",
                "env:environments:write",
                "env:environments:admin",
                "env:access:read",
                "env:access:admin"],
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
                "change-intelligence:read",
                "change-intelligence:write",
                "operations:incidents:read",
                "operations:incidents:write",
                "operations:mitigation:read",
                "operations:reliability:read",
                "operations:runbooks:read",
                "operations:runtime:read",
                "operations:automation:read",
                "governance:domains:read",
                "governance:teams:read",
                "governance:policies:read",
                "governance:compliance:read",
                "governance:reports:read",
                "governance:analytics:read",
                "promotion:requests:read",
                "promotion:requests:write",
                "promotion:environments:write",
                "rulesets:read",
                "rulesets:write",
                "audit:trail:read",
                "audit:reports:read",
                "ai:assistant:read",
                "ai:assistant:write",
                "ai:governance:read",
                "integrations:read",
                "integrations:write",
                "licensing:read",
                "notifications:inbox:read",
                "notifications:inbox:write",
                "notifications:preferences:read",
                "notifications:preferences:write",
                "env:environments:read",
                "env:environments:write",
                "env:access:read"],
            Role.Developer => [
                "identity:users:read",
                "catalog:assets:read",
                "contracts:read",
                "contracts:write",
                "contracts:import",
                "developer-portal:read",
                "developer-portal:write",
                "change-intelligence:read",
                "operations:incidents:read",
                "operations:incidents:write",
                "operations:reliability:read",
                "operations:runbooks:read",
                "operations:runtime:read",
                "governance:domains:read",
                "governance:teams:read",
                "promotion:requests:read",
                "rulesets:read",
                "audit:trail:read",
                "ai:assistant:read",
                "ai:assistant:write",
                "integrations:read",
                "notifications:inbox:read",
                "notifications:inbox:write",
                "notifications:preferences:read",
                "notifications:preferences:write",
                "env:environments:read",
                "env:access:read"],
            Role.Viewer => [
                "identity:users:read",
                "catalog:assets:read",
                "contracts:read",
                "developer-portal:read",
                "change-intelligence:read",
                "operations:incidents:read",
                "operations:reliability:read",
                "governance:domains:read",
                "governance:reports:read",
                "governance:analytics:read",
                "promotion:requests:read",
                "rulesets:read",
                "audit:trail:read",
                "ai:assistant:read",
                "notifications:inbox:read",
                "notifications:preferences:read",
                "env:environments:read"],
            Role.Auditor => [
                "identity:users:read",
                "identity:sessions:read",
                "catalog:assets:read",
                "contracts:read",
                "developer-portal:read",
                "change-intelligence:read",
                "operations:incidents:read",
                "operations:reliability:read",
                "operations:runbooks:read",
                "governance:domains:read",
                "governance:compliance:read",
                "governance:evidence:read",
                "governance:reports:read",
                "promotion:requests:read",
                "rulesets:read",
                "audit:trail:read",
                "audit:reports:read",
                "audit:compliance:read",
                "audit:compliance:write",
                "env:environments:read",
                "env:access:read",
                "notifications:inbox:read",
                "notifications:preferences:read"],
            Role.SecurityReview => [
                "identity:users:read",
                "identity:roles:read",
                "identity:sessions:read",
                "identity:sessions:revoke",
                "catalog:assets:read",
                "contracts:read",
                "developer-portal:read",
                "change-intelligence:read",
                "operations:incidents:read",
                "operations:reliability:read",
                "governance:compliance:read",
                "governance:risk:read",
                "governance:evidence:read",
                "governance:policies:read",
                "promotion:requests:read",
                "rulesets:read",
                "rulesets:write",
                "audit:trail:read",
                "audit:reports:read",
                "audit:compliance:read",
                "notifications:inbox:read",
                "notifications:preferences:read",
                "env:environments:read",
                "env:access:read"],
            Role.ApprovalOnly => [
                "change-intelligence:read",
                "promotion:requests:read",
                "promotion:requests:write",
                "promotion:gates:override",
                "audit:trail:read",
                "notifications:inbox:read",
                "notifications:inbox:write",
                "notifications:preferences:read"],
            _ => []
        };
}
