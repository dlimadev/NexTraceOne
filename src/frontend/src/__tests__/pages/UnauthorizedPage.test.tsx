import { describe, it, expect, vi } from 'vitest';
import { screen } from '@testing-library/react';
import { renderWithProviders } from '../test-utils';

import { UnauthorizedPage } from '../../features/identity-access/pages/UnauthorizedPage';

describe('UnauthorizedPage', () => {
  it('renders access denied heading', () => {
    renderWithProviders(<UnauthorizedPage />);
    expect(screen.getByText('Access Denied')).toBeInTheDocument();
  });

  it('renders description text', () => {
    renderWithProviders(<UnauthorizedPage />);
    expect(screen.getByText('You do not have permission to access this page. Contact your administrator to request access.')).toBeInTheDocument();
  });

  it('renders go to dashboard button', () => {
    renderWithProviders(<UnauthorizedPage />);
    expect(screen.getByRole('button', { name: /go to dashboard/i })).toBeInTheDocument();
  });
});
