import { describe, it, expect, vi, beforeEach } from 'vitest';
import { screen, waitFor } from '@testing-library/react';
import { renderWithProviders } from '../test-utils';
import { LicensingPage } from '../../features/saas/pages/LicensingPage';

// Mock the axios client used by saasApi
vi.mock('../../api/client', () => ({
  default: {
    get: vi.fn(),
    post: vi.fn(),
    patch: vi.fn(),
    put: vi.fn(),
    delete: vi.fn(),
  },
}));

// Mock saasApi directly — controls what getLicense and provisionLicense return
vi.mock('../../features/saas/api/saasApi', async (importOriginal) => {
  const actual = await importOriginal<typeof import('../../features/saas/api/saasApi')>();
  return {
    ...actual,
    saasApi: {
      ...actual.saasApi,
      getLicense: vi.fn(),
      provisionLicense: vi.fn(),
    },
  };
});

import { saasApi } from '../../features/saas/api/saasApi';

const mockLicense = {
  tenantId: 'tenant-123',
  plan: 'Professional' as const,
  status: 'Active' as const,
  includedHostUnits: 50,
  currentHostUnits: 12.5,
  overageHostUnits: 0,
  validFrom: '2026-01-01T00:00:00Z',
  validUntil: '2027-01-01T00:00:00Z',
  billingCycleStart: '2026-01-01T00:00:00Z',
  capabilities: ['change_governance', 'service_catalog', 'ai_hub'],
};

describe('LicensingPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('renders page heading', async () => {
    vi.mocked(saasApi.getLicense).mockResolvedValue(mockLicense);
    renderWithProviders(<LicensingPage />);
    // PageHeader renders the title as an h1 — wait for it to settle
    await waitFor(() => {
      expect(screen.getByRole('heading', { level: 1 })).toBeInTheDocument();
    });
  });

  it('displays license data when loaded successfully', async () => {
    vi.mocked(saasApi.getLicense).mockResolvedValue(mockLicense);
    renderWithProviders(<LicensingPage />);

    await waitFor(() => {
      // Current plan badge
      expect(screen.getByText('Professional')).toBeInTheDocument();
      // Status badge
      expect(screen.getByText('Active')).toBeInTheDocument();
    });
  });

  it('shows capabilities after loading', async () => {
    vi.mocked(saasApi.getLicense).mockResolvedValue(mockLicense);
    renderWithProviders(<LicensingPage />);

    await waitFor(() => {
      expect(screen.getByText('change_governance')).toBeInTheDocument();
      expect(screen.getByText('service_catalog')).toBeInTheDocument();
    });
  });

  it('shows upgrade options for non-Enterprise plans', async () => {
    vi.mocked(saasApi.getLicense).mockResolvedValue(mockLicense);
    renderWithProviders(<LicensingPage />);

    await waitFor(() => {
      // Professional plan can be upgraded to Enterprise
      expect(screen.getByText(/enterprise/i)).toBeInTheDocument();
    });
  });

  it('does not show upgrade section for Enterprise plan', async () => {
    vi.mocked(saasApi.getLicense).mockResolvedValue({
      ...mockLicense,
      plan: 'Enterprise',
    });
    renderWithProviders(<LicensingPage />);

    await waitFor(() => {
      expect(screen.getByText('Enterprise')).toBeInTheDocument();
    });

    // No upgrade section should be present
    expect(screen.queryByText(/upgrade to/i)).not.toBeInTheDocument();
  });

  it('renders without crashing on API error', async () => {
    vi.mocked(saasApi.getLicense).mockRejectedValue(new Error('Network error'));
    renderWithProviders(<LicensingPage />);

    await waitFor(() => {
      expect(screen.getByRole('heading', { level: 1 })).toBeInTheDocument();
    });
  });

  it('shows overage information when overageHostUnits > 0', async () => {
    vi.mocked(saasApi.getLicense).mockResolvedValue({
      ...mockLicense,
      currentHostUnits: 55.0,
      overageHostUnits: 5.0,
    });
    renderWithProviders(<LicensingPage />);

    await waitFor(() => {
      expect(screen.getByText(/\+5\.0/)).toBeInTheDocument();
    });
  });
});
