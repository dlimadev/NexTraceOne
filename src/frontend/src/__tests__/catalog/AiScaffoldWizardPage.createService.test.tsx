import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor, fireEvent } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter, Routes, Route } from 'react-router-dom';
import { AiScaffoldWizardPage } from '../../features/catalog/pages/AiScaffoldWizardPage';

vi.mock('react-i18next', () => ({ useTranslation: () => ({ t: (k: string, f?: unknown) => (typeof f === 'string' ? f : k) }) }));

const navigate = vi.fn();
vi.mock('react-router-dom', async (orig) => ({ ...(await orig() as object), useNavigate: () => navigate }));

const template = {
  id: 'tpl-1', slug: 'rest-dotnet', displayName: 'REST .NET', version: '1.0.0',
  description: 'A REST template', serviceType: 'RestApi', language: 'DotNet',
  defaultDomain: 'Payments', defaultTeam: 'Platform',
  hasBaseContract: true, hasScaffoldingManifest: true,
};
const scaffoldResult = {
  serviceName: 'payment-api', language: 'DotNet', isFallback: false,
  files: [{ path: 'src/Program.cs', content: 'class Program {}' }],
};
const getById = vi.fn().mockResolvedValue(template);
const generateWithAi = vi.fn().mockResolvedValue(scaffoldResult);
vi.mock('../../features/catalog/api/templates', () => ({
  templatesApi: {
    getById: (...a: unknown[]) => getById(...a),
    generateWithAi: (...a: unknown[]) => generateWithAi(...a),
  },
}));

const registerService = vi.fn().mockResolvedValue({ id: 'new-svc-id' });
vi.mock('../../features/catalog/api/serviceCatalog', () => ({
  serviceCatalogApi: { registerService: (...a: unknown[]) => registerService(...a) },
}));

function renderWizard() {
  const qc = new QueryClient({ defaultOptions: { queries: { retry: false }, mutations: { retry: false } } });
  render(
    <QueryClientProvider client={qc}>
      <MemoryRouter initialEntries={['/catalog/templates/tpl-1/scaffold']}>
        <Routes><Route path="/catalog/templates/:id/scaffold" element={<AiScaffoldWizardPage />} /></Routes>
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

/** Conduz o wizard até ao passo review. */
async function driveToReview() {
  renderWizard();
  // Step 1 (template) → Next
  fireEvent.click(await screen.findByRole('button', { name: 'templates.scaffold.next' }));
  // Step 2 (intent) → preencher obrigatórios
  fireEvent.change(screen.getByPlaceholderText('payment-api'), { target: { value: 'payment-api' } });
  fireEvent.change(screen.getByPlaceholderText('templates.scaffold.placeholders.serviceDescription'), { target: { value: 'Handles payments' } });
  // Generate
  fireEvent.click(screen.getByRole('button', { name: 'templates.scaffold.generate' }));
  // Step 4 (review) — botão de criar serviço aparece
  return screen.findByRole('button', { name: 'Create service in catalog' });
}

describe('AiScaffoldWizardPage — criar serviço no catálogo', () => {
  beforeEach(() => { navigate.mockClear(); registerService.mockClear(); generateWithAi.mockClear(); });

  it('mostra a ação de criar serviço no passo de review', async () => {
    const btn = await driveToReview();
    expect(btn).toBeInTheDocument();
  });

  it('cria o serviço com o payload mapeado e navega para o detalhe', async () => {
    const btn = await driveToReview();
    fireEvent.click(btn);
    await waitFor(() => expect(registerService).toHaveBeenCalledWith({
      name: 'payment-api',
      domain: 'Payments',
      team: 'Platform',
      description: 'Handles payments',
      serviceType: 'RestApi',
    }));
    await waitFor(() => expect(navigate).toHaveBeenCalledWith('/services/new-svc-id'));
  });
});
