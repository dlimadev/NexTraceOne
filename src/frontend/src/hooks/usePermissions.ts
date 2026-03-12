import { useMemo } from 'react';
import { useAuth } from '../contexts/AuthContext';
import { getPermissionsForRoles, type Permission } from '../auth/permissions';

/**
 * Hook que expõe as permissões do usuário autenticado.
 * Retorna uma função `can(permission)` para verificar permissões granulares.
 */
export function usePermissions() {
  const { user } = useAuth();
  const roles = user?.roles ?? [];

  const permissionSet = useMemo(() => getPermissionsForRoles(roles), [roles]);

  const can = (permission: Permission): boolean => permissionSet.has(permission);

  return { can, roles };
}
