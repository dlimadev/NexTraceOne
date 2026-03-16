import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Link, useParams } from 'react-router-dom';
import {
  Package, ArrowLeft, Info, List, Globe, History, BarChart3,
  FileCheck, Play, Shield, ShieldCheck, ShieldAlert,
} from 'lucide-react';
import { Card, CardBody, CardHeader } from '../../../components/Card';
import { Badge } from '../../../components/Badge';
import { StatCard } from '../../../components/StatCard';

/**
 * Tipos locais para detalhe de Governance Pack — alinhados com o backend.
 */
type PackStatus = 'Draft' | 'Published' | 'Deprecated';
type EnforcementMode = 'HardEnforce' | 'SoftEnforce' | 'Advisory' | 'Disabled';
type ScopeType = 'Team' | 'Domain' | 'Service' | 'Environment' | 'Global';

interface RuleBinding {
  ruleId: string;
  ruleName: string;
  description: string;
  enforcementMode: EnforcementMode;
  severity: string;
}

interface PackScope {
  scopeId: string;
  scopeName: string;
  scopeType: ScopeType;
  enforcementMode: EnforcementMode;
}

interface PackVersion {
  version: string;
  publishedAt: string;
  changeDescription: string;
}

interface CoverageEntry {
  scopeName: string;
  compliancePercent: number;
}

interface Waiver {
  waiverId: string;
  ruleName: string;
  scope: string;
  justification: string;
  status: string;
  expiresAt: string;
}

interface PackDetail {
  packId: string;
  name: string;
  displayName: string;
  description: string;
  category: string;
  status: PackStatus;
  version: string;
  scopeCount: number;
  ruleCount: number;
  createdAt: string;
  updatedAt: string;
  rules: RuleBinding[];
  scopes: PackScope[];
  versions: PackVersion[];
  coverage: CoverageEntry[];
  waivers: Waiver[];
}

/**
 * Dados simulados para detalhe do pack — alinhados com o backend.
 */
const mockPackDetails: Record<string, PackDetail> = {
  'contracts-baseline': {
    packId: 'contracts-baseline', name: 'contracts-baseline', displayName: 'Contracts Baseline',
    description: 'Baseline governance rules for API and event contract management, versioning, and compatibility',
    category: 'Contracts', status: 'Published', version: '2.1.0', scopeCount: 12, ruleCount: 8,
    createdAt: new Date(Date.now() - 120 * 86400000).toISOString(),
    updatedAt: new Date(Date.now() - 5 * 86400000).toISOString(),
    rules: [
      { ruleId: 'r-001', ruleName: 'CONTRACT-EXISTS', description: 'Every externally consumed service must have a published contract', enforcementMode: 'HardEnforce', severity: 'High' },
      { ruleId: 'r-002', ruleName: 'SEMVER-REQUIRED', description: 'All contracts must follow semantic versioning', enforcementMode: 'SoftEnforce', severity: 'Medium' },
      { ruleId: 'r-003', ruleName: 'BREAKING-CHANGE-REVIEW', description: 'Breaking changes require approval workflow', enforcementMode: 'HardEnforce', severity: 'Critical' },
      { ruleId: 'r-004', ruleName: 'SCHEMA-VALIDATION', description: 'Contract schemas must pass validation', enforcementMode: 'HardEnforce', severity: 'High' },
    ],
    scopes: [
      { scopeId: 's-001', scopeName: 'Production', scopeType: 'Environment', enforcementMode: 'HardEnforce' },
      { scopeId: 's-002', scopeName: 'Staging', scopeType: 'Environment', enforcementMode: 'SoftEnforce' },
      { scopeId: 's-003', scopeName: 'Platform Team', scopeType: 'Team', enforcementMode: 'HardEnforce' },
      { scopeId: 's-004', scopeName: 'Payments Domain', scopeType: 'Domain', enforcementMode: 'HardEnforce' },
    ],
    versions: [
      { version: '2.1.0', publishedAt: new Date(Date.now() - 5 * 86400000).toISOString(), changeDescription: 'Added schema validation rule' },
      { version: '2.0.0', publishedAt: new Date(Date.now() - 30 * 86400000).toISOString(), changeDescription: 'Breaking change review now mandatory' },
      { version: '1.0.0', publishedAt: new Date(Date.now() - 120 * 86400000).toISOString(), changeDescription: 'Initial release' },
    ],
    coverage: [
      { scopeName: 'Production', compliancePercent: 92 },
      { scopeName: 'Staging', compliancePercent: 78 },
      { scopeName: 'Platform Team', compliancePercent: 95 },
      { scopeName: 'Payments Domain', compliancePercent: 88 },
    ],
    waivers: [
      { waiverId: 'w-001', ruleName: 'SEMVER-REQUIRED', scope: 'Legacy Service A', justification: 'Legacy service migration in progress', status: 'Approved', expiresAt: new Date(Date.now() + 30 * 86400000).toISOString() },
    ],
  },
};

