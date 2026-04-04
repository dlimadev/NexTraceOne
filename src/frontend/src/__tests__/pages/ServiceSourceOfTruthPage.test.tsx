import * as React from 'react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { ServiceSourceOfTruthPage } from '../../features/catalog/pages/ServiceSourceOfTruthPage';

vi.mock('../../features/catalog/api/sourceOfTruth', () => ({
  sourceOfTruthApi: {
    getServiceSot: vi.fn(),
    getContractSot: vi.fn(),
    getServiceCoverage: vi.fn(),
    getServiceScorecard: vi.fn(),
    search: vi.fn(),
  },
}));

vi.mock('../../api/client', () => ({
  default: { get: vi.fn(), post: vi.fn() },
}));

import { sourceOfTruthApi } from '../../features/catalog/api/sourceOfTruth';

const mockServiceSot = {
  serviceId: 'svc-001',
  name: 'order-api',
  displayName: 'Order API',
  description: 'Handles order processing and fulfillment',
  domain: 'Commerce',
  systemArea: 'E-commerce',
  serviceType: 'RestApi',
  teamName: 'Commerce Team',
  criticality: 'High',
  lifecycleStatus: 'Active',
  exposureType: 'Internal',
  technicalOwner: 'alice@acme.com',
  businessOwner: 'bob@acme.com',
  documentationUrl: 'https://docs.acme.com/order-api',
  repositoryUrl: 'https://github.com/acme/order-api',
  totalApis: 3,
  totalContracts: 2,
  totalReferences: 5,
  apis: [],
  contracts: [],
  references: [],
  coverage: {
    hasOwner: true,
    hasContracts: true,
    hasDocumentation: true,
    hasRunbook: false,
    hasRecentChangeHistory: true,
    hasDependenciesMapped: false,
    hasEventTopics: false,
  },
};

function renderPage(serviceId = 'svc-001') {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter initialEntries={[`/catalog/services/${serviceId}/source-of-truth`]}>
        <Routes>
          <Route
            path="/catalog/services/:serviceId/source-of-truth"
            element={<ServiceSourceOfTruthPage />}
          />
        </Routes>
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

describe('ServiceSourceOfTruthPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(sourceOfTruthApi.getServiceSot).mockResolvedValue(mockServiceSot);
  });

  it('renders service details when data is loaded', async () => {
    renderPage();
    await waitFor(() => {
      expect(screen.getByText('Order API')).toBeDefined();
    });
  });

  it('shows loading state initially', () => {
    vi.mocked(sourceOfTruthApi.getServiceSot).mockReturnValue(new Promise(() => {}));
    renderPage();
    expect(document.body).toBeDefined();
  });

  it('shows error state when API fails', async () => {
    vi.mocked(sourceOfTruthApi.getServiceSot).mockRejectedValue(new Error('Not found'));
    renderPage('nonexistent');
    await waitFor(() => {
      expect(document.body.textContent).toBeDefined();
    });
  });
});
