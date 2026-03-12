import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { render, screen, waitFor, act } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { AuthProvider, useAuth } from '../../contexts/AuthContext';
import type { LoginResponse, UserProfile } from '../../types';

// Mock do módulo de API de identidade
vi.mock('../../api', () => ({
  identityApi: {
    login: vi.fn(),
    getUserProfile: vi.fn(),
  },
}));

import { identityApi } from '../../api';

const mockLoginResponse: LoginResponse = {
  accessToken: 'mock-access-token',
  refreshToken: 'mock-refresh-token',
  expiresIn: 3600,
  userId: 'user-123',
  email: 'dev@acme.com',
  roles: ['Developer'],
};

const mockProfile: UserProfile = {
  id: 'user-123',
  email: 'dev@acme.com',
  fullName: 'Dev User',
  roles: ['Developer'],
  tenantId: 'tenant-001',
};

// Componente auxiliar para acessar o contexto
function TestConsumer() {
  const { isAuthenticated, user, login, logout } = useAuth();
  return (
    <div>
      <span data-testid="auth-status">{isAuthenticated ? 'authenticated' : 'unauthenticated'}</span>
      <span data-testid="user-email">{user?.email ?? 'none'}</span>
      <button onClick={() => login('dev@acme.com', 'pass123', 'tenant-001')}>Login</button>
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
    localStorage.clear();
    vi.clearAllMocks();
  });

  afterEach(() => {
    localStorage.clear();
  });

  it('inicia não autenticado quando não há token armazenado', () => {
    renderWithAuth();
    expect(screen.getByTestId('auth-status')).toHaveTextContent('unauthenticated');
  });

  it('inicia autenticado quando há token no localStorage', () => {
    localStorage.setItem('access_token', 'existing-token');
    localStorage.setItem('tenant_id', 'tenant-001');
    renderWithAuth();
    expect(screen.getByTestId('auth-status')).toHaveTextContent('authenticated');
  });

  it('autentica o usuário após login bem-sucedido', async () => {
    vi.mocked(identityApi.login).mockResolvedValue(mockLoginResponse);
    vi.mocked(identityApi.getUserProfile).mockResolvedValue(mockProfile);

    renderWithAuth();
    await act(async () => {
      await userEvent.click(screen.getByText('Login'));
    });

    await waitFor(() => {
      expect(screen.getByTestId('auth-status')).toHaveTextContent('authenticated');
    });
  });

  it('persiste o token no localStorage após login', async () => {
    vi.mocked(identityApi.login).mockResolvedValue(mockLoginResponse);
    vi.mocked(identityApi.getUserProfile).mockResolvedValue(mockProfile);

    renderWithAuth();
    await act(async () => {
      await userEvent.click(screen.getByText('Login'));
    });

    await waitFor(() => {
      expect(localStorage.getItem('access_token')).toBe('mock-access-token');
      expect(localStorage.getItem('tenant_id')).toBe('tenant-001');
    });
  });

  it('carrega o perfil do usuário após login', async () => {
    vi.mocked(identityApi.login).mockResolvedValue(mockLoginResponse);
    vi.mocked(identityApi.getUserProfile).mockResolvedValue(mockProfile);

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
    vi.mocked(identityApi.getUserProfile).mockRejectedValue(new Error('Not found'));

    renderWithAuth();
    await act(async () => {
      await userEvent.click(screen.getByText('Login'));
    });

    await waitFor(() => {
      expect(screen.getByTestId('auth-status')).toHaveTextContent('authenticated');
    });
  });

  it('faz logout limpando o estado e o localStorage', async () => {
    localStorage.setItem('access_token', 'existing-token');
    localStorage.setItem('tenant_id', 'tenant-001');
    renderWithAuth();

    await act(async () => {
      await userEvent.click(screen.getByText('Logout'));
    });

    expect(screen.getByTestId('auth-status')).toHaveTextContent('unauthenticated');
    expect(localStorage.getItem('access_token')).toBeNull();
    expect(localStorage.getItem('tenant_id')).toBeNull();
  });

  it('lança erro quando useAuth é usado fora de AuthProvider', () => {
    // Suprime o console.error esperado durante o teste
    const consoleSpy = vi.spyOn(console, 'error').mockImplementation(() => {});
    expect(() => render(<TestConsumer />)).toThrow();
    consoleSpy.mockRestore();
  });
});
