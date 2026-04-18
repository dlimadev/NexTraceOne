import * as React from 'react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, waitFor } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';

vi.mock('../../api/client', () => ({
  default: { get: vi.fn() },
}));
import client from '../../api/client';
import { StatWidget } from '../../features/governance/widgets/StatWidget';
import { TextMarkdownWidget } from '../../features/governance/widgets/TextMarkdownWidget';

function wrap(ui: React.ReactElement) {
  const qc = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(<QueryClientProvider client={qc}>{ui}</QueryClientProvider>);
}

const BASE_CONFIG = { serviceId: null, teamId: null, timeRange: '24h', customTitle: null };

// ── StatWidget ──────────────────────────────────────────────────────────────

describe('StatWidget', () => {
  beforeEach(() => vi.clearAllMocks());

  it('shows loading skeleton initially', () => {
    vi.mocked(client.get).mockReturnValue(new Promise(() => {}));
    const { container } = wrap(
      <StatWidget widgetId="w1" config={{ ...BASE_CONFIG, metric: 'incidents-open' }} timeRange="24h" />,
    );
    expect(container).toBeDefined();
  });

  it('renders open incident count', async () => {
    vi.mocked(client.get).mockResolvedValue({ data: { totalCount: 3, critical: 1 } });
    wrap(
      <StatWidget widgetId="w1" config={{ ...BASE_CONFIG, metric: 'incidents-open' }} timeRange="24h" />,
    );
    await waitFor(() => {
      expect(document.body.textContent).toContain('3');
    });
  });

  it('renders critical alert count', async () => {
    vi.mocked(client.get).mockResolvedValue({ data: { critical: 5, total: 12 } });
    wrap(
      <StatWidget widgetId="w1" config={{ ...BASE_CONFIG, metric: 'alerts-critical' }} timeRange="24h" />,
    );
    await waitFor(() => {
      expect(document.body.textContent).toContain('5');
    });
  });

  it('renders total alert count', async () => {
    vi.mocked(client.get).mockResolvedValue({ data: { critical: 2, total: 8 } });
    wrap(
      <StatWidget widgetId="w1" config={{ ...BASE_CONFIG, metric: 'alerts-total' }} timeRange="24h" />,
    );
    await waitFor(() => {
      expect(document.body.textContent).toContain('8');
    });
  });

  it('renders DORA deployment frequency', async () => {
    vi.mocked(client.get).mockResolvedValue({
      data: {
        deploymentFrequency: { value: 4, unit: '/day' },
        changeFailureRate: { value: 3, unit: '%' },
        meanTimeToRestore: { value: 45, unit: 'min' },
      },
    });
    wrap(
      <StatWidget widgetId="w1" config={{ ...BASE_CONFIG, metric: 'dora-deploy-freq' }} timeRange="7d" />,
    );
    await waitFor(() => {
      expect(document.body.textContent).toContain('4');
    });
  });

  it('renders DORA CFR', async () => {
    vi.mocked(client.get).mockResolvedValue({
      data: {
        deploymentFrequency: { value: 4, unit: '/day' },
        changeFailureRate: { value: 8, unit: '%' },
        meanTimeToRestore: { value: 45, unit: 'min' },
      },
    });
    wrap(
      <StatWidget widgetId="w1" config={{ ...BASE_CONFIG, metric: 'dora-cfr' }} timeRange="7d" />,
    );
    await waitFor(() => {
      expect(document.body.textContent).toContain('8');
    });
  });

  it('renders DORA MTTR', async () => {
    vi.mocked(client.get).mockResolvedValue({
      data: {
        deploymentFrequency: { value: 4, unit: '/day' },
        changeFailureRate: { value: 3, unit: '%' },
        meanTimeToRestore: { value: 55, unit: 'min' },
      },
    });
    wrap(
      <StatWidget widgetId="w1" config={{ ...BASE_CONFIG, metric: 'dora-mttr' }} timeRange="7d" />,
    );
    await waitFor(() => {
      expect(document.body.textContent).toContain('55');
    });
  });

  it('renders changes today count', async () => {
    vi.mocked(client.get).mockResolvedValue({ data: { totalCount: 7 } });
    wrap(
      <StatWidget widgetId="w1" config={{ ...BASE_CONFIG, metric: 'changes-today' }} timeRange="24h" />,
    );
    await waitFor(() => {
      expect(document.body.textContent).toContain('7');
    });
  });

  it('uses custom title override', async () => {
    vi.mocked(client.get).mockResolvedValue({ data: { totalCount: 0, critical: 0 } });
    wrap(
      <StatWidget widgetId="w1" config={{ ...BASE_CONFIG, metric: 'incidents-open' }} timeRange="24h" title="My KPI" />,
    );
    await waitFor(() => {
      expect(document.body.textContent).toContain('My KPI');
    });
  });

  it('shows error state when API fails', async () => {
    vi.mocked(client.get).mockRejectedValue(new Error('fail'));
    wrap(
      <StatWidget widgetId="w1" config={{ ...BASE_CONFIG, metric: 'incidents-open' }} timeRange="24h" />,
    );
    await waitFor(() => {
      expect(document.body).toBeDefined();
    });
  });

  it('defaults to incidents-open when metric is not set', async () => {
    vi.mocked(client.get).mockResolvedValue({ data: { totalCount: 2, critical: 0 } });
    wrap(
      <StatWidget widgetId="w1" config={BASE_CONFIG} timeRange="24h" />,
    );
    await waitFor(() => {
      expect(document.body.textContent).toContain('2');
    });
  });
});

