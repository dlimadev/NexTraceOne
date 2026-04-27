import { useTranslation } from 'react-i18next';
import { useQuery } from '@tanstack/react-query';
import {
  Smartphone, Bell, AlertTriangle, CheckCircle2, Clock, User, ChevronRight, Wifi,
  RefreshCw, Activity, Shield
} from 'lucide-react';
import { Link } from 'react-router-dom';
import { PageContainer, PageSection } from '../../../components/shell';
import { PageHeader } from '../../../components/PageHeader';
import { Card, CardBody } from '../../../components/Card';
import { Badge } from '../../../components/Badge';
import { Button } from '../../../components/Button';
import { PageLoadingState } from '../../../components/PageLoadingState';
import client from '../../../api/client';

// ── Types ──────────────────────────────────────────────────────────────────────

interface OnCallIncident {
  id: string;
  title: string;
  severity: 'critical' | 'high' | 'medium' | 'low';
  status: 'open' | 'acknowledged' | 'resolved';
  service: string;
  assignedTo?: string;
  createdAt: string;
}

interface PendingApproval {
  id: string;
  title: string;
  type: string;
  requestedBy: string;
  requestedAt: string;
  riskLevel: 'low' | 'medium' | 'high';
}

const useMobileOnCall = () =>
  useQuery({
    queryKey: ['mobile-on-call'],
    queryFn: async () => {
      const [incidents, approvals] = await Promise.allSettled([
        client.get<{ items: OnCallIncident[] }>('/api/v1/operations/incidents', { params: { tenantId: 'default', status: 'open', page: 1, pageSize: 10 } }).then((r) => r.data.items),
        client.get<{ items: PendingApproval[] }>('/api/v1/changes/approvals/pending', { params: { tenantId: 'default' } }).then((r) => r.data.items),
      ]);
      return {
        incidents: incidents.status === 'fulfilled' ? incidents.value : [],
        approvals: approvals.status === 'fulfilled' ? approvals.value : [],
      };
    },
    refetchInterval: 30_000,
  });

// ── Severity Badge ─────────────────────────────────────────────────────────────

function SeverityBadge({ severity }: { severity: string }) {
  const variants: Record<string, string> = {
    critical: 'bg-destructive/10 text-destructive border-destructive/30',
    high: 'bg-warning/10 text-warning border-warning/30',
    medium: 'bg-info/10 text-info border-info/30',
    low: 'bg-muted text-muted-foreground border-border',
  };
  return (
    <span className={`inline-flex items-center px-1.5 py-0.5 rounded text-xs font-medium border ${variants[severity] ?? variants.low}`}>
      {severity}
    </span>
  );
}

// ── Main Page ──────────────────────────────────────────────────────────────────

