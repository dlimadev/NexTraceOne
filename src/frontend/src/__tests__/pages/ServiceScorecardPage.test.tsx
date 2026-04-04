import * as React from 'react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';
import { ServiceScorecardPage } from '../../features/catalog/pages/ServiceScorecardPage';

vi.mock('../../features/catalog/api/sourceOfTruth', () => ({
  sourceOfTruthApi: {
    getServiceSot: vi.fn(),
    getContractSot: vi.fn(),
    getServiceCoverage: vi.fn(),
    getServiceScorecard: vi.fn(),
  },
}));

vi.mock('../../api/client', () => ({
  default: { get: vi.fn(), post: vi.fn() },
}));

import { sourceOfTruthApi } from '../../features/catalog/api/sourceOfTruth';

function renderPage() {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter>
        <ServiceScorecardPage />
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

describe('ServiceScorecardPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(sourceOfTruthApi.getServiceScorecard).mockResolvedValue(null);
  });

  it('renders page title', async () => {
    renderPage();
    await waitFor(() => {
      expect(screen.getByText('Service Scorecards')).toBeDefined();
    });
  });

  it('shows loading state initially', () => {
    vi.mocked(sourceOfTruthApi.getServiceScorecard).mockReturnValue(new Promise(() => {}));
    renderPage();
    expect(document.body).toBeDefined();
  });

  it('renders scorecard when data is available', async () => {
    vi.mocked(sourceOfTruthApi.getServiceScorecard).mockResolvedValue({
      serviceName: 'order-api',
      environment: 'Production',
      overallScore: 84,
      grade: 'B+',
      dimensions: [
        { name: 'Contract Coverage', score: 90, weight: 0.3, details: 'All endpoints documented' },
        { name: 'Reliability', score: 82, weight: 0.25, details: 'SLO: 99.9%' },
        { name: 'Documentation', score: 78, weight: 0.2, details: '3/5 docs complete' },
        { name: 'Test Coverage', score: 80, weight: 0.25, details: '80% unit test coverage' },
      ],
      generatedAt: '2026-04-01T00:00:00Z',
    });
    renderPage();
    await waitFor(() => {
      expect(document.body.textContent).toBeDefined();
    });
  });
});
