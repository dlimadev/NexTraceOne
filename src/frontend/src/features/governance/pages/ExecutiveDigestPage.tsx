import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useQuery } from '@tanstack/react-query';
import { Mail, RefreshCw, Settings, Clock, Wand2 } from 'lucide-react';
import { Card, CardBody } from '../../../components/Card';
import { Badge } from '../../../components/Badge';
import { PageLoadingState } from '../../../components/PageLoadingState';
import { PageErrorState } from '../../../components/PageErrorState';
import { PageContainer, StatsGrid, PageSection } from '../../../components/shell';
import { PageHeader } from '../../../components/PageHeader';
import { Button } from '../../../components/Button';
import { StatCard } from '../../../components/StatCard';
import client from '../../../api/client';

interface DigestSettings {
  frequency: string;
  recipients: string[];
  enabled: boolean;
  lastSent: string;
  nextScheduled: string;
}

interface RecentDigest {
  id: string;
  sentAt: string;
  recipientCount: number;
  summary: string;
}

interface ExecutiveDigestResponse {
  settings: DigestSettings;
  recentDigests: RecentDigest[];
  totalSent: number;
}

const useExecutiveDigest = () =>
  useQuery({
    queryKey: ['executive-digest'],
    queryFn: () =>
      client
        .get<ExecutiveDigestResponse>('/governance/executive-digest')
        .then((r) => r.data),
  });

export function ExecutiveDigestPage() {
  const { t } = useTranslation();
  const [generating, setGenerating] = useState(false);
  const { data, isLoading, isError, refetch } = useExecutiveDigest();

  if (isLoading) return <PageLoadingState message={t('executiveDigest.loading')} />;
  if (isError) return <PageErrorState message={t('executiveDigest.error')} onRetry={() => refetch()} />;

  const handleGenerate = () => {
    setGenerating(true);
    setTimeout(() => setGenerating(false), 2000);
  };

  return (
    <PageContainer>
      <PageHeader
        title={t('executiveDigest.title')}
        subtitle={t('executiveDigest.subtitle')}
        actions={
          <div className="flex gap-2">
            <Button size="sm" variant="ghost">
              <Settings size={14} className="mr-1" />
              {t('executiveDigest.configure')}
            </Button>
            <Button size="sm" onClick={handleGenerate} disabled={generating}>
              <Wand2 size={14} className="mr-1" />
              {generating ? t('executiveDigest.generating') : t('executiveDigest.generateNow')}
            </Button>
          </div>
        }
      />

      <StatsGrid columns={2}>
        <StatCard title={t('common.total')} value={data?.totalSent ?? 0} icon={<Mail size={20} />} color="text-accent" />
        <StatCard title={t('executiveDigest.recipients')} value={data?.settings?.recipients?.length ?? 0} icon={<Mail size={20} />} color="text-info" />
      </StatsGrid>

      {data?.settings && (
        <PageSection title={t('executiveDigest.digestSettings')}>
          <Card>
            <CardBody className="p-4">
              <div className="grid grid-cols-2 gap-4 sm:grid-cols-4">
                <div>
                  <p className="text-xs text-gray-500 dark:text-gray-400">{t('executiveDigest.frequency')}</p>
                  <p className="font-medium text-sm text-gray-900 dark:text-white mt-0.5">
                    {data?.settings?.frequency}
                  </p>
                </div>
                <div>
                  <p className="text-xs text-gray-500 dark:text-gray-400">{t('executiveDigest.recipients')}</p>
                  <p className="font-medium text-sm text-gray-900 dark:text-white mt-0.5">
                    {data?.settings?.recipients?.length}
                  </p>
                </div>
                <div>
                  <p className="text-xs text-gray-500 dark:text-gray-400">{t('executiveDigest.lastSent')}</p>
                  <p className="font-medium text-sm text-gray-900 dark:text-white mt-0.5">
                    {data?.settings?.lastSent}
                  </p>
                </div>
                <div className="flex items-center gap-2">
                  <Badge variant={data?.settings?.enabled ? 'success' : 'neutral'}>
                    {data?.settings?.enabled ? t('executiveDigest.enabled') : t('executiveDigest.disabled')}
                  </Badge>
                  <Button size="sm" variant="ghost" onClick={() => refetch()}>
                    <RefreshCw size={12} />
                  </Button>
                </div>
              </div>
              <div className="mt-3 flex items-center gap-1 text-xs text-gray-400">
                <Clock size={11} />
                {t('executiveDigest.nextScheduled')}: {data?.settings?.nextScheduled}
              </div>
            </CardBody>
          </Card>
        </PageSection>
      )}

      <PageSection title={t('executiveDigest.recentDigests')}>
        {(data?.recentDigests ?? []).length === 0 ? (
          <p className="text-sm text-gray-400">{t('common.noData')}</p>
        ) : (
          <div className="space-y-2">
            {(data?.recentDigests ?? []).map((digest) => (
              <Card key={digest.id}>
                <CardBody className="p-3 flex items-center gap-3">
                  <Mail size={14} className="text-gray-400 flex-shrink-0" />
                  <div className="flex-1 min-w-0">
                    <p className="text-sm text-gray-900 dark:text-white">{digest.summary}</p>
                    <p className="text-xs text-gray-400">{digest.sentAt} · {digest.recipientCount} recipients</p>
                  </div>
                  <Button size="sm" variant="ghost">{t('executiveDigest.previewDigest')}</Button>
                </CardBody>
              </Card>
            ))}
          </div>
        )}
      </PageSection>
    </PageContainer>
  );
}
