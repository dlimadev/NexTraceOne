/**
 * Route group: Governance — Executive, Reports, Compliance, Risk, Teams, Domains
 * @see docs/frontend-audit/frontend-prioritized-improvement-roadmap.md §F4-05
 */
import { lazy } from 'react';
import { Route } from 'react-router-dom';
import { ProtectedRoute } from '../components/ProtectedRoute';

const ExecutiveOverviewPage = lazy(() => import('../features/governance/pages/ExecutiveOverviewPage').then(m => ({ default: m.ExecutiveOverviewPage })));
const ExecutiveDrillDownPage = lazy(() => import('../features/governance/pages/ExecutiveDrillDownPage').then(m => ({ default: m.ExecutiveDrillDownPage })));
const ExecutiveFinOpsPage = lazy(() => import('../features/governance/pages/ExecutiveFinOpsPage').then(m => ({ default: m.ExecutiveFinOpsPage })));
const ReportsPage = lazy(() => import('../features/governance/pages/ReportsPage').then(m => ({ default: m.ReportsPage })));
const CompliancePage = lazy(() => import('../features/governance/pages/CompliancePage').then(m => ({ default: m.CompliancePage })));
const RiskCenterPage = lazy(() => import('../features/governance/pages/RiskCenterPage').then(m => ({ default: m.RiskCenterPage })));
const RiskHeatmapPage = lazy(() => import('../features/governance/pages/RiskHeatmapPage').then(m => ({ default: m.RiskHeatmapPage })));
const FinOpsPage = lazy(() => import('../features/governance/pages/FinOpsPage').then(m => ({ default: m.FinOpsPage })));
const ServiceFinOpsPage = lazy(() => import('../features/governance/pages/ServiceFinOpsPage').then(m => ({ default: m.ServiceFinOpsPage })));
const TeamFinOpsPage = lazy(() => import('../features/governance/pages/TeamFinOpsPage').then(m => ({ default: m.TeamFinOpsPage })));
const DomainFinOpsPage = lazy(() => import('../features/governance/pages/DomainFinOpsPage').then(m => ({ default: m.DomainFinOpsPage })));
const FinOpsConfigurationPage = lazy(() => import('../features/governance/pages/FinOpsConfigurationPage').then(m => ({ default: m.FinOpsConfigurationPage })));
const FinOpsBudgetApprovalsPage = lazy(() => import('../features/governance/pages/FinOpsBudgetApprovalsPage').then(m => ({ default: m.FinOpsBudgetApprovalsPage })));
const PolicyCatalogPage = lazy(() => import('../features/governance/pages/PolicyCatalogPage').then(m => ({ default: m.PolicyCatalogPage })));
const EnterpriseControlsPage = lazy(() => import('../features/governance/pages/EnterpriseControlsPage').then(m => ({ default: m.EnterpriseControlsPage })));
const EvidencePackagesPage = lazy(() => import('../features/governance/pages/EvidencePackagesPage').then(m => ({ default: m.EvidencePackagesPage })));
const MaturityScorecardsPage = lazy(() => import('../features/governance/pages/MaturityScorecardsPage').then(m => ({ default: m.MaturityScorecardsPage })));
const BenchmarkingPage = lazy(() => import('../features/governance/pages/BenchmarkingPage').then(m => ({ default: m.BenchmarkingPage })));
const TeamsOverviewPage = lazy(() => import('../features/governance/pages/TeamsOverviewPage').then(m => ({ default: m.TeamsOverviewPage })));
const TeamDetailPage = lazy(() => import('../features/governance/pages/TeamDetailPage').then(m => ({ default: m.TeamDetailPage })));
const DomainsOverviewPage = lazy(() => import('../features/governance/pages/DomainsOverviewPage').then(m => ({ default: m.DomainsOverviewPage })));
const DomainDetailPage = lazy(() => import('../features/governance/pages/DomainDetailPage').then(m => ({ default: m.DomainDetailPage })));
const GovernancePacksOverviewPage = lazy(() => import('../features/governance/pages/GovernancePacksOverviewPage').then(m => ({ default: m.GovernancePacksOverviewPage })));
const GovernancePackDetailPage = lazy(() => import('../features/governance/pages/GovernancePackDetailPage').then(m => ({ default: m.GovernancePackDetailPage })));
const WaiversPage = lazy(() => import('../features/governance/pages/WaiversPage').then(m => ({ default: m.WaiversPage })));
const DelegatedAdminPage = lazy(() => import('../features/governance/pages/DelegatedAdminPage').then(m => ({ default: m.DelegatedAdminPage })));
const GovernanceConfigurationPage = lazy(() => import('../features/governance/pages/GovernanceConfigurationPage').then(m => ({ default: m.GovernanceConfigurationPage })));
const DoraMetricsPage = lazy(() => import('../features/governance/pages/DoraMetricsPage').then(m => ({ default: m.DoraMetricsPage })));
const ServiceScorecardPage = lazy(() => import('../features/governance/pages/ServiceScorecardPage').then(m => ({ default: m.ServiceScorecardPage })));
const CustomDashboardsPage = lazy(() => import('../features/governance/pages/CustomDashboardsPage').then(m => ({ default: m.CustomDashboardsPage })));
const DashboardViewPage = lazy(() => import('../features/governance/pages/DashboardViewPage').then(m => ({ default: m.DashboardViewPage })));
const DashboardBuilderPage = lazy(() => import('../features/governance/pages/DashboardBuilderPage').then(m => ({ default: m.DashboardBuilderPage })));
const TechnicalDebtPage = lazy(() => import('../features/governance/pages/TechnicalDebtPage').then(m => ({ default: m.TechnicalDebtPage })));
const ApiPolicyAsCodePage = lazy(() => import('../features/governance/pages/ApiPolicyAsCodePage').then(m => ({ default: m.ApiPolicyAsCodePage })));
const GovernanceGatesPage = lazy(() => import('../features/governance/pages/GovernanceGatesPage').then(m => ({ default: m.GovernanceGatesPage })));
const WasteDetectionPage = lazy(() => import('../features/governance/pages/WasteDetectionPage').then(m => ({ default: m.WasteDetectionPage })));

