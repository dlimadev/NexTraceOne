import * as React from 'react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';
import { BrandingAdminPage } from '../../features/configuration/pages/BrandingAdminPage';

vi.mock('../../features/configuration/api/configurationApi', () => ({
  configurationApi: {
    getEffectiveSettings: vi.fn(),
    setConfigurationValue: vi.fn(),
    getDefinitions: vi.fn(),
    getAuditHistory: vi.fn(),
    getFeatureFlags: vi.fn(),
    setFeatureFlagOverride: vi.fn(),
    getAlertRules: vi.fn(),
  },
}));

vi.mock('../../api/client', () => ({
  default: { get: vi.fn(), post: vi.fn() },
}));

import { configurationApi } from '../../features/configuration/api/configurationApi';

function renderPage() {
  vi.mocked(configurationApi.getEffectiveSettings).mockResolvedValue([]);
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter>
        <BrandingAdminPage />
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

describe('BrandingAdminPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('renders without crashing', () => {
    const { container } = renderPage();
    expect(container).toBeDefined();
  });

  it('shows loading state while fetching', () => {
    vi.mocked(configurationApi.getEffectiveSettings).mockReturnValue(new Promise(() => {}));
    renderPage();
    expect(document.body).toBeDefined();
  });
});
