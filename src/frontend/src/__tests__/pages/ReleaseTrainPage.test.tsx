import * as React from 'react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor, fireEvent } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';
import { ReleaseTrainPage } from '../../features/change-governance/pages/ReleaseTrainPage';

vi.mock('react-i18next', () => ({
  useTranslation: () => ({ t: (key: string, opts?: Record<string, unknown>) => {
    if (opts && typeof opts.count !== 'undefined') return `${key}__count${opts.count}`;
    return key;
  }, i18n: { language: 'en' } }),
}));

vi.mock('../../features/change-governance/api/changeIntelligence', () => ({
  changeIntelligenceApi: {
    evaluateReleaseTrain: vi.fn(),
    getRiskScoreTrend: vi.fn(),
  },
}));

vi.mock('../../api/client', () => ({
  default: { get: vi.fn(), post: vi.fn() },
}));

import { changeIntelligenceApi } from '../../features/change-governance/api/changeIntelligence';

const mockResult = {
  trainName: 'Sprint 42 Release',
  requestedCount: 2,
  foundCount: 2,
  notFoundIds: [],
  releases: [
    {
      releaseId: 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa',
      serviceName: 'payment-service',
      version: '2.5.0',
      environment: 'Staging',
      status: 'Succeeded',
      changeLevel: 'Breaking',
      riskScore: 0.72,
      isHighRisk: true,
      totalAffectedConsumers: 5,
      createdAt: '2026-04-10T14:00:00Z',
    },
    {
      releaseId: 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb',
      serviceName: 'notification-service',
      version: '1.3.0',
      environment: 'Staging',
      status: 'Succeeded',
      changeLevel: 'NonBreaking',
      riskScore: 0.18,
      isHighRisk: false,
      totalAffectedConsumers: 2,
      createdAt: '2026-04-10T14:00:00Z',
    },
  ],
  aggregateRiskScore: 0.45,
  combinedAffectedConsumers: 7,
  blockingServices: [],
  readiness: 'PartiallyReady',
  evaluatedAt: '2026-04-16T10:00:00Z',
};

function makeWrapper() {
  const qc = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return ({ children }: { children: React.ReactNode }) => (
    <MemoryRouter>
      <QueryClientProvider client={qc}>{children}</QueryClientProvider>
    </MemoryRouter>
  );
}

describe('ReleaseTrainPage', () => {
  beforeEach(() => vi.clearAllMocks());

  it('renders title', () => {
    render(<ReleaseTrainPage />, { wrapper: makeWrapper() });
    expect(screen.getByText('releaseTrain.title')).toBeInTheDocument();
  });

  it('renders composer card', () => {
    render(<ReleaseTrainPage />, { wrapper: makeWrapper() });
    expect(screen.getByText('releaseTrain.composerTitle')).toBeInTheDocument();
  });

  it('renders empty state initially', () => {
    render(<ReleaseTrainPage />, { wrapper: makeWrapper() });
    expect(screen.getByText('releaseTrain.emptyTitle')).toBeInTheDocument();
  });

  it('shows error on invalid UUID', async () => {
    render(<ReleaseTrainPage />, { wrapper: makeWrapper() });
    const input = screen.getByPlaceholderText('releaseTrain.addReleasePlaceholder');
    fireEvent.change(input, { target: { value: 'not-a-uuid' } });
    fireEvent.click(screen.getByText('releaseTrain.addButton'));
    await waitFor(() => {
      expect(screen.getByText('releaseTrain.invalidUuid')).toBeInTheDocument();
    });
  });

  it('adds valid UUID to list', async () => {
    render(<ReleaseTrainPage />, { wrapper: makeWrapper() });
    const input = screen.getByPlaceholderText('releaseTrain.addReleasePlaceholder');
    fireEvent.change(input, { target: { value: 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa' } });
    fireEvent.click(screen.getByText('releaseTrain.addButton'));
    await waitFor(() => {
      expect(screen.getByText('aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa')).toBeInTheDocument();
    });
  });

  it('evaluate button disabled with 1 release (needs min 2)', async () => {
    render(<ReleaseTrainPage />, { wrapper: makeWrapper() });
    const nameInput = screen.getByPlaceholderText('releaseTrain.namePlaceholder');
    fireEvent.change(nameInput, { target: { value: 'My Train' } });
    const uuidInput = screen.getByPlaceholderText('releaseTrain.addReleasePlaceholder');
    fireEvent.change(uuidInput, { target: { value: 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa' } });
    fireEvent.click(screen.getByText('releaseTrain.addButton'));
    await waitFor(() => {
      const btn = screen.getByText('releaseTrain.evaluateButton');
      expect(btn.closest('button')).toBeDisabled();
    });
  });

  it('shows train result after evaluation', async () => {
    vi.mocked(changeIntelligenceApi.evaluateReleaseTrain).mockResolvedValue(mockResult as never);
    render(<ReleaseTrainPage />, { wrapper: makeWrapper() });
    const nameInput = screen.getByPlaceholderText('releaseTrain.namePlaceholder');
    fireEvent.change(nameInput, { target: { value: 'Sprint 42 Release' } });
    const uuidInput = screen.getByPlaceholderText('releaseTrain.addReleasePlaceholder');
    // Add release 1
    fireEvent.change(uuidInput, { target: { value: 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa' } });
    fireEvent.click(screen.getByText('releaseTrain.addButton'));
    // Add release 2
    fireEvent.change(uuidInput, { target: { value: 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb' } });
    fireEvent.click(screen.getByText('releaseTrain.addButton'));
    fireEvent.click(screen.getByText('releaseTrain.evaluateButton'));
    await waitFor(() => {
      expect(screen.getByText('payment-service')).toBeInTheDocument();
    });
    expect(screen.getByText('notification-service')).toBeInTheDocument();
  });

  it('shows blockers section when blocking services present', async () => {
    const resultWithBlockers = { ...mockResult, blockingServices: ['payment-service'] };
    vi.mocked(changeIntelligenceApi.evaluateReleaseTrain).mockResolvedValue(resultWithBlockers as never);
    render(<ReleaseTrainPage />, { wrapper: makeWrapper() });
    const nameInput = screen.getByPlaceholderText('releaseTrain.namePlaceholder');
    fireEvent.change(nameInput, { target: { value: 'Test Train' } });
    const uuidInput = screen.getByPlaceholderText('releaseTrain.addReleasePlaceholder');
    fireEvent.change(uuidInput, { target: { value: 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa' } });
    fireEvent.click(screen.getByText('releaseTrain.addButton'));
    fireEvent.change(uuidInput, { target: { value: 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb' } });
    fireEvent.click(screen.getByText('releaseTrain.addButton'));
    fireEvent.click(screen.getByText('releaseTrain.evaluateButton'));
    await waitFor(() => {
      expect(screen.getByText('releaseTrain.blockersTitle')).toBeInTheDocument();
    });
  });

  it('renders evaluate button disabled with 0 releases', () => {
    render(<ReleaseTrainPage />, { wrapper: makeWrapper() });
    const btn = screen.getByText('releaseTrain.evaluateButton');
    expect(btn.closest('button')).toBeDisabled();
  });
});
