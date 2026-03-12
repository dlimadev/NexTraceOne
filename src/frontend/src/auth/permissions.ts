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
  | 'identity:users:read'
  | 'identity:users:write'
  | 'identity:roles:read'
  | 'identity:roles:assign'
  | 'identity:sessions:read'
  | 'identity:sessions:revoke'
  | 'identity:permissions:read'
  | 'engineering-graph:assets:read'
  | 'engineering-graph:assets:write'
  | 'contracts:read'
  | 'contracts:write'
  | 'contracts:import'
  | 'change-intelligence:releases:read'
  | 'change-intelligence:releases:write'
  | 'change-intelligence:blast-radius:read'
  | 'workflow:read'
  | 'workflow:write'
  | 'workflow:approve'
  | 'promotion:read'
  | 'promotion:write'
  | 'promotion:promote'
  | 'ruleset-governance:read'
  | 'ruleset-governance:write'
  | 'audit:read'
  | 'audit:export'
  | 'licensing:read'
  | 'licensing:write'
  | 'platform:settings:read'
  | 'platform:settings:write';

// Mapeamento de roles para permissões — espelha Role.GetPermissionsForRole do backend
const ROLE_PERMISSIONS: Record<AppRole, Permission[]> = {
  PlatformAdmin: [
    'identity:users:read',
    'identity:users:write',
    'identity:roles:read',
    'identity:roles:assign',
    'identity:sessions:read',
    'identity:sessions:revoke',
    'identity:permissions:read',
    'engineering-graph:assets:read',
    'engineering-graph:assets:write',
    'contracts:read',
    'contracts:write',
    'contracts:import',
    'change-intelligence:releases:read',
    'change-intelligence:releases:write',
    'change-intelligence:blast-radius:read',
    'workflow:read',
    'workflow:write',
    'workflow:approve',
    'promotion:read',
    'promotion:write',
    'promotion:promote',
    'ruleset-governance:read',
    'ruleset-governance:write',
    'audit:read',
    'audit:export',
    'licensing:read',
    'licensing:write',
    'platform:settings:read',
    'platform:settings:write',
  ],
  TechLead: [
    'identity:users:read',
    'identity:roles:read',
    'identity:sessions:read',
    'engineering-graph:assets:read',
    'engineering-graph:assets:write',
    'contracts:read',
    'contracts:write',
    'contracts:import',
    'change-intelligence:releases:read',
    'change-intelligence:releases:write',
    'change-intelligence:blast-radius:read',
    'workflow:read',
    'workflow:write',
    'workflow:approve',
    'promotion:read',
    'promotion:write',
    'promotion:promote',
    'ruleset-governance:read',
    'audit:read',
    'audit:export',
  ],
  Developer: [
    'identity:users:read',
    'engineering-graph:assets:read',
    'contracts:read',
    'contracts:write',
    'contracts:import',
    'change-intelligence:releases:read',
    'change-intelligence:releases:write',
    'change-intelligence:blast-radius:read',
    'workflow:read',
    'promotion:read',
    'ruleset-governance:read',
    'audit:read',
  ],
  Viewer: [
    'identity:users:read',
    'engineering-graph:assets:read',
    'contracts:read',
    'change-intelligence:releases:read',
    'change-intelligence:blast-radius:read',
    'workflow:read',
    'promotion:read',
    'audit:read',
  ],
  Auditor: [
    'identity:users:read',
    'identity:sessions:read',
    'engineering-graph:assets:read',
    'contracts:read',
    'change-intelligence:releases:read',
    'change-intelligence:blast-radius:read',
    'workflow:read',
    'promotion:read',
    'ruleset-governance:read',
    'audit:read',
    'audit:export',
  ],
  SecurityReview: [
    'identity:users:read',
    'identity:roles:read',
    'identity:sessions:read',
    'identity:sessions:revoke',
    'engineering-graph:assets:read',
    'contracts:read',
    'change-intelligence:releases:read',
    'change-intelligence:blast-radius:read',
    'workflow:read',
    'workflow:approve',
    'promotion:read',
    'ruleset-governance:read',
    'ruleset-governance:write',
    'audit:read',
    'audit:export',
  ],
  ApprovalOnly: [
    'workflow:read',
    'workflow:approve',
    'change-intelligence:releases:read',
    'change-intelligence:blast-radius:read',
    'promotion:read',
    'audit:read',
  ],
};

/**
 * Retorna o conjunto de permissões para uma lista de roles.
 * Se um usuário possui múltiplos roles, a union de permissões é retornada.
 */
export function getPermissionsForRoles(roles: string[]): Set<Permission> {
  const result = new Set<Permission>();
  for (const role of roles) {
    const perms = ROLE_PERMISSIONS[role as AppRole];
    if (perms) {
      perms.forEach((p) => result.add(p));
    }
  }
  return result;
}

/**
 * Verifica se a lista de roles possui a permissão solicitada.
 */
export function hasPermission(roles: string[], permission: Permission): boolean {
  const perms = getPermissionsForRoles(roles);
  return perms.has(permission);
}
