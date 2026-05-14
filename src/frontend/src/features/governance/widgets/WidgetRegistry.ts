/**
 * Dashboard widget types and registry for NexTraceOne custom dashboards.
 * Each widget is a self-contained React component that fetches its own data.
 * Registry maps widget type strings to React components.
 * Wave V3.2 — adds query-widget type, nqlQuery config field, and widget categories.
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
  | 'on-call-status'
  | 'alert-status'
  | 'change-timeline'
  | 'slo-gauge'
  | 'deployment-frequency'
  | 'stat'
  | 'text-markdown'
  | 'top-services'
  | 'contract-coverage'
  | 'blast-radius'
  | 'team-health'
  | 'release-calendar'
  | 'query-widget'
  // Extended widget types used in drill routes
  | 'incident-count'
  | 'mttr-widget'
  | 'slo-tracker'
  | 'change-failure-rate'
  | 'change-score-trend'
  | 'service-health-matrix'
  | 'maturity-score'
  | 'dependency-map'
  | 'compliance-summary'
  | 'policy-violations'
  | 'risk-heatmap'
  | 'cost-attribution'
  | 'finops-summary'
  | 'tech-debt-trend'
  | 'executive-kpis';

export const ALL_WIDGET_TYPES: WidgetType[] = [
  'dora-metrics',
  'service-scorecard',
  'incident-summary',
  'change-confidence',
  'cost-trend',
  'reliability-slo',
  'knowledge-graph',
  'on-call-status',
  'alert-status',
  'change-timeline',
  'slo-gauge',
  'deployment-frequency',
  'stat',
  'text-markdown',
  'top-services',
  'contract-coverage',
  'blast-radius',
  'team-health',
  'release-calendar',
  'query-widget',
];

export interface WidgetConfig {
  serviceId?: string | null;
  teamId?: string | null;
  timeRange?: string | null;
  customTitle?: string | null;
  /** StatWidget: which KPI metric to display */
  metric?: string | null;
  /** TextMarkdownWidget: markdown content stored in widget config */
  content?: string | null;
  /** QueryWidget (V3.2): NQL query string */
  nqlQuery?: string | null;
  /** QueryWidget (V3.2): render hint override (table|line|bar|area|stat|heatmap) */
  renderHint?: string | null;
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
  metric?: string | null;
  content?: string | null;
  nqlQuery?: string | null;
  renderHint?: string | null;
}

export interface WidgetProps {
  widgetId: string;
  config: WidgetConfig;
  environmentId?: string | null;
  /** Effective time range (from global override or widget config) */
  timeRange: string;
  /** Display title override */
  title?: string | null;
  /** V3.3: Called when a widget wants to apply a cross-filter to other widgets */
  onCrossFilter?: (filter: { serviceId?: string | null; teamId?: string | null; from?: string | null; to?: string | null }) => void;
  /** V3.3: Called when a widget requests drill-down navigation */
  onDrillDown?: (path: string) => void;
  /** V3.3: Active cross-filter state (from context) so widgets can dim un-matching data */
  activeCrossFilter?: { serviceId?: string | null; teamId?: string | null; from?: string | null; to?: string | null } | null;
}

// ── Widget categories (V3.2) ───────────────────────────────────────────────

export type WidgetCategory =
  | 'all'
  | 'services'
  | 'changes'
  | 'operations'
  | 'knowledge'
  | 'finops'
  | 'ai'
  | 'customQuery';

export const WIDGET_CATEGORIES: { value: WidgetCategory; labelKey: string }[] = [
  { value: 'all',         labelKey: 'widgetCategories.all' },
  { value: 'services',    labelKey: 'widgetCategories.services' },
  { value: 'changes',     labelKey: 'widgetCategories.changes' },
  { value: 'operations',  labelKey: 'widgetCategories.operations' },
  { value: 'knowledge',   labelKey: 'widgetCategories.knowledge' },
  { value: 'finops',      labelKey: 'widgetCategories.finops' },
  { value: 'customQuery', labelKey: 'widgetCategories.customQuery' },
];

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
  /** Widget category for the palette (V3.2) */
  category: WidgetCategory;
}

