/**
 * Catálogo de roles e permissões alinhado ao backend.
 *
 * IMPORTANTE — Segurança:
 * O frontend NUNCA deve fazer enforcement de autorização.
 * O backend é a única fonte de verdade para permissões.
 * O frontend usa as permissões recebidas do backend (via JWT/profile)
 * apenas para controle visual (exibir/ocultar elementos de UI).
 *
 * Não existe mapeamento client-side de role→permissões propositalmente.
 * As permissões efetivas do usuário são obtidas via endpoint /me
 * e refletidas pelo hook usePermissions.
 *
 * @see src/hooks/usePermissions.ts — hook que expõe permissões do usuário
 */

// Roles disponíveis no sistema RBAC do NexTraceOne (alinhados com o backend)
export type AppRole =
  | 'PlatformAdmin'
  | 'TechLead'
  | 'Developer'
  | 'Viewer'
  | 'Auditor'
  | 'SecurityReview'
  | 'ApprovalOnly';

// Permissões granulares por módulo (códigos idênticos ao catálogo do backend)
export type Permission =
  // ── Identity & Access ──
  | 'identity:users:read'
  | 'identity:users:write'
  | 'identity:roles:read'
  | 'identity:roles:assign'
  | 'identity:sessions:read'
  | 'identity:sessions:revoke'
  | 'identity:permissions:read'
  // ── Engineering Graph / Service Catalog ──
  | 'catalog:assets:read'
  | 'catalog:assets:write'
  // ── Contracts ──
  | 'contracts:read'
  | 'contracts:write'
  | 'contracts:import'
  // ── Developer Portal ──
  | 'developer-portal:read'
  | 'developer-portal:write'
  // ── Change Intelligence ──
  | 'change-intelligence:read'
  | 'change-intelligence:releases:read'
  | 'change-intelligence:releases:write'
  | 'change-intelligence:blast-radius:read'
  // ── Workflow ──
  | 'workflow:read'
  | 'workflow:write'
  | 'workflow:approve'
  // ── Promotion ──
  | 'promotion:read'
  | 'promotion:write'
  | 'promotion:promote'
  // ── Ruleset Governance ──
  | 'ruleset-governance:read'
  | 'ruleset-governance:write'
  // ── Operations ──
  | 'operations:incidents:read'
  | 'operations:incidents:write'
  | 'operations:runbooks:read'
  | 'operations:runbooks:write'
  | 'operations:reliability:read'
  | 'operations:automation:read'
  | 'operations:automation:write'
  | 'operations:automation:admin'
  // ── AI Hub ──
  | 'ai:assistant:read'
  | 'ai:models:read'
  | 'ai:models:write'
  | 'ai:policies:read'
  | 'ai:policies:write'
  | 'ai:ide:read'
  | 'ai:ide:write'
  | 'ai:governance:read'
  // ── Governance ──
  | 'governance:reports:read'
  | 'governance:risk:read'
  | 'governance:compliance:read'
  | 'governance:finops:read'
  | 'governance:policies:read'
  | 'governance:evidence:read'
  | 'governance:controls:read'
  // ── Product Analytics ──
  | 'governance:analytics:read'
  | 'governance:analytics:write'
  // ── Organization Governance ──
  | 'governance:teams:read'
  | 'governance:teams:write'
  | 'governance:domains:read'
  | 'governance:domains:write'
  // ── Governance Packs ──
  | 'governance:packs:read'
  | 'governance:packs:write'
  | 'governance:waivers:read'
  | 'governance:waivers:write'
  // ── Audit ──
  | 'audit:read'
  | 'audit:export'
  // ── Integrations ──
  | 'integrations:read'
  | 'integrations:write'
  // ── Platform ──
  | 'platform:settings:read'
  | 'platform:settings:write'
  | 'platform:admin:read';

// ── Helpers para UI gating e testes ──────────────────────────────────────────
// Mapeamento client-side simplificado de role→permissões para controle visual.
// O backend continua sendo a fonte de verdade para enforcement real.
//
// NOTA: Em produção, as permissões efetivas do usuário vêm do servidor via
// CurrentUserProfile.permissions (endpoint /auth/me). Este mapeamento é usado
// apenas para fallback visual quando o perfil ainda não foi carregado, e para
// testes unitários dos componentes de UI que dependem de roles.
// Sincronização: manter alinhado com IdentityAccess.Application/Roles/.

const rolePermissions: Record<string, string[]> = {
  Admin: [
    'users:read', 'users:write', 'releases:read', 'releases:write',
    'contracts:read', 'contracts:write', 'audit:read', 'audit:export',
    'workflow:approve',
  ],
  Developer: [
    'releases:read', 'releases:write', 'contracts:read', 'contracts:write',
  ],
  Viewer: [
    'releases:read', 'contracts:read',
  ],
  Auditor: [
    'audit:read', 'audit:export', 'releases:read', 'contracts:read',
  ],
  Manager: [
    'users:read', 'releases:read', 'contracts:read', 'workflow:approve',
  ],
};

export function getPermissionsForRoles(roles: string[]): Set<string> {
  const perms = new Set<string>();
  for (const role of roles) {
    const rolePerms = rolePermissions[role];
    if (rolePerms) {
      for (const p of rolePerms) perms.add(p);
    }
  }
  return perms;
}

export function hasPermission(roles: string[], permission: string): boolean {
  return getPermissionsForRoles(roles).has(permission);
}
