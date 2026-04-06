import * as React from 'react';
import { describe, it, expect, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';
import { PredictiveIntelligencePage } from '../../features/operations/pages/PredictiveIntelligencePage';

vi.mock('../../api/client', () => ({
  default: {
    post: vi.fn().mockResolvedValue({
      data: {
        predictionId: 'pred-001',
        serviceId: 'svc-001',
        serviceName: 'payment-service',
        failureProbabilityPercent: 72,
        riskLevel: 'High',
        predictionHorizon: '24h',
        causalFactors: ['High error rate', 'Recent deploy'],
        recommendedAction: 'Reduce deployment frequency and investigate error spikes.',
        computedAt: '2026-01-15T10:00:00Z',
      },
    }),
    get: vi.fn().mockResolvedValue({
      data: {
        changeId: '00000000-0000-0000-0000-000000000001',
        serviceId: 'svc-001',
        riskScore: 65,
        riskLevel: 'Medium',
        riskFactors: ['No test evidence', 'Business hours'],
        recommendations: ['Add test coverage', 'Deploy during off-peak hours'],
        assessedAt: '2026-01-15T10:00:00Z',
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

describe('PredictiveIntelligencePage', () => {
  it('renders title', () => {
    renderWithProviders(<PredictiveIntelligencePage />);
    expect(screen.getByText('predictiveIntelligence.title')).toBeDefined();
  });

  it('shows service failure prediction tab', () => {
    renderWithProviders(<PredictiveIntelligencePage />);
    expect(screen.getByText('predictiveIntelligence.serviceFailurePrediction')).toBeDefined();
    expect(screen.getByText('predictiveIntelligence.serviceId')).toBeDefined();
    expect(screen.getByText('predictiveIntelligence.serviceName')).toBeDefined();
    expect(screen.getByText('predictiveIntelligence.environment')).toBeDefined();
    expect(screen.getByText('predictiveIntelligence.submit')).toBeDefined();
  });

  it('shows change risk assessment tab', () => {
    renderWithProviders(<PredictiveIntelligencePage />);
    expect(screen.getByText('predictiveIntelligence.changeRiskAssessment')).toBeDefined();
  });

  it('shows submit button to handle form submission', () => {
    renderWithProviders(<PredictiveIntelligencePage />);
    const submitButton = screen.getByText('predictiveIntelligence.submit');
    expect(submitButton).toBeDefined();
  });
});
