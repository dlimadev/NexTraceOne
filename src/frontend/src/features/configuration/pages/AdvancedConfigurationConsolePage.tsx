/**
 * AdvancedConfigurationConsolePage — console avançada de administração de configuração.
 *
 * Organizada em 6 tabs extraídos como sub-componentes:
 *  - AdvancedConfigExplorerTab
 *  - AdvancedConfigDiffTab
 *  - AdvancedConfigImportExportTab
 *  - AdvancedConfigRollbackTab
 *  - AdvancedConfigHistoryTab
 *  - AdvancedConfigHealthTab
 *
 * Tipos, constantes e helpers partilhados em AdvancedConfigConsoleTypes.
 */
import { useState, useMemo, useCallback } from 'react';
import { useTranslation } from 'react-i18next';
import {
  Search,
  ArrowLeftRight,
  Download,
  RotateCcw,
  History,
  Activity,
  Eye,
} from 'lucide-react';
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
import { DOMAINS, matchDomain } from './AdvancedConfigConsoleTypes';
import type { ConfigDomain, AdminTab } from './AdvancedConfigConsoleTypes';
import { AdvancedConfigExplorerTab } from './AdvancedConfigExplorerTab';
import { AdvancedConfigDiffTab } from './AdvancedConfigDiffTab';
import { AdvancedConfigImportExportTab } from './AdvancedConfigImportExportTab';
import { AdvancedConfigRollbackTab } from './AdvancedConfigRollbackTab';
import { AdvancedConfigHistoryTab } from './AdvancedConfigHistoryTab';
import { AdvancedConfigHealthTab } from './AdvancedConfigHealthTab';
import type { HealthCheck } from './AdvancedConfigHealthTab';

// ── Main Component ─────────────────────────────────────────────────────

export function AdvancedConfigurationConsolePage() {
  const { t } = useTranslation();
  const [activeTab, setActiveTab] = useState<AdminTab>('explorer');
  const [activeDomain, setActiveDomain] = useState<ConfigDomain>('all');
  const [searchQuery, setSearchQuery] = useState('');
  const [selectedScope, setSelectedScope] = useState<ConfigurationScope>('System');
  const [compareScope, setCompareScope] = useState<ConfigurationScope>('Tenant');
  const [expandedKey, setExpandedKey] = useState<string | null>(null);
  const [showSensitive] = useState(false);
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
  const healthChecks = useMemo<HealthCheck[]>(() => {
    if (!definitions || !effective) return [];
    const checks: HealthCheck[] = [];

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
                : 'bg-card text-body hover:bg-subtle border border-edge'
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
                ? 'bg-brand-100 text-brand-700 border border-brand-300'
                : 'bg-subtle text-faded hover:bg-subtle border border-edge'
            }`}
          >
            {domain.icon}
            {t(`advancedConfig.domains.${domain.key}`, domain.key)}
          </button>
        ))}
      </div>

      {/* ── Search ────────────────────────────────────────────────────── */}
      <div className="relative mb-6">
        <Search className="absolute left-3 top-1/2 transform -translate-y-1/2 w-4 h-4 text-muted" />
        <input
          type="text"
          value={searchQuery}
          onChange={(e) => setSearchQuery(e.target.value)}
          placeholder={t('advancedConfig.searchPlaceholder', 'Search by key or name...')}
          className="w-full pl-10 pr-4 py-2 border border-edge rounded-lg bg-card text-sm"
        />
      </div>

      {/* ── Tab Content ───────────────────────────────────────────────── */}
      {activeTab === 'explorer' && (
        <AdvancedConfigExplorerTab
          selectedScope={selectedScope}
          filteredDefs={filteredDefs}
          effectiveMap={effectiveMap}
          expandedKey={expandedKey}
          loadingEffective={loadingEffective}
          showSensitive={showSensitive}
          setSelectedScope={setSelectedScope}
          setExpandedKey={setExpandedKey}
          setSelectedAuditKey={setSelectedAuditKey}
        />
      )}

      {activeTab === 'diff' && (
        <AdvancedConfigDiffTab
          selectedScope={selectedScope}
          compareScope={compareScope}
          diffItems={diffItems}
          setSelectedScope={setSelectedScope}
          setCompareScope={setCompareScope}
        />
      )}

      {activeTab === 'importExport' && (
        <AdvancedConfigImportExportTab
          selectedScope={selectedScope}
          activeDomain={activeDomain}
          setSelectedScope={setSelectedScope}
          setActiveDomain={setActiveDomain}
          onExport={handleExport}
        />
      )}

      {activeTab === 'rollback' && (
        <AdvancedConfigRollbackTab
          selectedAuditKey={selectedAuditKey}
          auditData={auditData}
          definitions={definitions}
          setSearchQuery={setSearchQuery}
          setSelectedAuditKey={setSelectedAuditKey}
        />
      )}

      {activeTab === 'history' && (
        <AdvancedConfigHistoryTab
          selectedAuditKey={selectedAuditKey}
          auditData={auditData}
          setSelectedAuditKey={setSelectedAuditKey}
        />
      )}

      {activeTab === 'health' && (
        <AdvancedConfigHealthTab
          healthChecks={healthChecks}
          definitions={definitions}
        />
      )}
    </PageContainer>
  );
}
