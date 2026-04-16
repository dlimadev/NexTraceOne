import * as React from 'react';
import { describe, it, expect, vi } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';
import { ParameterUsageReportPage } from '../../features/configuration/pages/ParameterUsageReportPage';

vi.mock('../../features/configuration/api/configurationApi', () => ({
  configurationApi: {
    getParameterUsageReport: vi.fn(),
    getParameterComplianceSummary: vi.fn(),
    getDefinitions: vi.fn(),
    getEffectiveSettings: vi.fn(),
    setConfigurationValue: vi.fn(),
    getAuditHistory: vi.fn(),
    getFeatureFlags: vi.fn(),
    setFeatureFlagOverride: vi.fn(),
  },
}));

vi.mock('../../api/client', () => ({
  default: { get: vi.fn(), post: vi.fn() },
}));

import { configurationApi } from '../../features/configuration/api/configurationApi';

function renderPage() {
  vi.mocked(configurationApi.getParameterUsageReport).mockResolvedValue({
    totalDefinitions: 100,
    totalOverrides: 45,
    definitionsWithOverrides: 30,
    definitionsUsingDefault: 70,
    overrideCoveragePercent: 30,
    mostOverridden: [],
    recentlyChanged: [],
    overridesByScope: [],
  });
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter>
        <ParameterUsageReportPage />
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

describe('ParameterUsageReportPage', () => {
  it('renders without crashing', () => {
    const { container } = renderPage();
    expect(container).toBeDefined();
  });

  it('renders page title after data loads', async () => {
    renderPage();
    await waitFor(() => {
      expect(screen.getByText('Parameter Usage Report')).toBeDefined();
    });
  });
});
