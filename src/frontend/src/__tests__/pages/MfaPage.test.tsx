import { describe, it, expect, vi, beforeEach } from 'vitest';
import { screen } from '@testing-library/react';
import { renderWithProviders } from '../test-utils';

vi.mock('../../features/identity-access/api', () => ({
  identityApi: { verifyMfa: vi.fn(), resendMfaCode: vi.fn() },
}));

import { MfaPage } from '../../features/identity-access/pages/MfaPage';

describe('MfaPage', () => {
  beforeEach(() => { vi.clearAllMocks(); });

  it('renders MFA verification heading', () => {
    renderWithProviders(<MfaPage />, { routerProps: { initialEntries: ['/mfa?session=s1'] } });
    expect(screen.getByText('Two-factor authentication')).toBeInTheDocument();
  });

  it('renders code input field', () => {
    renderWithProviders(<MfaPage />, { routerProps: { initialEntries: ['/mfa?session=s1'] } });
    expect(screen.getByRole('textbox')).toBeInTheDocument();
  });

  it('renders resend button', () => {
    renderWithProviders(<MfaPage />, { routerProps: { initialEntries: ['/mfa?session=s1'] } });
    expect(screen.getByText('Resend code')).toBeInTheDocument();
  });

  it('renders back to login link', () => {
    renderWithProviders(<MfaPage />, { routerProps: { initialEntries: ['/mfa?session=s1'] } });
    expect(screen.getByText('Back to sign in')).toBeInTheDocument();
  });
});
