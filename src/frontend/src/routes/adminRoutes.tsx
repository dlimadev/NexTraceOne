/**
 * Route group: Admin — Identity, Audit, Notifications, Platform Configuration, Analytics, Integrations
 * @see docs/frontend-audit/frontend-prioritized-improvement-roadmap.md §F4-05
 */
import { lazy } from 'react';
import { Route } from 'react-router-dom';
import { ProtectedRoute } from '../components/ProtectedRoute';

// Identity-access
const UsersPage = lazy(() => import('../features/identity-access/pages/UsersPage').then(m => ({ default: m.UsersPage })));
const EnvironmentsPage = lazy(() => import('../features/identity-access/pages/EnvironmentsPage').then(m => ({ default: m.EnvironmentsPage })));
const BreakGlassPage = lazy(() => import('../features/identity-access/pages/BreakGlassPage').then(m => ({ default: m.BreakGlassPage })));
const JitAccessPage = lazy(() => import('../features/identity-access/pages/JitAccessPage').then(m => ({ default: m.JitAccessPage })));
const DelegationPage = lazy(() => import('../features/identity-access/pages/DelegationPage').then(m => ({ default: m.DelegationPage })));
const AccessReviewPage = lazy(() => import('../features/identity-access/pages/AccessReviewPage').then(m => ({ default: m.AccessReviewPage })));
const MySessionsPage = lazy(() => import('../features/identity-access/pages/MySessionsPage').then(m => ({ default: m.MySessionsPage })));
const UnauthorizedPage = lazy(() => import('../features/identity-access/pages/UnauthorizedPage').then(m => ({ default: m.UnauthorizedPage })));

// Audit-compliance
const AuditPage = lazy(() => import('../features/audit-compliance/pages/AuditPage').then(m => ({ default: m.AuditPage })));

// Notifications
const NotificationCenterPage = lazy(() => import('../features/notifications/pages/NotificationCenterPage').then(m => ({ default: m.NotificationCenterPage })));
const NotificationAnalyticsPage = lazy(() => import('../features/notifications/pages/NotificationAnalyticsPage').then(m => ({ default: m.NotificationAnalyticsPage })));
const NotificationPreferencesPage = lazy(() => import('../features/notifications/pages/NotificationPreferencesPage').then(m => ({ default: m.NotificationPreferencesPage })));
const NotificationConfigurationPage = lazy(() => import('../features/notifications/pages/NotificationConfigurationPage').then(m => ({ default: m.NotificationConfigurationPage })));

// Configuration Admin
const ConfigurationAdminPage = lazy(() => import('../features/configuration/pages/ConfigurationAdminPage').then(m => ({ default: m.ConfigurationAdminPage })));
const AdvancedConfigurationConsolePage = lazy(() => import('../features/configuration/pages/AdvancedConfigurationConsolePage').then(m => ({ default: m.AdvancedConfigurationConsolePage })));
const UserPreferencesPage = lazy(() => import('../features/configuration/pages/UserPreferencesPage').then(m => ({ default: m.UserPreferencesPage })));
const ParameterUsageReportPage = lazy(() => import('../features/configuration/pages/ParameterUsageReportPage').then(m => ({ default: m.ParameterUsageReportPage })));
const ParameterComplianceDashboardPage = lazy(() => import('../features/configuration/pages/ParameterComplianceDashboardPage').then(m => ({ default: m.ParameterComplianceDashboardPage })));
const BrandingAdminPage = lazy(() => import('../features/configuration/pages/BrandingAdminPage').then(m => ({ default: m.BrandingAdminPage })));
const WebhookTemplatesPage = lazy(() => import('../features/configuration/pages/WebhookTemplatesPage').then(m => ({ default: m.WebhookTemplatesPage })));
const APIKeysPage = lazy(() => import('../features/configuration/pages/APIKeysPage').then(m => ({ default: m.APIKeysPage })));
const IntegrationMappingsPage = lazy(() => import('../features/configuration/pages/IntegrationMappingsPage').then(m => ({ default: m.IntegrationMappingsPage })));
const WorkflowConfigurationPage = lazy(() => import('../features/change-governance/pages/WorkflowConfigurationPage').then(m => ({ default: m.WorkflowConfigurationPage })));
const CatalogContractsConfigurationPage = lazy(() => import('../features/catalog/pages/CatalogContractsConfigurationPage').then(m => ({ default: m.CatalogContractsConfigurationPage })));
const OperationsFinOpsConfigurationPage = lazy(() => import('../features/operational-intelligence/pages/OperationsFinOpsConfigurationPage').then(m => ({ default: m.OperationsFinOpsConfigurationPage })));

