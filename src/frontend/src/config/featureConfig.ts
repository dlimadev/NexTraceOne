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
  { sort: 12960, key: "data_contract.stale_days", label: "reports.dataContractCompliance.staleDays", defaultValue: 90 },
  { sort: 12970, key: "data_contract.min_field_completeness_pct", label: "reports.dataContractCompliance.minFieldCompletenessPct", defaultValue: 80 },
  { sort: 12980, key: "schema_quality.min_desc_words", label: "reports.schemaQualityIndex.minDescWords", defaultValue: 5 },
  { sort: 12990, key: "schema_quality.snapshot_cron", label: "reports.schemaQualityIndex.snapshotCron", defaultValue: "0 3 1 * *" },
  { sort: 13000, key: "schema_evolution.breaking_change_low_pct", label: "reports.schemaEvolutionSafety.breakingChangeLowPct", defaultValue: 5 },
  { sort: 13010, key: "schema_evolution.breaking_change_dangerous_pct", label: "reports.schemaEvolutionSafety.breakingChangeDangerousPct", defaultValue: 25 },
  { sort: 13020, key: "schema_evolution.incident_correlation_hours", label: "reports.schemaEvolutionSafety.incidentCorrelationHours", defaultValue: 48 },
  { sort: 13030, key: "schema_evolution.lookback_days", label: "reports.schemaEvolutionSafety.lookbackDays", defaultValue: 30 },
];
