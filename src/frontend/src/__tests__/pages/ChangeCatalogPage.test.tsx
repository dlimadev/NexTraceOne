import { describe, it, expect, vi, beforeEach } from 'vitest';
import { screen, waitFor } from '@testing-library/react';
import { Route, Routes } from 'react-router-dom';
import { renderWithProviders } from '../test-utils';
import { ChangeCatalogPage } from '../../features/change-governance/pages/ChangeCatalogPage';

vi.mock('../../features/change-governance/api/changeConfidence', () => ({
  changeConfidenceApi: {
    listChanges: vi.fn(),
    getSummary: vi.fn(),
  },
}));

vi.mock('../../contexts/EnvironmentContext', () => ({
  useEnvironment: vi.fn().mockReturnValue({
    activeEnvironmentId: 'tenant-1-prod',
    activeEnvironment: { id: 'tenant-1-prod', name: 'Production', profile: 'production', isProductionLike: true },
    availableEnvironments: [
      { id: 'tenant-1-prod', name: 'Production', profile: 'production', isProductionLike: true },
      { id: 'tenant-1-stg', name: 'Staging', profile: 'staging', isProductionLike: false },
    ],
    isLoadingEnvironments: false,
    selectEnvironment: vi.fn(),
    clearEnvironment: vi.fn(),
  }),
}));

import { changeConfidenceApi } from '../../features/change-governance/api/changeConfidence';

const mockSummary = {
  totalChanges: 42,
  validatedChanges: 35,
  changesNeedingAttention: 5,
  suspectedRegressions: 1,
  changesCorrelatedWithIncidents: 1,
};

const mockChanges = {
  changes: [
    {
      changeId: '11111111-1111-1111-1111-111111111111',
      apiAssetId: 'a1',
      serviceName: 'payments-service',
      version: '2.1.0',
      environment: 'prod',
      changeType: 'Deployment',
      deploymentStatus: 'Completed',
      changeLevel: 'Minor',
      confidenceStatus: 'Validated',
      validationStatus: 'Passed',
      changeScore: 0.25,
      teamName: 'Payments Team',
      domain: 'Payments',
      description: 'Add retry logic',
      workItemReference: null,
      commitSha: 'abc123',
      createdAt: '2026-03-15T10:00:00Z',
    },
    {
      changeId: '22222222-2222-2222-2222-222222222222',
      apiAssetId: 'a2',
      serviceName: 'orders-service',
      version: '3.0.0',
      environment: 'staging',
      changeType: 'ContractChange',
      deploymentStatus: 'InProgress',
      changeLevel: 'Major',
      confidenceStatus: 'NeedsAttention',
      validationStatus: 'Pending',
      changeScore: 0.55,
      teamName: 'Orders Team',
      domain: 'Orders',
      description: 'Schema migration',
      workItemReference: null,
      commitSha: 'def456',
      createdAt: '2026-03-15T11:00:00Z',
    },
  ],
  totalCount: 2,
  page: 1,
  pageSize: 20,
};

function renderChangeCatalog() {
  return renderWithProviders(
    <Routes>
      <Route path="/changes" element={<ChangeCatalogPage />} />
    </Routes>,
    { routerProps: { initialEntries: ['/changes'] } },
  );
}

describe('ChangeCatalogPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('exibe estado de loading', () => {
    vi.mocked(changeConfidenceApi.listChanges).mockReturnValue(new Promise(() => {}));
    vi.mocked(changeConfidenceApi.getSummary).mockReturnValue(new Promise(() => {}));
    renderChangeCatalog();
    expect(screen.getByText(/loading/i)).toBeInTheDocument();
  });

  it('exibe título da página', async () => {
    vi.mocked(changeConfidenceApi.listChanges).mockResolvedValue(mockChanges);
    vi.mocked(changeConfidenceApi.getSummary).mockResolvedValue(mockSummary);
    renderChangeCatalog();
    await waitFor(() => {
      expect(screen.getAllByText(/change confidence/i).length).toBeGreaterThanOrEqual(1);
    });
  });

  it('exibe cards de resumo', async () => {
    vi.mocked(changeConfidenceApi.listChanges).mockResolvedValue(mockChanges);
    vi.mocked(changeConfidenceApi.getSummary).mockResolvedValue(mockSummary);
    renderChangeCatalog();
    await waitFor(() => {
      expect(screen.getByText('42')).toBeInTheDocument();
      expect(screen.getByText('35')).toBeInTheDocument();
    });
  });

  it('exibe lista de mudanças', async () => {
    vi.mocked(changeConfidenceApi.listChanges).mockResolvedValue(mockChanges);
    vi.mocked(changeConfidenceApi.getSummary).mockResolvedValue(mockSummary);
    renderChangeCatalog();
    await waitFor(() => {
      expect(screen.getByText('payments-service')).toBeInTheDocument();
      expect(screen.getByText('orders-service')).toBeInTheDocument();
    });
  });

  it('exibe filtros de pesquisa', async () => {
    vi.mocked(changeConfidenceApi.listChanges).mockResolvedValue(mockChanges);
    vi.mocked(changeConfidenceApi.getSummary).mockResolvedValue(mockSummary);
    renderChangeCatalog();
    await waitFor(() => {
      expect(screen.getByPlaceholderText(/search changes/i)).toBeInTheDocument();
    });
  });

  it('exibe empty state quando não há mudanças', async () => {
    vi.mocked(changeConfidenceApi.listChanges).mockResolvedValue({
      changes: [],
      totalCount: 0,
      page: 1,
      pageSize: 20,
    });
    vi.mocked(changeConfidenceApi.getSummary).mockResolvedValue({
      totalChanges: 0,
      validatedChanges: 0,
      changesNeedingAttention: 0,
      suspectedRegressions: 0,
      changesCorrelatedWithIncidents: 0,
    });
    renderChangeCatalog();
    await waitFor(() => {
      expect(screen.getByText(/no changes found/i)).toBeInTheDocument();
    });
  });
});
