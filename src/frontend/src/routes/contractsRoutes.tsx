/**
 * Route group: Contracts & Contract Governance
 * @see docs/frontend-audit/frontend-prioritized-improvement-roadmap.md §F4-05
 */
import { lazy } from 'react';
import { Route, Navigate } from 'react-router-dom';
import { ProtectedRoute } from '../components/ProtectedRoute';

const ContractCatalogPage = lazy(() => import('../features/contracts/catalog/ContractCatalogPage').then(m => ({ default: m.ContractCatalogPage })));
const CreateContractPage = lazy(() => import('../features/contracts/create/CreateContractPage').then(m => ({ default: m.CreateContractPage })));
const DraftStudioPage = lazy(() => import('../features/contracts/studio/DraftStudioPage').then(m => ({ default: m.DraftStudioPage })));
const ContractWorkspacePage = lazy(() => import('../features/contracts/workspace/ContractWorkspacePage').then(m => ({ default: m.ContractWorkspacePage })));
const SpectralRulesetManagerPage = lazy(() => import('../features/contracts/spectral/SpectralRulesetManagerPage').then(m => ({ default: m.SpectralRulesetManagerPage })));
const CanonicalEntityCatalogPage = lazy(() => import('../features/contracts/canonical/CanonicalEntityCatalogPage').then(m => ({ default: m.CanonicalEntityCatalogPage })));
const ContractGovernancePage = lazy(() => import('../features/contracts/governance/ContractGovernancePage').then(m => ({ default: m.ContractGovernancePage })));
const ContractHealthDashboardPage = lazy(() => import('../features/contracts/governance/ContractHealthDashboardPage').then(m => ({ default: m.ContractHealthDashboardPage })));
const ContractPortalPage = lazy(() => import('../features/contracts/portal/ContractPortalPage').then(m => ({ default: m.ContractPortalPage })));
const PublicationCenterPage = lazy(() => import('../features/contracts/publication/PublicationCenterPage').then(m => ({ default: m.PublicationCenterPage })));
const CanonicalEntityImpactCascadePage = lazy(() => import('../features/contracts/canonical/CanonicalEntityImpactCascadePage').then(m => ({ default: m.CanonicalEntityImpactCascadePage })));
const ContractHealthTimelinePage = lazy(() => import('../features/contracts/governance/ContractHealthTimelinePage').then(m => ({ default: m.ContractHealthTimelinePage })));
const ContractPlaygroundPage = lazy(() => import('../features/contracts/playground/ContractPlaygroundPage').then(m => ({ default: m.ContractPlaygroundPage })));
const ConsumerDrivenContractPage = lazy(() => import('../features/contracts/cdct/ConsumerDrivenContractPage').then(m => ({ default: m.ConsumerDrivenContractPage })));

export function ContractsRoutes() {
  return (
    <>
      <Route
        path="/contracts"
        element={
          <ProtectedRoute permission="contracts:read" redirectTo="/unauthorized">
            <ContractCatalogPage />
          </ProtectedRoute>
        }
      />
      <Route
        path="/contracts/new"
        element={
          <ProtectedRoute permission="contracts:write" redirectTo="/unauthorized">
            <CreateContractPage />
          </ProtectedRoute>
        }
      />
      <Route
        path="/contracts/studio/:draftId"
        element={
          <ProtectedRoute permission="contracts:write" redirectTo="/unauthorized">
            <DraftStudioPage />
          </ProtectedRoute>
        }
      />
      <Route path="/contracts/studio" element={<Navigate to="/contracts" replace />} />
      <Route path="/contracts/legacy" element={<Navigate to="/contracts" replace />} />
      <Route
        path="/contracts/governance"
        element={
          <ProtectedRoute permission="contracts:read" redirectTo="/unauthorized">
            <ContractGovernancePage />
          </ProtectedRoute>
        }
      />
      <Route
        path="/contracts/spectral"
        element={
          <ProtectedRoute permission="contracts:spectral:read" redirectTo="/unauthorized">
            <SpectralRulesetManagerPage />
          </ProtectedRoute>
        }
      />
      <Route
        path="/contracts/canonical"
        element={
          <ProtectedRoute permission="contracts:read" redirectTo="/unauthorized">
            <CanonicalEntityCatalogPage />
          </ProtectedRoute>
        }
      />
      <Route
        path="/contracts/health"
        element={
          <ProtectedRoute permission="contracts:read" redirectTo="/unauthorized">
            <ContractHealthDashboardPage />
          </ProtectedRoute>
        }
      />
      <Route
        path="/contracts/health/timeline"
        element={
          <ProtectedRoute permission="contracts:read" redirectTo="/unauthorized">
            <ContractHealthTimelinePage />
          </ProtectedRoute>
        }
      />
      <Route
        path="/contracts/canonical/impact-cascade"
        element={
          <ProtectedRoute permission="contracts:read" redirectTo="/unauthorized">
            <CanonicalEntityImpactCascadePage />
          </ProtectedRoute>
        }
      />
      <Route
        path="/contracts/publication"
        element={
          <ProtectedRoute permission="contracts:write" redirectTo="/unauthorized">
            <PublicationCenterPage />
          </ProtectedRoute>
        }
      />
      <Route
        path="/contracts/playground"
        element={
          <ProtectedRoute permission="contracts:read" redirectTo="/unauthorized">
            <ContractPlaygroundPage />
          </ProtectedRoute>
        }
      />
      <Route
        path="/contracts/cdct"
        element={
          <ProtectedRoute permission="contracts:read" redirectTo="/unauthorized">
            <ConsumerDrivenContractPage />
          </ProtectedRoute>
        }
      />
      <Route
        path="/contracts/portal/:contractVersionId"
        element={
          <ProtectedRoute permission="developer-portal:read" redirectTo="/unauthorized">
            <ContractPortalPage />
          </ProtectedRoute>
        }
      />
      <Route
        path="/contracts/:contractVersionId"
        element={
          <ProtectedRoute permission="contracts:read" redirectTo="/unauthorized">
            <ContractWorkspacePage />
          </ProtectedRoute>
        }
      />
    </>
  );
}
