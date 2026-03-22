import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter } from 'react-router-dom';

vi.mock('../../features/identity-access/api', () => ({
  identityApi: {
    forgotPassword: vi.fn(),
  },
}));

vi.mock('react-i18next', () => ({
  useTranslation: () => ({ t: (key: string) => key }),
}));

import { ForgotPasswordPage } from '../../features/identity-access/pages/ForgotPasswordPage';
import { identityApi } from '../../features/identity-access/api';

function renderPage() {
  return render(
    <MemoryRouter>
      <ForgotPasswordPage />
    </MemoryRouter>
  );
}

describe('ForgotPasswordPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('renders the forgot password form', () => {
    renderPage();
    expect(screen.getByText('forgotPassword.title')).toBeInTheDocument();
  });

  it('renders email input field', () => {
    renderPage();
    expect(screen.getByRole('textbox')).toBeInTheDocument();
  });

  it('renders submit button', () => {
    renderPage();
    expect(screen.getByRole('button', { name: /forgotPassword.submit/i })).toBeInTheDocument();
  });

  it('shows success message after successful submission', async () => {
    vi.mocked(identityApi.forgotPassword).mockResolvedValue(undefined);
    renderPage();

    await userEvent.type(screen.getByRole('textbox'), 'user@org.com');
    await userEvent.click(screen.getByRole('button', { name: /forgotPassword.submit/i }));

    await waitFor(() => {
      expect(screen.getByText('forgotPassword.successMessage')).toBeInTheDocument();
    });
  });

  it('renders link to go back to login', () => {
    renderPage();
    expect(screen.getByText('forgotPassword.backToLogin')).toBeInTheDocument();
  });
});
