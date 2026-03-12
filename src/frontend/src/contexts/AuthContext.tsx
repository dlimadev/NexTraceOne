import { createContext, useContext, useState, useCallback, useEffect, type ReactNode } from 'react';
import type { LoginResponse, UserProfile } from '../types';
import { identityApi } from '../api';

interface AuthState {
  isAuthenticated: boolean;
  accessToken: string | null;
  user: UserProfile | null;
  tenantId: string | null;
}

interface AuthContextValue extends AuthState {
  login: (email: string, password: string, tenantId: string) => Promise<void>;
  logout: () => void;
}

export const AuthContext = createContext<AuthContextValue | null>(null);

function persistSession(data: LoginResponse, tenantId: string): void {
  localStorage.setItem('access_token', data.accessToken);
  localStorage.setItem('refresh_token', data.refreshToken);
  localStorage.setItem('tenant_id', tenantId);
  localStorage.setItem('user_id', data.userId);
}

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

  // Carrega o perfil do usuário ao iniciar quando há token armazenado
  useEffect(() => {
    const token = localStorage.getItem('access_token');
    const userId = localStorage.getItem('user_id');
    if (token && userId) {
      identityApi.getUserProfile(userId).then((profile) => {
        setState((s) => ({ ...s, user: profile }));
      }).catch((error: unknown) => {
        // Log silencioso: usuário continua autenticado, mas sem perfil
        // (permissões serão vazias até um re-login bem-sucedido)
        console.warn('[AuthContext] Failed to load user profile on startup:', error);
      });
    }
  }, []);

  const login = useCallback(async (email: string, password: string, tenantId: string) => {
    const data = await identityApi.login({ email, password, tenantId });
    persistSession(data, tenantId);
    let profile = null;
    try {
      profile = await identityApi.getUserProfile(data.userId);
    } catch {
      // Profile fetch failed — user is still authenticated; UI will degrade gracefully
    }
    setState({
      isAuthenticated: true,
      accessToken: data.accessToken,
      user: profile,
      tenantId,
    });
  }, []);

  const logout = useCallback(() => {
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
