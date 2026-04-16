import * as React from 'react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor, fireEvent } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';
import { ReleaseChecklistExecutionPage } from '../../features/change-governance/pages/ReleaseChecklistExecutionPage';

vi.mock('react-i18next', () => ({
  useTranslation: () => ({ t: (key: string, opts?: Record<string, unknown>) => {
    if (opts && typeof opts.num !== 'undefined') return `${key}__num${opts.num}`;
    return key;
  }, i18n: { language: 'en' } }),
}));

vi.mock('../../features/change-governance/api/workflow', () => ({
  workflowApi: {
    listTemplates: vi.fn(),
    getTemplate: vi.fn(),
    listInstances: vi.fn(),
    getInstance: vi.fn(),
    approve: vi.fn(),
    reject: vi.fn(),
    requestChanges: vi.fn(),
    recordChecklistEvidence: vi.fn(),
  },
}));

vi.mock('../../api/client', () => ({
  default: { get: vi.fn(), post: vi.fn() },
}));

import { workflowApi } from '../../features/change-governance/api/workflow';

const mockResult = {
  evidencePackId: 'ep-001-uuid-here',
  workflowInstanceId: 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa',
  checklistName: 'Pre-Deploy Checklist',
  totalItems: 3,
  completedItems: 2,
  completionRate: 66.7,
  evidenceCompletenessPercentage: 80.0,
  recordedAt: '2026-04-16T10:00:00Z',
};

function makeWrapper() {
  const qc = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return ({ children }: { children: React.ReactNode }) => (
    <MemoryRouter>
      <QueryClientProvider client={qc}>{children}</QueryClientProvider>
    </MemoryRouter>
  );
}

describe('ReleaseChecklistExecutionPage', () => {
  beforeEach(() => vi.clearAllMocks());

  it('renders title', () => {
    render(<ReleaseChecklistExecutionPage />, { wrapper: makeWrapper() });
    expect(screen.getByText('checklist.title')).toBeInTheDocument();
  });

  it('renders context card', () => {
    render(<ReleaseChecklistExecutionPage />, { wrapper: makeWrapper() });
    expect(screen.getByText('checklist.contextTitle')).toBeInTheDocument();
  });

  it('renders progress panel', () => {
    render(<ReleaseChecklistExecutionPage />, { wrapper: makeWrapper() });
    expect(screen.getByText('checklist.progressTitle')).toBeInTheDocument();
  });

  it('renders default 3 checklist items', () => {
    render(<ReleaseChecklistExecutionPage />, { wrapper: makeWrapper() });
    const items = screen.getAllByPlaceholderText(/checklist.itemNamePlaceholder/);
    expect(items.length).toBe(3);
  });

  it('shows validation error for invalid instance id', async () => {
    render(<ReleaseChecklistExecutionPage />, { wrapper: makeWrapper() });
    const idInput = screen.getByPlaceholderText('checklist.instanceIdPlaceholder');
    fireEvent.change(idInput, { target: { value: 'not-a-uuid' } });
    fireEvent.click(screen.getByText('checklist.submitButton'));
    await waitFor(() => {
      expect(screen.getByText('checklist.invalidInstanceId')).toBeInTheDocument();
    });
  });

  it('shows error if checklist name missing', async () => {
    render(<ReleaseChecklistExecutionPage />, { wrapper: makeWrapper() });
    const idInput = screen.getByPlaceholderText('checklist.instanceIdPlaceholder');
    fireEvent.change(idInput, { target: { value: 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa' } });
    fireEvent.click(screen.getByText('checklist.submitButton'));
    await waitFor(() => {
      expect(screen.getByText('checklist.nameRequired')).toBeInTheDocument();
    });
  });

  it('adds a new checklist item', async () => {
    render(<ReleaseChecklistExecutionPage />, { wrapper: makeWrapper() });
    const initialCount = screen.getAllByPlaceholderText(/checklist.itemNamePlaceholder/).length;
    fireEvent.click(screen.getByText(/checklist.addItem/));
    await waitFor(() => {
      const afterCount = screen.getAllByPlaceholderText(/checklist.itemNamePlaceholder/).length;
      expect(afterCount).toBe(initialCount + 1);
    });
  });

  it('shows evidence result after successful submission', async () => {
    vi.mocked(workflowApi.recordChecklistEvidence).mockResolvedValue(mockResult as never);
    render(<ReleaseChecklistExecutionPage />, { wrapper: makeWrapper() });
    const idInput = screen.getByPlaceholderText('checklist.instanceIdPlaceholder');
    fireEvent.change(idInput, { target: { value: 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa' } });
    const nameInput = screen.getByPlaceholderText('checklist.namePlaceholder');
    fireEvent.change(nameInput, { target: { value: 'Pre-Deploy Checklist' } });
    const executedBy = screen.getByPlaceholderText('checklist.executedByPlaceholder');
    fireEvent.change(executedBy, { target: { value: 'john.doe@company.com' } });
    const itemInputs = screen.getAllByPlaceholderText(/checklist.itemNamePlaceholder/);
    fireEvent.change(itemInputs[0], { target: { value: 'Database migrations checked' } });
    fireEvent.click(screen.getByText('checklist.submitButton'));
    await waitFor(() => {
      expect(screen.getByText('checklist.recordedTitle')).toBeInTheDocument();
    });
  });

  it('renders submit button', () => {
    render(<ReleaseChecklistExecutionPage />, { wrapper: makeWrapper() });
    expect(screen.getByText('checklist.submitButton')).toBeInTheDocument();
  });
});
