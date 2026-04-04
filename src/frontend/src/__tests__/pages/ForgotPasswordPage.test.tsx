import { describe, it, expect, vi, beforeEach } from 'vitest';
import { screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { renderWithProviders } from '../test-utils';

vi.mock('../../features/identity-access/api', () => ({
  identityApi: {
    forgotPassword: vi.fn(),
  },
}));

import { ForgotPasswordPage } from '../../features/identity-access/pages/ForgotPasswordPage';
import { identityApi } from '../../features/identity-access/api';

function renderPage() {
  return renderWithProviders(<ForgotPasswordPage />);
}

describe('ForgotPasswordPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('renders the forgot password form', () => {
    renderPage();
    expect(screen.getByText('Forgot your password?')).toBeInTheDocument();
  });

  it('renders email input field', () => {
    renderPage();
    expect(screen.getByRole('textbox')).toBeInTheDocument();
  });

  it('renders submit button', () => {
    renderPage();
    expect(screen.getByRole('button', { name: /send reset link/i })).toBeInTheDocument();
  });

  it('shows success message after successful submission', async () => {
    vi.mocked(identityApi.forgotPassword).mockResolvedValue(undefined);
    renderPage();

    await userEvent.type(screen.getByRole('textbox'), 'user@org.com');
    await userEvent.click(screen.getByRole('button', { name: /send reset link/i }));

    await waitFor(() => {
      expect(screen.getByText(/you will receive a password reset link/i)).toBeInTheDocument();
    });
  });

  it('renders link to go back to login', () => {
    renderPage();
    expect(screen.getByText('Back to sign in')).toBeInTheDocument();
  });
});
