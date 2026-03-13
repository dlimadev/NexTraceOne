import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { AuthProvider } from './contexts/AuthContext';
import { AppLayout } from './components/AppLayout';
import { ProtectedRoute } from './components/ProtectedRoute';
import { LoginPage } from './pages/LoginPage';
import { TenantSelectionPage } from './pages/TenantSelectionPage';
import { DashboardPage } from './pages/DashboardPage';
import { ReleasesPage } from './pages/ReleasesPage';
import { EngineeringGraphPage } from './pages/EngineeringGraphPage';
import { ContractsPage } from './pages/ContractsPage';
import { UsersPage } from './pages/UsersPage';
import { WorkflowPage } from './pages/WorkflowPage';
import { PromotionPage } from './pages/PromotionPage';
import { LicensingPage } from './pages/LicensingPage';
import { DeveloperPortalPage } from './pages/DeveloperPortalPage';
import { AuditPage } from './pages/AuditPage';
import { BreakGlassPage } from './pages/BreakGlassPage';
import { JitAccessPage } from './pages/JitAccessPage';
import { DelegationPage } from './pages/DelegationPage';
import { AccessReviewPage } from './pages/AccessReviewPage';
import { UnauthorizedPage } from './pages/UnauthorizedPage';

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
              <Route path="/contracts" element={<ContractsPage />} />
              <Route path="/workflow" element={<WorkflowPage />} />
              <Route path="/promotion" element={<PromotionPage />} />
              <Route path="/licensing" element={<LicensingPage />} />
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
              <Route path="/unauthorized" element={<UnauthorizedPage />} />
            </Route>
            <Route path="*" element={<Navigate to="/" replace />} />
          </Routes>
        </BrowserRouter>
      </AuthProvider>
    </QueryClientProvider>
  );
}
