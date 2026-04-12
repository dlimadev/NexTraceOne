import * as React from 'react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';
import { CreateServicePage } from '../../features/contracts/create/CreateServicePage';

vi.mock('../../features/contracts/api/contractStudio', () => ({
  contractStudioApi: {
    createDraft: vi.fn(),
    getDraft: vi.fn(),
    submitForReview: vi.fn(),
  },
}));

vi.mock('../../features/catalog/api/serviceCatalog', () => ({
  serviceCatalogApi: {
    listServices: vi.fn(),
    getServiceDetail: vi.fn(),
  },
}));

vi.mock('../../contexts/AuthContext', () => ({
  useAuth: vi.fn(() => ({
    user: { id: 'user-1', name: 'Test User', email: 'test@example.com', roles: [] },
    isAuthenticated: true,
  })),
}));

vi.mock('../../api/client', () => ({
  default: { get: vi.fn(), post: vi.fn() },
}));

import { serviceCatalogApi } from '../../features/catalog/api/serviceCatalog';

function renderPage() {
  vi.mocked(serviceCatalogApi.listServices).mockResolvedValue({ services: [], totalCount: 0 } as never);
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter>
        <CreateServicePage />
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

describe('CreateServicePage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('renders without crashing (re-exports CreateContractPage)', () => {
    const { container } = renderPage();
    expect(container).toBeDefined();
  });
});