// Integrations
const IntegrationHubPage = lazy(() => import('../features/integrations/pages/IntegrationHubPage').then(m => ({ default: m.IntegrationHubPage })));
const ConnectorDetailPage = lazy(() => import('../features/integrations/pages/ConnectorDetailPage').then(m => ({ default: m.ConnectorDetailPage })));
const IngestionExecutionsPage = lazy(() => import('../features/integrations/pages/IngestionExecutionsPage').then(m => ({ default: m.IngestionExecutionsPage })));
const IngestionFreshnessPage = lazy(() => import('../features/integrations/pages/IngestionFreshnessPage').then(m => ({ default: m.IngestionFreshnessPage })));
const WebhookSubscriptionsPage = lazy(() => import('../features/integrations/pages/WebhookSubscriptionsPage').then(m => ({ default: m.WebhookSubscriptionsPage })));

// Product Analytics
const ProductAnalyticsOverviewPage = lazy(() => import('../features/product-analytics/pages/ProductAnalyticsOverviewPage').then(m => ({ default: m.ProductAnalyticsOverviewPage })));
const ModuleAdoptionPage = lazy(() => import('../features/product-analytics/pages/ModuleAdoptionPage').then(m => ({ default: m.ModuleAdoptionPage })));
const PersonaUsagePage = lazy(() => import('../features/product-analytics/pages/PersonaUsagePage').then(m => ({ default: m.PersonaUsagePage })));
const JourneyFunnelPage = lazy(() => import('../features/product-analytics/pages/JourneyFunnelPage').then(m => ({ default: m.JourneyFunnelPage })));
const ValueTrackingPage = lazy(() => import('../features/product-analytics/pages/ValueTrackingPage').then(m => ({ default: m.ValueTrackingPage })));
const AdoptionFunnelPage = lazy(() => import('../features/product-analytics/pages/AdoptionFunnelPage').then(m => ({ default: m.AdoptionFunnelPage })));
const FeatureHeatmapPage = lazy(() => import('../features/product-analytics/pages/FeatureHeatmapPage').then(m => ({ default: m.FeatureHeatmapPage })));
const TimeToValuePage = lazy(() => import('../features/product-analytics/pages/TimeToValuePage').then(m => ({ default: m.TimeToValuePage })));

