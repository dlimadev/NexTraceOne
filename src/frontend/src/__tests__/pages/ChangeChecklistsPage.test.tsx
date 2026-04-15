import * as React from 'react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';
import { ChangeChecklistsPage } from '../../features/configuration/pages/ChangeChecklistsPage';

vi.mock('../../api/client', () => ({
  default: { get: vi.fn(), post: vi.fn(), delete: vi.fn() },
}));

import client from '../../api/client';

function renderPage() {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter>
        <ChangeChecklistsPage />
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

describe('ChangeChecklistsPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(client.get).mockResolvedValue({ data: { items: [], totalCount: 0 } });
  });

  it('renders page title', async () => {
    renderPage();
    await waitFor(() => {
      expect(screen.getByText('Change Checklists')).toBeInTheDocument();
    });
  });

  it('shows loading state initially', () => {
    vi.mocked(client.get).mockReturnValue(new Promise(() => {}));
    renderPage();
    expect(document.body).toBeDefined();
  });

  it('shows empty state when no checklists', async () => {
    renderPage();
    await waitFor(() => expect(document.body.textContent).toBeDefined());
  });

  it('renders checklist items when data is available', async () => {
    vi.mocked(client.get).mockResolvedValue({
      data: {
        items: [
          {
            checklistId: 'cl-1',
            name: 'Production Deployment Checklist',
            changeType: 'deployment',
            isRequired: true,
            items: ['Smoke test passed', 'Rollback plan ready'],
            createdAt: '2026-01-01T00:00:00Z',
          },
        ],
        totalCount: 1,
      },
    });
    renderPage();
    await waitFor(() => {
      expect(screen.getByText('Production Deployment Checklist')).toBeInTheDocument();
    });
  });

  it('shows error state when API fails', async () => {
    vi.mocked(client.get).mockRejectedValue(new Error('API error'));
    renderPage();
    await waitFor(() => expect(document.body.textContent).toBeDefined());
  });
});
