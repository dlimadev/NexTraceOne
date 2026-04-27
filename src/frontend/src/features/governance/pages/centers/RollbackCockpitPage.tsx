import { useTranslation } from 'react-i18next';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { RotateCcw, AlertTriangle, CheckCircle2, Clock, GitBranch } from 'lucide-react';
import { PageContainer, PageSection } from '../../../../components/shell';
import { PageHeader } from '../../../../components/PageHeader';
import { Card, CardBody } from '../../../../components/Card';
import { Badge } from '../../../../components/Badge';
import { Button } from '../../../../components/Button';
import { PageLoadingState } from '../../../../components/PageLoadingState';
import client from '../../../../api/client';

interface RollbackCandidate {
  id: string;
  serviceName: string;
  currentVersion: string;
  targetVersion: string;
  environment: string;
  reason: string;
  riskLevel: 'Low' | 'Medium' | 'High';
  estimatedDowntime: number;
  deployedAt: string;
  status: 'Available' | 'InProgress' | 'Completed' | 'Failed';
}

const useRollbackCockpit = () =>
  useQuery({
    queryKey: ['rollback-cockpit'],
    queryFn: () =>
      client
        .get<{ candidates: RollbackCandidate[]; isSimulated: boolean }>(
          '/api/v1/changes/rollback/candidates',
          { params: { tenantId: 'default' } }
        )
        .then((r) => r.data),
  });

const RISK_CONFIG = {
  Low: { badge: 'success' as const, color: 'text-success' },
  Medium: { badge: 'warning' as const, color: 'text-warning' },
  High: { badge: 'destructive' as const, color: 'text-destructive' },
};

const STATUS_CONFIG = {
  Available: { badge: 'secondary' as const, icon: <CheckCircle2 size={12} /> },
  InProgress: { badge: 'warning' as const, icon: <Clock size={12} /> },
  Completed: { badge: 'success' as const, icon: <CheckCircle2 size={12} /> },
  Failed: { badge: 'destructive' as const, icon: <AlertTriangle size={12} /> },
};

export function RollbackCockpitPage() {
  const { t } = useTranslation();
  const qc = useQueryClient();
  const { data, isLoading } = useRollbackCockpit();

  const initiateRollback = useMutation({
    mutationFn: (id: string) =>
      client.post(`/api/v1/changes/rollback/${id}/initiate`, { tenantId: 'default', userId: 'current-user' }),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['rollback-cockpit'] }),
  });

  const candidates = data?.candidates ?? [];
  const available = candidates.filter((c) => c.status === 'Available');
  const inProgress = candidates.filter((c) => c.status === 'InProgress');

  return (
    <PageContainer>
      <PageHeader
        title={t('rollbackCockpit.title')}
        subtitle={t('rollbackCockpit.subtitle')}
      />
      <PageSection>
        {/* Stats */}
        <div className="grid grid-cols-3 gap-3 mb-6">
          <Card>
            <CardBody className="p-3">
              <div className="flex items-center gap-2 text-muted-foreground mb-1">
                <RotateCcw size={12} />
                <span className="text-xs">{t('rollbackCockpit.available')}</span>
              </div>
              <p className="text-2xl font-bold">{available.length}</p>
            </CardBody>
          </Card>
          <Card>
            <CardBody className="p-3">
              <div className="flex items-center gap-2 text-warning mb-1">
                <Clock size={12} />
                <span className="text-xs">{t('rollbackCockpit.inProgress')}</span>
              </div>
              <p className="text-2xl font-bold">{inProgress.length}</p>
            </CardBody>
          </Card>
          <Card>
            <CardBody className="p-3">
              <div className="flex items-center gap-2 text-destructive mb-1">
                <AlertTriangle size={12} />
                <span className="text-xs">{t('rollbackCockpit.highRisk')}</span>
              </div>
              <p className="text-2xl font-bold">{candidates.filter((c) => c.riskLevel === 'High').length}</p>
            </CardBody>
          </Card>
        </div>

        {isLoading ? (
          <PageLoadingState />
        ) : (
          <div className="space-y-2">
            {candidates.map((c) => {
              const riskCfg = RISK_CONFIG[c.riskLevel] ?? RISK_CONFIG.Medium;
              const statusCfg = STATUS_CONFIG[c.status] ?? STATUS_CONFIG.Available;
              return (
                <Card key={c.id}>
                  <CardBody className="p-4">
                    <div className="flex items-start justify-between gap-3">
                      <div className="flex-1 min-w-0">
                        <div className="flex items-center gap-2 mb-1">
                          <span className="text-sm font-semibold">{c.serviceName}</span>
                          <Badge variant="outline" className="text-xs">{c.environment}</Badge>
                          <Badge variant={riskCfg.badge} className="text-xs">{c.riskLevel} risk</Badge>
                        </div>
                        <div className="flex items-center gap-2 text-xs text-muted-foreground mb-1">
                          <GitBranch size={10} />
                          <span className="font-mono">{c.currentVersion}</span>
                          <span>→</span>
                          <span className="font-mono">{c.targetVersion}</span>
                        </div>
                        <p className="text-xs text-muted-foreground">{c.reason}</p>
                        <div className="flex items-center gap-3 mt-1 text-xs text-muted-foreground">
                          <span>{t('rollbackCockpit.downtime')}: ~{c.estimatedDowntime}s</span>
                          <span>{new Date(c.deployedAt).toLocaleString()}</span>
                        </div>
                      </div>
                      <div className="flex flex-col items-end gap-2">
                        <Badge variant={statusCfg.badge} className="flex items-center gap-1">
                          {statusCfg.icon}
                          {c.status}
                        </Badge>
                        {c.status === 'Available' && (
                          <Button
                            size="sm"
                            variant="ghost"
                            className="text-destructive hover:text-destructive"
                            onClick={() => initiateRollback.mutate(c.id)}
                            disabled={initiateRollback.isPending}
                          >
                            <RotateCcw size={12} className="mr-1" />
                            {t('rollbackCockpit.rollback')}
                          </Button>
                        )}
                      </div>
                    </div>
                  </CardBody>
                </Card>
              );
            })}
            {candidates.length === 0 && (
              <div className="flex items-center justify-center gap-2 p-8 text-success text-sm">
                <CheckCircle2 size={16} />
                {t('rollbackCockpit.noRollbacks')}
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
