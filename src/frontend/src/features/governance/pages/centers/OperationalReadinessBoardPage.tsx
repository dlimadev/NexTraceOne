import { useTranslation } from 'react-i18next';
import { useQuery } from '@tanstack/react-query';
import { CheckCircle2, XCircle, AlertTriangle, Activity, Zap } from 'lucide-react';
import { PageContainer, PageSection } from '../../../../components/shell';
import { PageHeader } from '../../../../components/PageHeader';
import { Card, CardBody } from '../../../../components/Card';
import { Badge } from '../../../../components/Badge';
import { PageLoadingState } from '../../../../components/PageLoadingState';
import client from '../../../../api/client';

interface ReadinessService {
  serviceName: string;
  overallReadiness: 'Go' | 'NoGo' | 'Review';
  sloScore: number;
  chaosScore: number;
  driftScore: number;
  profilingScore: number;
  baselineScore: number;
  blockers: string[];
  isSimulated: boolean;
}

const useReadinessBoard = () =>
  useQuery({
    queryKey: ['operational-readiness'],
    queryFn: () =>
      client
        .get<{ services: ReadinessService[]; isSimulated: boolean }>('/api/v1/operations/readiness-board', {
          params: { tenantId: 'default' },
        })
        .then((r) => r.data),
  });

const READINESS_CONFIG = {
  Go: { color: 'text-success', badge: 'success' as const, icon: <CheckCircle2 size={14} /> },
  Review: { color: 'text-warning', badge: 'warning' as const, icon: <AlertTriangle size={14} /> },
  NoGo: { color: 'text-destructive', badge: 'destructive' as const, icon: <XCircle size={14} /> },
};

export function OperationalReadinessBoardPage() {
  const { t } = useTranslation();
  const { data, isLoading } = useReadinessBoard();

  return (
    <PageContainer>
      <PageHeader
        title={t('operationalReadiness.title')}
        subtitle={t('operationalReadiness.subtitle')}
      />
      <PageSection>
        {isLoading ? (
          <PageLoadingState />
        ) : (
          <>
            <div className="space-y-3">
              {(data?.services ?? []).map((svc) => {
                const cfg = READINESS_CONFIG[svc.overallReadiness] ?? READINESS_CONFIG.Review;
                return (
                  <Card key={svc.serviceName}>
                    <CardBody className="p-4">
                      <div className="flex items-center justify-between gap-3 mb-3">
                        <div className="flex items-center gap-2">
                          <span className={cfg.color}>{cfg.icon}</span>
                          <h3 className="text-sm font-semibold">{svc.serviceName}</h3>
                        </div>
                        <Badge variant={cfg.badge}>{svc.overallReadiness}</Badge>
                      </div>

                      {/* Dimension scores */}
                      <div className="grid grid-cols-5 gap-2 mb-2">
                        {[
                          { label: t('operationalReadiness.slo'), score: svc.sloScore, icon: <Activity size={10} /> },
                          { label: t('operationalReadiness.chaos'), score: svc.chaosScore, icon: <Zap size={10} /> },
                          { label: t('operationalReadiness.drift'), score: svc.driftScore, icon: <AlertTriangle size={10} /> },
                          { label: t('operationalReadiness.profiling'), score: svc.profilingScore, icon: <Activity size={10} /> },
                          { label: t('operationalReadiness.baseline'), score: svc.baselineScore, icon: <CheckCircle2 size={10} /> },
                        ].map((dim) => (
                          <div key={dim.label} className="text-center">
                            <div className="flex items-center justify-center gap-1 mb-1 text-muted-foreground">{dim.icon}<span className="text-xs">{dim.label}</span></div>
                            <div className="relative w-full bg-muted rounded-full h-1.5">
                              <div
                                className={`h-1.5 rounded-full ${dim.score >= 80 ? 'bg-success' : dim.score >= 60 ? 'bg-warning' : 'bg-destructive'}`}
                                style={{ width: `${dim.score}%` }}
                              />
                            </div>
                            <span className="text-xs font-medium">{dim.score}</span>
                          </div>
                        ))}
                      </div>

                      {svc.blockers.length > 0 && (
                        <div className="flex flex-wrap gap-1 mt-2">
                          {svc.blockers.map((b) => (
                            <Badge key={b} variant="destructive" className="text-xs">{b}</Badge>
                          ))}
                        </div>
                      )}

                      {svc.isSimulated && (
                        <Badge variant="secondary" className="text-xs mt-2">~</Badge>
                      )}
                    </CardBody>
                  </Card>
                );
              })}
              {(data?.services ?? []).length === 0 && (
                <div className="text-center p-8 text-muted-foreground text-sm">
                  {t('operationalReadiness.empty')}
                </div>
              )}
            </div>

            <div className="mt-4 p-3 rounded-lg bg-muted/40 text-xs text-muted-foreground">
              {t('sotCenter.simulatedBanner')}
            </div>
          </>
        )}
      </PageSection>
    </PageContainer>
  );
}
