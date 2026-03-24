import { useState, useMemo, useCallback } from 'react';
import { useTranslation } from 'react-i18next';
import {
  Settings,
  Search,
  Filter,
  ArrowLeftRight,
  Download,
  Upload,
  RotateCcw,
  History,
  Activity,
  Shield,
  Eye,
  ChevronDown,
  ChevronUp,
  Lock,
  AlertTriangle,
  CheckCircle2,
  XCircle,
  Info,
  Layers,
  FileJson,
  RefreshCw,
  ArrowRight,
  Copy,
  Clock,
} from 'lucide-react';
import { Card, CardBody, CardHeader } from '../../../components/Card';
import { Badge } from '../../../components/Badge';
import { PageContainer } from '../../../components/shell';
import { PageHeader } from '../../../components/PageHeader';
import { PageLoadingState } from '../../../components/PageLoadingState';
import { PageErrorState } from '../../../components/PageErrorState';
import {
  useConfigurationDefinitions,
  useEffectiveSettings,
  useAuditHistory,
} from '../hooks/useConfiguration';
import type {
  ConfigurationDefinitionDto,
  EffectiveConfigurationDto,
  ConfigurationScope,
} from '../types';

// ── Domain Navigation ──────────────────────────────────────────────────

type ConfigDomain =
  | 'all'
  | 'instance'
  | 'notifications'
  | 'workflows'
  | 'governance'
  | 'catalog'
  | 'operations'
  | 'ai'
  | 'integrations';

type AdminTab =
  | 'explorer'
  | 'diff'
  | 'importExport'
  | 'rollback'
  | 'history'
  | 'health';

interface DomainMeta {
  key: ConfigDomain;
  prefixes: string[];
  icon: React.ReactNode;
}

const DOMAINS: DomainMeta[] = [
  { key: 'all', prefixes: [], icon: <Layers className="w-4 h-4" /> },
  { key: 'instance', prefixes: ['instance.', 'tenant.', 'environment.', 'branding.', 'featureFlags.'], icon: <Settings className="w-4 h-4" /> },
  { key: 'notifications', prefixes: ['notifications.'], icon: <Activity className="w-4 h-4" /> },
  { key: 'workflows', prefixes: ['workflow.', 'promotion.'], icon: <ArrowRight className="w-4 h-4" /> },
  { key: 'governance', prefixes: ['governance.'], icon: <Shield className="w-4 h-4" /> },
  { key: 'catalog', prefixes: ['catalog.', 'change.'], icon: <FileJson className="w-4 h-4" /> },
  { key: 'operations', prefixes: ['incidents.', 'operations.', 'finops.', 'benchmarking.'], icon: <Activity className="w-4 h-4" /> },
  { key: 'ai', prefixes: ['ai.'], icon: <Eye className="w-4 h-4" /> },
  { key: 'integrations', prefixes: ['integrations.'], icon: <ArrowLeftRight className="w-4 h-4" /> },
];

// ── Helpers ────────────────────────────────────────────────────────────

function matchDomain(key: string, domain: DomainMeta): boolean {
  if (domain.key === 'all') return true;
  return domain.prefixes.some((p) => key.startsWith(p));
}

function renderValuePreview(value: string | null, isSensitive: boolean): React.ReactNode {
  if (isSensitive) return <Badge variant="warning"><Lock className="w-3 h-3 mr-1" />Masked</Badge>;
  if (!value) return <span className="text-gray-400 italic text-xs">null</span>;
  if (value === 'true') return <Badge variant="success">Enabled</Badge>;
  if (value === 'false') return <Badge variant="default">Disabled</Badge>;
  try {
    const parsed = JSON.parse(value);
    if (Array.isArray(parsed)) return <Badge variant="info">{parsed.length} items</Badge>;
    if (typeof parsed === 'object') return <Badge variant="info">{Object.keys(parsed).length} fields</Badge>;
  } catch { /* not JSON */ }
  return <span className="text-sm text-gray-600 dark:text-gray-400 truncate max-w-[200px] inline-block">{value}</span>;
}

// ── Main Component ─────────────────────────────────────────────────────

