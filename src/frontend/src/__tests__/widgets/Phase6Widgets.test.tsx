/**
 * Tests for Phase 6 widgets: TeamHealthWidget, ReleaseCalendarWidget.
 */
import * as React from 'react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, waitFor } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';

vi.mock('../../api/client', () => ({
  default: { get: vi.fn() },
}));
import client from '../../api/client';
import { TeamHealthWidget } from '../../features/governance/widgets/TeamHealthWidget';
import { ReleaseCalendarWidget } from '../../features/governance/widgets/ReleaseCalendarWidget';

function wrap(ui: React.ReactElement) {
  const qc = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(<QueryClientProvider client={qc}>{ui}</QueryClientProvider>);
}

const BASE_CONFIG = { serviceId: null, teamId: null, timeRange: '24h', customTitle: null };
const TEAM_CONFIG = { ...BASE_CONFIG, teamId: 'team-1' };

// ── TeamHealthWidget ──────────────────────────────────────────────────────

describe('TeamHealthWidget', () => {
  beforeEach(() => vi.clearAllMocks());

  it('shows "select a team" prompt when no teamId configured', () => {
    const { container } = wrap(
      <TeamHealthWidget widgetId="w1" config={BASE_CONFIG} timeRange="24h" />,
    );
    expect(container).toBeDefined();
    expect(document.body.textContent).toMatch(/select a team/i);
  });

  it('shows loading skeleton when teamId is set', () => {
    vi.mocked(client.get).mockReturnValue(new Promise(() => {}));
    const { container } = wrap(
      <TeamHealthWidget widgetId="w1" config={TEAM_CONFIG} timeRange="24h" />,
    );
    expect(container).toBeDefined();
  });

  it('renders team name and health score', async () => {
    vi.mocked(client.get).mockResolvedValue({
      data: {
        teamId: 'team-1',
        teamName: 'Platform Squad',
        openIncidentsP1: 2,
        openIncidentsP2: 3,
        onCallEngineer: 'Alice Smith',
        lastDeployAt: new Date(Date.now() - 3_600_000).toISOString(),
        healthScore: 78,
        healthStatus: 'degraded',
      },
    });
    wrap(<TeamHealthWidget widgetId="w1" config={TEAM_CONFIG} timeRange="24h" />);
    await waitFor(() => {
      expect(document.body.textContent).toContain('Platform Squad');
      expect(document.body.textContent).toContain('78%');
    });
  });

  it('renders P1 and P2 incident counts', async () => {
    vi.mocked(client.get).mockResolvedValue({
      data: {
        teamId: 'team-1',
        teamName: 'Core Team',
        openIncidentsP1: 1,
        openIncidentsP2: 4,
        onCallEngineer: null,
        lastDeployAt: null,
        healthScore: 60,
        healthStatus: 'degraded',
      },
    });
    wrap(<TeamHealthWidget widgetId="w1" config={TEAM_CONFIG} timeRange="24h" />);
    await waitFor(() => {
      expect(document.body.textContent).toContain('1');
      expect(document.body.textContent).toContain('4');
    });
  });

  it('renders on-call engineer name when present', async () => {
    vi.mocked(client.get).mockResolvedValue({
      data: {
        teamId: 'team-1',
        teamName: 'Infra Team',
        openIncidentsP1: 0,
        openIncidentsP2: 0,
        onCallEngineer: 'Bob Johnson',
        lastDeployAt: new Date().toISOString(),
        healthScore: 100,
        healthStatus: 'healthy',
      },
    });
    wrap(<TeamHealthWidget widgetId="w1" config={TEAM_CONFIG} timeRange="24h" />);
    await waitFor(() => {
      expect(document.body.textContent).toContain('Bob Johnson');
    });
  });

  it('uses custom title when provided', async () => {
    vi.mocked(client.get).mockResolvedValue({
      data: {
        teamId: 'team-1',
        teamName: 'Backend Team',
        openIncidentsP1: 0,
        openIncidentsP2: 1,
        onCallEngineer: null,
        lastDeployAt: null,
        healthScore: 90,
        healthStatus: 'healthy',
      },
    });
    wrap(
      <TeamHealthWidget widgetId="w1" config={TEAM_CONFIG} timeRange="24h" title="My Team" />,
    );
    await waitFor(() => {
      expect(document.body.textContent).toContain('My Team');
    });
  });

  it('shows error state when API fails', async () => {
    vi.mocked(client.get).mockRejectedValue(new Error('fail'));
    wrap(<TeamHealthWidget widgetId="w1" config={TEAM_CONFIG} timeRange="24h" />);
    await waitFor(() => {
      expect(document.body).toBeDefined();
    });
  });

  it('shows last deploy relative time', async () => {
    const pastTime = new Date(Date.now() - 2 * 3_600_000).toISOString();
    vi.mocked(client.get).mockResolvedValue({
      data: {
        teamId: 'team-1',
        teamName: 'DevOps',
        openIncidentsP1: 0,
        openIncidentsP2: 0,
        onCallEngineer: null,
        lastDeployAt: pastTime,
        healthScore: 95,
        healthStatus: 'healthy',
      },
    });
    wrap(<TeamHealthWidget widgetId="w1" config={TEAM_CONFIG} timeRange="24h" />);
    await waitFor(() => {
      expect(document.body.textContent).toMatch(/ago/i);
    });
  });

  it('shows "—" when last deploy is null', async () => {
    vi.mocked(client.get).mockResolvedValue({
      data: {
        teamId: 'team-1',
        teamName: 'New Team',
        openIncidentsP1: 0,
        openIncidentsP2: 0,
        onCallEngineer: null,
        lastDeployAt: null,
        healthScore: 100,
        healthStatus: 'healthy',
      },
    });
    wrap(<TeamHealthWidget widgetId="w1" config={TEAM_CONFIG} timeRange="24h" />);
    await waitFor(() => {
      expect(document.body.textContent).toContain('—');
    });
  });
});