export const WIDGET_META: Record<WidgetType, WidgetMeta> = {
  'dora-metrics': {
    type: 'dora-metrics',
    labelKey: 'governance.customDashboards.widgets.doraMetrics',
    defaultWidth: 2,
    defaultHeight: 2,
    personas: ['Engineer', 'TechLead', 'Architect', 'Executive'],
    category: 'changes',
  },
  'service-scorecard': {
    type: 'service-scorecard',
    labelKey: 'governance.customDashboards.widgets.serviceScorecard',
    defaultWidth: 2,
    defaultHeight: 2,
    personas: ['Engineer', 'TechLead', 'Architect'],
    category: 'services',
  },
  'incident-summary': {
    type: 'incident-summary',
    labelKey: 'governance.customDashboards.widgets.incidentSummary',
    defaultWidth: 2,
    defaultHeight: 2,
    personas: ['Engineer', 'TechLead', 'Executive'],
    category: 'operations',
  },
  'change-confidence': {
    type: 'change-confidence',
    labelKey: 'governance.customDashboards.widgets.changeConfidence',
    defaultWidth: 2,
    defaultHeight: 2,
    personas: ['Engineer', 'TechLead', 'Architect'],
    category: 'changes',
  },
  'cost-trend': {
    type: 'cost-trend',
    labelKey: 'governance.customDashboards.widgets.costTrend',
    defaultWidth: 2,
    defaultHeight: 2,
    personas: ['Executive', 'Product', 'Architect'],
    category: 'finops',
  },
  'reliability-slo': {
    type: 'reliability-slo',
    labelKey: 'governance.customDashboards.widgets.reliabilitySlo',
    defaultWidth: 2,
    defaultHeight: 2,
    personas: ['Engineer', 'TechLead', 'Executive'],
    category: 'operations',
  },
  'knowledge-graph': {
    type: 'knowledge-graph',
    labelKey: 'governance.customDashboards.widgets.knowledgeGraph',
    defaultWidth: 3,
    defaultHeight: 3,
    personas: ['Architect', 'TechLead'],
    category: 'knowledge',
  },
  'on-call-status': {
    type: 'on-call-status',
    labelKey: 'governance.customDashboards.widgets.onCallStatus',
    defaultWidth: 2,
    defaultHeight: 1,
    personas: ['Engineer', 'TechLead'],
    category: 'operations',
  },
  'alert-status': {
    type: 'alert-status',
    labelKey: 'governance.customDashboards.widgets.alertStatus',
    defaultWidth: 2,
    defaultHeight: 2,
    personas: ['Engineer', 'TechLead', 'Executive', 'Auditor'],
    category: 'operations',
  },
  'change-timeline': {
    type: 'change-timeline',
    labelKey: 'governance.customDashboards.widgets.changeTimeline',
    defaultWidth: 3,
    defaultHeight: 3,
    personas: ['Engineer', 'TechLead', 'Architect', 'Auditor'],
    category: 'changes',
  },
  'slo-gauge': {
    type: 'slo-gauge',
    labelKey: 'governance.customDashboards.widgets.sloGauge',
    defaultWidth: 2,
    defaultHeight: 2,
    personas: ['Engineer', 'TechLead', 'Executive', 'Product'],
    category: 'operations',
  },
  'deployment-frequency': {
    type: 'deployment-frequency',
    labelKey: 'governance.customDashboards.widgets.deploymentFrequency',
    defaultWidth: 3,
    defaultHeight: 2,
    personas: ['Engineer', 'TechLead', 'Executive', 'Product'],
    category: 'changes',
  },
  'stat': {
    type: 'stat',
    labelKey: 'governance.customDashboards.widgets.stat',
    defaultWidth: 1,
    defaultHeight: 1,
    personas: ['Engineer', 'TechLead', 'Architect', 'Executive', 'Product', 'Auditor'],
    category: 'services',
  },
  'text-markdown': {
    type: 'text-markdown',
    labelKey: 'governance.customDashboards.widgets.textMarkdown',
    defaultWidth: 2,
    defaultHeight: 1,
    personas: ['Engineer', 'TechLead', 'Architect', 'Executive', 'Product', 'PlatformAdmin', 'Auditor'],
    category: 'knowledge',
  },
  'top-services': {
    type: 'top-services',
    labelKey: 'governance.customDashboards.widgets.topServices',
    defaultWidth: 2,
    defaultHeight: 2,
    personas: ['Engineer', 'TechLead', 'Executive', 'Auditor'],
    category: 'services',
  },
  'contract-coverage': {
    type: 'contract-coverage',
    labelKey: 'governance.customDashboards.widgets.contractCoverage',
    defaultWidth: 2,
    defaultHeight: 2,
    personas: ['Architect', 'TechLead', 'PlatformAdmin', 'Auditor', 'Executive'],
    category: 'services',
  },
  'blast-radius': {
    type: 'blast-radius',
    labelKey: 'governance.customDashboards.widgets.blastRadius',
    defaultWidth: 2,
    defaultHeight: 2,
    personas: ['Engineer', 'TechLead', 'Architect', 'Executive'],
    category: 'changes',
  },
  'team-health': {
    type: 'team-health',
    labelKey: 'governance.customDashboards.widgets.teamHealth',
    defaultWidth: 2,
    defaultHeight: 2,
    personas: ['TechLead', 'Engineer', 'Executive', 'PlatformAdmin'],
    category: 'services',
  },
  'release-calendar': {
    type: 'release-calendar',
    labelKey: 'governance.customDashboards.widgets.releaseCalendar',
    defaultWidth: 3,
    defaultHeight: 2,
    personas: ['Engineer', 'TechLead', 'Architect', 'Product'],
    category: 'changes',
  },
  'query-widget': {
    type: 'query-widget',
    labelKey: 'governance.customDashboards.widgets.queryWidget',
    defaultWidth: 3,
    defaultHeight: 3,
    personas: ['Engineer', 'TechLead', 'Architect', 'Executive', 'Product', 'PlatformAdmin', 'Auditor'],
    category: 'customQuery',
  },
  // Extended widget types
  'incident-count': {
    type: 'incident-count',
    labelKey: 'governance.customDashboards.widgets.incidentCount',
    defaultWidth: 1,
    defaultHeight: 1,
    personas: ['Engineer', 'TechLead', 'Executive'],
    category: 'operations',
  },
  'mttr-widget': {
    type: 'mttr-widget',
    labelKey: 'governance.customDashboards.widgets.mttrWidget',
    defaultWidth: 2,
    defaultHeight: 2,
    personas: ['Engineer', 'TechLead', 'Executive'],
    category: 'operations',
  },
  'slo-tracker': {
    type: 'slo-tracker',
    labelKey: 'governance.customDashboards.widgets.sloTracker',
    defaultWidth: 2,
    defaultHeight: 2,
    personas: ['Engineer', 'TechLead', 'Executive'],
    category: 'operations',
  },
  'change-failure-rate': {
    type: 'change-failure-rate',
    labelKey: 'governance.customDashboards.widgets.changeFailureRate',
    defaultWidth: 2,
    defaultHeight: 2,
    personas: ['Engineer', 'TechLead', 'Executive'],
    category: 'changes',
  },
  'change-score-trend': {
    type: 'change-score-trend',
    labelKey: 'governance.customDashboards.widgets.changeScoreTrend',
    defaultWidth: 3,
    defaultHeight: 2,
    personas: ['Engineer', 'TechLead', 'Architect'],
    category: 'changes',
  },
  'service-health-matrix': {
    type: 'service-health-matrix',
    labelKey: 'governance.customDashboards.widgets.serviceHealthMatrix',
    defaultWidth: 3,
    defaultHeight: 3,
    personas: ['TechLead', 'Architect', 'Executive'],
    category: 'services',
  },
  'maturity-score': {
    type: 'maturity-score',
    labelKey: 'governance.customDashboards.widgets.maturityScore',
    defaultWidth: 2,
    defaultHeight: 2,
    personas: ['Architect', 'TechLead', 'Executive'],
    category: 'services',
  },
  'dependency-map': {
    type: 'dependency-map',
    labelKey: 'governance.customDashboards.widgets.dependencyMap',
    defaultWidth: 3,
    defaultHeight: 3,
    personas: ['Architect', 'TechLead'],
    category: 'knowledge',
  },
  'compliance-summary': {
    type: 'compliance-summary',
    labelKey: 'governance.customDashboards.widgets.complianceSummary',
    defaultWidth: 2,
    defaultHeight: 2,
    personas: ['Auditor', 'Executive', 'PlatformAdmin'],
    category: 'operations',
  },
  'policy-violations': {
    type: 'policy-violations',
    labelKey: 'governance.customDashboards.widgets.policyViolations',
    defaultWidth: 2,
    defaultHeight: 2,
    personas: ['Auditor', 'TechLead', 'PlatformAdmin'],
    category: 'operations',
  },
  'risk-heatmap': {
    type: 'risk-heatmap',
    labelKey: 'governance.customDashboards.widgets.riskHeatmap',
    defaultWidth: 3,
    defaultHeight: 3,
    personas: ['Architect', 'Executive', 'Auditor'],
    category: 'operations',
  },
  'cost-attribution': {
    type: 'cost-attribution',
    labelKey: 'governance.customDashboards.widgets.costAttribution',
    defaultWidth: 2,
    defaultHeight: 2,
    personas: ['Executive', 'Product', 'PlatformAdmin'],
    category: 'finops',
  },
  'finops-summary': {
    type: 'finops-summary',
    labelKey: 'governance.customDashboards.widgets.finopsSummary',
    defaultWidth: 2,
    defaultHeight: 2,
    personas: ['Executive', 'Product', 'PlatformAdmin'],
    category: 'finops',
  },
  'tech-debt-trend': {
    type: 'tech-debt-trend',
    labelKey: 'governance.customDashboards.widgets.techDebtTrend',
    defaultWidth: 3,
    defaultHeight: 2,
    personas: ['Architect', 'TechLead', 'Executive'],
    category: 'services',
  },
  'executive-kpis': {
    type: 'executive-kpis',
    labelKey: 'governance.customDashboards.widgets.executiveKpis',
    defaultWidth: 3,
    defaultHeight: 2,
    personas: ['Executive', 'Product'],
    category: 'operations',
  },
};

