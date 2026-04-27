import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useQuery } from '@tanstack/react-query';
import { DollarSign, TrendingUp, TrendingDown, AlertTriangle, BarChart3 } from 'lucide-react';
import { PageContainer, PageSection } from '../../../../components/shell';
import { PageHeader } from '../../../../components/PageHeader';
import { Card, CardBody } from '../../../../components/Card';
import { Badge } from '../../../../components/Badge';
import { PageLoadingState } from '../../../../components/PageLoadingState';
import client from '../../../../api/client';

type ContextFilter = 'service' | 'team' | 'domain' | 'environment';

interface CostNode {
  id: string;
  name: string;
  type: ContextFilter;
  currentCost: number;
  previousCost: number;
  trend: 'up' | 'down' | 'stable';
  anomaly: boolean;
  topCostDrivers: string[];
}

const useFinOpsContext = (context: ContextFilter) =>
  useQuery({
    queryKey: ['finops-context-views', context],
    queryFn: () =>
      client
        .get<{ nodes: CostNode[]; totalCost: number; isSimulated: boolean }>(
          '/api/v1/governance/finops/context-views',
          { params: { tenantId: 'default', context } }
        )
        .then((r) => r.data),
  });

const CONTEXT_TABS: { key: ContextFilter; label: string }[] = [
  { key: 'service', label: 'Service' },
  { key: 'team', label: 'Team' },
  { key: 'domain', label: 'Domain' },
  { key: 'environment', label: 'Environment' },
];

function formatCost(v: number) {
  return `$${v.toLocaleString('en-US', { maximumFractionDigits: 0 })}`;
}

export function FinOpsContextViewsPage() {
  const { t } = useTranslation();
  const [context, setContext] = useState<ContextFilter>('service');
  const { data, isLoading } = useFinOpsContext(context);

  const nodes = data?.nodes ?? [];

  return (
    <PageContainer>
      <PageHeader
        title={t('finOpsContext.title')}
        subtitle={t('finOpsContext.subtitle')}
      />
      <PageSection>
        {/* Context filter tabs */}
        <div className="flex gap-2 mb-6">
          {CONTEXT_TABS.map((tab) => (
            <button
              key={tab.key}
              onClick={() => setContext(tab.key)}
              className={`px-3 py-1.5 text-xs font-medium rounded-md transition-colors ${
                context === tab.key
                  ? 'bg-accent text-accent-foreground'
                  : 'bg-muted text-muted-foreground hover:bg-muted/80'
              }`}
            >
              {tab.label}
            </button>
          ))}
        </div>

        {/* Total cost */}
        {data && (
          <Card className="mb-4">
            <CardBody className="p-4 flex items-center gap-3">
              <DollarSign size={16} className="text-accent" />
              <div>
                <p className="text-xs text-muted-foreground">{t('finOpsContext.totalCost')}</p>
                <p className="text-xl font-bold">{formatCost(data.totalCost)}</p>
              </div>
            </CardBody>
          </Card>
        )}

        {isLoading ? (
          <PageLoadingState />
        ) : (
          <div className="space-y-2">
            {nodes.map((node) => {
              const delta = node.currentCost - node.previousCost;
              const deltaPct = node.previousCost > 0 ? ((delta / node.previousCost) * 100).toFixed(1) : '0';
              return (
                <Card key={node.id}>
                  <CardBody className="p-4">
                    <div className="flex items-center justify-between gap-3 mb-2">
                      <div className="flex items-center gap-2">
                        <BarChart3 size={14} className="text-muted-foreground" />
                        <span className="text-sm font-medium">{node.name}</span>
                        {node.anomaly && (
                          <Badge variant="warning" className="text-xs">
                            <AlertTriangle size={10} className="mr-1" />
                            {t('finOpsContext.anomaly')}
                          </Badge>
                        )}
                      </div>
                      <div className="text-right">
                        <p className="text-sm font-bold">{formatCost(node.currentCost)}</p>
                        <div className="flex items-center gap-1 text-xs">
                          {node.trend === 'up' ? (
                            <TrendingUp size={10} className="text-destructive" />
                          ) : node.trend === 'down' ? (
                            <TrendingDown size={10} className="text-success" />
                          ) : null}
                          <span className={node.trend === 'up' ? 'text-destructive' : node.trend === 'down' ? 'text-success' : 'text-muted-foreground'}>
                            {delta >= 0 ? '+' : ''}{deltaPct}%
                          </span>
                        </div>
                      </div>
                    </div>
                    {node.topCostDrivers.length > 0 && (
                      <div className="flex flex-wrap gap-1">
                        {node.topCostDrivers.map((d) => (
                          <Badge key={d} variant="secondary" className="text-xs">{d}</Badge>
                        ))}
                      </div>
                    )}
                  </CardBody>
                </Card>
              );
            })}
            {nodes.length === 0 && (
              <div className="text-center p-8 text-muted-foreground text-sm">
                {t('finOpsContext.empty')}
              </div>
            )}
          </div>
        )}

        <div className="mt-4 p-3 rounded-lg bg-muted/40 text-xs text-muted-foreground">
          {t('sotCenter.simulatedBanner')}
        </div>
      </PageSection>
    </PageContainer>
  );
}
