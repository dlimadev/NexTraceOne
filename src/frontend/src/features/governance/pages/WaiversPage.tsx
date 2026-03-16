import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import {
  FileCheck, Search, Clock, CheckCircle, XCircle, Shield,
} from 'lucide-react';
import { Card, CardBody, CardHeader } from '../../../components/Card';
import { Badge } from '../../../components/Badge';
import { StatCard } from '../../../components/StatCard';

/**
 * Tipos locais para waivers de governança — alinhados com o backend ListGovernanceWaivers.
 */
type WaiverStatus = 'Pending' | 'Approved' | 'Rejected' | 'Expired' | 'Revoked';

interface GovernanceWaiver {
  waiverId: string;
  packName: string;
  ruleName: string;
  scope: string;
  justification: string;
  status: WaiverStatus;
  requestedBy: string;
  requestedAt: string;
  expiresAt: string;
}

/**
 * Dados simulados de waivers — alinhados com o backend ListGovernanceWaivers.
 * Em produção, virão da API /api/v1/governance/waivers.
 */
const mockWaivers: GovernanceWaiver[] = [
  {
    waiverId: 'w-001',
    packName: 'Contracts Baseline',
    ruleName: 'SEMVER-REQUIRED',
    scope: 'Legacy Service A',
    justification: 'Legacy service migration to new versioning in progress — expected Q2 completion',
    status: 'Approved',
    requestedBy: 'alice@company.com',
    requestedAt: new Date(Date.now() - 15 * 86400000).toISOString(),
    expiresAt: new Date(Date.now() + 45 * 86400000).toISOString(),
  },
  {
    waiverId: 'w-002',
    packName: 'Change Governance Pack',
    ruleName: 'BLAST-RADIUS-CHECK',
    scope: 'Internal Tools Service',
    justification: 'Low-risk internal tool with no external consumers — blast radius not applicable',
    status: 'Pending',
    requestedBy: 'bob@company.com',
    requestedAt: new Date(Date.now() - 2 * 86400000).toISOString(),
    expiresAt: new Date(Date.now() + 30 * 86400000).toISOString(),
  },
  {
    waiverId: 'w-003',
    packName: 'Source of Truth Standards',
    ruleName: 'OWNER-ASSIGNMENT',
    scope: 'Deprecated Reporting Service',
    justification: 'Service scheduled for decommission — no owner reassignment needed',
    status: 'Rejected',
    requestedBy: 'carol@company.com',
    requestedAt: new Date(Date.now() - 10 * 86400000).toISOString(),
    expiresAt: new Date(Date.now() + 20 * 86400000).toISOString(),
  },
  {
    waiverId: 'w-004',
    packName: 'Contracts Baseline',
    ruleName: 'BREAKING-CHANGE-REVIEW',
    scope: 'Legacy Payment Gateway',
    justification: 'Temporary waiver during payment provider migration — migration team tracking',
    status: 'Expired',
    requestedBy: 'dave@company.com',
    requestedAt: new Date(Date.now() - 60 * 86400000).toISOString(),
    expiresAt: new Date(Date.now() - 5 * 86400000).toISOString(),
  },
  {
    waiverId: 'w-005',
    packName: 'AI Usage Policy',
    ruleName: 'MODEL-APPROVAL',
    scope: 'Research Team',
    justification: 'Experimentation phase with new model — formal approval pending security review',
    status: 'Pending',
    requestedBy: 'eve@company.com',
    requestedAt: new Date(Date.now() - 1 * 86400000).toISOString(),
    expiresAt: new Date(Date.now() + 14 * 86400000).toISOString(),
  },
];

type StatusFilter = 'all' | WaiverStatus;

const waiverStatusBadge = (status: WaiverStatus): 'success' | 'warning' | 'danger' | 'default' | 'info' => {
  switch (status) {
    case 'Approved': return 'success';
    case 'Pending': return 'warning';
    case 'Rejected': return 'danger';
    case 'Expired': return 'default';
    case 'Revoked': return 'info';
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

  const totalWaivers = mockWaivers.length;
  const pendingCount = mockWaivers.filter(w => w.status === 'Pending').length;
  const approvedCount = mockWaivers.filter(w => w.status === 'Approved').length;
  const expiredCount = mockWaivers.filter(w => w.status === 'Expired').length;

  const filtered = mockWaivers.filter(w => {
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

  return (
    <div className="p-6 lg:p-8 animate-fade-in">
      {/* Header */}
      <div className="mb-6">
        <h1 className="text-2xl font-bold text-heading">{t('governancePacks.waivers.title')}</h1>
        <p className="text-muted mt-1">{t('governancePacks.waivers.subtitle')}</p>
      </div>

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
                        <span>{t('governancePacks.waivers.expiresAt')}: {new Date(waiver.expiresAt).toLocaleDateString()}</span>
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
    </div>
  );
}