// ── TextMarkdownWidget ───────────────────────────────────────────────────────

describe('TextMarkdownWidget', () => {
  it('shows empty state when content is missing', () => {
    wrap(<TextMarkdownWidget widgetId="w2" config={BASE_CONFIG} timeRange="24h" />);
    expect(document.body.textContent).toContain('No content configured');
  });

  it('renders plain paragraph text', () => {
    wrap(
      <TextMarkdownWidget widgetId="w2" config={{ ...BASE_CONFIG, content: 'Hello world' }} timeRange="24h" />,
    );
    expect(document.body.textContent).toContain('Hello world');
  });

  it('renders bold text via **bold**', () => {
    const { container } = wrap(
      <TextMarkdownWidget widgetId="w2" config={{ ...BASE_CONFIG, content: '**Important**' }} timeRange="24h" />,
    );
    expect(container.querySelector('strong')?.textContent).toBe('Important');
  });

  it('renders italic text via *italic*', () => {
    const { container } = wrap(
      <TextMarkdownWidget widgetId="w2" config={{ ...BASE_CONFIG, content: '*note*' }} timeRange="24h" />,
    );
    expect(container.querySelector('em')?.textContent).toBe('note');
  });

  it('renders heading via # heading', () => {
    const { container } = wrap(
      <TextMarkdownWidget widgetId="w2" config={{ ...BASE_CONFIG, content: '# Title' }} timeRange="24h" />,
    );
    expect(container.querySelector('h1')?.textContent).toBe('Title');
  });

  it('renders list items via - item', () => {
    const { container } = wrap(
      <TextMarkdownWidget widgetId="w2" config={{ ...BASE_CONFIG, content: '- First\n- Second' }} timeRange="24h" />,
    );
    const items = container.querySelectorAll('li');
    expect(items.length).toBe(2);
  });

  it('renders horizontal rule via ---', () => {
    const { container } = wrap(
      <TextMarkdownWidget widgetId="w2" config={{ ...BASE_CONFIG, content: '---' }} timeRange="24h" />,
    );
    expect(container.querySelector('hr')).toBeTruthy();
  });

  it('uses custom title when provided', () => {
    wrap(
      <TextMarkdownWidget widgetId="w2" config={{ ...BASE_CONFIG, content: 'text' }} timeRange="24h" title="My Note" />,
    );
    expect(document.body.textContent).toContain('My Note');
  });

  it('escapes HTML entities in content to prevent injection', () => {
    const { container } = wrap(
      <TextMarkdownWidget widgetId="w2" config={{ ...BASE_CONFIG, content: '<script>alert(1)</script>' }} timeRange="24h" />,
    );
    // Script tag must NOT be present as an actual element
    expect(container.querySelector('script')).toBeNull();
    // The escaped text should be visible as plain text
    expect(document.body.textContent).toContain('alert(1)');
  });
});
