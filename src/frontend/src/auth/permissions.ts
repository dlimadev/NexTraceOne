// Roles disponíveis no sistema RBAC do NexTraceOne
export type AppRole = 'Admin' | 'Manager' | 'Developer' | 'Viewer' | 'Auditor';

// Permissões granulares por módulo
export type Permission =
  | 'users:read'
  | 'users:write'
  | 'releases:read'
  | 'releases:write'
  | 'contracts:read'
  | 'contracts:write'
  | 'graph:read'
  | 'graph:write'
  | 'workflow:read'
  | 'workflow:approve'
  | 'promotion:read'
  | 'promotion:write'
  | 'audit:read'
  | 'audit:export';

// Mapeamento de roles para permissões
const ROLE_PERMISSIONS: Record<AppRole, Permission[]> = {
  Admin: [
    'users:read',
    'users:write',
    'releases:read',
    'releases:write',
    'contracts:read',
    'contracts:write',
    'graph:read',
    'graph:write',
    'workflow:read',
    'workflow:approve',
    'promotion:read',
    'promotion:write',
    'audit:read',
    'audit:export',
  ],
  Manager: [
    'users:read',
    'releases:read',
    'releases:write',
    'contracts:read',
    'contracts:write',
    'graph:read',
    'graph:write',
    'workflow:read',
    'workflow:approve',
    'promotion:read',
    'promotion:write',
    'audit:read',
  ],
  Developer: [
    'releases:read',
    'releases:write',
    'contracts:read',
    'contracts:write',
    'graph:read',
    'graph:write',
    'workflow:read',
    'promotion:read',
    'audit:read',
  ],
  Viewer: [
    'releases:read',
    'contracts:read',
    'graph:read',
    'workflow:read',
    'promotion:read',
    'audit:read',
  ],
  Auditor: [
    'releases:read',
    'contracts:read',
    'graph:read',
    'workflow:read',
    'promotion:read',
    'audit:read',
    'audit:export',
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
