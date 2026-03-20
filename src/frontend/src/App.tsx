import { lazy, Suspense } from 'react';
import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { ReactQueryDevtools } from '@tanstack/react-query-devtools';
import { AuthProvider } from './contexts/AuthContext';
import { PersonaProvider } from './contexts/PersonaContext';
import { AppShell } from './components/shell/AppShell';
import { ProtectedRoute } from './components/ProtectedRoute';
import { ReleaseScopeGate } from './components/ReleaseScopeGate';

// Eager imports — critical for fast first paint
import { LoginPage, TenantSelectionPage, ForgotPasswordPage, ResetPasswordPage, ActivationPage, MfaPage, InvitationPage } from './features/identity-access';

// ── Identity-access (lazy) ──
const UsersPage = lazy(() => import('./features/identity-access/pages/UsersPage').then(m => ({ default: m.UsersPage })));
const BreakGlassPage = lazy(() => import('./features/identity-access/pages/BreakGlassPage').then(m => ({ default: m.BreakGlassPage })));
const JitAccessPage = lazy(() => import('./features/identity-access/pages/JitAccessPage').then(m => ({ default: m.JitAccessPage })));
const DelegationPage = lazy(() => import('./features/identity-access/pages/DelegationPage').then(m => ({ default: m.DelegationPage })));
const AccessReviewPage = lazy(() => import('./features/identity-access/pages/AccessReviewPage').then(m => ({ default: m.AccessReviewPage })));
const MySessionsPage = lazy(() => import('./features/identity-access/pages/MySessionsPage').then(m => ({ default: m.MySessionsPage })));
const UnauthorizedPage = lazy(() => import('./features/identity-access/pages/UnauthorizedPage').then(m => ({ default: m.UnauthorizedPage })));

// ── Catalog (lazy) ──
const ServiceCatalogPage = lazy(() => import('./features/catalog/pages/ServiceCatalogPage').then(m => ({ default: m.ServiceCatalogPage })));
const ServiceCatalogListPage = lazy(() => import('./features/catalog/pages/ServiceCatalogListPage').then(m => ({ default: m.ServiceCatalogListPage })));
const ServiceDetailPage = lazy(() => import('./features/catalog/pages/ServiceDetailPage').then(m => ({ default: m.ServiceDetailPage })));
const SourceOfTruthExplorerPage = lazy(() => import('./features/catalog/pages/SourceOfTruthExplorerPage').then(m => ({ default: m.SourceOfTruthExplorerPage })));
const ServiceSourceOfTruthPage = lazy(() => import('./features/catalog/pages/ServiceSourceOfTruthPage').then(m => ({ default: m.ServiceSourceOfTruthPage })));
const ContractSourceOfTruthPage = lazy(() => import('./features/catalog/pages/ContractSourceOfTruthPage').then(m => ({ default: m.ContractSourceOfTruthPage })));
const GlobalSearchPage = lazy(() => import('./features/catalog/pages/GlobalSearchPage').then(m => ({ default: m.GlobalSearchPage })));

// ── Change-governance (lazy) ──
const ReleasesPage = lazy(() => import('./features/change-governance/pages/ReleasesPage').then(m => ({ default: m.ReleasesPage })));
const WorkflowPage = lazy(() => import('./features/change-governance/pages/WorkflowPage').then(m => ({ default: m.WorkflowPage })));
const PromotionPage = lazy(() => import('./features/change-governance/pages/PromotionPage').then(m => ({ default: m.PromotionPage })));
const ChangeCatalogPage = lazy(() => import('./features/change-governance/pages/ChangeCatalogPage').then(m => ({ default: m.ChangeCatalogPage })));
const ChangeDetailPage = lazy(() => import('./features/change-governance/pages/ChangeDetailPage').then(m => ({ default: m.ChangeDetailPage })));

// ── Audit-compliance (lazy) ──
const AuditPage = lazy(() => import('./features/audit-compliance/pages/AuditPage').then(m => ({ default: m.AuditPage })));

