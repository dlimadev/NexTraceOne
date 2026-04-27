import { useTranslation } from 'react-i18next';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { GitBranch, AlertTriangle, CheckCircle2, Clock } from 'lucide-react';
import { PageContainer, PageSection } from '../../../../components/shell';
import { PageHeader } from '../../../../components/PageHeader';
import { Card, CardBody } from '../../../../components/Card';
import { Badge } from '../../../../components/Badge';
import { Button } from '../../../../components/Button';
import { PageLoadingState } from '../../../../components/PageLoadingState';
import client from '../../../../api/client';

interface DriftRecord {
  id: string;
  serviceName: string;
  environment: string;
  driftType: string;
  severity: string;
  description: string;
  detectedAt: string;
  lastChangeId?: string;
  status: 'Open' | 'Acknowledged' | 'Resolved';
  isSimulated: boolean;
}

const useDriftCenter = () =>
  useQuery({
    queryKey: ['drift-center'],
    queryFn: () =>
      client
        .get<{ drifts: DriftRecord[]; openCount: number; criticalCount: number }>('/api/v1/operations/drift/report', {
          params: { tenantId: 'default' },
        })
        .then((r) => r.data),
  });

const SEVERITY_BADGE = {
  Critical: 'destructive',
  High: 'warning',
  Medium: 'secondary',
  Low: 'secondary',
} as const;

export function DriftCenterPage() {
  const { t } = useTranslation();
  const qc = useQueryClient();
  const { data, isLoading } = useDriftCenter();

  const acknowledge = useMutation({
    mutationFn: (id: string) =>
      client.post(`/api/v1/operations/drift/${id}/acknowledge`, { tenantId: 'default', userId: 'current-user' }),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['drift-center'] }),
  });

  const openDrifts = (data?.drifts ?? []).filter((d) => d.status === 'Open');
  const ackedDrifts = (data?.drifts ?? []).filter((d) => d.status === 'Acknowledged');

  return (
    <PageContainer>
      <PageHeader
        title={t('driftCenter.title')}
        subtitle={t('driftCenter.subtitle')}
      />
      <PageSection>
        {/* Stats */}
        <div className="grid grid-cols-3 gap-3 mb-6">
          <Card>
            <CardBody className="p-3">
              <div className="flex items-center gap-2 text-destructive mb-1">
                <AlertTriangle size={12} />
                <span className="text-xs">{t('driftCenter.openDrifts')}</span>
              </div>
              <p className="text-2xl font-bold">{data?.openCount ?? 0}</p>
            </CardBody>
          </Card>
          <Card>
            <CardBody className="p-3">
              <div className="flex items-center gap-2 text-warning mb-1">
                <GitBranch size={12} />
                <span className="text-xs">{t('driftCenter.criticalDrifts')}</span>
              </div>
              <p className="text-2xl font-bold">{data?.criticalCount ?? 0}</p>
            </CardBody>
          </Card>
          <Card>
            <CardBody className="p-3">
              <div className="flex items-center gap-2 text-success mb-1">
                <CheckCircle2 size={12} />
                <span className="text-xs">{t('driftCenter.acked')}</span>
              </div>
              <p className="text-2xl font-bold">{ackedDrifts.length}</p>
            </CardBody>
          </Card>
        </div>

        {isLoading ? (
          <PageLoadingState />
        ) : (
          <div className="space-y-2">
            {openDrifts.map((drift) => (
              <Card key={drift.id}>
                <CardBody className="p-3">
                  <div className="flex items-start justify-between gap-3">
                    <div className="flex-1 min-w-0">
                      <div className="flex items-center gap-2 mb-1">
                        <Badge variant={SEVERITY_BADGE[drift.severity as keyof typeof SEVERITY_BADGE] ?? 'secondary'}>
                          {drift.severity}
                        </Badge>
                        <span className="text-sm font-medium truncate">{drift.serviceName}</span>
                        <Badge variant="outline" className="text-xs">{drift.environment}</Badge>
                      </div>
                      <p className="text-xs text-muted-foreground">{drift.description}</p>
                      <div className="flex items-center gap-2 mt-1 text-xs text-muted-foreground">
                        <Clock size={10} />
                        <span>{new Date(drift.detectedAt).toLocaleString()}</span>
                        {drift.lastChangeId && (
                          <a href={`/changes/${drift.lastChangeId}`} className="text-accent hover:underline">
                            {t('driftCenter.correlatedChange')}
                          </a>
                        )}
                      </div>
                    </div>
                    <Button
                      size="sm"
                      variant="ghost"
                      onClick={() => acknowledge.mutate(drift.id)}
                      disabled={acknowledge.isPending}
                    >
                      {t('driftCenter.acknowledge')}
                    </Button>
                  </div>
                </CardBody>
              </Card>
            ))}
            {openDrifts.length === 0 && (
              <div className="flex items-center justify-center gap-2 p-8 text-success text-sm">
                <CheckCircle2 size={16} />
                {t('driftCenter.noDrifts')}
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
