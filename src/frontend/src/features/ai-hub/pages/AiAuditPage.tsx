import { useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useQuery } from '@tanstack/react-query';
import {
  ClipboardList, Search, Shield, ShieldOff, Monitor,
} from 'lucide-react';
import { Card, CardBody } from '../../../components/Card';
import { Badge } from '../../../components/Badge';
import { StatCard } from '../../../components/StatCard';
import { PageContainer } from '../../../components/shell';
import { Loader } from '../../../components/Loader';
import { PageErrorState } from '../../../components/PageErrorState';
import { EmptyState } from '../../../components/EmptyState';
import { Button } from '../../../components/Button';
import { aiGovernanceApi } from '../api';

interface AuditEntry {
  id: string;
  userId: string;
  userDisplayName: string;
  modelId: string;
  modelName: string;
  provider: string;
  isInternal: boolean;
  timestamp: string;
  promptTokens: number;
  completionTokens: number;
  totalTokens: number;
  result: string;
  clientType: string;
  policyName: string | null;
  contextScope: string;
  correlationId: string;
  conversationId: string | null;
}

const resultBadge = (result: string): 'success' | 'danger' | 'warning' | 'default' => {
  if (result === 'Allowed') return 'success';
  if (result === 'Blocked') return 'danger';
  if (result === 'QuotaExceeded') return 'warning';
  return 'default';
};

function timeAgo(dateStr: string): string {
  const diff = Date.now() - new Date(dateStr).getTime();
  const mins = Math.floor(diff / 60000);
  if (mins < 60) return `${mins}m`;
  const hrs = Math.floor(mins / 60);
  if (hrs < 24) return `${hrs}h`;
  return `${Math.floor(hrs / 24)}d`;
}

/**
 * Página de AI Usage Audit — trilha de auditoria de uso de IA.
 * Parte do módulo AI Hub do NexTraceOne.
 */
