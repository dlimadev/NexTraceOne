import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { AuthProvider } from './contexts/AuthContext';
import { AppLayout } from './components/AppLayout';
import { ProtectedRoute } from './components/ProtectedRoute';
import { LoginPage, TenantSelectionPage, UsersPage, BreakGlassPage, JitAccessPage, DelegationPage, AccessReviewPage, MySessionsPage, UnauthorizedPage } from './features/identity-access';
import { LicensingPage, VendorLicensingPage } from './features/commercial-governance';
import { ContractsPage, ServiceCatalogPage, ServiceCatalogListPage, ServiceDetailPage, DeveloperPortalPage } from './features/catalog';
import { ReleasesPage, WorkflowPage, PromotionPage } from './features/change-governance';
import { AuditPage } from './features/audit-compliance';
import { DashboardPage } from './features/shared';
import { IncidentsPage, RunbooksPage } from './features/operations';
import { AiAssistantPage, ModelRegistryPage, AiPoliciesPage } from './features/ai-hub';
import { ReportsPage, RiskCenterPage, CompliancePage, FinOpsPage } from './features/governance';

const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      retry: 1,
      staleTime: 10_000,
    },
  },
});

export default function App() {
  return (
    <QueryClientProvider client={queryClient}>
      <AuthProvider>
        <BrowserRouter>
          <Routes>
            <Route path="/login" element={<LoginPage />} />
            <Route path="/select-tenant" element={<TenantSelectionPage />} />
            <Route element={<AppLayout />}>
              {/* ── Home ── */}
              <Route path="/" element={<DashboardPage />} />
              {/* ── Services ── */}
              <Route path="/services" element={<ServiceCatalogListPage />} />
              <Route path="/services/graph" element={<ServiceCatalogPage />} />
              <Route path="/services/:serviceId" element={<ServiceDetailPage />} />
              <Route path="/graph" element={<Navigate to="/services/graph" replace />} />
              <Route
                path="/portal"
                element={
                  <ProtectedRoute permission="developer-portal:read" redirectTo="/unauthorized">
                    <DeveloperPortalPage />
                  </ProtectedRoute>
                }
              />
              {/* ── Contracts ── */}
              <Route
                path="/contracts"
                element={
                  <ProtectedRoute permission="contracts:read" redirectTo="/unauthorized">
                    <ContractsPage />
                  </ProtectedRoute>
                }
              />
              {/* ── Changes ── */}
              <Route path="/releases" element={<ReleasesPage />} />
              <Route path="/workflow" element={<WorkflowPage />} />
              <Route path="/promotion" element={<PromotionPage />} />
              {/* ── Operations ── */}
              <Route path="/operations/incidents" element={<IncidentsPage />} />
              <Route path="/operations/runbooks" element={<RunbooksPage />} />
              {/* ── AI Hub ── */}
              <Route path="/ai/assistant" element={<AiAssistantPage />} />
              <Route path="/ai/models" element={<ModelRegistryPage />} />
              <Route path="/ai/policies" element={<AiPoliciesPage />} />
              {/* ── Governance ── */}
              <Route path="/governance/reports" element={<ReportsPage />} />
              <Route path="/governance/risk" element={<RiskCenterPage />} />
              <Route path="/governance/compliance" element={<CompliancePage />} />
              <Route path="/governance/finops" element={<FinOpsPage />} />
              {/* ── Admin ── */}
              <Route
                path="/licensing"
                element={
                  <ProtectedRoute permission="licensing:read" redirectTo="/unauthorized">
                    <LicensingPage />
                  </ProtectedRoute>
                }
              />
              <Route
                path="/vendor/licensing"
                element={
                  <ProtectedRoute permission="licensing:vendor:license:read" redirectTo="/unauthorized">
                    <VendorLicensingPage />
                  </ProtectedRoute>
                }
              />
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
            </Route>
            <Route path="*" element={<Navigate to="/" replace />} />
          </Routes>
        </BrowserRouter>
      </AuthProvider>
    </QueryClientProvider>
  );
}
