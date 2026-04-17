import * as React from 'react';
import { describe, it, expect, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';
import { ChaosEngineeringPage } from '../../features/operations/pages/ChaosEngineeringPage';

vi.mock('../../api/client', () => ({
  default: {
    get: vi.fn().mockResolvedValue({
      data: {
        items: [
          {
            experimentId: '11111111-0000-0000-0000-000000000001',
            serviceName: 'payment-service',
            experimentType: 'latency-injection',
            riskLevel: 'Low',
            status: 'Completed',
            createdAt: '2026-01-15T10:00:00Z',
          },
        ],
        totalCount: 1,
      },
    }),
    post: vi.fn().mockResolvedValue({
      data: {
        experimentId: 'aaaaaaaa-0000-0000-0000-000000000001',
        serviceName: 'test-service',
        environment: 'Development',
        experimentType: 'latency-injection',
        steps: ['Step 1', 'Step 2'],
        riskLevel: 'Low',
        estimatedDurationSeconds: 60,
        targetPercentage: 10,
        safetyChecks: ['Ensure rollback plan exists'],
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

describe('ChaosEngineeringPage', () => {
  it('renders title', () => {
    renderWithProviders(<ChaosEngineeringPage />);
    expect(screen.getByText('chaosEngineering.title')).toBeDefined();
  });

  it('shows create experiment form', () => {
    renderWithProviders(<ChaosEngineeringPage />);
    expect(screen.getByText('chaosEngineering.createExperiment')).toBeDefined();
    expect(screen.getByText('chaosEngineering.serviceName')).toBeDefined();
    expect(screen.getByText('chaosEngineering.environment')).toBeDefined();
    expect(screen.getByText('chaosEngineering.experimentType')).toBeDefined();
    expect(screen.getByText('chaosEngineering.submit')).toBeDefined();
  });

  it('shows recent experiments section', () => {
    renderWithProviders(<ChaosEngineeringPage />);
    expect(screen.getByText('chaosEngineering.recentExperiments')).toBeDefined();
  });

  it('shows submit button to handle form submission', () => {
    renderWithProviders(<ChaosEngineeringPage />);
    const submitButton = screen.getByText('chaosEngineering.submit');
    expect(submitButton).toBeDefined();
  });
});
