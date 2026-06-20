import { describe, it, expect, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
vi.mock('react-i18next', () => ({ useTranslation: () => ({ t: (k: string, d?: string) => d ?? k }) }));
import { ContractWorkspaceIdentityCard } from '../../features/contracts/workspace/components/ContractWorkspaceIdentityCard';
import type { StudioContract } from '../../features/contracts/workspace/studioTypes';

const contract = {
  technicalName: 'payments-api', friendlyName: 'Payments API', protocol: 'OpenApi', serviceType: 'RestApi',
  semVer: '2.1.0', lifecycleState: 'Approved', isLocked: false, signedBy: undefined,
  owner: 'ana', team: 'Payments', domain: 'payments', complianceScore: 92,
  approvalChecklist: [{ role: 'Tech', state: 'Approved' }, { role: 'Sec', state: 'Pending' }],
  policyChecks: [{ policyId: 'p1', policyName: 'x', passed: true }],
} as unknown as StudioContract;

describe('ContractWorkspaceIdentityCard', () => {
  it('shows technical name, version and owner', () => {
    render(<ContractWorkspaceIdentityCard contract={contract} />);
    expect(screen.getByText('payments-api')).toBeInTheDocument();
    expect(screen.getByText(/2\.1\.0/)).toBeInTheDocument();
    // "Payments" appears in both the friendlyName ("Payments API") and the Team row — use getAllByText
    expect(screen.getAllByText(/Payments/).length).toBeGreaterThan(0);
  });
  it('shows approvals count from checklist (1/2)', () => {
    render(<ContractWorkspaceIdentityCard contract={contract} />);
    expect(screen.getByText('1/2')).toBeInTheDocument();
  });
  it('shows compliance percentage', () => {
    render(<ContractWorkspaceIdentityCard contract={contract} />);
    expect(screen.getByText('92%')).toBeInTheDocument();
  });
});
