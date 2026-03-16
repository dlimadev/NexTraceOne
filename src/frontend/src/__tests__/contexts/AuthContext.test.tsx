import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { render, screen, waitFor, act } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { AuthProvider, useAuth } from '../../contexts/AuthContext';
import type { LoginResponse, CurrentUserProfile } from '../../types';

// Mock do módulo de API de identidade
vi.mock('../../api', () => ({
  identityApi: {
    login: vi.fn(),
    getCurrentUser: vi.fn(),
    listMyTenants: vi.fn(),
    logout: vi.fn(),
    selectTenant: vi.fn(),
  },
}));

// Mock tokenStorage para controlar o armazenamento em testes
vi.mock('../../utils/tokenStorage', () => {
  let store: Record<string, string> = {};
  return {
    storeTokens: vi.fn((access: string, _refresh: string) => { store['access_token'] = access; }),
    updateAccessToken: vi.fn((access: string) => { store['access_token'] = access; }),
    getAccessToken: vi.fn(() => store['access_token'] ?? null),
    getTenantId: vi.fn(() => store['tenant_id'] ?? null),
    getUserId: vi.fn(() => store['user_id'] ?? null),
    storeTenantId: vi.fn((tid: string) => { store['tenant_id'] = tid; }),
    storeUserId: vi.fn((uid: string) => { store['user_id'] = uid; }),
    clearAllTokens: vi.fn(() => { store = {}; }),
    migrateFromLocalStorage: vi.fn(),
    hasActiveSession: vi.fn(() => !!store['access_token']),
  };
});

import { identityApi } from '../../api';
import { getAccessToken, getTenantId, clearAllTokens, storeTokens, hasActiveSession } from '../../utils/tokenStorage';

const mockLoginResponse: LoginResponse = {
  accessToken: 'mock-access-token',
  refreshToken: 'mock-refresh-token',
  expiresIn: 3600,
  user: {
    id: 'user-123',
    email: 'dev@acme.com',
    fullName: 'Dev User',
    tenantId: 'tenant-001',
    roleName: 'Developer',
    permissions: ['releases:read', 'releases:write', 'contracts:read', 'contracts:write'],
  },
};

const mockProfile: CurrentUserProfile = {
  id: 'user-123',
  email: 'dev@acme.com',
  firstName: 'Dev',
  lastName: 'User',
  fullName: 'Dev User',
  isActive: true,
  lastLoginAt: null,
  tenantId: 'tenant-001',
  roleName: 'Developer',
  permissions: ['releases:read', 'releases:write', 'contracts:read', 'contracts:write'],
};

// Componente auxiliar para acessar o contexto
function TestConsumer() {
  const { isAuthenticated, user, login, logout } = useAuth();
  return (
    <div>
      <span data-testid="auth-status">{isAuthenticated ? 'authenticated' : 'unauthenticated'}</span>
      <span data-testid="user-email">{user?.email ?? 'none'}</span>
      <button onClick={() => login('dev@acme.com', 'pass123')}>Login</button>
      <button onClick={logout}>Logout</button>
    </div>
  );
}

function renderWithAuth() {
  return render(
    <AuthProvider>
      <TestConsumer />
    </AuthProvider>
  );
}

describe('AuthContext', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    // Reset mock store
    vi.mocked(clearAllTokens).mockImplementation(() => {
      vi.mocked(getAccessToken).mockReturnValue(null);
      vi.mocked(getTenantId).mockReturnValue(null);
      vi.mocked(hasActiveSession).mockReturnValue(false);
    });
    vi.mocked(getAccessToken).mockReturnValue(null);
    vi.mocked(getTenantId).mockReturnValue(null);
    vi.mocked(hasActiveSession).mockReturnValue(false);
    vi.mocked(identityApi.listMyTenants).mockResolvedValue([]);
  });

  afterEach(() => {
    vi.restoreAllMocks();
  });

  it('inicia não autenticado quando não há token armazenado', () => {
    renderWithAuth();
    expect(screen.getByTestId('auth-status')).toHaveTextContent('unauthenticated');
  });

  it('inicia autenticado quando há token no storage', () => {
    vi.mocked(getAccessToken).mockReturnValue('existing-token');
    vi.mocked(getTenantId).mockReturnValue('tenant-001');
    vi.mocked(hasActiveSession).mockReturnValue(true);
    vi.mocked(identityApi.getCurrentUser).mockResolvedValue(mockProfile);
    renderWithAuth();
    expect(screen.getByTestId('auth-status')).toHaveTextContent('authenticated');
  });

  it('autentica o usuário após login bem-sucedido', async () => {
    vi.mocked(identityApi.login).mockResolvedValue(mockLoginResponse);
    vi.mocked(identityApi.getCurrentUser).mockResolvedValue(mockProfile);

    renderWithAuth();
    await act(async () => {
      await userEvent.click(screen.getByText('Login'));
    });

    await waitFor(() => {
      expect(screen.getByTestId('auth-status')).toHaveTextContent('authenticated');
    });
  });

  it('persiste o token no storage após login', async () => {
    vi.mocked(identityApi.login).mockResolvedValue(mockLoginResponse);
    vi.mocked(identityApi.getCurrentUser).mockResolvedValue(mockProfile);

    renderWithAuth();
    await act(async () => {
      await userEvent.click(screen.getByText('Login'));
    });

    await waitFor(() => {
      expect(storeTokens).toHaveBeenCalledWith('mock-access-token', 'mock-refresh-token');
    });
  });

  it('carrega o perfil do usuário após login', async () => {
    vi.mocked(identityApi.login).mockResolvedValue(mockLoginResponse);
    vi.mocked(identityApi.getCurrentUser).mockResolvedValue(mockProfile);

    renderWithAuth();
    await act(async () => {
      await userEvent.click(screen.getByText('Login'));
    });

    await waitFor(() => {
      expect(screen.getByTestId('user-email')).toHaveTextContent('dev@acme.com');
    });
  });

  it('permanece autenticado mesmo com falha ao carregar perfil', async () => {
    vi.mocked(identityApi.login).mockResolvedValue(mockLoginResponse);
    vi.mocked(identityApi.getCurrentUser).mockRejectedValue(new Error('Not found'));

    renderWithAuth();
    await act(async () => {
      await userEvent.click(screen.getByText('Login'));
    });

    await waitFor(() => {
      expect(screen.getByTestId('auth-status')).toHaveTextContent('authenticated');
    });
  });

  it('faz logout limpando o estado e o storage', async () => {
    vi.mocked(getAccessToken).mockReturnValue('existing-token');
    vi.mocked(getTenantId).mockReturnValue('tenant-001');
    vi.mocked(hasActiveSession).mockReturnValue(true);
    vi.mocked(identityApi.getCurrentUser).mockResolvedValue(mockProfile);
    vi.mocked(identityApi.logout).mockResolvedValue(undefined);
    renderWithAuth();

    await act(async () => {
      await userEvent.click(screen.getByText('Logout'));
    });

    expect(screen.getByTestId('auth-status')).toHaveTextContent('unauthenticated');
    expect(clearAllTokens).toHaveBeenCalled();
  });

  it('lança erro quando useAuth é usado fora de AuthProvider', () => {
    // Suprime o console.error esperado durante o teste
    const consoleSpy = vi.spyOn(console, 'error').mockImplementation(() => {});
    expect(() => render(<TestConsumer />)).toThrow();
    consoleSpy.mockRestore();
  });
});
