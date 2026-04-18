/**
 * Route group: Operations — Incidents, Runbooks, Reliability, Automation
 * @see docs/frontend-audit/frontend-prioritized-improvement-roadmap.md §F4-05
 */
import { lazy } from 'react';
import { Route } from 'react-router-dom';
import { ProtectedRoute } from '../components/ProtectedRoute';

const IncidentsPage = lazy(() => import('../features/operations/pages/IncidentsPage').then(m => ({ default: m.IncidentsPage })));
const IncidentDetailPage = lazy(() => import('../features/operations/pages/IncidentDetailPage').then(m => ({ default: m.IncidentDetailPage })));
const IncidentTimelinePage = lazy(() => import('../features/operations/pages/IncidentTimelinePage').then(m => ({ default: m.IncidentTimelinePage })));
const RunbooksPage = lazy(() => import('../features/operations/pages/RunbooksPage').then(m => ({ default: m.RunbooksPage })));
const RunbookBuilderPage = lazy(() => import('../features/operations/pages/RunbookBuilderPage').then(m => ({ default: m.RunbookBuilderPage })));
const TeamReliabilityPage = lazy(() => import('../features/operations/pages/TeamReliabilityPage').then(m => ({ default: m.TeamReliabilityPage })));
const ServiceReliabilityDetailPage = lazy(() => import('../features/operations/pages/ServiceReliabilityDetailPage').then(m => ({ default: m.ServiceReliabilityDetailPage })));
const ReliabilitySloManagementPage = lazy(() => import('../features/operations/pages/ReliabilitySloManagementPage').then(m => ({ default: m.ReliabilitySloManagementPage })));
const AutomationWorkflowsPage = lazy(() => import('../features/operations/pages/AutomationWorkflowsPage').then(m => ({ default: m.AutomationWorkflowsPage })));
const AutomationAdminPage = lazy(() => import('../features/operations/pages/AutomationAdminPage').then(m => ({ default: m.AutomationAdminPage })));
const AutomationWorkflowDetailPage = lazy(() => import('../features/operations/pages/AutomationWorkflowDetailPage').then(m => ({ default: m.AutomationWorkflowDetailPage })));
const EnvironmentComparisonPage = lazy(() => import('../features/operations/pages/EnvironmentComparisonPage').then(m => ({ default: m.EnvironmentComparisonPage })));
const OnCallIntelligencePage = lazy(() => import('../features/operations/pages/OnCallIntelligencePage').then(m => ({ default: m.OnCallIntelligencePage })));
const ChaosEngineeringPage = lazy(() => import('../features/operations/pages/ChaosEngineeringPage').then(m => ({ default: m.ChaosEngineeringPage })));
const PredictiveIntelligencePage = lazy(() => import('../features/operations/pages/PredictiveIntelligencePage').then(m => ({ default: m.PredictiveIntelligencePage })));
const PlatformOperationsPage = lazy(() => import('../features/operations/pages/PlatformOperationsPage').then(m => ({ default: m.PlatformOperationsPage })));
const TraceExplorerPage = lazy(() => import('../features/operations/pages/TraceExplorerPage').then(m => ({ default: m.TraceExplorerPage })));
const LogExplorerPage = lazy(() => import('../features/operations/pages/LogExplorerPage').then(m => ({ default: m.LogExplorerPage })));
const SreDashboardPage = lazy(() => import('../features/operations/pages/SreDashboardPage').then(m => ({ default: m.SreDashboardPage })));
const RequestExplorerPage = lazy(() => import('../features/operations/pages/RequestExplorerPage').then(m => ({ default: m.RequestExplorerPage })));
const ProfilingExplorerPage = lazy(() => import('../features/operations/pages/ProfilingExplorerPage').then(m => ({ default: m.ProfilingExplorerPage })));
const ErrorTrackingPage = lazy(() => import('../features/operations/pages/ErrorTrackingPage').then(m => ({ default: m.ErrorTrackingPage })));
const SyntheticMonitoringPage = lazy(() => import('../features/operations/pages/SyntheticMonitoringPage').then(m => ({ default: m.SyntheticMonitoringPage })));
const DbExplorerPage = lazy(() => import('../features/operations/pages/DbExplorerPage').then(m => ({ default: m.DbExplorerPage })));
const SloBurnRatePage = lazy(() => import('../features/operations/pages/SloBurnRatePage').then(m => ({ default: m.SloBurnRatePage })));
const PostIncidentPage = lazy(() => import('../features/operations/pages/PostIncidentPage').then(m => ({ default: m.PostIncidentPage })));
const OnCallSchedulePage = lazy(() => import('../features/operations/pages/OnCallSchedulePage').then(m => ({ default: m.OnCallSchedulePage })));
const ApiRegressionPage = lazy(() => import('../features/operations/pages/ApiRegressionPage').then(m => ({ default: m.ApiRegressionPage })));
const SloMarketplacePage = lazy(() => import('../features/operations/pages/SloMarketplacePage').then(m => ({ default: m.SloMarketplacePage })));
const DependencyRiskPage = lazy(() => import('../features/operations/pages/DependencyRiskPage').then(m => ({ default: m.DependencyRiskPage })));
const LoadTestingPage = lazy(() => import('../features/operations/pages/LoadTestingPage').then(m => ({ default: m.LoadTestingPage })));
const ServiceMaturitySrePage = lazy(() => import('../features/operations/pages/ServiceMaturitySrePage').then(m => ({ default: m.ServiceMaturitySrePage })));
const AiAnomalyPage = lazy(() => import('../features/operations/pages/AiAnomalyPage').then(m => ({ default: m.AiAnomalyPage })));
const AiIncidentSummarizerPage = lazy(() => import('../features/operations/pages/AiIncidentSummarizerPage').then(m => ({ default: m.AiIncidentSummarizerPage })));
const AiRunbookSuggesterPage = lazy(() => import('../features/operations/pages/AiRunbookSuggesterPage').then(m => ({ default: m.AiRunbookSuggesterPage })));

