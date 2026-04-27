import { useTranslation } from 'react-i18next';
import { useQuery } from '@tanstack/react-query';
import { TrendingUp, Shield, DollarSign, AlertTriangle, FileText, Download } from 'lucide-react';
import { PageContainer, PageSection } from '../../../../components/shell';
import { PageHeader } from '../../../../components/PageHeader';
import { Card, CardBody } from '../../../../components/Card';
import { Badge } from '../../../../components/Badge';
import { Button } from '../../../../components/Button';
import { PageLoadingState } from '../../../../components/PageLoadingState';
import client from '../../../../api/client';

const useExecutiveBrief = () =>
  useQuery({
    queryKey: ['executive-brief'],
    queryFn: () =>
      client
        .get('/api/v1/governance/executive/overview', { params: { tenantId: 'default' } })
        .then((r) => r.data as {
          doraTier?: string;
          complianceScore?: number;
          finOpsTrend?: string;
          topRisks?: Array<{ service: string; level: string }>;
          isSimulated?: boolean;
        }),
  });

export function ExecutiveBriefCenterPage() {
  const { t } = useTranslation();
  const { data, isLoading } = useExecutiveBrief();

  return (
    <PageContainer>
      <PageHeader
        title={t('personaSuite.executive.title')}
        subtitle={t('personaSuite.executive.subtitle')}
        actions={
          <Button size="sm" variant="ghost">
            <Download size={14} className="mr-1" />
            {t('personaSuite.executive.exportPdf')}
          </Button>
        }
      />
      <PageSection>
        {isLoading ? (
          <PageLoadingState />
        ) : (
          <>
            <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4 mb-6">
              <Card>
                <CardBody className="p-4">
                  <div className="flex items-center gap-2 mb-2 text-success">
                    <TrendingUp size={14} />
                    <span className="text-xs font-medium">{t('personaSuite.executive.doraTier')}</span>
                  </div>
                  <p className="text-lg font-bold">{data?.doraTier ?? 'High'}</p>
                  {data?.isSimulated && <Badge variant="secondary" className="text-xs mt-1">~</Badge>}
                </CardBody>
              </Card>
              <Card>
                <CardBody className="p-4">
                  <div className="flex items-center gap-2 mb-2 text-info">
                    <Shield size={14} />
                    <span className="text-xs font-medium">{t('personaSuite.executive.complianceScore')}</span>
                  </div>
                  <p className="text-lg font-bold">{data?.complianceScore ?? 87}%</p>
                </CardBody>
              </Card>
              <Card>
                <CardBody className="p-4">
                  <div className="flex items-center gap-2 mb-2 text-warning">
                    <DollarSign size={14} />
                    <span className="text-xs font-medium">{t('personaSuite.executive.finOpsTrend')}</span>
                  </div>
                  <p className="text-lg font-bold">{data?.finOpsTrend ?? '↓ 12%'}</p>
                </CardBody>
              </Card>
              <Card>
                <CardBody className="p-4">
                  <div className="flex items-center gap-2 mb-2 text-destructive">
                    <AlertTriangle size={14} />
                    <span className="text-xs font-medium">{t('personaSuite.executive.topRisks')}</span>
                  </div>
                  <p className="text-lg font-bold">{data?.topRisks?.length ?? 3}</p>
                </CardBody>
              </Card>
            </div>

            {/* Report Sections */}
            <div className="grid grid-cols-1 lg:grid-cols-2 gap-4">
              {[
                { title: t('personaSuite.executive.doraDetails'), icon: <TrendingUp size={14} />, href: '/governance/dora-metrics' },
                { title: t('personaSuite.executive.complianceDetails'), icon: <Shield size={14} />, href: '/governance/compliance' },
                { title: t('personaSuite.executive.finOpsDetails'), icon: <DollarSign size={14} />, href: '/governance/finops' },
                { title: t('personaSuite.executive.riskDetails'), icon: <AlertTriangle size={14} />, href: '/governance/risk' },
                { title: t('personaSuite.executive.evidenceDetails'), icon: <FileText size={14} />, href: '/governance/evidence' },
                { title: t('personaSuite.executive.scheduledReports'), icon: <Download size={14} />, href: '/governance/scheduled-reports' },
              ].map((s) => (
                <a key={s.href} href={s.href}>
                  <Card className="hover:border-accent/60 transition-colors">
                    <CardBody className="p-3 flex items-center gap-3">
                      <span className="text-muted-foreground">{s.icon}</span>
                      <span className="text-sm font-medium">{s.title}</span>
                    </CardBody>
                  </Card>
                </a>
              ))}
            </div>

            <div className="mt-6 p-3 rounded-lg bg-muted/40 text-xs text-muted-foreground">
              {t('personaSuite.simulatedBanner')}
            </div>
          </>
        )}
      </PageSection>
    </PageContainer>
  );
}
