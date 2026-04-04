import { useState, useMemo, useCallback } from 'react';
import { useTranslation } from 'react-i18next';
import {
  Settings,
  Lock,
  Unlock,
  Shield,
  History,
  Search,
  Filter,
  ChevronDown,
  Check,
  X,
  RefreshCw,
  Edit3,
  Trash2,
  ToggleLeft,
  ToggleRight,
} from 'lucide-react';
import { Card, CardBody, CardHeader } from '../../../components/Card';
import { Badge } from '../../../components/Badge';
import { PageContainer } from '../../../components/shell';
import { PageHeader } from '../../../components/PageHeader';
import { PageLoadingState } from '../../../components/PageLoadingState';
import { PageErrorState } from '../../../components/PageErrorState';
import {
  useConfigurationDefinitions,
  useConfigurationEntries,
  useEffectiveSettings,
  useSetConfigurationValue,
  useRemoveOverride,
  useToggleConfiguration,
  useAuditHistory,
} from '../hooks/useConfiguration';
import type {
  ConfigurationDefinitionDto,
  ConfigurationScope,
  ConfigurationCategory,
  ConfigurationView,
} from '../types';

const SCOPES: ConfigurationScope[] = [
  'System',
  'Tenant',
  'Environment',
  'Role',
  'Team',
  'User',
];

const CATEGORIES: ConfigurationCategory[] = [
  'Bootstrap',
  'SensitiveOperational',
  'Functional',
];

const categoryVariant = (
  cat: string,
): 'success' | 'warning' | 'danger' | 'default' => {
  switch (cat) {
    case 'Bootstrap':
      return 'danger';
    case 'SensitiveOperational':
      return 'warning';
    case 'Functional':
      return 'success';
    default:
      return 'default';
  }
};

const SENSITIVE_MASK = '••••••••';

/**
 * Página de administração de configuração — gestão centralizada de definições,
 * valores e histórico de auditoria do módulo de Configuration.
 * Restrita a Platform Admins.
 */
