import * as React from 'react';
import { describe, it, expect, vi } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';
import { ParameterComplianceDashboardPage } from '../../features/configuration/pages/ParameterComplianceDashboardPage';

vi.mock('../../features/configuration/api/configurationApi', () => ({
  configurationApi: {
    getParameterComplianceSummary: vi.fn(),
    getParameterUsageReport: vi.fn(),
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
  vi.mocked(configurationApi.getParameterComplianceSummary).mockResolvedValue({
    totalDefinitions: 100,
    withI18nKeys: 90,
    withoutI18nKeys: 10,
    i18nCoveragePercent: 90,
    deprecatedCount: 2,
    sensitiveCount: 5,
    withValidationRules: 70,
    withoutValidationRules: 30,
    validationCoveragePercent: 70,
    editableCount: 80,
    readOnlyCount: 20,
    byCategory: [],
    deprecatedKeys: [],
  });
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter>
        <ParameterComplianceDashboardPage />
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

describe('ParameterComplianceDashboardPage', () => {
  it('renders without crashing', () => {
    const { container } = renderPage();
    expect(container).toBeDefined();
  });

  it('renders page title after data loads', async () => {
    renderPage();
    await waitFor(() => {
      expect(screen.getByText('Parameterization Compliance Dashboard')).toBeDefined();
    });
  });
});
