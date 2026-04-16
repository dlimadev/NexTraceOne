/**
 * Route group: Change Intelligence, Releases & Workflow
 * @see docs/frontend-audit/frontend-prioritized-improvement-roadmap.md §F4-05
 */
import { lazy } from 'react';
import { Route } from 'react-router-dom';
import { ProtectedRoute } from '../components/ProtectedRoute';

const ReleasesPage = lazy(() => import('../features/change-governance/pages/ReleasesPage').then(m => ({ default: m.ReleasesPage })));
const WorkflowPage = lazy(() => import('../features/change-governance/pages/WorkflowPage').then(m => ({ default: m.WorkflowPage })));
const PromotionPage = lazy(() => import('../features/change-governance/pages/PromotionPage').then(m => ({ default: m.PromotionPage })));
const ChangeCatalogPage = lazy(() => import('../features/change-governance/pages/ChangeCatalogPage').then(m => ({ default: m.ChangeCatalogPage })));
const ChangeDetailPage = lazy(() => import('../features/change-governance/pages/ChangeDetailPage').then(m => ({ default: m.ChangeDetailPage })));
const ReleaseCalendarPage = lazy(() => import('../features/change-governance/pages/ReleaseCalendarPage').then(m => ({ default: m.ReleaseCalendarPage })));
const DoraMetricsPage = lazy(() => import('../features/change-governance/pages/DoraMetricsPage').then(m => ({ default: m.DoraMetricsPage })));
const ChangeAdvisoryPage = lazy(() => import('../features/change-governance/pages/ChangeAdvisoryPage').then(m => ({ default: m.ChangeAdvisoryPage })));
const ReleaseTrainPage = lazy(() => import('../features/change-governance/pages/ReleaseTrainPage').then(m => ({ default: m.ReleaseTrainPage })));
const ReleaseChecklistExecutionPage = lazy(() => import('../features/change-governance/pages/ReleaseChecklistExecutionPage').then(m => ({ default: m.ReleaseChecklistExecutionPage })));

export function ChangesRoutes() {
  return (
    <>
      <Route
        path="/changes"
        element={
          <ProtectedRoute permission="change-intelligence:read" redirectTo="/unauthorized">
            <ChangeCatalogPage />
          </ProtectedRoute>
        }
      />
      <Route
        path="/changes/:changeId"
        element={
          <ProtectedRoute permission="change-intelligence:read" redirectTo="/unauthorized">
            <ChangeDetailPage />
          </ProtectedRoute>
        }
      />
      <Route
        path="/releases"
        element={
          <ProtectedRoute permission="change-intelligence:read" redirectTo="/unauthorized">
            <ReleasesPage />
          </ProtectedRoute>
        }
      />
      <Route
        path="/workflow"
        element={
          <ProtectedRoute permission="workflow:instances:read" redirectTo="/unauthorized">
            <WorkflowPage />
          </ProtectedRoute>
        }
      />
      <Route
        path="/promotion"
        element={
          <ProtectedRoute permission="promotion:requests:read" redirectTo="/unauthorized">
            <PromotionPage />
          </ProtectedRoute>
        }
      />
      <Route
        path="/release-calendar"
        element={
          <ProtectedRoute permission="change-intelligence:read" redirectTo="/unauthorized">
            <ReleaseCalendarPage />
          </ProtectedRoute>
        }
      />
      <Route
        path="/dora-metrics"
        element={
          <ProtectedRoute permission="change-intelligence:read" redirectTo="/unauthorized">
            <DoraMetricsPage />
          </ProtectedRoute>
        }
      />
      <Route
        path="/change-advisory"
        element={
          <ProtectedRoute permission="change-intelligence:read" redirectTo="/unauthorized">
            <ChangeAdvisoryPage />
          </ProtectedRoute>
        }
      />
      <Route
        path="/release-train"
        element={
          <ProtectedRoute permission="change-intelligence:read" redirectTo="/unauthorized">
            <ReleaseTrainPage />
          </ProtectedRoute>
        }
      />
      <Route
        path="/workflow/checklist"
        element={
          <ProtectedRoute permission="workflow:instances:write" redirectTo="/unauthorized">
            <ReleaseChecklistExecutionPage />
          </ProtectedRoute>
        }
      />
    </>
  );
}
