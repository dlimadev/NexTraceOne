import { useTranslation } from 'react-i18next';
import { useQuery } from '@tanstack/react-query';
import { Server, Users, Plug, Brain, Shield, Activity, Key, BarChart3 } from 'lucide-react';
import { Link } from 'react-router-dom';
import { PageContainer, PageSection } from '../../../../components/shell';
import { PageHeader } from '../../../../components/PageHeader';
import { Card, CardBody } from '../../../../components/Card';
import { Badge } from '../../../../components/Badge';
import { PageLoadingState } from '../../../../components/PageLoadingState';
import client from '../../../../api/client';

const usePlatformAdminHome = () =>
  useQuery({
    queryKey: ['platform-admin-home'],
    queryFn: () =>
      client
        .get('/api/v1/governance/platform/status', { params: { tenantId: 'default' } })
        .then((r) => r.data as {
          tenantsCount?: number;
          integrationsCount?: number;
          aiTokenUsage?: number;
          auditEventsToday?: number;
          pendingAccessReviews?: number;
          isSimulated?: boolean;
        }),
  });

const QUICK_LINKS = [
  { key: 'tenants', label: 'platformAdmin.tenants', icon: <Server size={14} />, to: '/platform/tenants', color: 'text-accent' },
  { key: 'users', label: 'platformAdmin.usersAccess', icon: <Users size={14} />, to: '/platform/users', color: 'text-info' },
  { key: 'integrations', label: 'platformAdmin.integrations', icon: <Plug size={14} />, to: '/integrations', color: 'text-success' },
  { key: 'ai-budget', label: 'platformAdmin.aiTokenBudget', icon: <Brain size={14} />, to: '/ai-hub/governance', color: 'text-warning' },
  { key: 'audit', label: 'platformAdmin.auditLog', icon: <Shield size={14} />, to: '/audit-compliance/audit', color: 'text-destructive' },
  { key: 'health', label: 'platformAdmin.systemHealth', icon: <Activity size={14} />, to: '/admin/system-health', color: 'text-primary' },
  { key: 'api-keys', label: 'platformAdmin.apiKeys', icon: <Key size={14} />, to: '/platform/api-keys', color: 'text-secondary' },
  { key: 'access-reviews', label: 'platformAdmin.accessReviews', icon: <BarChart3 size={14} />, to: '/identity-access/access-reviews', color: 'text-muted-foreground' },
];

export function PlatformAdminCockpitPage() {
  const { t } = useTranslation();
  const { data, isLoading } = usePlatformAdminHome();

  return (
    <PageContainer>
      <PageHeader
        title={t('personaSuite.platformAdmin.title')}
        subtitle={t('personaSuite.platformAdmin.subtitle')}
        actions={<Badge variant="secondary">{t('personaSuite.platformAdmin.role')}</Badge>}
      />
      <PageSection>
        {isLoading ? (
          <PageLoadingState />
        ) : (
          <>
            {/* Stats Row */}
            <div className="grid grid-cols-2 md:grid-cols-4 gap-3 mb-6">
              {[
                { label: t('platformAdmin.tenants'), value: data?.tenantsCount ?? 3, color: 'text-accent' },
                { label: t('platformAdmin.integrations'), value: data?.integrationsCount ?? 12, color: 'text-success' },
                { label: t('platformAdmin.aiTokenUsagePct'), value: `${data?.aiTokenUsage ?? 64}%`, color: 'text-warning' },
                { label: t('platformAdmin.accessReviewsPending'), value: data?.pendingAccessReviews ?? 5, color: 'text-info' },
              ].map((stat) => (
                <Card key={stat.label}>
                  <CardBody className="p-3">
                    <p className="text-xs text-muted-foreground mb-1">{stat.label}</p>
                    <p className={`text-xl font-bold ${stat.color}`}>{stat.value}</p>
                    {data?.isSimulated && <Badge variant="secondary" className="text-xs">~</Badge>}
                  </CardBody>
                </Card>
              ))}
            </div>

            {/* Quick Links */}
            <div className="grid grid-cols-2 md:grid-cols-4 gap-3">
              {QUICK_LINKS.map((link) => (
                <Link key={link.key} to={link.to}>
                  <Card className="hover:border-accent/60 transition-colors">
                    <CardBody className="p-3 flex items-center gap-2">
                      <span className={link.color}>{link.icon}</span>
                      <span className="text-xs font-medium">{t(link.label, { defaultValue: link.key })}</span>
                    </CardBody>
                  </Card>
                </Link>
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
