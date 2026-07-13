import { describe, it, expect, vi } from 'vitest';
import { screen } from '@testing-library/react';
import { renderWithProviders } from '../test-utils';
import { ContractViewPanel } from '../../features/contracts/components/ContractViewPanel';
import { contractsApi } from '../../features/contracts/api/contracts';

vi.mock('react-i18next', () => ({
  useTranslation: () => ({ t: (k: string, d?: string) => d ?? k }),
  I18nextProvider: ({ children }: { children: React.ReactNode }) => children,
}));

vi.mock('../../features/contracts/api/contracts', () => ({
  contractsApi: {
    getDetail: vi.fn().mockResolvedValue({
      contractVersionId: 'cv-1', apiName: 'Payments API', protocol: 'OpenApi',
      semVer: '2.3.0', lifecycleState: 'Approved', isLocked: false, specContent: 'openapi: 3.0.0',
    }),
  },
}));
vi.mock('../../features/contracts/workspace/sections/ContractSection', () => ({
  ContractSection: ({ specContent }: { specContent: string }) => <div data-testid="spec">{specContent}</div>,
}));

describe('ContractViewPanel', () => {
  it('renders the contract summary and read-only spec', async () => {
    renderWithProviders(<ContractViewPanel contractVersionId="cv-1" />, {
      routerProps: { initialEntries: ['/services/svc-1'] },
    });
    expect(await screen.findByText('Payments API')).toBeInTheDocument();
    expect(screen.getByTestId('spec')).toHaveTextContent('openapi: 3.0.0');
    expect(contractsApi.getDetail).toHaveBeenCalledWith('cv-1');
  });
});
