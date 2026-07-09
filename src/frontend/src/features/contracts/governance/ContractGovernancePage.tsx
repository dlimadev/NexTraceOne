import { useState, useMemo } from 'react';
import { useTranslation } from 'react-i18next';
import { useQuery } from '@tanstack/react-query';
import {
  BarChart3, RefreshCw, CheckCircle2, Clock, ShieldCheck, FileWarning,
} from 'lucide-react';
import { PageHeader } from '../../../components/PageHeader';
import { PageContainer } from '../../../components/shell';
import { LoadingState, ErrorState } from '../shared/components/StateIndicators';
import { contractsApi } from '../api/contracts';
import { Button, Tabs } from '../../../shared/ui';
import { computeGovernanceInsights, computePolicyChecks } from './ContractGovernanceHelpers';
import { OverviewView, ApprovalsView, ComplianceView, GapsView, AuditView } from './ContractGovernanceViews';
import { GovernanceToolsSection } from './GovernanceToolsSection';
import type { GovernanceView } from './ContractGovernanceHelpers';

/**
 * Dashboard de governança de contratos — visão agregada e operacional.
 *
 * Inclui:
 * - KPIs de estado geral
 * - Distribuição por protocolo e lifecycle
 * - Approval workflow tracking
 * - Compliance checks e scoring
 * - Gap detection (ownership, docs, security, examples)
 * - Audit trail / history
 * - Risk identification
 */
export function ContractGovernancePage() {
  const { t } = useTranslation();
  const [view, setView] = useState<GovernanceView>('overview');

  const summaryQuery = useQuery({
    queryKey: ['contracts-summary'],
    queryFn: () => contractsApi.getContractsSummary(),
  });

  const listQuery = useQuery({
    queryKey: ['contracts-list-governance'],
    queryFn: () => contractsApi.listContracts({ pageSize: 200 }),
  });

  const summary = summaryQuery.data;
  const contracts = useMemo(() => listQuery.data?.items ?? [], [listQuery.data?.items]);
  const insights = useMemo(() => computeGovernanceInsights(contracts), [contracts]);
  const policyResults = useMemo(() => computePolicyChecks(contracts), [contracts]);

  if (summaryQuery.isLoading || listQuery.isLoading) return <PageContainer><LoadingState /></PageContainer>;
  if (summaryQuery.isError) return <PageContainer><ErrorState onRetry={() => summaryQuery.refetch()} /></PageContainer>;

  const views: { id: GovernanceView; labelKey: string; icon: React.ReactNode }[] = [
    { id: 'overview', labelKey: 'contracts.governance.views.overview', icon: <BarChart3 size={13} /> },
    { id: 'approvals', labelKey: 'contracts.governance.views.approvals', icon: <CheckCircle2 size={13} /> },
    { id: 'compliance', labelKey: 'contracts.governance.views.compliance', icon: <ShieldCheck size={13} /> },
    { id: 'gaps', labelKey: 'contracts.governance.views.gaps', icon: <FileWarning size={13} /> },
    { id: 'audit', labelKey: 'contracts.governance.views.audit', icon: <Clock size={13} /> },
  ];

  return (
    <PageContainer>
      <PageHeader
        title={t('contracts.governance.title', 'Contract Governance')}
        subtitle={t('contracts.governance.subtitle', 'Compliance overview, risk areas, and governance actions for all contracts.')}
        actions={
          <Button
            variant="ghost"
            size="sm"
            icon={<RefreshCw size={14} />}
            onClick={() => { summaryQuery.refetch(); listQuery.refetch(); }}
          >
            {t('common.refresh', 'Refresh')}
          </Button>
        }
      />

      {/* View tabs */}
      <Tabs
        items={views.map((v) => ({ id: v.id, label: t(v.labelKey, v.id), icon: v.icon }))}
        activeId={view}
        onChange={(id) => setView(id as GovernanceView)}
        variant="underline"
        size="sm"
        className="gap-1 overflow-x-auto mb-6"
      />

      {/* ── Overview View ── */}
      {view === 'overview' && (
        <OverviewView summary={summary} insights={insights} policyResults={policyResults} contracts={contracts} />
      )}

      {/* ── Approvals View ── */}
      {view === 'approvals' && (
        <ApprovalsView contracts={contracts} />
      )}

      {/* ── Compliance View ── */}
      {view === 'compliance' && (
        <ComplianceView policyResults={policyResults} contracts={contracts} />
      )}

      {/* ── Gaps View ── */}
      {view === 'gaps' && (
        <GapsView insights={insights} />
      )}

      {/* ── Audit View ── */}
      {view === 'audit' && (
        <AuditView contracts={contracts} />
      )}

      <GovernanceToolsSection />
    </PageContainer>
  );
}
