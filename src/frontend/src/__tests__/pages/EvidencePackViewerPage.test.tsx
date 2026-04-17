import * as React from 'react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';
import { EvidencePackViewerPage } from '../../features/change-governance/pages/EvidencePackViewerPage';

vi.mock('../../features/change-governance/api/workflow', () => ({
  workflowApi: {
    listInstances: vi.fn(),
    getEvidencePack: vi.fn(),
    generateEvidencePack: vi.fn(),
    exportEvidencePackPdf: vi.fn(),
  },
}));

vi.mock('../../api/client', () => ({
  default: { get: vi.fn(), post: vi.fn() },
}));

const mockInstances = {
  items: [
    { id: 'inst-001', templateName: 'Standard Release', status: 'Completed' },
    { id: 'inst-002', templateName: 'Hotfix', status: 'InProgress' },
  ],
  totalCount: 2,
  page: 1,
  pageSize: 20,
};

const mockPack = {
  evidencePackId: 'ep-001',
  workflowInstanceId: 'inst-001',
  releaseId: 'rel-001',
  contractDiffSummary: '+ GET /v2/users\n- DELETE /v1/users',
  blastRadiusScore: 0.82,
  spectralScore: 0.75,
  changeIntelligenceScore: 0.68,
  approvalHistory: 'Approved by john.doe at 2026-04-10T09:00:00Z',
  contractHash: 'sha256:abc123',
  completenessPercentage: 91,
  generatedAt: '2026-04-10T10:00:00Z',
  pipelineSource: 'GitHub Actions',
  buildId: 'run-9812',
  commitSha: 'a1b2c3d4e5f6',
  ciChecksResult: 'build_success,tests_passed',
};

function wrapper(children: React.ReactNode) {
  const qc = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return (
    <QueryClientProvider client={qc}>
      <MemoryRouter>{children}</MemoryRouter>
    </QueryClientProvider>
  );
}

import { workflowApi } from '../../features/change-governance/api/workflow';

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

describe('EvidencePackViewerPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(workflowApi.listInstances).mockResolvedValue(mockInstances as any);
    vi.mocked(workflowApi.getEvidencePack).mockResolvedValue(mockPack as any);
    vi.mocked(workflowApi.generateEvidencePack).mockResolvedValue(mockPack as any);
    vi.mocked(workflowApi.exportEvidencePackPdf).mockResolvedValue({
      base64Content: 'abc',
      fileName: 'evidence.pdf',
      generatedAt: '2026-04-10T10:00:00Z',
    } as any);
  });

  it('renders page title', async () => {
    render(wrapper(<EvidencePackViewerPage />));
    expect(await screen.findByText(/evidence pack viewer/i)).toBeTruthy();
  });

  it('shows empty state when no instance is selected', async () => {
    render(wrapper(<EvidencePackViewerPage />));
    expect(await screen.findByText(/no instance selected/i)).toBeTruthy();
  });

  it('renders instance selector with loaded instances', async () => {
    render(wrapper(<EvidencePackViewerPage />));
    expect(await screen.findByText('Standard Release — Completed')).toBeTruthy();
    expect(screen.getByText('Hotfix — InProgress')).toBeTruthy();
  });

  it('shows placeholder option in selector', async () => {
    render(wrapper(<EvidencePackViewerPage />));
    expect(await screen.findAllByText(/select a workflow instance/i)).toBeTruthy();
  });

  it('shows evidence pack completeness when instance is selected', async () => {
    render(wrapper(<EvidencePackViewerPage />));
    await screen.findByText('Standard Release — Completed');
    const selector = screen.getByRole('combobox');
    await userEvent.selectOptions(selector, 'inst-001');
    expect(await screen.findByText(/91% complete/i)).toBeTruthy();
  });

  it('shows blast radius score section when pack is loaded', async () => {
    render(wrapper(<EvidencePackViewerPage />));
    await screen.findByText('Standard Release — Completed');
    const selector = screen.getByRole('combobox');
    await userEvent.selectOptions(selector, 'inst-001');
    expect(await screen.findByText(/blast radius score/i)).toBeTruthy();
  });

  it('shows generate button when no pack found', async () => {
    vi.mocked(workflowApi.getEvidencePack).mockRejectedValue(new Error('Not found'));
    render(wrapper(<EvidencePackViewerPage />));
    await screen.findByText('Standard Release — Completed');
    const selector = screen.getByRole('combobox');
    await userEvent.selectOptions(selector, 'inst-001');
    expect(await screen.findByText(/generate evidence pack/i)).toBeTruthy();
  });

  it('shows export PDF button when pack is loaded', async () => {
    render(wrapper(<EvidencePackViewerPage />));
    await screen.findByText('Standard Release — Completed');
    const selector = screen.getByRole('combobox');
    await userEvent.selectOptions(selector, 'inst-001');
    expect(await screen.findByText(/export pdf/i)).toBeTruthy();
  });

  it('shows completeness percentage badge', async () => {
    render(wrapper(<EvidencePackViewerPage />));
    await screen.findByText('Standard Release — Completed');
    const selector = screen.getByRole('combobox');
    await userEvent.selectOptions(selector, 'inst-001');
    await waitFor(() => {
      expect(screen.getByText(/91%/)).toBeTruthy();
    });
  });

  it('shows quality scores section', async () => {
    render(wrapper(<EvidencePackViewerPage />));
    await screen.findByText('Standard Release — Completed');
    const selector = screen.getByRole('combobox');
    await userEvent.selectOptions(selector, 'inst-001');
    expect(await screen.findByText(/quality scores/i)).toBeTruthy();
  });
});