/** Filter WIDGET_META by category (V3.2) */
export function getWidgetsByCategory(category: WidgetCategory): WidgetMeta[] {
  const all = Object.values(WIDGET_META);
  if (category === 'all') return all;
  return all.filter(w => w.category === category);
}

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

/** StatWidget: available KPI metric options */
export const STAT_METRIC_OPTIONS = [
  { value: 'incidents-open',    labelKey: 'governance.customDashboards.statMetric.incidentsOpen' },
  { value: 'alerts-critical',   labelKey: 'governance.customDashboards.statMetric.alertsCritical' },
  { value: 'alerts-total',      labelKey: 'governance.customDashboards.statMetric.alertsTotal' },
  { value: 'dora-deploy-freq',  labelKey: 'governance.customDashboards.statMetric.doraDeployFreq' },
  { value: 'dora-cfr',          labelKey: 'governance.customDashboards.statMetric.doraCfr' },
  { value: 'dora-mttr',         labelKey: 'governance.customDashboards.statMetric.doraMttr' },
  { value: 'changes-today',     labelKey: 'governance.customDashboards.statMetric.changesToday' },
] as const;

export type StatMetric = (typeof STAT_METRIC_OPTIONS)[number]['value'];

/** Time range options available for global and per-widget selection */
export const TIME_RANGE_OPTIONS = [
  { value: '1h', labelKey: 'governance.dashboardView.timeRange.1h' },
  { value: '6h', labelKey: 'governance.dashboardView.timeRange.6h' },
  { value: '24h', labelKey: 'governance.dashboardView.timeRange.24h' },
  { value: '7d', labelKey: 'governance.dashboardView.timeRange.7d' },
  { value: '30d', labelKey: 'governance.dashboardView.timeRange.30d' },
] as const;

/** NQL render hint options (V3.2) */
export const NQL_RENDER_HINTS = [
  { value: 'table',   labelKey: 'nqlEditor.renderHintTable' },
  { value: 'line',    labelKey: 'nqlEditor.renderHintLine' },
  { value: 'bar',     labelKey: 'nqlEditor.renderHintBar' },
  { value: 'area',    labelKey: 'nqlEditor.renderHintArea' },
  { value: 'stat',    labelKey: 'nqlEditor.renderHintStat' },
  { value: 'heatmap', labelKey: 'nqlEditor.renderHintHeatmap' },
] as const;

export type NqlRenderHint = (typeof NQL_RENDER_HINTS)[number]['value'];
