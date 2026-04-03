/* eslint-disable react-refresh/only-export-components */
import { createContext, useContext, useState, useCallback, useEffect, type ReactNode } from 'react';
import type {
  AuthLoginResponse,
  CurrentUserProfile,
  TenantInfo,
  SelectTenantResponse,
  CookieSessionLoginResponse,
} from '../types';
import { identityApi } from '../api';
import {
  storeTokens,
  updateAccessToken,
  getAccessToken,
  getTenantId,
  getUserId,
  storeTenantId,
  storeUserId,
  storeCsrfToken,
  clearAllTokens,
  migrateFromLocalStorage,
} from '../utils/tokenStorage';

interface AuthState {
  isAuthenticated: boolean;
  isLoadingUser: boolean;
  accessToken: string | null;
  user: CurrentUserProfile | null;
  tenantId: string | null;
  requiresTenantSelection: boolean;
  availableTenants: TenantInfo[];
}

interface AuthContextValue extends AuthState {
  login: (email: string, password: string) => Promise<'authenticated' | 'select-tenant'>;
  selectTenant: (tenantId: string) => Promise<void>;
  logout: () => Promise<void>;
}

export const AuthContext = createContext<AuthContextValue | null>(null);

function isCookieSessionLoginResponse(data: AuthLoginResponse): data is CookieSessionLoginResponse {
  return 'csrfToken' in data;
}

function persistSession(data: AuthLoginResponse): void {
  storeTenantId(data.user.tenantId);
  storeUserId(data.user.id);

  if (isCookieSessionLoginResponse(data)) {
    storeCsrfToken(data.csrfToken);
    return;
  }

  storeTokens(data.accessToken, data.refreshToken);
}

export function AuthProvider({ children }: { children: ReactNode }) {
  useEffect(() => {
    migrateFromLocalStorage();
  }, []);

  const [state, setState] = useState<AuthState>(() => ({
    isAuthenticated: false,
    isLoadingUser: true,
    accessToken: getAccessToken(),
    user: null,
    tenantId: getTenantId(),
    requiresTenantSelection: false,
    availableTenants: [],
  }));

  useEffect(() => {
    const handleSessionExpired = () => {
      clearAllTokens();
      setState({
        isAuthenticated: false,
        isLoadingUser: false,
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

  useEffect(() => {
    let isMounted = true;

    const bootstrapSession = async () => {
      try {
        const profile = await identityApi.getCurrentUser();

        // Tenta refresh silencioso via cookie para repor o access token em memória.
        // Necessário para que o interceptor de 401 funcione após recarregamento de página.
        try {
          const refreshData = await identityApi.bootRefresh();
          const newAccess = refreshData?.accessToken;
          const newRefresh = refreshData?.refreshToken;
          if (
            typeof newAccess === 'string' && newAccess.length > 0 &&
            typeof newRefresh === 'string' && newRefresh.length > 0
          ) {
            storeTokens(newAccess, newRefresh);
          }
        } catch {
          // Refresh silencioso falhou — continua com autenticação baseada em cookie.
        }

        try {
          const csrf = await identityApi.getCsrfToken();
          if (csrf?.csrfToken) {
            storeCsrfToken(csrf.csrfToken);
          }
        } catch {
          // Fluxo bearer legado não expõe endpoint de CSRF ativo.
        }

        if (!isMounted) {
          return;
        }

        storeTenantId(profile.tenantId);
        storeUserId(profile.id);

        setState({
          isAuthenticated: true,
          isLoadingUser: false,
          accessToken: getAccessToken(),
          user: profile,
          tenantId: profile.tenantId,
          requiresTenantSelection: false,
          availableTenants: [],
        });
      } catch {
        if (!isMounted) {
          return;
        }

        clearAllTokens();
        setState({
          isAuthenticated: false,
          isLoadingUser: false,
          accessToken: null,
          user: null,
          tenantId: null,
          requiresTenantSelection: false,
          availableTenants: [],
        });
      }
    };

    void bootstrapSession();

    return () => {
      isMounted = false;
    };
  }, []);

  const login = useCallback(async (email: string, password: string): Promise<'authenticated' | 'select-tenant'> => {
    const data = await identityApi.login({ email, password });
    persistSession(data);

    let tenants: TenantInfo[] = [];
    try {
      tenants = await identityApi.listMyTenants();
    } catch {
      // Se falhar ao listar tenants, assume o tenant do login
    }

    const activeTenants = tenants.filter((t) => t.isActive);
    if (activeTenants.length > 1) {
      setState({
        isAuthenticated: true,
        isLoadingUser: false,
        accessToken: getAccessToken(),
        user: null,
        tenantId: null,
        requiresTenantSelection: true,
        availableTenants: activeTenants,
      });
      return 'select-tenant';
    }

    let profile: CurrentUserProfile | null = null;
    try {
      profile = await identityApi.getCurrentUser();
    } catch {
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
      isLoadingUser: false,
      accessToken: getAccessToken(),
      user: profile,
      tenantId: profile.tenantId,
      requiresTenantSelection: false,
      availableTenants: [],
    });
    return 'authenticated';
  }, []);

  const selectTenant = useCallback(async (tenantId: string) => {
    const response: SelectTenantResponse = await identityApi.selectTenant(tenantId);

    if (response.csrfToken) {
      storeCsrfToken(response.csrfToken);
    } else {
      updateAccessToken(response.accessToken);
    }

    storeTenantId(response.tenantId);

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
      isLoadingUser: false,
      accessToken: getAccessToken(),
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
      isLoadingUser: false,
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
