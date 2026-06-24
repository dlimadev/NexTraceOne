import { useState, useMemo, useCallback } from 'react';
import { useTranslation } from 'react-i18next';
import {
  FileCode2,
  Ruler,
  FileText,
  Upload,
  Zap,
  BarChart3,
  ChevronDown,
  ChevronUp,
  Pencil,
  Filter,
  Check,
  X,
  Lock,
  RefreshCw,
  Layers,
  GitBranch,
} from 'lucide-react';
import { Card, CardBody } from '../../../components/Card';
import { Badge } from '../../../components/Badge';
import { PageContainer } from '../../../components/shell';
import { PageHeader } from '../../../components/PageHeader';
import { PageLoadingState } from '../../../components/PageLoadingState';
import { PageErrorState } from '../../../components/PageErrorState';
import {
  Button,
  IconButton,
  TextField,
  TextArea,
  Select,
  SearchInput,
  Tabs,
  Toggle,
} from '../../../shared/ui';
import {
  useConfigurationDefinitions,
  useEffectiveSettings,
  useSetConfigurationValue,
  useAuditHistory,
} from '../../configuration/hooks/useConfiguration';
import type {
  ConfigurationDefinitionDto,
  EffectiveConfigurationDto,
  ConfigurationScope,
} from '../../configuration/types';

// ── Section Definitions ────────────────────────────────────────────────

type CatalogSection =
  | 'contracts'
  | 'validation'
  | 'requirements'
  | 'publication'
  | 'importExport'
  | 'changeTypes'
  | 'releaseScoring';

interface SectionMeta {
  key: CatalogSection;
  icon: React.ReactNode;
  prefixes: string[];
}

const SECTIONS: SectionMeta[] = [
  {
    key: 'contracts',
    icon: <FileCode2 className="w-4 h-4" />,
    prefixes: [
      'catalog.contract.',
    ],
  },
  {
    key: 'validation',
    icon: <Ruler className="w-4 h-4" />,
    prefixes: [
      'catalog.validation.',
      'catalog.templates.',
    ],
  },
  {
    key: 'requirements',
    icon: <FileText className="w-4 h-4" />,
    prefixes: [
      'catalog.requirements.',
    ],
  },
  {
    key: 'publication',
    icon: <GitBranch className="w-4 h-4" />,
    prefixes: [
      'catalog.publication.',
    ],
  },
  {
    key: 'importExport',
    icon: <Upload className="w-4 h-4" />,
    prefixes: [
      'catalog.import.',
      'catalog.export.',
    ],
  },
  {
    key: 'changeTypes',
    icon: <Zap className="w-4 h-4" />,
    prefixes: [
      'change.types_enabled',
      'change.criticality_defaults',
      'change.risk_classification',
      'change.blast_radius.',
      'change.severity_criteria',
    ],
  },
  {
    key: 'releaseScoring',
    icon: <BarChart3 className="w-4 h-4" />,
    prefixes: [
      'change.release_score.',
      'change.evidence_pack.',
      'change.rollback.',
      'change.release_calendar.',
      'change.incident_correlation.',
    ],
  },
];

const SCOPES: ConfigurationScope[] = ['System', 'Tenant', 'Environment', 'Role', 'Team', 'User'];

const MAX_JSON_PREVIEW_LENGTH = 200;

// ── Component ──────────────────────────────────────────────────────────

