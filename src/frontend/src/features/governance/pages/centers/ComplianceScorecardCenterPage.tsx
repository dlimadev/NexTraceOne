import { useTranslation } from 'react-i18next';
import { useQuery } from '@tanstack/react-query';
import { ShieldCheck, AlertTriangle, CheckCircle2, XCircle, FileText } from 'lucide-react';
import { PageContainer, PageSection } from '../../../../components/shell';
import { PageHeader } from '../../../../components/PageHeader';
import { Card, CardBody } from '../../../../components/Card';
import { Badge } from '../../../../components/Badge';
import { PageLoadingState } from '../../../../components/PageLoadingState';
import client from '../../../../api/client';

interface ComplianceControl {
  id: string;
  standard: string;
  control: string;
  description: string;
  status: 'Compliant' | 'Partial' | 'NonCompliant' | 'NotAssessed';
  evidence: string[];
  lastAssessed: string;
  owner: string;
}

interface ComplianceScorecard {
  overallScore: number;
  compliantCount: number;
  partialCount: number;
  nonCompliantCount: number;
  notAssessedCount: number;
  controls: ComplianceControl[];
  isSimulated: boolean;
}

const useComplianceScorecard = () =>
  useQuery({
    queryKey: ['compliance-scorecard-center'],
    queryFn: () =>
      client
        .get<ComplianceScorecard>('/api/v1/governance/compliance/scorecard', {
          params: { tenantId: 'default' },
        })
        .then((r) => r.data),
  });

const STATUS_CONFIG = {
  Compliant: { badge: 'success' as const, icon: <CheckCircle2 size={12} />, color: 'text-success' },
  Partial: { badge: 'warning' as const, icon: <AlertTriangle size={12} />, color: 'text-warning' },
  NonCompliant: { badge: 'destructive' as const, icon: <XCircle size={12} />, color: 'text-destructive' },
  NotAssessed: { badge: 'secondary' as const, icon: <FileText size={12} />, color: 'text-muted-foreground' },
};

export function ComplianceScorecardCenterPage() {
  const { t } = useTranslation();
  const { data, isLoading } = useComplianceScorecard();

  const controls = data?.controls ?? [];
  const standards = [...new Set(controls.map((c) => c.standard))];

  return (
    <PageContainer>
      <PageHeader
        title={t('complianceScorecard.title')}
        subtitle={t('complianceScorecard.subtitle')}
      />
      <PageSection>
        {isLoading ? (
          <PageLoadingState />
        ) : (
          <>
            {/* Score overview */}
            <div className="grid grid-cols-2 md:grid-cols-5 gap-3 mb-6">
              <Card className="md:col-span-1">
                <CardBody className="p-4 text-center">
                  <div className="flex items-center justify-center gap-1 text-accent mb-1">
                    <ShieldCheck size={14} />
                    <span className="text-xs">{t('complianceScorecard.score')}</span>
                  </div>
                  <p className="text-3xl font-bold">{data?.overallScore ?? 0}%</p>
                </CardBody>
              </Card>
              {[
                { label: t('complianceScorecard.compliant'), value: data?.compliantCount ?? 0, color: 'text-success' },
                { label: t('complianceScorecard.partial'), value: data?.partialCount ?? 0, color: 'text-warning' },
                { label: t('complianceScorecard.nonCompliant'), value: data?.nonCompliantCount ?? 0, color: 'text-destructive' },
                { label: t('complianceScorecard.notAssessed'), value: data?.notAssessedCount ?? 0, color: 'text-muted-foreground' },
              ].map((stat) => (
                <Card key={stat.label}>
                  <CardBody className="p-4 text-center">
                    <p className="text-xs text-muted-foreground mb-1">{stat.label}</p>
                    <p className={`text-2xl font-bold ${stat.color}`}>{stat.value}</p>
                  </CardBody>
                </Card>
              ))}
            </div>

            {/* Controls by standard */}
            {standards.map((std) => (
              <div key={std} className="mb-6">
                <h3 className="text-sm font-semibold mb-2 flex items-center gap-2">
                  <ShieldCheck size={14} className="text-accent" />
                  {std}
                </h3>
                <div className="space-y-2">
                  {controls.filter((c) => c.standard === std).map((ctrl) => {
                    const cfg = STATUS_CONFIG[ctrl.status] ?? STATUS_CONFIG.NotAssessed;
                    return (
                      <Card key={ctrl.id}>
                        <CardBody className="p-3">
                          <div className="flex items-start justify-between gap-3">
                            <div className="flex-1 min-w-0">
                              <div className="flex items-center gap-2 mb-1">
                                <span className={cfg.color}>{cfg.icon}</span>
                                <span className="text-sm font-medium">{ctrl.control}</span>
                              </div>
                              <p className="text-xs text-muted-foreground">{ctrl.description}</p>
                              <div className="flex items-center gap-3 mt-1 text-xs text-muted-foreground">
                                <span>{t('complianceScorecard.owner')}: {ctrl.owner}</span>
                                <span>{new Date(ctrl.lastAssessed).toLocaleDateString()}</span>
                              </div>
                            </div>
                            <Badge variant={cfg.badge}>{ctrl.status}</Badge>
                          </div>
                        </CardBody>
                      </Card>
                    );
                  })}
                </div>
              </div>
            ))}

            {controls.length === 0 && (
              <div className="text-center p-8 text-muted-foreground text-sm">
                {t('complianceScorecard.empty')}
              </div>
            )}

            <div className="mt-4 p-3 rounded-lg bg-muted/40 text-xs text-muted-foreground">
              {t('sotCenter.simulatedBanner')}
            </div>
          </>
        )}
      </PageSection>
    </PageContainer>
  );
}
