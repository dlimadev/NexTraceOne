import { describe, it, expect, vi } from 'vitest';
import { render, screen, fireEvent } from '@testing-library/react';
import { ServiceSetupChecklist } from '../../features/catalog/components/ServiceSetupChecklist';

vi.mock('react-i18next', () => ({
  useTranslation: () => ({
    t: (k: string, o?: Record<string, unknown>) =>
      (o && typeof o === 'object' && 'done' in o) ? `${o.done}/${o.total}` : k,
  }),
}));

const base = {
  service: { serviceType: 'RestApi', apis: [] as unknown[] },
  contractCount: 0,
  lifecycleStatus: 'Planning',
  onEditOwnership: vi.fn(),
  onEditReferences: vi.fn(),
  onAddInterface: vi.fn(),
  onAddContract: vi.fn(),
};

describe('ServiceSetupChecklist', () => {
  it('fires the contract CTA for an incomplete applicable item', () => {
    const onAddContract = vi.fn();
    render(<ServiceSetupChecklist {...base} onAddContract={onAddContract} />);
    fireEvent.click(screen.getByTestId('setup-cta-contract'));
    expect(onAddContract).toHaveBeenCalled();
  });

  it('shows a completion note when all applicable items are done and not Active', () => {
    render(
      <ServiceSetupChecklist
        {...base}
        service={{ serviceType: 'RestApi', apis: [{}], technicalOwner: 'a', repositoryUrl: 'r', documentationUrl: 'd' }}
        contractCount={1}
      />,
    );
    expect(screen.getByText('serviceSetup.complete')).toBeInTheDocument();
  });
});
