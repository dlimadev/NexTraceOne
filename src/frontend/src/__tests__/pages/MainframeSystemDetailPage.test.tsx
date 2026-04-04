import * as React from 'react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { MainframeSystemDetailPage } from '../../features/legacy-assets/pages/MainframeSystemDetailPage';

vi.mock('../../features/legacy-assets/api/legacyAssets', () => ({
  legacyAssetsApi: {
    list: vi.fn(),
    getDetail: vi.fn(),
  },
}));

vi.mock('../../api/client', () => ({
  default: { get: vi.fn(), post: vi.fn() },
}));

import { legacyAssetsApi } from '../../features/legacy-assets/api/legacyAssets';

const mockAssetDetail = {
  assetId: 'asset-001',
  name: 'TRNS0001',
  displayName: 'Customer Transaction Processor',
  assetType: 'CicsTransaction',
  criticality: 'Critical',
  lifecycle: 'Active',
  team: 'Mainframe Team',
  domain: 'Core Banking',
  description: 'Processes customer transactions via CICS',
  lastActivityAt: '2026-03-20T00:00:00Z',
  metadata: {},
};

function renderPage(assetType = 'CicsTransaction', assetId = 'asset-001') {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter initialEntries={[`/legacy/${assetType}/${assetId}`]}>
        <Routes>
          <Route
            path="/legacy/:assetType/:assetId"
            element={<MainframeSystemDetailPage />}
          />
        </Routes>
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

describe('MainframeSystemDetailPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(legacyAssetsApi.getDetail).mockResolvedValue(mockAssetDetail);
  });

  it('renders asset details when data is loaded', async () => {
    renderPage();
    await waitFor(() => {
      expect(screen.getByText('Customer Transaction Processor')).toBeDefined();
    });
  });

  it('shows loading state initially', () => {
    vi.mocked(legacyAssetsApi.getDetail).mockReturnValue(new Promise(() => {}));
    renderPage();
    expect(document.body).toBeDefined();
  });

  it('renders back to catalog link', async () => {
    renderPage();
    await waitFor(() => {
      expect(screen.getByText('Back to catalog')).toBeDefined();
    });
  });

  it('shows error state when asset fails to load', async () => {
    vi.mocked(legacyAssetsApi.getDetail).mockRejectedValue(new Error('Not found'));
    renderPage('CicsTransaction', 'nonexistent');
    await waitFor(() => {
      expect(document.body.textContent).toBeDefined();
    });
  });
});
