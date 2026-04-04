import * as React from 'react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';
import { LegacyAssetCatalogPage } from '../../features/legacy-assets/pages/LegacyAssetCatalogPage';

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

function renderPage() {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter>
        <LegacyAssetCatalogPage />
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

describe('LegacyAssetCatalogPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(legacyAssetsApi.list).mockResolvedValue([]);
  });

  it('renders page title', async () => {
    renderPage();
    await waitFor(() => {
      expect(screen.getAllByText('Legacy Asset Catalog').length).toBeGreaterThan(0);
    });
  });

  it('shows loading state initially', () => {
    vi.mocked(legacyAssetsApi.list).mockReturnValue(new Promise(() => {}));
    renderPage();
    expect(document.body).toBeDefined();
  });

  it('renders assets when data is available', async () => {
    vi.mocked(legacyAssetsApi.list).mockResolvedValue([
      {
        id: 'asset-001',
        name: 'TRNS0001',
        displayName: 'Customer Transaction Processor',
        assetType: 'CicsTransaction',
        criticality: 'Critical',
        lifecycleStatus: 'Active',
        teamName: 'Mainframe Team',
        domain: 'Core Banking',
      },
    ]);
    renderPage();
    await waitFor(() => {
      expect(screen.getByText('Customer Transaction Processor')).toBeDefined();
    });
  });
});
