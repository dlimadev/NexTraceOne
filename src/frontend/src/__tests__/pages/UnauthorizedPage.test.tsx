import { describe, it, expect, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';

vi.mock('react-i18next', () => ({
  useTranslation: () => ({ t: (key: string) => key }),
}));

import { UnauthorizedPage } from '../../features/identity-access/pages/UnauthorizedPage';

describe('UnauthorizedPage', () => {
  it('renders access denied heading', () => {
    render(<MemoryRouter><UnauthorizedPage /></MemoryRouter>);
    expect(screen.getByText('authorization.accessDenied')).toBeInTheDocument();
  });

  it('renders description text', () => {
    render(<MemoryRouter><UnauthorizedPage /></MemoryRouter>);
    expect(screen.getByText('authorization.noPermission')).toBeInTheDocument();
  });

  it('renders go to dashboard button', () => {
    render(<MemoryRouter><UnauthorizedPage /></MemoryRouter>);
    expect(screen.getByRole('button', { name: /authorization.goToDashboard/i })).toBeInTheDocument();
  });
});