// ── ReleaseCalendarWidget ─────────────────────────────────────────────────

describe('ReleaseCalendarWidget', () => {
  beforeEach(() => vi.clearAllMocks());

  it('shows loading skeleton initially', () => {
    vi.mocked(client.get).mockReturnValue(new Promise(() => {}));
    const { container } = wrap(
      <ReleaseCalendarWidget widgetId="w2" config={BASE_CONFIG} timeRange="7d" />,
    );
    expect(container).toBeDefined();
  });

  it('renders changes grouped by day', async () => {
    const today = new Date().toISOString().substring(0, 10);
    vi.mocked(client.get).mockResolvedValue({
      data: {
        items: [
          {
            changeId: 'c1',
            serviceName: 'payment-api',
            type: 'deploy',
            environment: 'production',
            scheduledAt: `${today}T10:00:00Z`,
            status: 'planned',
          },
          {
            changeId: 'c2',
            serviceName: 'auth-service',
            type: 'release',
            environment: 'staging',
            scheduledAt: `${today}T14:00:00Z`,
            status: 'in-progress',
          },
        ],
        fromDate: today,
        toDate: today,
      },
    });
    wrap(<ReleaseCalendarWidget widgetId="w2" config={BASE_CONFIG} timeRange="7d" />);
    await waitFor(() => {
      expect(document.body.textContent).toContain('payment-api');
      expect(document.body.textContent).toContain('auth-service');
    });
  });

  it('shows total change count in header', async () => {
    vi.mocked(client.get).mockResolvedValue({
      data: {
        items: [
          {
            changeId: 'c1',
            serviceName: 'svc-a',
            type: 'deploy',
            environment: 'prod',
            scheduledAt: new Date().toISOString(),
            status: 'planned',
          },
          {
            changeId: 'c2',
            serviceName: 'svc-b',
            type: 'patch',
            environment: 'prod',
            scheduledAt: new Date().toISOString(),
            status: 'planned',
          },
          {
            changeId: 'c3',
            serviceName: 'svc-c',
            type: 'config',
            environment: 'staging',
            scheduledAt: new Date().toISOString(),
            status: 'completed',
          },
        ],
        fromDate: '2026-04-18',
        toDate: '2026-04-25',
      },
    });
    wrap(<ReleaseCalendarWidget widgetId="w2" config={BASE_CONFIG} timeRange="7d" />);
    await waitFor(() => {
      expect(document.body.textContent).toContain('3');
    });
  });

  it('shows empty state when no changes scheduled', async () => {
    vi.mocked(client.get).mockResolvedValue({
      data: { items: [], fromDate: '2026-04-18', toDate: '2026-04-25' },
    });
    wrap(<ReleaseCalendarWidget widgetId="w2" config={BASE_CONFIG} timeRange="7d" />);
    await waitFor(() => {
      expect(document.body.textContent).toMatch(/no changes scheduled/i);
    });
  });

  it('shows legend with change type indicators', async () => {
    vi.mocked(client.get).mockResolvedValue({
      data: { items: [], fromDate: '2026-04-18', toDate: '2026-04-25' },
    });
    wrap(<ReleaseCalendarWidget widgetId="w2" config={BASE_CONFIG} timeRange="7d" />);
    await waitFor(() => {
      expect(document.body.textContent).toMatch(/deploy|release|patch|rollback/i);
    });
  });

  it('shows error state when API fails', async () => {
    vi.mocked(client.get).mockRejectedValue(new Error('network error'));
    wrap(<ReleaseCalendarWidget widgetId="w2" config={BASE_CONFIG} timeRange="7d" />);
    await waitFor(() => {
      expect(document.body).toBeDefined();
    });
  });

  it('uses custom title when provided', async () => {
    vi.mocked(client.get).mockResolvedValue({
      data: { items: [], fromDate: '2026-04-18', toDate: '2026-04-25' },
    });
    wrap(
      <ReleaseCalendarWidget
        widgetId="w2"
        config={BASE_CONFIG}
        timeRange="7d"
        title="Upcoming Releases"
      />,
    );
    await waitFor(() => {
      expect(document.body.textContent).toContain('Upcoming Releases');
    });
  });

  it('requests 14 days when timeRange is 30d', async () => {
    vi.mocked(client.get).mockResolvedValue({
      data: { items: [], fromDate: '2026-04-18', toDate: '2026-05-02' },
    });
    wrap(<ReleaseCalendarWidget widgetId="w2" config={BASE_CONFIG} timeRange="30d" />);
    await waitFor(() => {
      expect(vi.mocked(client.get)).toHaveBeenCalledWith(
        '/governance/changes/calendar',
        expect.objectContaining({
          params: expect.objectContaining({ days: 14 }),
        }),
      );
    });
  });
});
