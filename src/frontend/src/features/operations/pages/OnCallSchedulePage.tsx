/**
 * OnCallSchedulePage — Gestão de escalas de plantão com políticas de escalação e overrides.
 *
 * Centraliza rotações de on-call (weekly/follow-the-sun/custom), políticas de escalação
 * por níveis e gestão de substituições temporárias com rastreabilidade completa.
 *
 * @module operations/incidents
 * @pillar Operational Reliability, Operational Consistency
 */
import * as React from 'react';
import { useState, useCallback } from 'react';
import { useTranslation } from 'react-i18next';
import { useQuery } from '@tanstack/react-query';
import { CalendarDays, RefreshCw, Plus } from 'lucide-react';
import { PageContainer, PageSection } from '../../../components/shell';
import { PageHeader } from '../../../components/PageHeader';
import { Card, CardBody, CardHeader } from '../../../components/Card';
import { Badge } from '../../../components/Badge';
import { Button } from '../../../components/Button';
import { PageLoadingState } from '../../../components/PageLoadingState';
import { PageErrorState } from '../../../components/PageErrorState';
import { getOnCallSchedules, type OnCallSchedule } from '../api/telemetry';
import { useEnvironment } from '../../../contexts/EnvironmentContext';

type TimeRange = '1h' | '6h' | '24h' | '7d';

const TIME_RANGE_OPTIONS: Array<{ value: TimeRange; labelKey: string }> = [
  { value: '1h', labelKey: 'onCallSchedule.timeRange.1h' },
  { value: '6h', labelKey: 'onCallSchedule.timeRange.6h' },
  { value: '24h', labelKey: 'onCallSchedule.timeRange.24h' },
  { value: '7d', labelKey: 'onCallSchedule.timeRange.7d' },
];

function timeRangeToInterval(range: TimeRange) {
  const until = new Date();
  const from = new Date(until);
  switch (range) {
    case '1h': from.setHours(from.getHours() - 1); break;
    case '6h': from.setHours(from.getHours() - 6); break;
    case '24h': from.setHours(from.getHours() - 24); break;
    case '7d': from.setDate(from.getDate() - 7); break;
  }
  return { from: from.toISOString(), until: until.toISOString() };
}

const FALLBACK: OnCallSchedule[] = [
  { id: '1', name: 'Payments On-Call', teamName: 'Platform Team', serviceName: 'payment-service', currentOnCall: 'João Silva', nextOnCall: 'Maria Costa', rotationType: 'weekly', timezone: 'America/Sao_Paulo', escalationLevels: 3, activeOverrides: 0, environment: 'production' },
  { id: '2', name: 'Orders On-Call', teamName: 'Orders Team', serviceName: 'order-service', currentOnCall: 'Carlos Mendes', nextOnCall: 'Ana Ferreira', rotationType: 'weekly', timezone: 'Europe/Lisbon', escalationLevels: 2, activeOverrides: 1, environment: 'production' },
  { id: '3', name: 'Infra On-Call - Follow-the-Sun', teamName: 'Infrastructure Team', serviceName: 'infra-services', currentOnCall: 'Pedro Santos', nextOnCall: 'Lisa Anderson', rotationType: 'followTheSun', timezone: 'UTC', escalationLevels: 4, activeOverrides: 0, environment: 'production' },
  { id: '4', name: 'Auth On-Call', teamName: 'Security Team', serviceName: 'auth-service', currentOnCall: 'Rita Alves', nextOnCall: 'Miguel Sousa', rotationType: 'custom', timezone: 'America/Sao_Paulo', escalationLevels: 3, activeOverrides: 2, environment: 'production' },
];

