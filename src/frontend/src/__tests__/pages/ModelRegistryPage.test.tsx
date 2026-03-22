import * as React from 'react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { ModelRegistryPage } from '../../features/ai-hub/pages/ModelRegistryPage';

vi.mock('../../features/ai-hub/api/aiGovernance', () => ({
  aiGovernanceApi: {
    listModels: vi.fn(),
  },
}));

vi.mock('../../api/client', () => ({
  default: { get: vi.fn(), post: vi.fn(), patch: vi.fn() },
}));

import { aiGovernanceApi } from '../../features/ai-hub/api/aiGovernance';

const mockModels = {
  items: [
    {
      modelId: 'model-001',
      name: 'gpt-4o',
      displayName: 'GPT-4o',
      provider: 'OpenAI',
      modelType: 'Chat',
      isInternal: false,
      isExternal: true,
      status: 'Active',
      capabilities: 'chat,code,reasoning',
      sensitivityLevel: 2,
    },
    {
      modelId: 'model-002',
      name: 'nextra-local',
      displayName: 'NexTra Local',
      provider: 'NexTraceOne',
      modelType: 'Chat',
      isInternal: true,
      isExternal: false,
      status: 'Active',
      capabilities: 'chat',
      sensitivityLevel: 1,
    },
  ],
};

function renderPage() {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false } },
  });
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter initialEntries={['/ai/models']}>
        <Routes>
          <Route path="/ai/models" element={<ModelRegistryPage />} />
        </Routes>
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

describe('ModelRegistryPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('shows loading state while fetching models', () => {
    vi.mocked(aiGovernanceApi.listModels).mockReturnValue(new Promise(() => {}));
    renderPage();
    // Loader renders an animated SVG spinner without text; verify models are not yet rendered
    expect(screen.queryByText('GPT-4o')).not.toBeInTheDocument();
  });

  it('renders model list after loading', async () => {
    vi.mocked(aiGovernanceApi.listModels).mockResolvedValue(mockModels);
    renderPage();
    await waitFor(() => {
      expect(screen.getByText('GPT-4o')).toBeInTheDocument();
    });
    expect(screen.getByText('NexTra Local')).toBeInTheDocument();
    expect(screen.getByText('OpenAI')).toBeInTheDocument();
  });

  it('shows error state when API fails', async () => {
    vi.mocked(aiGovernanceApi.listModels).mockRejectedValue(new Error('Server error'));
    renderPage();
    await waitFor(() => {
      expect(screen.getByText(/error/i)).toBeInTheDocument();
    });
  });

  it('shows empty state when no models found', async () => {
    vi.mocked(aiGovernanceApi.listModels).mockResolvedValue({ items: [] });
    renderPage();
    await waitFor(() => {
      expect(screen.getByText(/no models found/i)).toBeInTheDocument();
    });
  });

  it('displays stat cards with correct counts', async () => {
    vi.mocked(aiGovernanceApi.listModels).mockResolvedValue(mockModels);
    renderPage();
    await waitFor(() => {
      expect(screen.getByText('GPT-4o')).toBeInTheDocument();
    });
    // 2 total models, 2 active models -> each appears in stat cards
    const twos = screen.getAllByText('2');
    expect(twos.length).toBeGreaterThanOrEqual(2);
    // 1 internal, 1 external
    const ones = screen.getAllByText('1');
    expect(ones.length).toBeGreaterThanOrEqual(2);
  });

  it('displays model status badges', async () => {
    vi.mocked(aiGovernanceApi.listModels).mockResolvedValue(mockModels);
    renderPage();
    await waitFor(() => {
      expect(screen.getAllByText('Active').length).toBeGreaterThanOrEqual(2);
    });
  });

  it('renders status filter buttons', async () => {
    vi.mocked(aiGovernanceApi.listModels).mockResolvedValue(mockModels);
    renderPage();
    await waitFor(() => {
      expect(screen.getByText('GPT-4o')).toBeInTheDocument();
    });
    // Filter buttons: All, Active, Inactive, Deprecated, Blocked (via i18n keys)
    expect(screen.getByText(/all/i)).toBeInTheDocument();
  });
});
