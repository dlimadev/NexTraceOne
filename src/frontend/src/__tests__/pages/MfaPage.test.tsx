import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';

vi.mock('../../features/identity-access/api', () => ({
  identityApi: { verifyMfa: vi.fn(), resendMfaCode: vi.fn() },
}));

vi.mock('react-i18next', () => ({
  useTranslation: () => ({ t: (key: string) => key }),
}));

import { MfaPage } from '../../features/identity-access/pages/MfaPage';

describe('MfaPage', () => {
  beforeEach(() => { vi.clearAllMocks(); });

  it('renders MFA verification heading', () => {
    render(<MemoryRouter initialEntries={['/mfa?session=s1']}><MfaPage /></MemoryRouter>);
    expect(screen.getByText('mfa.title')).toBeInTheDocument();
  });

  it('renders code input field', () => {
    render(<MemoryRouter initialEntries={['/mfa?session=s1']}><MfaPage /></MemoryRouter>);
    expect(screen.getByRole('textbox')).toBeInTheDocument();
  });

  it('renders resend button', () => {
    render(<MemoryRouter initialEntries={['/mfa?session=s1']}><MfaPage /></MemoryRouter>);
    expect(screen.getByText('mfa.resend')).toBeInTheDocument();
  });

  it('renders back to login link', () => {
    render(<MemoryRouter initialEntries={['/mfa?session=s1']}><MfaPage /></MemoryRouter>);
    expect(screen.getByText('mfa.backToLogin')).toBeInTheDocument();
  });
});
