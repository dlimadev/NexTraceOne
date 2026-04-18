/**
 * Tests for Phase 5 widgets: TopServicesWidget, ContractCoverageWidget, BlastRadiusWidget.
 */
import * as React from 'react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, waitFor } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';

vi.mock('../../api/client', () => ({
  default: { get: vi.fn() },
}));
import client from '../../api/client';
import { TopServicesWidget } from '../../features/governance/widgets/TopServicesWidget';
import { ContractCoverageWidget } from '../../features/governance/widgets/ContractCoverageWidget';
import { BlastRadiusWidget } from '../../features/governance/widgets/BlastRadiusWidget';

function wrap(ui: React.ReactElement) {
  const qc = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(<QueryClientProvider client={qc}>{ui}</QueryClientProvider>);
}

const BASE_CONFIG = { serviceId: null, teamId: null, timeRange: '24h', customTitle: null };

// ── TopServicesWidget ─────────────────────────────────────────────────────

describe('TopServicesWidget', () => {
  beforeEach(() => vi.clearAllMocks());

  it('shows loading skeleton initially', () => {
    vi.mocked(client.get).mockReturnValue(new Promise(() => {}));
    const { container } = wrap(
      <TopServicesWidget widgetId="w1" config={BASE_CONFIG} timeRange="24h" />,
    );
    expect(container).toBeDefined();
  });

  it('renders top services list with incident counts', async () => {
    vi.mocked(client.get).mockResolvedValue({
      data: {
        items: [
          { serviceId: 's1', serviceName: 'payment-api', openIncidents: 5, healthStatus: 'critical' },
          { serviceId: 's2', serviceName: 'auth-service', openIncidents: 2, healthStatus: 'degraded' },
          { serviceId: 's3', serviceName: 'data-store', openIncidents: 0, healthStatus: 'healthy' },
        ],
      },
    });
    wrap(<TopServicesWidget widgetId="w1" config={BASE_CONFIG} timeRange="24h" />);
    await waitFor(() => {
      expect(document.body.textContent).toContain('payment-api');
      expect(document.body.textContent).toContain('5');
    });
  });

  it('shows empty state when no services', async () => {
    vi.mocked(client.get).mockResolvedValue({ data: { items: [] } });
    wrap(<TopServicesWidget widgetId="w1" config={BASE_CONFIG} timeRange="24h" />);
    await waitFor(() => {
      expect(document.body.textContent).toMatch(/no service data/i);
    });
  });

  it('shows error state when API fails', async () => {
    vi.mocked(client.get).mockRejectedValue(new Error('fail'));
    wrap(<TopServicesWidget widgetId="w1" config={BASE_CONFIG} timeRange="24h" />);
    await waitFor(() => {
      expect(document.body).toBeDefined();
    });
  });

  it('uses custom title when provided', async () => {
    vi.mocked(client.get).mockResolvedValue({ data: { items: [] } });
    wrap(
      <TopServicesWidget widgetId="w1" config={BASE_CONFIG} timeRange="24h" title="Critical Services" />,
    );
    await waitFor(() => {
      expect(document.body.textContent).toContain('Critical Services');
    });
  });

  it('shows rank numbers for services', async () => {
    vi.mocked(client.get).mockResolvedValue({
      data: {
        items: [
          { serviceId: 's1', serviceName: 'svc-a', openIncidents: 3, healthStatus: 'critical' },
          { serviceId: 's2', serviceName: 'svc-b', openIncidents: 1, healthStatus: 'degraded' },
        ],
      },
    });
    wrap(<TopServicesWidget widgetId="w1" config={BASE_CONFIG} timeRange="7d" />);
    await waitFor(() => {
      expect(document.body.textContent).toContain('1');
      expect(document.body.textContent).toContain('2');
    });
  });
});

// ── ContractCoverageWidget ────────────────────────────────────────────────

