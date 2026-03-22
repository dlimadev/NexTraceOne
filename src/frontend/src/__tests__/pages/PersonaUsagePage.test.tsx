import * as React from 'react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';
import { PersonaUsagePage } from '../../features/product-analytics/pages/PersonaUsagePage';

vi.mock('../../features/product-analytics/api/productAnalyticsApi', () => ({
  productAnalyticsApi: { getPersonaUsage: vi.fn() },
}));
vi.mock('../../api/client', () => ({ default: { get: vi.fn(), post: vi.fn() } }));

import { productAnalyticsApi } from '../../features/product-analytics/api/productAnalyticsApi';

const mockResponse = {
  profiles: [
    { persona: 'Engineer', activeUsers: 98, totalActions: 4520, topModules: [{ module: 'SourceOfTruth', adoptionPercent: 92, actionCount: 1240 }], topActions: [], adoptionDepth: 85.2, commonFrictionPoints: ['zero_result_search'], milestonesReached: ['FirstSearchSuccess'] },
    { persona: 'Architect', activeUsers: 45, totalActions: 1620, topModules: [{ module: 'Governance', adoptionPercent: 62, actionCount: 220 }], topActions: [], adoptionDepth: 72.1, commonFrictionPoints: [], milestonesReached: [] },
  ],
  totalPersonas: 2, mostActivePersona: 'Engineer', deepestAdoptionPersona: 'Engineer', periodLabel: 'last_30d',
};

function renderPage() {
  const qc = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(<QueryClientProvider client={qc}><MemoryRouter><PersonaUsagePage /></MemoryRouter></QueryClientProvider>);
}

describe('PersonaUsagePage', () => {
  beforeEach(() => vi.clearAllMocks());

  it('shows loading state initially', () => {
    vi.mocked(productAnalyticsApi.getPersonaUsage).mockReturnValue(new Promise(() => {}));
    renderPage();
    expect(screen.getByText(/loading/i)).toBeInTheDocument();
  });

  it('renders persona profiles from API', async () => {
    vi.mocked(productAnalyticsApi.getPersonaUsage).mockResolvedValue(mockResponse);
    renderPage();
    await waitFor(() => expect(screen.getByText('SourceOfTruth')).toBeInTheDocument());
    expect(screen.getByText('Governance')).toBeInTheDocument();
  });

  it('shows error state on API failure', async () => {
    vi.mocked(productAnalyticsApi.getPersonaUsage).mockRejectedValue(new Error('fail'));
    renderPage();
    await waitFor(() => expect(screen.getByRole('button', { name: /retry/i })).toBeInTheDocument());
  });

  it('shows empty state when no data returned', async () => {
    vi.mocked(productAnalyticsApi.getPersonaUsage).mockResolvedValue({ profiles: [], totalPersonas: 0, mostActivePersona: '', deepestAdoptionPersona: '', periodLabel: 'last_30d' });
    renderPage();
    await waitFor(() => expect(screen.queryByText('SourceOfTruth')).not.toBeInTheDocument());
  });

  it('calls getPersonaUsage API', async () => {
    vi.mocked(productAnalyticsApi.getPersonaUsage).mockResolvedValue(mockResponse);
    renderPage();
    await waitFor(() => expect(productAnalyticsApi.getPersonaUsage).toHaveBeenCalledWith({ range: 'last_30d' }));
  });
});
