import * as React from 'react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';
import { ReleaseParameterAuditPage } from '../../features/change-governance/pages/ReleaseParameterAuditPage';

vi.mock('../../features/configuration/api/configurationApi', () => ({
  configurationApi: {
    getReleaseParameterAudit: vi.fn(),
  },
}));

vi.mock('../../api/client', () => ({
  default: { get: vi.fn(), post: vi.fn(), delete: vi.fn() },
}));

const mockAuditEntries = [
  {
    key: 'change.release.min_confidence_score_for_promotion',
    scope: 'System',
    scopeReferenceId: null,
    action: 'Set',
    previousValue: '0.60',
    newValue: '0.75',
    previousVersion: 1,
    newVersion: 2,
    changedBy: 'admin@nextraceone.io',
    changedAt: '2026-04-10T14:32:00Z',
    changeReason: 'Increased minimum confidence for production promotions',
    isSensitive: false,
  },
  {
    key: 'change.release.observation_window_minutes',
    scope: 'Tenant',
    scopeReferenceId: 'tenant-001',
    action: 'Set',
    previousValue: '60',
    newValue: '120',
    previousVersion: 1,
    newVersion: 2,
    changedBy: 'techlead@nextraceone.io',
    changedAt: '2026-04-12T09:15:00Z',
    changeReason: 'Extended observation window for critical services',
    isSensitive: false,
  },
  {
    key: 'change.release.evidence_pack.expiry_days',
    scope: 'Environment',
    scopeReferenceId: 'Production',
    action: 'Set',
    previousValue: '30',
    newValue: '90',
    previousVersion: 2,
    newVersion: 3,
    changedBy: 'admin@nextraceone.io',
    changedAt: '2026-04-15T11:00:00Z',
    changeReason: null,
    isSensitive: false,
  },
];

function wrapper(children: React.ReactNode) {
  const qc = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return (
    <QueryClientProvider client={qc}>
      <MemoryRouter>{children}</MemoryRouter>
    </QueryClientProvider>
  );
}

import { configurationApi } from '../../features/configuration/api/configurationApi';

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

describe('ReleaseParameterAuditPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('renders page title', () => {
    vi.mocked(configurationApi.getReleaseParameterAudit).mockResolvedValue(mockAuditEntries as any);
    const { container } = render(wrapper(<ReleaseParameterAuditPage />));
    expect(container).toBeTruthy();
  });

  it('renders audit entries after loading', async () => {
    vi.mocked(configurationApi.getReleaseParameterAudit).mockResolvedValue(mockAuditEntries as any);
    render(wrapper(<ReleaseParameterAuditPage />));
    expect(await screen.findByText('change.release.min_confidence_score_for_promotion')).toBeTruthy();
    expect(screen.getByText('change.release.observation_window_minutes')).toBeTruthy();
  });

  it('shows previous and new values', async () => {
    vi.mocked(configurationApi.getReleaseParameterAudit).mockResolvedValue(mockAuditEntries as any);
    render(wrapper(<ReleaseParameterAuditPage />));
    expect(await screen.findByText('0.60')).toBeTruthy();
    expect(screen.getByText('0.75')).toBeTruthy();
  });

  it('shows changedBy information', async () => {
    vi.mocked(configurationApi.getReleaseParameterAudit).mockResolvedValue(mockAuditEntries as any);
    render(wrapper(<ReleaseParameterAuditPage />));
    expect(await screen.findAllByText('admin@nextraceone.io')).toBeTruthy();
  });

  it('shows scope badges', async () => {
    vi.mocked(configurationApi.getReleaseParameterAudit).mockResolvedValue(mockAuditEntries as any);
    render(wrapper(<ReleaseParameterAuditPage />));
    expect(await screen.findByText('System')).toBeTruthy();
    expect(screen.getByText('Tenant')).toBeTruthy();
    expect(screen.getByText('Environment')).toBeTruthy();
  });

  it('shows empty state when no entries returned', async () => {
    vi.mocked(configurationApi.getReleaseParameterAudit).mockResolvedValue([]);
    render(wrapper(<ReleaseParameterAuditPage />));
    expect(await screen.findByText(/no audit records found/i)).toBeTruthy();
  });

  it('shows error state when API fails', async () => {
    vi.mocked(configurationApi.getReleaseParameterAudit).mockRejectedValue(new Error('Network error'));
    render(wrapper(<ReleaseParameterAuditPage />));
    expect(await screen.findByText(/could not load audit trail/i)).toBeTruthy();
  });

  it('renders filter input for key search', async () => {
    vi.mocked(configurationApi.getReleaseParameterAudit).mockResolvedValue(mockAuditEntries as any);
    render(wrapper(<ReleaseParameterAuditPage />));
    const filterInput = await screen.findByPlaceholderText(/filter by parameter/i);
    expect(filterInput).toBeTruthy();
  });

  it('filters entries by key when filter input is used', async () => {
    vi.mocked(configurationApi.getReleaseParameterAudit).mockResolvedValue(mockAuditEntries as any);
    render(wrapper(<ReleaseParameterAuditPage />));
    const filterInput = await screen.findByPlaceholderText(/filter by parameter/i);
    await userEvent.type(filterInput, 'confidence');
    expect(screen.getByText('change.release.min_confidence_score_for_promotion')).toBeTruthy();
    expect(screen.queryByText('change.release.observation_window_minutes')).toBeNull();
  });

  it('renders export CSV button', async () => {
    vi.mocked(configurationApi.getReleaseParameterAudit).mockResolvedValue(mockAuditEntries as any);
    render(wrapper(<ReleaseParameterAuditPage />));
    expect(await screen.findByText(/export csv/i)).toBeTruthy();
  });

  it('scope filter buttons are rendered', async () => {
    vi.mocked(configurationApi.getReleaseParameterAudit).mockResolvedValue(mockAuditEntries as any);
    render(wrapper(<ReleaseParameterAuditPage />));
    await screen.findByText('change.release.min_confidence_score_for_promotion');
    expect(screen.getByText('All scopes')).toBeTruthy();
  });
});
