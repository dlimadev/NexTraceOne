import { describe, it, expect, vi, beforeEach } from 'vitest';
import { screen } from '@testing-library/react';
import { renderWithProviders } from '../test-utils';

vi.mock('../../features/identity-access/api', () => ({
  identityApi: {
    resetPassword: vi.fn(),
  },
}));

import { ResetPasswordPage } from '../../features/identity-access/pages/ResetPasswordPage';

describe('ResetPasswordPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('renders invalid token state when no token in URL', () => {
    renderWithProviders(<ResetPasswordPage />, {
      routerProps: { initialEntries: ['/reset-password'] },
    });
    expect(screen.getByText(/reset link is invalid/i)).toBeInTheDocument();
  });

  it('renders the reset password form when token is present', () => {
    renderWithProviders(<ResetPasswordPage />, {
      routerProps: { initialEntries: ['/reset-password?token=valid-token'] },
    });
    expect(screen.getByText('Reset your password')).toBeInTheDocument();
  });

  it('renders password input fields with valid token', () => {
    renderWithProviders(<ResetPasswordPage />, {
      routerProps: { initialEntries: ['/reset-password?token=valid-token'] },
    });
    const passwordInputs = screen.getAllByLabelText(/password/i);
    expect(passwordInputs.length).toBeGreaterThanOrEqual(1);
  });

  it('renders request new link button for invalid token', () => {
    renderWithProviders(<ResetPasswordPage />, {
      routerProps: { initialEntries: ['/reset-password'] },
    });
    expect(screen.getByText('Request new link')).toBeInTheDocument();
  });
});
