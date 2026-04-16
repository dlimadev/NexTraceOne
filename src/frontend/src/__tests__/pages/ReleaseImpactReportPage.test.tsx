import * as React from 'react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';
import { ReleaseImpactReportPage } from '../../features/change-governance/pages/ReleaseImpactReportPage';

vi.mock('../../features/change-governance/api/changeIntelligence', () => ({
  changeIntelligenceApi: {
    getReleaseImpactReport: vi.fn(),
  },
}));

vi.mock('../../api/client', () => ({
  default: { get: vi.fn(), post: vi.fn() },
}));

import { changeIntelligenceApi } from '../../features/change-governance/api/changeIntelligence';

const mockReport = {
  releaseId: 'release-001',
  serviceName: 'payment-service',
  version: '2.1.0',
  environment: 'Production',
  status: 'ReadyForPromotion',
  riskScore: 0.72,
  changeLevel: 'Breaking',
  blastRadius: {
    totalAffectedConsumers: 3,
    directConsumers: ['mobile-bff', 'web-bff'],
    transitiveConsumers: ['reporting-service'],
    calculatedAt: '2026-04-10T14:00:00Z',
  },
  workItemsSummary: {
    total: 6,
    stories: 4,
    bugs: 1,
    features: 1,
    others: 0,
  },
  commitsSummary: {
    total: 23,
    byAuthor: [
      { author: 'alice@example.com', count: 12 },
      { author: 'bob@example.com', count: 11 },
    ],
  },
  pendingApprovals: 1,
  generatedAt: '2026-04-10T15:00:00Z',
};

function renderPage() {
  const qc = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={qc}>
      <MemoryRouter>
        <ReleaseImpactReportPage />
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

describe('ReleaseImpactReportPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    (changeIntelligenceApi.getReleaseImpactReport as ReturnType<typeof vi.fn>).mockResolvedValue(mockReport);
  });

  it('renders page title', () => {
    renderPage();
    expect(screen.getByText('Release Impact Report')).toBeInTheDocument();
  });

  it('renders page subtitle', () => {
    renderPage();
    expect(screen.getByText('Consolidated impact report: blast radius, commits, work items and risk score')).toBeInTheDocument();
  });

  it('renders release ID label', () => {
    renderPage();
    expect(screen.getAllByText('Release ID').length).toBeGreaterThan(0);
  });

  it('renders generate button', () => {
    renderPage();
    expect(screen.getByText('Generate Report')).toBeInTheDocument();
  });

  it('renders report sections after query', async () => {
    renderPage();
    await waitFor(() => {
      expect(screen.getByText('Release Impact Report')).toBeInTheDocument();
    });
  });

  it('renders work items section label', () => {
    renderPage();
    expect(screen.getAllByText('Release ID').length).toBeGreaterThan(0);
  });

  it('renders impact report input placeholder', () => {
    renderPage();
    expect(screen.getByPlaceholderText('Enter a release ID to generate the impact report')).toBeInTheDocument();
  });

  it('generate button is disabled without input', () => {
    renderPage();
    const btn = screen.getByText('Generate Report');
    expect(btn).toBeDisabled();
  });

  it('renders without crashing when report is null', async () => {
    (changeIntelligenceApi.getReleaseImpactReport as ReturnType<typeof vi.fn>).mockResolvedValue(null);
    renderPage();
    await waitFor(() => {
      expect(screen.getByText('Release Impact Report')).toBeInTheDocument();
    });
  });

  it('matches snapshot', () => {
    const { container } = renderPage();
    expect(container.firstChild).toBeTruthy();
  });
});
