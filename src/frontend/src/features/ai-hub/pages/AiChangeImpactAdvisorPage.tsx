import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useQuery } from '@tanstack/react-query';
import { Zap, ShieldCheck, AlertTriangle, XCircle, ClipboardList } from 'lucide-react';
import { Card, CardBody } from '../../../components/Card';
import { Badge } from '../../../components/Badge';
import { PageLoadingState } from '../../../components/PageLoadingState';
import { PageErrorState } from '../../../components/PageErrorState';
import { PageContainer, StatsGrid, PageSection } from '../../../components/shell';
import { PageHeader } from '../../../components/PageHeader';
import { Button } from '../../../components/Button';
import { StatCard } from '../../../components/StatCard';
import client from '../../../api/client';

interface ChangeImpactResult {
  changeId: string;
  environment: string;
  recommendation: 'promoteConfident' | 'promoteCaution' | 'reviewFirst';
  riskFactors: string[];
  contractsAffected: number;
  blastRadius: string;
  incidentHistory: number;
  auditNote: string;
}

const useChangeImpact = (changeId: string, environment: string, enabled: boolean) =>
  useQuery({
    queryKey: ['ai-change-impact', changeId, environment],
    queryFn: () =>
      client
        .get<ChangeImpactResult>('/ai/change-impact', {
          params: { changeId, environment },
        })
        .then((r) => r.data),
    enabled,
  });

const REC_VARIANT: Record<string, 'success' | 'warning' | 'danger'> = {
  promoteConfident: 'success',
  promoteCaution: 'warning',
  reviewFirst: 'danger',
};

const REC_ICON: Record<string, React.ReactNode> = {
  promoteConfident: <ShieldCheck size={16} className="text-green-500" />,
  promoteCaution: <AlertTriangle size={16} className="text-amber-500" />,
  reviewFirst: <XCircle size={16} className="text-red-500" />,
};

export function AiChangeImpactAdvisorPage() {
  const { t } = useTranslation();
  const [changeId, setChangeId] = useState('');
  const [environment, setEnvironment] = useState('production');
  const [analyze, setAnalyze] = useState(false);
  const { data, isLoading, isError, refetch } = useChangeImpact(changeId, environment, analyze && !!changeId);

  const handleAnalyze = () => {
    if (!changeId) return;
    setAnalyze(true);
  };

  if (isLoading && analyze) return <PageLoadingState message={t('aiChangeImpact.analyzing')} />;
  if (isError && analyze) return <PageErrorState message={t('aiChangeImpact.error')} onRetry={() => refetch()} />;

  return (
    <PageContainer>
      <PageHeader
        title={t('aiChangeImpact.title')}
        subtitle={t('aiChangeImpact.subtitle')}
      />

      <Card className="mb-4">
        <CardBody className="p-4">
          <div className="flex flex-wrap gap-3">
            <input
              type="text"
              value={changeId}
              onChange={(e) => { setChangeId(e.target.value); setAnalyze(false); }}
              placeholder={t('aiChangeImpact.changeId')}
              className="rounded border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 text-sm px-2 py-1 flex-1 min-w-40"
            />
            <select
              value={environment}
              onChange={(e) => setEnvironment(e.target.value)}
              className="rounded border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 text-sm px-2 py-1"
            >
              <option value="production">Production</option>
              <option value="staging">Staging</option>
            </select>
            <Button size="sm" onClick={handleAnalyze} disabled={!changeId}>
              <Zap size={14} className="mr-1" />
              {t('aiChangeImpact.analyze')}
            </Button>
          </div>
        </CardBody>
      </Card>

      {data && (
        <>
          <StatsGrid columns={2}>
            <StatCard title={t('aiChangeImpact.contractsAffected')} value={data.contractsAffected} icon={<Zap size={20} />} color="text-warning" />
            <StatCard title={t('aiChangeImpact.incidentHistory')} value={data.incidentHistory} icon={<AlertTriangle size={20} />} color="text-critical" />
          </StatsGrid>

          <PageSection title={t('aiChangeImpact.recommendation')}>
            <Card>
              <CardBody className="p-4">
                <div className="flex items-center gap-2 mb-3">
                  {REC_ICON[data.recommendation]}
                  <Badge variant={REC_VARIANT[data.recommendation]}>
                    {t(`aiChangeImpact.${data.recommendation}`)}
                  </Badge>
                </div>
                <p className="text-xs text-gray-500 dark:text-gray-400 mb-2">
                  {t('aiChangeImpact.blastRadius')}: {data.blastRadius}
                </p>
                <div className="mb-2">
                  <p className="text-xs font-medium text-gray-700 dark:text-gray-300 mb-1">
                    {t('aiChangeImpact.riskFactors')}:
                  </p>
                  <ul className="list-disc list-inside space-y-0.5">
                    {data.riskFactors.map((rf, i) => (
                      <li key={i} className="text-xs text-gray-500 dark:text-gray-400">{rf}</li>
                    ))}
                  </ul>
                </div>
                <div className="flex items-start gap-2 mt-3 p-2 bg-gray-50 dark:bg-gray-800 rounded text-xs text-gray-500">
                  <ClipboardList size={12} className="mt-0.5 flex-shrink-0" />
                  <span>{t('aiChangeImpact.auditNote')}: {data.auditNote}</span>
                </div>
              </CardBody>
            </Card>
          </PageSection>
        </>
      )}

      {!data && !isLoading && (
        <p className="text-sm text-gray-500 dark:text-gray-400 text-center py-8">{t('aiChangeImpact.noResults')}</p>
      )}
    </PageContainer>
  );
}
