import { describe, it, expect, vi } from 'vitest';
import { Suspense } from 'react';
import { screen } from '@testing-library/react';
import { Routes } from 'react-router-dom';
import { renderWithProviders } from '../test-utils';
import { CatalogRoutes } from '../../routes/catalogRoutes';
import { ContractsRoutes } from '../../routes/contractsRoutes';

// ProtectedRoute passa os filhos à frente (permissões fora do âmbito deste teste).
vi.mock('../../components/ProtectedRoute', () => ({
  ProtectedRoute: ({ children }: { children: React.ReactNode }) => <>{children}</>,
}));

// Destinos pesados substituídos por marcadores — só o comportamento de redirect é testado.
vi.mock('../../features/catalog/pages/DeveloperExperienceScorePage', () => ({
  DeveloperExperienceScorePage: () => <div data-testid="dx-page">DX</div>,
}));
vi.mock('../../features/catalog/pages/ContractPipelinePage', () => ({
  ContractPipelinePage: () => <div data-testid="pipeline-page">PIPE</div>,
}));

function renderAt(path: string) {
  return renderWithProviders(
    <Suspense fallback={<div>loading</div>}>
      <Routes>
        {CatalogRoutes()}
        {ContractsRoutes()}
      </Routes>
    </Suspense>,
    { routerProps: { initialEntries: [path] } },
  );
}

describe('catalog navigation redirects', () => {
  it('redirects /catalog/developer-experience-score to /services/experience', async () => {
    renderAt('/catalog/developer-experience-score');
    expect(await screen.findByTestId('dx-page')).toBeInTheDocument();
  });

  it('redirects /catalog/contracts/pipeline to /contracts/pipeline', async () => {
    renderAt('/catalog/contracts/pipeline');
    expect(await screen.findByTestId('pipeline-page')).toBeInTheDocument();
  });
});
