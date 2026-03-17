import { describe, it, expect, vi } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter } from 'react-router-dom';
import { LoginPage } from '../../features/identity-access/pages/LoginPage';
import { AuthContext } from '../../contexts/AuthContext';

// Mock the identity API used by the SSO handler inside LoginPage
vi.mock('../../features/identity-access/api/identity', () => ({
  identityApi: {
    startOidcLogin: vi.fn(),
  },
}));

// Mock resolveApiError so failed logins return a simple string
vi.mock('../../utils/apiErrors', () => ({
  resolveApiError: () => 'Invalid credentials. Please try again.',
}));

// Helper para criar um valor de contexto de autenticação fake
function makeAuthContext(overrides: Partial<{
  isAuthenticated: boolean;
  login: (email: string, password: string) => Promise<'authenticated' | 'select-tenant'>;
}> = {}) {
  return {
    isAuthenticated: false,
    accessToken: null,
    user: null,
    tenantId: null,
    requiresTenantSelection: false,
    availableTenants: [],
    login: vi.fn().mockResolvedValue('authenticated' as const),
    selectTenant: vi.fn(),
    logout: vi.fn(),
    ...overrides,
  };
}

function renderLoginPage(authOverrides = {}) {
  const auth = makeAuthContext(authOverrides);
  return {
    auth,
    ...render(
      <AuthContext.Provider value={auth}>
        <MemoryRouter>
          <LoginPage />
        </MemoryRouter>
      </AuthContext.Provider>
    ),
  };
}

describe('LoginPage', () => {
  it('renderiza o formulário de login', () => {
    renderLoginPage();
    expect(screen.getByLabelText('Email')).toBeInTheDocument();
    expect(screen.getByLabelText('Password')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /sign in$/i })).toBeInTheDocument();
  });

  it('exibe o título e subtítulo da plataforma', () => {
    renderLoginPage();
    // Split layout has NexTraceOne in both left panel and mobile header
    expect(screen.getAllByText('NexTraceOne').length).toBeGreaterThanOrEqual(1);
    // Tagline appears in both mobile header and desktop left panel
    expect(screen.getAllByText(/sovereign/i).length).toBeGreaterThanOrEqual(1);
  });

  it('chama login com as credenciais corretas ao submeter', async () => {
    const { auth } = renderLoginPage();

    await userEvent.type(screen.getByLabelText('Email'), 'dev@acme.com');
    await userEvent.type(screen.getByLabelText('Password'), 'secret123');
    await userEvent.click(screen.getByRole('button', { name: /sign in$/i }));

    await waitFor(() => {
      expect(auth.login).toHaveBeenCalledWith('dev@acme.com', 'secret123');
    });
  });

  it('exibe mensagem de erro quando o login falha', async () => {
    const loginFn = vi.fn().mockRejectedValue(new Error('Unauthorized'));
    renderLoginPage({ login: loginFn });

    await userEvent.type(screen.getByLabelText('Email'), 'wrong@acme.com');
    await userEvent.type(screen.getByLabelText('Password'), 'wrong');
    await userEvent.click(screen.getByRole('button', { name: /sign in$/i }));

    await waitFor(() => {
      expect(screen.getByText(/invalid credentials/i)).toBeInTheDocument();
    });
  });

  it('desabilita o botão de submit enquanto o login está em progresso', async () => {
    let resolveLogin!: (value: 'authenticated') => void;
    const loginFn = vi.fn().mockImplementation(
      () => new Promise<'authenticated'>((res) => { resolveLogin = res; })
    );
    renderLoginPage({ login: loginFn });

    await userEvent.type(screen.getByLabelText('Email'), 'dev@acme.com');
    await userEvent.type(screen.getByLabelText('Password'), 'pass');
    await userEvent.click(screen.getByRole('button', { name: /sign in$/i }));

    // The submit button should be disabled while loading
    const buttons = screen.getAllByRole('button');
    const submitBtn = buttons.find(b => b.getAttribute('type') === 'submit');
    expect(submitBtn).toBeDisabled();
    resolveLogin('authenticated');
  });

  it('limpa a mensagem de erro ao submeter novamente', async () => {
    const loginFn = vi.fn()
      .mockRejectedValueOnce(new Error('Unauthorized'))
      .mockResolvedValueOnce('authenticated' as const);
    renderLoginPage({ login: loginFn });

    await userEvent.type(screen.getByLabelText('Email'), 'dev@acme.com');
    await userEvent.type(screen.getByLabelText('Password'), 'wrong');
    await userEvent.click(screen.getByRole('button', { name: /sign in$/i }));

    await waitFor(() => {
      expect(screen.getByText(/invalid credentials/i)).toBeInTheDocument();
    });

    await userEvent.click(screen.getByRole('button', { name: /sign in$/i }));

    await waitFor(() => {
      expect(screen.queryByText(/invalid credentials/i)).not.toBeInTheDocument();
    });
  });

  it('exibe o rodapé indicando self-hosted', () => {
    renderLoginPage();
    expect(screen.getByText(/self-hosted/i)).toBeInTheDocument();
  });

  it('exibe botão de toggle de visibilidade de password', () => {
    renderLoginPage();
    expect(screen.getByLabelText(/show password/i)).toBeInTheDocument();
  });

  it('exibe SSO como opção primária de autenticação', () => {
    renderLoginPage();
    expect(screen.getByRole('button', { name: /sso/i })).toBeInTheDocument();
  });
});
