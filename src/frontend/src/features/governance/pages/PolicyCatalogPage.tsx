import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import {
  Shield, Search, ShieldCheck, ShieldAlert, FileText, AlertTriangle,
  Settings, BookOpen, Bot, Lock,
} from 'lucide-react';
import { Card, CardBody, CardHeader } from '../../../components/Card';
import { Badge } from '../../../components/Badge';
import { StatCard } from '../../../components/StatCard';
import type { PolicyDto, PolicyListResponse, PolicyCategoryType, PolicyStatusType } from '../../../types';
import { PageContainer } from '../../../components/shell';

/**
 * Dados simulados de políticas — alinhados com o backend ListPolicies.
 * Em produção, virão da API /api/v1/policies.
 */
const mockPolicies: PolicyListResponse = {
  totalPolicies: 10,
  activeCount: 9,
  draftCount: 1,
  policies: [
    {
      policyId: 'pol-001', name: 'SVC-OWNER-REQ', displayName: 'Service Owner Required',
      description: 'Every service must have a designated owner and team assignment',
      category: 'ServiceGovernance', scope: 'Services', status: 'Active',
      severity: 'High', enforcementMode: 'HardEnforce',
      effectiveEnvironments: ['Production', 'Staging'], affectedAssetsCount: 38, violationCount: 4,
      createdAt: new Date(Date.now() - 90 * 86400000).toISOString(),
    },
    {
      policyId: 'pol-002', name: 'CONTRACT-EXISTS', displayName: 'Contract Definition Required',
      description: 'All externally consumed services must have a published contract',
      category: 'ContractGovernance', scope: 'Contracts', status: 'Active',
      severity: 'High', enforcementMode: 'SoftEnforce',
      effectiveEnvironments: ['Production'], affectedAssetsCount: 42, violationCount: 7,
      createdAt: new Date(Date.now() - 85 * 86400000).toISOString(),
    },
    {
      policyId: 'pol-003', name: 'RUNBOOK-PRESENT', displayName: 'Runbook Availability',
      description: 'Critical and high-criticality services must have an operational runbook',
      category: 'OperationalReadiness', scope: 'Operations', status: 'Active',
      severity: 'Medium', enforcementMode: 'SoftEnforce',
      effectiveEnvironments: ['Production'], affectedAssetsCount: 28, violationCount: 12,
      createdAt: new Date(Date.now() - 60 * 86400000).toISOString(),
    },
    {
      policyId: 'pol-004', name: 'CHANGE-VALIDATION', displayName: 'Change Validation Required',
      description: 'All production changes must pass validation and blast radius assessment',
      category: 'ChangeGovernance', scope: 'Changes', status: 'Active',
      severity: 'Critical', enforcementMode: 'HardEnforce',
      effectiveEnvironments: ['Production'], affectedAssetsCount: 156, violationCount: 3,
      createdAt: new Date(Date.now() - 120 * 86400000).toISOString(),
    },
    {
      policyId: 'pol-005', name: 'AI-MODEL-POLICY', displayName: 'AI Model Usage Policy',
      description: 'AI model usage must comply with approved model registry and access policies',
      category: 'AiGovernance', scope: 'AI', status: 'Active',
      severity: 'High', enforcementMode: 'HardEnforce',
      effectiveEnvironments: ['Production', 'Staging', 'Development'], affectedAssetsCount: 89, violationCount: 2,
      createdAt: new Date(Date.now() - 45 * 86400000).toISOString(),
    },
    {
      policyId: 'pol-006', name: 'DOC-STANDARDS', displayName: 'Documentation Standards',
      description: 'Services must have up-to-date technical documentation and API references',
      category: 'DocumentationStandards', scope: 'Knowledge', status: 'Active',
      severity: 'Medium', enforcementMode: 'Advisory',
      effectiveEnvironments: ['Production', 'Staging'], affectedAssetsCount: 42, violationCount: 15,
      createdAt: new Date(Date.now() - 70 * 86400000).toISOString(),
    },
    {
      policyId: 'pol-007', name: 'DEP-MAPPING', displayName: 'Dependency Mapping Required',
      description: 'All services must have dependencies mapped in the service catalog',
      category: 'ServiceGovernance', scope: 'Services', status: 'Active',
      severity: 'Medium', enforcementMode: 'SoftEnforce',
      effectiveEnvironments: ['Production'], affectedAssetsCount: 42, violationCount: 5,
      createdAt: new Date(Date.now() - 55 * 86400000).toISOString(),
    },
    {
      policyId: 'pol-008', name: 'INCIDENT-EVIDENCE', displayName: 'Incident Mitigation Evidence',
      description: 'Incident resolution must include mitigation evidence and post-mortem when applicable',
      category: 'OperationalReadiness', scope: 'Operations', status: 'Draft',
      severity: 'High', enforcementMode: 'Advisory',
      effectiveEnvironments: ['Production'], affectedAssetsCount: 0, violationCount: 0,
      createdAt: new Date(Date.now() - 10 * 86400000).toISOString(),
    },
    {
      policyId: 'pol-009', name: 'VERSION-CONTROL', displayName: 'Contract Versioning Required',
      description: 'Contracts must follow semantic versioning with proper changelog',
      category: 'ContractGovernance', scope: 'Contracts', status: 'Active',
      severity: 'Medium', enforcementMode: 'SoftEnforce',
      effectiveEnvironments: ['Production', 'Staging'], affectedAssetsCount: 35, violationCount: 8,
      createdAt: new Date(Date.now() - 80 * 86400000).toISOString(),
    },
    {
      policyId: 'pol-010', name: 'SEC-REVIEW', displayName: 'Security Review Required',
      description: 'Services handling sensitive data must pass security review before production deployment',
      category: 'SecurityCompliance', scope: 'Security', status: 'Active',
      severity: 'Critical', enforcementMode: 'HardEnforce',
      effectiveEnvironments: ['Production'], affectedAssetsCount: 18, violationCount: 1,
      createdAt: new Date(Date.now() - 100 * 86400000).toISOString(),
    },
  ],
};

