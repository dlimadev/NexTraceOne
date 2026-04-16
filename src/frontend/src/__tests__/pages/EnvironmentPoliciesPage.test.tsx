import { render, screen, waitFor } from '@testing-library/react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { EnvironmentPoliciesPage } from '../../features/platform-admin/pages/EnvironmentPoliciesPage';
import { platformAdminApi } from '../../features/platform-admin/api/platformAdmin';
import type { EnvironmentPoliciesResponse } from '../../features/platform-admin/api/platformAdmin';

vi.mock('react-i18next', () => ({
  useTranslation: () => ({
    t: (key: string, _opts?: Record<string, unknown>) => key,
    i18n: { language: 'en' },
  }),
}));

vi.mock('../../features/platform-admin/api/platformAdmin', () => ({
  platformAdminApi: {
    getEnvironmentPolicies: vi.fn(),
    updateEnvironmentPolicy: vi.fn(),
    getExternalHttpAudit: vi.fn(),
    getProxyConfig: vi.fn(),
    updateProxyConfig: vi.fn(),
    testProxyConnectivity: vi.fn(),
    getGreenOpsReport: vi.fn(),
    updateGreenOpsConfig: vi.fn(),
    getPlatformAlerts: vi.fn(),
    updateAlertRule: vi.fn(),
    getNetworkPolicy: vi.fn(),
    getElasticsearchManager: vi.fn(),
    updateIlmPolicy: vi.fn(),
    getResourceBudgets: vi.fn(),
    updateTenantQuota: vi.fn(),
    getSupportBundle: vi.fn(),
    getBackupStatus: vi.fn(),
    triggerBackup: vi.fn(),
    getStartupReport: vi.fn(),
    getRestorePoints: vi.fn(),
    initiateRecovery: vi.fn(),
    getAiGovernorStatus: vi.fn(),
    updateAiGovernorConfig: vi.fn(),
    getAiGovernanceDashboard: vi.fn(),
    updateAiGovernanceConfig: vi.fn(),
    getNonProdSchedules: vi.fn(),
    updateNonProdSchedule: vi.fn(),
    overrideNonProdSchedule: vi.fn(),
    getCapacityForecast: vi.fn(),
  },
}));

import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { ReactNode } from 'react';
const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
const Wrapper = ({ children }: { children: ReactNode }) => (
  <QueryClientProvider client={queryClient}>{children}</QueryClientProvider>
);

const mockPoliciesResponse: EnvironmentPoliciesResponse = {
  policies: [
    {
      id: 'policy-1',
      policyName: 'ProductionDataAccess',
      environments: ['Production'],
      allowedRoles: ['TechLead', 'Architect', 'PlatformAdmin'],
      requireJitFor: ['Engineer'],
      jitApprovalRequiredFrom: 'TechLead',
      description: 'Restricts direct Production access to senior roles.',
      updatedAt: '2026-04-15T09:00:00Z',
    },
    {
      id: 'policy-2',
      policyName: 'StagingAccess',
      environments: ['Staging'],
      allowedRoles: ['Engineer', 'TechLead', 'Architect'],
      requireJitFor: [],
      description: 'Staging is accessible to engineers and above.',
      updatedAt: '2026-04-10T09:00:00Z',
    },
  ],
  availableEnvironments: ['Development', 'Staging', 'Production'],
  generatedAt: '2026-04-15T12:00:00Z',
  simulatedNote: 'Simulated policy data',
};

describe('EnvironmentPoliciesPage', () => {
  beforeEach(() => {
    queryClient.clear();
    vi.clearAllMocks();
  });

  it('shows loading state', () => {
    vi.mocked(platformAdminApi.getEnvironmentPolicies).mockImplementation(() => new Promise(() => {}));
    render(<EnvironmentPoliciesPage />, { wrapper: Wrapper });
    expect(screen.getByText('loading')).toBeDefined();
  });

  it('shows error state on API failure', async () => {
    vi.mocked(platformAdminApi.getEnvironmentPolicies).mockRejectedValue(new Error('API error'));
    render(<EnvironmentPoliciesPage />, { wrapper: Wrapper });
    await waitFor(() => expect(screen.getByText('error')).toBeDefined());
  });

  it('renders page title', async () => {
    vi.mocked(platformAdminApi.getEnvironmentPolicies).mockResolvedValue(mockPoliciesResponse);
    render(<EnvironmentPoliciesPage />, { wrapper: Wrapper });
    await waitFor(() => expect(screen.getByText('title')).toBeDefined());
  });

  it('renders policy names', async () => {
    vi.mocked(platformAdminApi.getEnvironmentPolicies).mockResolvedValue(mockPoliciesResponse);
    render(<EnvironmentPoliciesPage />, { wrapper: Wrapper });
    await waitFor(() => {
      expect(screen.getByText('ProductionDataAccess')).toBeDefined();
      expect(screen.getByText('StagingAccess')).toBeDefined();
    });
  });

  it('renders environment badges for policies', async () => {
    vi.mocked(platformAdminApi.getEnvironmentPolicies).mockResolvedValue(mockPoliciesResponse);
    render(<EnvironmentPoliciesPage />, { wrapper: Wrapper });
    await waitFor(() => expect(screen.getByText('Production')).toBeDefined());
  });

  it('shows simulated note', async () => {
    vi.mocked(platformAdminApi.getEnvironmentPolicies).mockResolvedValue(mockPoliciesResponse);
    render(<EnvironmentPoliciesPage />, { wrapper: Wrapper });
    await waitFor(() => expect(screen.getByText('Simulated policy data')).toBeDefined());
  });
});
