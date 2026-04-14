import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useQuery } from '@tanstack/react-query';
import { GitBranch, Eye, Wrench, RefreshCw } from 'lucide-react';
import { Card, CardBody } from '../../../components/Card';
import { Badge } from '../../../components/Badge';
import { PageLoadingState } from '../../../components/PageLoadingState';
import { PageErrorState } from '../../../components/PageErrorState';
import { PageContainer, StatsGrid, PageSection } from '../../../components/shell';
import { PageHeader } from '../../../components/PageHeader';
import { Button } from '../../../components/Button';
import { StatCard } from '../../../components/StatCard';
import client from '../../../api/client';

interface ConfigDrift {
  id: string;
  service: string;
  environmentPair: string;
  driftedKeys: number;
  lastCheck: string;
  severity: 'critical' | 'warning' | 'info';
  baseline: string;
  target: string;
}

interface ConfigDriftResponse {
  drifts: ConfigDrift[];
  driftCount: number;
  criticalCount: number;
}

const useConfigDrifts = (envPair: string) =>
  useQuery({
    queryKey: ['config-drifts', envPair],
    queryFn: () =>
      client
        .get<ConfigDriftResponse>('/operations/config-drift', {
          params: { envPair: envPair || undefined },
        })
        .then((r) => r.data),
  });

const SEV_VARIANT: Record<string, 'danger' | 'warning' | 'neutral'> = {
  critical: 'danger',
  warning: 'warning',
  info: 'neutral',
};

export function ConfigurationDriftPage() {
  const { t } = useTranslation();
  const [envPair, setEnvPair] = useState('staging→production');
  const { data, isLoading, isError, refetch } = useConfigDrifts(envPair);

  if (isLoading) return <PageLoadingState message={t('configDrift.loading')} />;
  if (isError) return <PageErrorState message={t('configDrift.error')} onRetry={() => refetch()} />;

  const drifts = data?.drifts ?? [];

  return (
    <PageContainer>
      <PageHeader
        title={t('configDrift.title')}
        subtitle={t('configDrift.subtitle')}
        actions={
          <div className="flex gap-2">
            <select
              value={envPair}
              onChange={(e) => setEnvPair(e.target.value)}
              className="rounded border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 text-sm px-2 py-1"
            >
              <option value="staging→production">Staging → Production</option>
              <option value="dev→staging">Dev → Staging</option>
            </select>
            <Button size="sm" onClick={() => refetch()}>
              <RefreshCw size={14} className="mr-1" />
              {t('common.refresh')}
            </Button>
          </div>
        }
      />

      <StatsGrid columns={2}>
        <StatCard title={t('configDrift.driftCount')} value={data?.driftCount ?? 0} icon={<GitBranch size={20} />} color="text-warning" />
        <StatCard title={t('common.critical')} value={data?.criticalCount ?? 0} icon={<GitBranch size={20} />} color="text-critical" />
      </StatsGrid>

      <PageSection title={t('configDrift.drifts')}>
        {drifts.length === 0 ? (
          <p className="text-sm text-gray-500 dark:text-gray-400">{t('configDrift.noDrifts')}</p>
        ) : (
          <div className="grid grid-cols-1 gap-3 sm:grid-cols-2">
            {drifts.map((d) => (
              <Card key={d.id}>
                <CardBody className="p-4">
                  <div className="flex items-start justify-between mb-2">
                    <p className="font-medium text-sm text-gray-900 dark:text-white">{d.service}</p>
                    <Badge variant={SEV_VARIANT[d.severity] ?? 'neutral'}>{d.severity}</Badge>
                  </div>
                  <p className="text-xs text-gray-500 dark:text-gray-400 mb-1">
                    {t('configDrift.environmentPair')}: {d.environmentPair}
                  </p>
                  <p className="text-xs text-gray-500 dark:text-gray-400 mb-1">
                    {t('configDrift.baseline')}: {d.baseline} / {t('configDrift.target')}: {d.target}
                  </p>
                  <p className="text-xs text-gray-500 dark:text-gray-400 mb-3">
                    {t('configDrift.driftedKeys')}: {d.driftedKeys} · {t('configDrift.lastCheck')}: {d.lastCheck}
                  </p>
                  <div className="flex gap-2">
                    <Button size="sm" variant="ghost">
                      <Eye size={12} className="mr-1" />
                      {t('configDrift.viewDiff')}
                    </Button>
                    <Button size="sm" variant="ghost">
                      <Wrench size={12} className="mr-1" />
                      {t('configDrift.remediate')}
                    </Button>
                  </div>
                </CardBody>
              </Card>
            ))}
          </div>
        )}
      </PageSection>
    </PageContainer>
  );
}
