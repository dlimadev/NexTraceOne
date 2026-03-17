import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import {
  Coins, Search, AlertCircle, TrendingUp, BarChart3,
} from 'lucide-react';
import { Card, CardBody } from '../../../components/Card';
import { Badge } from '../../../components/Badge';
import { StatCard } from '../../../components/StatCard';
import { PageContainer } from '../../../components/shell';

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
}

const mockBudgets: Budget[] = [
  { id: '1', name: 'Engineering Team Budget', scope: 'team', scopeValue: 'Engineering', period: 'Monthly', maxTokens: 500000, maxRequests: 5000, currentTokensUsed: 234500, currentRequestCount: 2345, isActive: true },
  { id: '2', name: 'Product Team Budget', scope: 'team', scopeValue: 'Product', period: 'Monthly', maxTokens: 200000, maxRequests: 2000, currentTokensUsed: 195000, currentRequestCount: 1890, isActive: true },
  { id: '3', name: 'Default User Budget', scope: 'role', scopeValue: 'Engineer', period: 'Daily', maxTokens: 10000, maxRequests: 100, currentTokensUsed: 11200, currentRequestCount: 112, isActive: true },
  { id: '4', name: 'Executive Budget', scope: 'role', scopeValue: 'Executive', period: 'Weekly', maxTokens: 50000, maxRequests: 200, currentTokensUsed: 12000, currentRequestCount: 45, isActive: true },
];

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

  const filtered = mockBudgets.filter((b) =>
    !search || b.name.toLowerCase().includes(search.toLowerCase()) || b.scopeValue.toLowerCase().includes(search.toLowerCase()),
  );

  const totalActive = mockBudgets.filter((b) => b.isActive).length;
  const quotaExceeded = mockBudgets.filter((b) => b.currentTokensUsed > b.maxTokens || b.currentRequestCount > b.maxRequests).length;
  const totalTokensUsed = mockBudgets.reduce((s, b) => s + b.currentTokensUsed, 0);

  return (
    <PageContainer>
      <div className="mb-6">
        <h1 className="text-2xl font-bold text-heading">{t('aiHub.budgetTitle')}</h1>
        <p className="text-muted mt-1">{t('aiHub.budgetSubtitle')}</p>
      </div>

      {/* Stats */}
      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4 mb-6">
        <StatCard title={t('aiHub.budgetTotalStat')} value={mockBudgets.length} icon={<Coins size={20} />} color="text-accent" />
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
        {filtered.map((b) => {
          const tokenPct = usagePct(b.currentTokensUsed, b.maxTokens);
          const reqPct = usagePct(b.currentRequestCount, b.maxRequests);
          const exceeded = b.currentTokensUsed > b.maxTokens || b.currentRequestCount > b.maxRequests;

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
        {filtered.length === 0 && (
          <Card><CardBody><p className="text-center text-muted py-8">{t('aiHub.noBudgetsFound')}</p></CardBody></Card>
        )}
      </div>
    </PageContainer>
  );
}
