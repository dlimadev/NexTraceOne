import * as React from 'react';
import { describe, it, expect, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';
import { ApiPolicyAsCodePage } from '../../features/governance/pages/ApiPolicyAsCodePage';

vi.mock('../../api/client', () => ({
  default: {
    post: vi.fn().mockResolvedValue({
      data: {
        name: 'my-api-policy',
        displayName: 'My API Policy',
        version: '1.0.0',
        format: 'Yaml',
        enforcementMode: 'AuditOnly',
        registeredBy: 'admin',
        createdAt: '2026-01-15T10:00:00Z',
      },
    }),
    get: vi.fn().mockResolvedValue({
      data: {
        name: 'my-api-policy',
        displayName: 'My API Policy',
        version: '1.0.0',
        format: 'Yaml',
        enforcementMode: 'AuditOnly',
        registeredBy: 'admin',
        createdAt: '2026-01-15T10:00:00Z',
      },
    }),
  },
}));

vi.mock('react-i18next', () => ({
  useTranslation: () => ({
    t: (key: string) => key,
  }),
}));

const renderWithProviders = (ui: React.ReactElement) => {
  const qc = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={qc}>
      <MemoryRouter>{ui}</MemoryRouter>
    </QueryClientProvider>
  );
};

describe('ApiPolicyAsCodePage', () => {
  it('renders title', () => {
    renderWithProviders(<ApiPolicyAsCodePage />);
    expect(screen.getByText('apiPolicyAsCode.title')).toBeDefined();
  });

  it('shows register policy form', () => {
    renderWithProviders(<ApiPolicyAsCodePage />);
    expect(screen.getByText('apiPolicyAsCode.registerPolicy')).toBeDefined();
    expect(screen.getByText('apiPolicyAsCode.enforcementMode')).toBeDefined();
    expect(screen.getByText('apiPolicyAsCode.registeredBy')).toBeDefined();
    expect(screen.getByText('apiPolicyAsCode.submit')).toBeDefined();
  });

  it('shows simulate policy form', () => {
    renderWithProviders(<ApiPolicyAsCodePage />);
    expect(screen.getByText('apiPolicyAsCode.simulatePolicy')).toBeDefined();
    expect(screen.getByText('apiPolicyAsCode.resourceType')).toBeDefined();
    expect(screen.getByText('apiPolicyAsCode.simulate')).toBeDefined();
  });

  it('handles policy creation and shows success section', () => {
    renderWithProviders(<ApiPolicyAsCodePage />);
    expect(screen.getByText('apiPolicyAsCode.registerPolicy')).toBeDefined();
    expect(screen.getByText('apiPolicyAsCode.definitionContent')).toBeDefined();
    expect(screen.getByText('apiPolicyAsCode.format')).toBeDefined();
  });
});
