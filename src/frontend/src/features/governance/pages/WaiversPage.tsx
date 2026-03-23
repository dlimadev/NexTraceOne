import { useState, useEffect } from 'react';
import { useTranslation } from 'react-i18next';
import {
  FileCheck, Search, Clock, CheckCircle, XCircle, Shield,
} from 'lucide-react';
import { Card, CardBody, CardHeader } from '../../../components/Card';
import { Badge } from '../../../components/Badge';
import { StatCard } from '../../../components/StatCard';
import { PageContainer } from '../../../components/shell';
import { PageHeader } from '../../../components/PageHeader';
import { PageLoadingState } from '../../../components/PageLoadingState';
import { PageErrorState } from '../../../components/PageErrorState';
import { organizationGovernanceApi } from '../api/organizationGovernance';
import type { GovernanceWaiverDto, WaiverStatus as WaiverStatusType } from '../../../types';

type StatusFilter = 'all' | WaiverStatusType;

const waiverStatusBadge = (status: string): 'success' | 'warning' | 'danger' | 'default' | 'info' => {
  switch (status) {
    case 'Approved': return 'success';
    case 'Pending': return 'warning';
    case 'Rejected': return 'danger';
    case 'Expired': return 'default';
    case 'Revoked': return 'info';
    default: return 'default';
  }
};

/**
 * Página de gestão de waivers de governança — listagem, filtragem e ações visuais.
 * Parte do módulo Governance do NexTraceOne.
 */
