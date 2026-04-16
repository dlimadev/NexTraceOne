import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { MultiTenantSchemaPage } from '../../features/platform-admin/pages/MultiTenantSchemaPage';
import { platformAdminApi } from '../../features/platform-admin/api/platformAdmin';
import type {
  TenantSchemasResponse,
  ProvisionTenantSchemaResult,
} from '../../features/platform-admin/api/platformAdmin';

vi.mock('react-i18next', () => ({
  useTranslation: () => ({ t: (key: string) => key, i18n: { language: 'en' } }),
}));

vi.mock('../../features/platform-admin/api/platformAdmin', () => ({
  platformAdminApi: {
    getTenantSchemas: vi.fn(),
    provisionTenantSchema: vi.fn(),
  },
}));

import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { ReactNode } from 'react';
const qc = new QueryClient({ defaultOptions: { queries: { retry: false } } });
const Wrapper = ({ children }: { children: ReactNode }) => (
  <QueryClientProvider client={qc}>{children}</QueryClientProvider>
);

const mockData: TenantSchemasResponse = {
  totalSchemas: 2,
  checkedAt: '2026-04-15T12:00:00Z',
  schemas: [
    {
      tenantSlug: 'acme-corp',
      schemaName: 'tenant_acme_corp',
      searchPath: 'tenant_acme_corp, public',
    },
    {
      tenantSlug: 'globex',
      schemaName: 'tenant_globex',
      searchPath: 'tenant_globex, public',
    },
  ],
};

const mockEmptyData: TenantSchemasResponse = {
  totalSchemas: 0,
  checkedAt: '2026-04-15T12:00:00Z',
  schemas: [],
};

const mockProvisionResult: ProvisionTenantSchemaResult = {
  tenantSlug: 'new-tenant',
  schemaName: 'tenant_new_tenant',
  wasCreated: true,
  provisionedAt: '2026-04-15T13:00:00Z',
};

