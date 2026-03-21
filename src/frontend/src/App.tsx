import { lazy, Suspense } from 'react';
import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { ReactQueryDevtools } from '@tanstack/react-query-devtools';
import { AuthProvider } from './contexts/AuthContext';
import { EnvironmentProvider } from './contexts/EnvironmentContext';
import { PersonaProvider } from './contexts/PersonaContext';
import { AppShell } from './components/shell/AppShell';
import { ProtectedRoute } from './components/ProtectedRoute';

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
const DeveloperPortalPage = lazy(() => import('./features/catalog/pages/DeveloperPortalPage').then(m => ({ default: m.DeveloperPortalPage })));

// ── Contracts (lazy) ──
const ContractCatalogPage = lazy(() => import('./features/contracts/catalog/ContractCatalogPage').then(m => ({ default: m.ContractCatalogPage })));
const CreateServicePage = lazy(() => import('./features/contracts/create/CreateServicePage').then(m => ({ default: m.CreateServicePage })));
const DraftStudioPage = lazy(() => import('./features/contracts/studio/DraftStudioPage').then(m => ({ default: m.DraftStudioPage })));
const ContractWorkspacePage = lazy(() => import('./features/contracts/workspace/ContractWorkspacePage').then(m => ({ default: m.ContractWorkspacePage })));

// ── Change-governance (lazy) ──
const ReleasesPage = lazy(() => import('./features/change-governance/pages/ReleasesPage').then(m => ({ default: m.ReleasesPage })));
const WorkflowPage = lazy(() => import('./features/change-governance/pages/WorkflowPage').then(m => ({ default: m.WorkflowPage })));
const PromotionPage = lazy(() => import('./features/change-governance/pages/PromotionPage').then(m => ({ default: m.PromotionPage })));
const ChangeCatalogPage = lazy(() => import('./features/change-governance/pages/ChangeCatalogPage').then(m => ({ default: m.ChangeCatalogPage })));
const ChangeDetailPage = lazy(() => import('./features/change-governance/pages/ChangeDetailPage').then(m => ({ default: m.ChangeDetailPage })));

// ── Operations (lazy) ──
const IncidentsPage = lazy(() => import('./features/operations/pages/IncidentsPage').then(m => ({ default: m.IncidentsPage })));
const IncidentDetailPage = lazy(() => import('./features/operations/pages/IncidentDetailPage').then(m => ({ default: m.IncidentDetailPage })));
const RunbooksPage = lazy(() => import('./features/operations/pages/RunbooksPage').then(m => ({ default: m.RunbooksPage })));
const TeamReliabilityPage = lazy(() => import('./features/operations/pages/TeamReliabilityPage').then(m => ({ default: m.TeamReliabilityPage })));
const ServiceReliabilityDetailPage = lazy(() => import('./features/operations/pages/ServiceReliabilityDetailPage').then(m => ({ default: m.ServiceReliabilityDetailPage })));
const AutomationWorkflowsPage = lazy(() => import('./features/operations/pages/AutomationWorkflowsPage').then(m => ({ default: m.AutomationWorkflowsPage })));
const AutomationAdminPage = lazy(() => import('./features/operations/pages/AutomationAdminPage').then(m => ({ default: m.AutomationAdminPage })));
const AutomationWorkflowDetailPage = lazy(() => import('./features/operations/pages/AutomationWorkflowDetailPage').then(m => ({ default: m.AutomationWorkflowDetailPage })));
const PlatformOperationsPage = lazy(() => import('./features/operations/pages/PlatformOperationsPage').then(m => ({ default: m.PlatformOperationsPage })));

