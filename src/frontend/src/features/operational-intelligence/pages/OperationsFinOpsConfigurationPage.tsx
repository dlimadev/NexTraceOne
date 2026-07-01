import { useState, useMemo, useCallback } from 'react';
import { useTranslation } from 'react-i18next';
import {
  AlertTriangle,
  Shield,
  Users,
  DollarSign,
  BarChart3,
  Activity,
  ChevronDown,
  ChevronUp,
  Pencil,
  Search,
  Filter,
  Check,
  X,
  Lock,
  RefreshCw,
  Layers,
} from 'lucide-react';
import { Card, CardBody } from '../../../components/Card';
import { Badge } from '../../../components/Badge';
import { Button } from '../../../components/Button';
import { IconButton } from '../../../components/IconButton';
import { TextField } from '../../../components/TextField';
import { TextArea } from '../../../components/TextArea';
import { Select } from '../../../components/Select';
import { SearchInput } from '../../../components/SearchInput';
import { Tabs } from '../../../components/Tabs';
import { PageContainer } from '../../../components/shell';
import { PageHeader } from '../../../components/PageHeader';
import { PageLoadingState } from '../../../components/PageLoadingState';
import { PageErrorState } from '../../../components/PageErrorState';
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

type OpsSection =
  | 'incidentTaxonomy'
  | 'ownersCorrelation'
  | 'playbooksAutomation'
  | 'budgets'
  | 'anomalyWaste'
  | 'benchmarking';

interface SectionMeta {
  key: OpsSection;
  icon: React.ReactNode;
  prefixes: string[];
}

const SECTIONS: SectionMeta[] = [
  {
    key: 'incidentTaxonomy',
    icon: <AlertTriangle className="w-4 h-4" />,
    prefixes: [
      'incidents.taxonomy.',
      'incidents.severity.',
      'incidents.criticality.',
      'incidents.sla.',
    ],
  },
  {
    key: 'ownersCorrelation',
    icon: <Users className="w-4 h-4" />,
    prefixes: [
      'incidents.owner.',
      'incidents.classification.',
      'incidents.correlation.',
      'incidents.auto_creation.',
      'incidents.enrichment.',
    ],
  },
  {
    key: 'playbooksAutomation',
    icon: <Shield className="w-4 h-4" />,
    prefixes: [
      'operations.playbook.',
      'operations.runbook.',
      'operations.automation.',
      'operations.postincident.',
    ],
  },
  {
    key: 'budgets',
    icon: <DollarSign className="w-4 h-4" />,
    prefixes: [
      'finops.budget.',
    ],
  },
  {
    key: 'anomalyWaste',
    icon: <Activity className="w-4 h-4" />,
    prefixes: [
      'finops.anomaly.',
      'finops.waste.',
      'finops.recommendation.',
      'finops.notification.',
      'operations.health.',
    ],
  },
  {
    key: 'benchmarking',
    icon: <BarChart3 className="w-4 h-4" />,
    prefixes: [
      'benchmarking.',
    ],
  },
];

const SCOPES: ConfigurationScope[] = ['System', 'Tenant', 'Environment', 'Role', 'Team', 'User'];

const MAX_JSON_PREVIEW_LENGTH = 200;

// ── Component ──────────────────────────────────────────────────────────

