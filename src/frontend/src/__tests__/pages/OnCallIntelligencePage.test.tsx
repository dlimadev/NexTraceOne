import * as React from 'react';
import { describe, it, expect, vi } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';
import { OnCallIntelligencePage } from '../../features/operations/pages/OnCallIntelligencePage';

vi.mock('../../api/client', () => ({
  default: {
    get: vi.fn().mockResolvedValue({
      data: {
        periodDays: 30,
        generatedAt: '2026-04-06T12:00:00Z',
        totalIncidentsInPeriod: 18,
        avgIncidentsPerWeek: 4.5,
        peakHour: 14,
        peakDayOfWeek: 'Tuesday',
        fatigueSeverity: 'Moderate',
        recommendations: ['Reduce deployment frequency during peak hours'],
        distribution: [{ hour: 14, dayOfWeek: 'Tuesday', incidentCount: 5 }],
        teamFatigue: [
          {
            teamName: 'Platform Team',
            incidentsLastWeek: 4,
            incidentsLastMonth: 18,
            avgResponseMinutes: 12,
            fatigueLevel: 'Moderate',
          },
        ],
      },
    }),
  },
}));

vi.mock('react-i18next', () => ({
  useTranslation: () => ({
    t: (key: string, opts?: Record<string, unknown>) => {
      if (opts?.minutes !== undefined) return `Avg response: ${opts.minutes} min`;
      if (opts?.level) return `High on-call fatigue detected: ${opts.level}`;
      return key;
    },
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

describe('OnCallIntelligencePage', () => {
  it('renders title', async () => {
    renderWithProviders(<OnCallIntelligencePage />);
    await waitFor(() => {
      expect(screen.getByText('operations.onCall.title')).toBeDefined();
    });
  });

  it('shows loading state initially', () => {
    renderWithProviders(<OnCallIntelligencePage />);
    expect(screen.getByText('operations.onCall.loading')).toBeDefined();
  });

  it('shows team fatigue entries after loading', async () => {
    renderWithProviders(<OnCallIntelligencePage />);
    await waitFor(() => {
      expect(screen.getByText('Platform Team')).toBeDefined();
    });
  });

  it('shows recommendations section after loading', async () => {
    renderWithProviders(<OnCallIntelligencePage />);
    await waitFor(() => {
      expect(screen.getByText('Reduce deployment frequency during peak hours')).toBeDefined();
    });
  });

  it('shows fatigue alert for non-low severity', async () => {
    renderWithProviders(<OnCallIntelligencePage />);
    await waitFor(() => {
      expect(screen.getByText('High on-call fatigue detected: Moderate')).toBeDefined();
    });
  });
});
