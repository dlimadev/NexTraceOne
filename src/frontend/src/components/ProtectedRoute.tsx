import { Navigate } from 'react-router-dom';
import { usePermissions } from '../hooks/usePermissions';
import type { Permission } from '../auth/permissions';

interface ProtectedRouteProps {
  /** Permissão necessária para acessar a rota */
  permission: Permission;
  /** Componente filho a renderizar quando autorizado */
  children: React.ReactNode;
  /** Rota de redirecionamento quando não autorizado (padrão: '/') */
  redirectTo?: string;
}

/**
 * Componente que protege rotas com base em permissões do usuário.
 * Redireciona para `redirectTo` se o usuário não tiver a permissão necessária.
 */
export function ProtectedRoute({ permission, children, redirectTo = '/' }: ProtectedRouteProps) {
  const { can } = usePermissions();

  if (!can(permission)) {
    return <Navigate to={redirectTo} replace />;
  }

  return <>{children}</>;
}
