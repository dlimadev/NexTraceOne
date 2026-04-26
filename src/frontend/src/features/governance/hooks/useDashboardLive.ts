import { useCallback, useEffect, useRef, useState } from 'react';

// ── Types ──────────────────────────────────────────────────────────────────

export interface LiveEvent {
  eventType: string;
  widgetId: string | null;
  timestamp: string;
  isSimulated: boolean;
  payload: unknown;
}

interface UseDashboardLiveOptions {
  dashboardId: string;
  tenantId: string;
  widgetIds?: string[];
  enabled?: boolean;
  /** Fallback polling interval in ms when SSE is unavailable (default: 30000) */
  pollIntervalMs?: number;
}

interface UseDashboardLiveResult {
  isLive: boolean;
  isSimulated: boolean;
  lastEvent: LiveEvent | null;
  /** Per-widget: widgetId → latest event for that widget */
  widgetEvents: Record<string, LiveEvent>;
  error: string | null;
  /** Call to manually reconnect after an error */
  reconnect: () => void;
}

// ── Hook ───────────────────────────────────────────────────────────────────

export function useDashboardLive({
  dashboardId,
  tenantId,
  widgetIds,
  enabled = false,
  pollIntervalMs = 30_000,
}: UseDashboardLiveOptions): UseDashboardLiveResult {
  const [isLive, setIsLive] = useState(false);
  const [isSimulated, setIsSimulated] = useState(false);
  const [lastEvent, setLastEvent] = useState<LiveEvent | null>(null);
  const [widgetEvents, setWidgetEvents] = useState<Record<string, LiveEvent>>({});
  const [error, setError] = useState<string | null>(null);
  const [reconnectKey, setReconnectKey] = useState(0);

  const sseRef = useRef<EventSource | null>(null);
  const pollRef = useRef<ReturnType<typeof setInterval> | null>(null);

  const reconnect = useCallback(() => setReconnectKey(k => k + 1), []);

  const handleEvent = useCallback((evt: LiveEvent) => {
    setLastEvent(evt);
    setIsSimulated(evt.isSimulated);
    if (evt.widgetId) {
      setWidgetEvents(prev => ({ ...prev, [evt.widgetId!]: evt }));
    }
  }, []);

  useEffect(() => {
    if (!enabled || !dashboardId || !tenantId) return;

    const params = new URLSearchParams({ tenantId });
    if (widgetIds?.length) params.set('widgetIds', widgetIds.join(','));
    const url = `/api/v1/governance/dashboards/${dashboardId}/live?${params}`;

    let usingSse = false;

    // Try SSE first
    if (typeof EventSource !== 'undefined') {
      try {
        const es = new EventSource(url);
        sseRef.current = es;
        usingSse = true;

        es.onopen = () => {
          setIsLive(true);
          setError(null);
        };

        es.onmessage = (e) => {
          try {
            const data = JSON.parse(e.data) as LiveEvent;
            handleEvent(data);
          } catch {
            // ignore parse errors
          }
        };

        // Named event handlers
        const handleNamed = (e: MessageEvent) => {
          try {
            const data = JSON.parse(e.data) as LiveEvent;
            handleEvent(data);
          } catch {
            // ignore
          }
        };

        es.addEventListener('widget.refresh', handleNamed);
        es.addEventListener('annotation.new', handleNamed);
        es.addEventListener('heartbeat', handleNamed);

        es.onerror = () => {
          setIsLive(false);
          setError('SSE connection lost — switching to polling fallback.');
          es.close();
          sseRef.current = null;
          usingSse = false;
          startPolling();
        };
      } catch {
        usingSse = false;
      }
    }

    function startPolling() {
      if (pollRef.current) return;
      pollRef.current = setInterval(async () => {
        try {
          const since = lastEvent?.timestamp ?? new Date(Date.now() - pollIntervalMs).toISOString();
          const deltaParams = new URLSearchParams({ tenantId, since });
          // Poll the first widgetId (or 'default')
          const wid = widgetIds?.[0] ?? 'default';
          const res = await fetch(
            `/api/v1/governance/dashboards/${dashboardId}/widgets/${wid}/delta?${deltaParams}`
          );
          if (res.ok) {
            setIsLive(true);
            setError(null);
          }
        } catch {
          setIsLive(false);
        }
      }, pollIntervalMs);
    }

    if (!usingSse) startPolling();

    return () => {
      sseRef.current?.close();
      sseRef.current = null;
      if (pollRef.current) {
        clearInterval(pollRef.current);
        pollRef.current = null;
      }
      setIsLive(false);
    };
  // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [enabled, dashboardId, tenantId, reconnectKey]);

  return { isLive, isSimulated, lastEvent, widgetEvents, error, reconnect };
}