export function GovernanceRoutes() {
  return (
    <>
      <Route
        path="/governance/executive"
        element={
          <ProtectedRoute permission="governance:reports:read" redirectTo="/unauthorized">
            <ExecutiveOverviewPage />
          </ProtectedRoute>
        }
      />
      <Route
        path="/governance/executive/drilldown"
        element={
          <ProtectedRoute permission="governance:reports:read" redirectTo="/unauthorized">
            <ExecutiveDrillDownPage />
          </ProtectedRoute>
        }
      />
      <Route
        path="/governance/executive/finops"
        element={
          <ProtectedRoute permission="governance:finops:read" redirectTo="/unauthorized">
            <ExecutiveFinOpsPage />
          </ProtectedRoute>
        }
      />
      <Route
        path="/governance/reports"
        element={
          <ProtectedRoute permission="governance:reports:read" redirectTo="/unauthorized">
            <ReportsPage />
          </ProtectedRoute>
        }
      />
      <Route
        path="/governance/compliance"
        element={
          <ProtectedRoute permission="governance:compliance:read" redirectTo="/unauthorized">
            <CompliancePage />
          </ProtectedRoute>
        }
      />
      <Route
        path="/governance/risk"
        element={
          <ProtectedRoute permission="governance:risk:read" redirectTo="/unauthorized">
            <RiskCenterPage />
          </ProtectedRoute>
        }
      />
      <Route
        path="/governance/risk/heatmap"
        element={
          <ProtectedRoute permission="governance:risk:read" redirectTo="/unauthorized">
            <RiskHeatmapPage />
          </ProtectedRoute>
        }
      />
      <Route
        path="/governance/finops"
        element={
          <ProtectedRoute permission="governance:finops:read" redirectTo="/unauthorized">
            <FinOpsPage />
          </ProtectedRoute>
        }
      />
      <Route
        path="/governance/finops/service/:serviceId"
        element={
          <ProtectedRoute permission="governance:finops:read" redirectTo="/unauthorized">
            <ServiceFinOpsPage />
          </ProtectedRoute>
        }
      />
      <Route
        path="/governance/finops/team/:teamId"
        element={
          <ProtectedRoute permission="governance:finops:read" redirectTo="/unauthorized">
            <TeamFinOpsPage />
          </ProtectedRoute>
        }
      />
      <Route
        path="/governance/finops/domain/:domainId"
        element={
          <ProtectedRoute permission="governance:finops:read" redirectTo="/unauthorized">
            <DomainFinOpsPage />
          </ProtectedRoute>
        }
      />
      <Route
        path="/governance/finops/configuration"
        element={
          <ProtectedRoute permission="governance:finops:read" redirectTo="/unauthorized">
            <FinOpsConfigurationPage />
          </ProtectedRoute>
        }
      />
      <Route
        path="/governance/finops/approvals"
        element={
          <ProtectedRoute permission="governance:finops:read" redirectTo="/unauthorized">
            <FinOpsBudgetApprovalsPage />
          </ProtectedRoute>
        }
      />
      <Route
        path="/governance/policies"
        element={
          <ProtectedRoute permission="governance:policies:read" redirectTo="/unauthorized">
            <PolicyCatalogPage />
          </ProtectedRoute>
        }
      />
      <Route
        path="/governance/controls"
        element={
          <ProtectedRoute permission="governance:controls:read" redirectTo="/unauthorized">
            <EnterpriseControlsPage />
          </ProtectedRoute>
        }
      />
      <Route
        path="/governance/evidence"
        element={
          <ProtectedRoute permission="governance:evidence:read" redirectTo="/unauthorized">
            <EvidencePackagesPage />
          </ProtectedRoute>
        }
      />
      <Route
        path="/governance/maturity"
        element={
          <ProtectedRoute permission="governance:reports:read" redirectTo="/unauthorized">
            <MaturityScorecardsPage />
          </ProtectedRoute>
        }
      />
      <Route
        path="/governance/benchmarking"
        element={
          <ProtectedRoute permission="governance:reports:read" redirectTo="/unauthorized">
            <BenchmarkingPage />
          </ProtectedRoute>
        }
      />
      <Route
        path="/governance/teams"
        element={
          <ProtectedRoute permission="governance:teams:read" redirectTo="/unauthorized">
            <TeamsOverviewPage />
          </ProtectedRoute>
        }
      />
      <Route
        path="/governance/teams/:teamId"
        element={
          <ProtectedRoute permission="governance:teams:read" redirectTo="/unauthorized">
            <TeamDetailPage />
          </ProtectedRoute>
        }
      />
      <Route
        path="/governance/domains"
        element={
          <ProtectedRoute permission="governance:domains:read" redirectTo="/unauthorized">
            <DomainsOverviewPage />
          </ProtectedRoute>
        }
      />
      <Route
        path="/governance/domains/:domainId"
        element={
          <ProtectedRoute permission="governance:domains:read" redirectTo="/unauthorized">
            <DomainDetailPage />
          </ProtectedRoute>
        }
      />
      <Route
        path="/governance/packs"
        element={
          <ProtectedRoute permission="governance:packs:read" redirectTo="/unauthorized">
            <GovernancePacksOverviewPage />
          </ProtectedRoute>
        }
      />
      <Route
        path="/governance/packs/:packId"
        element={
          <ProtectedRoute permission="governance:packs:read" redirectTo="/unauthorized">
            <GovernancePackDetailPage />
          </ProtectedRoute>
        }
      />
      <Route
        path="/governance/waivers"
        element={
          <ProtectedRoute permission="governance:waivers:read" redirectTo="/unauthorized">
            <WaiversPage />
          </ProtectedRoute>
        }
      />
      <Route
        path="/governance/delegated-admin"
        element={
          <ProtectedRoute permission="governance:admin:read" redirectTo="/unauthorized">
            <DelegatedAdminPage />
          </ProtectedRoute>
        }
      />
      <Route
        path="/platform/configuration/governance"
        element={
          <ProtectedRoute permission="platform:admin:read" redirectTo="/unauthorized">
            <GovernanceConfigurationPage />
          </ProtectedRoute>
        }
      />
      <Route
        path="/governance/dora-metrics"
        element={
          <ProtectedRoute permission="governance:reports:read" redirectTo="/unauthorized">
            <DoraMetricsPage />
          </ProtectedRoute>
        }
      />
      <Route
        path="/governance/scorecards"
        element={
          <ProtectedRoute permission="governance:reports:read" redirectTo="/unauthorized">
            <ServiceScorecardPage />
          </ProtectedRoute>
        }
      />
      <Route
        path="/governance/scorecards/:serviceName"
        element={
          <ProtectedRoute permission="governance:reports:read" redirectTo="/unauthorized">
            <ServiceScorecardPage />
          </ProtectedRoute>
        }
      />
      <Route
        path="/governance/custom-dashboards"
        element={
          <ProtectedRoute permission="governance:reports:read" redirectTo="/unauthorized">
            <CustomDashboardsPage />
          </ProtectedRoute>
        }
      />
      <Route
        path="/governance/dashboards/:dashboardId"
        element={
          <ProtectedRoute permission="governance:reports:read" redirectTo="/unauthorized">
            <DashboardViewPage />
          </ProtectedRoute>
        }
      />
      <Route
        path="/governance/dashboards/:dashboardId/edit"
        element={
          <ProtectedRoute permission="governance:reports:write" redirectTo="/unauthorized">
            <DashboardBuilderPage />
          </ProtectedRoute>
        }
      />
      <Route
        path="/governance/technical-debt"
        element={
          <ProtectedRoute permission="governance:reports:read" redirectTo="/unauthorized">
            <TechnicalDebtPage />
          </ProtectedRoute>
        }
      />
      <Route
        path="/governance/api-policy-as-code"
        element={
          <ProtectedRoute permission="governance:policies:read" redirectTo="/unauthorized">
            <ApiPolicyAsCodePage />
          </ProtectedRoute>
        }
      />
      <Route
        path="/governance/gates"
        element={
          <ProtectedRoute permission="governance:gates:read" redirectTo="/unauthorized">
            <GovernanceGatesPage />
          </ProtectedRoute>
        }
      />
      <Route
        path="/governance/waste-detection"
        element={
          <ProtectedRoute permission="governance:finops:read" redirectTo="/unauthorized">
            <WasteDetectionPage />
          </ProtectedRoute>
        }
      />
    </>
  );
}
