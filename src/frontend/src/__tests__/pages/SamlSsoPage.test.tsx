import { render, screen, waitFor } from '@testing-library/react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { SamlSsoPage } from '../../features/platform-admin/pages/SamlSsoPage';
import { platformAdminApi } from '../../features/platform-admin/api/platformAdmin';
import type { SamlSsoConfig } from '../../features/platform-admin/api/platformAdmin';

vi.mock('react-i18next', () => ({
  useTranslation: () => ({ t: (key: string) => key, i18n: { language: 'en' } }),
}));

vi.mock('../../features/platform-admin/api/platformAdmin', () => ({
  platformAdminApi: {
    getSamlSsoConfig: vi.fn(),
    updateSamlSsoConfig: vi.fn(),
    testSamlConnection: vi.fn(),
  },
}));

import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { ReactNode } from 'react';
const qc = new QueryClient({ defaultOptions: { queries: { retry: false } } });
const Wrapper = ({ children }: { children: ReactNode }) => (
  <QueryClientProvider client={qc}>{children}</QueryClientProvider>
);

const mockConfig: SamlSsoConfig = {
  status: 'NotConfigured',
  entityId: 'https://nextraceone.example.com/saml/metadata',
  ssoUrl: 'https://idp.example.com/saml/sso',
  sloUrl: 'https://idp.example.com/saml/slo',
  idpCertificate: '-----BEGIN CERTIFICATE-----\nMIICxyz\n-----END CERTIFICATE-----',
  jitProvisioningEnabled: false,
  defaultRole: 'Engineer',
  attributeMappings: [
    { samlAttr: 'email', nxtField: 'email' },
    { samlAttr: 'displayName', nxtField: 'name' },
  ],
  lastTestedAt: undefined,
  testResult: null,
  simulatedNote: 'Simulated SAML SSO config data',
};

describe('SamlSsoPage', () => {
  beforeEach(() => {
    qc.clear();
    vi.clearAllMocks();
  });

  it('shows loading state', () => {
    vi.mocked(platformAdminApi.getSamlSsoConfig).mockImplementation(() => new Promise(() => {}));
    render(<SamlSsoPage />, { wrapper: Wrapper });
    expect(screen.getByText('loading')).toBeDefined();
  });

  it('shows error state', async () => {
    vi.mocked(platformAdminApi.getSamlSsoConfig).mockRejectedValue(new Error('fail'));
    render(<SamlSsoPage />, { wrapper: Wrapper });
    await waitFor(() => expect(screen.getByText('error')).toBeDefined());
  });

  it('renders page title', async () => {
    vi.mocked(platformAdminApi.getSamlSsoConfig).mockResolvedValue(mockConfig);
    render(<SamlSsoPage />, { wrapper: Wrapper });
    await waitFor(() => expect(screen.getByText('title')).toBeDefined());
  });

  it('renders warning banner', async () => {
    vi.mocked(platformAdminApi.getSamlSsoConfig).mockResolvedValue(mockConfig);
    render(<SamlSsoPage />, { wrapper: Wrapper });
    await waitFor(() => expect(screen.getByText('warningBanner')).toBeDefined());
  });

  it('renders SSO status', async () => {
    vi.mocked(platformAdminApi.getSamlSsoConfig).mockResolvedValue(mockConfig);
    render(<SamlSsoPage />, { wrapper: Wrapper });
    await waitFor(() => expect(screen.getByText('labelStatus:')).toBeDefined());
  });

  it('renders configuration form sections', async () => {
    vi.mocked(platformAdminApi.getSamlSsoConfig).mockResolvedValue(mockConfig);
    render(<SamlSsoPage />, { wrapper: Wrapper });
    await waitFor(() => {
      expect(screen.getByText('sectionConfig')).toBeDefined();
      expect(screen.getByText('sectionAttrMapping')).toBeDefined();
    });
  });

  it('renders attribute mappings table', async () => {
    vi.mocked(platformAdminApi.getSamlSsoConfig).mockResolvedValue(mockConfig);
    render(<SamlSsoPage />, { wrapper: Wrapper });
    await waitFor(() => {
      expect(screen.getByText('colSamlAttr')).toBeDefined();
      expect(screen.getByText('colNxtField')).toBeDefined();
    });
  });

  it('shows simulated note', async () => {
    vi.mocked(platformAdminApi.getSamlSsoConfig).mockResolvedValue(mockConfig);
    render(<SamlSsoPage />, { wrapper: Wrapper });
    await waitFor(() => expect(screen.getByText(/Simulated SAML SSO config data/)).toBeDefined());
  });
});
