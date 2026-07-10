import { describe, it, expect, vi } from 'vitest';
import { render, screen, waitFor, fireEvent } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import ServiceMaturityPage from '../../features/catalog/pages/ServiceMaturityPage';

vi.mock('react-i18next', () => ({ useTranslation: () => ({ t: (k: string, f?: unknown) => (typeof f === 'string' ? f : k) }) }));
vi.mock('../../contexts/EnvironmentContext', () => ({ useEnvironment: () => ({ activeEnvironmentId: 'env-1' }) }));

const maturity = {
  summary: { totalServices: 1, averageScore: 0.8, withoutOwnership: 0, withoutContracts: 0, withoutDocumentation: 0, withoutRunbooks: 0, initial: 0, developing: 0, defined: 0, managed: 1, optimizing: 0 },
  services: [{ serviceId: 'svc-9', serviceName: 'orders-api', displayName: 'Orders API', teamName: 'Orders', domain: 'Commerce', level: 'Managed', overallScore: 0.8, hasOwnership: true, hasContracts: true, hasDocumentation: true, hasRepository: true, hasMonitoring: true, hasRunbook: true }],
};
const audit = {
  summary: { totalServicesAudited: 1, healthyServices: 0, servicesWithIssues: 1, criticalFindings: 1, withoutTeam: 0, apisWithoutContracts: 1 },
  findings: [{ serviceId: 'svc-9', serviceName: 'orders-api', displayName: 'Orders API', teamName: 'Orders', domain: 'Commerce', severity: 'high', findings: ['noContracts:1'] }],
};
const getMaturityDashboard = vi.fn().mockResolvedValue(maturity);
const getOwnershipAudit = vi.fn().mockResolvedValue(audit);
vi.mock('../../features/catalog/api/serviceCatalog', () => ({
  serviceCatalogApi: {
    getMaturityDashboard: (...a: unknown[]) => getMaturityDashboard(...a),
    getOwnershipAudit: (...a: unknown[]) => getOwnershipAudit(...a),
  },
}));

function renderPage() {
  const qc = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  render(
    <QueryClientProvider client={qc}>
      <MemoryRouter><ServiceMaturityPage /></MemoryRouter>
    </QueryClientProvider>,
  );
}

describe('ServiceMaturityPage drill-through', () => {
  it('linha de maturity liga ao detalhe do serviço', async () => {
    renderPage();
    const link = await waitFor(() => screen.getByRole('link', { name: 'Open service' }));
    expect(link).toHaveAttribute('href', '/services/svc-9');
  });

  it('abrir-serviço não alterna o expand/collapse da linha', async () => {
    renderPage();
    const link = await waitFor(() => screen.getByRole('link', { name: 'Open service' }));
    // a dimensão só aparece após expand; clicar no link não a deve mostrar
    fireEvent.click(link);
    expect(screen.queryByText('serviceMaturity.dim.ownership')).not.toBeInTheDocument();
  });

  it('finding de auditoria liga ao detalhe do serviço', async () => {
    renderPage();
    fireEvent.click(await waitFor(() => screen.getByText('serviceMaturity.tabs.audit')));
    const links = await waitFor(() => screen.getAllByRole('link', { name: 'Open service' }));
    expect(links[0]).toHaveAttribute('href', '/services/svc-9');
  });
});
