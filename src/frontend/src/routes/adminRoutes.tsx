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
    </>
  );
}