// ── AI Hub (lazy) ──
const AiAssistantPage = lazy(() => import('./features/ai-hub/pages/AiAssistantPage').then(m => ({ default: m.AiAssistantPage })));
const ModelRegistryPage = lazy(() => import('./features/ai-hub/pages/ModelRegistryPage').then(m => ({ default: m.ModelRegistryPage })));
const AiPoliciesPage = lazy(() => import('./features/ai-hub/pages/AiPoliciesPage').then(m => ({ default: m.AiPoliciesPage })));
const AiRoutingPage = lazy(() => import('./features/ai-hub/pages/AiRoutingPage').then(m => ({ default: m.AiRoutingPage })));
const IdeIntegrationsPage = lazy(() => import('./features/ai-hub/pages/IdeIntegrationsPage').then(m => ({ default: m.IdeIntegrationsPage })));
const TokenBudgetPage = lazy(() => import('./features/ai-hub/pages/TokenBudgetPage').then(m => ({ default: m.TokenBudgetPage })));
const AiAuditPage = lazy(() => import('./features/ai-hub/pages/AiAuditPage').then(m => ({ default: m.AiAuditPage })));
const AiAnalysisPage = lazy(() => import('./features/ai-hub/pages/AiAnalysisPage').then(m => ({ default: m.AiAnalysisPage })));
const AiAgentsPage = lazy(() => import('./features/ai-hub/pages/AiAgentsPage').then(m => ({ default: m.AiAgentsPage })));
const AgentDetailPage = lazy(() => import('./features/ai-hub/pages/AgentDetailPage').then(m => ({ default: m.AgentDetailPage })));

// ── Governance (lazy) ──
const ExecutiveOverviewPage = lazy(() => import('./features/governance/pages/ExecutiveOverviewPage').then(m => ({ default: m.ExecutiveOverviewPage })));
const ExecutiveDrillDownPage = lazy(() => import('./features/governance/pages/ExecutiveDrillDownPage').then(m => ({ default: m.ExecutiveDrillDownPage })));
const ExecutiveFinOpsPage = lazy(() => import('./features/governance/pages/ExecutiveFinOpsPage').then(m => ({ default: m.ExecutiveFinOpsPage })));
const ReportsPage = lazy(() => import('./features/governance/pages/ReportsPage').then(m => ({ default: m.ReportsPage })));
const CompliancePage = lazy(() => import('./features/governance/pages/CompliancePage').then(m => ({ default: m.CompliancePage })));
const RiskCenterPage = lazy(() => import('./features/governance/pages/RiskCenterPage').then(m => ({ default: m.RiskCenterPage })));
const RiskHeatmapPage = lazy(() => import('./features/governance/pages/RiskHeatmapPage').then(m => ({ default: m.RiskHeatmapPage })));
const FinOpsPage = lazy(() => import('./features/governance/pages/FinOpsPage').then(m => ({ default: m.FinOpsPage })));
const ServiceFinOpsPage = lazy(() => import('./features/governance/pages/ServiceFinOpsPage').then(m => ({ default: m.ServiceFinOpsPage })));
const TeamFinOpsPage = lazy(() => import('./features/governance/pages/TeamFinOpsPage').then(m => ({ default: m.TeamFinOpsPage })));
const DomainFinOpsPage = lazy(() => import('./features/governance/pages/DomainFinOpsPage').then(m => ({ default: m.DomainFinOpsPage })));
const PolicyCatalogPage = lazy(() => import('./features/governance/pages/PolicyCatalogPage').then(m => ({ default: m.PolicyCatalogPage })));
const EnterpriseControlsPage = lazy(() => import('./features/governance/pages/EnterpriseControlsPage').then(m => ({ default: m.EnterpriseControlsPage })));
const EvidencePackagesPage = lazy(() => import('./features/governance/pages/EvidencePackagesPage').then(m => ({ default: m.EvidencePackagesPage })));
const MaturityScorecardsPage = lazy(() => import('./features/governance/pages/MaturityScorecardsPage').then(m => ({ default: m.MaturityScorecardsPage })));
const BenchmarkingPage = lazy(() => import('./features/governance/pages/BenchmarkingPage').then(m => ({ default: m.BenchmarkingPage })));
const TeamsOverviewPage = lazy(() => import('./features/governance/pages/TeamsOverviewPage').then(m => ({ default: m.TeamsOverviewPage })));
const TeamDetailPage = lazy(() => import('./features/governance/pages/TeamDetailPage').then(m => ({ default: m.TeamDetailPage })));
const DomainsOverviewPage = lazy(() => import('./features/governance/pages/DomainsOverviewPage').then(m => ({ default: m.DomainsOverviewPage })));
const DomainDetailPage = lazy(() => import('./features/governance/pages/DomainDetailPage').then(m => ({ default: m.DomainDetailPage })));
const GovernancePacksOverviewPage = lazy(() => import('./features/governance/pages/GovernancePacksOverviewPage').then(m => ({ default: m.GovernancePacksOverviewPage })));
const GovernancePackDetailPage = lazy(() => import('./features/governance/pages/GovernancePackDetailPage').then(m => ({ default: m.GovernancePackDetailPage })));
const WaiversPage = lazy(() => import('./features/governance/pages/WaiversPage').then(m => ({ default: m.WaiversPage })));
const DelegatedAdminPage = lazy(() => import('./features/governance/pages/DelegatedAdminPage').then(m => ({ default: m.DelegatedAdminPage })));