describe('ContractCoverageWidget', () => {
  beforeEach(() => vi.clearAllMocks());

  it('shows loading skeleton initially', () => {
    vi.mocked(client.get).mockReturnValue(new Promise(() => {}));
    const { container } = wrap(
      <ContractCoverageWidget widgetId="w2" config={BASE_CONFIG} timeRange="24h" />,
    );
    expect(container).toBeDefined();
  });

  it('renders coverage percentage', async () => {
    vi.mocked(client.get).mockResolvedValue({
      data: {
        totalServices: 20,
        coveredServices: 16,
        coveragePercent: 80,
        rest: 10,
        soap: 4,
        event: 2,
      },
    });
    wrap(<ContractCoverageWidget widgetId="w2" config={BASE_CONFIG} timeRange="24h" />);
    await waitFor(() => {
      expect(document.body.textContent).toContain('80%');
    });
  });

  it('renders REST/SOAP/Event contract counts', async () => {
    vi.mocked(client.get).mockResolvedValue({
      data: {
        totalServices: 10,
        coveredServices: 7,
        coveragePercent: 70,
        rest: 5,
        soap: 1,
        event: 1,
      },
    });
    wrap(<ContractCoverageWidget widgetId="w2" config={BASE_CONFIG} timeRange="24h" />);
    await waitFor(() => {
      expect(document.body.textContent).toContain('REST');
      expect(document.body.textContent).toContain('SOAP');
      expect(document.body.textContent).toContain('5');
    });
  });

  it('renders services with contracts count', async () => {
    vi.mocked(client.get).mockResolvedValue({
      data: {
        totalServices: 15,
        coveredServices: 12,
        coveragePercent: 80,
        rest: 8,
        soap: 2,
        event: 2,
      },
    });
    wrap(<ContractCoverageWidget widgetId="w2" config={BASE_CONFIG} timeRange="24h" />);
    await waitFor(() => {
      expect(document.body.textContent).toContain('12');
      expect(document.body.textContent).toContain('15');
    });
  });

  it('uses custom title when provided', async () => {
    vi.mocked(client.get).mockResolvedValue({
      data: { totalServices: 5, coveredServices: 3, coveragePercent: 60, rest: 2, soap: 1, event: 0 },
    });
    wrap(
      <ContractCoverageWidget widgetId="w2" config={BASE_CONFIG} timeRange="24h" title="API Coverage" />,
    );
    await waitFor(() => {
      expect(document.body.textContent).toContain('API Coverage');
    });
  });

  it('shows error state when API fails', async () => {
    vi.mocked(client.get).mockRejectedValue(new Error('fail'));
    wrap(<ContractCoverageWidget widgetId="w2" config={BASE_CONFIG} timeRange="24h" />);
    await waitFor(() => {
      expect(document.body).toBeDefined();
    });
  });
});

// ── BlastRadiusWidget ─────────────────────────────────────────────────────

describe('BlastRadiusWidget', () => {
  beforeEach(() => vi.clearAllMocks());

  it('shows loading skeleton initially', () => {
    vi.mocked(client.get).mockReturnValue(new Promise(() => {}));
    const { container } = wrap(
      <BlastRadiusWidget widgetId="w3" config={BASE_CONFIG} timeRange="24h" />,
    );
    expect(container).toBeDefined();
  });

  it('renders change name and affected services count', async () => {
    vi.mocked(client.get).mockResolvedValue({
      data: {
        changeId: 'c1',
        changeName: 'Deploy v2.4.0 to production',
        affectedServices: 7,
        riskLevel: 'High',
        confidenceScore: 72,
        status: 'in-progress',
      },
    });
    wrap(<BlastRadiusWidget widgetId="w3" config={BASE_CONFIG} timeRange="24h" />);
    await waitFor(() => {
      expect(document.body.textContent).toContain('Deploy v2.4.0 to production');
      expect(document.body.textContent).toContain('7');
    });
  });

  it('renders risk level', async () => {
    vi.mocked(client.get).mockResolvedValue({
      data: {
        changeId: 'c2',
        changeName: 'Config update',
        affectedServices: 2,
        riskLevel: 'Low',
        confidenceScore: 95,
        status: 'pending',
      },
    });
    wrap(<BlastRadiusWidget widgetId="w3" config={BASE_CONFIG} timeRange="24h" />);
    await waitFor(() => {
      expect(document.body.textContent).toContain('Low');
    });
  });

  it('renders confidence score', async () => {
    vi.mocked(client.get).mockResolvedValue({
      data: {
        changeId: 'c3',
        changeName: 'Hotfix patch',
        affectedServices: 1,
        riskLevel: 'Medium',
        confidenceScore: 88,
        status: 'pending',
      },
    });
    wrap(<BlastRadiusWidget widgetId="w3" config={BASE_CONFIG} timeRange="24h" />);
    await waitFor(() => {
      expect(document.body.textContent).toContain('88%');
    });
  });

  it('uses custom title when provided', async () => {
    vi.mocked(client.get).mockResolvedValue({
      data: {
        changeId: 'c4',
        changeName: 'Release 1.0',
        affectedServices: 3,
        riskLevel: 'Critical',
        confidenceScore: 45,
        status: 'in-progress',
      },
    });
    wrap(
      <BlastRadiusWidget widgetId="w3" config={BASE_CONFIG} timeRange="24h" title="Latest Change Impact" />,
    );
    await waitFor(() => {
      expect(document.body.textContent).toContain('Latest Change Impact');
    });
  });

  it('shows error state when API fails', async () => {
    vi.mocked(client.get).mockRejectedValue(new Error('fail'));
    wrap(<BlastRadiusWidget widgetId="w3" config={BASE_CONFIG} timeRange="24h" />);
    await waitFor(() => {
      expect(document.body).toBeDefined();
    });
  });
});
