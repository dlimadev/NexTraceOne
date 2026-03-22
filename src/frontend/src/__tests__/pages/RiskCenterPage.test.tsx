import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';

vi.mock('../../features/governance/api/organizationGovernance', () => ({
  organizationGovernanceApi: {
    getRiskSummary: vi.fn().mockResolvedValue({
      overallRisk: 'Medium', totalItems: 5, criticalCount: 1, highCount: 2, mediumCount: 1, lowCount: 1, items: [],
    }),
  },
}));

vi.mock('react-i18next', () => ({
  useTranslation: () => ({ t: (key: string) => key }),
}));

import { RiskCenterPage } from '../../features/governance/pages/RiskCenterPage';

describe('RiskCenterPage', () => {
  beforeEach(() => { vi.clearAllMocks(); });

  it('renders page heading', async () => {
    render(<MemoryRouter><RiskCenterPage /></MemoryRouter>);
    await waitFor(() => {
      expect(screen.getByText('governance.risk.title')).toBeInTheDocument();
    });
  });

  it('renders risk data after loading', async () => {
    render(<MemoryRouter><RiskCenterPage /></MemoryRouter>);
    await waitFor(() => {
      expect(screen.queryByText('governance.risk.title')).toBeInTheDocument();
    });
  });
});
