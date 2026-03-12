import { createContext, useContext, useState, useCallback, useEffect, type ReactNode } from 'react';
import type { LoginResponse, CurrentUserProfile } from '../types';
import { identityApi } from '../api';

interface AuthState {
  isAuthenticated: boolean;
  accessToken: string | null;
  user: CurrentUserProfile | null;
  tenantId: string | null;
}

interface AuthContextValue extends AuthState {
  login: (email: string, password: string, tenantId: string) => Promise<void>;
  logout: () => Promise<void>;
}

export const AuthContext = createContext<AuthContextValue | null>(null);

/** Persiste dados de sessão no localStorage após login bem-sucedido. */
function persistSession(data: LoginResponse, tenantId: string): void {
  localStorage.setItem('access_token', data.accessToken);
  localStorage.setItem('refresh_token', data.refreshToken);
  localStorage.setItem('tenant_id', tenantId);
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

  const login = useCallback(async (email: string, password: string, tenantId: string) => {
    const data = await identityApi.login({ email, password, tenantId });
    persistSession(data, tenantId);

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
      tenantId,
    });
  }, []);

  const logout = useCallback(async () => {
    try {
      await identityApi.logout();
    } catch {
      // Logout local mesmo se o backend falhar
    }
    clearSession();
    setState({ isAuthenticated: false, accessToken: null, user: null, tenantId: null });
  }, []);

  return (
    <AuthContext.Provider value={{ ...state, login, logout }}>
      {children}
    </AuthContext.Provider>
  );
}

export function useAuth(): AuthContextValue {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error('useAuth must be used within AuthProvider');
  return ctx;
}
