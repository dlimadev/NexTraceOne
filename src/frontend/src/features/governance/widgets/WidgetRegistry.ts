/**
 * Dashboard widget types and registry for NexTraceOne custom dashboards.
 * Each widget is a self-contained React component that fetches its own data.
 * Registry maps widget type strings to React components.
 */
import type { ComponentType } from 'react';

// ── Widget types ───────────────────────────────────────────────────────────

export type WidgetType =
  | 'dora-metrics'
  | 'service-scorecard'
  | 'incident-summary'
  | 'change-confidence'
  | 'cost-trend'
  | 'reliability-slo'
  | 'knowledge-graph'
  | 'on-call-status';

export const ALL_WIDGET_TYPES: WidgetType[] = [
  'dora-metrics',
  'service-scorecard',
  'incident-summary',
  'change-confidence',
  'cost-trend',
  'reliability-slo',
  'knowledge-graph',
  'on-call-status',
];

export interface WidgetConfig {
  serviceId?: string | null;
  teamId?: string | null;
  timeRange?: string | null;
  customTitle?: string | null;
}

export interface WidgetSlot {
  widgetId: string;
  type: WidgetType;
  posX: number;
  posY: number;
  width: number;
  height: number;
  serviceId?: string | null;
  teamId?: string | null;
  timeRange?: string | null;
  customTitle?: string | null;
}

export interface WidgetProps {
  widgetId: string;
  config: WidgetConfig;
  environmentId?: string | null;
  /** Effective time range (from global override or widget config) */
  timeRange: string;
  /** Display title override */
  title?: string | null;
}

// ── Registry ───────────────────────────────────────────────────────────────

/** Map of widget type → React component (lazy-loaded from individual files) */
export type WidgetComponentType = ComponentType<WidgetProps>;

/** Widget metadata for the builder UI */
export interface WidgetMeta {
  type: WidgetType;
  labelKey: string;
  /** Default grid size */
  defaultWidth: number;
  defaultHeight: number;
  /** Persona relevance hint */
  personas: string[];
}

export const WIDGET_META: Record<WidgetType, WidgetMeta> = {
  'dora-metrics': {
    type: 'dora-metrics',
    labelKey: 'governance.customDashboards.widgets.doraMetrics',
    defaultWidth: 2,
    defaultHeight: 2,
    personas: ['Engineer', 'TechLead', 'Architect', 'Executive'],
  },
  'service-scorecard': {
    type: 'service-scorecard',
    labelKey: 'governance.customDashboards.widgets.serviceScorecard',
    defaultWidth: 2,
    defaultHeight: 2,
    personas: ['Engineer', 'TechLead', 'Architect'],
  },
  'incident-summary': {
    type: 'incident-summary',
    labelKey: 'governance.customDashboards.widgets.incidentSummary',
    defaultWidth: 2,
    defaultHeight: 2,
    personas: ['Engineer', 'TechLead', 'Executive'],
  },
  'change-confidence': {
    type: 'change-confidence',
    labelKey: 'governance.customDashboards.widgets.changeConfidence',
    defaultWidth: 2,
    defaultHeight: 2,
    personas: ['Engineer', 'TechLead', 'Architect'],
  },
  'cost-trend': {
    type: 'cost-trend',
    labelKey: 'governance.customDashboards.widgets.costTrend',
    defaultWidth: 2,
    defaultHeight: 2,
    personas: ['Executive', 'Product', 'Architect'],
  },
  'reliability-slo': {
    type: 'reliability-slo',
    labelKey: 'governance.customDashboards.widgets.reliabilitySlo',
    defaultWidth: 2,
    defaultHeight: 2,
    personas: ['Engineer', 'TechLead', 'Executive'],
  },
  'knowledge-graph': {
    type: 'knowledge-graph',
    labelKey: 'governance.customDashboards.widgets.knowledgeGraph',
    defaultWidth: 3,
    defaultHeight: 3,
    personas: ['Architect', 'TechLead'],
  },
  'on-call-status': {
    type: 'on-call-status',
    labelKey: 'governance.customDashboards.widgets.onCallStatus',
    defaultWidth: 2,
    defaultHeight: 1,
    personas: ['Engineer', 'TechLead'],
  },
};

/** Convert a time range string (e.g. '7d') to an approximate number of days for API calls */
export function timeRangeToDays(timeRange: string): number {
  switch (timeRange) {
    case '1h': return 1;
    case '6h': return 1;
    case '24h': return 1;
    case '7d': return 7;
    case '30d': return 30;
    default: return 1;
  }
}

/** Time range options available for global and per-widget selection */
export const TIME_RANGE_OPTIONS = [
  { value: '1h', labelKey: 'governance.dashboardView.timeRange.1h' },
  { value: '6h', labelKey: 'governance.dashboardView.timeRange.6h' },
  { value: '24h', labelKey: 'governance.dashboardView.timeRange.24h' },
  { value: '7d', labelKey: 'governance.dashboardView.timeRange.7d' },
  { value: '30d', labelKey: 'governance.dashboardView.timeRange.30d' },
] as const;
