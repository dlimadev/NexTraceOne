import type { WidgetType } from './WidgetRegistry';

// ── Types ──────────────────────────────────────────────────────────────────

export interface DrillContext {
  serviceId?: string | null;
  teamId?: string | null;
  environmentId?: string | null;
  from?: string | null;
  to?: string | null;
  /** Extra label/value pairs specific to the widget (e.g. incidentId, scorecard tier) */
  extra?: Record<string, string | null | undefined>;
}

export interface DrillDestination {
  /** Absolute path to navigate to */
  path: string;
  /** Human-readable label for the "Open with…" button */
  label: string;
  /** i18n key for the label (frontend resolves via t()) */
  labelKey: string;
}

// ── Route resolver ────────────────────────────────────────────────────────

/**
 * Returns the canonical drill-down destination for a given widget type and
 * context.  Returns null when the widget has no meaningful drill-down target.
 */
export function getDrillRoute(
  widgetType: WidgetType,
  ctx: DrillContext
): DrillDestination | null {
  const q = buildQs(ctx);

  switch (widgetType) {
    // ── Operations ─────────────────────────────────────────────────────
    case 'incident-count':
    case 'mttr-widget':
      return {
        path: `/operations/incidents${q({ serviceId: ctx.serviceId, from: ctx.from, to: ctx.to })}`,
        label: 'Open Incidents',
        labelKey: 'drillDown.openIncidents',
      };

    case 'slo-tracker':
      return {
        path: `/operations/slos${q({ serviceId: ctx.serviceId, environmentId: ctx.environmentId })}`,
        label: 'Open SLOs',
        labelKey: 'drillDown.openSlos',
      };

    // ── Changes ────────────────────────────────────────────────────────
    case 'dora-metrics':
    case 'change-failure-rate':
      return {
        path: `/changes/dora${q({ serviceId: ctx.serviceId, teamId: ctx.teamId, from: ctx.from, to: ctx.to })}`,
        label: 'Open DORA Metrics',
        labelKey: 'drillDown.openDora',
      };

    case 'release-calendar':
      return {
        path: `/changes/calendar${q({ serviceId: ctx.serviceId, from: ctx.from, to: ctx.to })}`,
        label: 'Open Release Calendar',
        labelKey: 'drillDown.openReleaseCalendar',
      };

    case 'change-score-trend':
      return {
        path: `/changes/scores${q({ serviceId: ctx.serviceId, from: ctx.from, to: ctx.to })}`,
        label: 'Open Change Scores',
        labelKey: 'drillDown.openChangeScores',
      };

    // ── Catalog ────────────────────────────────────────────────────────
    case 'service-health-matrix':
    case 'maturity-score':
      return {
        path: `/catalog/services${q({ teamId: ctx.teamId, environmentId: ctx.environmentId })}`,
        label: 'Open Service Catalog',
        labelKey: 'drillDown.openCatalog',
      };

    case 'dependency-map':
      return {
        path: `/catalog/dependencies${q({ serviceId: ctx.serviceId })}`,
        label: 'Open Dependency Map',
        labelKey: 'drillDown.openDependencyMap',
      };

    // ── Governance ─────────────────────────────────────────────────────
    case 'team-health':
      return {
        path: `/governance/teams${q({ teamId: ctx.teamId })}`,
        label: 'Open Team Dashboard',
        labelKey: 'drillDown.openTeams',
      };

    case 'compliance-summary':
    case 'policy-violations':
      return {
        path: `/governance/compliance${q({ serviceId: ctx.serviceId, teamId: ctx.teamId })}`,
        label: 'Open Compliance',
        labelKey: 'drillDown.openCompliance',
      };

    case 'risk-heatmap':
      return {
        path: `/governance/risk${q({ serviceId: ctx.serviceId, teamId: ctx.teamId })}`,
        label: 'Open Risk Center',
        labelKey: 'drillDown.openRisk',
      };

    // ── FinOps ─────────────────────────────────────────────────────────
    case 'cost-attribution':
    case 'finops-summary':
      return {
        path: `/governance/finops${q({ serviceId: ctx.serviceId, teamId: ctx.teamId, from: ctx.from, to: ctx.to })}`,
        label: 'Open FinOps',
        labelKey: 'drillDown.openFinOps',
      };

    // ── Technical Debt ─────────────────────────────────────────────────
    case 'tech-debt-trend':
      return {
        path: `/governance/technical-debt${q({ serviceId: ctx.serviceId, teamId: ctx.teamId })}`,
        label: 'Open Technical Debt',
        labelKey: 'drillDown.openTechDebt',
      };

    // ── Executive ──────────────────────────────────────────────────────
    case 'executive-kpis':
      return {
        path: `/governance/executive${q({})}`,
        label: 'Open Executive Overview',
        labelKey: 'drillDown.openExecutive',
      };

    // ── Query widget has no fixed destination ──────────────────────────
    case 'query-widget':
    default:
      return null;
  }
}

// ── URL builder ───────────────────────────────────────────────────────────

function buildQs(ctx: DrillContext) {
  return (params: Record<string, string | null | undefined>): string => {
    const merged: Record<string, string | null | undefined> = { ...params };
    // Always carry environmentId if available
    if (ctx.environmentId && !merged.environmentId) merged.environmentId = ctx.environmentId;

    const sp = new URLSearchParams();
    for (const [k, v] of Object.entries(merged)) {
      if (v != null && v !== '') sp.set(k, v);
    }
    const s = sp.toString();
    return s ? `?${s}` : '';
  };
}
