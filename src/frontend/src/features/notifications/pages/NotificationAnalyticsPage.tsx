import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useNavigate } from 'react-router-dom';
import {
  BarChart3,
  Bell,
  Clock3,
  Mail,
  Settings,
  ShieldAlert,
  Siren,
  Target,
} from 'lucide-react';
import { Button } from '../../../components/Button';
import { Card, CardBody, CardHeader } from '../../../components/Card';
import { PageErrorState } from '../../../components/PageErrorState';
import { PageHeader } from '../../../components/PageHeader';
import { PageLoadingState } from '../../../components/PageLoadingState';
import { StatCard } from '../../../components/StatCard';
import { PageContainer, PageSection, StatsGrid } from '../../../components/shell';
import { useNotificationAnalytics } from '../hooks/useNotificationConfiguration';
import type { NotificationTypeCountDto } from '../types';

function formatPercent(value: number) {
  return `${(value * 100).toFixed(1)}%`;
}

function formatDuration(minutes: number) {
  if (minutes <= 0) {
    return '0m';
  }

  if (minutes >= 60) {
    return `${(minutes / 60).toFixed(1)}h`;
  }

  return `${minutes.toFixed(1)}m`;
}

function renderMetricList(
  entries: Array<[string, number]>,
  emptyLabel: string,
) {
  if (entries.length === 0) {
    return <p className="text-sm text-muted">{emptyLabel}</p>;
  }

  return (
    <div className="space-y-3">
      {entries.map(([label, value]) => (
        <div key={label} className="flex items-center justify-between gap-4">
          <span className="truncate text-sm text-body">{label}</span>
          <span className="shrink-0 text-sm font-semibold text-heading tabular-nums">{value}</span>
        </div>
      ))}
    </div>
  );
}

function renderTypeList(items: NotificationTypeCountDto[], emptyLabel: string) {
  if (items.length === 0) {
    return <p className="text-sm text-muted">{emptyLabel}</p>;
  }

  return (
    <div className="space-y-3">
      {items.map((item) => (
        <div key={item.eventType} className="flex items-center justify-between gap-4">
          <span className="truncate text-sm text-body">{item.eventType}</span>
          <span className="shrink-0 text-sm font-semibold text-heading tabular-nums">{item.count}</span>
        </div>
      ))}
    </div>
  );
}

