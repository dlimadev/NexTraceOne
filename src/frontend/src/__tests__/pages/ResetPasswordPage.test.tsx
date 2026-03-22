import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';

vi.mock('../../features/identity-access/api', () => ({
  identityApi: {
    resetPassword: vi.fn(),
  },
}));

vi.mock('react-i18next', () => ({
  useTranslation: () => ({ t: (key: string) => key }),
}));

import { ResetPasswordPage } from '../../features/identity-access/pages/ResetPasswordPage';

describe('ResetPasswordPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('renders invalid token state when no token in URL', () => {
    render(
      <MemoryRouter initialEntries={['/reset-password']}>
        <ResetPasswordPage />
      </MemoryRouter>
    );
    expect(screen.getByText('resetPassword.invalidToken')).toBeInTheDocument();
  });

  it('renders the reset password form when token is present', () => {
    render(
      <MemoryRouter initialEntries={['/reset-password?token=valid-token']}>
        <ResetPasswordPage />
      </MemoryRouter>
    );
    expect(screen.getByText('resetPassword.title')).toBeInTheDocument();
  });

  it('renders password input fields with valid token', () => {
    render(
      <MemoryRouter initialEntries={['/reset-password?token=valid-token']}>
        <ResetPasswordPage />
      </MemoryRouter>
    );
    const passwordInputs = screen.getAllByLabelText(/password/i);
    expect(passwordInputs.length).toBeGreaterThanOrEqual(1);
  });

  it('renders request new link button for invalid token', () => {
    render(
      <MemoryRouter initialEntries={['/reset-password']}>
        <ResetPasswordPage />
      </MemoryRouter>
    );
    expect(screen.getByText('resetPassword.requestNewLink')).toBeInTheDocument();
  });
});
