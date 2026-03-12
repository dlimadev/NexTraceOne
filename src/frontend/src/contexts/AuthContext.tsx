import { createContext, useContext, useState, useCallback, useEffect, type ReactNode } from 'react';
import type { LoginResponse, CurrentUserProfile, TenantInfo, SelectTenantResponse } from '../types';
import { identityApi } from '../api';

/**
 * Estado de autenticação e contexto do tenant selecionado.
 *
 * `requiresTenantSelection` indica que o login foi bem-sucedido mas o usuário
 * possui múltiplos tenants — o frontend deve exibir a tela de seleção.
 * `availableTenants` contém a lista de tenants disponíveis para seleção.
 */
interface AuthState {
  isAuthenticated: boolean;
  accessToken: string | null;
  user: CurrentUserProfile | null;
  tenantId: string | null;
  requiresTenantSelection: boolean;
  availableTenants: TenantInfo[];
}

interface AuthContextValue extends AuthState {
  /** Autentica com email e senha. Se houver múltiplos tenants, redireciona para seleção. */
  login: (email: string, password: string) => Promise<'authenticated' | 'select-tenant'>;
  /** Seleciona um tenant após login multi-tenant. */
  selectTenant: (tenantId: string) => Promise<void>;
  /** Encerra a sessão e limpa dados locais. */
  logout: () => Promise<void>;
}

export const AuthContext = createContext<AuthContextValue | null>(null);

/** Persiste dados de sessão no localStorage após login bem-sucedido. */
function persistSession(data: LoginResponse): void {
  localStorage.setItem('access_token', data.accessToken);
  localStorage.setItem('refresh_token', data.refreshToken);
  localStorage.setItem('tenant_id', data.user.tenantId);
  localStorage.setItem('user_id', data.user.id);
}

/** Remove todos os dados de sessão do localStorage. */
function clearSession(): void {
  localStorage.removeItem('access_token');
  localStorage.removeItem('refresh_token');
  localStorage.removeItem('tenant_id');
  localStorage.removeItem('user_id');
}

export function AuthProvider({ children }: { children: ReactNode }) {
  const [state, setState] = useState<AuthState>(() => {
    const token = localStorage.getItem('access_token');
    const tenantId = localStorage.getItem('tenant_id');
    return {
      isAuthenticated: !!token,
      accessToken: token,
      user: null,
      tenantId,
      requiresTenantSelection: false,
      availableTenants: [],
    };
  });

  // Carrega o perfil do usuário autenticado ao iniciar quando há token armazenado
  useEffect(() => {
    const token = localStorage.getItem('access_token');
    if (token) {
      identityApi.getCurrentUser().then((profile) => {
        setState((s) => ({ ...s, user: profile }));
      }).catch((error: unknown) => {
        console.warn('[AuthContext] Failed to load user profile on startup:', error);
      });
    }
  }, []);

  const login = useCallback(async (email: string, password: string): Promise<'authenticated' | 'select-tenant'> => {
    const data = await identityApi.login({ email, password });
    persistSession(data);

    // Verifica se o usuário tem múltiplos tenants disponíveis
    let tenants: TenantInfo[] = [];
    try {
      tenants = await identityApi.listMyTenants();
    } catch {
      // Se falhar ao listar tenants, assume o tenant do login
    }

    // Se houver múltiplos tenants ativos, exige seleção explícita
    const activeTenants = tenants.filter((t) => t.isActive);
    if (activeTenants.length > 1) {
      setState({
        isAuthenticated: true,
        accessToken: data.accessToken,
        user: null,
        tenantId: null,
        requiresTenantSelection: true,
        availableTenants: activeTenants,
      });
      return 'select-tenant';
    }

    // Carrega perfil completo via /me após login para obter permissões atualizadas
    let profile: CurrentUserProfile | null = null;
    try {
      profile = await identityApi.getCurrentUser();
    } catch {
      // Fallback: monta perfil básico a partir da resposta de login
      profile = {
        id: data.user.id,
        email: data.user.email,
        firstName: '',
        lastName: '',
        fullName: data.user.fullName,
        isActive: true,
        lastLoginAt: null,
        tenantId: data.user.tenantId,
        roleName: data.user.roleName,
        permissions: data.user.permissions,
      };
    }

    setState({
      isAuthenticated: true,
      accessToken: data.accessToken,
      user: profile,
      tenantId: data.user.tenantId,
      requiresTenantSelection: false,
      availableTenants: [],
    });
    return 'authenticated';
  }, []);

  const selectTenant = useCallback(async (tenantId: string) => {
    const response: SelectTenantResponse = await identityApi.selectTenant(tenantId);

    localStorage.setItem('access_token', response.accessToken);
    localStorage.setItem('tenant_id', response.tenantId);

    // Carrega perfil completo com o novo contexto de tenant
    let profile: CurrentUserProfile | null = null;
    try {
      profile = await identityApi.getCurrentUser();
    } catch {
      profile = {
        id: localStorage.getItem('user_id') ?? '',
        email: '',
        firstName: '',
        lastName: '',
        fullName: '',
        isActive: true,
        lastLoginAt: null,
        tenantId: response.tenantId,
        roleName: response.roleName,
        permissions: response.permissions,
      };
    }

    setState({
      isAuthenticated: true,
      accessToken: response.accessToken,
      user: profile,
      tenantId: response.tenantId,
      requiresTenantSelection: false,
      availableTenants: [],
    });
  }, []);

  const logout = useCallback(async () => {
    try {
      await identityApi.logout();
    } catch {
      // Logout local mesmo se o backend falhar
    }
    clearSession();
    setState({
      isAuthenticated: false,
      accessToken: null,
      user: null,
      tenantId: null,
      requiresTenantSelection: false,
      availableTenants: [],
    });
  }, []);

  return (
    <AuthContext.Provider value={{ ...state, login, selectTenant, logout }}>
      {children}
    </AuthContext.Provider>
  );
}

export function useAuth(): AuthContextValue {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error('useAuth must be used within AuthProvider');
  return ctx;
}
