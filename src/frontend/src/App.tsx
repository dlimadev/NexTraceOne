import { lazy, Suspense } from 'react';
import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { AuthProvider } from './contexts/AuthContext';
import { EnvironmentProvider } from './contexts/EnvironmentContext';
import { PersonaProvider } from './contexts/PersonaContext';
import { ThemeProvider } from './contexts/ThemeContext';
import { BrandingProvider } from './contexts/BrandingContext';
import { ToastProvider } from './components/Toast';
import { AppShell } from './components/shell/AppShell';

// Eager imports — critical for fast first paint
import { LoginPage, TenantSelectionPage, ForgotPasswordPage, ResetPasswordPage, ActivationPage, MfaPage, InvitationPage } from './features/identity-access';

// ── Route groups (lazy-loaded by module) ──────────────────────────────────────
import { CatalogRoutes } from './routes/catalogRoutes';
import { ContractsRoutes } from './routes/contractsRoutes';
import { KnowledgeRoutes } from './routes/knowledgeRoutes';
import { ChangesRoutes } from './routes/changesRoutes';
import { OperationsRoutes } from './routes/operationsRoutes';
import { AiHubRoutes } from './routes/aiHubRoutes';
import { GovernanceRoutes } from './routes/governanceRoutes';
import { AdminRoutes } from './routes/adminRoutes';

// ── Shared (lazy) ─────────────────────────────────────────────────────────────
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
      <ThemeProvider>
      <BrandingProvider>
      <AuthProvider>
        <EnvironmentProvider>
          <PersonaProvider>
            <ToastProvider>
            <BrowserRouter>
              <Suspense fallback={<PageLoader />}>
                <Routes>
                  {/* ── Public auth routes ── */}
                  <Route path="/login" element={<LoginPage />} />
                  <Route path="/forgot-password" element={<ForgotPasswordPage />} />
                  <Route path="/reset-password" element={<ResetPasswordPage />} />
                  <Route path="/activate" element={<ActivationPage />} />
                  <Route path="/mfa" element={<MfaPage />} />
                  <Route path="/invitation" element={<InvitationPage />} />
                  <Route path="/select-tenant" element={<TenantSelectionPage />} />

                  {/* ── Authenticated shell ── */}
                  <Route element={<AppShell />}>
                    {/* Home */}
                    <Route path="/" element={<DashboardPage />} />

                    {/* Module route groups — called as functions so React Router
                        receives <React.Fragment> children directly, not component wrappers */}
                    {CatalogRoutes()}
                    {ContractsRoutes()}
                    {KnowledgeRoutes()}
                    {ChangesRoutes()}
                    {OperationsRoutes()}
                    {AiHubRoutes()}
                    {GovernanceRoutes()}
                    {AdminRoutes()}
                  </Route>

                  <Route path="*" element={<Navigate to="/" replace />} />
                </Routes>
              </Suspense>
            </BrowserRouter>
            </ToastProvider>
          </PersonaProvider>
        </EnvironmentProvider>
      </AuthProvider>
      </BrandingProvider>
      </ThemeProvider>
    </QueryClientProvider>
  );
}
