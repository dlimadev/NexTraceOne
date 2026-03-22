import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';

vi.mock('../../features/identity-access/api', () => ({
  identityApi: { activateAccount: vi.fn() },
}));

vi.mock('react-i18next', () => ({
  useTranslation: () => ({ t: (key: string) => key }),
}));

import { ActivationPage } from '../../features/identity-access/pages/ActivationPage';

describe('ActivationPage', () => {
  beforeEach(() => { vi.clearAllMocks(); });

  it('renders invalid token state when no token in URL', () => {
    render(<MemoryRouter initialEntries={['/activate']}><ActivationPage /></MemoryRouter>);
    expect(screen.getByText('activation.invalidToken')).toBeInTheDocument();
  });

  it('renders activation form when token is present', () => {
    render(<MemoryRouter initialEntries={['/activate?token=valid']}><ActivationPage /></MemoryRouter>);
    expect(screen.getByText('activation.title')).toBeInTheDocument();
  });

  it('renders password input fields with valid token', () => {
    render(<MemoryRouter initialEntries={['/activate?token=valid']}><ActivationPage /></MemoryRouter>);
    const inputs = screen.getAllByLabelText(/password/i);
    expect(inputs.length).toBeGreaterThanOrEqual(1);
  });
});
