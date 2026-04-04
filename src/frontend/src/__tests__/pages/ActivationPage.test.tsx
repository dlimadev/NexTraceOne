import { describe, it, expect, vi, beforeEach } from 'vitest';
import { screen } from '@testing-library/react';
import { renderWithProviders } from '../test-utils';

vi.mock('../../features/identity-access/api', () => ({
  identityApi: { activateAccount: vi.fn() },
}));

import { ActivationPage } from '../../features/identity-access/pages/ActivationPage';

describe('ActivationPage', () => {
  beforeEach(() => { vi.clearAllMocks(); });

  it('renders invalid token state when no token in URL', () => {
    renderWithProviders(<ActivationPage />, { routerProps: { initialEntries: ['/activate'] } });
    expect(screen.getByText(/activation link is invalid/i)).toBeInTheDocument();
  });

  it('renders activation form when token is present', () => {
    renderWithProviders(<ActivationPage />, { routerProps: { initialEntries: ['/activate?token=valid'] } });
    expect(screen.getByText('Activate your account')).toBeInTheDocument();
  });

  it('renders password input fields with valid token', () => {
    renderWithProviders(<ActivationPage />, { routerProps: { initialEntries: ['/activate?token=valid'] } });
    const inputs = screen.getAllByLabelText(/password/i);
    expect(inputs.length).toBeGreaterThanOrEqual(1);
  });
});