export function ConfigurationAdminPage() {
  const { t } = useTranslation();

  // --- State ---
  const [scope, setScope] = useState<ConfigurationScope>('System');
  const [scopeRefId, setScopeRefId] = useState('');
  const [searchTerm, setSearchTerm] = useState('');
  const [categoryFilter, setCategoryFilter] = useState<string>('');
  const [activeView, setActiveView] =
    useState<ConfigurationView>('definitions');
  const [editingKey, setEditingKey] = useState<string | null>(null);
  const [editValue, setEditValue] = useState('');
  const [editReason, setEditReason] = useState('');
  const [auditKey, setAuditKey] = useState<string | null>(null);

  // --- Queries ---
  const {
    data: definitions,
    isLoading: defsLoading,
    isError: defsError,
    refetch: refetchDefs,
  } = useConfigurationDefinitions();

  const {
    data: entries,
    isLoading: entriesLoading,
    isError: entriesError,
    refetch: refetchEntries,
  } = useConfigurationEntries(scope, scopeRefId || null);

  const {
    data: effectiveSettings,
    isLoading: effectiveLoading,
    isError: effectiveError,
    refetch: refetchEffective,
  } = useEffectiveSettings(scope, scopeRefId || null);

  const { data: auditEntries } = useAuditHistory(auditKey);

  // --- Mutations ---
  const setValueMutation = useSetConfigurationValue();
  const removeMutation = useRemoveOverride();
  const toggleMutation = useToggleConfiguration();

  // --- Computed ---
  const filteredDefinitions = useMemo(() => {
    let items = definitions ?? [];
    if (searchTerm) {
      const lc = searchTerm.toLowerCase();
      items = items.filter(
        (d) =>
          d.key.toLowerCase().includes(lc) ||
          d.displayName.toLowerCase().includes(lc),
      );
    }
    if (categoryFilter) {
      items = items.filter((d) => d.category === categoryFilter);
    }
    return items.sort((a, b) => a.sortOrder - b.sortOrder);
  }, [definitions, searchTerm, categoryFilter]);

  const filteredEntries = useMemo(() => {
    let items = entries ?? [];
    if (searchTerm) {
      const lc = searchTerm.toLowerCase();
      items = items.filter((e) => e.definitionKey.toLowerCase().includes(lc));
    }
    return items;
  }, [entries, searchTerm]);

  const filteredEffective = useMemo(() => {
    let items = effectiveSettings ?? [];
    if (searchTerm) {
      const lc = searchTerm.toLowerCase();
      items = items.filter((e) => e.key.toLowerCase().includes(lc));
    }
    return items;
  }, [effectiveSettings, searchTerm]);

  const definitionMap = useMemo(() => {
    const map = new Map<string, ConfigurationDefinitionDto>();
    (definitions ?? []).forEach((d) => map.set(d.key, d));
    return map;
  }, [definitions]);

  // --- Handlers ---
  const handleEdit = useCallback(
    (key: string, currentValue: string | null) => {
      setEditingKey(key);
      setEditValue(currentValue ?? '');
      setEditReason('');
    },
    [],
  );

  const handleSave = useCallback(() => {
    if (!editingKey) return;
    setValueMutation.mutate(
      {
        key: editingKey,
        data: {
          scope,
          scopeReferenceId: scopeRefId || null,
          value: editValue,
          changeReason: editReason || undefined,
        },
      },
      { onSuccess: () => setEditingKey(null) },
    );
  }, [editingKey, editValue, editReason, scope, scopeRefId, setValueMutation]);

  const handleCancelEdit = useCallback(() => {
    setEditingKey(null);
    setEditValue('');
    setEditReason('');
  }, []);

  const handleToggle = useCallback(
    (key: string, activate: boolean) => {
      toggleMutation.mutate({
        key,
        data: {
          scope,
          scopeReferenceId: scopeRefId || null,
          activate,
          changeReason: `Toggle ${activate ? 'on' : 'off'}`,
        },
      });
    },
    [scope, scopeRefId, toggleMutation],
  );

  const handleRemoveOverride = useCallback(
    (key: string) => {
      removeMutation.mutate({
        key,
        scope,
        scopeReferenceId: scopeRefId || null,
        changeReason: 'Override removed via admin',
      });
    },
    [scope, scopeRefId, removeMutation],
  );

  const handleToggleAudit = useCallback(
    (key: string) => {
      setAuditKey((prev) => (prev === key ? null : key));
    },
    [],
  );

  // --- Loading / Error states ---
  const isLoading =
    activeView === 'definitions'
      ? defsLoading
      : activeView === 'entries'
        ? entriesLoading
        : effectiveLoading;

  const isError =
    activeView === 'definitions'
      ? defsError
      : activeView === 'entries'
        ? entriesError
        : effectiveError;

  const refetch =
    activeView === 'definitions'
      ? refetchDefs
      : activeView === 'entries'
        ? refetchEntries
        : refetchEffective;

  if (isLoading) return <PageLoadingState />;
  if (isError) return <PageErrorState action={<button className="text-xs text-accent hover:underline" onClick={() => refetch()}>{t('common.retry')}</button>} />;

  const renderValue = (
    value: string | null,
    isSensitive: boolean,
  ): string => {
    if (isSensitive) return SENSITIVE_MASK;
    return value ?? '—';
  };

  // ─── Edit Modal ────────────────────────────────────────────────
  const renderEditModal = () => {
    if (!editingKey) return null;
    const def = definitionMap.get(editingKey);
    const valueType = def?.valueType ?? 'String';
    const isBool = valueType === 'Boolean';
    const isJson = valueType === 'Json';

    return (
      <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40">
        <div className="bg-surface rounded-lg shadow-xl w-full max-w-lg mx-4">
          <div className="flex items-center justify-between px-6 py-4 border-b border-edge">
            <h3 className="text-sm font-semibold text-heading flex items-center gap-2">
              <Edit3 size={16} className="text-accent" />
              {t('configuration.edit.title')}
            </h3>
            <button
              onClick={handleCancelEdit}
              className="text-muted hover:text-heading transition-colors"
            >
              <X size={18} />
            </button>
          </div>
          <div className="px-6 py-4 space-y-4">
            {/* Key (read-only) */}
            <div>
              <label className="block text-xs font-medium text-muted mb-1">
                {t('configuration.edit.key')}
              </label>
              <div className="text-sm text-heading font-mono bg-elevated px-3 py-2 rounded">
                {editingKey}
              </div>
            </div>

            {/* Value input */}
            <div>
              <label className="block text-xs font-medium text-muted mb-1">
                {t('configuration.edit.newValue')}
              </label>
              {isBool ? (
                <button
                  type="button"
                  onClick={() =>
                    setEditValue((v) =>
                      v === 'true' ? 'false' : 'true',
                    )
                  }
                  className="flex items-center gap-2 text-sm"
                >
                  {editValue === 'true' ? (
                    <ToggleRight
                      size={24}
                      className="text-accent"
                    />
                  ) : (
                    <ToggleLeft size={24} className="text-muted" />
                  )}
                  <span className="text-heading">
                    {editValue === 'true'
                      ? t('common.yes')
                      : t('common.no')}
                  </span>
                </button>
              ) : isJson ? (
                <textarea
                  value={editValue}
                  onChange={(e) => setEditValue(e.target.value)}
                  rows={6}
                  className="w-full text-sm font-mono bg-elevated border border-edge rounded px-3 py-2 text-heading focus:outline-none focus:ring-1 focus:ring-accent"
                />
              ) : (
                <input
                  type={valueType === 'Integer' || valueType === 'Decimal' ? 'number' : 'text'}
                  value={editValue}
                  onChange={(e) => setEditValue(e.target.value)}
                  className="w-full text-sm bg-elevated border border-edge rounded px-3 py-2 text-heading focus:outline-none focus:ring-1 focus:ring-accent"
                />
              )}
            </div>

            {/* Change reason */}
            <div>
              <label className="block text-xs font-medium text-muted mb-1">
                {t('configuration.edit.changeReason')}
              </label>
              <input
                type="text"
                value={editReason}
                onChange={(e) => setEditReason(e.target.value)}
                placeholder={t('configuration.edit.changeReasonPlaceholder')}
                className="w-full text-sm bg-elevated border border-edge rounded px-3 py-2 text-heading focus:outline-none focus:ring-1 focus:ring-accent"
              />
            </div>
          </div>

          <div className="flex justify-end gap-2 px-6 py-4 border-t border-edge">
            <button
              onClick={handleCancelEdit}
              className="px-4 py-2 text-sm rounded border border-edge text-muted hover:text-heading transition-colors"
            >
              {t('common.cancel')}
            </button>
            <button
              onClick={handleSave}
              disabled={setValueMutation.isPending}
              className="px-4 py-2 text-sm rounded bg-accent text-white hover:bg-accent/90 transition-colors disabled:opacity-50"
            >
              {setValueMutation.isPending ? (
                <RefreshCw size={14} className="animate-spin inline mr-1" />
              ) : (
                <Check size={14} className="inline mr-1" />
              )}
              {t('common.save')}
            </button>
          </div>
        </div>
      </div>
    );
  };

  // ─── Audit Panel ───────────────────────────────────────────────
  const renderAuditPanel = () => {
    if (!auditKey) return null;
    const items = auditEntries ?? [];
    return (
      <Card className="mb-6">
        <CardHeader>
          <div className="flex items-center justify-between">
            <h3 className="text-sm font-semibold text-heading flex items-center gap-2">
              <History size={16} className="text-accent" />
              {t('configuration.audit.title')} — <span className="font-mono">{auditKey}</span>
            </h3>
            <button
              onClick={() => setAuditKey(null)}
              className="text-muted hover:text-heading transition-colors"
            >
              <X size={16} />
            </button>
          </div>
        </CardHeader>
        <CardBody className="p-0">
          {items.length === 0 ? (
            <div className="px-4 py-6 text-center text-muted text-sm">
              {t('configuration.audit.empty')}
            </div>
          ) : (
            <div className="overflow-x-auto">
              <table className="w-full text-sm">
                <thead>
                  <tr className="border-b border-edge text-xs text-muted">
                    <th className="text-left px-4 py-3 font-medium">{t('configuration.audit.action')}</th>
                    <th className="text-left px-4 py-3 font-medium">{t('configuration.audit.scope')}</th>
                    <th className="text-left px-4 py-3 font-medium">{t('configuration.audit.previousValue')}</th>
                    <th className="text-left px-4 py-3 font-medium">{t('configuration.audit.newValue')}</th>
                    <th className="text-left px-4 py-3 font-medium">{t('configuration.audit.changedBy')}</th>
                    <th className="text-left px-4 py-3 font-medium">{t('configuration.audit.changedAt')}</th>
                    <th className="text-left px-4 py-3 font-medium">{t('configuration.audit.reason')}</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-edge">
                  {items.map((entry, idx) => (
                    <tr key={idx} className="hover:bg-hover transition-colors">
                      <td className="px-4 py-3 font-medium text-heading">{entry.action}</td>
                      <td className="px-4 py-3 text-muted">{entry.scope}</td>
                      <td className="px-4 py-3 text-muted font-mono text-xs">
                        {renderValue(entry.previousValue, entry.isSensitive)}
                      </td>
                      <td className="px-4 py-3 text-muted font-mono text-xs">
                        {renderValue(entry.newValue, entry.isSensitive)}
                      </td>
                      <td className="px-4 py-3 text-muted">{entry.changedBy}</td>
                      <td className="px-4 py-3 text-muted">
                        {new Date(entry.changedAt).toLocaleString()}
                      </td>
                      <td className="px-4 py-3 text-muted">{entry.changeReason ?? '—'}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </CardBody>
      </Card>
    );
  };

  // ─── View Tabs ─────────────────────────────────────────────────
  const views: { key: ConfigurationView; label: string }[] = [
    { key: 'definitions', label: t('configuration.views.definitions') },
    { key: 'entries', label: t('configuration.views.entries') },
    { key: 'effective', label: t('configuration.views.effective') },
  ];

  return (
    <PageContainer>
      <PageHeader
        title={t('configuration.title')}
        subtitle={t('configuration.subtitle')}
        badge={<Settings size={22} className="text-accent" />}
      />

      {/* Filters Bar */}
      <Card className="mb-6">
        <CardBody>
          <div className="flex flex-wrap items-end gap-4">
            {/* Scope */}
            <div className="min-w-[160px]">
              <label className="block text-xs font-medium text-muted mb-1">
                {t('configuration.filters.scope')}
              </label>
              <div className="relative">
                <select
                  value={scope}
                  onChange={(e) => setScope(e.target.value as ConfigurationScope)}
                  className="w-full text-sm bg-elevated border border-edge rounded px-3 py-2 text-heading appearance-none focus:outline-none focus:ring-1 focus:ring-accent pr-8"
                >
                  {SCOPES.map((s) => (
                    <option key={s} value={s}>
                      {t(`configuration.scope.${s.toLowerCase()}`)}
                    </option>
                  ))}
                </select>
                <ChevronDown
                  size={14}
                  className="absolute right-2 top-1/2 -translate-y-1/2 text-muted pointer-events-none"
                />
              </div>
            </div>

            {/* Scope Reference ID */}
            {scope !== 'System' && (
              <div className="min-w-[200px]">
                <label className="block text-xs font-medium text-muted mb-1">
                  {t('configuration.filters.scopeReferenceId')}
                </label>
                <input
                  type="text"
                  value={scopeRefId}
                  onChange={(e) => setScopeRefId(e.target.value)}
                  placeholder={t('configuration.filters.scopeReferenceIdPlaceholder')}
                  className="w-full text-sm bg-elevated border border-edge rounded px-3 py-2 text-heading focus:outline-none focus:ring-1 focus:ring-accent"
                />
              </div>
            )}

            {/* Search */}
            <div className="min-w-[220px] flex-1">
              <label className="block text-xs font-medium text-muted mb-1">
                {t('configuration.filters.search')}
              </label>
              <div className="relative">
                <Search
                  size={14}
                  className="absolute left-3 top-1/2 -translate-y-1/2 text-muted"
                />
                <input
                  type="text"
                  value={searchTerm}
                  onChange={(e) => setSearchTerm(e.target.value)}
                  placeholder={t('configuration.filters.searchPlaceholder')}
                  className="w-full text-sm bg-elevated border border-edge rounded pl-9 pr-3 py-2 text-heading focus:outline-none focus:ring-1 focus:ring-accent"
                />
              </div>
            </div>

            {/* Category filter */}
            <div className="min-w-[160px]">
              <label className="block text-xs font-medium text-muted mb-1 flex items-center gap-1">
                <Filter size={12} />
                {t('configuration.filters.category')}
              </label>
              <div className="relative">
                <select
                  value={categoryFilter}
                  onChange={(e) => setCategoryFilter(e.target.value)}
                  className="w-full text-sm bg-elevated border border-edge rounded px-3 py-2 text-heading appearance-none focus:outline-none focus:ring-1 focus:ring-accent pr-8"
                >
                  <option value="">{t('configuration.filters.allCategories')}</option>
                  {CATEGORIES.map((c) => (
                    <option key={c} value={c}>
                      {t(`configuration.category.${c.toLowerCase()}`)}
                    </option>
                  ))}
                </select>
                <ChevronDown
                  size={14}
                  className="absolute right-2 top-1/2 -translate-y-1/2 text-muted pointer-events-none"
                />
              </div>
            </div>
          </div>
        </CardBody>
      </Card>

      {/* View Tabs */}
      <div className="flex gap-1 mb-6">
        {views.map((v) => (
          <button
            key={v.key}
            onClick={() => setActiveView(v.key)}
            className={`px-4 py-2 text-sm rounded-t font-medium transition-colors ${
              activeView === v.key
                ? 'bg-surface text-heading border border-edge border-b-0'
                : 'text-muted hover:text-heading'
            }`}
          >
            {v.label}
          </button>
        ))}
      </div>

      {/* Audit Panel (expanded) */}
      {renderAuditPanel()}

      {/* ─── Definitions View ─────────────────────────────────── */}
      {activeView === 'definitions' && (
        <Card className="mb-6">
          <CardHeader>
            <h2 className="text-sm font-semibold text-heading flex items-center gap-2">
              <Shield size={16} className="text-accent" />
              {t('configuration.definitions.title')}
            </h2>
          </CardHeader>
          <CardBody className="p-0">
            {filteredDefinitions.length === 0 ? (
              <div className="px-4 py-12 text-center text-muted text-sm">
                {t('configuration.empty')}
              </div>
            ) : (
              <div className="overflow-x-auto">
                <table className="w-full text-sm">
                  <thead>
                    <tr className="border-b border-edge text-xs text-muted">
                      <th className="text-left px-4 py-3 font-medium">{t('configuration.table.key')}</th>
                      <th className="text-left px-4 py-3 font-medium">{t('configuration.table.category')}</th>
                      <th className="text-left px-4 py-3 font-medium">{t('configuration.table.valueType')}</th>
                      <th className="text-left px-4 py-3 font-medium">{t('configuration.table.defaultValue')}</th>
                      <th className="text-left px-4 py-3 font-medium">{t('configuration.table.scopes')}</th>
                      <th className="text-center px-4 py-3 font-medium">{t('configuration.table.sensitive')}</th>
                      <th className="text-center px-4 py-3 font-medium">{t('configuration.table.inheritable')}</th>
                      <th className="text-right px-4 py-3 font-medium">{t('configuration.table.actions')}</th>
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-edge">
                    {filteredDefinitions.map((def) => (
                      <tr key={def.key} className="hover:bg-hover transition-colors">
                        <td className="px-4 py-3">
                          <div>
                            <button
                              onClick={() => handleToggleAudit(def.key)}
                              className="font-medium text-heading hover:text-accent transition-colors text-left"
                              title={def.description ?? ''}
                            >
                              {def.displayName}
                            </button>
                            <div className="text-[10px] text-muted font-mono">{def.key}</div>
                          </div>
                        </td>
                        <td className="px-4 py-3">
                          <Badge variant={categoryVariant(def.category)}>
                            {t(`configuration.category.${def.category.toLowerCase()}`)}
                          </Badge>
                        </td>
                        <td className="px-4 py-3 text-muted">{def.valueType}</td>
                        <td className="px-4 py-3 text-muted font-mono text-xs">
                          {renderValue(def.defaultValue, def.isSensitive)}
                        </td>
                        <td className="px-4 py-3">
                          <div className="flex flex-wrap gap-1">
                            {def.allowedScopes.map((s) => (
                              <span
                                key={s}
                                className="text-[10px] px-1.5 py-0.5 rounded bg-elevated text-muted"
                              >
                                {s}
                              </span>
                            ))}
                          </div>
                        </td>
                        <td className="px-4 py-3 text-center">
                          {def.isSensitive ? (
                            <Lock size={14} className="inline text-warning" />
                          ) : (
                            <Unlock size={14} className="inline text-muted" />
                          )}
                        </td>
                        <td className="px-4 py-3 text-center">
                          {def.isInheritable ? (
                            <Check size={14} className="inline text-accent" />
                          ) : (
                            <X size={14} className="inline text-muted" />
                          )}
                        </td>
                        <td className="px-4 py-3 text-right">
                          <button
                            onClick={() => handleToggleAudit(def.key)}
                            className="text-muted hover:text-accent transition-colors p-1"
                            title={t('configuration.actions.audit')}
                          >
                            <History size={14} />
                          </button>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            )}
          </CardBody>
        </Card>
      )}

      {/* ─── Configured Values View ───────────────────────────── */}
      {activeView === 'entries' && (
        <Card className="mb-6">
          <CardHeader>
            <h2 className="text-sm font-semibold text-heading flex items-center gap-2">
              <Settings size={16} className="text-accent" />
              {t('configuration.entries.title')}
            </h2>
          </CardHeader>
          <CardBody className="p-0">
            {filteredEntries.length === 0 ? (
              <div className="px-4 py-12 text-center text-muted text-sm">
                {t('configuration.empty')}
              </div>
            ) : (
              <div className="overflow-x-auto">
                <table className="w-full text-sm">
                  <thead>
                    <tr className="border-b border-edge text-xs text-muted">
                      <th className="text-left px-4 py-3 font-medium">{t('configuration.table.key')}</th>
                      <th className="text-left px-4 py-3 font-medium">{t('configuration.table.scope')}</th>
                      <th className="text-left px-4 py-3 font-medium">{t('configuration.table.value')}</th>
                      <th className="text-center px-4 py-3 font-medium">{t('configuration.table.active')}</th>
                      <th className="text-left px-4 py-3 font-medium">{t('configuration.table.version')}</th>
                      <th className="text-left px-4 py-3 font-medium">{t('configuration.table.updatedBy')}</th>
                      <th className="text-left px-4 py-3 font-medium">{t('configuration.table.updatedAt')}</th>
                      <th className="text-right px-4 py-3 font-medium">{t('configuration.table.actions')}</th>
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-edge">
                    {filteredEntries.map((entry) => {
                      const def = definitionMap.get(entry.definitionKey);
                      const isSensitive = def?.isSensitive ?? false;
                      return (
                        <tr key={entry.id} className="hover:bg-hover transition-colors">
                          <td className="px-4 py-3">
                            <button
                              onClick={() => handleToggleAudit(entry.definitionKey)}
                              className="font-medium text-heading hover:text-accent transition-colors text-left"
                            >
                              {def?.displayName ?? entry.definitionKey}
                            </button>
                            <div className="text-[10px] text-muted font-mono">
                              {entry.definitionKey}
                            </div>
                          </td>
                          <td className="px-4 py-3">
                            <span className="text-[10px] px-1.5 py-0.5 rounded bg-elevated text-muted">
                              {entry.scope}
                            </span>
                          </td>
                          <td className="px-4 py-3 font-mono text-xs text-muted">
                            {renderValue(entry.value, isSensitive)}
                          </td>
                          <td className="px-4 py-3 text-center">
                            <button
                              onClick={() =>
                                handleToggle(entry.definitionKey, !entry.isActive)
                              }
                              className="inline-flex items-center"
                              title={
                                entry.isActive
                                  ? t('configuration.actions.deactivate')
                                  : t('configuration.actions.activate')
                              }
                            >
                              {entry.isActive ? (
                                <ToggleRight size={20} className="text-accent" />
                              ) : (
                                <ToggleLeft size={20} className="text-muted" />
                              )}
                            </button>
                          </td>
                          <td className="px-4 py-3 text-muted">v{entry.version}</td>
                          <td className="px-4 py-3 text-muted">{entry.updatedBy}</td>
                          <td className="px-4 py-3 text-muted">
                            {new Date(entry.updatedAt).toLocaleString()}
                          </td>
                          <td className="px-4 py-3 text-right">
                            <div className="flex items-center justify-end gap-1">
                              {(def?.isEditable ?? true) && (
                                <button
                                  onClick={() =>
                                    handleEdit(entry.definitionKey, entry.value)
                                  }
                                  className="text-muted hover:text-accent transition-colors p-1"
                                  title={t('configuration.actions.edit')}
                                >
                                  <Edit3 size={14} />
                                </button>
                              )}
                              <button
                                onClick={() => handleRemoveOverride(entry.definitionKey)}
                                className="text-muted hover:text-danger transition-colors p-1"
                                title={t('configuration.actions.removeOverride')}
                              >
                                <Trash2 size={14} />
                              </button>
                              <button
                                onClick={() => handleToggleAudit(entry.definitionKey)}
                                className="text-muted hover:text-accent transition-colors p-1"
                                title={t('configuration.actions.audit')}
                              >
                                <History size={14} />
                              </button>
                            </div>
                          </td>
                        </tr>
                      );
                    })}
                  </tbody>
                </table>
              </div>
            )}
          </CardBody>
        </Card>
      )}

      {/* ─── Effective Settings View ──────────────────────────── */}
      {activeView === 'effective' && (
        <Card className="mb-6">
          <CardHeader>
            <h2 className="text-sm font-semibold text-heading flex items-center gap-2">
              <Shield size={16} className="text-accent" />
              {t('configuration.effective.title')}
            </h2>
          </CardHeader>
          <CardBody className="p-0">
            {filteredEffective.length === 0 ? (
              <div className="px-4 py-12 text-center text-muted text-sm">
                {t('configuration.empty')}
              </div>
            ) : (
              <div className="overflow-x-auto">
                <table className="w-full text-sm">
                  <thead>
                    <tr className="border-b border-edge text-xs text-muted">
                      <th className="text-left px-4 py-3 font-medium">{t('configuration.table.key')}</th>
                      <th className="text-left px-4 py-3 font-medium">{t('configuration.table.effectiveValue')}</th>
                      <th className="text-left px-4 py-3 font-medium">{t('configuration.table.resolvedScope')}</th>
                      <th className="text-left px-4 py-3 font-medium">{t('configuration.table.inheritance')}</th>
                      <th className="text-left px-4 py-3 font-medium">{t('configuration.table.valueType')}</th>
                      <th className="text-center px-4 py-3 font-medium">{t('configuration.table.sensitive')}</th>
                      <th className="text-left px-4 py-3 font-medium">{t('configuration.table.version')}</th>
                      <th className="text-right px-4 py-3 font-medium">{t('configuration.table.actions')}</th>
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-edge">
                    {filteredEffective.map((eff) => {
                      const def = definitionMap.get(eff.definitionKey);
                      return (
                        <tr key={eff.key} className="hover:bg-hover transition-colors">
                          <td className="px-4 py-3">
                            <button
                              onClick={() => handleToggleAudit(eff.key)}
                              className="font-medium text-heading hover:text-accent transition-colors text-left"
                            >
                              {def?.displayName ?? eff.key}
                            </button>
                            <div className="text-[10px] text-muted font-mono">{eff.key}</div>
                          </td>
                          <td className="px-4 py-3 font-mono text-xs text-muted">
                            {renderValue(eff.effectiveValue, eff.isSensitive)}
                          </td>
                          <td className="px-4 py-3">
                            <span className="text-[10px] px-1.5 py-0.5 rounded bg-elevated text-muted">
                              {eff.resolvedScope}
                            </span>
                          </td>
                          <td className="px-4 py-3">
                            {eff.isDefault ? (
                              <Badge variant="default">
                                {t('configuration.inheritance.default')}
                              </Badge>
                            ) : eff.isInherited ? (
                              <Badge variant="warning">
                                {t('configuration.inheritance.inherited')}
                              </Badge>
                            ) : (
                              <Badge variant="success">
                                {t('configuration.inheritance.override')}
                              </Badge>
                            )}
                          </td>
                          <td className="px-4 py-3 text-muted">{eff.valueType}</td>
                          <td className="px-4 py-3 text-center">
                            {eff.isSensitive ? (
                              <Lock size={14} className="inline text-warning" />
                            ) : (
                              <Unlock size={14} className="inline text-muted" />
                            )}
                          </td>
                          <td className="px-4 py-3 text-muted">v{eff.version}</td>
                          <td className="px-4 py-3 text-right">
                            <div className="flex items-center justify-end gap-1">
                              {(def?.isEditable ?? true) && (
                                <button
                                  onClick={() =>
                                    handleEdit(eff.key, eff.effectiveValue)
                                  }
                                  className="text-muted hover:text-accent transition-colors p-1"
                                  title={t('configuration.actions.edit')}
                                >
                                  <Edit3 size={14} />
                                </button>
                              )}
                              <button
                                onClick={() => handleToggleAudit(eff.key)}
                                className="text-muted hover:text-accent transition-colors p-1"
                                title={t('configuration.actions.audit')}
                              >
                                <History size={14} />
                              </button>
                            </div>
                          </td>
                        </tr>
                      );
                    })}
                  </tbody>
                </table>
              </div>
            )}
          </CardBody>
        </Card>
      )}

      {/* Edit Modal */}
      {renderEditModal()}
    </PageContainer>
  );
}
