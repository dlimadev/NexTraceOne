import * as React from 'react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';
import { ReleaseRollbackPage } from '../../features/change-governance/pages/ReleaseRollbackPage';

vi.mock('../../features/change-governance/api/changeIntelligence', () => ({
  changeIntelligenceApi: {
    getRollbackAssessment: vi.fn(),
    assessRollbackViability: vi.fn(),
    registerRollback: vi.fn(),
  },
}));

vi.mock('../../api/client', () => ({
  default: { get: vi.fn(), post: vi.fn() },
}));

import { changeIntelligenceApi } from '../../features/change-governance/api/changeIntelligence';

const mockAssessment = {
  id: 'assessment-001',
  releaseId: 'release-xyz',
  isViable: true,
  previousVersion: '1.1.0',
  hasReversibleMigrations: true,
  consumersAlreadyMigrated: 2,
  totalConsumersImpacted: 8,
  inviabilityReason: null,
  recommendation: 'Rollback to v1.1.0 is safe. Database migrations are reversible and only 2 of 8 consumers have migrated.',
  readinessScore: 82,
  assessedBy: 'tech-lead@example.com',
  assessedAt: '2026-04-12T14:00:00Z',
};

function renderPage() {
  const qc = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={qc}>
      <MemoryRouter>
        <ReleaseRollbackPage />
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

describe('ReleaseRollbackPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(changeIntelligenceApi.getRollbackAssessment).mockResolvedValue(mockAssessment);
    vi.mocked(changeIntelligenceApi.assessRollbackViability).mockResolvedValue({ id: 'assessment-001' });
    vi.mocked(changeIntelligenceApi.registerRollback).mockResolvedValue({});
  });

  it('renders page title', () => {
    renderPage();
    expect(screen.getByText(/Release Rollback/i)).toBeTruthy();
  });

  it('renders search input and button', () => {
    renderPage();
    expect(screen.getByPlaceholderText(/Release ID/i)).toBeTruthy();
    expect(screen.getByText('Search')).toBeTruthy();
  });

  it('shows empty state before searching', () => {
    renderPage();
    expect(screen.getByText(/No assessment loaded/i)).toBeTruthy();
  });

  it('loads and shows assessment after searching', async () => {
    renderPage();
    const input = screen.getByPlaceholderText(/Release ID/i);
    await userEvent.type(input, 'release-xyz');
    await userEvent.click(screen.getByText('Search'));

    await waitFor(() => {
      expect(screen.getByText(/Rollback Assessment Result/i)).toBeTruthy();
    });
    expect(screen.getByText('Viable')).toBeTruthy();
  });

  it('shows readiness score in badge', async () => {
    renderPage();
    const input = screen.getByPlaceholderText(/Release ID/i);
    await userEvent.type(input, 'release-xyz');
    await userEvent.click(screen.getByText('Search'));

    await waitFor(() => {
      expect(screen.getByText(/Readiness.*82/i)).toBeTruthy();
    });
  });

  it('displays recommendation text', async () => {
    renderPage();
    const input = screen.getByPlaceholderText(/Release ID/i);
    await userEvent.type(input, 'release-xyz');
    await userEvent.click(screen.getByText('Search'));

    await waitFor(() => {
      expect(screen.getByText(/Rollback to v1.1.0 is safe/i)).toBeTruthy();
    });
  });

  it('shows Execute Rollback button for viable assessment', async () => {
    renderPage();
    const input = screen.getByPlaceholderText(/Release ID/i);
    await userEvent.type(input, 'release-xyz');
    await userEvent.click(screen.getByText('Search'));

    await waitFor(() => {
      expect(screen.getByText(/Execute Rollback/i)).toBeTruthy();
    });
  });

  it('shows create assessment option when not found', async () => {
    vi.mocked(changeIntelligenceApi.getRollbackAssessment).mockRejectedValue(new Error('404'));
    renderPage();
    const input = screen.getByPlaceholderText(/Release ID/i);
    await userEvent.type(input, 'release-new');
    await userEvent.click(screen.getByText('Search'));

    await waitFor(() => {
      expect(screen.getByText(/Create Rollback Assessment/i)).toBeTruthy();
    });
  });

  it('shows assessment form when create button is clicked', async () => {
    vi.mocked(changeIntelligenceApi.getRollbackAssessment).mockRejectedValue(new Error('404'));
    renderPage();
    const input = screen.getByPlaceholderText(/Release ID/i);
    await userEvent.type(input, 'release-new');
    await userEvent.click(screen.getByText('Search'));

    await waitFor(() => {
      expect(screen.getByText(/Create Rollback Assessment/i)).toBeTruthy();
    });
    await userEvent.click(screen.getByText(/Create Rollback Assessment/i));
    expect(screen.getByText(/Recommendation/i)).toBeTruthy();
  });

  it('shows rollback confirmation form on Execute Rollback click', async () => {
    renderPage();
    const input = screen.getByPlaceholderText(/Release ID/i);
    await userEvent.type(input, 'release-xyz');
    await userEvent.click(screen.getByText('Search'));

    await waitFor(() => {
      expect(screen.getByText(/Execute Rollback/i)).toBeTruthy();
    });
    await userEvent.click(screen.getByText(/Execute Rollback/i));
    expect(screen.getByText(/this action will be audited/i)).toBeTruthy();
  });

  it('calls registerRollback with reason on confirm', async () => {
    renderPage();
    const input = screen.getByPlaceholderText(/Release ID/i);
    await userEvent.type(input, 'release-xyz');
    await userEvent.click(screen.getByText('Search'));

    await waitFor(() => {
      expect(screen.getByText(/Execute Rollback/i)).toBeTruthy();
    });
    await userEvent.click(screen.getByText(/Execute Rollback/i));

    const reasonInput = screen.getByPlaceholderText(/reason for rollback/i);
    await userEvent.type(reasonInput, 'Critical bug introduced in v1.2.0');
    const confirmBtns = screen.getAllByText(/Confirm Rollback/i);
    // Click the button (not the title text which may also match)
    await userEvent.click(confirmBtns[confirmBtns.length - 1]);

    await waitFor(() => {
      expect(changeIntelligenceApi.registerRollback).toHaveBeenCalledWith(
        'release-xyz',
        'Critical bug introduced in v1.2.0',
      );
    });
  });
});