type CategoryFilter = 'all' | PolicyCategoryType;
type StatusFilter = 'all' | PolicyStatusType;

const severityBadge = (sev: string): 'danger' | 'warning' | 'info' | 'default' => {
  switch (sev) {
    case 'Critical': return 'danger';
    case 'High': return 'warning';
    case 'Medium': return 'info';
    default: return 'default';
  }
};

const statusBadge = (st: string): 'success' | 'warning' | 'default' => {
  switch (st) {
    case 'Active': return 'success';
    case 'Draft': return 'warning';
    default: return 'default';
  }
};

const categoryIcon = (cat: PolicyCategoryType) => {
  switch (cat) {
    case 'ServiceGovernance': return <Settings size={14} />;
    case 'ContractGovernance': return <FileText size={14} />;
    case 'ChangeGovernance': return <ShieldCheck size={14} />;
    case 'OperationalReadiness': return <AlertTriangle size={14} />;
    case 'AiGovernance': return <Bot size={14} />;
    case 'SecurityCompliance': return <Lock size={14} />;
    case 'DocumentationStandards': return <BookOpen size={14} />;
    default: return <Shield size={14} />;
  }
};

/**
 * Página do Policy Catalog — catálogo de políticas de governança enterprise.
 * Parte do módulo Governance do NexTraceOne.
 */