// Platform Admin (on-prem)
const PreflightPage = lazy(() => import('../features/platform-admin/pages/PreflightPage').then(m => ({ default: m.PreflightPage })));
const SetupWizardPage = lazy(() => import('../features/platform-admin/pages/SetupWizardPage').then(m => ({ default: m.SetupWizardPage })));
const PlatformHealthDashboardPage = lazy(() => import('../features/platform-admin/pages/PlatformHealthDashboardPage').then(m => ({ default: m.PlatformHealthDashboardPage })));
const AiModelManagerPage = lazy(() => import('../features/platform-admin/pages/AiModelManagerPage').then(m => ({ default: m.AiModelManagerPage })));
const NetworkPolicyPage = lazy(() => import('../features/platform-admin/pages/NetworkPolicyPage').then(m => ({ default: m.NetworkPolicyPage })));
const DatabaseHealthPage = lazy(() => import('../features/platform-admin/pages/DatabaseHealthPage').then(m => ({ default: m.DatabaseHealthPage })));
const SupportBundlePage = lazy(() => import('../features/platform-admin/pages/SupportBundlePage').then(m => ({ default: m.SupportBundlePage })));
const BackupCoordinatorPage = lazy(() => import('../features/platform-admin/pages/BackupCoordinatorPage').then(m => ({ default: m.BackupCoordinatorPage })));
const StartupReportPage = lazy(() => import('../features/platform-admin/pages/StartupReportPage').then(m => ({ default: m.StartupReportPage })));
const ResourceBudgetPage = lazy(() => import('../features/platform-admin/pages/ResourceBudgetPage').then(m => ({ default: m.ResourceBudgetPage })));
const ElasticsearchManagerPage = lazy(() => import('../features/platform-admin/pages/ElasticsearchManagerPage').then(m => ({ default: m.ElasticsearchManagerPage })));
const PlatformAlertRulesPage = lazy(() => import('../features/platform-admin/pages/PlatformAlertRulesPage').then(m => ({ default: m.PlatformAlertRulesPage })));
const RecoveryWizardPage = lazy(() => import('../features/platform-admin/pages/RecoveryWizardPage').then(m => ({ default: m.RecoveryWizardPage })));
const GreenOpsPage = lazy(() => import('../features/platform-admin/pages/GreenOpsPage').then(m => ({ default: m.GreenOpsPage })));
const AiResourceGovernorPage = lazy(() => import('../features/platform-admin/pages/AiResourceGovernorPage').then(m => ({ default: m.AiResourceGovernorPage })));
const AiGovernancePage = lazy(() => import('../features/platform-admin/pages/AiGovernancePage').then(m => ({ default: m.AiGovernancePage })));
const ProxyConfigPage = lazy(() => import('../features/platform-admin/pages/ProxyConfigPage').then(m => ({ default: m.ProxyConfigPage })));
const ExternalHttpAuditPage = lazy(() => import('../features/platform-admin/pages/ExternalHttpAuditPage').then(m => ({ default: m.ExternalHttpAuditPage })));
const EnvironmentPoliciesPage = lazy(() => import('../features/platform-admin/pages/EnvironmentPoliciesPage').then(m => ({ default: m.EnvironmentPoliciesPage })));
const NonProdSchedulerPage = lazy(() => import('../features/platform-admin/pages/NonProdSchedulerPage').then(m => ({ default: m.NonProdSchedulerPage })));
const CapacityForecastPage = lazy(() => import('../features/platform-admin/pages/CapacityForecastPage').then(m => ({ default: m.CapacityForecastPage })));