export function OperationsRoutes() {
  return (
    <>
      <Route
        path="/operations/incidents"
        element={
          <ProtectedRoute permission="operations:incidents:read" redirectTo="/unauthorized">
            <IncidentsPage />
          </ProtectedRoute>
        }
      />
      <Route
        path="/operations/incidents/timeline"
        element={
          <ProtectedRoute permission="operations:incidents:read" redirectTo="/unauthorized">
            <IncidentTimelinePage />
          </ProtectedRoute>
        }
      />
      <Route
        path="/operations/incidents/:incidentId"
        element={
          <ProtectedRoute permission="operations:incidents:read" redirectTo="/unauthorized">
            <IncidentDetailPage />
          </ProtectedRoute>
        }
      />
      <Route
        path="/operations/runbooks"
        element={
          <ProtectedRoute permission="operations:runbooks:read" redirectTo="/unauthorized">
            <RunbooksPage />
          </ProtectedRoute>
        }
      />
      <Route
        path="/operations/runbooks/create"
        element={
          <ProtectedRoute permission="operations:runbooks:write" redirectTo="/unauthorized">
            <RunbookBuilderPage />
          </ProtectedRoute>
        }
      />
      <Route
        path="/operations/runbooks/:runbookId/edit"
        element={
          <ProtectedRoute permission="operations:runbooks:write" redirectTo="/unauthorized">
            <RunbookBuilderPage />
          </ProtectedRoute>
        }
      />
      <Route
        path="/operations/reliability"
        element={
          <ProtectedRoute permission="operations:reliability:read" redirectTo="/unauthorized">
            <TeamReliabilityPage />
          </ProtectedRoute>
        }
      />
      <Route
        path="/operations/reliability/slos"
        element={
          <ProtectedRoute permission="operations:reliability:read" redirectTo="/unauthorized">
            <ReliabilitySloManagementPage />
          </ProtectedRoute>
        }
      />
      <Route
        path="/operations/reliability/:serviceId"
        element={
          <ProtectedRoute permission="operations:reliability:read" redirectTo="/unauthorized">
            <ServiceReliabilityDetailPage />
          </ProtectedRoute>
        }
      />
      <Route
        path="/operations/automation"
        element={
          <ProtectedRoute permission="operations:automation:read" redirectTo="/unauthorized">
            <AutomationWorkflowsPage />
          </ProtectedRoute>
        }
      />
      <Route
        path="/operations/automation/admin"
        element={
          <ProtectedRoute permission="operations:automation:read" redirectTo="/unauthorized">
            <AutomationAdminPage />
          </ProtectedRoute>
        }
      />
      <Route
        path="/operations/automation/:workflowId"
        element={
          <ProtectedRoute permission="operations:automation:read" redirectTo="/unauthorized">
            <AutomationWorkflowDetailPage />
          </ProtectedRoute>
        }
      />
      <Route
        path="/operations/runtime-comparison"
        element={
          <ProtectedRoute permission="operations:runtime:read" redirectTo="/unauthorized">
            <EnvironmentComparisonPage />
          </ProtectedRoute>
        }
      />
      <Route
        path="/operations/on-call-intelligence"
        element={
          <ProtectedRoute permission="operations:incidents:read" redirectTo="/unauthorized">
            <OnCallIntelligencePage />
          </ProtectedRoute>
        }
      />
      <Route
        path="/operations/chaos-engineering"
        element={
          <ProtectedRoute permission="operations:runtime:write" redirectTo="/unauthorized">
            <ChaosEngineeringPage />
          </ProtectedRoute>
        }
      />
      <Route
        path="/operations/predictive-intelligence"
        element={
          <ProtectedRoute permission="operations:runtime:read" redirectTo="/unauthorized">
            <PredictiveIntelligencePage />
          </ProtectedRoute>
        }
      />
      <Route
        path="/platform/operations"
        element={
          <ProtectedRoute permission="platform:admin:read" redirectTo="/unauthorized">
            <PlatformOperationsPage />
          </ProtectedRoute>
        }
      />
      <Route
        path="/operations/telemetry/traces"
        element={
          <ProtectedRoute permission="operations:telemetry:read" redirectTo="/unauthorized">
            <TraceExplorerPage />
          </ProtectedRoute>
        }
      />
      <Route
        path="/operations/telemetry/logs"
        element={
          <ProtectedRoute permission="operations:telemetry:read" redirectTo="/unauthorized">
            <LogExplorerPage />
          </ProtectedRoute>
        }
      />
      <Route
        path="/operations/sre-dashboard"
        element={
          <ProtectedRoute permission="operations:reliability:read" redirectTo="/unauthorized">
            <SreDashboardPage />
          </ProtectedRoute>
        }
      />
      <Route
        path="/operations/request-explorer"
        element={
          <ProtectedRoute permission="operations:telemetry:read" redirectTo="/unauthorized">
            <RequestExplorerPage />
          </ProtectedRoute>
        }
      />
      <Route
        path="/operations/profiling-explorer"
        element={
          <ProtectedRoute permission="operations:telemetry:read" redirectTo="/unauthorized">
            <ProfilingExplorerPage />
          </ProtectedRoute>
        }
      />
      <Route
        path="/operations/error-tracking"
        element={
          <ProtectedRoute permission="operations:incidents:read" redirectTo="/unauthorized">
            <ErrorTrackingPage />
          </ProtectedRoute>
        }
      />
      <Route
        path="/operations/synthetic-monitoring"
        element={
          <ProtectedRoute permission="operations:reliability:read" redirectTo="/unauthorized">
            <SyntheticMonitoringPage />
          </ProtectedRoute>
        }
      />
      <Route
        path="/operations/db-explorer"
        element={
          <ProtectedRoute permission="operations:telemetry:read" redirectTo="/unauthorized">
            <DbExplorerPage />
          </ProtectedRoute>
        }
      />
      <Route
        path="/operations/slo-burn-rate"
        element={
          <ProtectedRoute permission="operations:reliability:read" redirectTo="/unauthorized">
            <SloBurnRatePage />
          </ProtectedRoute>
        }
      />
      <Route
        path="/operations/post-incident"
        element={
          <ProtectedRoute permission="operations:incidents:read" redirectTo="/unauthorized">
            <PostIncidentPage />
          </ProtectedRoute>
        }
      />
      <Route
        path="/operations/on-call-schedule"
        element={
          <ProtectedRoute permission="operations:incidents:read" redirectTo="/unauthorized">
            <OnCallSchedulePage />
          </ProtectedRoute>
        }
      />
      <Route
        path="/operations/api-regression"
        element={
          <ProtectedRoute permission="operations:telemetry:read" redirectTo="/unauthorized">
            <ApiRegressionPage />
          </ProtectedRoute>
        }
      />
      <Route
        path="/operations/slo-marketplace"
        element={
          <ProtectedRoute permission="operations:reliability:read" redirectTo="/unauthorized">
            <SloMarketplacePage />
          </ProtectedRoute>
        }
      />
      <Route
        path="/operations/dependency-risk"
        element={
          <ProtectedRoute permission="operations:reliability:read" redirectTo="/unauthorized">
            <DependencyRiskPage />
          </ProtectedRoute>
        }
      />
      <Route
        path="/operations/load-testing"
        element={
          <ProtectedRoute permission="operations:telemetry:read" redirectTo="/unauthorized">
            <LoadTestingPage />
          </ProtectedRoute>
        }
      />
      <Route
        path="/operations/service-maturity-sre"
        element={
          <ProtectedRoute permission="operations:reliability:read" redirectTo="/unauthorized">
            <ServiceMaturitySrePage />
          </ProtectedRoute>
        }
      />
      <Route
        path="/operations/ai-anomaly"
        element={
          <ProtectedRoute permission="operations:runtime:read" redirectTo="/unauthorized">
            <AiAnomalyPage />
          </ProtectedRoute>
        }
      />
      <Route
        path="/operations/ai-incident-summarizer"
        element={
          <ProtectedRoute permission="operations:incidents:read" redirectTo="/unauthorized">
            <AiIncidentSummarizerPage />
          </ProtectedRoute>
        }
      />
      <Route
        path="/operations/ai-runbook-suggester"
        element={
          <ProtectedRoute permission="operations:runbooks:read" redirectTo="/unauthorized">
            <AiRunbookSuggesterPage />
          </ProtectedRoute>
        }
      />
    </>
  );
}
