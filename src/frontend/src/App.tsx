import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { AuthProvider } from './contexts/AuthContext';
import { AppLayout } from './components/AppLayout';
import { ProtectedRoute } from './components/ProtectedRoute';
import { LoginPage, TenantSelectionPage, UsersPage, BreakGlassPage, JitAccessPage, DelegationPage, AccessReviewPage, MySessionsPage, UnauthorizedPage } from './features/identity-access';
import { LicensingPage, VendorLicensingPage } from './features/commercial-governance';
import { ContractsPage, EngineeringGraphPage, DeveloperPortalPage } from './features/catalog';
import { ReleasesPage, WorkflowPage, PromotionPage } from './features/change-governance';
import { AuditPage } from './features/audit-compliance';
import { DashboardPage } from './features/shared';

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
              <Route path="/" element={<DashboardPage />} />
              <Route path="/releases" element={<ReleasesPage />} />
              <Route path="/graph" element={<EngineeringGraphPage />} />
              <Route
                path="/contracts"
                element={
                  <ProtectedRoute permission="contracts:read" redirectTo="/unauthorized">
                    <ContractsPage />
                  </ProtectedRoute>
                }
              />
              <Route path="/workflow" element={<WorkflowPage />} />
              <Route path="/promotion" element={<PromotionPage />} />
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
              <Route path="/portal" element={<DeveloperPortalPage />} />
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
