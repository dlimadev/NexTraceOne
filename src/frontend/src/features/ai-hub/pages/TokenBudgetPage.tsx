import { useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useQuery } from '@tanstack/react-query';
import {
  Coins, Search, AlertCircle, TrendingUp, BarChart3,
} from 'lucide-react';
import { Card, CardBody } from '../../../components/Card';
import { Badge } from '../../../components/Badge';
import { StatCard } from '../../../components/StatCard';
import { PageContainer } from '../../../components/shell';
import { PageHeader } from '../../../components/PageHeader';
import { Loader } from '../../../components/Loader';
import { PageErrorState } from '../../../components/PageErrorState';
import { EmptyState } from '../../../components/EmptyState';
import { Button } from '../../../components/Button';
import { aiGovernanceApi } from '../api';

interface Budget {
  id: string;
  name: string;
  scope: string;
  scopeValue: string;
  period: string;
  maxTokens: number;
  maxRequests: number;
  currentTokensUsed: number;
  currentRequestCount: number;
  isActive: boolean;
  isQuotaExceeded: boolean;
}

function usagePct(used: number, max: number): number {
  return max > 0 ? Math.min(Math.round((used / max) * 100), 100) : 0;
}

function barColor(pct: number): string {
  if (pct >= 100) return 'bg-critical';
  if (pct >= 80) return 'bg-warning';
  return 'bg-accent';
}

/**
 * Página de Token / Budget Governance — gestão de orçamentos e quotas IA.
 * Parte do módulo AI Hub do NexTraceOne.
 */
export function TokenBudgetPage() {
  const { t } = useTranslation();
  const [search, setSearch] = useState('');

  const {
    data,
    isLoading,
    isError,
    refetch,
  } = useQuery({
    queryKey: ['ai-governance', 'budgets'],
    queryFn: () => aiGovernanceApi.listBudgets(),
    staleTime: 30_000,
  });

  const budgets: Budget[] = useMemo(() => {
    const items = (data?.items ?? []) as Array<{
      budgetId: string;
      name: string;
      scope: string;
      scopeValue: string;
      period: string;
      maxTokens: number;
      maxRequests: number;
      currentTokensUsed: number;
      currentRequestCount: number;
      isActive: boolean;
      isQuotaExceeded: boolean;
    }>;

    return items.map((b) => ({
      id: b.budgetId,
      name: b.name,
      scope: b.scope,
      scopeValue: b.scopeValue,
      period: b.period,
      maxTokens: b.maxTokens,
      maxRequests: b.maxRequests,
      currentTokensUsed: b.currentTokensUsed,
      currentRequestCount: b.currentRequestCount,
      isActive: b.isActive,
      isQuotaExceeded: b.isQuotaExceeded,
    }));
  }, [data]);

  const filtered = budgets.filter((b) =>
    !search || b.name.toLowerCase().includes(search.toLowerCase()) || b.scopeValue.toLowerCase().includes(search.toLowerCase()),
  );

  const totalActive = budgets.filter((b) => b.isActive).length;
  const quotaExceeded = budgets.filter((b) => b.isQuotaExceeded).length;
  const totalTokensUsed = budgets.reduce((s, b) => s + b.currentTokensUsed, 0);

  return (
    <PageContainer>
      <PageHeader
        title={t('aiHub.budgetTitle')}
        subtitle={t('aiHub.budgetSubtitle')}
      />

      {/* Stats */}
      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4 mb-6">
        <StatCard title={t('aiHub.budgetTotalStat')} value={budgets.length} icon={<Coins size={20} />} color="text-accent" />
        <StatCard title={t('aiHub.budgetActiveStat')} value={totalActive} icon={<BarChart3 size={20} />} color="text-success" />
        <StatCard title={t('aiHub.budgetExceededStat')} value={quotaExceeded} icon={<AlertCircle size={20} />} color="text-critical" />
        <StatCard title={t('aiHub.budgetTokensUsedStat')} value={totalTokensUsed.toLocaleString()} icon={<TrendingUp size={20} />} color="text-info" />
      </div>

      {/* Search */}
      <div className="flex items-center gap-3 mb-6">
        <div className="relative flex-1 max-w-xs">
          <Search size={16} className="absolute left-3 top-1/2 -translate-y-1/2 text-muted" />
          <input
            type="text"
            placeholder={t('aiHub.searchBudgets')}
            value={search}
            onChange={(e) => setSearch(e.target.value)}
            className="w-full pl-9 pr-3 py-2 rounded-md bg-surface border border-edge text-body text-sm placeholder:text-muted focus:outline-none focus:ring-2 focus:ring-accent"
          />
        </div>
      </div>

      {/* Budget list */}
      <div className="space-y-3">
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

        {!isLoading && !isError && filtered.map((b) => {
          const tokenPct = usagePct(b.currentTokensUsed, b.maxTokens);
          const reqPct = usagePct(b.currentRequestCount, b.maxRequests);
          const exceeded = b.isQuotaExceeded;

          return (
            <Card key={b.id}>
              <CardBody>
                <div className="flex items-start justify-between gap-4 mb-3">
                  <div>
                    <div className="flex items-center gap-2 mb-1">
                      <h3 className="text-sm font-semibold text-heading">{b.name}</h3>
                      {exceeded && <Badge variant="danger">{t('aiHub.quotaExceeded')}</Badge>}
                      <Badge variant={b.isActive ? 'success' : 'default'}>{b.isActive ? t('aiHub.statusActive') : t('aiHub.statusInactive')}</Badge>
                    </div>
                    <p className="text-xs text-muted">{b.scope}: {b.scopeValue} · {b.period}</p>
                  </div>
                </div>
                {/* Token progress */}
                <div className="mb-2">
                  <div className="flex justify-between text-xs text-muted mb-1">
                    <span>{t('aiHub.tokens')}</span>
                    <span>{b.currentTokensUsed.toLocaleString()} / {b.maxTokens.toLocaleString()} ({tokenPct}%)</span>
                  </div>
                  <div className="w-full h-2 bg-elevated rounded-full overflow-hidden">
                    <div className={`h-full rounded-full transition-all ${barColor(tokenPct)}`} style={{ width: `${tokenPct}%` }} />
                  </div>
                </div>
                {/* Request progress */}
                <div>
                  <div className="flex justify-between text-xs text-muted mb-1">
                    <span>{t('aiHub.requests')}</span>
                    <span>{b.currentRequestCount.toLocaleString()} / {b.maxRequests.toLocaleString()} ({reqPct}%)</span>
                  </div>
                  <div className="w-full h-2 bg-elevated rounded-full overflow-hidden">
                    <div className={`h-full rounded-full transition-all ${barColor(reqPct)}`} style={{ width: `${reqPct}%` }} />
                  </div>
                </div>
              </CardBody>
            </Card>
          );
        })}
        {!isLoading && !isError && filtered.length === 0 && (
          <EmptyState title={t('aiHub.noBudgetsFound')} size="compact" />
        )}
      </div>
    </PageContainer>
  );
}
