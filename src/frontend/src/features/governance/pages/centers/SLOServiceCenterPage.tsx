import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useQuery } from '@tanstack/react-query';
import { Activity, Zap, BookOpen, AlertTriangle, CheckCircle2, TrendingDown } from 'lucide-react';
import { PageContainer, PageSection } from '../../../../components/shell';
import { PageHeader } from '../../../../components/PageHeader';
import { Card, CardBody } from '../../../../components/Card';
import { Badge } from '../../../../components/Badge';
import { PageLoadingState } from '../../../../components/PageLoadingState';
import client from '../../../../api/client';

type CenterTab = 'slo' | 'chaos' | 'postmortem';

interface SLORecord {
  id: string;
  serviceName: string;
  sloName: string;
  target: number;
  current: number;
  errorBudgetRemaining: number;
  status: 'Healthy' | 'AtRisk' | 'Breached';
  window: string;
}

interface ChaosExperiment {
  id: string;
  name: string;
  targetService: string;
  type: string;
  lastRun: string;
  result: 'Passed' | 'Failed' | 'Running' | 'NotRun';
  hypothesis: string;
}

interface PostmortemRecord {
  id: string;
  title: string;
  serviceName: string;
  severity: string;
  occurredAt: string;
  status: 'Draft' | 'InReview' | 'Published';
  actionItems: number;
  openItems: number;
}

const useSLOCenter = () =>
  useQuery({
    queryKey: ['slo-service-center'],
    queryFn: () =>
      client
        .get<{
          slos: SLORecord[];
          chaos: ChaosExperiment[];
          postmortems: PostmortemRecord[];
          isSimulated: boolean;
        }>('/api/v1/operations/slo-center', { params: { tenantId: 'default' } })
        .then((r) => r.data),
  });

const SLO_STATUS = {
  Healthy: { badge: 'success' as const, color: 'text-success', icon: <CheckCircle2 size={12} /> },
  AtRisk: { badge: 'warning' as const, color: 'text-warning', icon: <AlertTriangle size={12} /> },
  Breached: { badge: 'destructive' as const, color: 'text-destructive', icon: <TrendingDown size={12} /> },
};

const CHAOS_RESULT = {
  Passed: { badge: 'success' as const },
  Failed: { badge: 'destructive' as const },
  Running: { badge: 'warning' as const },
  NotRun: { badge: 'secondary' as const },
};

