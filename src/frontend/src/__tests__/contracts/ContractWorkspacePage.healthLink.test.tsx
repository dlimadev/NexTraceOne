import { describe, it, expect, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import { ContractWorkspacePage } from '../../features/contracts/workspace/ContractWorkspacePage';

vi.mock('react-i18next', () => ({ useTranslation: () => ({ t: (k: string, f?: string) => f ?? k }) }));
// Corta a cadeia de import do Monaco (worker `?worker` não resolve no vitest).
vi.mock('../../features/contracts/workspace/editor/MonacoEditorWrapper', () => ({ MonacoEditorWrapper: () => null }));
// Renderiza só o header do WorkspaceLayout para isolar o teste do resto do workspace.
vi.mock('../../features/contracts/workspace/WorkspaceLayout', () => ({
  WorkspaceLayout: ({ header }: { header: React.ReactNode }) => <div>{header}</div>,
}));
vi.mock('../../features/contracts/workspace/components/ContractLifecycleActions', () => ({
  ContractLifecycleActions: () => null,
}));
vi.mock('../../features/contracts/hooks', () => ({
  useContractDetail: () => ({
    data: { apiAssetId: 'asset-1', protocol: 'OpenApi', semVer: '1.0.0', lifecycleState: 'Approved', isLocked: false, format: 'json' },
    isLoading: false, isError: false, refetch: vi.fn(),
  }),
  useContractViolations: () => ({ data: [] }),
  useContractTransition: () => ({ mutate: vi.fn() }),
  useContractExport: () => ({ exportVersion: vi.fn() }),
}));
vi.mock('../../features/contracts/workspace/toStudioContract', () => ({
  toStudioContract: (d: Record<string, unknown>) => ({ technicalName: 'orders-api', domain: 'Commerce', ...d }),
}));

describe('ContractWorkspacePage health link', () => {
  it('links the header to the contract health timeline', () => {
    render(<MemoryRouter initialEntries={['/contracts/cv-1']}><ContractWorkspacePage /></MemoryRouter>);
    const link = screen.getByRole('link', { name: /health timeline/i });
    expect(link.getAttribute('href')).toBe('/contracts/health/timeline?apiAssetId=asset-1');
  });
});
