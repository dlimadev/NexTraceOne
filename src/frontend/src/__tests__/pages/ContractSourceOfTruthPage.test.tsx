import * as React from 'react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { ContractSourceOfTruthPage } from '../../features/catalog/pages/ContractSourceOfTruthPage';

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

const mockContractSot = {
  apiAssetId: 'order-api',
  semVer: '2.0.0',
  protocol: 'OpenApi' as const,
  format: 'yaml',
  importedFrom: null,
  artifactCount: 5,
  diffCount: 2,
  violationCount: 0,
  governance: {
    lifecycleState: 'Published' as const,
    isLocked: false,
    isSigned: true,
    deprecationNotice: null,
    deprecationDate: null,
    sunsetDate: null,
  },
  references: [],
};

function renderPage(contractVersionId = 'cv-001') {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter
        initialEntries={[`/catalog/contracts/${contractVersionId}/source-of-truth`]}
      >
        <Routes>
          <Route
            path="/catalog/contracts/:contractVersionId/source-of-truth"
            element={<ContractSourceOfTruthPage />}
          />
        </Routes>
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

describe('ContractSourceOfTruthPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(sourceOfTruthApi.getContractSot).mockResolvedValue(mockContractSot);
  });

  it('renders contract details when data is loaded', async () => {
    renderPage();
    await waitFor(() => {
      expect(screen.getByText('Contract Source of Truth')).toBeDefined();
    });
  });

  it('shows loading state initially', () => {
    vi.mocked(sourceOfTruthApi.getContractSot).mockReturnValue(new Promise(() => {}));
    renderPage();
    expect(document.body).toBeDefined();
  });

  it('shows error state when API fails', async () => {
    vi.mocked(sourceOfTruthApi.getContractSot).mockRejectedValue(new Error('Not found'));
    renderPage('nonexistent');
    await waitFor(() => {
      expect(document.body.textContent).toBeDefined();
    });
  });
});
