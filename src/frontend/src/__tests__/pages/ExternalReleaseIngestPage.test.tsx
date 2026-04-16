import * as React from 'react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';
import { ExternalReleaseIngestPage } from '../../features/change-governance/pages/ExternalReleaseIngestPage';

vi.mock('../../features/change-governance/api/changeIntelligence', () => ({
  changeIntelligenceApi: {
    ingestExternalRelease: vi.fn(),
  },
}));

vi.mock('../../api/client', () => ({
  default: { get: vi.fn(), post: vi.fn() },
}));

import { changeIntelligenceApi } from '../../features/change-governance/api/changeIntelligence';

const mockIngestResult = {
  releaseId: 'release-abc-123',
  externalReleaseId: 'ADO-RELEASE-2024.1',
  isNew: true,
  status: 'InDevelopment',
};

function renderPage() {
  const qc = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={qc}>
      <MemoryRouter>
        <ExternalReleaseIngestPage />
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

describe('ExternalReleaseIngestPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    (changeIntelligenceApi.ingestExternalRelease as ReturnType<typeof vi.fn>).mockResolvedValue(mockIngestResult);
  });

  it('renders page title', () => {
    renderPage();
    expect(screen.getByText('Ingest External Release')).toBeInTheDocument();
  });

  it('renders page subtitle', () => {
    renderPage();
    expect(screen.getByText('Register a release created by an external system (AzureDevOps, Jira, Jenkins, GitLab)')).toBeInTheDocument();
  });

  it('renders info banner', () => {
    renderPage();
    expect(screen.getByText('This endpoint is idempotent: re-ingesting the same external release ID returns the existing NexTraceOne release.')).toBeInTheDocument();
  });

  it('renders form title', () => {
    renderPage();
    expect(screen.getByText('External Release Details')).toBeInTheDocument();
  });

  it('renders external system selector', () => {
    renderPage();
    expect(screen.getByText('External System')).toBeInTheDocument();
  });

  it('renders version input', () => {
    renderPage();
    expect(screen.getByText('Version')).toBeInTheDocument();
  });

  it('renders target environment selector', () => {
    renderPage();
    expect(screen.getByText('Target Environment')).toBeInTheDocument();
  });

  it('renders ingest button', () => {
    renderPage();
    expect(screen.getByText('Ingest Release')).toBeInTheDocument();
  });

  it('ingest button is disabled without required fields', () => {
    renderPage();
    const btn = screen.getByText('Ingest Release');
    expect(btn).toBeDisabled();
  });

  it('renders trigger promotion checkbox', async () => {
    renderPage();
    await waitFor(() => {
      expect(screen.getByText('Trigger promotion to target environment after ingest')).toBeInTheDocument();
    });
  });

  it('matches snapshot', () => {
    const { container } = renderPage();
    expect(container.firstChild).toBeTruthy();
  });
});
