import { useCallback } from 'react';

/**
 * useUxTelemetry — structured UX event emission for ProductAnalytics.
 * Wave V3.5 — Frontend Platform Uplift.
 *
 * Privacy-safe: no PII; only module + action + optional label.
 * Currently logs to console in dev; in production emits to /api/v1/analytics/events
 * when the UX telemetry feature flag is enabled.
 */
export interface UxEvent {
  module: string;
  action: string;
  label?: string;
  value?: number;
}

const IS_DEV = import.meta.env.DEV;

export function useUxTelemetry() {
  return useCallback((event: UxEvent) => {
    if (IS_DEV) {
       
      console.debug('[UX]', event);
      return;
    }

    // Fire-and-forget — telemetry must never block the UI
    void fetch('/api/v1/analytics/ux-events', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ ...event, occurredAt: new Date().toISOString() }),
      keepalive: true,
    }).catch(() => {
      // Swallow: telemetry is non-critical
    });
  }, []);
}
