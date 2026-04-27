import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { Calendar, Lock, Unlock, AlertTriangle, CheckCircle2, Clock } from 'lucide-react';
import { PageContainer, PageSection } from '../../../../components/shell';
import { PageHeader } from '../../../../components/PageHeader';
import { Card, CardBody } from '../../../../components/Card';
import { Badge } from '../../../../components/Badge';
import { Button } from '../../../../components/Button';
import { PageLoadingState } from '../../../../components/PageLoadingState';
import client from '../../../../api/client';

interface DeploymentWindow {
  id: string;
  name: string;
  start: string;
  end: string;
  environments: string[];
  status: 'Open' | 'Closed' | 'Freeze';
  requiresApproval: boolean;
}

interface ScheduledRelease {
  id: string;
  serviceName: string;
  version: string;
  targetEnvironment: string;
  scheduledAt: string;
  gateStatus: 'Approved' | 'Pending' | 'Blocked';
  confidence: number;
}

const useReleaseCalendar = () =>
  useQuery({
    queryKey: ['release-calendar-gate'],
    queryFn: () =>
      client
        .get<{ windows: DeploymentWindow[]; releases: ScheduledRelease[]; isSimulated: boolean }>(
          '/api/v1/changes/release-calendar',
          { params: { tenantId: 'default' } }
        )
        .then((r) => r.data),
  });

const GATE_STATUS = {
  Approved: { badge: 'success' as const, icon: <CheckCircle2 size={12} /> },
  Pending: { badge: 'warning' as const, icon: <Clock size={12} /> },
  Blocked: { badge: 'destructive' as const, icon: <AlertTriangle size={12} /> },
};

const WINDOW_STATUS = {
  Open: { badge: 'success' as const, icon: <Unlock size={12} /> },
  Closed: { badge: 'secondary' as const, icon: <Lock size={12} /> },
  Freeze: { badge: 'destructive' as const, icon: <Lock size={12} /> },
};

type Tab = 'calendar' | 'releases';

export function ReleaseCalendarGatePage() {
  const { t } = useTranslation();
  const [tab, setTab] = useState<Tab>('calendar');
  const qc = useQueryClient();
  const { data, isLoading } = useReleaseCalendar();

  const approveRelease = useMutation({
    mutationFn: (id: string) =>
      client.post(`/api/v1/changes/releases/${id}/approve`, { tenantId: 'default', userId: 'current-user' }),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['release-calendar-gate'] }),
  });

  return (
    <PageContainer>
      <PageHeader
        title={t('releaseCalendar.title')}
        subtitle={t('releaseCalendar.subtitle')}
      />
      <PageSection>
        <div className="flex gap-2 mb-4">
          {(['calendar', 'releases'] as Tab[]).map((t_) => (
            <button
              key={t_}
              onClick={() => setTab(t_)}
              className={`px-3 py-1.5 text-xs font-medium rounded-md transition-colors ${
                tab === t_ ? 'bg-accent text-accent-foreground' : 'bg-muted text-muted-foreground hover:bg-muted/80'
              }`}
            >
              {t_ === 'calendar' ? t('releaseCalendar.windowsTab') : t('releaseCalendar.releasesTab')}
            </button>
          ))}
        </div>

        {isLoading ? (
          <PageLoadingState />
        ) : tab === 'calendar' ? (
          <div className="space-y-2">
            {(data?.windows ?? []).map((win) => {
              const cfg = WINDOW_STATUS[win.status] ?? WINDOW_STATUS.Closed;
              return (
                <Card key={win.id}>
                  <CardBody className="p-4">
                    <div className="flex items-center justify-between gap-3 mb-2">
                      <div className="flex items-center gap-2">
                        <Calendar size={14} className="text-muted-foreground" />
                        <span className="text-sm font-semibold">{win.name}</span>
                        {win.requiresApproval && (
                          <Badge variant="secondary" className="text-xs">{t('releaseCalendar.approvalRequired')}</Badge>
                        )}
                      </div>
                      <Badge variant={cfg.badge} className="flex items-center gap-1">
                        {cfg.icon}
                        {win.status}
                      </Badge>
                    </div>
                    <div className="text-xs text-muted-foreground flex gap-4">
                      <span>{new Date(win.start).toLocaleString()}</span>
                      <span>→</span>
                      <span>{new Date(win.end).toLocaleString()}</span>
                    </div>
                    <div className="flex flex-wrap gap-1 mt-2">
                      {win.environments.map((e) => (
                        <Badge key={e} variant="outline" className="text-xs">{e}</Badge>
                      ))}
                    </div>
                  </CardBody>
                </Card>
              );
            })}
          </div>
        ) : (
          <div className="space-y-2">
            {(data?.releases ?? []).map((rel) => {
              const cfg = GATE_STATUS[rel.gateStatus] ?? GATE_STATUS.Pending;
              return (
                <Card key={rel.id}>
                  <CardBody className="p-3">
                    <div className="flex items-center justify-between gap-3">
                      <div className="flex-1 min-w-0">
                        <div className="flex items-center gap-2 mb-1">
                          <span className="text-sm font-medium">{rel.serviceName}</span>
                          <Badge variant="outline" className="text-xs font-mono">{rel.version}</Badge>
                          <Badge variant="secondary" className="text-xs">{rel.targetEnvironment}</Badge>
                        </div>
                        <div className="flex items-center gap-3 text-xs text-muted-foreground">
                          <span>{new Date(rel.scheduledAt).toLocaleString()}</span>
                          <span>{t('releaseCalendar.confidence')}: {rel.confidence}%</span>
                        </div>
                      </div>
                      <div className="flex items-center gap-2">
                        <Badge variant={cfg.badge} className="flex items-center gap-1">
                          {cfg.icon}
                          {rel.gateStatus}
                        </Badge>
                        {rel.gateStatus === 'Pending' && (
                          <Button
                            size="sm"
                            variant="ghost"
                            onClick={() => approveRelease.mutate(rel.id)}
                            disabled={approveRelease.isPending}
                          >
                            {t('releaseCalendar.approve')}
                          </Button>
                        )}
                      </div>
                    </div>
                  </CardBody>
                </Card>
              );
            })}
            {(data?.releases ?? []).length === 0 && (
              <div className="text-center p-8 text-muted-foreground text-sm">
                {t('releaseCalendar.empty')}
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