const defaultPack: PackDetail = {
  packId: 'unknown', name: 'unknown', displayName: 'Unknown Pack',
  description: 'Pack details not found', category: 'General', status: 'Draft',
  version: '0.0.0', scopeCount: 0, ruleCount: 0,
  createdAt: new Date().toISOString(), updatedAt: new Date().toISOString(),
  rules: [], scopes: [], versions: [], coverage: [], waivers: [],
};

type TabKey = 'overview' | 'rules' | 'scopes' | 'versions' | 'coverage' | 'waivers' | 'simulation';

const enforcementBadge = (mode: EnforcementMode): 'danger' | 'warning' | 'info' | 'default' => {
  switch (mode) {
    case 'HardEnforce': return 'danger';
    case 'SoftEnforce': return 'warning';
    case 'Advisory': return 'info';
    default: return 'default';
  }
};

const statusBadge = (st: PackStatus): 'success' | 'warning' | 'default' => {
  switch (st) {
    case 'Published': return 'success';
    case 'Draft': return 'warning';
    case 'Deprecated': return 'default';
  }
};

/**
 * Página de detalhe de um Governance Pack — tabs com regras, scopes, versões, cobertura e waivers.
 */
export function GovernancePackDetailPage() {
  const { t } = useTranslation();
  const { packId } = useParams<{ packId: string }>();
  const [activeTab, setActiveTab] = useState<TabKey>('overview');

  const pack = (packId && mockPackDetails[packId]) ? mockPackDetails[packId] : defaultPack;

  const tabs: { key: TabKey; labelKey: string; icon: React.ReactNode }[] = [
    { key: 'overview', labelKey: 'governancePacks.detail.tabOverview', icon: <Info size={14} /> },
    { key: 'rules', labelKey: 'governancePacks.detail.tabRules', icon: <List size={14} /> },
    { key: 'scopes', labelKey: 'governancePacks.detail.tabScopes', icon: <Globe size={14} /> },
    { key: 'versions', labelKey: 'governancePacks.detail.tabVersions', icon: <History size={14} /> },
    { key: 'coverage', labelKey: 'governancePacks.detail.tabCoverage', icon: <BarChart3 size={14} /> },
    { key: 'waivers', labelKey: 'governancePacks.detail.tabWaivers', icon: <FileCheck size={14} /> },
    { key: 'simulation', labelKey: 'governancePacks.detail.tabSimulation', icon: <Play size={14} /> },
  ];

  return (
    <div className="p-6 lg:p-8 animate-fade-in">
      {/* Back navigation */}
      <Link to="/governance/packs" className="inline-flex items-center gap-1 text-sm text-muted hover:text-accent transition-colors mb-4">
        <ArrowLeft size={14} />
        {t('governancePacks.detail.backToPacks')}
      </Link>

      {/* Header */}
      <div className="mb-6">
        <div className="flex items-center gap-3 mb-2">
          <Package size={24} className="text-accent" />
          <h1 className="text-2xl font-bold text-heading">{pack.displayName}</h1>
          <Badge variant={statusBadge(pack.status)}>{t(`governancePacks.status.${pack.status}`)}</Badge>
        </div>
        <p className="text-muted mt-1">{pack.description}</p>
      </div>

      {/* Stats */}
      <div className="grid grid-cols-2 md:grid-cols-4 gap-4 mb-6">
        <StatCard title={t('governancePacks.detail.version')} value={`v${pack.version}`} icon={<History size={20} />} color="text-accent" />
        <StatCard title={t('governancePacks.rules')} value={pack.ruleCount} icon={<Shield size={20} />} color="text-info" />
        <StatCard title={t('governancePacks.scopes')} value={pack.scopeCount} icon={<Globe size={20} />} color="text-success" />
        <StatCard title={t('governancePacks.detail.waiverCount')} value={pack.waivers.length} icon={<FileCheck size={20} />} color="text-warning" />
      </div>

      {/* Tabs */}
      <div className="flex flex-wrap items-center gap-1 mb-6 border-b border-edge">
        {tabs.map(tab => (
          <button
            key={tab.key}
            onClick={() => setActiveTab(tab.key)}
            className={`flex items-center gap-1.5 px-4 py-2.5 text-xs font-medium border-b-2 transition-colors ${
              activeTab === tab.key
                ? 'border-accent text-accent'
                : 'border-transparent text-muted hover:text-body'
            }`}
          >
            {tab.icon}
            {t(tab.labelKey)}
          </button>
        ))}
      </div>

      {/* Tab content */}
      {activeTab === 'overview' && (
        <Card>
          <CardHeader>
            <h2 className="text-sm font-semibold text-heading flex items-center gap-2">
              <Info size={16} className="text-accent" />
              {t('governancePacks.detail.overviewTitle')}
            </h2>
          </CardHeader>
          <CardBody>
            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
              <div>
                <p className="text-xs text-faded mb-1">{t('governancePacks.detail.packName')}</p>
                <p className="text-sm text-heading font-mono">{pack.name}</p>
              </div>
              <div>
                <p className="text-xs text-faded mb-1">{t('governancePacks.detail.category')}</p>
                <p className="text-sm text-heading">{pack.category}</p>
              </div>
              <div>
                <p className="text-xs text-faded mb-1">{t('governancePacks.detail.status')}</p>
                <Badge variant={statusBadge(pack.status)}>{t(`governancePacks.status.${pack.status}`)}</Badge>
              </div>
              <div>
                <p className="text-xs text-faded mb-1">{t('governancePacks.detail.version')}</p>
                <p className="text-sm text-heading">v{pack.version}</p>
              </div>
              <div>
                <p className="text-xs text-faded mb-1">{t('governancePacks.detail.createdAt')}</p>
                <p className="text-sm text-heading">{new Date(pack.createdAt).toLocaleDateString()}</p>
              </div>
              <div>
                <p className="text-xs text-faded mb-1">{t('governancePacks.detail.updatedAt')}</p>
                <p className="text-sm text-heading">{new Date(pack.updatedAt).toLocaleDateString()}</p>
              </div>
            </div>
          </CardBody>
        </Card>
      )}

      {activeTab === 'rules' && (
        <Card>
          <CardHeader>
            <h2 className="text-sm font-semibold text-heading flex items-center gap-2">
              <ShieldCheck size={16} className="text-accent" />
              {t('governancePacks.detail.rulesTitle')}
            </h2>
            <p className="text-xs text-muted mt-1">{t('governancePacks.detail.rulesDescription')}</p>
          </CardHeader>
          <CardBody className="p-0">
            <div className="divide-y divide-edge">
              {pack.rules.length === 0 ? (
                <div className="p-8 text-center text-muted text-sm">{t('common.noResults')}</div>
              ) : (
                pack.rules.map(rule => (
                  <div key={rule.ruleId} className="flex items-start gap-4 px-4 py-3 hover:bg-hover transition-colors">
                    <div className="mt-1 text-muted"><Shield size={14} /></div>
                    <div className="min-w-0 flex-1">
                      <div className="flex items-center gap-2 mb-1 flex-wrap">
                        <span className="text-sm font-medium text-heading">{rule.ruleName}</span>
                        <Badge variant={enforcementBadge(rule.enforcementMode)}>
                          {t(`governancePacks.detail.enforcement.${rule.enforcementMode}`)}
                        </Badge>
                        <Badge variant="default">{rule.severity}</Badge>
                      </div>
                      <p className="text-xs text-muted">{rule.description}</p>
                    </div>
                  </div>
                ))
              )}
            </div>
          </CardBody>
        </Card>
      )}

      {activeTab === 'scopes' && (
        <Card>
          <CardHeader>
            <h2 className="text-sm font-semibold text-heading flex items-center gap-2">
              <Globe size={16} className="text-accent" />
              {t('governancePacks.detail.scopesTitle')}
            </h2>
            <p className="text-xs text-muted mt-1">{t('governancePacks.detail.scopesDescription')}</p>
          </CardHeader>
          <CardBody className="p-0">
            <div className="divide-y divide-edge">
              {pack.scopes.length === 0 ? (
                <div className="p-8 text-center text-muted text-sm">{t('common.noResults')}</div>
              ) : (
                pack.scopes.map(scope => (
                  <div key={scope.scopeId} className="flex items-center gap-4 px-4 py-3 hover:bg-hover transition-colors">
                    <Globe size={14} className="text-muted" />
                    <div className="min-w-0 flex-1">
                      <span className="text-sm font-medium text-heading">{scope.scopeName}</span>
                      <div className="flex items-center gap-2 mt-1">
                        <Badge variant="info">{t(`governancePacks.detail.scopeType.${scope.scopeType}`)}</Badge>
                        <Badge variant={enforcementBadge(scope.enforcementMode)}>
                          {t(`governancePacks.detail.enforcement.${scope.enforcementMode}`)}
                        </Badge>
                      </div>
                    </div>
                  </div>
                ))
              )}
            </div>
          </CardBody>
        </Card>
      )}

      {activeTab === 'versions' && (
        <Card>
          <CardHeader>
            <h2 className="text-sm font-semibold text-heading flex items-center gap-2">
              <History size={16} className="text-accent" />
              {t('governancePacks.detail.versionsTitle')}
            </h2>
          </CardHeader>
          <CardBody className="p-0">
            <div className="divide-y divide-edge">
              {pack.versions.length === 0 ? (
                <div className="p-8 text-center text-muted text-sm">{t('common.noResults')}</div>
              ) : (
                pack.versions.map(v => (
                  <div key={v.version} className="flex items-start gap-4 px-4 py-3 hover:bg-hover transition-colors">
                    <History size={14} className="text-muted mt-1" />
                    <div className="min-w-0 flex-1">
                      <div className="flex items-center gap-2 mb-1">
                        <span className="text-sm font-medium text-heading font-mono">v{v.version}</span>
                        <span className="text-xs text-faded">{new Date(v.publishedAt).toLocaleDateString()}</span>
                      </div>
                      <p className="text-xs text-muted">{v.changeDescription}</p>
                    </div>
                  </div>
                ))
              )}
            </div>
          </CardBody>
        </Card>
      )}

      {activeTab === 'coverage' && (
        <Card>
          <CardHeader>
            <h2 className="text-sm font-semibold text-heading flex items-center gap-2">
              <BarChart3 size={16} className="text-accent" />
              {t('governancePacks.detail.coverageTitle')}
            </h2>
            <p className="text-xs text-muted mt-1">{t('governancePacks.detail.coverageDescription')}</p>
          </CardHeader>
          <CardBody>
            <div className="space-y-4">
              {pack.coverage.length === 0 ? (
                <div className="p-8 text-center text-muted text-sm">{t('common.noResults')}</div>
              ) : (
                pack.coverage.map(entry => (
                  <div key={entry.scopeName}>
                    <div className="flex items-center justify-between mb-1">
                      <span className="text-sm text-heading">{entry.scopeName}</span>
                      <span className="text-xs font-medium text-heading">{entry.compliancePercent}%</span>
                    </div>
                    <div className="w-full bg-elevated rounded-full h-2">
                      <div
                        className={`h-2 rounded-full transition-all ${
                          entry.compliancePercent >= 90 ? 'bg-success' :
                          entry.compliancePercent >= 70 ? 'bg-warning' : 'bg-critical'
                        }`}
                        style={{ width: `${entry.compliancePercent}%` }}
                      />
                    </div>
                  </div>
                ))
              )}
            </div>
          </CardBody>
        </Card>
      )}

      {activeTab === 'waivers' && (
        <Card>
          <CardHeader>
            <h2 className="text-sm font-semibold text-heading flex items-center gap-2">
              <FileCheck size={16} className="text-accent" />
              {t('governancePacks.detail.waiversTitle')}
            </h2>
          </CardHeader>
          <CardBody className="p-0">
            <div className="divide-y divide-edge">
              {pack.waivers.length === 0 ? (
                <div className="p-8 text-center text-muted text-sm">{t('common.noResults')}</div>
              ) : (
                pack.waivers.map(w => (
                  <div key={w.waiverId} className="flex items-start gap-4 px-4 py-3 hover:bg-hover transition-colors">
                    <ShieldAlert size={14} className="text-warning mt-1" />
                    <div className="min-w-0 flex-1">
                      <div className="flex items-center gap-2 mb-1 flex-wrap">
                        <span className="text-sm font-medium text-heading">{w.ruleName}</span>
                        <Badge variant="warning">{w.status}</Badge>
                        <span className="text-xs text-faded">{w.scope}</span>
                      </div>
                      <p className="text-xs text-muted mb-1">{w.justification}</p>
                      <span className="text-xs text-faded">{t('governancePacks.detail.expiresAt')}: {new Date(w.expiresAt).toLocaleDateString()}</span>
                    </div>
                  </div>
                ))
              )}
            </div>
          </CardBody>
        </Card>
      )}

      {activeTab === 'simulation' && (
        <Card>
          <CardHeader>
            <h2 className="text-sm font-semibold text-heading flex items-center gap-2">
              <Play size={16} className="text-accent" />
              {t('governancePacks.detail.simulationTitle')}
            </h2>
            <p className="text-xs text-muted mt-1">{t('governancePacks.detail.simulationDescription')}</p>
          </CardHeader>
          <CardBody>
            <Link
              to={`/governance/packs/${pack.packId}/simulate`}
              className="inline-flex items-center gap-2 px-4 py-2 text-sm font-medium rounded-md bg-accent text-white hover:bg-accent-hover transition-colors"
            >
              <Play size={14} />
              {t('governancePacks.detail.runSimulation')}
            </Link>
          </CardBody>
        </Card>
      )}
    </div>
  );
}
