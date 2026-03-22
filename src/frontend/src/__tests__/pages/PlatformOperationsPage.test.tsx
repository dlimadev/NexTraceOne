import * as React from 'react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';
import { PlatformOperationsPage } from '../../features/operations/pages/PlatformOperationsPage';

vi.mock('../../features/operations/api/platformOps', () => ({
  platformOpsApi: {
    getHealth: vi.fn(),
    getJobs: vi.fn(),
    getQueues: vi.fn(),
    getEvents: vi.fn(),
    getConfig: vi.fn(),
  },
}));

vi.mock('../../api/client', () => ({
  default: { get: vi.fn(), post: vi.fn() },
}));

import { platformOpsApi } from '../../features/operations/api/platformOps';

const mockHealthResponse = {
  overallStatus: 'Healthy' as const,
  subsystems: [
    { name: 'API', status: 'Healthy' as const, description: 'All API endpoints responding', lastCheckedAt: '2026-03-22T10:00:00Z' },
    { name: 'Database', status: 'Healthy' as const, description: 'PostgreSQL operational', lastCheckedAt: '2026-03-22T10:00:00Z' },
  ],
  uptimeSeconds: 86400,
  version: '1.0.0',
  checkedAt: '2026-03-22T10:00:00Z',
};

const mockJobsResponse = {
  jobs: [
    {
      jobId: 'job-outbox-processor',
      name: 'Outbox Processor',
      status: 'Running' as const,
      lastRunAt: '2026-03-22T09:58:00Z',
      nextRunAt: '2026-03-22T10:00:00Z',
      executionCount: 14832,
      failureCount: 3,
      lastError: null,
    },
  ],
  totalCount: 1,
  page: 1,
  pageSize: 20,
};

const mockQueuesResponse = {
  queues: [
    {
      queueName: 'outbox',
      pendingCount: 12,
      processingCount: 3,
      failedCount: 0,
      deadLetterCount: 0,
      averageProcessingMs: 45.2,
      lastActivityAt: '2026-03-22T09:59:45Z',
    },
  ],
  checkedAt: '2026-03-22T10:00:00Z',
};

const mockEventsResponse = {
  events: [
    {
      eventId: 'evt-001',
      timestamp: '2026-03-22T09:55:00Z',
      severity: 'Info' as const,
      subsystem: 'API',
      message: 'Rate limiter adjusted',
      correlationId: 'corr-001',
      resolved: true,
    },
  ],
  totalCount: 1,
  page: 1,
  pageSize: 20,
};

function renderPage() {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false } },
  });
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter>
        <PlatformOperationsPage />
      </MemoryRouter>
    </QueryClientProvider>
  );
}

describe('PlatformOperationsPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(platformOpsApi.getHealth).mockResolvedValue(mockHealthResponse);
    vi.mocked(platformOpsApi.getJobs).mockResolvedValue(mockJobsResponse);
    vi.mocked(platformOpsApi.getQueues).mockResolvedValue(mockQueuesResponse);
    vi.mocked(platformOpsApi.getEvents).mockResolvedValue(mockEventsResponse);
  });

  it('shows health status from API in health tab', async () => {
    renderPage();
    await waitFor(() => {
      expect(screen.getByText('API')).toBeInTheDocument();
    });
    expect(screen.getByText('Database')).toBeInTheDocument();
  });

  it('shows version from API', async () => {
    renderPage();
    await waitFor(() => {
      expect(screen.getByText('1.0.0')).toBeInTheDocument();
    });
  });

  it('navigates to jobs tab and shows jobs from API', async () => {
    renderPage();
    await waitFor(() => {
      expect(screen.getByText('API')).toBeInTheDocument();
    });
    const jobsTab = screen.getByRole('button', { name: /jobs/i });
    await userEvent.click(jobsTab);
    await waitFor(() => {
      expect(screen.getByText('Outbox Processor')).toBeInTheDocument();
    });
  });

  it('navigates to queues tab and shows queues from API', async () => {
    renderPage();
    await waitFor(() => {
      expect(screen.getByText('API')).toBeInTheDocument();
    });
    const queuesTab = screen.getByRole('button', { name: /queues/i });
    await userEvent.click(queuesTab);
    await waitFor(() => {
      expect(screen.getByText('outbox')).toBeInTheDocument();
    });
  });

  it('navigates to events tab and shows events from API', async () => {
    renderPage();
    await waitFor(() => {
      expect(screen.getByText('API')).toBeInTheDocument();
    });
    const eventsTab = screen.getByRole('button', { name: /events/i });
    await userEvent.click(eventsTab);
    await waitFor(() => {
      expect(screen.getByText('Rate limiter adjusted')).toBeInTheDocument();
    });
  });

  it('shows error state when health API fails', async () => {
    vi.mocked(platformOpsApi.getHealth).mockRejectedValue(new Error('Network error'));
    renderPage();
    await waitFor(() => {
      expect(screen.getAllByRole('button', { name: /retry/i }).length).toBeGreaterThan(0);
    });
  });

  it('calls real API endpoints not local mock arrays', async () => {
    renderPage();
    await waitFor(() => {
      expect(platformOpsApi.getHealth).toHaveBeenCalled();
      expect(platformOpsApi.getJobs).toHaveBeenCalled();
      expect(platformOpsApi.getQueues).toHaveBeenCalled();
      expect(platformOpsApi.getEvents).toHaveBeenCalled();
    });
  });
});
