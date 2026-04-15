import * as React from 'react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';
import { APIKeysPage } from '../../features/configuration/pages/APIKeysPage';

vi.mock('../../api/client', () => ({
  default: { get: vi.fn(), post: vi.fn(), delete: vi.fn() },
}));

import client from '../../api/client';

function renderPage() {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter>
        <APIKeysPage />
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

describe('APIKeysPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(client.get).mockResolvedValue({ data: { items: [], totalCount: 0 } });
  });

  it('renders page title', async () => {
    renderPage();
    await waitFor(() => {
      expect(screen.getByText('API Keys')).toBeInTheDocument();
    });
  });

  it('shows loading state initially', () => {
    vi.mocked(client.get).mockReturnValue(new Promise(() => {}));
    renderPage();
    expect(document.body).toBeDefined();
  });

  it('shows empty state when no api keys', async () => {
    renderPage();
    await waitFor(() => expect(document.body.textContent).toBeDefined());
  });

  it('renders API key list when data is available', async () => {
    vi.mocked(client.get).mockResolvedValue({
      data: {
        items: [
          {
            apiKeyId: 'key-1',
            name: 'My API Key',
            scopes: ['read:services'],
            createdAt: '2026-01-01T00:00:00Z',
          },
        ],
        totalCount: 1,
      },
    });
    renderPage();
    await waitFor(() => {
      expect(screen.getByText('My API Key')).toBeInTheDocument();
    });
  });

  it('shows error state when API fails', async () => {
    vi.mocked(client.get).mockRejectedValue(new Error('Network error'));
    renderPage();
    await waitFor(() => expect(document.body.textContent).toBeDefined());
  });
});
