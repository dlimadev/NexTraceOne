/**
 * Route group: Catalog, Services & Source of Truth
 * @see docs/frontend-audit/frontend-prioritized-improvement-roadmap.md §F4-05
 */
import { lazy } from 'react';
import { Route, Navigate } from 'react-router-dom';
import { ProtectedRoute } from '../components/ProtectedRoute';

const ServiceCatalogPage = lazy(() => import('../features/catalog/pages/ServiceCatalogPage').then(m => ({ default: m.ServiceCatalogPage })));
const ServiceCatalogListPage = lazy(() => import('../features/catalog/pages/ServiceCatalogListPage').then(m => ({ default: m.ServiceCatalogListPage })));
const ServiceDetailPage = lazy(() => import('../features/catalog/pages/ServiceDetailPage').then(m => ({ default: m.ServiceDetailPage })));
const CreateServiceInterfacePage = lazy(() => import('../features/catalog/pages/CreateServiceInterfacePage').then(m => ({ default: m.CreateServiceInterfacePage })));
const SourceOfTruthExplorerPage = lazy(() => import('../features/catalog/pages/SourceOfTruthExplorerPage').then(m => ({ default: m.SourceOfTruthExplorerPage })));
const ServiceSourceOfTruthPage = lazy(() => import('../features/catalog/pages/ServiceSourceOfTruthPage').then(m => ({ default: m.ServiceSourceOfTruthPage })));
const ContractSourceOfTruthPage = lazy(() => import('../features/catalog/pages/ContractSourceOfTruthPage').then(m => ({ default: m.ContractSourceOfTruthPage })));
const GlobalSearchPage = lazy(() => import('../features/catalog/pages/GlobalSearchPage').then(m => ({ default: m.GlobalSearchPage })));
const DeveloperPortalPage = lazy(() => import('../features/catalog/pages/DeveloperPortalPage').then(m => ({ default: m.DeveloperPortalPage })));
const LegacyAssetCatalogPage = lazy(() => import('../features/legacy-assets/pages/LegacyAssetCatalogPage'));
const MainframeSystemDetailPage = lazy(() => import('../features/legacy-assets/pages/MainframeSystemDetailPage'));
const ServiceDiscoveryPage = lazy(() => import('../features/catalog/pages/ServiceDiscoveryPage'));
const ServiceMaturityPage = lazy(() => import('../features/catalog/pages/ServiceMaturityPage'));
const ServiceScorecardPage = lazy(() => import('../features/catalog/pages/ServiceScorecardPage').then(m => ({ default: m.ServiceScorecardPage })));
const TemplateLibraryPage = lazy(() => import('../features/catalog/pages/TemplateLibraryPage').then(m => ({ default: m.TemplateLibraryPage })));
const TemplateDetailPage = lazy(() => import('../features/catalog/pages/TemplateDetailPage').then(m => ({ default: m.TemplateDetailPage })));
const TemplateEditorPage = lazy(() => import('../features/catalog/pages/TemplateEditorPage').then(m => ({ default: m.TemplateEditorPage })));
const AiScaffoldWizardPage = lazy(() => import('../features/catalog/pages/AiScaffoldWizardPage').then(m => ({ default: m.AiScaffoldWizardPage })));
const ContractPipelinePage = lazy(() => import('../features/catalog/pages/ContractPipelinePage').then(m => ({ default: m.ContractPipelinePage })));
const SecurityGateDashboardPage = lazy(() => import('../features/catalog/pages/SecurityGateDashboardPage').then(m => ({ default: m.SecurityGateDashboardPage })));
const SelfServicePortalPage = lazy(() => import('../features/catalog/pages/SelfServicePortalPage').then(m => ({ default: m.SelfServicePortalPage })));
const DeveloperExperienceScorePage = lazy(() => import('../features/catalog/pages/DeveloperExperienceScorePage').then(m => ({ default: m.DeveloperExperienceScorePage })));
const DependencyDashboardPage = lazy(() => import('../features/catalog/pages/DependencyDashboardPage').then(m => ({ default: m.DependencyDashboardPage })));
const LicenseCompliancePage = lazy(() => import('../features/catalog/pages/LicenseCompliancePage').then(m => ({ default: m.LicenseCompliancePage })));