export function SLOServiceCenterPage() {
  const { t } = useTranslation();
  const [tab, setTab] = useState<CenterTab>('slo');
  const { data, isLoading } = useSLOCenter();

  const tabs: { key: CenterTab; label: string; icon: React.ReactNode }[] = [
    { key: 'slo', label: t('sloCenter.tabSLO'), icon: <Activity size={12} /> },
    { key: 'chaos', label: t('sloCenter.tabChaos'), icon: <Zap size={12} /> },
    { key: 'postmortem', label: t('sloCenter.tabPostmortem'), icon: <BookOpen size={12} /> },
  ];

  return (
    <PageContainer>
      <PageHeader
        title={t('sloCenter.title')}
        subtitle={t('sloCenter.subtitle')}
      />
      <PageSection>
        <div className="flex gap-2 mb-4">
          {tabs.map((tb) => (
            <button
              key={tb.key}
              onClick={() => setTab(tb.key)}
              className={`flex items-center gap-1.5 px-3 py-1.5 text-xs font-medium rounded-md transition-colors ${
                tab === tb.key ? 'bg-accent text-accent-foreground' : 'bg-muted text-muted-foreground hover:bg-muted/80'
              }`}
            >
              {tb.icon}
              {tb.label}
            </button>
          ))}
        </div>

        {isLoading ? (
          <PageLoadingState />
        ) : tab === 'slo' ? (
          <div className="space-y-2">
            {(data?.slos ?? []).map((slo) => {
              const cfg = SLO_STATUS[slo.status] ?? SLO_STATUS.Healthy;
              const budgetColor = slo.errorBudgetRemaining > 50 ? 'bg-success' : slo.errorBudgetRemaining > 20 ? 'bg-warning' : 'bg-destructive';
              return (
                <Card key={slo.id}>
                  <CardBody className="p-4">
                    <div className="flex items-center justify-between gap-3 mb-3">
                      <div>
                        <div className="flex items-center gap-2">
                          <span className={cfg.color}>{cfg.icon}</span>
                          <span className="text-sm font-semibold">{slo.serviceName}</span>
                          <span className="text-xs text-muted-foreground">— {slo.sloName}</span>
                        </div>
                        <p className="text-xs text-muted-foreground mt-0.5">{slo.window}</p>
                      </div>
                      <Badge variant={cfg.badge}>{slo.status}</Badge>
                    </div>
                    <div className="grid grid-cols-3 gap-4 text-center text-xs">
                      <div>
                        <p className="text-muted-foreground mb-0.5">{t('sloCenter.target')}</p>
                        <p className="font-bold">{slo.target}%</p>
                      </div>
                      <div>
                        <p className="text-muted-foreground mb-0.5">{t('sloCenter.current')}</p>
                        <p className={`font-bold ${cfg.color}`}>{slo.current}%</p>
                      </div>
                      <div>
                        <p className="text-muted-foreground mb-0.5">{t('sloCenter.errorBudget')}</p>
                        <p className="font-bold">{slo.errorBudgetRemaining}%</p>
                      </div>
                    </div>
                    <div className="mt-2 w-full bg-muted rounded-full h-1.5">
                      <div className={`h-1.5 rounded-full ${budgetColor}`} style={{ width: `${slo.errorBudgetRemaining}%` }} />
                    </div>
                  </CardBody>
                </Card>
              );
            })}
            {(data?.slos ?? []).length === 0 && (
              <div className="text-center p-8 text-muted-foreground text-sm">{t('sloCenter.empty')}</div>
            )}
          </div>
        ) : tab === 'chaos' ? (
          <div className="space-y-2">
            {(data?.chaos ?? []).map((exp) => {
              const cfg = CHAOS_RESULT[exp.result] ?? CHAOS_RESULT.NotRun;
              return (
                <Card key={exp.id}>
                  <CardBody className="p-3">
                    <div className="flex items-start justify-between gap-3">
                      <div className="flex-1 min-w-0">
                        <div className="flex items-center gap-2 mb-1">
                          <Zap size={12} className="text-muted-foreground" />
                          <span className="text-sm font-medium">{exp.name}</span>
                          <Badge variant="outline" className="text-xs">{exp.targetService}</Badge>
                          <Badge variant="secondary" className="text-xs">{exp.type}</Badge>
                        </div>
                        <p className="text-xs text-muted-foreground italic">{exp.hypothesis}</p>
                        <p className="text-xs text-muted-foreground mt-0.5">
                          {t('sloCenter.lastRun')}: {exp.lastRun !== 'never' ? new Date(exp.lastRun).toLocaleString() : t('sloCenter.never')}
                        </p>
                      </div>
                      <Badge variant={cfg.badge}>{exp.result}</Badge>
                    </div>
                  </CardBody>
                </Card>
              );
            })}
          </div>
        ) : (
          <div className="space-y-2">
            {(data?.postmortems ?? []).map((pm) => (
              <Card key={pm.id}>
                <CardBody className="p-3">
                  <div className="flex items-start justify-between gap-3">
                    <div className="flex-1 min-w-0">
                      <div className="flex items-center gap-2 mb-1">
                        <BookOpen size={12} className="text-muted-foreground" />
                        <span className="text-sm font-medium truncate">{pm.title}</span>
                        <Badge variant="secondary" className="text-xs">{pm.severity}</Badge>
                      </div>
                      <p className="text-xs text-muted-foreground">{pm.serviceName} · {new Date(pm.occurredAt).toLocaleDateString()}</p>
                      <p className="text-xs text-muted-foreground mt-0.5">
                        {t('sloCenter.actionItems')}: {pm.openItems}/{pm.actionItems} {t('sloCenter.open')}
                      </p>
                    </div>
                    <Badge variant={pm.status === 'Published' ? 'success' : pm.status === 'InReview' ? 'warning' : 'secondary'}>
                      {pm.status}
                    </Badge>
                  </div>
                </CardBody>
              </Card>
            ))}
          </div>
        )}

        <div className="mt-4 p-3 rounded-lg bg-muted/40 text-xs text-muted-foreground">
          {t('sotCenter.simulatedBanner')}
        </div>
      </PageSection>
    </PageContainer>
  );
}
