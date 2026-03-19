import { useState, useEffect } from 'react';
import { useTranslation } from 'react-i18next';
import { Link, useParams } from 'react-router-dom';
import {
  Package, ArrowLeft, Info, List, Globe, History, BarChart3,
  FileCheck, Play, Shield, ShieldCheck, ShieldAlert, Loader2, AlertTriangle,
} from 'lucide-react';
import { Card, CardBody, CardHeader } from '../../../components/Card';
import { Badge } from '../../../components/Badge';
import { StatCard } from '../../../components/StatCard';
import { PageContainer } from '../../../components/shell';
import { PageLoadingState } from '../../../components/PageLoadingState';
import { PageErrorState } from '../../../components/PageErrorState';
import { organizationGovernanceApi } from '../api/organizationGovernance';
import type { GovernancePackDetail, EnforcementMode, GovernancePackStatus } from '../../../types';

type TabKey = 'overview' | 'rules' | 'scopes' | 'versions' | 'simulation';

const enforcementBadge = (mode: EnforcementMode): 'danger' | 'warning' | 'info' | 'default' => {
  switch (mode) {
    case 'HardEnforce': return 'danger';
    case 'SoftEnforce': return 'warning';
    case 'Advisory': return 'info';
    default: return 'default';
  }
};

const statusBadge = (st: GovernancePackStatus): 'success' | 'warning' | 'default' => {
  switch (st) {
    case 'Published': return 'success';
    case 'Draft': return 'warning';
    case 'Deprecated': return 'default';
    case 'Archived': return 'default';
  }
};

/**
 * Página de detalhe de um Governance Pack — tabs com regras, scopes e versões.
 */
export function GovernancePackDetailPage() {
  const { t } = useTranslation();
  const { packId } = useParams<{ packId: string }>();
  const [activeTab, setActiveTab] = useState<TabKey>('overview');
  const [pack, setPack] = useState<GovernancePackDetail | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (!packId) return;
    let cancelled = false;
    setLoading(true);
    setError(null);

    organizationGovernanceApi.getGovernancePack(packId)
      .then((response) => {
        if (!cancelled) {
          setPack(response.pack);
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
  }, [packId, t]);

  const tabs: { key: TabKey; labelKey: string; icon: React.ReactNode }[] = [
    { key: 'overview', labelKey: 'governancePacks.detail.tabOverview', icon: <Info size={14} /> },
    { key: 'rules', labelKey: 'governancePacks.detail.tabRules', icon: <List size={14} /> },
    { key: 'scopes', labelKey: 'governancePacks.detail.tabScopes', icon: <Globe size={14} /> },
    { key: 'versions', labelKey: 'governancePacks.detail.tabVersions', icon: <History size={14} /> },
    { key: 'simulation', labelKey: 'governancePacks.detail.tabSimulation', icon: <Play size={14} /> },
  ];

  if (loading) {
    return (
      <PageContainer>
        <Link to="/governance/packs" className="inline-flex items-center gap-1 text-sm text-muted hover:text-accent transition-colors mb-4">
          <ArrowLeft size={14} />
          {t('governancePacks.detail.backToPacks')}
        </Link>
        <PageLoadingState />
      </PageContainer>
    );
  }

  if (error || !pack) {
    return (
      <PageContainer>
        <Link to="/governance/packs" className="inline-flex items-center gap-1 text-sm text-muted hover:text-accent transition-colors mb-4">
          <ArrowLeft size={14} />
          {t('governancePacks.detail.backToPacks')}
        </Link>
        <PageErrorState message={error || t('governancePacks.detail.notFound')} />
      </PageContainer>
    );
  }

  return (
    <PageContainer>
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
        <StatCard title={t('governancePacks.detail.version')} value={`v${pack.currentVersion}`} icon={<History size={20} />} color="text-accent" />
        <StatCard title={t('governancePacks.rules')} value={pack.ruleCount} icon={<Shield size={20} />} color="text-info" />
        <StatCard title={t('governancePacks.scopes')} value={pack.scopeCount} icon={<Globe size={20} />} color="text-success" />
        <StatCard title={t('governancePacks.detail.versions')} value={pack.recentVersions.length} icon={<History size={20} />} color="text-warning" />
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
                <p className="text-sm text-heading">v{pack.currentVersion}</p>
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
                        <Badge variant={enforcementBadge(rule.defaultEnforcementMode)}>
                          {t(`governancePacks.detail.enforcement.${rule.defaultEnforcementMode}`)}
                        </Badge>
                        {rule.isRequired && <Badge variant="danger">{t('governancePacks.detail.required')}</Badge>}
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
                pack.scopes.map((scope, idx) => (
                  <div key={`${scope.scopeType}-${scope.scopeValue}-${idx}`} className="flex items-center gap-4 px-4 py-3 hover:bg-hover transition-colors">
                    <Globe size={14} className="text-muted" />
                    <div className="min-w-0 flex-1">
                      <span className="text-sm font-medium text-heading">{scope.scopeValue}</span>
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
              {pack.recentVersions.length === 0 ? (
                <div className="p-8 text-center text-muted text-sm">{t('common.noResults')}</div>
              ) : (
                pack.recentVersions.map(v => (
                  <div key={v.versionId} className="flex items-start gap-4 px-4 py-3 hover:bg-hover transition-colors">
                    <History size={14} className="text-muted mt-1" />
                    <div className="min-w-0 flex-1">
                      <div className="flex items-center gap-2 mb-1">
                        <span className="text-sm font-medium text-heading font-mono">v{v.version}</span>
                        <span className="text-xs text-faded">{new Date(v.createdAt).toLocaleDateString()}</span>
                        <span className="text-xs text-muted">{t('governancePacks.detail.createdBy')}: {v.createdBy}</span>
                      </div>
                      <p className="text-xs text-muted">{v.ruleCount} {t('governancePacks.rules')}</p>
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
    </PageContainer>
  );
}