export function CatalogRoutes() {
  return (
    <>
      <Route
        path="/search"
        element={
          <ProtectedRoute permission="catalog:assets:read" redirectTo="/unauthorized">
            <GlobalSearchPage />
          </ProtectedRoute>
        }
      />
      <Route
        path="/source-of-truth"
        element={
          <ProtectedRoute permission="catalog:assets:read" redirectTo="/unauthorized">
            <SourceOfTruthExplorerPage />
          </ProtectedRoute>
        }
      />
      <Route
        path="/source-of-truth/services/:serviceId"
        element={
          <ProtectedRoute permission="catalog:assets:read" redirectTo="/unauthorized">
            <ServiceSourceOfTruthPage />
          </ProtectedRoute>
        }
      />
      <Route
        path="/source-of-truth/contracts/:contractVersionId"
        element={
          <ProtectedRoute permission="catalog:assets:read" redirectTo="/unauthorized">
            <ContractSourceOfTruthPage />
          </ProtectedRoute>
        }
      />
      <Route
        path="/services"
        element={
          <ProtectedRoute permission="catalog:assets:read" redirectTo="/unauthorized">
            <ServiceCatalogListPage />
          </ProtectedRoute>
        }
      />
      <Route
        path="/services/graph"
        element={
          <ProtectedRoute permission="catalog:assets:read" redirectTo="/unauthorized">
            <ServiceCatalogPage />
          </ProtectedRoute>
        }
      />
      <Route
        path="/services/:serviceId"
        element={
          <ProtectedRoute permission="catalog:assets:read" redirectTo="/unauthorized">
            <ServiceDetailPage />
          </ProtectedRoute>
        }
      />
      <Route
        path="/services/:serviceId/interfaces/new"
        element={
          <ProtectedRoute permission="catalog:assets:write" redirectTo="/unauthorized">
            <CreateServiceInterfacePage />
          </ProtectedRoute>
        }
      />
      <Route path="/graph" element={<Navigate to="/services/graph" replace />} />
      <Route
        path="/services/legacy"
        element={
          <ProtectedRoute permission="catalog:assets:read" redirectTo="/unauthorized">
            <LegacyAssetCatalogPage />
          </ProtectedRoute>
        }
      />
      <Route
        path="/services/legacy/:assetType/:assetId"
        element={
          <ProtectedRoute permission="catalog:assets:read" redirectTo="/unauthorized">
            <MainframeSystemDetailPage />
          </ProtectedRoute>
        }
      />
      <Route
        path="/services/discovery"
        element={
          <ProtectedRoute permission="catalog:assets:read" redirectTo="/unauthorized">
            <ServiceDiscoveryPage />
          </ProtectedRoute>
        }
      />
      <Route
        path="/services/maturity"
        element={
          <ProtectedRoute permission="catalog:assets:read" redirectTo="/unauthorized">
            <ServiceMaturityPage />
          </ProtectedRoute>
        }
      />
      <Route
        path="/services/scorecards"
        element={
          <ProtectedRoute permission="catalog:assets:read" redirectTo="/unauthorized">
            <ServiceScorecardPage />
          </ProtectedRoute>
        }
      />
      <Route
        path="/portal/*"
        element={
          <ProtectedRoute permission="developer-portal:read" redirectTo="/unauthorized">
            <DeveloperPortalPage />
          </ProtectedRoute>
        }
      />
      {/* ── Service Templates & AI Scaffold ── */}
      <Route
        path="/catalog/templates"
        element={
          <ProtectedRoute permission="catalog:templates:read" redirectTo="/unauthorized">
            <TemplateLibraryPage />
          </ProtectedRoute>
        }
      />
      <Route
        path="/catalog/templates/new"
        element={
          <ProtectedRoute permission="catalog:templates:write" redirectTo="/unauthorized">
            <TemplateEditorPage />
          </ProtectedRoute>
        }
      />
      <Route
        path="/catalog/templates/:id"
        element={
          <ProtectedRoute permission="catalog:templates:read" redirectTo="/unauthorized">
            <TemplateDetailPage />
          </ProtectedRoute>
        }
      />
      <Route
        path="/catalog/templates/:id/edit"
        element={
          <ProtectedRoute permission="catalog:templates:write" redirectTo="/unauthorized">
            <TemplateEditorPage />
          </ProtectedRoute>
        }
      />
      <Route
        path="/catalog/templates/:id/scaffold"
        element={
          <ProtectedRoute permission="catalog:templates:scaffold" redirectTo="/unauthorized">
            <AiScaffoldWizardPage />
          </ProtectedRoute>
        }
      />
      {/* ── Phase 5: Contract Pipeline & Security Gate ── */}
      <Route
        path="/catalog/contracts/pipeline"
        element={
          <ProtectedRoute permission="catalog:contracts:pipeline:read" redirectTo="/unauthorized">
            <ContractPipelinePage />
          </ProtectedRoute>
        }
      />
      <Route
        path="/catalog/security-gate"
        element={
          <ProtectedRoute permission="governance:security:scan" redirectTo="/unauthorized">
            <SecurityGateDashboardPage />
          </ProtectedRoute>
        }
      />
      <Route
        path="/catalog/self-service"
        element={
          <ProtectedRoute permission="catalog:assets:read" redirectTo="/unauthorized">
            <SelfServicePortalPage />
          </ProtectedRoute>
        }
      />
      <Route
        path="/catalog/developer-experience-score"
        element={
          <ProtectedRoute permission="catalog:assets:read" redirectTo="/unauthorized">
            <DeveloperExperienceScorePage />
          </ProtectedRoute>
        }
      />
      <Route
        path="/catalog/dependency-dashboard"
        element={
          <ProtectedRoute permission="catalog:assets:read" redirectTo="/unauthorized">
            <DependencyDashboardPage />
          </ProtectedRoute>
        }
      />
      <Route
        path="/catalog/license-compliance"
        element={
          <ProtectedRoute permission="catalog:assets:read" redirectTo="/unauthorized">
            <LicenseCompliancePage />
          </ProtectedRoute>
        }
      />
    </>
  );
}
