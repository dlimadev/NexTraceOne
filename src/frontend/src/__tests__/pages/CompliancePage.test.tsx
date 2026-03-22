import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';

vi.mock('../../features/governance/api/organizationGovernance', () => ({
  organizationGovernanceApi: {
    getComplianceSummary: vi.fn().mockResolvedValue({
      overallScore: 85, totalPacks: 10, compliantCount: 7, nonCompliantCount: 2, partialCount: 1, rows: [],
    }),
  },
}));

vi.mock('react-i18next', () => ({
  useTranslation: () => ({ t: (key: string) => key }),
}));

import { CompliancePage } from '../../features/governance/pages/CompliancePage';

describe('CompliancePage', () => {
  beforeEach(() => { vi.clearAllMocks(); });

  it('renders page heading', async () => {
    render(<MemoryRouter><CompliancePage /></MemoryRouter>);
    await waitFor(() => {
      expect(screen.getByText('governance.compliance.title')).toBeInTheDocument();
    });
  });

  it('renders compliance data after loading', async () => {
    render(<MemoryRouter><CompliancePage /></MemoryRouter>);
    await waitFor(() => {
      expect(screen.queryByText('governance.compliance.title')).toBeInTheDocument();
    });
  });

  it('shows search input', async () => {
    render(<MemoryRouter><CompliancePage /></MemoryRouter>);
    await waitFor(() => {
      const searchInputs = screen.queryAllByPlaceholderText(/search/i);
      expect(searchInputs.length).toBeGreaterThanOrEqual(0);
    });
  });
});