// ── Integrations (lazy) ──
const IntegrationHubPage = lazy(() => import('./features/integrations/pages/IntegrationHubPage').then(m => ({ default: m.IntegrationHubPage })));
const ConnectorDetailPage = lazy(() => import('./features/integrations/pages/ConnectorDetailPage').then(m => ({ default: m.ConnectorDetailPage })));
const IngestionExecutionsPage = lazy(() => import('./features/integrations/pages/IngestionExecutionsPage').then(m => ({ default: m.IngestionExecutionsPage })));
const IngestionFreshnessPage = lazy(() => import('./features/integrations/pages/IngestionFreshnessPage').then(m => ({ default: m.IngestionFreshnessPage })));

// ── Product Analytics (lazy) ──
const ProductAnalyticsOverviewPage = lazy(() => import('./features/product-analytics/pages/ProductAnalyticsOverviewPage').then(m => ({ default: m.ProductAnalyticsOverviewPage })));
const ModuleAdoptionPage = lazy(() => import('./features/product-analytics/pages/ModuleAdoptionPage').then(m => ({ default: m.ModuleAdoptionPage })));
const PersonaUsagePage = lazy(() => import('./features/product-analytics/pages/PersonaUsagePage').then(m => ({ default: m.PersonaUsagePage })));
const JourneyFunnelPage = lazy(() => import('./features/product-analytics/pages/JourneyFunnelPage').then(m => ({ default: m.JourneyFunnelPage })));
const ValueTrackingPage = lazy(() => import('./features/product-analytics/pages/ValueTrackingPage').then(m => ({ default: m.ValueTrackingPage })));

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
        <EnvironmentProvider>
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
                path="/portal/*"
                element={
                  <ProtectedRoute permission="developer-portal:read" redirectTo="/unauthorized">
                    <DeveloperPortalPage />
                  </ProtectedRoute>
                }
              />
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
                    <CreateServicePage />
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
                path="/contracts/:contractVersionId"
                element={
                  <ProtectedRoute permission="contracts:read" redirectTo="/unauthorized">
                    <ContractWorkspacePage />
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
                    <IncidentsPage />
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
                path="/operations/reliability"
                element={
                  <ProtectedRoute permission="operations:reliability:read" redirectTo="/unauthorized">
                    <TeamReliabilityPage />
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
              {/* ── AI Hub ── */}
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
              {/* ── Governance ── */}
              <Route
                path="/governance/executive"
                element={
                  <ProtectedRoute permission="governance:read" redirectTo="/unauthorized">
                    <ExecutiveOverviewPage />
                  </ProtectedRoute>
                }
              />
              <Route
                path="/governance/executive/drilldown"
                element={
                  <ProtectedRoute permission="governance:read" redirectTo="/unauthorized">
                    <ExecutiveDrillDownPage />
                  </ProtectedRoute>
                }
              />
              <Route
                path="/governance/executive/finops"
                element={
                  <ProtectedRoute permission="governance:read" redirectTo="/unauthorized">
                    <ExecutiveFinOpsPage />
                  </ProtectedRoute>
                }
              />
              <Route
                path="/governance/reports"
                element={
                  <ProtectedRoute permission="governance:read" redirectTo="/unauthorized">
                    <ReportsPage />
                  </ProtectedRoute>
                }
              />
              <Route
                path="/governance/compliance"
                element={
                  <ProtectedRoute permission="governance:read" redirectTo="/unauthorized">
                    <CompliancePage />
                  </ProtectedRoute>
                }
              />
              <Route
                path="/governance/risk"
                element={
                  <ProtectedRoute permission="governance:read" redirectTo="/unauthorized">
                    <RiskCenterPage />
                  </ProtectedRoute>
                }
              />
              <Route
                path="/governance/risk/heatmap"
                element={
                  <ProtectedRoute permission="governance:read" redirectTo="/unauthorized">
                    <RiskHeatmapPage />
                  </ProtectedRoute>
                }
              />
              <Route
                path="/governance/finops"
                element={
                  <ProtectedRoute permission="governance:read" redirectTo="/unauthorized">
                    <FinOpsPage />
                  </ProtectedRoute>
                }
              />
              <Route
                path="/governance/finops/service/:serviceId"
                element={
                  <ProtectedRoute permission="governance:read" redirectTo="/unauthorized">
                    <ServiceFinOpsPage />
                  </ProtectedRoute>
                }
              />
              <Route
                path="/governance/finops/team/:teamId"
                element={
                  <ProtectedRoute permission="governance:read" redirectTo="/unauthorized">
                    <TeamFinOpsPage />
                  </ProtectedRoute>
                }
              />
              <Route
                path="/governance/finops/domain/:domainId"
                element={
                  <ProtectedRoute permission="governance:read" redirectTo="/unauthorized">
                    <DomainFinOpsPage />
                  </ProtectedRoute>
                }
              />
              <Route
                path="/governance/policies"
                element={
                  <ProtectedRoute permission="governance:read" redirectTo="/unauthorized">
                    <PolicyCatalogPage />
                  </ProtectedRoute>
                }
              />
              <Route
                path="/governance/controls"
                element={
                  <ProtectedRoute permission="governance:read" redirectTo="/unauthorized">
                    <EnterpriseControlsPage />
                  </ProtectedRoute>
                }
              />
              <Route
                path="/governance/evidence"
                element={
                  <ProtectedRoute permission="governance:read" redirectTo="/unauthorized">
                    <EvidencePackagesPage />
                  </ProtectedRoute>
                }
              />
              <Route
                path="/governance/maturity"
                element={
                  <ProtectedRoute permission="governance:read" redirectTo="/unauthorized">
                    <MaturityScorecardsPage />
                  </ProtectedRoute>
                }
              />
              <Route
                path="/governance/benchmarking"
                element={
                  <ProtectedRoute permission="governance:read" redirectTo="/unauthorized">
                    <BenchmarkingPage />
                  </ProtectedRoute>
                }
              />
              <Route
                path="/governance/teams"
                element={
                  <ProtectedRoute permission="governance:read" redirectTo="/unauthorized">
                    <TeamsOverviewPage />
                  </ProtectedRoute>
                }
              />
              <Route
                path="/governance/teams/:teamId"
                element={
                  <ProtectedRoute permission="governance:read" redirectTo="/unauthorized">
                    <TeamDetailPage />
                  </ProtectedRoute>
                }
              />
              <Route
                path="/governance/domains"
                element={
                  <ProtectedRoute permission="governance:read" redirectTo="/unauthorized">
                    <DomainsOverviewPage />
                  </ProtectedRoute>
                }
              />
              <Route
                path="/governance/domains/:domainId"
                element={
                  <ProtectedRoute permission="governance:read" redirectTo="/unauthorized">
                    <DomainDetailPage />
                  </ProtectedRoute>
                }
              />
              <Route
                path="/governance/packs"
                element={
                  <ProtectedRoute permission="governance:read" redirectTo="/unauthorized">
                    <GovernancePacksOverviewPage />
                  </ProtectedRoute>
                }
              />
              <Route
                path="/governance/packs/:packId"
                element={
                  <ProtectedRoute permission="governance:read" redirectTo="/unauthorized">
                    <GovernancePackDetailPage />
                  </ProtectedRoute>
                }
              />
              <Route
                path="/governance/waivers"
                element={
                  <ProtectedRoute permission="governance:read" redirectTo="/unauthorized">
                    <WaiversPage />
                  </ProtectedRoute>
                }
              />
              <Route
                path="/governance/delegated-admin"
                element={
                  <ProtectedRoute permission="governance:read" redirectTo="/unauthorized">
                    <DelegatedAdminPage />
                  </ProtectedRoute>
                }
              />
              {/* ── Integrations ── */}
              <Route
                path="/integrations"
                element={
                  <ProtectedRoute permission="integrations:read" redirectTo="/unauthorized">
                    <IntegrationHubPage />
                  </ProtectedRoute>
                }
              />
              <Route
                path="/integrations/:connectorId"
                element={
                  <ProtectedRoute permission="integrations:read" redirectTo="/unauthorized">
                    <ConnectorDetailPage />
                  </ProtectedRoute>
                }
              />
              <Route
                path="/integrations/executions"
                element={
                  <ProtectedRoute permission="integrations:read" redirectTo="/unauthorized">
                    <IngestionExecutionsPage />
                  </ProtectedRoute>
                }
              />
              <Route
                path="/integrations/freshness"
                element={
                  <ProtectedRoute permission="integrations:read" redirectTo="/unauthorized">
                    <IngestionFreshnessPage />
                  </ProtectedRoute>
                }
              />
              {/* ── Product Analytics ── */}
              <Route
                path="/analytics"
                element={
                  <ProtectedRoute permission="analytics:read" redirectTo="/unauthorized">
                    <ProductAnalyticsOverviewPage />
                  </ProtectedRoute>
                }
              />
              <Route
                path="/analytics/adoption"
                element={
                  <ProtectedRoute permission="analytics:read" redirectTo="/unauthorized">
                    <ModuleAdoptionPage />
                  </ProtectedRoute>
                }
              />
              <Route
                path="/analytics/personas"
                element={
                  <ProtectedRoute permission="analytics:read" redirectTo="/unauthorized">
                    <PersonaUsagePage />
                  </ProtectedRoute>
                }
              />
              <Route
                path="/analytics/journeys"
                element={
                  <ProtectedRoute permission="analytics:read" redirectTo="/unauthorized">
                    <JourneyFunnelPage />
                  </ProtectedRoute>
                }
              />
              <Route
                path="/analytics/value"
                element={
                  <ProtectedRoute permission="analytics:read" redirectTo="/unauthorized">
                    <ValueTrackingPage />
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
              <Route
                path="/platform/operations"
                element={
                  <ProtectedRoute permission="platform:admin:read" redirectTo="/unauthorized">
                    <PlatformOperationsPage />
                  </ProtectedRoute>
                }
              />
            </Route>
            <Route path="*" element={<Navigate to="/" replace />} />
          </Routes>
          </Suspense>
          </BrowserRouter>
        </PersonaProvider>
        </EnvironmentProvider>
      </AuthProvider>
      <ReactQueryDevtools initialIsOpen={false} buttonPosition="bottom-left" />
    </QueryClientProvider>
  );
}