export function WaiversPage() {
  const { t } = useTranslation();
  const [statusFilter, setStatusFilter] = useState<StatusFilter>('all');
  const [search, setSearch] = useState('');
  const [waivers, setWaivers] = useState<GovernanceWaiverDto[]>([]);
  const [totalWaivers, setTotalWaivers] = useState(0);
  const [pendingCount, setPendingCount] = useState(0);
  const [approvedCount, setApprovedCount] = useState(0);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;
    // eslint-disable-next-line react-hooks/set-state-in-effect -- synchronous setState before async fetch is intentional
    setLoading(true);
    setError(null);

    organizationGovernanceApi.listGovernanceWaivers()
      .then((data) => {
        if (!cancelled) {
          setWaivers(data.waivers);
          setTotalWaivers(data.totalWaivers);
          setPendingCount(data.pendingCount);
          setApprovedCount(data.approvedCount);
          setLoading(false);
        }
      })
      .catch((err) => {
        if (!cancelled) {
          setError(err.message || t('common.errorLoading'));
          setLoading(false);
        }
      });

    return () => { cancelled = true; };
  }, [t]);

  const expiredCount = waivers.filter(w => w.status === 'Expired').length;

  const filtered = waivers.filter(w => {
    if (statusFilter !== 'all' && w.status !== statusFilter) return false;
    if (search) {
      const q = search.toLowerCase();
      return w.packName.toLowerCase().includes(q)
        || w.ruleName.toLowerCase().includes(q)
        || w.scope.toLowerCase().includes(q)
        || w.justification.toLowerCase().includes(q)
        || w.requestedBy.toLowerCase().includes(q);
    }
    return true;
  });

  if (loading) {
    return (
      <PageContainer>
        <PageHeader
          title={t('governancePacks.waivers.title')}
          subtitle={t('governancePacks.waivers.subtitle')}
        />
        <PageLoadingState />
      </PageContainer>
    );
  }

  if (error) {
    return (
      <PageContainer>
        <PageHeader
          title={t('governancePacks.waivers.title')}
          subtitle={t('governancePacks.waivers.subtitle')}
        />
        <PageErrorState message={error} />
      </PageContainer>
    );
  }

  return (
    <PageContainer>
      <PageHeader
        title={t('governancePacks.waivers.title')}
        subtitle={t('governancePacks.waivers.subtitle')}
      />

      {/* Stats */}
      <div className="grid grid-cols-2 md:grid-cols-4 gap-4 mb-6">
        <StatCard title={t('governancePacks.waivers.totalWaivers')} value={totalWaivers} icon={<FileCheck size={20} />} color="text-accent" />
        <StatCard title={t('governancePacks.waivers.pendingApproval')} value={pendingCount} icon={<Clock size={20} />} color="text-warning" />
        <StatCard title={t('governancePacks.waivers.approved')} value={approvedCount} icon={<CheckCircle size={20} />} color="text-success" />
        <StatCard title={t('governancePacks.waivers.expired')} value={expiredCount} icon={<XCircle size={20} />} color="text-muted" />
      </div>

      {/* Filters */}
      <div className="flex flex-wrap items-center gap-3 mb-6">
        <div className="relative flex-1 max-w-xs">
          <Search size={16} className="absolute left-3 top-1/2 -translate-y-1/2 text-muted" />
          <input
            type="text"
            value={search}
            onChange={e => setSearch(e.target.value)}
            placeholder={t('governancePacks.waivers.searchPlaceholder')}
            className="w-full pl-9 pr-3 py-2 text-sm rounded-md bg-surface border border-edge text-body placeholder:text-muted focus:outline-none focus:ring-1 focus:ring-accent"
          />
        </div>
        {(['all', 'Pending', 'Approved', 'Rejected', 'Expired', 'Revoked'] as StatusFilter[]).map(f => (
          <button
            key={f}
            onClick={() => setStatusFilter(f)}
            className={`px-3 py-1.5 text-xs rounded-md border transition-colors ${
              statusFilter === f
                ? 'bg-accent/10 text-accent border-accent/30'
                : 'bg-surface text-muted border-edge hover:text-body'
            }`}
          >
            {f === 'all' ? t('governancePacks.waivers.filterAll') : t(`governancePacks.waivers.status.${f}`)}
          </button>
        ))}
      </div>

      {/* Waivers list */}
      {waivers.length === 0 ? (
        <div className="p-8 text-center text-muted text-sm">{t('governancePacks.waivers.noWaivers')}</div>
      ) : (
        <Card>
          <CardHeader>
            <h2 className="text-sm font-semibold text-heading flex items-center gap-2">
              <Shield size={16} className="text-accent" />
              {t('governancePacks.waivers.listTitle')}
            </h2>
            <p className="text-xs text-muted mt-1">{t('governancePacks.waivers.listDescription')}</p>
          </CardHeader>
          <CardBody className="p-0">
            <div className="divide-y divide-edge">
              {filtered.length === 0 ? (
                <div className="p-8 text-center text-muted text-sm">{t('common.noResults')}</div>
              ) : (
                filtered.map(waiver => (
                  <div key={waiver.waiverId} className="px-4 py-4 hover:bg-hover transition-colors">
                    <div className="flex items-start gap-4">
                      <FileCheck size={14} className="text-muted mt-1 shrink-0" />
                      <div className="min-w-0 flex-1">
                        <div className="flex items-center gap-2 mb-1 flex-wrap">
                          <span className="text-sm font-medium text-heading">{waiver.ruleName}</span>
                          <Badge variant={waiverStatusBadge(waiver.status)}>
                            {t(`governancePacks.waivers.status.${waiver.status}`)}
                          </Badge>
                          <Badge variant="default">{waiver.packName}</Badge>
                        </div>
                        <p className="text-xs text-muted mb-2">{waiver.justification}</p>
                        <div className="flex items-center gap-4 text-xs text-faded flex-wrap">
                          <span>{t('governancePacks.waivers.scope')}: {waiver.scope}</span>
                          <span>{t('governancePacks.waivers.requestedBy')}: {waiver.requestedBy}</span>
                          {waiver.expiresAt && (
                            <span>{t('governancePacks.waivers.expiresAt')}: {new Date(waiver.expiresAt).toLocaleDateString()}</span>
                          )}
                        </div>
                        {/* Action buttons — visual only */}
                        {waiver.status === 'Pending' && (
                          <div className="flex items-center gap-2 mt-3">
                            <button className="inline-flex items-center gap-1 px-3 py-1.5 text-xs font-medium rounded-md bg-success/15 text-success hover:bg-success/25 transition-colors">
                              <CheckCircle size={12} />
                              {t('governancePacks.waivers.approve')}
                            </button>
                            <button className="inline-flex items-center gap-1 px-3 py-1.5 text-xs font-medium rounded-md bg-critical/15 text-critical hover:bg-critical/25 transition-colors">
                              <XCircle size={12} />
                              {t('governancePacks.waivers.reject')}
                            </button>
                          </div>
                        )}
                      </div>
                    </div>
                  </div>
                ))
              )}
            </div>
          </CardBody>
        </Card>
      )}
    </PageContainer>
  );
}
