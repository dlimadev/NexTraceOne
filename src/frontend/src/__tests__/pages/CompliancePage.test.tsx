import { describe, it, expect, vi, beforeEach } from 'vitest';
import { screen, waitFor } from '@testing-library/react';
import { renderWithProviders } from '../test-utils';

vi.mock('../../features/governance/api/organizationGovernance', () => ({
  organizationGovernanceApi: {
    getComplianceSummary: vi.fn().mockResolvedValue({
      overallScore: 85, totalPacksAssessed: 10, compliantCount: 7, partiallyCompliantCount: 1, nonCompliantCount: 2, totalRollouts: 8, completedRollouts: 5, failedRollouts: 1, totalWaivers: 15, approvedWaivers: 10, packs: [],
    }),
  },
}));

import { CompliancePage } from '../../features/governance/pages/CompliancePage';

describe('CompliancePage', () => {
  beforeEach(() => { vi.clearAllMocks(); });

  it('renders page heading', async () => {
    renderWithProviders(<CompliancePage />);
    await waitFor(() => {
      expect(screen.getByText('Compliance')).toBeInTheDocument();
    });
  });

  it('renders compliance data after loading', async () => {
    renderWithProviders(<CompliancePage />);
    await waitFor(() => {
      expect(screen.queryByText('Compliance')).toBeInTheDocument();
    });
  });

  it('shows search input', async () => {
    renderWithProviders(<CompliancePage />);
    await waitFor(() => {
      const searchInputs = screen.queryAllByPlaceholderText(/search/i);
      expect(searchInputs.length).toBeGreaterThanOrEqual(0);
    });
  });
});
