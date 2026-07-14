/**
 * Testes do reshell "browse-first" da ContractCatalogPage (Task 6 + Task 2).
 *
 * Cobrem o comportamento novo:
 *  - A barra de pesquisa do ContractBrowseSurface (searchbox) está visível.
 *  - Existe um controlo de vista Tabela|Cartões (tab "Cards").
 *  - A CTA "New contract" NÃO está presente — criação de contrato nasce do serviço.
 */
import * as React from 'react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen } from '../test-utils';
import { ContractCatalogPage } from '../../features/contracts/catalog/ContractCatalogPage';

vi.mock('../../features/contracts/hooks/useContractList', () => ({
  useContractList: vi.fn(),
  useContractsSummary: vi.fn(),
}));

vi.mock('../../api/client', () => ({
  default: { get: vi.fn(), post: vi.fn() },
}));

// Substituição do hook de permissões — evita dependência do AuthProvider em testes unitários.
vi.mock('../../hooks/usePermissions', () => ({
  usePermissions: vi.fn(() => ({ can: () => true, roleName: 'Admin', permissions: [] })),
}));

import { useContractList, useContractsSummary } from '../../features/contracts/hooks/useContractList';

const oneContract = {
  items: [
    {
      versionId: 'cv-001',
      contractVersionId: 'cv-001',
      apiAssetId: 'order-api',
      name: 'Order API v2',
      protocol: 'OpenApi' as const,
      lifecycleState: 'Published' as const,
      version: '2.0.0',
      domain: 'Commerce',
      teamName: 'Commerce Team',
      criticality: 'High',
      createdAt: '2026-01-01T00:00:00Z',
    },
  ],
  totalCount: 1,
};

describe('ContractCatalogPage — reshell browse-first (Tabela|Cartões)', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(useContractList).mockReturnValue({
      data: oneContract,
      isLoading: false,
      isError: false,
    } as ReturnType<typeof useContractList>);
    vi.mocked(useContractsSummary).mockReturnValue({
      data: {
        totalVersions: 1,
        draftCount: 0,
        inReviewCount: 0,
        approvedCount: 0,
        lockedCount: 0,
        deprecatedCount: 0,
        distinctContracts: 1,
      },
      isLoading: false,
      isError: false,
    } as ReturnType<typeof useContractsSummary>);
  });

  it('renders the browse facet bar search input', () => {
    render(<ContractCatalogPage />);
    expect(screen.getByRole('searchbox')).toBeInTheDocument();
  });

  it('exposes a Table|Cards view toggle', () => {
    render(<ContractCatalogPage />);
    expect(screen.getByRole('tab', { name: /cards/i })).toBeInTheDocument();
  });

  it('não mostra o botão de criação de contrato (contrato nasce do serviço)', () => {
    render(<ContractCatalogPage />);
    expect(screen.queryByRole('button', { name: /new contract/i })).not.toBeInTheDocument();
  });
});
