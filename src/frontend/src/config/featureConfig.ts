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
  { sort: 13040, key: "topology.freshness_days", label: "reports.serviceTopologyHealth.freshnessDays", defaultValue: 30 },
  { sort: 13050, key: "topology.hub_fanin_threshold", label: "reports.serviceTopologyHealth.hubFanInThreshold", defaultValue: 5 },
  { sort: 13060, key: "topology.health.hub_penalty", label: "reports.serviceTopologyHealth.hubPenalty", defaultValue: 15 },
  { sort: 13070, key: "topology.health.circular_penalty", label: "reports.serviceTopologyHealth.circularPenalty", defaultValue: 20 },
  { sort: 13080, key: "topology.critical_path.top_n_chains", label: "reports.criticalPath.topNChains", defaultValue: 10 },
  { sort: 13090, key: "topology.critical_path.bottleneck_path_count", label: "reports.criticalPath.bottleneckPathCount", defaultValue: 3 },
  { sort: 13100, key: "topology.alignment.major_drift_threshold", label: "reports.dependencyVersionAlignment.majorDriftThreshold", defaultValue: 3 },
  { sort: 13110, key: "topology.alignment.critical_service_count", label: "reports.dependencyVersionAlignment.criticalServiceCount", defaultValue: 2 },
  { sort: 13120, key: "feature_flags.stale_flag_days", label: "featureFlagInventory.staleFlagDays", defaultValue: 60 },
  { sort: 13130, key: "feature_flags.prod_presence_days", label: "featureFlagRisk.prodPresenceDays", defaultValue: 90 },
  { sort: 13140, key: "feature_flags.incident_window_hours", label: "featureFlagRisk.incidentWindowHours", defaultValue: 24 },
  { sort: 13150, key: "feature_flags.risk.staleness_weight", label: "featureFlagRisk.stalenessWeight", defaultValue: 30 },
  { sort: 13160, key: "feature_flags.experiment.max_days", label: "experimentGovernance.experimentMaxDays", defaultValue: 30 },
  { sort: 13170, key: "feature_flags.experiment.governed_overdue_pct", label: "experimentGovernance.governedOverduePct", defaultValue: 20 },
  { sort: 13180, key: "feature_flags.experiment.governed_no_criteria_pct", label: "experimentGovernance.governedNoCriteriaPct", defaultValue: 10 },
  { sort: 13190, key: "feature_flags.inventory.ingest_endpoint_enabled", label: "featureFlagInventory.ingestEndpointEnabled", defaultValue: true },
  // ── Wave AT — AI Model Quality & Drift Governance ─────────────────────
  { sort: 13200, key: "ai.model_drift.input_drift_warning_score", label: "modelDrift.inputDriftWarningScore", defaultValue: 20 },
  { sort: 13210, key: "ai.model_drift.output_drift_warning_score", label: "modelDrift.outputDriftWarningScore", defaultValue: 15 },
  { sort: 13220, key: "ai.model_quality.min_samples_for_quality", label: "aiModelQuality.minSamplesForQuality", defaultValue: 100 },
  { sort: 13230, key: "ai.model_quality.low_confidence_threshold", label: "aiModelQuality.lowConfidenceThreshold", defaultValue: 0.6 },
  { sort: 13240, key: "ai.model_quality.latency_budget_ms", label: "aiModelQuality.latencyBudgetMs", defaultValue: 500 },
  { sort: 13250, key: "ai.governance.model_review_days", label: "aiGovernanceCompliance.modelReviewDays", defaultValue: 90 },
  { sort: 13260, key: "ai.governance.budget_overrun_threshold", label: "aiGovernanceCompliance.budgetOverrunThreshold", defaultValue: 2 },
  { sort: 13270, key: "ai.governance.audit_trail_lookback_days", label: "aiGovernanceCompliance.auditTrailLookbackDays", defaultValue: 30 },
  // ── Wave AU — Platform Self-Optimization & Adaptive Intelligence ─────────
  { sort: 13280, key: "platform.config_drift.stale_days", label: "configurationDrift.staleDays", defaultValue: 90 },
  { sort: 13290, key: "platform.config_drift.high_impact_modules", label: "configurationDrift.highImpactModules", defaultValue: "governance,sre,sbom" },
  { sort: 13300, key: "platform.health.freshness_days", label: "platformHealthIndex.freshnessDays", defaultValue: 30 },
  { sort: 13310, key: "platform.health.optimized_threshold", label: "platformHealthIndex.optimizedThreshold", defaultValue: 85 },
  { sort: 13320, key: "platform.health.operational_threshold", label: "platformHealthIndex.operationalThreshold", defaultValue: 65 },
  { sort: 13330, key: "platform.recommendations.top_n", label: "adaptiveRecommendations.topN", defaultValue: 10 },
  { sort: 13340, key: "platform.recommendations.refresh_cron", label: "adaptiveRecommendations.refreshCron", defaultValue: "0 6 * * *" },
  { sort: 13350, key: "platform.recommendations.low_effort_sprints", label: "adaptiveRecommendations.lowEffortSprints", defaultValue: 1 },
  // ── Wave AV — Contract Lifecycle Automation & Deprecation Intelligence ─
  { sort: 13360, key: "contract.deprecation.max_days", label: "contractDeprecationPipeline.maxDays", defaultValue: 180 },
  { sort: 13370, key: "contract.deprecation.sunset_warning_days", label: "contractDeprecationPipeline.sunsetWarningDays", defaultValue: 30 },
  { sort: 13380, key: "contract.deprecation.min_notification_pct", label: "contractDeprecationPipeline.minNotificationPct", defaultValue: 80 },
  { sort: 13390, key: "contract.versioning.breaking_change_warning_threshold", label: "apiVersionStrategy.breakingChangeWarningThreshold", defaultValue: 3 },
  { sort: 13400, key: "contract.versioning.proliferation_threshold", label: "apiVersionStrategy.proliferationThreshold", defaultValue: 3 },
  { sort: 13410, key: "contract.deprecation_forecast.max_age_days", label: "contractDeprecationForecast.maxAgeDays", defaultValue: 365 },
  { sort: 13420, key: "contract.deprecation_forecast.consumer_decline_pct", label: "contractDeprecationForecast.consumerDeclinePct", defaultValue: 20 },
  { sort: 13430, key: "contract.deprecation.schedule_endpoint_enabled", label: "contractDeprecationForecast.scheduleEndpointEnabled", defaultValue: true },
  // ── Wave AW — Release Intelligence & Deployment Analytics ─────────────
  { sort: 13440, key: "release.pattern.large_release_threshold", label: "releasePatternAnalysis.largeReleaseThreshold", defaultValue: 5 },
  { sort: 13450, key: "release.pattern.repeat_failure_threshold", label: "releasePatternAnalysis.repeatFailureThreshold", defaultValue: 0.3 },
  { sort: 13460, key: "release.lead_time.approval_sla_hours", label: "changeLeadTime.approvalSlaHours", defaultValue: 24 },
  { sort: 13470, key: "release.lead_time.bottleneck_approval_pct", label: "changeLeadTime.bottleneckApprovalPct", defaultValue: 50 },
  { sort: 13480, key: "release.deploy_frequency.stale_deploy_days", label: "deployFrequencyHealth.staleDeployDays", defaultValue: 60 },
  { sort: 13490, key: "release.deploy_frequency.high_variability_threshold", label: "deployFrequencyHealth.highVariabilityThreshold", defaultValue: 0.5 },
  { sort: 13500, key: "release.pattern.cluster_warning_per_week", label: "releasePatternAnalysis.clusterWarningPerWeek", defaultValue: 3 },
  { sort: 13510, key: "release.pattern.end_of_sprint_days", label: "releasePatternAnalysis.endOfSprintDays", defaultValue: 3 },
  // ── Wave AX — Security Posture & Vulnerability Intelligence ──────────
  { sort: 13520, key: "security.vulnerability.critical_cve_threshold", label: "vulnerabilityExposure.criticalCveThreshold", defaultValue: 1 },
  { sort: 13530, key: "security.patch_sla.critical_days", label: "securityPatchCompliance.patchSlaCriticalDays", defaultValue: 7 },
  { sort: 13540, key: "security.patch_sla.high_days", label: "securityPatchCompliance.patchSlaHighDays", defaultValue: 30 },
  { sort: 13550, key: "security.patch_sla.medium_days", label: "securityPatchCompliance.patchSlaMediumDays", defaultValue: 90 },
  { sort: 13560, key: "security.patch_sla.low_days", label: "securityPatchCompliance.patchSlaLowDays", defaultValue: 180 },
  { sort: 13570, key: "security.patch_compliance.compliant_critical_rate", label: "securityPatchCompliance.compliantCriticalRate", defaultValue: 95 },
  { sort: 13580, key: "security.incident.correlation_window_hours", label: "securityIncidentCorrelation.correlationWindowHours", defaultValue: 72 },
  { sort: 13590, key: "security.incident.sbom_snapshot_frequency_days", label: "securityIncidentCorrelation.sbomSnapshotFrequencyDays", defaultValue: 7 },
  // ── Wave AY — Organizational Knowledge & Documentation Intelligence ──
  { sort: 13600, key: "knowledge.doc.freshness_days", label: "documentationHealth.freshnessDays", defaultValue: 180 },
  { sort: 13610, key: "knowledge.doc.runbook_freshness_days", label: "documentationHealth.runbookFreshnessDays", defaultValue: 90 },
  { sort: 13620, key: "knowledge.doc.critical_without_runbook_tier", label: "documentationHealth.criticalWithoutRunbookTier", defaultValue: "Critical,High" },
  { sort: 13630, key: "knowledge.hub.resolution_rate_thriving", label: "knowledgeBaseUtilization.resolutionRateThriving", defaultValue: 70 },
  { sort: 13640, key: "knowledge.hub.gap_count_thriving", label: "knowledgeBaseUtilization.gapCountThriving", defaultValue: 10 },
  { sort: 13650, key: "knowledge.sharing.silo_threshold", label: "teamKnowledgeSharing.siloThreshold", defaultValue: 0.15 },
  { sort: 13660, key: "knowledge.sharing.bus_factor_max_contributors", label: "teamKnowledgeSharing.busFactorMaxContributors", defaultValue: 1 },
  { sort: 13670, key: "knowledge.hub.search_event_tracking_enabled", label: "knowledgeBaseUtilization.searchEventTrackingEnabled", defaultValue: true },
  // Wave AZ — Service Mesh & Runtime Traffic Intelligence
  { sort: 13680, key: "traffic.contract.minor_drift_threshold", label: "trafficContractDeviation.minorDriftThreshold", defaultValue: 3 },
  { sort: 13690, key: "traffic.contract.undeclared_consumer_critical", label: "trafficContractDeviation.undeclaredConsumerCritical", defaultValue: 1 },
  { sort: 13700, key: "traffic.high_risk.rps_threshold", label: "highTrafficEndpointRisk.rpsThreshold", defaultValue: 100 },
  { sort: 13710, key: "traffic.high_risk.top_n", label: "highTrafficEndpointRisk.topN", defaultValue: 20 },
  { sort: 13720, key: "traffic.high_risk.chaos_coverage_days", label: "highTrafficEndpointRisk.chaosCoverageDays", defaultValue: 90 },
  { sort: 13730, key: "traffic.anomaly.spike_sigma", label: "trafficAnomaly.spikeSigma", defaultValue: 3 },
  { sort: 13740, key: "traffic.anomaly.drop_pct", label: "trafficAnomaly.dropPct", defaultValue: 50 },
  { sort: 13750, key: "traffic.anomaly.error_rate_spike_threshold", label: "trafficAnomaly.errorRateSpikeThreshold", defaultValue: 5 },
];
