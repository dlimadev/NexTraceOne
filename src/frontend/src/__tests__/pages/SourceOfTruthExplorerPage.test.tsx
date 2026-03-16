import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { SourceOfTruthExplorerPage } from '../../features/catalog/pages/SourceOfTruthExplorerPage';

vi.mock('../../features/catalog/api/sourceOfTruth', () => ({
  sourceOfTruthApi: {
    search: vi.fn(),
  },
}));

vi.mock('../../api/client', () => ({
  default: { get: vi.fn(), post: vi.fn(), patch: vi.fn() },
}));

import { sourceOfTruthApi } from '../../features/catalog/api/sourceOfTruth';

const mockSearchResults = {
  totalResults: 3,
  services: [
    {
      serviceId: 'svc-1',
      name: 'order-service',
      displayName: 'Order Service',
      domain: 'Orders',
      teamName: 'order-squad',
      criticality: 'High',
      lifecycleStatus: 'Active',
    },
  ],
  contracts: [
    {
      versionId: 'cv-1',
      apiAssetId: 'order-api',
      semVer: '2.1.0',
      protocol: 'OpenApi',
      lifecycleState: 'Approved',
    },
  ],
  references: [
    {
      referenceId: 'ref-1',
      title: 'Order API Runbook',
      description: 'Operational runbook for the Order API',
      assetType: 'Service',
      referenceType: 'Runbook',
      url: 'https://docs.example.com/order-runbook',
    },
  ],
};

function renderPage() {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false } },
  });
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter initialEntries={['/source-of-truth']}>
        <Routes>
          <Route path="/source-of-truth" element={<SourceOfTruthExplorerPage />} />
        </Routes>
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

describe('SourceOfTruthExplorerPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('renders page title', () => {
    renderPage();
    expect(screen.getAllByText(/source of truth/i).length).toBeGreaterThanOrEqual(1);
  });

  it('shows empty state before search', () => {
    renderPage();
    expect(screen.getByRole('textbox')).toBeInTheDocument();
  });

  it('shows search results after typing', async () => {
    vi.mocked(sourceOfTruthApi.search).mockResolvedValue(mockSearchResults);
    const user = userEvent.setup();
    renderPage();
    const input = screen.getByRole('textbox');
    await user.type(input, 'order');
    await waitFor(
      () => {
        expect(screen.getByText('Order Service')).toBeInTheDocument();
      },
      { timeout: 2000 },
    );
    expect(screen.getByText('order-api')).toBeInTheDocument();
    expect(screen.getByText('Order API Runbook')).toBeInTheDocument();
  });

  it('shows scope filter buttons', () => {
    renderPage();
    expect(screen.getByRole('textbox')).toBeInTheDocument();
  });

  it('shows error state on API failure', async () => {
    vi.mocked(sourceOfTruthApi.search).mockRejectedValue(new Error('Network error'));
    const user = userEvent.setup();
    renderPage();
    const input = screen.getByRole('textbox');
    await user.type(input, 'failing-query');
    await waitFor(
      () => {
        expect(screen.getByText(/error/i)).toBeInTheDocument();
      },
      { timeout: 2000 },
    );
  });
});
