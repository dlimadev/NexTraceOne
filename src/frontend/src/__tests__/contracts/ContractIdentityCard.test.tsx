import { describe, it, expect, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import * as React from 'react';
vi.mock('react-i18next', () => ({ useTranslation: () => ({ t: (k: string, d?: string) => d ?? k }) }));
import { ContractIdentityCard } from '../../features/contracts/create/ContractIdentityCard';

const base = { title: '', serviceName: '', type: null, protocol: '', mode: null, proposedVersion: '1.0.0', author: 'me@x.io' };

describe('ContractIdentityCard', () => {
  it('shows placeholder name when empty', () => {
    render(<ContractIdentityCard summary={base} />);
    expect(screen.getByText('novo-contrato')).toBeInTheDocument();
  });
  it('reflects live title and service', () => {
    render(<ContractIdentityCard summary={{ ...base, title: 'Orders API', serviceName: 'Payments' }} />);
    expect(screen.getByText('Orders API')).toBeInTheDocument();
    expect(screen.getByText(/Payments/)).toBeInTheDocument();
  });
  it('always renders a Draft badge in create mode', () => {
    render(<ContractIdentityCard summary={base} />);
    expect(screen.getByText('Draft')).toBeInTheDocument();
  });
});
