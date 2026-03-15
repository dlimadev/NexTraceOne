import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import {
  Package, Search, Lock, Download, FileCheck, FileText,
  Clock, Shield, Bot, AlertTriangle, ClipboardList,
} from 'lucide-react';
import { Card, CardBody, CardHeader } from '../../../components/Card';
import { Badge } from '../../../components/Badge';
import { StatCard } from '../../../components/StatCard';
import type {
  EvidencePackageDto, EvidencePackageListResponse, EvidencePackageStatusType,
  EvidenceItemDto, EvidenceTypeValue,
} from '../../../types';

/**
 * Dados simulados de pacotes de evidência — alinhados com o backend ListEvidencePackages.
 * Em produção, virão da API /api/v1/evidence/packages.
 */
const mockPackages: EvidencePackageListResponse = {
  totalPackages: 5,
  sealedCount: 3,
  exportedCount: 1,
  draftCount: 1,
  packages: [
    {
      packageId: 'evp-001', name: 'Q1 2026 Compliance Evidence',
      description: 'Quarterly compliance evidence package for production services',
      scope: 'quarterly-review', status: 'Sealed', itemCount: 24,
      includedTypes: ['Approvals', 'Change History', 'Contract Publications', 'Compliance Results'],
      createdBy: 'auditor@nextraceone.com',
      createdAt: new Date(Date.now() - 15 * 86400000).toISOString(),
      sealedAt: new Date(Date.now() - 14 * 86400000).toISOString(),
    },
    {
      packageId: 'evp-002', name: 'Payment Gateway Security Review',
      description: 'Security compliance evidence for PCI-DSS audit',
      scope: 'security-audit', status: 'Exported', itemCount: 18,
      includedTypes: ['Security Reviews', 'Change Validations', 'AI Usage Records', 'Mitigation Records'],
      createdBy: 'security@nextraceone.com',
      createdAt: new Date(Date.now() - 30 * 86400000).toISOString(),
      sealedAt: new Date(Date.now() - 28 * 86400000).toISOString(),
    },
    {
      packageId: 'evp-003', name: 'AI Governance Audit Pack',
      description: 'Evidence package for AI model usage and policy compliance',
      scope: 'ai-governance', status: 'Sealed', itemCount: 35,
      includedTypes: ['AI Usage Records', 'Policy Decisions', 'Model Registry Snapshots', 'Token Usage'],
      createdBy: 'ai-governance@nextraceone.com',
      createdAt: new Date(Date.now() - 7 * 86400000).toISOString(),
      sealedAt: new Date(Date.now() - 6 * 86400000).toISOString(),
    },
    {
      packageId: 'evp-004', name: 'Change Governance March 2026',
      description: 'Change validation and blast radius evidence for March releases',
      scope: 'change-governance', status: 'Draft', itemCount: 12,
      includedTypes: ['Change History', 'Blast Radius', 'Approval History', 'Rollback Records'],
      createdBy: 'release-mgr@nextraceone.com',
      createdAt: new Date(Date.now() - 2 * 86400000).toISOString(),
      sealedAt: null,
    },
    {
      packageId: 'evp-005', name: 'Incident Mitigation Evidence',
      description: 'Post-incident mitigation and resolution evidence pack',
      scope: 'incident-review', status: 'Sealed', itemCount: 8,
      includedTypes: ['Mitigation Records', 'Approval History', 'Post-mortem References', 'Audit Trails'],
      createdBy: 'ops-lead@nextraceone.com',
      createdAt: new Date(Date.now() - 20 * 86400000).toISOString(),
      sealedAt: new Date(Date.now() - 19 * 86400000).toISOString(),
    },
  ],
};

/** Itens simulados para visualização de detalhe. */
const mockItems: EvidenceItemDto[] = [
  { itemId: 'evi-001', type: 'Approval', title: 'Change Approval #CH-2026-0142',
    description: 'Production deployment approved by Tech Lead', sourceModule: 'change-governance',
    referenceId: 'CH-2026-0142', recordedBy: 'techlead@nextraceone.com',
    recordedAt: new Date(Date.now() - 16 * 86400000).toISOString() },
  { itemId: 'evi-002', type: 'ChangeHistory', title: 'Release v3.2.0 Deployment',
    description: 'Successful production deployment with blast radius assessment', sourceModule: 'change-governance',
    referenceId: 'REL-2026-0089', recordedBy: 'ci-system',
    recordedAt: new Date(Date.now() - 16 * 86400000).toISOString() },
  { itemId: 'evi-003', type: 'ContractPublication', title: 'Order API Contract v2.1.0',
    description: 'Contract published with breaking change notification', sourceModule: 'catalog',
    referenceId: 'CTR-ORDER-API-2.1.0', recordedBy: 'architect@nextraceone.com',
    recordedAt: new Date(Date.now() - 18 * 86400000).toISOString() },
  { itemId: 'evi-004', type: 'ComplianceResult', title: 'Q1 Compliance Check Run',
    description: 'Quarterly compliance evaluation: 78% coverage', sourceModule: 'governance',
    referenceId: 'CHK-RUN-2026-Q1', recordedBy: 'system',
    recordedAt: new Date(Date.now() - 15 * 86400000).toISOString() },
  { itemId: 'evi-005', type: 'AiUsageRecord', title: 'AI Assistant Usage Summary',
    description: 'AI assistant usage within approved policy limits', sourceModule: 'ai-governance',
    referenceId: 'AI-USAGE-2026-03', recordedBy: 'system',
    recordedAt: new Date(Date.now() - 15 * 86400000).toISOString() },
  { itemId: 'evi-006', type: 'MitigationRecord', title: 'Incident INC-2026-0034 Mitigation',
    description: 'Incident resolved with documented mitigation steps', sourceModule: 'operations',
    referenceId: 'INC-2026-0034', recordedBy: 'oncall@nextraceone.com',
    recordedAt: new Date(Date.now() - 20 * 86400000).toISOString() },
];

type StatusFilter = 'all' | EvidencePackageStatusType;

const statusBadge = (st: EvidencePackageStatusType): 'success' | 'warning' | 'info' | 'default' => {
  switch (st) {
    case 'Sealed': return 'success';
    case 'Exported': return 'info';
    case 'Draft': return 'warning';
    default: return 'default';
  }
};

const evidenceIcon = (type: EvidenceTypeValue) => {
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

  const d = mockPackages;

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
    <div className="p-6 lg:p-8 animate-fade-in">
      {/* Header */}
      <div className="mb-6">
        <h1 className="text-2xl font-bold text-heading">{t('governance.evidence.title')}</h1>
        <p className="text-muted mt-1">{t('governance.evidence.subtitle')}</p>
      </div>

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
                    <div className="space-y-2">
                      {mockItems.map(item => (
                        <div key={item.itemId} className="flex items-start gap-2 p-2 rounded bg-surface/50">
                          <div className="mt-0.5 text-muted">{evidenceIcon(item.type)}</div>
                          <div className="min-w-0">
                            <p className="text-xs font-medium text-body">{item.title}</p>
                            <p className="text-[10px] text-muted">{item.description}</p>
                            <p className="text-[10px] text-faded">{item.referenceId} · {item.recordedBy}</p>
                          </div>
                        </div>
                      ))}
                    </div>
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
    </div>
  );
}
