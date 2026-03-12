import { createContext, useContext, useState, useCallback, useEffect, type ReactNode } from 'react';
import type { LoginResponse, CurrentUserProfile, TenantInfo, SelectTenantResponse } from '../types';
import { identityApi } from '../api';
import {
  storeTokens,
  updateAccessToken,
  getAccessToken,
  getTenantId,
  getUserId,
  storeTenantId,
  storeUserId,
  clearAllTokens,
  migrateFromLocalStorage,
  hasActiveSession,
} from '../utils/tokenStorage';

/**
 * Estado de autenticação e contexto do tenant selecionado.
 *
 * `requiresTenantSelection` indica que o login foi bem-sucedido mas o usuário
 * possui múltiplos tenants — o frontend deve exibir a tela de seleção.
 * `availableTenants` contém a lista de tenants disponíveis para seleção.
 *
 * Tokens são armazenados de forma segura via módulo tokenStorage:
 * - Access token: sessionStorage (escopo de aba)
 * - Refresh token: memória apenas (não persiste no browser)
 * - Tenant/User ID: sessionStorage (necessário para re-hidratação)
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

/**
 * Persiste dados de sessão no storage seguro após login bem-sucedido.
 * Access token vai para sessionStorage, refresh token fica em memória.
 */
function persistSession(data: LoginResponse): void {
  storeTokens(data.accessToken, data.refreshToken);
  storeTenantId(data.user.tenantId);
  storeUserId(data.user.id);
}

export function AuthProvider({ children }: { children: ReactNode }) {
  // Migra dados de localStorage para o novo storage na primeira carga
  useEffect(() => {
    migrateFromLocalStorage();
  }, []);

  const [state, setState] = useState<AuthState>(() => {
    // Tenta migrar dados antigos do localStorage se existirem
    migrateFromLocalStorage();
    const token = getAccessToken();
    const tenantId = getTenantId();
    return {
      isAuthenticated: !!token,
      accessToken: token,
      user: null,
      tenantId,
      requiresTenantSelection: false,
      availableTenants: [],
    };
  });

  /**
   * Escuta evento de sessão expirada disparado pelo interceptor do API client.
   * Limpa estado de autenticação e redireciona para login.
   * Usar evento customizado evita import circular entre AuthContext e apiClient.
   */
  useEffect(() => {
    const handleSessionExpired = () => {
      clearAllTokens();
      setState({
        isAuthenticated: false,
        accessToken: null,
        user: null,
        tenantId: null,
        requiresTenantSelection: false,
        availableTenants: [],
      });
    };

    window.addEventListener('auth:session-expired', handleSessionExpired);
    return () => window.removeEventListener('auth:session-expired', handleSessionExpired);
  }, []);

  // Carrega o perfil do usuário autenticado ao iniciar quando há token armazenado
  useEffect(() => {
    if (hasActiveSession()) {
      identityApi.getCurrentUser().then((profile) => {
        setState((s) => ({ ...s, user: profile }));
      }).catch(() => {
        // Se falhar ao carregar perfil, sessão pode estar inválida.
        // Não loga erro com detalhes para evitar vazamento de dados sensíveis.
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

    updateAccessToken(response.accessToken);
    storeTenantId(response.tenantId);

    // Carrega perfil completo com o novo contexto de tenant
    let profile: CurrentUserProfile | null = null;
    try {
      profile = await identityApi.getCurrentUser();
    } catch {
      profile = {
        id: getUserId() ?? '',
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
    clearAllTokens();
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
