import * as React from 'react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';

vi.mock('../../features/identity-access/api', () => ({
  identityApi: {
    listEnvironments: vi.fn(),
    createEnvironment: vi.fn(),
    updateEnvironment: vi.fn(),
    setPrimaryProductionEnvironment: vi.fn(),
  },
}));

vi.mock('../../api/client', () => ({
  default: { get: vi.fn(), post: vi.fn() },
}));

vi.mock('react-i18next', () => ({
  useTranslation: () => ({ t: (key: string) => key }),
}));

import { identityApi } from '../../features/identity-access/api';
import { EnvironmentsPage } from '../../features/identity-access/pages/EnvironmentsPage';

const mockEnvironments = [
  {
    environmentId: 'env-1',
    name: 'Production',
    slug: 'production',
    sortOrder: 1,
    profile: 'Production',
    criticality: 'Critical',
    code: 'PRD',
    description: 'Production environment',
    region: 'eu-west-1',
    isPrimaryProduction: true,
    isActive: true,
  },
  {
    environmentId: 'env-2',
    name: 'Staging',
    slug: 'staging',
    sortOrder: 2,
    profile: 'Staging',
    criticality: 'Medium',
    code: 'STG',
    description: 'Staging environment',
    region: 'eu-west-1',
    isPrimaryProduction: false,
    isActive: true,
  },
];

function renderPage() {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter><EnvironmentsPage /></MemoryRouter>
    </QueryClientProvider>,
  );
}

describe('EnvironmentsPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(identityApi.listEnvironments).mockResolvedValue(mockEnvironments);
  });

  it('renders page heading', async () => {
    renderPage();
    await waitFor(() => {
      expect(screen.getByText('environments.title')).toBeInTheDocument();
    });
  });

  it('renders data after loading', async () => {
    renderPage();
    await waitFor(() => {
      expect(screen.getByText('Production')).toBeInTheDocument();
    });
  });

  it('shows loading state while fetching', () => {
    vi.mocked(identityApi.listEnvironments).mockReturnValue(new Promise(() => {}));
    renderPage();
    expect(screen.queryByText('Production')).not.toBeInTheDocument();
  });

  it('does not render DemoBanner', async () => {
    renderPage();
    await waitFor(() => screen.getByText('Production'));
    expect(screen.queryByTestId('demo-banner')).not.toBeInTheDocument();
  });

  it('calls identityApi.listEnvironments on mount', async () => {
    renderPage();
    await waitFor(() => expect(identityApi.listEnvironments).toHaveBeenCalled());
  });
});
