import { describe, it, expect, vi, beforeEach } from 'vitest';
import { renderHook, act, waitFor } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';
import type { ReactNode } from 'react';
import { useOnboardWizard } from '../../features/catalog/onboard/useOnboardWizard';

const navigate = vi.fn();
vi.mock('react-router-dom', async (orig) => {
  const actual = await orig<typeof import('react-router-dom')>();
  return { ...actual, useNavigate: () => navigate };
});
vi.mock('react-i18next', () => ({ useTranslation: () => ({ t: (_k: string, d?: string) => d ?? _k }) }));

// useContractDraftForm depende de useAuth; sem provider o hook lança.
vi.mock('../../contexts/AuthContext', () => ({
  useAuth: () => ({ user: { email: 'tester@acme.com' } }),
}));

const registerService = vi.fn();
vi.mock('../../features/catalog/api', () => ({
  serviceCatalogApi: {
    registerService: (...a: unknown[]) => registerService(...a),
    createServiceInterface: vi.fn(() => Promise.resolve({})),
    listServices: vi.fn(() => Promise.resolve({ items: [] })),
  },
}));
// useContractDraftForm consome ../../catalog/api/serviceCatalog no path real; stub coerente.
vi.mock('../../features/catalog/api/serviceCatalog', () => ({
  serviceCatalogApi: { listServices: vi.fn(() => Promise.resolve({ items: [] })) },
}));

function wrapper({ children }: { children: ReactNode }) {
  const qc = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return (
    <QueryClientProvider client={qc}>
      <MemoryRouter>{children}</MemoryRouter>
    </QueryClientProvider>
  );
}

describe('useOnboardWizard', () => {
  beforeEach(() => { navigate.mockReset(); registerService.mockReset(); });

  it('blocks Next on step 1 until required fields valid', () => {
    const { result } = renderHook(() => useOnboardWizard(), { wrapper });
    expect(result.current.canGoNext).toBe(false);
    act(() => {
      result.current.setIdentityField('name', 'orders');
      result.current.setIdentityField('domain', 'Commerce');
      result.current.setIdentityField('teamName', 'Orders');
    });
    expect(result.current.canGoNext).toBe(true);
  });

  it('creates the service on leaving step 1 and advances to interface', async () => {
    registerService.mockResolvedValue({ id: 'svc-1' });
    const { result } = renderHook(() => useOnboardWizard(), { wrapper });
    act(() => {
      result.current.setIdentityField('name', 'orders');
      result.current.setIdentityField('domain', 'Commerce');
      result.current.setIdentityField('teamName', 'Orders');
    });
    act(() => { result.current.onNext(); });
    await waitFor(() => expect(registerService).toHaveBeenCalledTimes(1));
    await waitFor(() => expect(result.current.serviceId).toBe('svc-1'));
    expect(result.current.activeStep).toBe('interface');
  });

  it('finishes from review by navigating to the service page', async () => {
    registerService.mockResolvedValue({ id: 'svc-9' });
    const { result } = renderHook(() => useOnboardWizard(), { wrapper });
    act(() => {
      result.current.setIdentityField('name', 'o');
      result.current.setIdentityField('domain', 'd');
      result.current.setIdentityField('teamName', 't');
    });
    act(() => { result.current.onNext(); });           // create → interface
    await waitFor(() => expect(result.current.serviceId).toBe('svc-9'));
    act(() => { result.current.onSkip(); });            // skip interface → contract
    act(() => { result.current.onSkip(); });            // skip contract → review
    expect(result.current.activeStep).toBe('review');
    act(() => { result.current.onNext(); });            // finish
    expect(navigate).toHaveBeenCalledWith('/services/svc-9');
  });
});