export function PolicyCatalogPage() {
  const { t } = useTranslation();
  const [categoryFilter, setCategoryFilter] = useState<CategoryFilter>('all');
  const [statusFilter, setStatusFilter] = useState<StatusFilter>('all');
  const [search, setSearch] = useState('');

  const d = mockPolicies;
  const totalViolations = d.policies.reduce((sum, p) => sum + p.violationCount, 0);

  const filtered = d.policies.filter(p => {
    if (categoryFilter !== 'all' && p.category !== categoryFilter) return false;
    if (statusFilter !== 'all' && p.status !== statusFilter) return false;
    if (search) {
      const q = search.toLowerCase();
      return p.displayName.toLowerCase().includes(q)
        || p.name.toLowerCase().includes(q)
        || p.description.toLowerCase().includes(q);
    }
    return true;
  });

  const categories: { key: CategoryFilter; labelKey: string }[] = [
    { key: 'all', labelKey: 'governance.policies.filterAll' },
    { key: 'ServiceGovernance', labelKey: 'governance.policies.category.ServiceGovernance' },
    { key: 'ContractGovernance', labelKey: 'governance.policies.category.ContractGovernance' },
    { key: 'ChangeGovernance', labelKey: 'governance.policies.category.ChangeGovernance' },
    { key: 'OperationalReadiness', labelKey: 'governance.policies.category.OperationalReadiness' },
    { key: 'AiGovernance', labelKey: 'governance.policies.category.AiGovernance' },
    { key: 'SecurityCompliance', labelKey: 'governance.policies.category.SecurityCompliance' },
    { key: 'DocumentationStandards', labelKey: 'governance.policies.category.DocumentationStandards' },
  ];

  return (
    <PageContainer>
      {/* Header */}
      <div className="mb-6">
        <h1 className="text-2xl font-bold text-heading">{t('governance.policies.title')}</h1>
        <p className="text-muted mt-1">{t('governance.policies.subtitle')}</p>
      </div>

      {/* Stats */}
      <div className="grid grid-cols-2 md:grid-cols-4 gap-4 mb-6">
        <StatCard title={t('governance.policies.totalPolicies')} value={d.totalPolicies} icon={<Shield size={20} />} color="text-accent" />
        <StatCard title={t('governance.policies.active')} value={d.activeCount} icon={<ShieldCheck size={20} />} color="text-success" />
        <StatCard title={t('governance.policies.draft')} value={d.draftCount} icon={<FileText size={20} />} color="text-amber-500" />
        <StatCard title={t('governance.policies.totalViolations')} value={totalViolations} icon={<ShieldAlert size={20} />} color="text-critical" />
      </div>

      {/* Filters */}
      <div className="flex flex-wrap items-center gap-3 mb-4">
        <div className="relative flex-1 max-w-xs">
          <Search size={16} className="absolute left-3 top-1/2 -translate-y-1/2 text-muted" />
          <input
            type="text"
            value={search}
            onChange={e => setSearch(e.target.value)}
            placeholder={t('governance.policies.searchPlaceholder')}
            className="w-full pl-9 pr-3 py-2 text-sm rounded-md bg-surface border border-edge text-body placeholder:text-muted focus:outline-none focus:ring-1 focus:ring-accent"
          />
        </div>
        {(['all', 'Active', 'Draft'] as StatusFilter[]).map(f => (
          <button
            key={f}
            onClick={() => setStatusFilter(f)}
            className={`px-3 py-1.5 text-xs rounded-md border transition-colors ${
              statusFilter === f
                ? 'bg-accent/10 text-accent border-accent/30'
                : 'bg-surface text-muted border-edge hover:text-body'
            }`}
          >
            {f === 'all' ? t('governance.policies.filterAll') : t(`governance.policies.status.${f}`)}
          </button>
        ))}
      </div>

      {/* Category filter */}
      <div className="flex flex-wrap items-center gap-2 mb-6">
        {categories.map(c => (
          <button
            key={c.key}
            onClick={() => setCategoryFilter(c.key)}
            className={`px-3 py-1 text-xs rounded-full border transition-colors ${
              categoryFilter === c.key
                ? 'bg-accent/10 text-accent border-accent/30'
                : 'bg-surface text-muted border-edge hover:text-body'
            }`}
          >
            {t(c.labelKey)}
          </button>
        ))}
      </div>

      {/* Policy list */}
      <Card>
        <CardHeader>
          <h2 className="text-sm font-semibold text-heading flex items-center gap-2">
            <Shield size={16} className="text-accent" />
            {t('governance.policies.catalogTitle')}
          </h2>
          <p className="text-xs text-muted mt-1">{t('governance.policies.catalogDescription')}</p>
        </CardHeader>
        <CardBody className="p-0">
          <div className="divide-y divide-edge">
            {filtered.length === 0 ? (
              <div className="p-8 text-center text-muted text-sm">{t('common.noResults')}</div>
            ) : (
              filtered.map(pol => (
                <div key={pol.policyId} className="flex items-start gap-4 px-4 py-3 hover:bg-hover transition-colors">
                  <div className="mt-1 text-muted">{categoryIcon(pol.category)}</div>
                  <div className="min-w-0 flex-1">
                    <div className="flex items-center gap-2 mb-1 flex-wrap">
                      <span className="text-sm font-medium text-heading">{pol.displayName}</span>
                      <span className="text-xs text-faded font-mono">{pol.name}</span>
                      <Badge variant={statusBadge(pol.status)}>{t(`governance.policies.status.${pol.status}`)}</Badge>
                      <Badge variant={severityBadge(pol.severity)}>{t(`governance.policies.severity.${pol.severity}`)}</Badge>
                      <Badge variant="default">{t(`governance.policies.enforcement.${pol.enforcementMode}`)}</Badge>
                    </div>
                    <p className="text-xs text-muted mb-1">{pol.description}</p>
                    <div className="flex items-center gap-4 text-xs text-faded">
                      <span>{t('governance.policies.scope')}: {pol.scope}</span>
                      <span>{t('governance.policies.environments')}: {pol.effectiveEnvironments.join(', ')}</span>
                      <span>{t('governance.policies.affected')}: {pol.affectedAssetsCount}</span>
                      {pol.violationCount > 0 && (
                        <span className="text-critical">{t('governance.policies.violations')}: {pol.violationCount}</span>
                      )}
                    </div>
                  </div>
                </div>
              ))
            )}
          </div>
        </CardBody>
      </Card>
    </PageContainer>
  );
}
