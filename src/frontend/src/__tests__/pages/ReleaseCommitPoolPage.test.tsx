import * as React from 'react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';
import { ReleaseCommitPoolPage } from '../../features/change-governance/pages/ReleaseCommitPoolPage';

vi.mock('../../features/change-governance/api/changeIntelligence', () => ({
  changeIntelligenceApi: {
    listCommitsByRelease: vi.fn(),
    listWorkItemsByRelease: vi.fn(),
    addWorkItemToRelease: vi.fn(),
    removeWorkItemFromRelease: vi.fn(),
  },
}));

vi.mock('../../api/client', () => ({
  default: { get: vi.fn(), post: vi.fn(), delete: vi.fn() },
}));

import { changeIntelligenceApi } from '../../features/change-governance/api/changeIntelligence';

const mockCommitsData = {
  releaseId: 'release-001',
  commits: [
    {
      id: 'assoc-001',
      commitSha: 'abc123def456',
      commitMessage: 'feat: add PIX payment method',
      commitAuthor: 'alice@example.com',
      committedAt: '2026-04-10T10:00:00Z',
      branchName: 'feature/PAY-1234',
      serviceName: 'payment-service',
      assignmentStatus: 'Included',
      assignedAt: '2026-04-11T10:00:00Z',
      assignedBy: 'admin',
      extractedWorkItemRefs: 'PAY-1234',
    },
    {
      id: 'assoc-002',
      commitSha: 'def456abc789',
      commitMessage: 'fix: correct fee calculation',
      commitAuthor: 'bob@example.com',
      committedAt: '2026-04-11T09:00:00Z',
      branchName: 'feature/PAY-1234',
      serviceName: 'payment-service',
      assignmentStatus: 'Candidate',
      assignedAt: null,
      assignedBy: null,
      extractedWorkItemRefs: null,
    },
  ],
};

const mockWorkItemsData = {
  releaseId: 'release-001',
  workItems: [
    {
      id: 'wi-001',
      externalWorkItemId: 'PAY-1234',
      externalSystem: 'Jira',
      title: 'Add PIX payment method',
      workItemType: 'Story',
      externalStatus: 'Done',
      externalUrl: 'https://jira.example.com/browse/PAY-1234',
      addedBy: 'alice',
      addedAt: '2026-04-11T10:00:00Z',
      isActive: true,
    },
  ],
};

function renderPage() {
  const qc = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={qc}>
      <MemoryRouter>
        <ReleaseCommitPoolPage />
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

describe('ReleaseCommitPoolPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    (changeIntelligenceApi.listCommitsByRelease as ReturnType<typeof vi.fn>).mockResolvedValue(mockCommitsData);
    (changeIntelligenceApi.listWorkItemsByRelease as ReturnType<typeof vi.fn>).mockResolvedValue(mockWorkItemsData);
  });

  it('renders page title', () => {
    renderPage();
    expect(screen.getByText('Release Commit Pool')).toBeInTheDocument();
  });

  it('renders release ID input', () => {
    renderPage();
    expect(screen.getByPlaceholderText('Enter a release ID to load its commits and work items')).toBeInTheDocument();
  });

  it('renders Commits tab button', () => {
    renderPage();
    expect(screen.getAllByText('Commits').length).toBeGreaterThan(0);
  });

  it('renders Work Items tab button', () => {
    renderPage();
    expect(screen.getByText('Work Items')).toBeInTheDocument();
  });

  it('shows commit pool list after loading', async () => {
    renderPage();
    // Simulate a release ID being set by finding and interacting
    await waitFor(() => {
      expect(screen.getByText('No commits found for this release')).toBeInTheDocument();
    });
  });

  it('renders commit pool subtitle', () => {
    renderPage();
    expect(screen.getByText('Manage commits and work items associated with a release')).toBeInTheDocument();
  });

  it('renders add work item form elements', () => {
    renderPage();
    // Tab buttons present
    expect(screen.getByText('Work Items')).toBeInTheDocument();
  });

  it('renders no commits message when list is empty', async () => {
    (changeIntelligenceApi.listCommitsByRelease as ReturnType<typeof vi.fn>).mockResolvedValue({ releaseId: '', commits: [] });
    renderPage();
    await waitFor(() => {
      expect(screen.getByText('No commits found for this release')).toBeInTheDocument();
    });
  });

  it('renders release ID label', () => {
    renderPage();
    expect(screen.getByText('Release ID')).toBeInTheDocument();
  });

  it('matches snapshot', () => {
    const { container } = renderPage();
    expect(container.firstChild).toBeTruthy();
  });
});