export function OperationsFinOpsConfigurationPage() {
  const { t } = useTranslation();
  const [activeSection, setActiveSection] = useState<OpsSection>('incidentTaxonomy');
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

  // Filter definitions to operations/incidents/finops/benchmarking ones
  const opsDefinitions = useMemo(() => {
    if (!definitions) return [];
    return definitions.filter((d) =>
      d.key.startsWith('incidents.') ||
      d.key.startsWith('operations.') ||
      d.key.startsWith('finops.') ||
      d.key.startsWith('benchmarking.')
    );
  }, [definitions]);

  // Get definitions for the active section
  const sectionDefinitions = useMemo(() => {
    const section = SECTIONS.find((s) => s.key === activeSection);
    if (!section || !opsDefinitions.length) return [];
    return opsDefinitions.filter((d) =>
      section.prefixes.some((p) => d.key.startsWith(p))
    );
  }, [opsDefinitions, activeSection]);

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
          title={t('opsFinOpsConfig.error.title', 'Error loading configuration')}
          message={t(
            'opsFinOpsConfig.error.message',
            'Could not load operations, incidents, FinOps and benchmarking configuration.'
          )}
          action={
            <Button
              variant="primary"
              icon={<RefreshCw className="w-4 h-4" />}
              onClick={() => refetchDefinitions()}
            >
              {t('opsFinOpsConfig.error.retry', 'Retry')}
            </Button>
          }
        />
      </PageContainer>
    );
  }

  return (
    <PageContainer>
      <PageHeader
        title={t('opsFinOpsConfig.title', 'Operations, Incidents, FinOps & Benchmarking Configuration')}
        subtitle={t(
          'opsFinOpsConfig.subtitle',
          'Configure incident taxonomy, SLAs, owners, automation, budgets, anomaly detection, waste policies and benchmarking'
        )}
      />

      {/* ── Section Tabs ─────────────────────────────────────────── */}
      <div className="mb-6">
        <Tabs
          variant="pill"
          items={SECTIONS.map((section) => ({
            id: section.key,
            label: t(`opsFinOpsConfig.sections.${section.key}`, section.key),
            icon: section.icon,
          }))}
          activeId={activeSection}
          onChange={(id) => setActiveSection(id as OpsSection)}
        />
      </div>

      {/* ── Scope & Search Controls ──────────────────────────────── */}
      <Card className="mb-6">
        <CardBody>
          <div className="flex flex-wrap gap-4 items-end">
            <div className="flex-1 min-w-[200px]">
              <Select
                label={t('opsFinOpsConfig.scope', 'Scope')}
                value={scope}
                onChange={(e) => setScope(e.target.value as ConfigurationScope)}
                options={SCOPES.map((s) => ({
                  value: s,
                  label: t(`configuration.scope.${s.toLowerCase()}`, s),
                }))}
              />
            </div>
            {scope !== 'System' && (
              <div className="flex-1 min-w-[200px]">
                <TextField
                  label={t('opsFinOpsConfig.scopeReference', 'Scope Reference ID')}
                  value={scopeReferenceId}
                  onChange={(e) => setScopeReferenceId(e.target.value)}
                  placeholder={t('opsFinOpsConfig.scopeReferencePlaceholder', 'Enter tenant/environment ID...')}
                />
              </div>
            )}
            <div className="flex-1 min-w-[200px]">
              <label className="block text-sm font-medium text-body mb-1">
                <Search className="w-3 h-3 inline mr-1" />
                {t('opsFinOpsConfig.search', 'Search')}
              </label>
              <SearchInput
                value={search}
                onChange={(e) => setSearch(e.target.value)}
                placeholder={t('opsFinOpsConfig.searchPlaceholder', 'Search by key or name...')}
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
                {t('opsFinOpsConfig.empty.title', 'No definitions found')}
              </p>
              <p className="text-sm mt-1">
                {t(
                  'opsFinOpsConfig.empty.description',
                  'No operations, incidents, FinOps or benchmarking definitions match the current filters.'
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
                            {t('opsFinOpsConfig.badges.sensitive', 'Sensitive')}
                          </Badge>
                        )}
                        {!def.isInheritable && (
                          <Badge variant="default">
                            {t('opsFinOpsConfig.badges.nonInheritable', 'Non-inheritable')}
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
                              {t('opsFinOpsConfig.effectiveValue', 'Effective Value')}
                            </span>
                            {eff.isDefault && (
                              <Badge variant="default">
                                {t('opsFinOpsConfig.badges.default', 'Default')}
                              </Badge>
                            )}
                            {eff.isInherited && (
                              <Badge variant="info">
                                <Layers className="w-3 h-3 mr-1" />
                                {t('opsFinOpsConfig.badges.inherited', 'Inherited')}
                              </Badge>
                            )}
                            {!eff.isDefault && !eff.isInherited && (
                              <Badge variant="success">
                                {t('opsFinOpsConfig.badges.override', 'Override')}
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
                            {t('opsFinOpsConfig.resolvedFrom', 'Resolved from')}: {eff.resolvedScope}
                            {eff.resolvedScopeReferenceId ? ` (${eff.resolvedScopeReferenceId})` : ''}
                          </div>
                        </div>
                      )}

                      {/* Editing Form */}
                      {isEditing && (
                        <div className="mt-3 p-3 rounded-lg bg-info/15 border border-info/25">
                          <label className="block text-xs font-medium text-body mb-1">
                            {t('opsFinOpsConfig.edit.value', 'Value')}
                          </label>
                          {def.valueType === 'Boolean' ? (
                            <Button
                              variant={editValue === 'true' ? 'primary' : 'subtle'}
                              size="sm"
                              onClick={() => setEditValue(editValue === 'true' ? 'false' : 'true')}
                            >
                              {editValue === 'true' ? 'Enabled' : 'Disabled'}
                            </Button>
                          ) : def.valueType === 'Json' ? (
                            <TextArea
                              value={editValue}
                              onChange={(e) => setEditValue(e.target.value)}
                              rows={6}
                              textareaClassName="font-mono"
                            />
                          ) : (
                            <TextField
                              type={def.valueType === 'Integer' || def.valueType === 'Decimal' ? 'number' : 'text'}
                              value={editValue}
                              onChange={(e) => setEditValue(e.target.value)}
                            />
                          )}
                          <label className="block text-xs font-medium text-body mt-2 mb-1">
                            {t('opsFinOpsConfig.edit.reason', 'Change Reason')}
                          </label>
                          <TextField
                            type="text"
                            value={editReason}
                            onChange={(e) => setEditReason(e.target.value)}
                            placeholder={t('opsFinOpsConfig.edit.reasonPlaceholder', 'Optional reason for this change...')}
                          />
                          <div className="flex gap-2 mt-3">
                            <Button
                              variant="primary"
                              size="sm"
                              icon={<Check className="w-3 h-3" />}
                              onClick={handleSave}
                              loading={setValueMutation.isPending}
                            >
                              {t('opsFinOpsConfig.edit.save', 'Save')}
                            </Button>
                            <Button
                              variant="subtle"
                              size="sm"
                              icon={<X className="w-3 h-3" />}
                              onClick={handleCancelEdit}
                            >
                              {t('opsFinOpsConfig.edit.cancel', 'Cancel')}
                            </Button>
                          </div>
                        </div>
                      )}
                    </div>

                    {/* Actions */}
                    <div className="flex items-center gap-1 shrink-0">
                      {def.isEditable && !isEditing && (
                        <IconButton
                          variant="ghost"
                          size="sm"
                          icon={<Pencil className="w-4 h-4" />}
                          onClick={() => handleEdit(def)}
                          label={t('opsFinOpsConfig.actions.edit', 'Edit')}
                          title={t('opsFinOpsConfig.actions.edit', 'Edit')}
                        />
                      )}
                      <IconButton
                        variant="ghost"
                        size="sm"
                        icon={isAuditExpanded ? <ChevronUp className="w-4 h-4" /> : <ChevronDown className="w-4 h-4" />}
                        onClick={() => setExpandedAudit(isAuditExpanded ? null : def.key)}
                        label={t('opsFinOpsConfig.actions.audit', 'Audit History')}
                        title={t('opsFinOpsConfig.actions.audit', 'Audit History')}
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
        {t('opsFinOpsConfig.footer', '{{count}} operations, incidents, FinOps & benchmarking definitions configured', {
          count: opsDefinitions.length,
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
        <p className="text-xs text-muted">{t('opsFinOpsConfig.audit.loading', 'Loading audit history...')}</p>
      </div>
    );
  }

  if (!audits || audits.length === 0) {
    return (
      <div className="mt-3 pt-3 border-t border-edge">
        <p className="text-xs text-muted">{t('opsFinOpsConfig.audit.empty', 'No audit history available.')}</p>
      </div>
    );
  }

  return (
    <div className="mt-3 pt-3 border-t border-edge">
      <h4 className="text-xs font-semibold text-faded mb-2">
        {t('opsFinOpsConfig.audit.title', 'Audit History')}
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
                {t('opsFinOpsConfig.audit.by', 'By')}: {audit.changedBy}
              </p>
            )}
            {audit.changeReason && (
              <p className="text-faded italic">
                {t('opsFinOpsConfig.audit.reason', 'Reason')}: {audit.changeReason}
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