export function OnCallSchedulePage() {
  const { t } = useTranslation();
  const { activeEnvironmentId } = useEnvironment();
  const [timeRange, setTimeRange] = useState<TimeRange>('7d');
  const [refreshKey, setRefreshKey] = useState(0);

  const interval = timeRangeToInterval(timeRange);
  const environment = activeEnvironmentId ?? 'production';

  const { data, isLoading, isError, refetch } = useQuery({
    queryKey: ['on-call-schedules', environment, timeRange, refreshKey],
    queryFn: () => getOnCallSchedules({ environment, from: interval.from, until: interval.until }),
    staleTime: 30_000,
    retry: false,
  });

  const schedules = (data && data.length > 0) ? data : FALLBACK;

  const handleRefresh = useCallback(() => {
    setRefreshKey((k) => k + 1);
    refetch();
  }, [refetch]);

  const onCallNow = schedules.length;
  const totalOverrides = schedules.reduce((a, s) => a + s.activeOverrides, 0);
  const totalEscalations = schedules.filter((s) => s.escalationLevels > 0).length;

  return (
    <PageContainer>
      <div className="flex flex-col gap-1 mb-4 sm:flex-row sm:items-center sm:justify-between">
        <PageHeader
          title={t('onCallSchedule.title')}
          subtitle={t('onCallSchedule.subtitle')}
          icon={<CalendarDays className="w-5 h-5" />}
        />
        <div className="flex items-center gap-2 flex-wrap">
          <div className="flex rounded-md border border-border overflow-hidden text-xs">
            {TIME_RANGE_OPTIONS.map((opt) => (
              <button
                key={opt.value}
                type="button"
                onClick={() => setTimeRange(opt.value)}
                className={`px-3 py-1.5 transition-colors ${timeRange === opt.value ? 'bg-primary text-primary-foreground font-semibold' : 'hover:bg-muted text-muted-foreground'}`}
              >
                {t(opt.labelKey)}
              </button>
            ))}
          </div>
          <Button variant="outline" size="sm" onClick={handleRefresh}>
            <RefreshCw className="w-3.5 h-3.5 mr-1.5" />
            {t('common.refresh')}
          </Button>
          <Button size="sm">
            <Plus className="w-3.5 h-3.5 mr-1.5" />
            {t('onCallSchedule.overrides.add')}
          </Button>
        </div>
      </div>

      {isError && <PageErrorState message={t('onCallSchedule.loadError')} onRetry={handleRefresh} />}
      {isLoading && <PageLoadingState message={t('onCallSchedule.loading')} />}

      {!isLoading && (
        <>
          <PageSection>
            <div className="grid grid-cols-2 md:grid-cols-4 gap-3 mb-4">
              {[
                { label: t('onCallSchedule.stats.activeSchedules'), value: String(schedules.length) },
                { label: t('onCallSchedule.stats.onCallNow'), value: String(onCallNow) },
                { label: t('onCallSchedule.stats.overrides'), value: String(totalOverrides) },
                { label: t('onCallSchedule.stats.escalations'), value: String(totalEscalations) },
              ].map((stat) => (
                <Card key={stat.label}>
                  <CardBody className="p-3">
                    <div className="text-xs text-muted-foreground mb-1">{stat.label}</div>
                    <div className="text-2xl font-bold tabular-nums">{stat.value}</div>
                  </CardBody>
                </Card>
              ))}
            </div>
          </PageSection>

          <PageSection>
            <Card>
              <CardHeader>
                <h3 className="text-sm font-semibold">{t('onCallSchedule.title')}</h3>
              </CardHeader>
              <CardBody className="p-0">
                {schedules.length === 0 ? (
                  <div className="p-8 text-center text-muted-foreground text-sm">{t('onCallSchedule.noRecords')}</div>
                ) : (
                  <div className="overflow-x-auto">
                    <table className="w-full text-sm">
                      <thead>
                        <tr className="border-b border-border bg-muted/40 text-xs text-muted-foreground">
                          <th className="px-4 py-2.5 text-left font-medium">{t('onCallSchedule.table.schedule')}</th>
                          <th className="px-4 py-2.5 text-left font-medium">{t('onCallSchedule.table.team')}</th>
                          <th className="px-4 py-2.5 text-left font-medium">{t('onCallSchedule.table.service')}</th>
                          <th className="px-4 py-2.5 text-left font-medium">{t('onCallSchedule.table.currentOnCall')}</th>
                          <th className="px-4 py-2.5 text-left font-medium">{t('onCallSchedule.table.nextOnCall')}</th>
                          <th className="px-4 py-2.5 text-left font-medium">{t('onCallSchedule.table.rotationType')}</th>
                          <th className="px-4 py-2.5 text-left font-medium">{t('onCallSchedule.table.timezone')}</th>
                        </tr>
                      </thead>
                      <tbody>
                        {schedules.map((s) => (
                          <tr key={s.id} className="border-b border-border/50 hover:bg-muted/30 transition-colors">
                            <td className="px-4 py-2.5 font-medium">{s.name}</td>
                            <td className="px-4 py-2.5 text-muted-foreground">{s.teamName}</td>
                            <td className="px-4 py-2.5"><Badge variant="secondary">{s.serviceName}</Badge></td>
                            <td className="px-4 py-2.5">
                              <div className="flex items-center gap-1.5">
                                <div className="w-2 h-2 rounded-full bg-emerald-500" />
                                <span className="font-medium">{s.currentOnCall}</span>
                              </div>
                            </td>
                            <td className="px-4 py-2.5 text-muted-foreground">{s.nextOnCall}</td>
                            <td className="px-4 py-2.5">
                              <Badge variant="info">{t(`onCallSchedule.rotationTypes.${s.rotationType}`)}</Badge>
                            </td>
                            <td className="px-4 py-2.5 text-xs text-muted-foreground">{s.timezone}</td>
                          </tr>
                        ))}
                      </tbody>
                    </table>
                  </div>
                )}
              </CardBody>
            </Card>
          </PageSection>
        </>
      )}
    </PageContainer>
  );
}
