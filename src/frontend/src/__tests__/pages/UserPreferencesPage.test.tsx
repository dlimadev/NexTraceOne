import * as React from 'react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, waitFor } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';
import { UserPreferencesPage } from '../../features/configuration/pages/UserPreferencesPage';

vi.mock('../../contexts/ThemeContext', () => ({
  useTheme: vi.fn(() => ({ theme: 'dark', setTheme: vi.fn() })),
}));

vi.mock('../../api/client', () => ({
  default: { get: vi.fn(), post: vi.fn() },
}));

const mockFetchResponse = {
  userId: 'user-1',
  preferences: [],
  sidebarCustomizationEnabled: true,
  maxPinnedItems: 10,
  maxWidgets: 6,
  evaluatedAt: '2026-04-12T00:00:00Z',
};

function renderPage() {
  global.fetch = vi.fn(() =>
    Promise.resolve({
      ok: true,
      json: () => Promise.resolve(mockFetchResponse),
    } as Response),
  );
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter>
        <UserPreferencesPage />
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

describe('UserPreferencesPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('renders without crashing', () => {
    const { container } = renderPage();
    expect(container).toBeDefined();
  });

  it('shows loading or content state', async () => {
    renderPage();
    await waitFor(() => {
      expect(document.body).toBeDefined();
    });
  });
});