describe('MultiTenantSchemaPage', () => {
  beforeEach(() => {
    qc.clear();
    vi.clearAllMocks();
  });

  it('shows loading state', () => {
    vi.mocked(platformAdminApi.getTenantSchemas).mockImplementation(
      () => new Promise(() => {}),
    );
    render(<MultiTenantSchemaPage />, { wrapper: Wrapper });
    expect(screen.getByText('loading')).toBeDefined();
  });

  it('shows error state', async () => {
    vi.mocked(platformAdminApi.getTenantSchemas).mockRejectedValue(
      new Error('fail'),
    );
    render(<MultiTenantSchemaPage />, { wrapper: Wrapper });
    await waitFor(() => expect(screen.getByText('error')).toBeDefined());
  });

  it('renders page title and subtitle', async () => {
    vi.mocked(platformAdminApi.getTenantSchemas).mockResolvedValue(mockData);
    render(<MultiTenantSchemaPage />, { wrapper: Wrapper });
    await waitFor(() => {
      expect(screen.getByText('title')).toBeDefined();
      expect(screen.getByText('subtitle')).toBeDefined();
    });
  });

  it('renders info banner', async () => {
    vi.mocked(platformAdminApi.getTenantSchemas).mockResolvedValue(mockData);
    render(<MultiTenantSchemaPage />, { wrapper: Wrapper });
    await waitFor(() =>
      expect(screen.getByText('schemaBanner')).toBeDefined(),
    );
  });

  it('renders summary cards', async () => {
    vi.mocked(platformAdminApi.getTenantSchemas).mockResolvedValue(mockData);
    render(<MultiTenantSchemaPage />, { wrapper: Wrapper });
    await waitFor(() => {
      expect(screen.getByText('totalSchemas')).toBeDefined();
      expect(screen.getByText('isolationMode')).toBeDefined();
      expect(screen.getByText('schemaPerTenant')).toBeDefined();
    });
  });

  it('renders provision form', async () => {
    vi.mocked(platformAdminApi.getTenantSchemas).mockResolvedValue(mockData);
    render(<MultiTenantSchemaPage />, { wrapper: Wrapper });
    await waitFor(() => {
      expect(screen.getByText('provisionTitle')).toBeDefined();
      expect(screen.getByPlaceholderText('slugPlaceholder')).toBeDefined();
      expect(screen.getByText('provision')).toBeDefined();
    });
  });

  it('renders schema table with tenant slugs', async () => {
    vi.mocked(platformAdminApi.getTenantSchemas).mockResolvedValue(mockData);
    render(<MultiTenantSchemaPage />, { wrapper: Wrapper });
    await waitFor(() => {
      expect(screen.getByText('acme-corp')).toBeDefined();
      expect(screen.getByText('globex')).toBeDefined();
    });
  });

  it('renders schema names in table', async () => {
    vi.mocked(platformAdminApi.getTenantSchemas).mockResolvedValue(mockData);
    render(<MultiTenantSchemaPage />, { wrapper: Wrapper });
    await waitFor(() => {
      expect(screen.getByText('tenant_acme_corp')).toBeDefined();
      expect(screen.getByText('tenant_globex')).toBeDefined();
    });
  });

  it('shows empty state when no schemas', async () => {
    vi.mocked(platformAdminApi.getTenantSchemas).mockResolvedValue(
      mockEmptyData,
    );
    render(<MultiTenantSchemaPage />, { wrapper: Wrapper });
    await waitFor(() =>
      expect(screen.getByText('noSchemas')).toBeDefined(),
    );
  });

  it('shows validation error for empty slug', async () => {
    vi.mocked(platformAdminApi.getTenantSchemas).mockResolvedValue(mockData);
    render(<MultiTenantSchemaPage />, { wrapper: Wrapper });
    await waitFor(() => expect(screen.getByText('provision')).toBeDefined());

    await userEvent.click(screen.getByText('provision'));

    await waitFor(() =>
      expect(screen.getByText('slugRequired')).toBeDefined(),
    );
  });

  it('shows validation error for invalid slug characters', async () => {
    vi.mocked(platformAdminApi.getTenantSchemas).mockResolvedValue(mockData);
    render(<MultiTenantSchemaPage />, { wrapper: Wrapper });
    await waitFor(() => expect(screen.getByPlaceholderText('slugPlaceholder')).toBeDefined());

    await userEvent.type(
      screen.getByPlaceholderText('slugPlaceholder'),
      'INVALID Slug!',
    );
    await userEvent.click(screen.getByText('provision'));

    await waitFor(() =>
      expect(screen.getByText('slugInvalid')).toBeDefined(),
    );
  });

  it('calls provisionTenantSchema with valid slug', async () => {
    vi.mocked(platformAdminApi.getTenantSchemas).mockResolvedValue(mockData);
    vi.mocked(platformAdminApi.provisionTenantSchema).mockResolvedValue(
      mockProvisionResult,
    );
    render(<MultiTenantSchemaPage />, { wrapper: Wrapper });
    await waitFor(() => expect(screen.getByPlaceholderText('slugPlaceholder')).toBeDefined());

    await userEvent.type(
      screen.getByPlaceholderText('slugPlaceholder'),
      'new-tenant',
    );
    await userEvent.click(screen.getByText('provision'));

    await waitFor(() =>
      expect(
        vi.mocked(platformAdminApi.provisionTenantSchema),
      ).toHaveBeenCalledWith({ tenantSlug: 'new-tenant' }),
    );
  });

  it('shows success message after provision', async () => {
    vi.mocked(platformAdminApi.getTenantSchemas).mockResolvedValue(mockData);
    vi.mocked(platformAdminApi.provisionTenantSchema).mockResolvedValue(
      mockProvisionResult,
    );
    render(<MultiTenantSchemaPage />, { wrapper: Wrapper });
    await waitFor(() => expect(screen.getByPlaceholderText('slugPlaceholder')).toBeDefined());

    await userEvent.type(
      screen.getByPlaceholderText('slugPlaceholder'),
      'new-tenant',
    );
    await userEvent.click(screen.getByText('provision'));

    await waitFor(() =>
      expect(screen.getByText('provisionSuccess')).toBeDefined(),
    );
  });

  it('shows error after provision failure', async () => {
    vi.mocked(platformAdminApi.getTenantSchemas).mockResolvedValue(mockData);
    vi.mocked(platformAdminApi.provisionTenantSchema).mockRejectedValue(
      new Error('server error'),
    );
    render(<MultiTenantSchemaPage />, { wrapper: Wrapper });
    await waitFor(() => expect(screen.getByPlaceholderText('slugPlaceholder')).toBeDefined());

    await userEvent.type(
      screen.getByPlaceholderText('slugPlaceholder'),
      'valid-slug',
    );
    await userEvent.click(screen.getByText('provision'));

    await waitFor(() =>
      expect(screen.getByText('provisionError')).toBeDefined(),
    );
  });

  it('renders column headers', async () => {
    vi.mocked(platformAdminApi.getTenantSchemas).mockResolvedValue(mockData);
    render(<MultiTenantSchemaPage />, { wrapper: Wrapper });
    await waitFor(() => {
      expect(screen.getByText('colTenantSlug')).toBeDefined();
      expect(screen.getByText('colSchemaName')).toBeDefined();
      expect(screen.getByText('colSearchPath')).toBeDefined();
    });
  });

  it('renders refresh button', async () => {
    vi.mocked(platformAdminApi.getTenantSchemas).mockResolvedValue(mockData);
    render(<MultiTenantSchemaPage />, { wrapper: Wrapper });
    await waitFor(() =>
      expect(screen.getByText('refresh')).toBeDefined(),
    );
  });
});
