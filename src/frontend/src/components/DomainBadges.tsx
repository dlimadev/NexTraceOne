import { useTranslation } from 'react-i18next';
import { Badge } from './Badge';

/**
 * Domain-specific badge components for NexTraceOne.
 *
 * These components express core domain concepts — confidence, compatibility,
 * risk, freshness — using consistent visual language aligned with the product
 * design system.
 *
 * @see docs/DESIGN-SYSTEM.md — badge variant mapping
 * @see docs/UX-PRINCIPLES.md — domain-driven component design
 */

// ── Confidence Badge ──────────────────────────────────────────────────────────

type ConfidenceLevel = 'high' | 'medium' | 'low' | 'unknown';

function confidenceVariant(level: ConfidenceLevel): 'success' | 'warning' | 'danger' | 'info' {
  switch (level) {
    case 'high': return 'success';
    case 'medium': return 'warning';
    case 'low': return 'danger';
    default: return 'info';
  }
}

/** Displays a change confidence level badge (high/medium/low). */
export function ConfidenceBadge({ level }: { level: ConfidenceLevel }) {
  const { t } = useTranslation();
  return (
    <Badge variant={confidenceVariant(level)}>
      {t(`domainBadges.confidence.${level}`)}
    </Badge>
  );
}

// ── Compatibility Badge ──────────────────────────────────────────────────────

type CompatibilityLevel = 'compatible' | 'breaking' | 'additive' | 'unknown';

function compatibilityVariant(level: CompatibilityLevel): 'success' | 'danger' | 'warning' | 'info' {
  switch (level) {
    case 'compatible': return 'success';
    case 'additive': return 'warning';
    case 'breaking': return 'danger';
    default: return 'info';
  }
}

/** Displays a contract compatibility badge (compatible/breaking/additive). */
export function CompatibilityBadge({ level }: { level: CompatibilityLevel }) {
  const { t } = useTranslation();
  return (
    <Badge variant={compatibilityVariant(level)}>
      {t(`domainBadges.compatibility.${level}`)}
    </Badge>
  );
}

// ── Risk Badge ──────────────────────────────────────────────────────────────

type RiskLevel = 'critical' | 'high' | 'medium' | 'low' | 'none';

function riskVariant(level: RiskLevel): 'danger' | 'warning' | 'success' | 'info' {
  switch (level) {
    case 'critical':
    case 'high': return 'danger';
    case 'medium': return 'warning';
    case 'low': return 'success';
    default: return 'info';
  }
}

/** Displays a risk level badge. */
export function RiskBadge({ level }: { level: RiskLevel }) {
  const { t } = useTranslation();
  return (
    <Badge variant={riskVariant(level)}>
      {t(`domainBadges.risk.${level}`)}
    </Badge>
  );
}

// ── Freshness Badge ──────────────────────────────────────────────────────────

type FreshnessLevel = 'fresh' | 'stale' | 'outdated' | 'unknown';

function freshnessVariant(level: FreshnessLevel): 'success' | 'warning' | 'danger' | 'info' {
  switch (level) {
    case 'fresh': return 'success';
    case 'stale': return 'warning';
    case 'outdated': return 'danger';
    default: return 'info';
  }
}

/** Displays a data freshness badge for integration/source of truth. */
export function FreshnessBadge({ level }: { level: FreshnessLevel }) {
  const { t } = useTranslation();
  return (
    <Badge variant={freshnessVariant(level)}>
      {t(`domainBadges.freshness.${level}`)}
    </Badge>
  );
}

// ── Incident Severity Badge ─────────────────────────────────────────────────

type IncidentSeverity = 'critical' | 'major' | 'minor' | 'informational';

function severityVariant(sev: IncidentSeverity): 'danger' | 'warning' | 'info' | 'default' {
  switch (sev) {
    case 'critical': return 'danger';
    case 'major': return 'warning';
    case 'minor': return 'info';
    default: return 'default';
  }
}

/** Displays an incident severity badge. */
export function SeverityBadge({ severity }: { severity: IncidentSeverity }) {
  const { t } = useTranslation();
  return (
    <Badge variant={severityVariant(severity)}>
      {t(`domainBadges.severity.${severity}`)}
    </Badge>
  );
}
