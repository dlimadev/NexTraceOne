import { useTranslation } from 'react-i18next';
import { useQuery } from '@tanstack/react-query';
import { Activity, AlertTriangle, BookOpen, CheckCircle2, Clock, GitBranch, User, Zap } from 'lucide-react';
import { Link } from 'react-router-dom';
import { PageContainer, PageSection } from '../../../../components/shell';
import { PageHeader } from '../../../../components/PageHeader';
import { Card, CardBody } from '../../../../components/Card';
import { Badge } from '../../../../components/Badge';
import { PageLoadingState } from '../../../../components/PageLoadingState';
import client from '../../../../api/client';

const useEngineerHome = () =>
  useQuery({
    queryKey: ['persona-home', 'engineer'],
    queryFn: () =>
      client
        .get<{ cards: Array<{ key: string; title: string; value?: string; severity: string; linkTo?: string; isSimulated: boolean }>; quickActions: Array<{ key: string; label: string; url: string }> }>('/api/v1/governance/persona-home', {
          params: { tenantId: 'default', userId: 'current-user', persona: 'engineer' },
        })
        .then((r) => r.data),
  });

const ICON_MAP: Record<string, React.ReactNode> = {
  'on-call': <Zap size={14} />,
  'services-health': <Activity size={14} />,
  'open-drifts': <GitBranch size={14} />,
  'failing-slos': <AlertTriangle size={14} />,
  'incidents-assigned': <Clock size={14} />,
  'pending-approvals': <CheckCircle2 size={14} />,
  'last-deploys': <GitBranch size={14} />,
  'runbooks': <BookOpen size={14} />,
};

const SEVERITY_COLOR: Record<string, string> = {
  ok: 'text-success',
  warning: 'text-warning',
  critical: 'text-destructive',
  info: 'text-info',
};

export function EngineerCockpitPage() {
  const { t } = useTranslation();
  const { data, isLoading } = useEngineerHome();

  return (
    <PageContainer>
      <PageHeader
        title={t('personaSuite.engineer.title')}
        subtitle={t('personaSuite.engineer.subtitle')}
        actions={
          <Badge variant="secondary">
            <User size={10} className="mr-1" />
            {t('personaSuite.engineer.role')}
          </Badge>
        }
      />
      <PageSection>
        {isLoading ? (
          <PageLoadingState />
        ) : (
          <>
            {/* Quick Actions */}
            <div className="flex gap-2 flex-wrap mb-6">
              {(data?.quickActions ?? []).map((action) => (
                <Link key={action.key} to={action.url}>
                  <button className="px-3 py-1.5 rounded-full text-xs font-medium bg-accent/10 text-accent hover:bg-accent/20 transition-colors">
                    {action.label}
                  </button>
                </Link>
              ))}
            </div>

            {/* Cards Grid */}
            <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
              {(data?.cards ?? []).map((card) => (
                <Link key={card.key} to={card.linkTo ?? '#'}>
                  <Card className="hover:border-accent/60 transition-colors h-full">
                    <CardBody className="p-4">
                      <div className={`flex items-center gap-2 mb-2 ${SEVERITY_COLOR[card.severity] ?? 'text-muted-foreground'}`}>
                        {ICON_MAP[card.key] ?? <Activity size={14} />}
                        <span className="text-xs font-medium">{card.title}</span>
                        {card.isSimulated && (
                          <Badge variant="secondary" className="text-xs ml-auto">~</Badge>
                        )}
                      </div>
                      <p className={`text-2xl font-bold ${SEVERITY_COLOR[card.severity] ?? 'text-foreground'}`}>
                        {card.value ?? '—'}
                      </p>
                    </CardBody>
                  </Card>
                </Link>
              ))}
            </div>

            {/* Simulated Banner */}
            <div className="mt-6 p-3 rounded-lg bg-muted/40 text-xs text-muted-foreground">
              {t('personaSuite.simulatedBanner')}
            </div>
          </>
        )}
      </PageSection>
    </PageContainer>
  );
}