// ── Shared (lazy) ──
const DashboardPage = lazy(() => import('./features/shared/pages/DashboardPage').then(m => ({ default: m.DashboardPage })));


function PageLoader() {
  return (
    <div className="flex items-center justify-center h-full min-h-[50vh]">
      <div className="h-8 w-8 animate-spin rounded-full border-4 border-accent border-t-transparent" />
    </div>
  );
}

const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      retry: 1,
      staleTime: 30_000,
      gcTime: 300_000,
      refetchOnWindowFocus: false,
    },
  },
});

export default function App() {
  return (
    <QueryClientProvider client={queryClient}>
      <AuthProvider>
        <PersonaProvider>
          <BrowserRouter>
          <Suspense fallback={<PageLoader />}>
          <Routes>
            <Route path="/login" element={<LoginPage />} />
            <Route path="/forgot-password" element={<ForgotPasswordPage />} />
            <Route path="/reset-password" element={<ResetPasswordPage />} />
            <Route path="/activate" element={<ActivationPage />} />
            <Route path="/mfa" element={<MfaPage />} />
            <Route path="/invitation" element={<InvitationPage />} />
            <Route path="/select-tenant" element={<TenantSelectionPage />} />
            <Route element={<AppShell />}>
              {/* ── Home ── */}
              <Route path="/" element={<DashboardPage />} />
              {/* ── Global Search ── */}
              <Route
                path="/search"
                element={
                  <ProtectedRoute permission="catalog:assets:read" redirectTo="/unauthorized">
                    <GlobalSearchPage />
                  </ProtectedRoute>
                }
              />
              {/* ── Source of Truth ── */}
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
              {/* ── Services ── */}
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
              <Route path="/graph" element={<Navigate to="/services/graph" replace />} />
              <Route
                path="/portal"
                element={
                  <ProtectedRoute permission="developer-portal:read" redirectTo="/unauthorized">
                    <ReleaseScopeGate moduleKey="developerPortal" />
                  </ProtectedRoute>
                }
              />
              <Route
                path="/contracts"
                element={
                  <ProtectedRoute permission="contracts:read" redirectTo="/unauthorized">
                    <ReleaseScopeGate moduleKey="contracts" />
                  </ProtectedRoute>
                }
              />
              <Route
                path="/contracts/new"
                element={
                  <ProtectedRoute permission="contracts:write" redirectTo="/unauthorized">
                    <ReleaseScopeGate moduleKey="contracts" />
                  </ProtectedRoute>
                }
              />
              <Route
                path="/contracts/studio/:draftId"
                element={
                  <ProtectedRoute permission="contracts:write" redirectTo="/unauthorized">
                    <ReleaseScopeGate moduleKey="contracts" />
                  </ProtectedRoute>
                }
              />
              <Route path="/contracts/studio" element={<Navigate to="/contracts" replace />} />
              <Route path="/contracts/legacy" element={<Navigate to="/contracts" replace />} />
              <Route
                path="/contracts/governance"
                element={
                  <ProtectedRoute permission="contracts:read" redirectTo="/unauthorized">
                    <ReleaseScopeGate moduleKey="contracts" />
                  </ProtectedRoute>
                }
              />
              <Route
                path="/contracts/spectral"
                element={
                  <ProtectedRoute permission="contracts:write" redirectTo="/unauthorized">
                    <ReleaseScopeGate moduleKey="contracts" />
                  </ProtectedRoute>
                }
              />
              <Route
                path="/contracts/canonical"
                element={
                  <ProtectedRoute permission="contracts:read" redirectTo="/unauthorized">
                    <ReleaseScopeGate moduleKey="contracts" />
                  </ProtectedRoute>
                }
              />
              <Route
                path="/contracts/:contractVersionId/portal"
                element={
                  <ProtectedRoute permission="contracts:read" redirectTo="/unauthorized">
                    <ReleaseScopeGate moduleKey="contracts" />
                  </ProtectedRoute>
                }
              />
              <Route
                path="/contracts/:contractVersionId"
                element={
                  <ProtectedRoute permission="contracts:read" redirectTo="/unauthorized">
                    <ReleaseScopeGate moduleKey="contracts" />
                  </ProtectedRoute>
                }
              />
              {/* ── Changes ── */}
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
                  <ProtectedRoute permission="change-intelligence:releases:read" redirectTo="/unauthorized">
                    <ReleasesPage />
                  </ProtectedRoute>
                }
              />
              <Route
                path="/workflow"
                element={
                  <ProtectedRoute permission="workflow:read" redirectTo="/unauthorized">
                    <WorkflowPage />
                  </ProtectedRoute>
                }
              />
              <Route
                path="/promotion"
                element={
                  <ProtectedRoute permission="promotion:read" redirectTo="/unauthorized">
                    <PromotionPage />
                  </ProtectedRoute>
                }
              />
              {/* ── Operations ── */}
              <Route
                path="/operations/incidents"
                element={
                  <ProtectedRoute permission="operations:incidents:read" redirectTo="/unauthorized">
                    <ReleaseScopeGate moduleKey="operations" />
                  </ProtectedRoute>
                }
              />
              <Route
                path="/operations/incidents/:incidentId"
                element={
                  <ProtectedRoute permission="operations:incidents:read" redirectTo="/unauthorized">
                    <ReleaseScopeGate moduleKey="operations" />
                  </ProtectedRoute>
                }
              />
              <Route
                path="/operations/runbooks"
                element={
                  <ProtectedRoute permission="operations:incidents:read" redirectTo="/unauthorized">
                    <ReleaseScopeGate moduleKey="operations" />
                  </ProtectedRoute>
                }
              />
              <Route
                path="/operations/reliability"
                element={
                  <ProtectedRoute permission="operations:reliability:read" redirectTo="/unauthorized">
                    <ReleaseScopeGate moduleKey="operations" />
                  </ProtectedRoute>
                }
              />
              <Route
                path="/operations/reliability/:serviceId"
                element={
                  <ProtectedRoute permission="operations:reliability:read" redirectTo="/unauthorized">
                    <ReleaseScopeGate moduleKey="operations" />
                  </ProtectedRoute>
                }
              />
              <Route
                path="/operations/automation"
                element={
                  <ProtectedRoute permission="operations:automation:read" redirectTo="/unauthorized">
                    <ReleaseScopeGate moduleKey="operations" />
                  </ProtectedRoute>
                }
              />
              <Route
                path="/operations/automation/admin"
                element={
                  <ProtectedRoute permission="operations:automation:read" redirectTo="/unauthorized">
                    <ReleaseScopeGate moduleKey="operations" />
                  </ProtectedRoute>
                }
              />
              <Route
                path="/operations/automation/:workflowId"
                element={
                  <ProtectedRoute permission="operations:automation:read" redirectTo="/unauthorized">
                    <ReleaseScopeGate moduleKey="operations" />
                  </ProtectedRoute>
                }
              />
              <Route path="/ai/assistant" element={<ReleaseScopeGate moduleKey="aiHub" />} />
              <Route path="/ai/models" element={<ReleaseScopeGate moduleKey="aiHub" />} />
              <Route path="/ai/policies" element={<ReleaseScopeGate moduleKey="aiHub" />} />
              <Route path="/ai/ide" element={<ReleaseScopeGate moduleKey="aiHub" />} />
              <Route path="/ai/routing" element={<ReleaseScopeGate moduleKey="aiHub" />} />
              <Route path="/ai/budgets" element={<ReleaseScopeGate moduleKey="aiHub" />} />
              <Route path="/ai/audit" element={<ReleaseScopeGate moduleKey="aiHub" />} />
              <Route path="/governance/reports" element={<ReleaseScopeGate moduleKey="governance" />} />
              <Route path="/governance/risk" element={<ReleaseScopeGate moduleKey="governance" />} />
              <Route path="/governance/compliance" element={<ReleaseScopeGate moduleKey="governance" />} />
              <Route path="/governance/finops" element={<ReleaseScopeGate moduleKey="governance" />} />
              <Route path="/governance/finops/services/:serviceId" element={<ReleaseScopeGate moduleKey="governance" />} />
              <Route path="/governance/finops/teams/:teamId" element={<ReleaseScopeGate moduleKey="governance" />} />
              <Route path="/governance/finops/domains/:domainId" element={<ReleaseScopeGate moduleKey="governance" />} />
              <Route path="/governance/finops/executive" element={<ReleaseScopeGate moduleKey="governance" />} />
              <Route path="/governance/executive" element={<ReleaseScopeGate moduleKey="governance" />} />
              <Route path="/governance/executive/heatmap" element={<ReleaseScopeGate moduleKey="governance" />} />
              <Route path="/governance/executive/maturity" element={<ReleaseScopeGate moduleKey="governance" />} />
              <Route path="/governance/executive/benchmarking" element={<ReleaseScopeGate moduleKey="governance" />} />
              <Route path="/governance/executive/drilldown/:entityType/:entityId" element={<ReleaseScopeGate moduleKey="governance" />} />
              <Route
                path="/governance/policies"
                element={
                  <ProtectedRoute permission="governance:policies:read" redirectTo="/unauthorized">
                    <ReleaseScopeGate moduleKey="governance" />
                  </ProtectedRoute>
                }
              />
              <Route
                path="/governance/evidence"
                element={
                  <ProtectedRoute permission="governance:evidence:read" redirectTo="/unauthorized">
                    <ReleaseScopeGate moduleKey="governance" />
                  </ProtectedRoute>
                }
              />
              <Route
                path="/governance/controls"
                element={
                  <ProtectedRoute permission="governance:controls:read" redirectTo="/unauthorized">
                    <ReleaseScopeGate moduleKey="governance" />
                  </ProtectedRoute>
                }
              />
              <Route
                path="/governance/packs"
                element={
                  <ProtectedRoute permission="governance:packs:read" redirectTo="/unauthorized">
                    <ReleaseScopeGate moduleKey="governance" />
                  </ProtectedRoute>
                }
              />
              <Route
                path="/governance/packs/:packId"
                element={
                  <ProtectedRoute permission="governance:packs:read" redirectTo="/unauthorized">
                    <ReleaseScopeGate moduleKey="governance" />
                  </ProtectedRoute>
                }
              />
              <Route
                path="/governance/packs/:packId/simulate"
                element={
                  <ProtectedRoute permission="governance:packs:read" redirectTo="/unauthorized">
                    <ReleaseScopeGate moduleKey="governance" />
                  </ProtectedRoute>
                }
              />
              <Route
                path="/governance/waivers"
                element={
                  <ProtectedRoute permission="governance:waivers:read" redirectTo="/unauthorized">
                    <ReleaseScopeGate moduleKey="governance" />
                  </ProtectedRoute>
                }
              />
              <Route
                path="/governance/teams"
                element={
                  <ProtectedRoute permission="governance:teams:read" redirectTo="/unauthorized">
                    <ReleaseScopeGate moduleKey="governance" />
                  </ProtectedRoute>
                }
              />
              <Route
                path="/governance/teams/:teamId"
                element={
                  <ProtectedRoute permission="governance:teams:read" redirectTo="/unauthorized">
                    <ReleaseScopeGate moduleKey="governance" />
                  </ProtectedRoute>
                }
              />
              <Route
                path="/governance/domains"
                element={
                  <ProtectedRoute permission="governance:domains:read" redirectTo="/unauthorized">
                    <ReleaseScopeGate moduleKey="governance" />
                  </ProtectedRoute>
                }
              />
              <Route
                path="/governance/domains/:domainId"
                element={
                  <ProtectedRoute permission="governance:domains:read" redirectTo="/unauthorized">
                    <ReleaseScopeGate moduleKey="governance" />
                  </ProtectedRoute>
                }
              />
              <Route
                path="/governance/delegated-admin"
                element={
                  <ProtectedRoute permission="governance:teams:read" redirectTo="/unauthorized">
                    <ReleaseScopeGate moduleKey="governance" />
                  </ProtectedRoute>
                }
              />
              <Route
                path="/integrations"
                element={
                  <ProtectedRoute permission="integrations:read" redirectTo="/unauthorized">
                    <ReleaseScopeGate moduleKey="integrations" />
                  </ProtectedRoute>
                }
              />
              <Route
                path="/integrations/connectors/:connectorId"
                element={
                  <ProtectedRoute permission="integrations:read" redirectTo="/unauthorized">
                    <ReleaseScopeGate moduleKey="integrations" />
                  </ProtectedRoute>
                }
              />
              <Route
                path="/integrations/executions"
                element={
                  <ProtectedRoute permission="integrations:read" redirectTo="/unauthorized">
                    <ReleaseScopeGate moduleKey="integrations" />
                  </ProtectedRoute>
                }
              />
              <Route
                path="/integrations/freshness"
                element={
                  <ProtectedRoute permission="integrations:read" redirectTo="/unauthorized">
                    <ReleaseScopeGate moduleKey="integrations" />
                  </ProtectedRoute>
                }
              />
              {/* ── Admin ── */}
              <Route
                path="/users"
                element={
                  <ProtectedRoute permission="identity:users:read" redirectTo="/unauthorized">
                    <UsersPage />
                  </ProtectedRoute>
                }
              />
              <Route
                path="/audit"
                element={
                  <ProtectedRoute permission="audit:read" redirectTo="/unauthorized">
                    <AuditPage />
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

              {/* ── Product Analytics ── */}
              <Route
                path="/analytics"
                element={
                  <ProtectedRoute permission="governance:analytics:read" redirectTo="/unauthorized">
                    <ReleaseScopeGate moduleKey="analytics" />
                  </ProtectedRoute>
                }
              />
              <Route
                path="/analytics/adoption"
                element={
                  <ProtectedRoute permission="governance:analytics:read" redirectTo="/unauthorized">
                    <ReleaseScopeGate moduleKey="analytics" />
                  </ProtectedRoute>
                }
              />
              <Route
                path="/analytics/personas"
                element={
                  <ProtectedRoute permission="governance:analytics:read" redirectTo="/unauthorized">
                    <ReleaseScopeGate moduleKey="analytics" />
                  </ProtectedRoute>
                }
              />
              <Route
                path="/analytics/journeys"
                element={
                  <ProtectedRoute permission="governance:analytics:read" redirectTo="/unauthorized">
                    <ReleaseScopeGate moduleKey="analytics" />
                  </ProtectedRoute>
                }
              />
              <Route
                path="/analytics/value"
                element={
                  <ProtectedRoute permission="governance:analytics:read" redirectTo="/unauthorized">
                    <ReleaseScopeGate moduleKey="analytics" />
                  </ProtectedRoute>
                }
              />
              <Route
                path="/platform/operations"
                element={
                  <ProtectedRoute permission="platform:admin:read" redirectTo="/unauthorized">
                    <ReleaseScopeGate moduleKey="platformOperations" />
                  </ProtectedRoute>
                }
              />
            </Route>
            <Route path="*" element={<Navigate to="/" replace />} />
          </Routes>
          </Suspense>
          </BrowserRouter>
        </PersonaProvider>
      </AuthProvider>
      <ReactQueryDevtools initialIsOpen={false} buttonPosition="bottom-left" />
    </QueryClientProvider>
  );
}
