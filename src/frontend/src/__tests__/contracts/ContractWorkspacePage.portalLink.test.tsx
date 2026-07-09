import { describe, it, expect, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import { MemoryRouter, Routes, Route } from 'react-router-dom';
import { ContractWorkspacePage } from '../../features/contracts/workspace/ContractWorkspacePage';

vi.mock('react-i18next', () => ({ useTranslation: () => ({ t: (k: string, f?: string) => f ?? k }) }));
vi.mock('../../features/contracts/workspace/editor/MonacoEditorWrapper', () => ({ MonacoEditorWrapper: () => null }));
vi.mock('../../features/contracts/workspace/WorkspaceLayout', () => ({
  WorkspaceLayout: ({ header }: { header: React.ReactNode }) => <div>{header}</div>,
}));
vi.mock('../../features/contracts/workspace/components/ContractLifecycleActions', () => ({
  ContractLifecycleActions: () => null,
}));
vi.mock('../../features/contracts/hooks', () => ({
  useContractDetail: () => ({
    data: { apiAssetId: 'a-1', protocol: 'OpenApi', semVer: '1.0.0', lifecycleState: 'Approved', isLocked: false, format: 'json' },
    isLoading: false, isError: false, refetch: vi.fn(),
  }),
  useContractViolations: () => ({ data: [] }),
  useContractTransition: () => ({ mutate: vi.fn() }),
  useContractExport: () => ({ exportVersion: vi.fn() }),
}));
vi.mock('../../features/contracts/workspace/toStudioContract', () => ({
  toStudioContract: (d: Record<string, unknown>) => ({ technicalName: 'orders-api', domain: 'Commerce', ...d }),
}));

describe('ContractWorkspacePage consumer portal link', () => {
  it('links the header to the consumer portal', () => {
    render(
      <MemoryRouter initialEntries={['/contracts/cv-1']}>
        <Routes><Route path="/contracts/:contractVersionId" element={<ContractWorkspacePage />} /></Routes>
      </MemoryRouter>,
    );
    const link = screen.getByRole('link', { name: /consumer portal/i });
    expect(link.getAttribute('href')).toBe('/contracts/portal/cv-1');
  });
});