export function MobileOnCallPage() {
  const { t } = useTranslation();
  const { data, isLoading, refetch } = useMobileOnCall();

  const criticalCount = (data?.incidents ?? []).filter((i) => i.severity === 'critical').length;
  const pendingCount = data?.approvals?.length ?? 0;

  return (
    <PageContainer>
      <PageHeader
        title={t('mobileOnCall.title')}
        subtitle={t('mobileOnCall.subtitle')}
        actions={
          <Button size="sm" variant="ghost" onClick={() => refetch()}>
            <RefreshCw size={14} className="mr-1" />
            {t('mobileOnCall.refresh')}
          </Button>
        }
      />

      {/* PWA Install Banner */}
      <div className="mx-4 mb-4 p-3 rounded-lg border border-accent/40 bg-accent/5 flex items-center gap-2 text-xs">
        <Smartphone size={12} className="text-accent shrink-0" />
        <span className="text-muted-foreground">{t('mobileOnCall.pwaBanner')}</span>
        <Button size="sm" variant="ghost" className="ml-auto shrink-0 text-accent text-xs px-2 py-0.5 h-6">
          {t('mobileOnCall.installPwa')}
        </Button>
      </div>

      <PageSection>
        {/* Summary Cards */}
        <div className="grid grid-cols-2 md:grid-cols-4 gap-3 mb-6">
          {[
            { label: t('mobileOnCall.activeIncidents'), value: data?.incidents?.length ?? 0, icon: <AlertTriangle size={14} />, color: 'text-destructive' },
            { label: t('mobileOnCall.criticalAlerts'), value: criticalCount, icon: <Bell size={14} />, color: 'text-warning' },
            { label: t('mobileOnCall.pendingApprovals'), value: pendingCount, icon: <CheckCircle2 size={14} />, color: 'text-info' },
            { label: t('mobileOnCall.onCallStatus'), value: t('mobileOnCall.active'), icon: <User size={14} />, color: 'text-success' },
          ].map((card) => (
            <Card key={card.label}>
              <CardBody className="p-3">
                <div className={`flex items-center gap-1.5 mb-1 ${card.color}`}>
                  {card.icon}
                  <span className="text-xs font-medium">{card.label}</span>
                </div>
                <p className="text-xl font-bold">{card.value}</p>
              </CardBody>
            </Card>
          ))}
        </div>

        {isLoading ? (
          <PageLoadingState />
        ) : (
          <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
            {/* Open Incidents */}
            <Card>
              <CardBody>
                <h3 className="text-sm font-semibold mb-3 flex items-center gap-2">
                  <Activity size={14} className="text-destructive" />
                  {t('mobileOnCall.openIncidents')}
                  <Badge variant="destructive" className="ml-auto">{data?.incidents?.length ?? 0}</Badge>
                </h3>
                <div className="space-y-2">
                  {(data?.incidents ?? []).slice(0, 5).map((inc) => (
                    <Link key={inc.id} to={`/operations/incidents/${inc.id}`}>
                      <div className="p-2 rounded hover:bg-muted/40 flex items-center gap-2 group">
                        <SeverityBadge severity={inc.severity} />
                        <div className="flex-1 min-w-0">
                          <p className="text-xs font-medium truncate">{inc.title}</p>
                          <p className="text-xs text-muted-foreground">{inc.service}</p>
                        </div>
                        <ChevronRight size={12} className="text-muted-foreground group-hover:text-foreground" />
                      </div>
                    </Link>
                  ))}
                  {(data?.incidents ?? []).length === 0 && (
                    <div className="flex items-center gap-2 text-sm text-success">
                      <CheckCircle2 size={14} />
                      {t('mobileOnCall.noIncidents')}
                    </div>
                  )}
                </div>
                <Link to="/operations/incidents">
                  <Button size="sm" variant="ghost" className="mt-3 w-full text-xs">
                    {t('mobileOnCall.viewAllIncidents')}
                    <ChevronRight size={12} className="ml-1" />
                  </Button>
                </Link>
              </CardBody>
            </Card>

            {/* Pending Approvals */}
            <Card>
              <CardBody>
                <h3 className="text-sm font-semibold mb-3 flex items-center gap-2">
                  <Shield size={14} className="text-info" />
                  {t('mobileOnCall.pendingApprovals')}
                  <Badge variant="secondary" className="ml-auto">{pendingCount}</Badge>
                </h3>
                <div className="space-y-2">
                  {(data?.approvals ?? []).slice(0, 5).map((appr) => (
                    <div key={appr.id} className="p-2 rounded border border-border hover:bg-muted/40">
                      <div className="flex items-center justify-between gap-2 mb-1">
                        <p className="text-xs font-medium truncate">{appr.title}</p>
                        <SeverityBadge severity={appr.riskLevel} />
                      </div>
                      <p className="text-xs text-muted-foreground">
                        {t('mobileOnCall.requestedBy', { user: appr.requestedBy })}
                      </p>
                      <div className="flex gap-2 mt-2">
                        <Button size="sm" className="flex-1 text-xs h-6">
                          {t('mobileOnCall.approve')}
                        </Button>
                        <Button size="sm" variant="ghost" className="flex-1 text-xs h-6">
                          {t('mobileOnCall.reject')}
                        </Button>
                      </div>
                    </div>
                  ))}
                  {(data?.approvals ?? []).length === 0 && (
                    <div className="flex items-center gap-2 text-sm text-success">
                      <CheckCircle2 size={14} />
                      {t('mobileOnCall.noApprovals')}
                    </div>
                  )}
                </div>
              </CardBody>
            </Card>
          </div>
        )}

        {/* Quick Links */}
        <div className="mt-6">
          <h3 className="text-sm font-semibold mb-3">{t('mobileOnCall.quickLinks')}</h3>
          <div className="grid grid-cols-2 md:grid-cols-4 gap-2">
            {[
              { label: t('mobileOnCall.runbooks'), icon: <Clock size={16} />, to: '/knowledge/runbooks' },
              { label: t('mobileOnCall.sloStatus'), icon: <Activity size={16} />, to: '/operations/slos' },
              { label: t('mobileOnCall.myDashboards'), icon: <Wifi size={16} />, to: '/governance/custom-dashboards' },
              { label: t('mobileOnCall.contacts'), icon: <User size={16} />, to: '/governance/teams' },
            ].map((link) => (
              <Link key={link.to} to={link.to}>
                <Card className="hover:border-accent/60 transition-colors">
                  <CardBody className="p-3 flex items-center gap-2">
                    <span className="text-muted-foreground">{link.icon}</span>
                    <span className="text-xs font-medium">{link.label}</span>
                    <ChevronRight size={12} className="ml-auto text-muted-foreground" />
                  </CardBody>
                </Card>
              </Link>
            ))}
          </div>
        </div>
      </PageSection>
    </PageContainer>
  );
}
