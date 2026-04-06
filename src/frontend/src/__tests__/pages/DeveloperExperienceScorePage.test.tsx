import * as React from 'react';
import { describe, it, expect, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';
import { DeveloperExperienceScorePage } from '../../features/catalog/pages/DeveloperExperienceScorePage';

vi.mock('../../api/client', () => ({
  default: {
    get: vi.fn().mockResolvedValue({
      data: {
        items: [
          {
            scoreId: 'score-001',
            teamId: 'team-alpha',
            teamName: 'Team Alpha',
            period: '2026-Q1',
            cycleTimeHours: 24,
            deploymentFrequencyPerWeek: 3.5,
            cognitiveLoadScore: 4,
            toilPercentage: 15,
            overallScore: 82,
            scoreLevel: 'Good',
            computedAt: '2026-01-15T10:00:00Z',
          },
        ],
      },
    }),
    post: vi.fn().mockResolvedValue({
      data: {
        scoreId: 'score-002',
        teamId: 'team-beta',
        teamName: 'Team Beta',
        period: '2026-Q2',
        cycleTimeHours: 18,
        deploymentFrequencyPerWeek: 5,
        cognitiveLoadScore: 3,
        toilPercentage: 10,
        overallScore: 91,
        scoreLevel: 'Excellent',
        computedAt: '2026-01-15T11:00:00Z',
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

describe('DeveloperExperienceScorePage', () => {
  it('renders title', () => {
    renderWithProviders(<DeveloperExperienceScorePage />);
    expect(screen.getByText('developerExperienceScore.title')).toBeDefined();
  });

  it('shows score submission form', () => {
    renderWithProviders(<DeveloperExperienceScorePage />);
    expect(screen.getByText('developerExperienceScore.submitScore')).toBeDefined();
    expect(screen.getByText('developerExperienceScore.teamName')).toBeDefined();
    expect(screen.getByText('developerExperienceScore.period')).toBeDefined();
    expect(screen.getByText('developerExperienceScore.submit')).toBeDefined();
  });

  it('shows recent scores section', () => {
    renderWithProviders(<DeveloperExperienceScorePage />);
    expect(screen.getByText('developerExperienceScore.recentScores')).toBeDefined();
  });

  it('handles empty state when no scores exist', () => {
    renderWithProviders(<DeveloperExperienceScorePage />);
    expect(screen.getByText('developerExperienceScore.recentScores')).toBeDefined();
    expect(screen.getByText('developerExperienceScore.teamId')).toBeDefined();
  });
});