export function NotificationAnalyticsPage() {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const [days, setDays] = useState(30);

  const { data, isLoading, isError, refetch } = useNotificationAnalytics(days);

  if (isLoading) {
    return (
      <PageContainer>
        <PageLoadingState message={t('common.loading')} />
      </PageContainer>
    );
  }

  if (isError || !data) {
    return (
      <PageContainer>
        <PageErrorState
          action={
            <Button variant="secondary" size="sm" onClick={() => refetch()}>
              {t('common.retry')}
            </Button>
          }
        />
      </PageContainer>
    );
  }

  const deliveryAttempts =
    data.platform.totalDelivered +
    data.platform.totalFailed +
    data.platform.totalPending +
    data.platform.totalSkipped;

  const deliverySuccessRate = deliveryAttempts > 0
    ? data.platform.totalDelivered / deliveryAttempts
    : 0;

  const categoryEntries = Object.entries(data.platform.byCategory)
    .sort((left, right) => right[1] - left[1]);

  const severityEntries = Object.entries(data.platform.bySeverity)
    .sort((left, right) => right[1] - left[1]);

  const channelEntries = Object.entries(data.platform.deliveriesByChannel)
    .sort((left, right) => right[1] - left[1]);

  const sourceEntries = Object.entries(data.platform.bySourceModule)
    .sort((left, right) => right[1] - left[1]);

  const ranges = [
    { days: 7, label: t('notifications.analytics.range.last7Days') },
    { days: 30, label: t('notifications.analytics.range.last30Days') },
    { days: 90, label: t('notifications.analytics.range.last90Days') },
  ];

  return (
    <PageContainer>
      <PageHeader
        title={t('notifications.analytics.title')}
        subtitle={t('notifications.analytics.subtitle')}
        actions={
          <div className="flex flex-wrap items-center gap-2">
            <Button variant="ghost" size="sm" onClick={() => navigate('/notifications')}>
              <Bell className="h-4 w-4 mr-1.5" />
              {t('notifications.analytics.backToInbox')}
            </Button>
            <Button variant="secondary" size="sm" onClick={() => navigate('/platform/configuration/notifications')}>
              <Settings className="h-4 w-4 mr-1.5" />
              {t('notifications.analytics.openConfiguration')}
            </Button>
          </div>
        }
      >
        <div className="mt-4 flex flex-wrap gap-2">
          {ranges.map((range) => (
            <Button
              key={range.days}
              variant={days === range.days ? 'secondary' : 'ghost'}
              size="sm"
              onClick={() => setDays(range.days)}
            >
              {range.label}
            </Button>
          ))}
        </div>
      </PageHeader>

      <PageSection>
        <StatsGrid columns={3}>
          <StatCard
            title={t('notifications.analytics.generated')}
            value={data.platform.totalGenerated}
            icon={<BarChart3 size={20} />}
            color="text-accent"
          />
          <StatCard
            title={t('notifications.analytics.deliverySuccessRate')}
            value={formatPercent(deliverySuccessRate)}
            icon={<Mail size={20} />}
            color="text-info"
          />
          <StatCard
            title={t('notifications.analytics.readRate')}
            value={formatPercent(data.interaction.readRate)}
            icon={<Target size={20} />}
            color="text-success"
          />
          <StatCard
            title={t('notifications.analytics.avgReadTime')}
            value={formatDuration(data.interaction.averageTimeToReadMinutes)}
            icon={<Clock3 size={20} />}
            color="text-warning"
          />
          <StatCard
            title={t('notifications.analytics.avgAcknowledgeTime')}
            value={formatDuration(data.interaction.averageTimeToAcknowledgeMinutes)}
            icon={<Siren size={20} />}
            color="text-critical"
          />
          <StatCard
            title={t('notifications.analytics.unacknowledgedActionItems')}
            value={data.interaction.totalUnacknowledgedActionRequired}
            icon={<ShieldAlert size={20} />}
            color="text-critical"
          />
        </StatsGrid>
      </PageSection>

      <PageSection>
        <StatsGrid columns={4}>
          <StatCard
            title={t('notifications.analytics.pendingDeliveries')}
            value={data.platform.totalPending}
            icon={<Mail size={20} />}
            color="text-warning"
          />
          <StatCard
            title={t('notifications.analytics.suppressed')}
            value={data.quality.totalSuppressed}
            icon={<ShieldAlert size={20} />}
            color="text-muted"
          />
          <StatCard
            title={t('notifications.analytics.grouped')}
            value={data.quality.totalGrouped}
            icon={<BarChart3 size={20} />}
            color="text-info"
          />
          <StatCard
            title={t('notifications.analytics.avgPerUserPerDay')}
            value={data.quality.averagePerUserPerDay.toFixed(2)}
            icon={<Clock3 size={20} />}
            color="text-accent"
            context={t('notifications.analytics.correlatedIncidents') + `: ${data.quality.totalCorrelatedWithIncidents}`}
          />
        </StatsGrid>
      </PageSection>

      <PageSection>
        <div className="grid grid-cols-1 gap-4 xl:grid-cols-2">
          <Card>
            <CardHeader>
              <span className="font-semibold text-heading">{t('notifications.analytics.byCategory')}</span>
            </CardHeader>
            <CardBody>
              {renderMetricList(categoryEntries, t('notifications.analytics.none'))}
            </CardBody>
          </Card>

          <Card>
            <CardHeader>
              <span className="font-semibold text-heading">{t('notifications.analytics.bySeverity')}</span>
            </CardHeader>
            <CardBody>
              {renderMetricList(severityEntries, t('notifications.analytics.none'))}
            </CardBody>
          </Card>

          <Card>
            <CardHeader>
              <span className="font-semibold text-heading">{t('notifications.analytics.byChannel')}</span>
            </CardHeader>
            <CardBody>
              {renderMetricList(channelEntries, t('notifications.analytics.none'))}
            </CardBody>
          </Card>

          <Card>
            <CardHeader>
              <span className="font-semibold text-heading">{t('notifications.analytics.bySourceModule')}</span>
            </CardHeader>
            <CardBody>
              {renderMetricList(sourceEntries, t('notifications.analytics.none'))}
            </CardBody>
          </Card>
        </div>
      </PageSection>

      <PageSection>
        <div className="grid grid-cols-1 gap-4 xl:grid-cols-3">
          <Card>
            <CardHeader>
              <span className="font-semibold text-heading">{t('notifications.analytics.topNoisyTypes')}</span>
            </CardHeader>
            <CardBody>
              {renderTypeList(data.quality.topNoisyTypes, t('notifications.analytics.none'))}
            </CardBody>
          </Card>

          <Card>
            <CardHeader>
              <span className="font-semibold text-heading">{t('notifications.analytics.leastEngagedTypes')}</span>
            </CardHeader>
            <CardBody>
              {renderTypeList(data.quality.leastEngagedTypes, t('notifications.analytics.none'))}
            </CardBody>
          </Card>

          <Card>
            <CardHeader>
              <span className="font-semibold text-heading">{t('notifications.analytics.unacknowledgedByType')}</span>
            </CardHeader>
            <CardBody>
              {renderTypeList(data.quality.unacknowledgedActionTypes, t('notifications.analytics.none'))}
            </CardBody>
          </Card>
        </div>
      </PageSection>
    </PageContainer>
  );
}
