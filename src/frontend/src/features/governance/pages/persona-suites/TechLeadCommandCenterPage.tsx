import { useTranslation } from 'react-i18next';
import { useQuery } from '@tanstack/react-query';
import { Users, TrendingUp, AlertTriangle, GitBranch, BarChart3, Shield } from 'lucide-react';
import { Link } from 'react-router-dom';
import { PageContainer, PageSection } from '../../../../components/shell';
import { PageHeader } from '../../../../components/PageHeader';
import { Card, CardBody } from '../../../../components/Card';
import { Badge } from '../../../../components/Badge';
import { StatCard } from '../../../../components/StatCard';
import { PageLoadingState } from '../../../../components/PageLoadingState';
import client from '../../../../api/client';

const useTechLeadHome = () =>
  useQuery({
    queryKey: ['persona-home', 'tech-lead'],
    queryFn: () =>
      client
        .get('/api/v1/governance/persona-home', {
          params: { tenantId: 'default', userId: 'current-user', persona: 'tech-lead' },
        })
        .then((r) => r.data as { cards: Array<{ key: string; title: string; value?: string; severity: string; linkTo?: string; isSimulated: boolean }> }),
  });

export function TechLeadCommandCenterPage() {
  const { t } = useTranslation();
  const { data, isLoading } = useTechLeadHome();

  return (
    <PageContainer>
      <PageHeader
        title={t('personaSuite.techLead.title')}
        subtitle={t('personaSuite.techLead.subtitle')}
        actions={
          <Badge variant="secondary">
            <Users size={10} className="mr-1" />
            {t('personaSuite.techLead.role')}
          </Badge>
        }
      />
      <PageSection>
        {isLoading ? (
          <PageLoadingState />
        ) : (
          <>
            <div className="grid grid-cols-2 lg:grid-cols-4 gap-4 mb-6">
              {(data?.cards ?? []).map((card) => (
                <Link key={card.key} to={card.linkTo ?? '#'}>
                  <StatCard
                    title={card.title}
                    value={card.value ?? '—'}
                    trend={card.severity === 'ok' ? 'up' : card.severity === 'warning' ? 'neutral' : 'down'}
                  />
                </Link>
              ))}
            </div>

            {/* Team Health Overview */}
            <div className="grid grid-cols-1 lg:grid-cols-3 gap-4">
              {[
                { label: t('personaSuite.techLead.teamVelocity'), icon: <TrendingUp size={14} />, to: '/governance/dora-metrics', color: 'text-success' },
                { label: t('personaSuite.techLead.ownershipGaps'), icon: <AlertTriangle size={14} />, to: '/catalog/services', color: 'text-warning' },
                { label: t('personaSuite.techLead.promotionBlockers'), icon: <GitBranch size={14} />, to: '/changes/promotion-readiness', color: 'text-info' },
                { label: t('personaSuite.techLead.changeVelocity'), icon: <BarChart3 size={14} />, to: '/changes', color: 'text-primary' },
                { label: t('personaSuite.techLead.sloCompliance'), icon: <Shield size={14} />, to: '/operations/slos', color: 'text-accent' },
                { label: t('personaSuite.techLead.windowConformance'), icon: <Users size={14} />, to: '/governance/gates', color: 'text-secondary' },
              ].map((item) => (
                <Link key={item.to} to={item.to}>
                  <Card className="hover:border-accent/60 transition-colors">
                    <CardBody className="p-3 flex items-center gap-2">
                      <span className={item.color}>{item.icon}</span>
                      <span className="text-sm font-medium">{item.label}</span>
                    </CardBody>
                  </Card>
                </Link>
              ))}
            </div>

            <div className="mt-4 p-3 rounded-lg bg-muted/40 text-xs text-muted-foreground">
              {t('personaSuite.simulatedBanner')}
            </div>
          </>
        )}
      </PageSection>
    </PageContainer>
  );
}