export function AiAuditPage() {
  const { t } = useTranslation();
  const [search, setSearch] = useState('');
  const [resultFilter, setResultFilter] = useState<string>('all');

  const {
    data,
    isLoading,
    isError,
    refetch,
  } = useQuery({
    queryKey: ['ai-governance', 'audit', resultFilter],
    queryFn: () => aiGovernanceApi.listAuditEntries({
      result: resultFilter === 'all' ? undefined : resultFilter,
      pageSize: 200,
    }),
    staleTime: 15_000,
  });

  const entries: AuditEntry[] = useMemo(() => {
    const items = (data?.items ?? []) as Array<{
      entryId: string;
      userId: string;
      userDisplayName: string;
      modelId: string;
      modelName: string;
      provider: string;
      isInternal: boolean;
      timestamp: string;
      promptTokens: number;
      completionTokens: number;
      totalTokens: number;
      policyName?: string | null;
      result: string;
      contextScope: string;
      clientType: string;
      correlationId: string;
      conversationId?: string | null;
    }>;

    return items.map((e) => ({
      id: e.entryId,
      userId: e.userId,
      userDisplayName: e.userDisplayName,
      modelId: e.modelId,
      modelName: e.modelName,
      provider: e.provider,
      isInternal: e.isInternal,
      timestamp: e.timestamp,
      promptTokens: e.promptTokens,
      completionTokens: e.completionTokens,
      totalTokens: e.totalTokens,
      result: e.result,
      clientType: e.clientType,
      policyName: e.policyName ?? null,
      contextScope: e.contextScope,
      correlationId: e.correlationId,
      conversationId: e.conversationId ?? null,
    }));
  }, [data]);

  const filtered = entries.filter((e) => {
    if (resultFilter !== 'all' && e.result !== resultFilter) return false;
    if (search && !e.userDisplayName.toLowerCase().includes(search.toLowerCase()) && !e.modelName.toLowerCase().includes(search.toLowerCase())) return false;
    return true;
  });

  const totalInternal = entries.filter((e) => e.isInternal).length;
  const totalExternal = entries.filter((e) => !e.isInternal).length;
  const totalBlocked = entries.filter((e) => e.result === 'Blocked' || e.result === 'QuotaExceeded').length;

  const resultFilters: { key: string; label: string }[] = [
    { key: 'all', label: t('aiHub.filterAll') },
    { key: 'Allowed', label: t('aiHub.resultAllowed') },
    { key: 'Blocked', label: t('aiHub.resultBlocked') },
    { key: 'QuotaExceeded', label: t('aiHub.resultQuotaExceeded') },
  ];

  return (
    <PageContainer>
      <div className="mb-6">
        <h1 className="text-2xl font-bold text-heading">{t('aiHub.auditTitle')}</h1>
        <p className="text-muted mt-1">{t('aiHub.auditSubtitle')}</p>
      </div>

      {/* Stats */}
      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4 mb-6">
        <StatCard title={t('aiHub.auditTotalStat')} value={entries.length} icon={<ClipboardList size={20} />} color="text-accent" />
        <StatCard title={t('aiHub.auditInternalStat')} value={totalInternal} icon={<Shield size={20} />} color="text-success" />
        <StatCard title={t('aiHub.auditExternalStat')} value={totalExternal} icon={<Monitor size={20} />} color="text-info" />
        <StatCard title={t('aiHub.auditBlockedStat')} value={totalBlocked} icon={<ShieldOff size={20} />} color="text-critical" />
      </div>

      {/* Filters */}
      <div className="flex flex-wrap items-center gap-3 mb-6">
        <div className="relative flex-1 min-w-[200px] max-w-xs">
          <Search size={16} className="absolute left-3 top-1/2 -translate-y-1/2 text-muted" />
          <input
            type="text"
            placeholder={t('aiHub.searchAudit')}
            value={search}
            onChange={(e) => setSearch(e.target.value)}
            className="w-full pl-9 pr-3 py-2 rounded-md bg-surface border border-edge text-body text-sm placeholder:text-muted focus:outline-none focus:ring-2 focus:ring-accent"
          />
        </div>
        <div className="flex gap-1.5">
          {resultFilters.map((f) => (
            <button
              key={f.key}
              onClick={() => setResultFilter(f.key)}
              className={`px-3 py-1.5 rounded-md text-xs font-medium transition-colors ${resultFilter === f.key ? 'bg-accent text-heading' : 'bg-elevated text-muted hover:text-body'}`}
            >
              {f.label}
            </button>
          ))}
        </div>
      </div>

      {/* Audit table */}
      {isLoading && (
        <Card>
          <CardBody className="flex justify-center py-16">
            <Loader size="lg" />
          </CardBody>
        </Card>
      )}

      {isError && (
        <PageErrorState
          action={(
            <Button variant="secondary" size="sm" onClick={() => refetch()}>
              {t('common.retry', 'Retry')}
            </Button>
          )}
        />
      )}

      {!isLoading && !isError && filtered.length === 0 && (
        <EmptyState title={t('aiHub.noAuditFound')} size="compact" />
      )}

      {!isLoading && !isError && filtered.length > 0 && (
        <Card>
          <CardBody className="overflow-x-auto">
            <table className="w-full text-sm">
              <thead>
                <tr className="border-b border-edge text-left text-xs text-muted">
                  <th className="pb-2 pr-4">{t('aiHub.auditColTime')}</th>
                  <th className="pb-2 pr-4">{t('aiHub.auditColUser')}</th>
                  <th className="pb-2 pr-4">{t('aiHub.auditColModel')}</th>
                  <th className="pb-2 pr-4">{t('aiHub.auditColType')}</th>
                  <th className="pb-2 pr-4">{t('aiHub.auditColTokens')}</th>
                  <th className="pb-2 pr-4">{t('aiHub.auditColResult')}</th>
                  <th className="pb-2 pr-4">{t('aiHub.auditColClient')}</th>
                  <th className="pb-2">{t('aiHub.auditColPolicy')}</th>
                </tr>
              </thead>
              <tbody>
                {filtered.map((e) => (
                  <tr key={e.id} className="border-b border-edge/50 hover:bg-hover transition-colors">
                    <td className="py-2.5 pr-4 text-muted whitespace-nowrap">{timeAgo(e.timestamp)}</td>
                    <td className="py-2.5 pr-4 text-heading font-medium">{e.userDisplayName}</td>
                    <td className="py-2.5 pr-4">
                      <span className="text-body">{e.modelName}</span>
                      <span className="text-xs text-muted ml-1">({e.provider})</span>
                    </td>
                    <td className="py-2.5 pr-4">
                      <Badge variant={e.isInternal ? 'info' : 'warning'}>{e.isInternal ? t('aiHub.internal') : t('aiHub.external')}</Badge>
                    </td>
                    <td className="py-2.5 pr-4 text-body tabular-nums">{e.totalTokens.toLocaleString()}</td>
                    <td className="py-2.5 pr-4"><Badge variant={resultBadge(e.result)}>{e.result}</Badge></td>
                    <td className="py-2.5 pr-4"><Badge variant="default">{e.clientType}</Badge></td>
                    <td className="py-2.5 text-muted text-xs">{e.policyName ?? '—'}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </CardBody>
        </Card>
      )}
    </PageContainer>
  );
}
