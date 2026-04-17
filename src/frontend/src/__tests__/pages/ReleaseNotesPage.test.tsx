import * as React from 'react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';
import { ReleaseNotesPage } from '../../features/change-governance/pages/ReleaseNotesPage';

vi.mock('../../features/change-governance/api/changeIntelligence', () => ({
  changeIntelligenceApi: {
    getReleaseNotes: vi.fn(),
    generateReleaseNotes: vi.fn(),
    regenerateReleaseNotes: vi.fn(),
    listRecentReleases: vi.fn().mockResolvedValue({ items: [], totalCount: 0, page: 1, pageSize: 50 }),
  },
}));

vi.mock('../../api/client', () => ({
  default: { get: vi.fn(), post: vi.fn() },
}));

vi.mock('../../features/change-governance/components/ReleaseSelector', () => ({
  ReleaseSelector: ({ onChange, placeholder }: { value: string; onChange: (id: string) => void; placeholder?: string }) => (
    <select
      data-testid="release-selector"
      defaultValue=""
      onChange={(e) => onChange(e.target.value)}
      aria-label={placeholder ?? 'Select a release'}
    >
      <option value="">{placeholder ?? 'Select a release'}</option>
      <option value="release-abc">release-abc — payment-service v1.2.0</option>
      <option value="release-new">release-new — new-service v1.0.0</option>
    </select>
  ),
}));

import { changeIntelligenceApi } from '../../features/change-governance/api/changeIntelligence';

const mockNotes = {
  releaseNotesId: 'notes-001',
  releaseId: 'release-abc',
  technicalSummary: 'Added new /payments/v2 endpoint with idempotency support. Deprecated legacy /payments/v1.',
  executiveSummary: 'Payment service upgrade enables faster checkout with 40% latency improvement.',
  newEndpointsSection: 'GET /payments/v2/status\nPOST /payments/v2/initiate',
  breakingChangesSection: 'Field `legacy_id` removed from response body.',
  affectedServicesSection: 'checkout-service, billing-service, reconciliation-worker',
  confidenceMetricsSection: 'Change Score: 72 | Blast Radius: 3 direct consumers',
  evidenceLinksSection: 'https://jira.example.com/PROJ-1234',
  modelUsed: 'gpt-4o',
  tokensUsed: 1847,
  status: 'Generated',
  generatedAt: '2026-04-12T10:00:00Z',
  lastRegeneratedAt: null,
  regenerationCount: 0,
};

function renderPage() {
  const qc = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={qc}>
      <MemoryRouter>
        <ReleaseNotesPage />
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

async function selectRelease(value: string) {
  const selector = screen.getByTestId('release-selector');
  await userEvent.selectOptions(selector, value);
}

vi.mock('../../contexts/EnvironmentContext', () => ({
  useEnvironment: vi.fn().mockReturnValue({
    activeEnvironmentId: 'env-prod-001',
    activeEnvironment: { id: 'env-prod-001', name: 'Production', profile: 'production', isProductionLike: true },
    availableEnvironments: [{ id: 'env-prod-001', name: 'Production', profile: 'production', isProductionLike: true }],
    isLoadingEnvironments: false,
    selectEnvironment: vi.fn(),
    clearEnvironment: vi.fn(),
  }),
}));

describe('ReleaseNotesPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(changeIntelligenceApi.getReleaseNotes).mockResolvedValue(mockNotes);
    vi.mocked(changeIntelligenceApi.generateReleaseNotes).mockResolvedValue({ releaseNotesId: 'notes-001' });
    vi.mocked(changeIntelligenceApi.regenerateReleaseNotes).mockResolvedValue({});
  });

  it('renders page title', () => {
    renderPage();
    expect(screen.getByText('Release Notes')).toBeTruthy();
  });

  it('renders release selector', () => {
    renderPage();
    expect(screen.getByTestId('release-selector')).toBeTruthy();
  });

  it('shows empty state before selecting a release', () => {
    renderPage();
    expect(screen.getByText(/No release selected/i)).toBeTruthy();
  });

  it('loads and shows notes after selecting a release', async () => {
    renderPage();
    await selectRelease('release-abc');
    await waitFor(() => {
      expect(screen.getAllByText(/Generated/i).length).toBeGreaterThan(0);
    });
  });

  it('shows model and token info', async () => {
    renderPage();
    await selectRelease('release-abc');
    await waitFor(() => {
      expect(screen.getByText('gpt-4o')).toBeTruthy();
    });
    expect(screen.getByText('1,847')).toBeTruthy();
  });

  it('shows technical summary section', async () => {
    renderPage();
    await selectRelease('release-abc');
    await waitFor(() => {
      expect(screen.getAllByText(/Technical Summary/i).length).toBeGreaterThan(0);
    });
    expect(screen.getByText(/Added new \/payments\/v2/i)).toBeTruthy();
  });

  it('shows breaking changes section', async () => {
    renderPage();
    await selectRelease('release-abc');
    await waitFor(() => {
      expect(screen.getAllByText(/Breaking Changes/i).length).toBeGreaterThan(0);
    });
  });

  it('shows regenerate button', async () => {
    renderPage();
    await selectRelease('release-abc');
    await waitFor(() => {
      expect(screen.getByText('Regenerate')).toBeTruthy();
    });
  });

  it('shows generate button when notes not found', async () => {
    vi.mocked(changeIntelligenceApi.getReleaseNotes).mockRejectedValue(new Error('404'));
    renderPage();
    await selectRelease('release-new');
    await waitFor(() => {
      expect(screen.getByText(/Generate with AI/i)).toBeTruthy();
    });
  });

  it('calls generateReleaseNotes on Generate click', async () => {
    vi.mocked(changeIntelligenceApi.getReleaseNotes).mockRejectedValue(new Error('404'));
    renderPage();
    await selectRelease('release-new');
    await waitFor(() => {
      expect(screen.getByText(/Generate with AI/i)).toBeTruthy();
    });
    await userEvent.click(screen.getByText(/Generate with AI/i));
    await waitFor(() => {
      expect(changeIntelligenceApi.generateReleaseNotes).toHaveBeenCalledWith('release-new', 'Technical');
    });
  });
});
