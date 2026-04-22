/**
 * Feature configuration registry for NexTraceOne frontend.
 * Each entry maps a feature flag/config key to its default value and i18n label.
 */

export interface FeatureConfigEntry {
  sort: number;
  key: string;
  label: string;
  defaultValue: boolean | number | string;
}

export const featureConfig: FeatureConfigEntry[] = [
  { sort: 12880, key: "report.approval_workflow.enabled", label: "reports.approvalWorkflow.enabled", defaultValue: true },
  { sort: 12890, key: "report.approval_workflow.lookback_days", label: "reports.approvalWorkflow.lookbackDays", defaultValue: 30 },
  { sort: 12900, key: "report.peer_review_coverage.enabled", label: "reports.peerReviewCoverage.enabled", defaultValue: true },
  { sort: 12910, key: "report.peer_review_coverage.high_risk_threshold", label: "reports.peerReviewCoverage.highRiskThreshold", defaultValue: 50 },
  { sort: 12920, key: "report.governance_escalation.enabled", label: "reports.governanceEscalation.enabled", defaultValue: true },
  { sort: 12930, key: "report.governance_escalation.break_glass_critical_threshold", label: "reports.governanceEscalation.breakGlassCriticalThreshold", defaultValue: 20 },
  { sort: 12940, key: "report.governance_escalation.jit_auto_approved_max_pct", label: "reports.governanceEscalation.jitAutoApprovedMaxPct", defaultValue: 80 },
  { sort: 12950, key: "report.governance_escalation.jit_unused_max_count", label: "reports.governanceEscalation.jitUnusedMaxCount", defaultValue: 5 },
];
