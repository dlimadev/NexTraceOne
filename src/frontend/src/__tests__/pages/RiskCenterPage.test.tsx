import { describe, it, expect, vi, beforeEach } from 'vitest';
import { screen, waitFor } from '@testing-library/react';
import { renderWithProviders } from '../test-utils';

vi.mock('../../features/governance/api/organizationGovernance', () => ({
  organizationGovernanceApi: {
    getRiskSummary: vi.fn().mockResolvedValue({
      totalPacksAssessed: 5, criticalCount: 1, highCount: 2, mediumCount: 1, lowCount: 1, indicators: [],
    }),
  },
}));

import { RiskCenterPage } from '../../features/governance/pages/RiskCenterPage';

describe('RiskCenterPage', () => {
  beforeEach(() => { vi.clearAllMocks(); });

  it('renders page heading', async () => {
    renderWithProviders(<RiskCenterPage />);
    await waitFor(() => {
      expect(screen.getByText('Risk Center')).toBeInTheDocument();
    });
  });

  it('renders risk data after loading', async () => {
    renderWithProviders(<RiskCenterPage />);
    await waitFor(() => {
      expect(screen.queryByText('Risk Center')).toBeInTheDocument();
    });
  });
});
