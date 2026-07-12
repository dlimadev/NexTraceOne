import { describe, it, expect, vi } from 'vitest';
import { screen } from '@testing-library/react';
import { renderWithProviders } from '../test-utils';
import { ContractDraftEditor } from '../../features/contracts/studio/components/ContractDraftEditor';
import { contractStudioApi } from '../../features/contracts/api/contractStudio';

vi.mock('react-i18next', async (importOriginal) => {
  const actual = await importOriginal<typeof import('react-i18next')>();
  return { ...actual, useTranslation: () => ({ t: (k: string, d?: string) => d ?? k }) };
});

vi.mock('../../features/contracts/api/contractStudio', () => ({
  contractStudioApi: {
    getDraft: vi.fn().mockResolvedValue({
      draftId: 'd-1', title: 'Draft One', protocol: 'OpenApi', contractType: 'RestApi',
      proposedVersion: '1.0.0', status: 'Editing', specContent: 'openapi: 3.0.0', format: 'yaml',
      description: '', serviceId: '',
    }),
    updateContent: vi.fn(),
    updateMetadata: vi.fn(),
    submitForReview: vi.fn(),
  },
}));

// Monaco pesado — o ContractSection é mockado para um textarea simples.
vi.mock('../../features/contracts/workspace/sections/ContractSection', () => ({
  ContractSection: ({ specContent }: { specContent: string }) => <div data-testid="spec">{specContent}</div>,
}));

vi.mock('../../contexts/AuthContext', () => ({
  useAuth: vi.fn(() => ({
    user: { id: 'user-1', email: 'test@example.com', fullName: 'Test User', roles: [] },
    isAuthenticated: true,
  })),
}));

vi.mock('../../features/contracts/hooks/useDraftExport', () => ({
  useDraftExport: vi.fn(() => ({ exportDraft: vi.fn(), isExporting: false, exportError: null })),
}));

vi.mock('../../features/contracts/hooks/useDraftValidation', () => ({
  useDraftValidation: vi.fn(() => ({
    state: { summary: { totalIssues: 0 } },
    isRunning: false,
    validateAll: vi.fn(),
  })),
}));

vi.mock('../../features/catalog/api/serviceCatalog', () => ({
  serviceCatalogApi: {
    listServices: vi.fn().mockResolvedValue({ items: [] }),
  },
}));

vi.mock('../../api/client', () => ({
  default: { get: vi.fn(), post: vi.fn(), put: vi.fn(), delete: vi.fn() },
}));

describe('ContractDraftEditor', () => {
  it('renders the editor tabs and spec content for a draftId', async () => {
    renderWithProviders(<ContractDraftEditor draftId="d-1" />);
    expect(await screen.findByTestId('spec')).toHaveTextContent('openapi: 3.0.0');
    // A tab Spec está presente
    expect(screen.getByText('Spec')).toBeInTheDocument();
    expect(contractStudioApi.getDraft).toHaveBeenCalledWith('d-1');
  });
});
