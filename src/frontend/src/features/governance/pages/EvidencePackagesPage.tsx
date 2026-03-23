import { useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { useTranslation } from 'react-i18next';
import {
  Package, Search, Lock, Download, FileCheck, FileText,
  Clock, Shield, Bot, AlertTriangle, ClipboardList,
} from 'lucide-react';
import { Card, CardBody, CardHeader } from '../../../components/Card';
import { Badge } from '../../../components/Badge';
import { StatCard } from '../../../components/StatCard';
import { PageContainer } from '../../../components/shell';
import { PageHeader } from '../../../components/PageHeader';
import { PageLoadingState } from '../../../components/PageLoadingState';
import { PageErrorState } from '../../../components/PageErrorState';
import { evidenceApi } from '../api/evidence';
import { queryKeys } from '../../../shared/api/queryKeys';
import type {
  EvidencePackageStatusType,
  EvidenceTypeValue,
} from '../../../types';
type StatusFilter = 'all' | EvidencePackageStatusType;

const statusBadge = (st: EvidencePackageStatusType): 'success' | 'warning' | 'info' | 'default' => {
  switch (st) {
    case 'Sealed': return 'success';
    case 'Exported': return 'info';
    case 'Draft': return 'warning';
    default: return 'default';
  }
};

const evidenceIcon = (type: string) => {
  switch (type) {
    case 'Approval': return <FileCheck size={14} />;
    case 'ChangeHistory': return <Clock size={14} />;
    case 'ContractPublication': return <FileText size={14} />;
    case 'MitigationRecord': return <AlertTriangle size={14} />;
    case 'AiUsageRecord': return <Bot size={14} />;
    case 'PolicyDecision': return <Shield size={14} />;
    case 'ComplianceResult': return <ClipboardList size={14} />;
    case 'AuditReference': return <ClipboardList size={14} />;
    default: return <FileText size={14} />;
  }
};

/**
 * Página de Evidence Packages — pacotes de evidência para auditoria e compliance.
 * Parte do módulo Governance do NexTraceOne.
 */
export function EvidencePackagesPage() {
  const { t } = useTranslation();
  const [statusFilter, setStatusFilter] = useState<StatusFilter>('all');
  const [search, setSearch] = useState('');
  const [selectedPackageId, setSelectedPackageId] = useState<string | null>(null);

  const { data: d, isLoading, isError, refetch } = useQuery({
    queryKey: queryKeys.governance.evidence.list(),
    queryFn: () => evidenceApi.listPackages(),
    staleTime: 30_000,
  });

  useQuery({
    queryKey: queryKeys.governance.evidence.detail(selectedPackageId!),
    queryFn: () => evidenceApi.getPackage(selectedPackageId!),
    staleTime: 30_000,
    enabled: !!selectedPackageId,
  });

  if (isLoading) return (<PageContainer><PageLoadingState /></PageContainer>);
  if (isError || !d) return (<PageContainer><PageErrorState action={<button onClick={() => refetch()} className="px-3 py-1.5 text-xs rounded-md bg-accent/10 text-accent border border-accent/20 hover:bg-accent/20 transition-colors">{t('common.retry')}</button>} /></PageContainer>);

  const filtered = d.packages.filter(p => {
    if (statusFilter !== 'all' && p.status !== statusFilter) return false;
    if (search) {
      const q = search.toLowerCase();
      return p.name.toLowerCase().includes(q)
        || p.description.toLowerCase().includes(q)
        || p.scope.toLowerCase().includes(q);
    }
    return true;
  });

  const selectedPackage = selectedPackageId
    ? d.packages.find(p => p.packageId === selectedPackageId)
    : null;

  return (
    <PageContainer>
      <PageHeader
        title={t('governance.evidence.title')}
        subtitle={t('governance.evidence.subtitle')}
      />

      {/* Stats */}
      <div className="grid grid-cols-2 md:grid-cols-4 gap-4 mb-6">
        <StatCard title={t('governance.evidence.totalPackages')} value={d.totalPackages} icon={<Package size={20} />} color="text-accent" />
        <StatCard title={t('governance.evidence.sealed')} value={d.sealedCount} icon={<Lock size={20} />} color="text-success" />
        <StatCard title={t('governance.evidence.exported')} value={d.exportedCount} icon={<Download size={20} />} color="text-info" />
        <StatCard title={t('governance.evidence.draft')} value={d.draftCount} icon={<FileText size={20} />} color="text-amber-500" />
      </div>

      {/* Filters */}
      <div className="flex flex-wrap items-center gap-3 mb-6">
        <div className="relative flex-1 max-w-xs">
          <Search size={16} className="absolute left-3 top-1/2 -translate-y-1/2 text-muted" />
          <input
            type="text"
            value={search}
            onChange={e => setSearch(e.target.value)}
            placeholder={t('governance.evidence.searchPlaceholder')}
            className="w-full pl-9 pr-3 py-2 text-sm rounded-md bg-surface border border-edge text-body placeholder:text-muted focus:outline-none focus:ring-1 focus:ring-accent"
          />
        </div>
        {(['all', 'Sealed', 'Exported', 'Draft'] as StatusFilter[]).map(f => (
          <button
            key={f}
            onClick={() => setStatusFilter(f)}
            className={`px-3 py-1.5 text-xs rounded-md border transition-colors ${
              statusFilter === f
                ? 'bg-accent/10 text-accent border-accent/30'
                : 'bg-surface text-muted border-edge hover:text-body'
            }`}
          >
            {f === 'all' ? t('governance.evidence.filterAll') : t(`governance.evidence.status.${f}`)}
          </button>
        ))}
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
        {/* Packages list */}
        <div className="lg:col-span-2">
          <Card>
            <CardHeader>
              <h2 className="text-sm font-semibold text-heading flex items-center gap-2">
                <Package size={16} className="text-accent" />
                {t('governance.evidence.packagesTitle')}
              </h2>
              <p className="text-xs text-muted mt-1">{t('governance.evidence.packagesDescription')}</p>
            </CardHeader>
            <CardBody className="p-0">
              <div className="divide-y divide-edge">
                {filtered.length === 0 ? (
                  <div className="p-8 text-center text-muted text-sm">{t('common.noResults')}</div>
                ) : (
                  filtered.map(pkg => (
                    <button
                      key={pkg.packageId}
                      onClick={() => setSelectedPackageId(pkg.packageId)}
                      className={`w-full text-left flex items-start gap-4 px-4 py-3 transition-colors ${
                        selectedPackageId === pkg.packageId ? 'bg-accent/5 border-l-2 border-accent' : 'hover:bg-hover'
                      }`}
                    >
                      <div className="mt-1 text-muted"><Package size={14} /></div>
                      <div className="min-w-0 flex-1">
                        <div className="flex items-center gap-2 mb-1 flex-wrap">
                          <span className="text-sm font-medium text-heading">{pkg.name}</span>
                          <Badge variant={statusBadge(pkg.status)}>{t(`governance.evidence.status.${pkg.status}`)}</Badge>
                        </div>
                        <p className="text-xs text-muted mb-1">{pkg.description}</p>
                        <div className="flex items-center gap-4 text-xs text-faded">
                          <span>{t('governance.evidence.items')}: {pkg.itemCount}</span>
                          <span>{t('governance.evidence.createdBy')}: {pkg.createdBy}</span>
                          <span>{t('governance.evidence.scope')}: {pkg.scope}</span>
                        </div>
                      </div>
                    </button>
                  ))
                )}
              </div>
            </CardBody>
          </Card>
        </div>

        {/* Package detail / evidence items */}
        <div>
          <Card>
            <CardHeader>
              <h2 className="text-sm font-semibold text-heading flex items-center gap-2">
                <FileCheck size={16} className="text-accent" />
                {selectedPackage ? selectedPackage.name : t('governance.evidence.detailTitle')}
              </h2>
            </CardHeader>
            <CardBody>
              {!selectedPackage ? (
                <p className="text-sm text-muted text-center py-4">{t('governance.evidence.selectPackage')}</p>
              ) : (
                <div className="space-y-4">
                  <div className="text-xs text-muted space-y-1">
                    <p>{t('governance.evidence.scope')}: <span className="text-body">{selectedPackage.scope}</span></p>
                    <p>{t('governance.evidence.items')}: <span className="text-body">{selectedPackage.itemCount}</span></p>
                    <p>{t('governance.evidence.createdBy')}: <span className="text-body">{selectedPackage.createdBy}</span></p>
                    {selectedPackage.sealedAt && (
                      <p>{t('governance.evidence.sealedAt')}: <span className="text-body">{new Date(selectedPackage.sealedAt).toLocaleDateString()}</span></p>
                    )}
                  </div>
                  <div className="flex flex-wrap gap-1">
                    {selectedPackage.includedTypes.map(t2 => (
                      <span key={t2} className="text-[10px] px-2 py-0.5 rounded-full bg-accent/10 text-accent">{t2}</span>
                    ))}
                  </div>
                  <div className="border-t border-edge pt-3">
                    <p className="text-xs font-semibold text-heading mb-2">{t('governance.evidence.evidenceItems')}</p>
                    <p className="text-xs text-muted text-center py-4">{t('governance.evidence.itemsNotAvailable', 'Evidence items are not yet available from the API.')}</p>
                  </div>
                  <button className="w-full mt-2 px-3 py-2 text-xs font-medium rounded-md bg-accent/10 text-accent border border-accent/20 hover:bg-accent/20 transition-colors flex items-center justify-center gap-2">
                    <Download size={14} />
                    {t('governance.evidence.exportPackage')}
                  </button>
                </div>
              )}
            </CardBody>
          </Card>
        </div>
      </div>
    </PageContainer>
  );
}
