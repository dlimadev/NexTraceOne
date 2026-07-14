import { describe, it, expect, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import { MemoryRouter, Routes, Route } from 'react-router-dom';
import { Suspense } from 'react';

// Mock ProtectedRoute para passar sempre (sem auth) e as páginas lazy alvo do catálogo
vi.mock('../../components/ProtectedRoute', () => ({
  ProtectedRoute: ({ children }: { children: React.ReactNode }) => <>{children}</>,
}));
vi.mock('../../features/contracts/catalog/ContractCatalogPage', () => ({
  ContractCatalogPage: () => <div>CONTRACT_CATALOG</div>,
}));

import { ContractsRoutes } from '../../routes/contractsRoutes';

function renderAt(path: string) {
  return render(
    <MemoryRouter initialEntries={[path]}>
      <Suspense fallback={<div>loading</div>}>
        <Routes>{ContractsRoutes()}</Routes>
      </Suspense>
    </MemoryRouter>,
  );
}

describe('rotas de criação de contrato redirecionam para o catálogo', () => {
  it('/contracts/new redireciona para /contracts', async () => {
    renderAt('/contracts/new');
    expect(await screen.findByText('CONTRACT_CATALOG')).toBeInTheDocument();
  });

  it('/contracts/studio/new redireciona para /contracts', async () => {
    renderAt('/contracts/studio/new');
    expect(await screen.findByText('CONTRACT_CATALOG')).toBeInTheDocument();
  });
});
