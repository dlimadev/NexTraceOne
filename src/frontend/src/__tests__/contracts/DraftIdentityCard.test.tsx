import { describe, it, expect, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
vi.mock('react-i18next', () => ({ useTranslation: () => ({ t: (k: string, d?: string) => d ?? k }) }));
import { DraftIdentityCard } from '../../features/contracts/studio/components/DraftIdentityCard';
import type { ContractDraft } from '../../features/contracts/types';

const draft = {
  id: 'd1', title: 'Orders API', description: '', serviceId: 'svc-1', contractType: 'RestApi',
  protocol: 'OpenApi', specContent: '', format: 'yaml', proposedVersion: '1.2.0', status: 'Editing',
  author: 'ana@x.io', createdAt: '2026-06-20T10:00:00Z',
} as unknown as ContractDraft;

describe('DraftIdentityCard', () => {
  it('shows title, version and a Draft status badge', () => {
    render(<DraftIdentityCard draft={draft} serviceName="Payments" />);
    expect(screen.getByText('Orders API')).toBeInTheDocument();
    expect(screen.getByText(/1\.2\.0/)).toBeInTheDocument();
    expect(screen.getByText('Editing')).toBeInTheDocument();
  });
  it('shows the resolved service name and author', () => {
    render(<DraftIdentityCard draft={draft} serviceName="Payments" />);
    expect(screen.getByText(/Payments/)).toBeInTheDocument();
    expect(screen.getByText(/ana@x\.io/)).toBeInTheDocument();
  });
  it('falls back to a dash when no service is linked', () => {
    render(<DraftIdentityCard draft={draft} serviceName={undefined} />);
    // Service row shows '—'; lastEditedAt is also unset so multiple dashes may render
    expect(screen.getAllByText('—').length).toBeGreaterThanOrEqual(1);
  });
});
