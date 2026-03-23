import { useMemo } from 'react';
import { useAuth } from '../contexts/AuthContext';
import type { Permission } from '../auth/permissions';

/**
 * Hook que expõe as permissões efetivas do usuário autenticado.
 *
 * As permissões são obtidas do servidor (campo `permissions` em CurrentUserProfile),
 * que é a fonte de verdade — reflete o cálculo real do backend baseado em role + tenant.
 * Não usamos mapeamento client-side para evitar divergência com o backend.
 *
 * Retorna:
 * - `can(permission)`: verifica se o usuário tem a permissão solicitada.
 * - `roleName`: nome do papel do usuário no tenant atual.
 * - `permissions`: lista bruta de permissões para renderização condicional.
 */
export function usePermissions() {
  const { user } = useAuth();

  const permissions = useMemo(() => user?.permissions ?? [], [user?.permissions]);
  const roleName = user?.roleName ?? '';

  const permissionSet = useMemo(
    () => new Set<string>(permissions),
    [permissions],
  );

  const can = (permission: Permission): boolean => permissionSet.has(permission);

  return { can, roleName, permissions };
}
