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
const PlatformOperationsPage = lazy(() => import('../features/operations/pages/PlatformOperationsPage').then(m => ({ default: m.PlatformOperationsPage })));

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
          <ProtectedRoute permission="operations:incidents:read" redirectTo="/unauthorized">
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
        path="/platform/operations"
        element={
          <ProtectedRoute permission="platform:admin:read" redirectTo="/unauthorized">
            <PlatformOperationsPage />
          </ProtectedRoute>
        }
      />
    </>
  );
}