export function AdvancedConfigurationConsolePage() {
  const { t } = useTranslation();
  const [activeTab, setActiveTab] = useState<AdminTab>('explorer');
  const [activeDomain, setActiveDomain] = useState<ConfigDomain>('all');
  const [searchQuery, setSearchQuery] = useState('');
  const [selectedScope, setSelectedScope] = useState<ConfigurationScope>('System');
  const [compareScope, setCompareScope] = useState<ConfigurationScope>('Tenant');
  const [expandedKey, setExpandedKey] = useState<string | null>(null);
  const [showSensitive, setShowSensitive] = useState(false);
  const [selectedAuditKey, setSelectedAuditKey] = useState<string | null>(null);

  // ── Data ────────────────────────────────────────────────────────────
  const { data: definitions, isLoading: loadingDefs, isError: errorDefs, refetch: refetchDefs } = useConfigurationDefinitions();
  const { data: effective, isLoading: loadingEffective } = useEffectiveSettings(selectedScope);
  const { data: compareEffective } = useEffectiveSettings(compareScope);
  const { data: auditData } = useAuditHistory(selectedAuditKey);

  // ── Filtered definitions ───────────────────────────────────────────
  const filteredDefs = useMemo(() => {
    if (!definitions) return [];
    const domain = DOMAINS.find((d) => d.key === activeDomain);
    return definitions.filter((d: ConfigurationDefinitionDto) => {
      const domainMatch = domain ? matchDomain(d.key, domain) : true;
      const searchMatch = !searchQuery ||
        d.key.toLowerCase().includes(searchQuery.toLowerCase()) ||
        d.displayName.toLowerCase().includes(searchQuery.toLowerCase());
      return domainMatch && searchMatch;
    });
  }, [definitions, activeDomain, searchQuery]);

  // ── Effective map ──────────────────────────────────────────────────
  const effectiveMap = useMemo(() => {
    const map = new Map<string, EffectiveConfigurationDto>();
    if (effective) effective.forEach((e: EffectiveConfigurationDto) => map.set(e.key, e));
    return map;
  }, [effective]);

  const compareMap = useMemo(() => {
    const map = new Map<string, EffectiveConfigurationDto>();
    if (compareEffective) compareEffective.forEach((e: EffectiveConfigurationDto) => map.set(e.key, e));
    return map;
  }, [compareEffective]);

  // ── Diff data ──────────────────────────────────────────────────────
  const diffItems = useMemo(() => {
    if (!filteredDefs) return [];
    return filteredDefs.map((def: ConfigurationDefinitionDto) => {
      const left = effectiveMap.get(def.key);
      const right = compareMap.get(def.key);
      const leftVal = left?.effectiveValue ?? def.defaultValue;
      const rightVal = right?.effectiveValue ?? def.defaultValue;
      const isDifferent = leftVal !== rightVal;
      return { def, left, right, leftVal, rightVal, isDifferent };
    }).filter(item => item.isDifferent);
  }, [filteredDefs, effectiveMap, compareMap]);

  // ── Health checks ──────────────────────────────────────────────────
  const healthChecks = useMemo(() => {
    if (!definitions || !effective) return [];
    const checks: { name: string; status: 'ok' | 'warning' | 'error'; message: string }[] = [];
    
    const defCount = definitions.length;
    checks.push({
      name: t('advancedConfig.health.definitionCount', 'Definition Count'),
      status: defCount > 0 ? 'ok' : 'error',
      message: `${defCount} ${t('advancedConfig.health.definitionsLoaded', 'definitions loaded')}`,
    });

    checks.push({
      name: t('advancedConfig.health.effectiveResolution', 'Effective Resolution'),
      status: effective.length > 0 ? 'ok' : 'warning',
      message: `${effective.length} ${t('advancedConfig.health.settingsResolved', 'settings resolved')}`,
    });

    const sensitiveDefs = definitions.filter((d: ConfigurationDefinitionDto) => d.isSensitive);
    checks.push({
      name: t('advancedConfig.health.sensitiveProtection', 'Sensitive Protection'),
      status: sensitiveDefs.length > 0 ? 'ok' : 'ok',
      message: `${sensitiveDefs.length} ${t('advancedConfig.health.sensitiveKeys', 'sensitive keys protected')}`,
    });

    const orphanedEffective = effective.filter(
      (e: EffectiveConfigurationDto) => !definitions.find((d: ConfigurationDefinitionDto) => d.key === e.key)
    );
    checks.push({
      name: t('advancedConfig.health.orphanCheck', 'Orphan Check'),
      status: orphanedEffective.length === 0 ? 'ok' : 'warning',
      message: orphanedEffective.length === 0
        ? t('advancedConfig.health.noOrphans', 'No orphaned entries')
        : `${orphanedEffective.length} ${t('advancedConfig.health.orphansFound', 'orphaned entries found')}`,
    });

    const duplicateKeys = definitions.map((d: ConfigurationDefinitionDto) => d.key).filter((k: string, i: number, arr: string[]) => arr.indexOf(k) !== i);
    checks.push({
      name: t('advancedConfig.health.duplicateCheck', 'Duplicate Check'),
      status: duplicateKeys.length === 0 ? 'ok' : 'error',
      message: duplicateKeys.length === 0
        ? t('advancedConfig.health.noDuplicates', 'No duplicate keys')
        : `${duplicateKeys.length} ${t('advancedConfig.health.duplicatesFound', 'duplicate keys found')}`,
    });

    return checks;
  }, [definitions, effective, t]);

  // ── Export handler ─────────────────────────────────────────────────
  const handleExport = useCallback(() => {
    if (!filteredDefs || !effective) return;
    const exportData = {
      exportedAt: new Date().toISOString(),
      scope: selectedScope,
      domain: activeDomain,
      version: '1.0',
      definitions: filteredDefs.map((def: ConfigurationDefinitionDto) => {
        const eff = effectiveMap.get(def.key);
        return {
          key: def.key,
          displayName: def.displayName,
          category: def.category,
          valueType: def.valueType,
          defaultValue: def.defaultValue,
          effectiveValue: def.isSensitive ? '***MASKED***' : (eff?.effectiveValue ?? def.defaultValue),
          isSensitive: def.isSensitive,
          resolvedScope: eff?.resolvedScope ?? 'System',
          isInherited: eff?.isInherited ?? true,
          isDefault: eff?.isDefault ?? true,
        };
      }),
    };
    const blob = new Blob([JSON.stringify(exportData, null, 2)], { type: 'application/json' });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = `nextraceone-config-${activeDomain}-${selectedScope}-${new Date().toISOString().slice(0, 10)}.json`;
    a.click();
    URL.revokeObjectURL(url);
  }, [filteredDefs, effective, effectiveMap, selectedScope, activeDomain]);

  // ── Loading / Error ────────────────────────────────────────────────
  if (loadingDefs) {
    return <PageLoadingState message={t('advancedConfig.loading', 'Loading advanced configuration console...')} />;
  }

  if (errorDefs) {
    return (
      <PageErrorState
        title={t('advancedConfig.errorTitle', 'Failed to load configuration')}
        message={t('advancedConfig.errorMessage', 'Unable to load configuration definitions for the advanced console.')}
        action={<button onClick={() => refetchDefs()} className="btn btn-primary">{t('common.retry', 'Retry')}</button>}
      />
    );
  }

  return (
    <PageContainer>
      <PageHeader
        title={t('advancedConfig.title', 'Advanced Configuration Console')}
        subtitle={t('advancedConfig.subtitle', 'Enterprise administration: explore, compare, export, rollback, and govern configuration')}
      />

      {/* ── Tab Navigation ────────────────────────────────────────────── */}
      <div className="flex flex-wrap gap-2 mb-6">
        {([
          { key: 'explorer' as AdminTab, icon: <Eye className="w-4 h-4" />, label: t('advancedConfig.tabs.explorer', 'Effective Explorer') },
          { key: 'diff' as AdminTab, icon: <ArrowLeftRight className="w-4 h-4" />, label: t('advancedConfig.tabs.diff', 'Diff & Compare') },
          { key: 'importExport' as AdminTab, icon: <Download className="w-4 h-4" />, label: t('advancedConfig.tabs.importExport', 'Import / Export') },
          { key: 'rollback' as AdminTab, icon: <RotateCcw className="w-4 h-4" />, label: t('advancedConfig.tabs.rollback', 'Rollback & Restore') },
          { key: 'history' as AdminTab, icon: <History className="w-4 h-4" />, label: t('advancedConfig.tabs.history', 'History & Timeline') },
          { key: 'health' as AdminTab, icon: <Activity className="w-4 h-4" />, label: t('advancedConfig.tabs.health', 'Health & Troubleshooting') },
        ]).map(tab => (
          <button
            key={tab.key}
            onClick={() => setActiveTab(tab.key)}
            className={`flex items-center gap-2 px-4 py-2 rounded-lg text-sm font-medium transition-colors ${
              activeTab === tab.key
                ? 'bg-brand-600 text-white shadow-sm'
                : 'bg-white dark:bg-gray-800 text-gray-700 dark:text-gray-300 hover:bg-gray-50 dark:hover:bg-gray-700 border border-gray-200 dark:border-gray-700'
            }`}
          >
            {tab.icon}
            {tab.label}
          </button>
        ))}
      </div>

      {/* ── Domain Navigation ─────────────────────────────────────────── */}
      <div className="flex flex-wrap gap-1 mb-4">
        {DOMAINS.map(domain => (
          <button
            key={domain.key}
            onClick={() => setActiveDomain(domain.key)}
            className={`flex items-center gap-1.5 px-3 py-1.5 rounded text-xs font-medium transition-colors ${
              activeDomain === domain.key
                ? 'bg-brand-100 dark:bg-brand-900/30 text-brand-700 dark:text-brand-300 border border-brand-300 dark:border-brand-700'
                : 'bg-gray-50 dark:bg-gray-800 text-gray-600 dark:text-gray-400 hover:bg-gray-100 dark:hover:bg-gray-700 border border-gray-200 dark:border-gray-700'
            }`}
          >
            {domain.icon}
            {t(`advancedConfig.domains.${domain.key}`, domain.key)}
          </button>
        ))}
      </div>

      {/* ── Search ────────────────────────────────────────────────────── */}
      <div className="relative mb-6">
        <Search className="absolute left-3 top-1/2 transform -translate-y-1/2 w-4 h-4 text-gray-400" />
        <input
          type="text"
          value={searchQuery}
          onChange={(e) => setSearchQuery(e.target.value)}
          placeholder={t('advancedConfig.searchPlaceholder', 'Search by key or name...')}
          className="w-full pl-10 pr-4 py-2 border border-gray-200 dark:border-gray-700 rounded-lg bg-white dark:bg-gray-800 text-sm"
        />
      </div>

      {/* ══════════════════════════════════════════════════════════════ */}
      {/* TAB: Effective Explorer                                       */}
      {/* ══════════════════════════════════════════════════════════════ */}
      {activeTab === 'explorer' && (
        <div className="space-y-3">
          <div className="flex items-center gap-4 mb-4">
            <select
              value={selectedScope}
              onChange={(e) => setSelectedScope(e.target.value as ConfigurationScope)}
              className="px-3 py-2 border border-gray-200 dark:border-gray-700 rounded-lg text-sm bg-white dark:bg-gray-800"
            >
              <option value="System">System</option>
              <option value="Tenant">Tenant</option>
              <option value="Environment">Environment</option>
            </select>
            <span className="text-sm text-gray-500">
              {t('advancedConfig.explorer.showing', 'Showing')} {filteredDefs.length} {t('advancedConfig.explorer.definitions', 'definitions')}
              {loadingEffective && <RefreshCw className="w-3 h-3 ml-2 animate-spin inline" />}
            </span>
          </div>

          {filteredDefs.map((def: ConfigurationDefinitionDto) => {
            const eff = effectiveMap.get(def.key);
            const isExpanded = expandedKey === def.key;

            return (
              <Card key={def.key}>
                <CardBody>
                  <div className="flex items-start justify-between">
                    <div className="flex-1">
                      <div className="flex items-center gap-2">
                        <button
                          onClick={() => setExpandedKey(isExpanded ? null : def.key)}
                          className="flex items-center gap-1 text-left"
                        >
                          {isExpanded ? <ChevronUp className="w-4 h-4" /> : <ChevronDown className="w-4 h-4" />}
                          <span className="font-medium text-sm">{def.displayName}</span>
                        </button>
                        {eff?.isInherited && <Badge variant="info" className="text-xs">Inherited</Badge>}
                        {eff?.isDefault && <Badge variant="default" className="text-xs">Default</Badge>}
                        {!def.isInheritable && <Badge variant="warning" className="text-xs"><Lock className="w-3 h-3 mr-1" />Mandatory</Badge>}
                        {def.isSensitive && <Badge variant="warning" className="text-xs"><Shield className="w-3 h-3 mr-1" />Sensitive</Badge>}
                      </div>
                      <p className="text-xs text-gray-500 dark:text-gray-400 mt-1 ml-5">{def.key}</p>
                    </div>
                    <div className="flex items-center gap-3">
                      {eff && (
                        <div className="text-right">
                          <div className="text-xs text-gray-400">{eff.resolvedScope}</div>
                          {renderValuePreview(eff.effectiveValue, def.isSensitive && !showSensitive)}
                        </div>
                      )}
                      {!eff && renderValuePreview(def.defaultValue, def.isSensitive && !showSensitive)}
                    </div>
                  </div>

                  {isExpanded && (
                    <div className="mt-4 pt-4 border-t border-gray-100 dark:border-gray-700 space-y-3">
                      <div className="grid grid-cols-2 md:grid-cols-4 gap-3 text-xs">
                        <div>
                          <span className="text-gray-400">{t('advancedConfig.explorer.type', 'Type')}</span>
                          <p className="font-medium">{def.valueType}</p>
                        </div>
                        <div>
                          <span className="text-gray-400">{t('advancedConfig.explorer.scopes', 'Scopes')}</span>
                          <p className="font-medium">{def.allowedScopes?.join(', ')}</p>
                        </div>
                        <div>
                          <span className="text-gray-400">{t('advancedConfig.explorer.editor', 'Editor')}</span>
                          <p className="font-medium">{def.uiEditorType ?? 'text'}</p>
                        </div>
                        <div>
                          <span className="text-gray-400">{t('advancedConfig.explorer.inheritable', 'Inheritable')}</span>
                          <p className="font-medium">{def.isInheritable ? 'Yes' : 'No'}</p>
                        </div>
                      </div>
                      {def.description && (
                        <p className="text-xs text-gray-500">{def.description}</p>
                      )}
                      <div>
                        <span className="text-xs text-gray-400">{t('advancedConfig.explorer.defaultValue', 'Default Value')}</span>
                        <pre className="mt-1 p-2 bg-gray-50 dark:bg-gray-900 rounded text-xs overflow-x-auto">
                          {def.defaultValue ?? 'null'}
                        </pre>
                      </div>
                      {eff && (
                        <div className="p-3 bg-brand-50 dark:bg-brand-900/20 rounded-lg">
                          <div className="flex items-center gap-2 text-xs text-brand-700 dark:text-brand-300 mb-1">
                            <Layers className="w-3 h-3" />
                            {t('advancedConfig.explorer.effectiveValue', 'Effective Value')}
                            <Badge variant="info" className="text-xs">{eff.resolvedScope}</Badge>
                            {eff.isInherited && <Badge variant="default" className="text-xs">Inherited</Badge>}
                          </div>
                          <pre className="text-xs overflow-x-auto">
                            {def.isSensitive && !showSensitive ? '***MASKED***' : (eff.effectiveValue ?? 'null')}
                          </pre>
                        </div>
                      )}
                      <div className="flex gap-2">
                        <button
                          onClick={() => setSelectedAuditKey(def.key)}
                          className="flex items-center gap-1 px-2 py-1 text-xs text-gray-600 dark:text-gray-400 hover:text-brand-600 transition-colors"
                        >
                          <History className="w-3 h-3" />
                          {t('advancedConfig.explorer.viewHistory', 'View History')}
                        </button>
                      </div>
                    </div>
                  )}
                </CardBody>
              </Card>
            );
          })}

          {filteredDefs.length === 0 && (
            <Card>
              <CardBody>
                <div className="text-center py-8 text-gray-500">
                  <Filter className="w-8 h-8 mx-auto mb-2 opacity-50" />
                  <p>{t('advancedConfig.explorer.noResults', 'No matching definitions found.')}</p>
                </div>
              </CardBody>
            </Card>
          )}
        </div>
      )}

      {/* ══════════════════════════════════════════════════════════════ */}
      {/* TAB: Diff & Compare                                          */}
      {/* ══════════════════════════════════════════════════════════════ */}
      {activeTab === 'diff' && (
        <div className="space-y-4">
          <Card>
            <CardBody>
              <div className="flex items-center gap-4">
                <div className="flex-1">
                  <label className="text-xs text-gray-500 mb-1 block">{t('advancedConfig.diff.leftScope', 'Left Scope')}</label>
                  <select
                    value={selectedScope}
                    onChange={(e) => setSelectedScope(e.target.value as ConfigurationScope)}
                    className="w-full px-3 py-2 border border-gray-200 dark:border-gray-700 rounded-lg text-sm bg-white dark:bg-gray-800"
                  >
                    <option value="System">System</option>
                    <option value="Tenant">Tenant</option>
                    <option value="Environment">Environment</option>
                  </select>
                </div>
                <ArrowLeftRight className="w-5 h-5 text-gray-400 mt-5" />
                <div className="flex-1">
                  <label className="text-xs text-gray-500 mb-1 block">{t('advancedConfig.diff.rightScope', 'Right Scope')}</label>
                  <select
                    value={compareScope}
                    onChange={(e) => setCompareScope(e.target.value as ConfigurationScope)}
                    className="w-full px-3 py-2 border border-gray-200 dark:border-gray-700 rounded-lg text-sm bg-white dark:bg-gray-800"
                  >
                    <option value="System">System</option>
                    <option value="Tenant">Tenant</option>
                    <option value="Environment">Environment</option>
                  </select>
                </div>
              </div>
            </CardBody>
          </Card>

          <div className="flex items-center gap-2 text-sm text-gray-600 dark:text-gray-400">
            <ArrowLeftRight className="w-4 h-4" />
            {diffItems.length} {t('advancedConfig.diff.differences', 'differences found')}
          </div>

          {diffItems.map(({ def, leftVal, rightVal }) => (
            <Card key={def.key}>
              <CardBody>
                <div className="flex items-center gap-2 mb-3">
                  <span className="font-medium text-sm">{def.displayName}</span>
                  <span className="text-xs text-gray-400">{def.key}</span>
                </div>
                <div className="grid grid-cols-2 gap-4">
                  <div className="p-3 bg-red-50 dark:bg-red-900/10 rounded-lg">
                    <div className="text-xs text-red-600 dark:text-red-400 mb-1">{selectedScope}</div>
                    <pre className="text-xs overflow-x-auto">{def.isSensitive ? '***' : (leftVal ?? 'null')}</pre>
                  </div>
                  <div className="p-3 bg-green-50 dark:bg-green-900/10 rounded-lg">
                    <div className="text-xs text-green-600 dark:text-green-400 mb-1">{compareScope}</div>
                    <pre className="text-xs overflow-x-auto">{def.isSensitive ? '***' : (rightVal ?? 'null')}</pre>
                  </div>
                </div>
              </CardBody>
            </Card>
          ))}

          {diffItems.length === 0 && (
            <Card>
              <CardBody>
                <div className="text-center py-8 text-gray-500">
                  <CheckCircle2 className="w-8 h-8 mx-auto mb-2 text-green-500" />
                  <p>{t('advancedConfig.diff.noDifferences', 'No differences between the selected scopes.')}</p>
                </div>
              </CardBody>
            </Card>
          )}
        </div>
      )}

      {/* ══════════════════════════════════════════════════════════════ */}
      {/* TAB: Import / Export                                         */}
      {/* ══════════════════════════════════════════════════════════════ */}
      {activeTab === 'importExport' && (
        <div className="space-y-4">
          <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
            {/* Export */}
            <Card>
              <CardHeader>
                <div className="flex items-center gap-2">
                  <Download className="w-5 h-5 text-brand-600" />
                  <h3 className="font-semibold">{t('advancedConfig.export.title', 'Export Configuration')}</h3>
                </div>
              </CardHeader>
              <CardBody>
                <p className="text-sm text-gray-600 dark:text-gray-400 mb-4">
                  {t('advancedConfig.export.description', 'Export configuration definitions and effective values as a validated JSON file. Sensitive values are automatically masked.')}
                </p>
                <div className="space-y-3">
                  <div>
                    <label className="text-xs text-gray-500 mb-1 block">{t('advancedConfig.export.scope', 'Scope')}</label>
                    <select
                      value={selectedScope}
                      onChange={(e) => setSelectedScope(e.target.value as ConfigurationScope)}
                      className="w-full px-3 py-2 border border-gray-200 dark:border-gray-700 rounded-lg text-sm bg-white dark:bg-gray-800"
                    >
                      <option value="System">System</option>
                      <option value="Tenant">Tenant</option>
                      <option value="Environment">Environment</option>
                    </select>
                  </div>
                  <div>
                    <label className="text-xs text-gray-500 mb-1 block">{t('advancedConfig.export.domain', 'Domain')}</label>
                    <select
                      value={activeDomain}
                      onChange={(e) => setActiveDomain(e.target.value as ConfigDomain)}
                      className="w-full px-3 py-2 border border-gray-200 dark:border-gray-700 rounded-lg text-sm bg-white dark:bg-gray-800"
                    >
                      {DOMAINS.map(d => <option key={d.key} value={d.key}>{d.key}</option>)}
                    </select>
                  </div>
                  <div className="bg-yellow-50 dark:bg-yellow-900/20 p-3 rounded-lg">
                    <div className="flex items-start gap-2 text-xs text-yellow-700 dark:text-yellow-300">
                      <AlertTriangle className="w-4 h-4 mt-0.5 flex-shrink-0" />
                      <span>{t('advancedConfig.export.sensitiveWarning', 'Sensitive values will be masked in the export for security.')}</span>
                    </div>
                  </div>
                  <button
                    onClick={handleExport}
                    className="w-full flex items-center justify-center gap-2 px-4 py-2 bg-brand-600 text-white rounded-lg text-sm font-medium hover:bg-brand-700"
                  >
                    <Download className="w-4 h-4" />
                    {t('advancedConfig.export.button', 'Export JSON')}
                  </button>
                </div>
              </CardBody>
            </Card>

            {/* Import */}
            <Card>
              <CardHeader>
                <div className="flex items-center gap-2">
                  <Upload className="w-5 h-5 text-brand-600" />
                  <h3 className="font-semibold">{t('advancedConfig.import.title', 'Import Configuration')}</h3>
                </div>
              </CardHeader>
              <CardBody>
                <p className="text-sm text-gray-600 dark:text-gray-400 mb-4">
                  {t('advancedConfig.import.description', 'Import a previously exported configuration file. All values will be validated against current definitions before applying.')}
                </p>
                <div className="space-y-3">
                  <div className="border-2 border-dashed border-gray-200 dark:border-gray-700 rounded-lg p-8 text-center">
                    <Upload className="w-8 h-8 mx-auto mb-2 text-gray-400" />
                    <p className="text-sm text-gray-500">{t('advancedConfig.import.dropzone', 'Drop JSON file here or click to select')}</p>
                    <p className="text-xs text-gray-400 mt-1">{t('advancedConfig.import.format', 'Accepts NexTraceOne configuration export format')}</p>
                  </div>
                  <div className="bg-blue-50 dark:bg-blue-900/20 p-3 rounded-lg">
                    <div className="flex items-start gap-2 text-xs text-blue-700 dark:text-blue-300">
                      <Info className="w-4 h-4 mt-0.5 flex-shrink-0" />
                      <span>{t('advancedConfig.import.previewNote', 'Import will show a preview and validation report before applying any changes.')}</span>
                    </div>
                  </div>
                </div>
              </CardBody>
            </Card>
          </div>
        </div>
      )}

      {/* ══════════════════════════════════════════════════════════════ */}
      {/* TAB: Rollback & Restore                                      */}
      {/* ══════════════════════════════════════════════════════════════ */}
      {activeTab === 'rollback' && (
        <div className="space-y-4">
          <Card>
            <CardBody>
              <div className="flex items-center gap-2 mb-4">
                <RotateCcw className="w-5 h-5 text-brand-600" />
                <h3 className="font-semibold">{t('advancedConfig.rollback.title', 'Configuration Rollback')}</h3>
              </div>
              <p className="text-sm text-gray-600 dark:text-gray-400 mb-6">
                {t('advancedConfig.rollback.description', 'Select a configuration key to view its version history and restore a previous value. All rollbacks are audited and validated.')}
              </p>

              <div className="relative mb-4">
                <Search className="absolute left-3 top-1/2 transform -translate-y-1/2 w-4 h-4 text-gray-400" />
                <input
                  type="text"
                  placeholder={t('advancedConfig.rollback.searchKey', 'Search key to rollback...')}
                  className="w-full pl-10 pr-4 py-2 border border-gray-200 dark:border-gray-700 rounded-lg bg-white dark:bg-gray-800 text-sm"
                  onChange={(e) => {
                    setSearchQuery(e.target.value);
                    if (e.target.value.length > 3) {
                      const found = definitions?.find((d: ConfigurationDefinitionDto) => d.key === e.target.value);
                      if (found) setSelectedAuditKey(found.key);
                    }
                  }}
                />
              </div>

              {selectedAuditKey && auditData && auditData.length > 0 && (
                <div className="space-y-3">
                  <h4 className="text-sm font-medium text-gray-700 dark:text-gray-300">
                    {t('advancedConfig.rollback.historyFor', 'Version History for')} <code className="text-brand-600">{selectedAuditKey}</code>
                  </h4>
                  {auditData.map((entry, idx) => (
                    <div key={idx} className="flex items-start gap-3 p-3 bg-gray-50 dark:bg-gray-800 rounded-lg">
                      <Clock className="w-4 h-4 text-gray-400 mt-0.5 flex-shrink-0" />
                      <div className="flex-1">
                        <div className="flex items-center gap-2 text-xs text-gray-500">
                          <span>{new Date(entry.changedAt).toLocaleString()}</span>
                          <span>•</span>
                          <span>{entry.changedBy}</span>
                          <Badge variant="default" className="text-xs">{entry.action}</Badge>
                        </div>
                        <div className="grid grid-cols-2 gap-2 mt-2 text-xs">
                          {entry.previousValue !== null && (
                            <div className="p-2 bg-red-50 dark:bg-red-900/10 rounded">
                              <span className="text-red-600 dark:text-red-400">Previous:</span>
                              <pre className="mt-1 overflow-x-auto">{entry.isSensitive ? '***' : entry.previousValue}</pre>
                            </div>
                          )}
                          <div className="p-2 bg-green-50 dark:bg-green-900/10 rounded">
                            <span className="text-green-600 dark:text-green-400">New:</span>
                            <pre className="mt-1 overflow-x-auto">{entry.isSensitive ? '***' : entry.newValue}</pre>
                          </div>
                        </div>
                        {entry.changeReason && (
                          <p className="text-xs text-gray-500 mt-1 italic">"{entry.changeReason}"</p>
                        )}
                      </div>
                      {idx > 0 && (
                        <button className="flex items-center gap-1 px-2 py-1 text-xs text-brand-600 hover:bg-brand-50 dark:hover:bg-brand-900/20 rounded transition-colors">
                          <RotateCcw className="w-3 h-3" />
                          {t('advancedConfig.rollback.restore', 'Restore')}
                        </button>
                      )}
                    </div>
                  ))}
                </div>
              )}

              {(!selectedAuditKey || !auditData || auditData.length === 0) && (
                <div className="text-center py-8 text-gray-500">
                  <RotateCcw className="w-8 h-8 mx-auto mb-2 opacity-50" />
                  <p>{t('advancedConfig.rollback.selectKey', 'Search and select a configuration key to view its version history.')}</p>
                </div>
              )}
            </CardBody>
          </Card>
        </div>
      )}

      {/* ══════════════════════════════════════════════════════════════ */}
      {/* TAB: History & Timeline                                      */}
      {/* ══════════════════════════════════════════════════════════════ */}
      {activeTab === 'history' && (
        <div className="space-y-4">
          <Card>
            <CardBody>
              <div className="flex items-center gap-2 mb-4">
                <History className="w-5 h-5 text-brand-600" />
                <h3 className="font-semibold">{t('advancedConfig.history.title', 'Configuration Change Timeline')}</h3>
              </div>
              <p className="text-sm text-gray-600 dark:text-gray-400 mb-4">
                {t('advancedConfig.history.description', 'View all configuration changes across domains. Filter by key, user, or time period.')}
              </p>

              <div className="relative mb-4">
                <Search className="absolute left-3 top-1/2 transform -translate-y-1/2 w-4 h-4 text-gray-400" />
                <input
                  type="text"
                  placeholder={t('advancedConfig.history.searchPlaceholder', 'Search by key...')}
                  className="w-full pl-10 pr-4 py-2 border border-gray-200 dark:border-gray-700 rounded-lg bg-white dark:bg-gray-800 text-sm"
                  onChange={(e) => {
                    if (e.target.value.length > 2) setSelectedAuditKey(e.target.value);
                  }}
                />
              </div>

              {selectedAuditKey && auditData && auditData.length > 0 && (
                <div className="space-y-2">
                  {auditData.map((entry, idx) => (
                    <div key={idx} className="flex items-start gap-3 py-3 border-b border-gray-100 dark:border-gray-700 last:border-0">
                      <div className="w-2 h-2 rounded-full bg-brand-500 mt-2 flex-shrink-0" />
                      <div className="flex-1">
                        <div className="flex items-center gap-2 text-xs">
                          <span className="font-medium text-gray-700 dark:text-gray-300">{entry.key}</span>
                          <Badge variant={entry.action === 'Set' ? 'success' : entry.action === 'Remove' ? 'danger' : 'default'} className="text-xs">{entry.action}</Badge>
                          <span className="text-gray-400">{entry.scope}</span>
                        </div>
                        <div className="flex items-center gap-2 text-xs text-gray-500 mt-1">
                          <span>{entry.changedBy}</span>
                          <span>•</span>
                          <span>{new Date(entry.changedAt).toLocaleString()}</span>
                          {entry.changeReason && <span className="italic">— {entry.changeReason}</span>}
                        </div>
                      </div>
                    </div>
                  ))}
                </div>
              )}

              {(!selectedAuditKey || !auditData || auditData.length === 0) && (
                <div className="text-center py-8 text-gray-500">
                  <History className="w-8 h-8 mx-auto mb-2 opacity-50" />
                  <p>{t('advancedConfig.history.empty', 'Enter a configuration key to view its change timeline.')}</p>
                </div>
              )}
            </CardBody>
          </Card>
        </div>
      )}

      {/* ══════════════════════════════════════════════════════════════ */}
      {/* TAB: Health & Troubleshooting                                */}
      {/* ══════════════════════════════════════════════════════════════ */}
      {activeTab === 'health' && (
        <div className="space-y-4">
          <Card>
            <CardHeader>
              <div className="flex items-center gap-2">
                <Activity className="w-5 h-5 text-brand-600" />
                <h3 className="font-semibold">{t('advancedConfig.health.title', 'Configuration Platform Health')}</h3>
              </div>
            </CardHeader>
            <CardBody>
              <div className="space-y-3">
                {healthChecks.map((check, idx) => (
                  <div key={idx} className="flex items-center justify-between p-3 bg-gray-50 dark:bg-gray-800 rounded-lg">
                    <div className="flex items-center gap-3">
                      {check.status === 'ok' && <CheckCircle2 className="w-5 h-5 text-green-500" />}
                      {check.status === 'warning' && <AlertTriangle className="w-5 h-5 text-yellow-500" />}
                      {check.status === 'error' && <XCircle className="w-5 h-5 text-red-500" />}
                      <span className="text-sm font-medium">{check.name}</span>
                    </div>
                    <span className="text-xs text-gray-500">{check.message}</span>
                  </div>
                ))}
              </div>
            </CardBody>
          </Card>

          <Card>
            <CardHeader>
              <div className="flex items-center gap-2">
                <Shield className="w-5 h-5 text-brand-600" />
                <h3 className="font-semibold">{t('advancedConfig.health.governanceTitle', 'Definition Governance')}</h3>
              </div>
            </CardHeader>
            <CardBody>
              <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
                <div className="text-center p-4 bg-gray-50 dark:bg-gray-800 rounded-lg">
                  <p className="text-2xl font-bold text-brand-600">{definitions?.length ?? 0}</p>
                  <p className="text-xs text-gray-500">{t('advancedConfig.health.totalDefinitions', 'Total Definitions')}</p>
                </div>
                <div className="text-center p-4 bg-gray-50 dark:bg-gray-800 rounded-lg">
                  <p className="text-2xl font-bold text-yellow-600">{definitions?.filter((d: ConfigurationDefinitionDto) => d.isSensitive).length ?? 0}</p>
                  <p className="text-xs text-gray-500">{t('advancedConfig.health.sensitiveDefinitions', 'Sensitive')}</p>
                </div>
                <div className="text-center p-4 bg-gray-50 dark:bg-gray-800 rounded-lg">
                  <p className="text-2xl font-bold text-green-600">{definitions?.filter((d: ConfigurationDefinitionDto) => d.isEditable).length ?? 0}</p>
                  <p className="text-xs text-gray-500">{t('advancedConfig.health.editableDefinitions', 'Editable')}</p>
                </div>
                <div className="text-center p-4 bg-gray-50 dark:bg-gray-800 rounded-lg">
                  <p className="text-2xl font-bold text-blue-600">{definitions?.filter((d: ConfigurationDefinitionDto) => !d.isInheritable).length ?? 0}</p>
                  <p className="text-xs text-gray-500">{t('advancedConfig.health.mandatoryDefinitions', 'Mandatory (System-only)')}</p>
                </div>
              </div>

              <div className="mt-6">
                <h4 className="text-sm font-medium text-gray-700 dark:text-gray-300 mb-3">
                  {t('advancedConfig.health.domainBreakdown', 'Domain Breakdown')}
                </h4>
                <div className="space-y-2">
                  {DOMAINS.filter(d => d.key !== 'all').map(domain => {
                    const count = definitions?.filter((def: ConfigurationDefinitionDto) => matchDomain(def.key, domain)).length ?? 0;
                    return (
                      <div key={domain.key} className="flex items-center justify-between py-2 px-3 bg-gray-50 dark:bg-gray-800 rounded">
                        <div className="flex items-center gap-2">
                          {domain.icon}
                          <span className="text-sm">{t(`advancedConfig.domains.${domain.key}`, domain.key)}</span>
                        </div>
                        <Badge variant="info">{count}</Badge>
                      </div>
                    );
                  })}
                </div>
              </div>
            </CardBody>
          </Card>
        </div>
      )}
    </PageContainer>
  );
}