export function AdminRoutes() {
  return (
    <>
      {/* ── Identity & Access ── */}
      <Route
        path="/users"
        element={
          <ProtectedRoute permission="identity:users:read" redirectTo="/unauthorized">
            <UsersPage />
          </ProtectedRoute>
        }
      />
      <Route
        path="/environments"
        element={
          <ProtectedRoute permission="identity:users:read" redirectTo="/unauthorized">
            <EnvironmentsPage />
          </ProtectedRoute>
        }
      />
      <Route
        path="/break-glass"
        element={
          <ProtectedRoute permission="identity:sessions:read" redirectTo="/unauthorized">
            <BreakGlassPage />
          </ProtectedRoute>
        }
      />
      <Route
        path="/jit-access"
        element={
          <ProtectedRoute permission="identity:users:read" redirectTo="/unauthorized">
            <JitAccessPage />
          </ProtectedRoute>
        }
      />
      <Route
        path="/delegations"
        element={
          <ProtectedRoute permission="identity:users:read" redirectTo="/unauthorized">
            <DelegationPage />
          </ProtectedRoute>
        }
      />
      <Route
        path="/access-reviews"
        element={
          <ProtectedRoute permission="identity:users:read" redirectTo="/unauthorized">
            <AccessReviewPage />
          </ProtectedRoute>
        }
      />
      <Route
        path="/my-sessions"
        element={
          <ProtectedRoute permission="identity:sessions:read" redirectTo="/unauthorized">
            <MySessionsPage />
          </ProtectedRoute>
        }
      />
      <Route path="/unauthorized" element={<UnauthorizedPage />} />

      {/* ── Audit ── */}
      <Route
        path="/audit"
        element={
          <ProtectedRoute permission="audit:trail:read" redirectTo="/unauthorized">
            <AuditPage />
          </ProtectedRoute>
        }
      />

      {/* ── Notifications ── */}
      <Route
        path="/notifications"
        element={
          <ProtectedRoute permission="notifications:inbox:read" redirectTo="/unauthorized">
            <NotificationCenterPage />
          </ProtectedRoute>
        }
      />
      <Route
        path="/notifications/preferences"
        element={
          <ProtectedRoute permission="notifications:inbox:read" redirectTo="/unauthorized">
            <NotificationPreferencesPage />
          </ProtectedRoute>
        }
      />
      <Route
        path="/notifications/analytics"
        element={
          <ProtectedRoute permission="notifications:configuration:read" redirectTo="/unauthorized">
            <NotificationAnalyticsPage />
          </ProtectedRoute>
        }
      />

      {/* ── Platform Configuration ── */}
      <Route
        path="/platform/configuration"
        element={
          <ProtectedRoute permission="platform:admin:read" redirectTo="/unauthorized">
            <ConfigurationAdminPage />
          </ProtectedRoute>
        }
      />
      <Route
        path="/platform/configuration/notifications"
        element={
          <ProtectedRoute permission="platform:admin:read" redirectTo="/unauthorized">
            <NotificationConfigurationPage />
          </ProtectedRoute>
        }
      />
      <Route
        path="/platform/configuration/workflows"
        element={
          <ProtectedRoute permission="platform:admin:read" redirectTo="/unauthorized">
            <WorkflowConfigurationPage />
          </ProtectedRoute>
        }
      />
      <Route
        path="/platform/configuration/catalog-contracts"
        element={
          <ProtectedRoute permission="platform:admin:read" redirectTo="/unauthorized">
            <CatalogContractsConfigurationPage />
          </ProtectedRoute>
        }
      />
      <Route
        path="/platform/configuration/operations-finops"
        element={
          <ProtectedRoute permission="platform:admin:read" redirectTo="/unauthorized">
            <OperationsFinOpsConfigurationPage />
          </ProtectedRoute>
        }
      />
      <Route
        path="/platform/configuration/advanced"
        element={
          <ProtectedRoute permission="platform:admin:read" redirectTo="/unauthorized">
            <AdvancedConfigurationConsolePage />
          </ProtectedRoute>
        }
      />
      <Route
        path="/platform/configuration/analytics/usage"
        element={
          <ProtectedRoute permission="configuration:analytics:read" redirectTo="/unauthorized">
            <ParameterUsageReportPage />
          </ProtectedRoute>
        }
      />
      <Route
        path="/platform/configuration/analytics/compliance"
        element={
          <ProtectedRoute permission="configuration:analytics:read" redirectTo="/unauthorized">
            <ParameterComplianceDashboardPage />
          </ProtectedRoute>
        }
      />
      <Route
        path="/platform/branding"
        element={
          <ProtectedRoute permission="configuration:admin" redirectTo="/unauthorized">
            <BrandingAdminPage />
          </ProtectedRoute>
        }
      />

      {/* ── Integrations ── */}
      <Route
        path="/integrations"
        element={
          <ProtectedRoute permission="integrations:read" redirectTo="/unauthorized">
            <IntegrationHubPage />
          </ProtectedRoute>
        }
      />
      <Route
        path="/integrations/:connectorId"
        element={
          <ProtectedRoute permission="integrations:read" redirectTo="/unauthorized">
            <ConnectorDetailPage />
          </ProtectedRoute>
        }
      />
      <Route
        path="/integrations/executions"
        element={
          <ProtectedRoute permission="integrations:read" redirectTo="/unauthorized">
            <IngestionExecutionsPage />
          </ProtectedRoute>
        }
      />
      <Route
        path="/integrations/freshness"
        element={
          <ProtectedRoute permission="integrations:read" redirectTo="/unauthorized">
            <IngestionFreshnessPage />
          </ProtectedRoute>
        }
      />
      <Route
        path="/integrations/webhooks"
        element={
          <ProtectedRoute permission="integrations:connectors:read" redirectTo="/unauthorized">
            <WebhookSubscriptionsPage />
          </ProtectedRoute>
        }
      />

      {/* ── Product Analytics ── */}
      <Route
        path="/analytics"
        element={
          <ProtectedRoute permission="analytics:read" redirectTo="/unauthorized">
            <ProductAnalyticsOverviewPage />
          </ProtectedRoute>
        }
      />
      <Route
        path="/analytics/adoption"
        element={
          <ProtectedRoute permission="analytics:read" redirectTo="/unauthorized">
            <ModuleAdoptionPage />
          </ProtectedRoute>
        }
      />
      <Route
        path="/analytics/personas"
        element={
          <ProtectedRoute permission="analytics:read" redirectTo="/unauthorized">
            <PersonaUsagePage />
          </ProtectedRoute>
        }
      />
      <Route
        path="/analytics/journeys"
        element={
          <ProtectedRoute permission="analytics:read" redirectTo="/unauthorized">
            <JourneyFunnelPage />
          </ProtectedRoute>
        }
      />
      <Route
        path="/analytics/value"
        element={
          <ProtectedRoute permission="analytics:read" redirectTo="/unauthorized">
            <ValueTrackingPage />
          </ProtectedRoute>
        }
      />
      <Route
        path="/analytics/funnel"
        element={
          <ProtectedRoute permission="analytics:read" redirectTo="/unauthorized">
            <AdoptionFunnelPage />
          </ProtectedRoute>
        }
      />
      <Route
        path="/analytics/heatmap"
        element={
          <ProtectedRoute permission="analytics:read" redirectTo="/unauthorized">
            <FeatureHeatmapPage />
          </ProtectedRoute>
        }
      />
      <Route
        path="/analytics/time-to-value"
        element={
          <ProtectedRoute permission="analytics:read" redirectTo="/unauthorized">
            <TimeToValuePage />
          </ProtectedRoute>
        }
      />
      <Route
        path="/user-preferences"
        element={
          <UserPreferencesPage />
        }
      />
      <Route
        path="/platform/configuration/webhook-templates"
        element={
          <ProtectedRoute permission="configuration:write" redirectTo="/unauthorized">
            <WebhookTemplatesPage />
          </ProtectedRoute>
        }
      />
      <Route
        path="/platform/configuration/api-keys"
        element={
          <APIKeysPage />
        }
      />
      <Route
        path="/platform/configuration/integration-mappings"
        element={
          <IntegrationMappingsPage />
        }
      />

      {/* ── Platform Health Dashboard (on-prem admin) ── */}
      <Route
        path="/platform/health"
        element={
          <ProtectedRoute permission="platform:admin:read" redirectTo="/unauthorized">
            <PlatformHealthDashboardPage />
          </ProtectedRoute>
        }
      />

      {/* ── AI Model Manager — hardware assessment + model compatibility (on-prem admin) ── */}
      <Route
        path="/admin/ai/models"
        element={
          <ProtectedRoute permission="platform:admin:read" redirectTo="/unauthorized">
            <AiModelManagerPage />
          </ProtectedRoute>
        }
      />

      {/* ── Network Policy — Air-Gap mode + external calls audit (on-prem admin) ── */}
      <Route
        path="/admin/network-policy"
        element={
          <ProtectedRoute permission="platform:admin:read" redirectTo="/unauthorized">
            <NetworkPolicyPage />
          </ProtectedRoute>
        }
      />

      {/* ── Database Health — PostgreSQL diagnostics (on-prem admin) ── */}
      <Route
        path="/admin/database-health"
        element={
          <ProtectedRoute permission="platform:admin:read" redirectTo="/unauthorized">
            <DatabaseHealthPage />
          </ProtectedRoute>
        }
      />

      {/* ── Setup Wizard — first-run on-prem setup (no auth required) ── */}
      <Route path="/setup" element={<SetupWizardPage />} />

      {/* ── Preflight Check UI — pre-login system check (no auth required) ── */}
      <Route path="/preflight" element={<PreflightPage />} />

      {/* ── Support Bundle Generator — W2-04 (on-prem admin) ── */}
      <Route
        path="/admin/support-bundle"
        element={
          <ProtectedRoute permission="platform:admin:read" redirectTo="/unauthorized">
            <SupportBundlePage />
          </ProtectedRoute>
        }
      />

      {/* ── Backup Coordinator — W3-03 (on-prem admin) ── */}
      <Route
        path="/admin/backup"
        element={
          <ProtectedRoute permission="platform:admin:read" redirectTo="/unauthorized">
            <BackupCoordinatorPage />
          </ProtectedRoute>
        }
      />

      {/* ── Startup Report — W2-02 (on-prem admin) ── */}
      <Route
        path="/admin/startup-report"
        element={
          <ProtectedRoute permission="platform:admin:read" redirectTo="/unauthorized">
            <StartupReportPage />
          </ProtectedRoute>
        }
      />

      {/* ── Resource Budget — W6-03 (on-prem admin) ── */}
      <Route
        path="/admin/resource-budget"
        element={
          <ProtectedRoute permission="platform:admin:read" redirectTo="/unauthorized">
            <ResourceBudgetPage />
          </ProtectedRoute>
        }
      />

      {/* ── Elasticsearch Manager — W7-01/02 (on-prem admin) ── */}
      <Route
        path="/admin/elasticsearch"
        element={
          <ProtectedRoute permission="platform:admin:read" redirectTo="/unauthorized">
            <ElasticsearchManagerPage />
          </ProtectedRoute>
        }
      />
      {/* ── Platform Alert Rules — W2-03 (on-prem admin) ── */}
      <Route
        path="/admin/platform-alerts"
        element={
          <ProtectedRoute permission="platform:admin:read" redirectTo="/unauthorized">
            <PlatformAlertRulesPage />
          </ProtectedRoute>
        }
      />
      {/* ── Recovery Wizard — W3-04 (on-prem admin) ── */}
      <Route
        path="/admin/recovery"
        element={
          <ProtectedRoute permission="platform:admin:read" redirectTo="/unauthorized">
            <RecoveryWizardPage />
          </ProtectedRoute>
        }
      />
      {/* ── GreenOps — W6-04 (on-prem admin) ── */}
      <Route
        path="/admin/greenops"
        element={
          <ProtectedRoute permission="platform:admin:read" redirectTo="/unauthorized">
            <GreenOpsPage />
          </ProtectedRoute>
        }
      />
      {/* ── AI Resource Governor — W4-03 (on-prem admin) ── */}
      <Route
        path="/admin/ai-governor"
        element={
          <ProtectedRoute permission="platform:admin:read" redirectTo="/unauthorized">
            <AiResourceGovernorPage />
          </ProtectedRoute>
        }
      />
      {/* ── AI Governance — W4-04 (on-prem admin) ── */}
      <Route
        path="/admin/ai-governance"
        element={
          <ProtectedRoute permission="platform:admin:read" redirectTo="/unauthorized">
            <AiGovernancePage />
          </ProtectedRoute>
        }
      />
      {/* ── Proxy Config — W5-02 (on-prem admin) ── */}
      <Route
        path="/admin/proxy-config"
        element={
          <ProtectedRoute permission="platform:admin:read" redirectTo="/unauthorized">
            <ProxyConfigPage />
          </ProtectedRoute>
        }
      />
      {/* ── External HTTP Audit — W5-03 (on-prem admin) ── */}
      <Route
        path="/admin/external-http-audit"
        element={
          <ProtectedRoute permission="platform:admin:read" redirectTo="/unauthorized">
            <ExternalHttpAuditPage />
          </ProtectedRoute>
        }
      />
      {/* ── Environment Policies — W5-05 (on-prem admin) ── */}
      <Route
        path="/admin/environment-policies"
        element={
          <ProtectedRoute permission="platform:admin:read" redirectTo="/unauthorized">
            <EnvironmentPoliciesPage />
          </ProtectedRoute>
        }
      />
      {/* ── Non-Prod Scheduler — W6-02 (on-prem admin) ── */}
      <Route
        path="/admin/nonprod-scheduler"
        element={
          <ProtectedRoute permission="platform:admin:read" redirectTo="/unauthorized">
            <NonProdSchedulerPage />
          </ProtectedRoute>
        }
      />
      {/* ── Capacity Forecast — W8-01 (on-prem admin) ── */}
      <Route
        path="/admin/capacity-forecast"
        element={
          <ProtectedRoute permission="platform:admin:read" redirectTo="/unauthorized">
            <CapacityForecastPage />
          </ProtectedRoute>
        }
      />
    </>
  );
}
