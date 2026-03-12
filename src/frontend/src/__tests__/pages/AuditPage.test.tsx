import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';
import { AuditPage } from '../../pages/AuditPage';
import type { AuditEvent, PagedList } from '../../types';

vi.mock('../../api', () => ({
  auditApi: {
    listEvents: vi.fn(),
    verifyIntegrity: vi.fn(),
  },
}));

import { auditApi } from '../../api';

const mockEvents: PagedList<AuditEvent> = {
  items: [
    {
      id: 'evt-1',
      eventType: 'ReleaseCreated',
      aggregateId: 'rel-001',
      aggregateType: 'Release',
      actorId: 'usr-001',
      actorEmail: 'admin@acme.com',
      payload: {},
      hash: 'abc123def456789012345678901234567890123456789012',
      occurredAt: '2024-01-15T10:00:00Z',
    },
    {
      id: 'evt-2',
      eventType: 'UserLoggedIn',
      aggregateId: 'ses-001',
      aggregateType: 'Session',
      actorId: 'usr-002',
      actorEmail: 'dev@acme.com',
      payload: {},
      hash: 'def456ghi789012345678901234567890123456789012345',
      previousHash: 'abc123def456789012345678901234567890123456789012',
      occurredAt: '2024-01-15T09:00:00Z',
    },
  ],
  totalCount: 2,
  page: 1,
  pageSize: 20,
  totalPages: 1,
};

function renderAudit() {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false } },
  });
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter>
        <AuditPage />
      </MemoryRouter>
    </QueryClientProvider>
  );
}

describe('AuditPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('exibe o título da página', () => {
    vi.mocked(auditApi.listEvents).mockResolvedValue({
      items: [], totalCount: 0, page: 1, pageSize: 20, totalPages: 0,
    });
    renderAudit();
    expect(screen.getByRole('heading', { name: /audit log/i })).toBeInTheDocument();
  });

  it('exibe os eventos de auditoria carregados da API', async () => {
    vi.mocked(auditApi.listEvents).mockResolvedValue(mockEvents);
    renderAudit();
    await waitFor(() => {
      expect(screen.getByText('ReleaseCreated')).toBeInTheDocument();
      expect(screen.getByText('UserLoggedIn')).toBeInTheDocument();
      expect(screen.getByText('admin@acme.com')).toBeInTheDocument();
    });
  });

  it('exibe mensagem quando não há eventos', async () => {
    vi.mocked(auditApi.listEvents).mockResolvedValue({
      items: [], totalCount: 0, page: 1, pageSize: 20, totalPages: 0,
    });
    renderAudit();
    await waitFor(() => {
      expect(screen.getByText(/no audit events found/i)).toBeInTheDocument();
    });
  });

  it('exibe mensagem de erro quando API falha', async () => {
    vi.mocked(auditApi.listEvents).mockRejectedValue(new Error('Server error'));
    renderAudit();
    await waitFor(() => {
      expect(screen.getByText(/failed to load audit events/i)).toBeInTheDocument();
    });
  });

  it('exibe o botão Verify Integrity', () => {
    vi.mocked(auditApi.listEvents).mockResolvedValue({
      items: [], totalCount: 0, page: 1, pageSize: 20, totalPages: 0,
    });
    renderAudit();
    expect(screen.getByRole('button', { name: /verify integrity/i })).toBeInTheDocument();
  });

  it('exibe o resultado de integridade válida ao clicar no botão', async () => {
    vi.mocked(auditApi.listEvents).mockResolvedValue({
      items: [], totalCount: 0, page: 1, pageSize: 20, totalPages: 0,
    });
    vi.mocked(auditApi.verifyIntegrity).mockResolvedValue({
      valid: true,
      message: 'Hash chain is valid. All 42 events verified.',
    });
    renderAudit();
    await userEvent.click(screen.getByRole('button', { name: /verify integrity/i }));
    await waitFor(() => {
      expect(screen.getByText(/hash chain is valid/i)).toBeInTheDocument();
    });
  });

  it('exibe o resultado de integridade inválida', async () => {
    vi.mocked(auditApi.listEvents).mockResolvedValue({
      items: [], totalCount: 0, page: 1, pageSize: 20, totalPages: 0,
    });
    vi.mocked(auditApi.verifyIntegrity).mockResolvedValue({
      valid: false,
      message: 'Integrity violation detected at event evt-5.',
    });
    renderAudit();
    await userEvent.click(screen.getByRole('button', { name: /verify integrity/i }));
    await waitFor(() => {
      expect(screen.getByText(/integrity violation detected/i)).toBeInTheDocument();
    });
  });

  it('filtra eventos ao digitar no campo de busca', async () => {
    vi.mocked(auditApi.listEvents).mockResolvedValue(mockEvents);
    renderAudit();
    const input = screen.getByPlaceholderText(/filter by event type/i);
    await userEvent.type(input, 'Release');
    expect(auditApi.listEvents).toHaveBeenCalled();
  });

  it('exibe o total de eventos', async () => {
    vi.mocked(auditApi.listEvents).mockResolvedValue(mockEvents);
    renderAudit();
    await waitFor(() => {
      expect(screen.getByText('2 total')).toBeInTheDocument();
    });
  });
});
