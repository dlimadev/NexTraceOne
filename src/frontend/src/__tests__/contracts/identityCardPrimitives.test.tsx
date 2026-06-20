import { describe, it, expect } from 'vitest';
import { render, screen } from '@testing-library/react';
import { IdentityMiniStat, IdentityMetaRow } from '../../features/contracts/shared/components/identityCardPrimitives';

describe('identityCardPrimitives', () => {
  it('IdentityMiniStat shows value and label', () => {
    render(<IdentityMiniStat value="3/3" label="Approvals" />);
    expect(screen.getByText('3/3')).toBeInTheDocument();
    expect(screen.getByText('Approvals')).toBeInTheDocument();
  });
  it('IdentityMetaRow shows label and value', () => {
    render(<IdentityMetaRow label="Owner" value="@ana" />);
    expect(screen.getByText('Owner')).toBeInTheDocument();
    expect(screen.getByText('@ana')).toBeInTheDocument();
  });
});
