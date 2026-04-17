import * as React from 'react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';
import { ReleaseParameterEnvironmentOverridePage } from '../../features/change-governance/pages/ReleaseParameterEnvironmentOverridePage';

vi.mock('../../features/configuration/api/configurationApi', () => ({
  configurationApi: {
    getReleaseParameterEnvironmentOverrides: vi.fn(),
    setConfigurationValue: vi.fn(),
    removeOverride: vi.fn(),
  },
}));

vi.mock('../../api/client', () => ({
  default: { get: vi.fn(), post: vi.fn(), put: vi.fn(), delete: vi.fn() },
}));

const mockOverrides = [
  {
    id: 'entry-001',
    definitionKey: 'change.release.observation_window_minutes',
    scope: 'Environment',
    scopeReferenceId: 'Production',
    value: '120',
    isActive: true,
    version: 2,
    changeReason: 'Production services require longer observation windows',
    updatedAt: '2026-04-10T14:32:00Z',
    updatedBy: 'admin@nextraceone.io',
  },
  {
    id: 'entry-002',
    definitionKey: 'change.release.min_confidence_score_for_promotion',
    scope: 'Environment',
    scopeReferenceId: 'Pre-Production',
    value: '0.65',
    isActive: true,
    version: 1,
    changeReason: null,
    updatedAt: '2026-04-12T09:15:00Z',
    updatedBy: 'techlead@nextraceone.io',
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

describe('ReleaseParameterEnvironmentOverridePage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('renders page title', () => {
    vi.mocked(configurationApi.getReleaseParameterEnvironmentOverrides).mockResolvedValue(mockOverrides as any);
    const { container } = render(wrapper(<ReleaseParameterEnvironmentOverridePage />));
    expect(container).toBeTruthy();
  });

  it('renders overrides table after loading', async () => {
    vi.mocked(configurationApi.getReleaseParameterEnvironmentOverrides).mockResolvedValue(mockOverrides as any);
    render(wrapper(<ReleaseParameterEnvironmentOverridePage />));
    expect(await screen.findByText('change.release.observation_window_minutes')).toBeTruthy();
    expect(screen.getByText('change.release.min_confidence_score_for_promotion')).toBeTruthy();
  });

  it('shows environment badges for each override', async () => {
    vi.mocked(configurationApi.getReleaseParameterEnvironmentOverrides).mockResolvedValue(mockOverrides as any);
    render(wrapper(<ReleaseParameterEnvironmentOverridePage />));
    expect(await screen.findByText('Production')).toBeTruthy();
    expect(screen.getByText('Pre-Production')).toBeTruthy();
  });

  it('shows override values in the table', async () => {
    vi.mocked(configurationApi.getReleaseParameterEnvironmentOverrides).mockResolvedValue(mockOverrides as any);
    render(wrapper(<ReleaseParameterEnvironmentOverridePage />));
    expect(await screen.findByText('120')).toBeTruthy();
    expect(screen.getByText('0.65')).toBeTruthy();
  });

  it('shows updatedBy information', async () => {
    vi.mocked(configurationApi.getReleaseParameterEnvironmentOverrides).mockResolvedValue(mockOverrides as any);
    render(wrapper(<ReleaseParameterEnvironmentOverridePage />));
    expect(await screen.findByText('admin@nextraceone.io')).toBeTruthy();
  });

  it('shows empty state when no overrides configured', async () => {
    vi.mocked(configurationApi.getReleaseParameterEnvironmentOverrides).mockResolvedValue([]);
    render(wrapper(<ReleaseParameterEnvironmentOverridePage />));
    expect(await screen.findByText(/no overrides configured/i)).toBeTruthy();
  });

  it('shows error state when API fails', async () => {
    vi.mocked(configurationApi.getReleaseParameterEnvironmentOverrides).mockRejectedValue(new Error('Network error'));
    render(wrapper(<ReleaseParameterEnvironmentOverridePage />));
    expect(await screen.findByText(/could not load overrides/i)).toBeTruthy();
  });

  it('opens add override form when Add Override button is clicked', async () => {
    vi.mocked(configurationApi.getReleaseParameterEnvironmentOverrides).mockResolvedValue(mockOverrides as any);
    render(wrapper(<ReleaseParameterEnvironmentOverridePage />));
    const addButton = await screen.findByText('Add Override');
    await userEvent.click(addButton);
    expect(screen.getByPlaceholderText('change.release…')).toBeTruthy();
  });

  it('closes form when Cancel is clicked', async () => {
    vi.mocked(configurationApi.getReleaseParameterEnvironmentOverrides).mockResolvedValue(mockOverrides as any);
    render(wrapper(<ReleaseParameterEnvironmentOverridePage />));
    const addButton = await screen.findByText('Add Override');
    await userEvent.click(addButton);
    const cancelButton = screen.getByText('Cancel');
    await userEvent.click(cancelButton);
    expect(screen.queryByPlaceholderText('change.release…')).toBeNull();
  });

  it('renders column headers', async () => {
    vi.mocked(configurationApi.getReleaseParameterEnvironmentOverrides).mockResolvedValue(mockOverrides as any);
    render(wrapper(<ReleaseParameterEnvironmentOverridePage />));
    expect(await screen.findByText('Parameter')).toBeTruthy();
    expect(screen.getByText('Environment')).toBeTruthy();
    expect(screen.getByText('Value')).toBeTruthy();
  });
});