export function CatalogContractsConfigurationPage() {
  const { t } = useTranslation();
  const [activeSection, setActiveSection] = useState<CatalogSection>('contracts');
  const [scope, setScope] = useState<ConfigurationScope>('System');
  const [scopeReferenceId, setScopeReferenceId] = useState('');
  const [search, setSearch] = useState('');
  const [editingKey, setEditingKey] = useState<string | null>(null);
  const [editValue, setEditValue] = useState('');
  const [editReason, setEditReason] = useState('');
  const [expandedAudit, setExpandedAudit] = useState<string | null>(null);

  const {
    data: definitions,
    isLoading: definitionsLoading,
    error: definitionsError,
    refetch: refetchDefinitions,
  } = useConfigurationDefinitions();

  const {
    data: effective,
  } = useEffectiveSettings(scope, scopeReferenceId || undefined);

  const setValueMutation = useSetConfigurationValue();

  // Tabs items para as secções
  const tabItems = useMemo(
    () =>
      SECTIONS.map((s) => ({
        id: s.key,
        label: t(`catalogContractsConfig.sections.${s.key}`, s.key),
        icon: s.icon,
      })),
    [t],
  );

  // Opções do scope Select
  const scopeOptions = useMemo(
    () => SCOPES.map((s) => ({ value: s, label: t(`configuration.scope.${s.toLowerCase()}`, s) })),
    [t],
  );

  // Filter definitions to catalog/change-related ones
  const catalogDefinitions = useMemo(() => {
    if (!definitions) return [];
    return definitions.filter((d) => d.key.startsWith('catalog.') || d.key.startsWith('change.'));
  }, [definitions]);

  // Get definitions for the active section
  const sectionDefinitions = useMemo(() => {
    const section = SECTIONS.find((s) => s.key === activeSection);
    if (!section || !catalogDefinitions.length) return [];
    return catalogDefinitions.filter((d) =>
      section.prefixes.some((p) => d.key.startsWith(p))
    );
  }, [catalogDefinitions, activeSection]);

  // Apply search filter
  const filteredDefinitions = useMemo(() => {
    if (!search) return sectionDefinitions;
    const q = search.toLowerCase();
    return sectionDefinitions.filter(
      (d) =>
        d.key.toLowerCase().includes(q) ||
        d.displayName.toLowerCase().includes(q) ||
        (d.description && d.description.toLowerCase().includes(q))
    );
  }, [sectionDefinitions, search]);

  // Map effective values by key
  const effectiveMap = useMemo(() => {
    if (!effective) return new Map<string, EffectiveConfigurationDto>();
    const map = new Map<string, EffectiveConfigurationDto>();
    effective.forEach((e) => map.set(e.key, e));
    return map;
  }, [effective]);

  const handleEdit = useCallback(
    (def: ConfigurationDefinitionDto) => {
      const eff = effectiveMap.get(def.key);
      setEditingKey(def.key);
      setEditValue(eff?.effectiveValue ?? def.defaultValue ?? '');
      setEditReason('');
    },
    [effectiveMap]
  );

  const handleSave = useCallback(async () => {
    if (!editingKey) return;
    try {
      await setValueMutation.mutateAsync({
        key: editingKey,
        data: {
          scope,
          scopeReferenceId: scopeReferenceId || null,
          value: editValue,
          changeReason: editReason || undefined,
        },
      });
      setEditingKey(null);
      setEditValue('');
      setEditReason('');
    } catch {
      // handled by mutation state
    }
  }, [editingKey, editValue, editReason, scope, scopeReferenceId, setValueMutation]);

  const handleCancelEdit = useCallback(() => {
    setEditingKey(null);
    setEditValue('');
    setEditReason('');
  }, []);

  // ── Loading / Error states ────────────────────────────────────────

  if (definitionsLoading) {
    return (
      <PageContainer>
        <PageLoadingState />
      </PageContainer>
    );
  }

  if (definitionsError) {
    return (
      <PageContainer>
        <PageErrorState
          title={t('catalogContractsConfig.error.title', 'Error loading configuration')}
          message={t(
            'catalogContractsConfig.error.message',
            'Could not load catalog, contracts and change governance configuration.'
          )}
          action={
            <Button
              variant="primary"
              icon={<RefreshCw className="w-4 h-4" />}
              onClick={() => refetchDefinitions()}
            >
              {t('catalogContractsConfig.error.retry', 'Retry')}
            </Button>
          }
        />
      </PageContainer>
    );
  }

  return (
    <PageContainer>
      <PageHeader
        title={t('catalogContractsConfig.title', 'Catalog, Contracts & Change Governance Configuration')}
        subtitle={t(
          'catalogContractsConfig.subtitle',
          'Configure contract types, versioning, validation, publication policies, change types, blast radius and release scoring'
        )}
      />

      {/* ── Section Tabs ─────────────────────────────────────────── */}
      <Tabs
        variant="pill"
        items={tabItems}
        activeId={activeSection}
        onChange={(id) => setActiveSection(id as CatalogSection)}
        className="mb-6 flex-wrap h-auto"
      />

      {/* ── Scope & Search Controls ──────────────────────────────── */}
      <Card className="mb-6">
        <CardBody>
          <div className="flex flex-wrap gap-4 items-end">
            <div className="flex-1 min-w-[200px]">
              <Select
                label={t('catalogContractsConfig.scope', 'Scope')}
                options={scopeOptions}
                value={scope}
                onChange={(e) => setScope(e.target.value as ConfigurationScope)}
                size="sm"
              />
            </div>
            {scope !== 'System' && (
              <div className="flex-1 min-w-[200px]">
                <TextField
                  label={t('catalogContractsConfig.scopeReference', 'Scope Reference ID')}
                  value={scopeReferenceId}
                  onChange={(e) => setScopeReferenceId(e.target.value)}
                  placeholder={t('catalogContractsConfig.scopeReferencePlaceholder', 'Enter tenant/environment ID...')}
                  size="sm"
                />
              </div>
            )}
            <div className="flex-1 min-w-[200px]">
              <SearchInput
                value={search}
                onChange={(e) => setSearch(e.target.value)}
                placeholder={t('catalogContractsConfig.searchPlaceholder', 'Search by key or name...')}
                aria-label={t('catalogContractsConfig.search', 'Search')}
                size="sm"
              />
            </div>
          </div>
        </CardBody>
      </Card>

      {/* ── Definitions Table / Cards ────────────────────────────── */}
      {filteredDefinitions.length === 0 ? (
        <Card>
          <CardBody>
            <div className="text-center py-12 text-faded">
              <Filter className="w-8 h-8 mx-auto mb-3 opacity-50" />
              <p className="font-medium">
                {t('catalogContractsConfig.empty.title', 'No definitions found')}
              </p>
              <p className="text-sm mt-1">
                {t(
                  'catalogContractsConfig.empty.description',
                  'No catalog or change governance configuration definitions match the current filters.'
                )}
              </p>
            </div>
          </CardBody>
        </Card>
      ) : (
        <div className="space-y-3">
          {filteredDefinitions.map((def) => {
            const eff = effectiveMap.get(def.key);
            const isEditing = editingKey === def.key;
            const isAuditExpanded = expandedAudit === def.key;

            return (
              <Card key={def.key}>
                <CardBody>
                  <div className="flex items-start justify-between gap-4">
                    <div className="flex-1 min-w-0">
                      <div className="flex items-center gap-2 mb-1">
                        <h3 className="text-sm font-semibold text-heading truncate">
                          {def.displayName}
                        </h3>
                        <Badge
                          variant={def.valueType === 'Json' ? 'info' : def.valueType === 'Boolean' ? 'success' : 'default'}
                        >
                          {def.valueType}
                        </Badge>
                        {def.isSensitive && (
                          <Badge variant="warning">
                            <Lock className="w-3 h-3 mr-1" />
                            {t('catalogContractsConfig.badges.sensitive', 'Sensitive')}
                          </Badge>
                        )}
                        {!def.isInheritable && (
                          <Badge variant="default">
                            {t('catalogContractsConfig.badges.nonInheritable', 'Non-inheritable')}
                          </Badge>
                        )}
                      </div>
                      <p className="text-xs text-faded font-mono mb-1">
                        {def.key}
                      </p>
                      {def.description && (
                        <p className="text-xs text-faded mt-1">
                          {def.description}
                        </p>
                      )}

                      {/* Effective Value Display */}
                      {eff && !isEditing && (
                        <div className="mt-3 p-3 rounded-lg bg-subtle border border-edge">
                          <div className="flex items-center gap-2 mb-1">
                            <span className="text-xs font-medium text-faded">
                              {t('catalogContractsConfig.effectiveValue', 'Effective Value')}
                            </span>
                            {eff.isDefault && (
                              <Badge variant="default">
                                {t('catalogContractsConfig.badges.default', 'Default')}
                              </Badge>
                            )}
                            {eff.isInherited && (
                              <Badge variant="info">
                                <Layers className="w-3 h-3 mr-1" />
                                {t('catalogContractsConfig.badges.inherited', 'Inherited')}
                              </Badge>
                            )}
                            {!eff.isDefault && !eff.isInherited && (
                              <Badge variant="success">
                                {t('catalogContractsConfig.badges.override', 'Override')}
                              </Badge>
                            )}
                          </div>
                          <div className="text-sm text-heading font-mono break-all">
                            {def.isSensitive
                              ? '••••••••'
                              : def.valueType === 'Json'
                                ? formatJsonPreview(eff.effectiveValue)
                                : eff.effectiveValue ?? '(empty)'}
                          </div>
                          <div className="text-xs text-muted mt-1">
                            {t('catalogContractsConfig.resolvedFrom', 'Resolved from')}: {eff.resolvedScope}
                            {eff.resolvedScopeReferenceId ? ` (${eff.resolvedScopeReferenceId})` : ''}
                          </div>
                        </div>
                      )}

                      {/* Editing Form */}
                      {isEditing && (
                        <div className="mt-3 p-3 rounded-lg bg-info/15 border border-info/25">
                          {def.valueType === 'Boolean' ? (
                            <>
                              <p className="text-xs font-medium text-body mb-2">
                                {t('catalogContractsConfig.edit.value', 'Value')}
                              </p>
                              <Toggle
                                checked={editValue === 'true'}
                                onChange={(v) => setEditValue(v ? 'true' : 'false')}
                                label={editValue === 'true' ? 'Enabled' : 'Disabled'}
                              />
                            </>
                          ) : def.valueType === 'Json' ? (
                            <TextArea
                              label={t('catalogContractsConfig.edit.value', 'Value')}
                              value={editValue}
                              onChange={(e) => setEditValue(e.target.value)}
                              rows={6}
                              textareaClassName="font-mono"
                            />
                          ) : (
                            <TextField
                              label={t('catalogContractsConfig.edit.value', 'Value')}
                              type={def.valueType === 'Integer' || def.valueType === 'Decimal' ? 'number' : 'text'}
                              value={editValue}
                              onChange={(e) => setEditValue(e.target.value)}
                              size="sm"
                            />
                          )}
                          <div className="mt-3">
                            <TextField
                              label={t('catalogContractsConfig.edit.reason', 'Change Reason')}
                              value={editReason}
                              onChange={(e) => setEditReason(e.target.value)}
                              placeholder={t('catalogContractsConfig.edit.reasonPlaceholder', 'Optional reason for this change...')}
                              size="sm"
                            />
                          </div>
                          <div className="flex gap-2 mt-3">
                            <Button
                              variant="primary"
                              size="sm"
                              icon={<Check className="w-3 h-3" />}
                              onClick={handleSave}
                              disabled={setValueMutation.isPending}
                              loading={setValueMutation.isPending}
                            >
                              {t('catalogContractsConfig.edit.save', 'Save')}
                            </Button>
                            <Button
                              variant="ghost"
                              size="sm"
                              icon={<X className="w-3 h-3" />}
                              onClick={handleCancelEdit}
                            >
                              {t('catalogContractsConfig.edit.cancel', 'Cancel')}
                            </Button>
                          </div>
                        </div>
                      )}
                    </div>

                    {/* Actions */}
                    <div className="flex items-center gap-1 shrink-0">
                      {def.isEditable && !isEditing && (
                        <IconButton
                          icon={<Pencil className="w-4 h-4" />}
                          label={t('catalogContractsConfig.actions.edit', 'Edit')}
                          onClick={() => handleEdit(def)}
                          variant="ghost"
                          size="sm"
                        />
                      )}
                      <IconButton
                        icon={isAuditExpanded ? <ChevronUp className="w-4 h-4" /> : <ChevronDown className="w-4 h-4" />}
                        label={t('catalogContractsConfig.actions.audit', 'Audit History')}
                        onClick={() => setExpandedAudit(isAuditExpanded ? null : def.key)}
                        variant="ghost"
                        size="sm"
                      />
                    </div>
                  </div>

                  {/* Audit History */}
                  {isAuditExpanded && <AuditHistoryPanel configKey={def.key} />}
                </CardBody>
              </Card>
            );
          })}
        </div>
      )}

      {/* ── Summary Footer ───────────────────────────────────────── */}
      <div className="mt-6 text-xs text-muted text-center">
        {t('catalogContractsConfig.footer', '{{count}} catalog, contracts & change governance definitions configured', {
          count: catalogDefinitions.length,
        })}
      </div>
    </PageContainer>
  );
}

