import { describe, it, expect, vi } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter } from 'react-router-dom';
import { LoginPage } from '../../pages/LoginPage';
import { AuthContext } from '../../contexts/AuthContext';

// Helper para criar um valor de contexto de autenticação fake
function makeAuthContext(overrides: Partial<{
  isAuthenticated: boolean;
  login: (email: string, password: string, tenantId: string) => Promise<void>;
}> = {}) {
  return {
    isAuthenticated: false,
    accessToken: null,
    user: null,
    tenantId: null,
    login: vi.fn().mockResolvedValue(undefined),
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
    expect(screen.getByLabelText(/tenant id/i)).toBeInTheDocument();
    expect(screen.getByLabelText(/email/i)).toBeInTheDocument();
    expect(screen.getByLabelText(/password/i)).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /sign in/i })).toBeInTheDocument();
  });

  it('exibe o título e subtítulo da plataforma', () => {
    renderLoginPage();
    expect(screen.getByText('NexTraceOne')).toBeInTheDocument();
    expect(screen.getByText(/sovereign change intelligence platform/i)).toBeInTheDocument();
  });

  it('chama login com as credenciais corretas ao submeter', async () => {
    const { auth } = renderLoginPage();

    await userEvent.type(screen.getByLabelText(/tenant id/i), 'tenant-001');
    await userEvent.type(screen.getByLabelText(/email/i), 'dev@acme.com');
    await userEvent.type(screen.getByLabelText(/password/i), 'secret123');
    await userEvent.click(screen.getByRole('button', { name: /sign in/i }));

    await waitFor(() => {
      expect(auth.login).toHaveBeenCalledWith('dev@acme.com', 'secret123', 'tenant-001');
    });
  });

  it('exibe mensagem de erro quando o login falha', async () => {
    const loginFn = vi.fn().mockRejectedValue(new Error('Unauthorized'));
    renderLoginPage({ login: loginFn });

    await userEvent.type(screen.getByLabelText(/tenant id/i), 'tenant-001');
    await userEvent.type(screen.getByLabelText(/email/i), 'wrong@acme.com');
    await userEvent.type(screen.getByLabelText(/password/i), 'wrong');
    await userEvent.click(screen.getByRole('button', { name: /sign in/i }));

    await waitFor(() => {
      expect(screen.getByText(/invalid credentials or tenant/i)).toBeInTheDocument();
    });
  });

  it('desabilita o botão de submit enquanto o login está em progresso', async () => {
    let resolveLogin!: () => void;
    const loginFn = vi.fn().mockImplementation(
      () => new Promise<void>((res) => { resolveLogin = res; })
    );
    renderLoginPage({ login: loginFn });

    await userEvent.type(screen.getByLabelText(/tenant id/i), 'tenant-001');
    await userEvent.type(screen.getByLabelText(/email/i), 'dev@acme.com');
    await userEvent.type(screen.getByLabelText(/password/i), 'pass');
    await userEvent.click(screen.getByRole('button', { name: /sign in/i }));

    expect(screen.getByRole('button')).toBeDisabled();
    resolveLogin();
  });

  it('limpa a mensagem de erro ao submeter novamente', async () => {
    const loginFn = vi.fn()
      .mockRejectedValueOnce(new Error('Unauthorized'))
      .mockResolvedValueOnce(undefined);
    renderLoginPage({ login: loginFn });

    await userEvent.type(screen.getByLabelText(/tenant id/i), 'tenant-001');
    await userEvent.type(screen.getByLabelText(/email/i), 'dev@acme.com');
    await userEvent.type(screen.getByLabelText(/password/i), 'wrong');
    await userEvent.click(screen.getByRole('button', { name: /sign in/i }));

    await waitFor(() => {
      expect(screen.getByText(/invalid credentials/i)).toBeInTheDocument();
    });

    await userEvent.click(screen.getByRole('button', { name: /sign in/i }));

    await waitFor(() => {
      expect(screen.queryByText(/invalid credentials/i)).not.toBeInTheDocument();
    });
  });

  it('exibe o rodapé indicando self-hosted', () => {
    renderLoginPage();
    expect(screen.getByText(/self-hosted/i)).toBeInTheDocument();
  });
});
