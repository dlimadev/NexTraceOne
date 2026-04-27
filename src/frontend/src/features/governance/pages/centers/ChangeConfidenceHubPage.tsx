import { useTranslation } from 'react-i18next';
import { useQuery } from '@tanstack/react-query';
import { useParams } from 'react-router-dom';
import { Shield, GitBranch, CheckCircle2, AlertTriangle, BarChart3, RotateCcw, Link as LinkIcon } from 'lucide-react';
import { PageContainer, PageSection } from '../../../../components/shell';
import { PageHeader } from '../../../../components/PageHeader';
import { Card, CardBody } from '../../../../components/Card';
import { Badge } from '../../../../components/Badge';
import { Button } from '../../../../components/Button';
import { PageLoadingState } from '../../../../components/PageLoadingState';
import { PageErrorState } from '../../../../components/PageErrorState';
import client from '../../../../api/client';

const useChangeConfidence = (changeId?: string) =>
  useQuery({
    queryKey: ['change-confidence-hub', changeId],
    queryFn: () =>
      changeId
        ? client.get(`/api/v1/changes/${changeId}/confidence/breakdown`, { params: { tenantId: 'default' } }).then((r) => r.data)
        : client.get('/api/v1/changes/confidence/latest', { params: { tenantId: 'default' } }).then((r) => r.data),
    enabled: true,
  });

export function ChangeConfidenceHubPage() {
  const { t } = useTranslation();
  const { changeId } = useParams<{ changeId: string }>();
  const { data, isLoading, isError } = useChangeConfidence(changeId);

  if (isLoading) return <PageLoadingState />;
  if (isError) return <PageErrorState />;

  const score = (data as { overallScore?: number })?.overallScore ?? 85;
  const level = score >= 80 ? 'high' : score >= 60 ? 'medium' : 'low';
  const levelColor = { high: 'text-success', medium: 'text-warning', low: 'text-destructive' }[level];
  const subScores = (data as { subScores?: Array<{ name: string; score: number; label: string }> })?.subScores ?? [];

  return (
    <PageContainer>
      <PageHeader
        title={t('changeConfidenceHub.title')}
        subtitle={t('changeConfidenceHub.subtitle')}
        actions={
          <Button size="sm" variant="ghost">
            <RotateCcw size={14} className="mr-1" />
            {t('changeConfidenceHub.rollback')}
          </Button>
        }
      />
      <PageSection>
        {/* Score Overview */}
        <div className="grid grid-cols-1 md:grid-cols-3 gap-4 mb-6">
          <Card className="md:col-span-1">
            <CardBody className="p-6 text-center">
              <p className="text-xs text-muted-foreground mb-2">{t('changeConfidenceHub.confidenceScore')}</p>
              <p className={`text-5xl font-bold ${levelColor}`}>{score}</p>
              <Badge
                variant={level === 'high' ? 'success' : level === 'medium' ? 'warning' : 'destructive'}
                className="mt-2"
              >
                {t(`changeConfidenceHub.level.${level}`)}
              </Badge>
            </CardBody>
          </Card>

          <div className="md:col-span-2 grid grid-cols-2 gap-3">
            {[
              { label: t('changeConfidenceHub.blastRadius'), icon: <GitBranch size={14} />, to: `/governance/blast-radius/${changeId ?? ''}`, color: 'text-warning' },
              { label: t('changeConfidenceHub.evidencePack'), icon: <Shield size={14} />, to: '/governance/evidence', color: 'text-info' },
              { label: t('changeConfidenceHub.contractCompat'), icon: <CheckCircle2 size={14} />, to: '/contracts', color: 'text-success' },
              { label: t('changeConfidenceHub.incidentCorrelation'), icon: <AlertTriangle size={14} />, to: '/operations/incidents', color: 'text-destructive' },
            ].map((item) => (
              <a key={item.label} href={item.to}>
                <Card className="hover:border-accent/60 transition-colors h-full">
                  <CardBody className="p-3 flex items-center gap-2">
                    <span className={item.color}>{item.icon}</span>
                    <span className="text-xs font-medium">{item.label}</span>
                  </CardBody>
                </Card>
              </a>
            ))}
          </div>
        </div>

        {/* Sub-scores */}
        {subScores.length > 0 && (
          <>
            <h3 className="text-sm font-semibold mb-3">{t('changeConfidenceHub.subScores')}</h3>
            <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-3">
              {subScores.map((sub) => (
                <Card key={sub.name}>
                  <CardBody className="p-3">
                    <p className="text-xs text-muted-foreground mb-1">{sub.name}</p>
                    <div className="flex items-center gap-2">
                      <div className="flex-1 bg-muted rounded-full h-1.5">
                        <div
                          className="h-1.5 rounded-full bg-accent"
                          style={{ width: `${sub.score}%` }}
                        />
                      </div>
                      <span className="text-xs font-bold">{sub.score}</span>
                    </div>
                    <p className="text-xs text-muted-foreground mt-1">{sub.label}</p>
                  </CardBody>
                </Card>
              ))}
            </div>
          </>
        )}

        {/* Promotion Decision */}
        <div className="mt-6 p-4 rounded-lg border border-border">
          <h3 className="text-sm font-semibold mb-3 flex items-center gap-2">
            <BarChart3 size={14} />
            {t('changeConfidenceHub.promotionDecision')}
          </h3>
          <div className="flex gap-3">
            <Button className="flex-1" disabled={level === 'low'}>
              <CheckCircle2 size={14} className="mr-1" />
              {t('changeConfidenceHub.promote')}
            </Button>
            <Button variant="ghost" className="flex-1">
              <AlertTriangle size={14} className="mr-1" />
              {t('changeConfidenceHub.requestReview')}
            </Button>
            <Button variant="ghost">
              <RotateCcw size={14} className="mr-1" />
              {t('changeConfidenceHub.rollback')}
            </Button>
          </div>
        </div>

        <div className="mt-4 p-3 rounded-lg bg-muted/40 text-xs text-muted-foreground">
          {t('sotCenter.simulatedBanner')}
        </div>
      </PageSection>
    </PageContainer>
  );
}
