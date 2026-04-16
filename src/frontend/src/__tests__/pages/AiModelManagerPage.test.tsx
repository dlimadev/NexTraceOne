import { render, screen, waitFor } from '@testing-library/react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { AiModelManagerPage } from '../../features/platform-admin/pages/AiModelManagerPage';
import { platformAdminApi } from '../../features/platform-admin/api/platformAdmin';
import type { HardwareAssessmentReport } from '../../features/platform-admin/api/platformAdmin';

// i18n mock
vi.mock('react-i18next', () => ({
  useTranslation: () => ({
    t: (key: string) => key,
    i18n: { language: 'en' },
  }),
}));

// API mock
vi.mock('../../features/platform-admin/api/platformAdmin', () => ({
  platformAdminApi: {
    getHardwareAssessment: vi.fn(),
    getPreflight: vi.fn(),
    getConfigHealth: vi.fn(),
    getPendingMigrations: vi.fn(),
    getNetworkPolicy: vi.fn(),
    getDatabaseHealth: vi.fn(),
  },
}));

// React Query wrapper
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { ReactNode } from 'react';
const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
const Wrapper = ({ children }: { children: ReactNode }) => (
  <QueryClientProvider client={queryClient}>{children}</QueryClientProvider>
);

const mockReport: HardwareAssessmentReport = {
  cpuModel: 'Intel Xeon E5-2686 v4',
  cpuCores: 8,
  totalRamGb: 32,
  availableRamGb: 20,
  diskFreeGb: 150,
  hasGpu: false,
  gpuModel: null,
  gpuVramGb: 0,
  osDescription: 'Linux 5.15.0',
  assessedAt: new Date().toISOString(),
  models: [
    {
      name: 'deepseek-r1:1.5b',
      displayName: 'DeepSeek R1 1.5B',
      sizeGb: 1.1,
      requiredRamGb: 2.0,
      estTokPerSec: 52.5,
      acceleratedByGpu: false,
      status: 'Compatible',
      warning: null,
      description: 'Low resource model',
    },
    {
      name: 'llama3.1:70b',
      displayName: 'Llama 3.1 70B',
      sizeGb: 40,
      requiredRamGb: 56,
      estTokPerSec: 1.5,
      acceleratedByGpu: false,
      status: 'Incompatible',
      warning: 'Insufficient RAM',
      description: 'High quality model',
    },
  ],
};

describe('AiModelManagerPage', () => {
  beforeEach(() => {
    queryClient.clear();
    vi.clearAllMocks();
  });

  it('shows loading state initially', () => {
    vi.mocked(platformAdminApi.getHardwareAssessment).mockImplementation(
      () => new Promise(() => {}) // never resolves
    );
    render(<AiModelManagerPage />, { wrapper: Wrapper });
    expect(screen.getByText('loading')).toBeDefined();
  });

  it('displays hardware profile after data loads', async () => {
    vi.mocked(platformAdminApi.getHardwareAssessment).mockResolvedValue(mockReport);
    render(<AiModelManagerPage />, { wrapper: Wrapper });
    await waitFor(() => expect(screen.getByText('hardwareTitle')).toBeDefined());
    expect(screen.getByText('Intel Xeon E5-2686 v4')).toBeDefined();
  });

  it('displays model compatibility table', async () => {
    vi.mocked(platformAdminApi.getHardwareAssessment).mockResolvedValue(mockReport);
    render(<AiModelManagerPage />, { wrapper: Wrapper });
    await waitFor(() => expect(screen.getByText('modelsTitle')).toBeDefined());
    expect(screen.getByText('deepseek-r1:1.5b')).toBeDefined();
    expect(screen.getByText('llama3.1:70b')).toBeDefined();
  });

  it('shows error state on API failure', async () => {
    vi.mocked(platformAdminApi.getHardwareAssessment).mockRejectedValue(new Error('Network error'));
    render(<AiModelManagerPage />, { wrapper: Wrapper });
    await waitFor(() => expect(screen.getByText('error')).toBeDefined());
  });

  it('shows OS description', async () => {
    vi.mocked(platformAdminApi.getHardwareAssessment).mockResolvedValue(mockReport);
    render(<AiModelManagerPage />, { wrapper: Wrapper });
    await waitFor(() => expect(screen.getByText('Linux 5.15.0')).toBeDefined());
  });
});