// ── Audit History Panel ────────────────────────────────────────────────

function AuditHistoryPanel({ configKey }: { configKey: string }) {
  const { t } = useTranslation();
  const { data: audits, isLoading } = useAuditHistory(configKey);

  if (isLoading) {
    return (
      <div className="mt-3 pt-3 border-t border-edge">
        <p className="text-xs text-muted">{t('catalogContractsConfig.audit.loading', 'Loading audit history...')}</p>
      </div>
    );
  }

  if (!audits || audits.length === 0) {
    return (
      <div className="mt-3 pt-3 border-t border-edge">
        <p className="text-xs text-muted">{t('catalogContractsConfig.audit.empty', 'No audit history available.')}</p>
      </div>
    );
  }

  return (
    <div className="mt-3 pt-3 border-t border-edge">
      <h4 className="text-xs font-semibold text-faded mb-2">
        {t('catalogContractsConfig.audit.title', 'Audit History')}
      </h4>
      <div className="space-y-2 max-h-64 overflow-y-auto">
        {audits.map((audit, idx) => (
          <div
            key={idx}
            className="text-xs bg-subtle rounded-lg p-2 border border-edge"
          >
            <div className="flex justify-between items-center mb-1">
              <Badge variant={audit.action === 'Created' ? 'success' : audit.action === 'Updated' ? 'info' : 'default'}>
                {audit.action}
              </Badge>
              <span className="text-muted">
                {audit.changedAt ? new Date(audit.changedAt).toLocaleString() : ''}
              </span>
            </div>
            {audit.changedBy && (
              <p className="text-faded">
                {t('catalogContractsConfig.audit.by', 'By')}: {audit.changedBy}
              </p>
            )}
            {audit.changeReason && (
              <p className="text-faded italic">
                {t('catalogContractsConfig.audit.reason', 'Reason')}: {audit.changeReason}
              </p>
            )}
            {audit.previousValue && !audit.isSensitive && (
              <p className="text-muted font-mono truncate">
                {audit.previousValue} → {audit.newValue}
              </p>
            )}
          </div>
        ))}
      </div>
    </div>
  );
}

// ── Helper ─────────────────────────────────────────────────────────────

function formatJsonPreview(value: string | null | undefined): string {
  if (!value) return '(empty)';
  try {
    const parsed = JSON.parse(value);
    const formatted = JSON.stringify(parsed, null, 2);
    return formatted.length > MAX_JSON_PREVIEW_LENGTH ? formatted.substring(0, MAX_JSON_PREVIEW_LENGTH) + '...' : formatted;
  } catch {
    return value.length > MAX_JSON_PREVIEW_LENGTH ? value.substring(0, MAX_JSON_PREVIEW_LENGTH) + '...' : value;
  }
}
