import * as React from 'react';
import { describe, it, expect, vi } from 'vitest';
import { render, waitFor } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter, Routes, Route } from 'react-router-dom';
import { ContractWorkspacePage } from '../../features/contracts/workspace/ContractWorkspacePage';

vi.mock('react-i18next', () => ({ useTranslation: () => ({ t: (k: string, d?: string) => (typeof d === 'string' ? d : k) }) }));
vi.mock('../../contexts/EnvironmentContext', () => ({ useEnvironment: () => ({ activeEnvironmentId: 'env-1' }) }));
vi.mock('../../features/contracts/workspace/editor/MonacoEditorWrapper', () => ({ MonacoEditorWrapper: () => null }));
vi.mock('../../features/contracts/workspace/WorkspaceLayout', () => ({
  WorkspaceLayout: ({ header }: { header: React.ReactNode }) => <div>{header}</div>,
}));
vi.mock('../../features/contracts/workspace/components/ContractLifecycleActions', () => ({
  ContractLifecycleActions: () => null,
}));
vi.mock('../../features/contracts/workspace/toStudioContract', () => ({
  toStudioContract: () => ({ technicalName: 'orders-api', domain: 'Commerce' }),
}));

const detail = {
  apiAssetId: 'a-1', semVer: '1.0.0', protocol: 'OpenApi', format: 'json',
  lifecycleState: 'Draft', isLocked: false,
};
vi.mock('../../features/contracts/hooks', () => ({
  useContractDetail: () => ({ data: detail, isLoading: false, isError: false, refetch: vi.fn() }),
  useContractViolations: () => ({ data: [] }),
  useContractTransition: () => ({ mutate: vi.fn() }),
  useContractExport: () => ({ exportVersion: vi.fn() }),
}));

function renderAt(id: string) {
  const qc = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  render(
    <QueryClientProvider client={qc}>
      <MemoryRouter initialEntries={[`/contracts/${id}`]}>
        <Routes><Route path="/contracts/:contractVersionId" element={<ContractWorkspacePage />} /></Routes>
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

describe('ContractWorkspacePage source-of-truth link', () => {
  it('liga à vista SoT consolidada do contrato', async () => {
    renderAt('cv-1');
    const link = await waitFor(() => {
      const a = document.querySelector('a[href="/source-of-truth/contracts/cv-1"]');
      if (!a) throw new Error('SoT link ainda não renderizado');
      return a;
    });
    expect(link).toHaveTextContent('View source of truth');
  });
});
