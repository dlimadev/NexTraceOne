import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useQuery } from '@tanstack/react-query';
import { CheckSquare, Download, Copy, Wand2, FlaskConical } from 'lucide-react';
import { Card, CardBody } from '../../../components/Card';
import { Badge } from '../../../components/Badge';
import { PageLoadingState } from '../../../components/PageLoadingState';
import { PageErrorState } from '../../../components/PageErrorState';
import { PageContainer, StatsGrid, PageSection } from '../../../components/shell';
import { PageHeader } from '../../../components/PageHeader';
import { Button } from '../../../components/Button';
import { StatCard } from '../../../components/StatCard';
import client from '../../../api/client';

interface TestScenario {
  id: string;
  name: string;
  type: 'happyPath' | 'edgeCase' | 'errorCase' | 'invalidPayload';
  code: string;
}

interface TestScenariosResponse {
  scenarios: TestScenario[];
  scenarioCount: number;
  contractType: string;
}

const useTestScenarios = (contractId: string, enabled: boolean) =>
  useQuery({
    queryKey: ['ai-test-scenarios', contractId],
    queryFn: () =>
      client
        .post<TestScenariosResponse>('/ai/test-scenarios', { contractId })
        .then((r) => r.data),
    enabled,
  });

const TYPE_VARIANT: Record<string, 'success' | 'warning' | 'danger' | 'neutral'> = {
  happyPath: 'success',
  edgeCase: 'warning',
  errorCase: 'danger',
  invalidPayload: 'neutral',
};

export function AiTestScenarioGeneratorPage() {
  const { t } = useTranslation();
  const [contractId, setContractId] = useState('');
  const [generate, setGenerate] = useState(false);
  const { data, isLoading, isError, refetch } = useTestScenarios(contractId, generate && !!contractId);

  if (isLoading && generate) return <PageLoadingState message={t('aiTestScenario.generating')} />;
  if (isError && generate) return <PageErrorState message={t('aiTestScenario.error')} onRetry={() => refetch()} />;

  return (
    <PageContainer>
      <PageHeader
        title={t('aiTestScenario.title')}
        subtitle={t('aiTestScenario.subtitle')}
        actions={
          <div className="flex gap-2">
            <input
              type="text"
              value={contractId}
              onChange={(e) => { setContractId(e.target.value); setGenerate(false); }}
              placeholder={t('aiTestScenario.selectContract')}
              className="rounded border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 text-sm px-2 py-1 w-56"
            />
            <Button size="sm" onClick={() => setGenerate(true)} disabled={!contractId}>
              <Wand2 size={14} className="mr-1" />
              {t('aiTestScenario.generate')}
            </Button>
          </div>
        }
      />

      {data && (
        <>
          <StatsGrid columns={2}>
            <StatCard title={t('aiTestScenario.scenarioCount')} value={data.scenarioCount} icon={<CheckSquare size={20} />} color="text-accent" />
            <StatCard title={t('aiTestScenario.contractType')} value={data.contractType} icon={<FlaskConical size={20} />} color="text-info" />
          </StatsGrid>

          <div className="flex gap-2 mb-4">
            <Button size="sm" variant="ghost">
              <Download size={14} className="mr-1" />
              {t('aiTestScenario.exportPostman')}
            </Button>
            <Button size="sm" variant="ghost">
              <Download size={14} className="mr-1" />
              {t('aiTestScenario.exportBruno')}
            </Button>
          </div>

          <PageSection title={t('aiTestScenario.scenarios')}>
            <div className="space-y-3">
              {data.scenarios.map((sc) => (
                <Card key={sc.id}>
                  <CardBody className="p-4">
                    <div className="flex items-start justify-between mb-2">
                      <div className="flex items-center gap-2">
                        <FlaskConical size={14} className="text-indigo-500" />
                        <p className="font-medium text-sm text-gray-900 dark:text-white">{sc.name}</p>
                        <Badge variant={TYPE_VARIANT[sc.type] ?? 'neutral'}>
                          {t(`aiTestScenario.${sc.type}`)}
                        </Badge>
                      </div>
                      <Button size="sm" variant="ghost">
                        <Copy size={12} className="mr-1" />
                        {t('aiTestScenario.copyCode')}
                      </Button>
                    </div>
                    <pre className="text-xs font-mono bg-gray-50 dark:bg-gray-900 rounded p-2 overflow-x-auto text-gray-700 dark:text-gray-300">
                      {sc.code}
                    </pre>
                  </CardBody>
                </Card>
              ))}
            </div>
          </PageSection>
        </>
      )}

      {!data && !isLoading && (
        <p className="text-sm text-center text-gray-400 py-8">{t('aiTestScenario.noScenarios')}</p>
      )}
    </PageContainer>
  );
}
