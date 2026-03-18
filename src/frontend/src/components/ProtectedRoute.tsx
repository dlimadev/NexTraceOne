import { Navigate } from 'react-router-dom';
import { useAuth } from '../contexts/AuthContext';
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
 * Aguarda o carregamento do perfil antes de avaliar permissões para evitar
 * redirecionamento incorreto durante a hidratação inicial da sessão.
 * Redireciona para `redirectTo` se o usuário não tiver a permissão necessária.
 */
export function ProtectedRoute({ permission, children, redirectTo = '/' }: ProtectedRouteProps) {
  const { isLoadingUser } = useAuth();
  const { can } = usePermissions();

  if (isLoadingUser) {
    return (
      <div className="flex items-center justify-center h-full min-h-[50vh]">
        <div className="h-8 w-8 animate-spin rounded-full border-4 border-accent border-t-transparent" />
      </div>
    );
  }

  if (!can(permission)) {
    return <Navigate to={redirectTo} replace />;
  }

  return <>{children}</>;
}
