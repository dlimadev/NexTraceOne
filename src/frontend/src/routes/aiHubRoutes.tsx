/**
 * Route group: AI Hub
 * @see docs/frontend-audit/frontend-prioritized-improvement-roadmap.md §F4-05
 */
import { lazy } from 'react';
import { Route } from 'react-router-dom';
import { ProtectedRoute } from '../components/ProtectedRoute';

const AiAssistantPage = lazy(() => import('../features/ai-hub/pages/AiAssistantPage').then(m => ({ default: m.AiAssistantPage })));
const ModelRegistryPage = lazy(() => import('../features/ai-hub/pages/ModelRegistryPage').then(m => ({ default: m.ModelRegistryPage })));
const AiPoliciesPage = lazy(() => import('../features/ai-hub/pages/AiPoliciesPage').then(m => ({ default: m.AiPoliciesPage })));
const AiRoutingPage = lazy(() => import('../features/ai-hub/pages/AiRoutingPage').then(m => ({ default: m.AiRoutingPage })));
const IdeIntegrationsPage = lazy(() => import('../features/ai-hub/pages/IdeIntegrationsPage').then(m => ({ default: m.IdeIntegrationsPage })));
const TokenBudgetPage = lazy(() => import('../features/ai-hub/pages/TokenBudgetPage').then(m => ({ default: m.TokenBudgetPage })));
const AiAuditPage = lazy(() => import('../features/ai-hub/pages/AiAuditPage').then(m => ({ default: m.AiAuditPage })));
const AiAnalysisPage = lazy(() => import('../features/ai-hub/pages/AiAnalysisPage').then(m => ({ default: m.AiAnalysisPage })));
const AiAgentsPage = lazy(() => import('../features/ai-hub/pages/AiAgentsPage').then(m => ({ default: m.AiAgentsPage })));
const AgentDetailPage = lazy(() => import('../features/ai-hub/pages/AgentDetailPage').then(m => ({ default: m.AgentDetailPage })));
const AiIntegrationsConfigurationPage = lazy(() => import('../features/ai-hub/pages/AiIntegrationsConfigurationPage').then(m => ({ default: m.AiIntegrationsConfigurationPage })));

export function AiHubRoutes() {
  return (
    <>
      <Route
        path="/ai/assistant"
        element={
          <ProtectedRoute permission="ai:assistant:read" redirectTo="/unauthorized">
            <AiAssistantPage />
          </ProtectedRoute>
        }
      />
      <Route
        path="/ai/models"
        element={
          <ProtectedRoute permission="ai:governance:read" redirectTo="/unauthorized">
            <ModelRegistryPage />
          </ProtectedRoute>
        }
      />
      <Route
        path="/ai/policies"
        element={
          <ProtectedRoute permission="ai:governance:read" redirectTo="/unauthorized">
            <AiPoliciesPage />
          </ProtectedRoute>
        }
      />
      <Route
        path="/ai/routing"
        element={
          <ProtectedRoute permission="ai:governance:read" redirectTo="/unauthorized">
            <AiRoutingPage />
          </ProtectedRoute>
        }
      />
      <Route
        path="/ai/ide"
        element={
          <ProtectedRoute permission="ai:governance:read" redirectTo="/unauthorized">
            <IdeIntegrationsPage />
          </ProtectedRoute>
        }
      />
      <Route
        path="/ai/budgets"
        element={
          <ProtectedRoute permission="ai:governance:read" redirectTo="/unauthorized">
            <TokenBudgetPage />
          </ProtectedRoute>
        }
      />
      <Route
        path="/ai/audit"
        element={
          <ProtectedRoute permission="ai:governance:read" redirectTo="/unauthorized">
            <AiAuditPage />
          </ProtectedRoute>
        }
      />
      <Route
        path="/ai/agents"
        element={
          <ProtectedRoute permission="ai:assistant:read" redirectTo="/unauthorized">
            <AiAgentsPage />
          </ProtectedRoute>
        }
      />
      <Route
        path="/ai/agents/:agentId"
        element={
          <ProtectedRoute permission="ai:assistant:read" redirectTo="/unauthorized">
            <AgentDetailPage />
          </ProtectedRoute>
        }
      />
      <Route
        path="/ai/analysis"
        element={
          <ProtectedRoute permission="ai:runtime:write" redirectTo="/unauthorized">
            <AiAnalysisPage />
          </ProtectedRoute>
        }
      />
      <Route
        path="/platform/configuration/ai-integrations"
        element={
          <ProtectedRoute permission="platform:admin:read" redirectTo="/unauthorized">
            <AiIntegrationsConfigurationPage />
          </ProtectedRoute>
        }
      />
    </>
  );
}
