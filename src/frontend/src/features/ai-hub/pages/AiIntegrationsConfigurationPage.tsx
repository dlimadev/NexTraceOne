import { useState, useMemo, useCallback } from 'react';
import { useTranslation } from 'react-i18next';
import type { TFunction } from 'i18next';
import {
  Brain,
  Server,
  DollarSign,
  Shield,
  Plug,
  Clock,
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
import { Checkbox } from '../../../components/Checkbox';
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
} from '../../configuration/types';

// ── Section Definitions ────────────────────────────────────────────────

type AiIntSection =
  | 'providersModels'
  | 'budgetsQuotas'
  | 'promptsRetrieval'
  | 'connectorsSchedules'
  | 'filtersMappings'
  | 'failureGovernance';

interface SectionMeta {
  key: AiIntSection;
  icon: React.ReactNode;
  prefixes: string[];
}

const SECTIONS: SectionMeta[] = [
  {
    key: 'providersModels',
    icon: <Brain className="w-4 h-4" />,
    prefixes: [
      'ai.providers.',
      'ai.models.',
      'ai.usage.',
    ],
  },
  {
    key: 'budgetsQuotas',
    icon: <DollarSign className="w-4 h-4" />,
    prefixes: [
      'ai.budget.',
      'ai.quota.',
    ],
  },
  {
    key: 'promptsRetrieval',
    icon: <Shield className="w-4 h-4" />,
    prefixes: [
      'ai.retention.',
      'ai.audit.',
      'ai.prompts.',
      'ai.retrieval.',
      'ai.defaults.',
    ],
  },
  {
    key: 'connectorsSchedules',
    icon: <Plug className="w-4 h-4" />,
    prefixes: [
      'integrations.connectors.',
      'integrations.schedule.',
      'integrations.retry.',
      'integrations.timeout.',
      'integrations.execution.',
    ],
  },
  {
    key: 'filtersMappings',
    icon: <Server className="w-4 h-4" />,
    prefixes: [
      'integrations.sync.',
      'integrations.import.',
      'integrations.export.',
      'integrations.freshness.',
    ],
  },
  {
    key: 'failureGovernance',
    icon: <Clock className="w-4 h-4" />,
    prefixes: [
      'integrations.failure.',
      'integrations.owner.',
      'integrations.governance.',
    ],
  },
];

// ── Helpers ────────────────────────────────────────────────────────────

function matchSection(key: string, section: SectionMeta): boolean {
  return section.prefixes.some((p) => key.startsWith(p));
}

function renderValuePreview(value: string, t: TFunction): React.ReactNode {
  if (value === 'true') return <Badge variant="success">{t('advancedConfig.badges.enabled', 'Enabled')}</Badge>;
  if (value === 'false') return <Badge variant="default">{t('advancedConfig.badges.disabled', 'Disabled')}</Badge>;
  try {
    const parsed = JSON.parse(value);
    if (Array.isArray(parsed)) return <Badge variant="info">{parsed.length} items</Badge>;
    if (typeof parsed === 'object') return <Badge variant="info">{Object.keys(parsed).length} fields</Badge>;
  } catch {
    /* not JSON */
  }
  return <span className="text-sm text-faded truncate max-w-[200px] inline-block">{value}</span>;
}

// ── Component ──────────────────────────────────────────────────────────

export function AiIntegrationsConfigurationPage() {
  const { t } = useTranslation();
  const [activeSection, setActiveSection] = useState<AiIntSection>('providersModels');
  const [expandedDef, setExpandedDef] = useState<string | null>(null);
  const [editingDef, setEditingDef] = useState<string | null>(null);
  const [editValue, setEditValue] = useState('');
  const [searchQuery, setSearchQuery] = useState('');
  const [showEffective, setShowEffective] = useState(false);

  // ── Data hooks ─────────────────────────────────────────────────────
  const { data: definitions, isLoading: loadingDefs, isError: errorDefs, refetch: refetchDefs } = useConfigurationDefinitions();
  const { data: effective } = useEffectiveSettings('System');
  const { mutateAsync: setValue } = useSetConfigurationValue();
  const { data: auditHistory } = useAuditHistory(null);

  // ── Filtered definitions ───────────────────────────────────────────
  const phase7Defs = useMemo(() => {
    if (!definitions) return [];
    return definitions.filter(
      (d: ConfigurationDefinitionDto) =>
        d.key.startsWith('ai.') || d.key.startsWith('integrations.')
    );
  }, [definitions]);

  const sectionDefs = useMemo(() => {
    const section = SECTIONS.find((s) => s.key === activeSection);
    if (!section) return [];
    return phase7Defs.filter((d: ConfigurationDefinitionDto) => {
      const matchesSec = matchSection(d.key, section);
      const matchesSearch =
        !searchQuery ||
        d.key.toLowerCase().includes(searchQuery.toLowerCase()) ||
        d.displayName.toLowerCase().includes(searchQuery.toLowerCase());
      return matchesSec && matchesSearch;
    });
  }, [phase7Defs, activeSection, searchQuery]);

  // ── Effective lookup ───────────────────────────────────────────────
  const getEffective = useCallback(
    (key: string): EffectiveConfigurationDto | undefined => {
      return effective?.find((e: EffectiveConfigurationDto) => e.key === key);
    },
    [effective]
  );

  // ── Edit handlers ──────────────────────────────────────────────────
  const startEdit = useCallback((def: ConfigurationDefinitionDto) => {
    const eff = effective?.find((e: EffectiveConfigurationDto) => e.key === def.key);
    setEditingDef(def.key);
    setEditValue(eff?.effectiveValue ?? def.defaultValue ?? '');
  }, [effective]);

  const cancelEdit = useCallback(() => {
    setEditingDef(null);
    setEditValue('');
  }, []);

  const saveEdit = useCallback(async (key: string) => {
    await setValue({ key, data: { value: editValue, scope: 'System' } });
    setEditingDef(null);
    setEditValue('');
  }, [editValue, setValue]);

  // ── Loading / Error / Empty ────────────────────────────────────────
  if (loadingDefs) {
    return <PageLoadingState message={t('aiIntegrationsConfig.loading', 'Loading AI & Integrations configuration...')} />;
  }

  if (errorDefs) {
    return (
      <PageErrorState
        title={t('aiIntegrationsConfig.errorTitle', 'Failed to load configuration')}
        message={t('aiIntegrationsConfig.errorMessage', 'Unable to load AI & integrations configuration definitions.')}
        action={<Button variant="primary" size="sm" onClick={() => refetchDefs()}>{t('common.retry', 'Retry')}</Button>}
      />
    );
  }

  if (phase7Defs.length === 0) {
    return (
      <PageContainer>
        <PageHeader
          title={t('aiIntegrationsConfig.title', 'AI & Integrations Configuration')}
          subtitle={t('aiIntegrationsConfig.subtitle', 'Manage AI providers, models, budgets, connectors, and integration governance')}
        />
        <Card>
          <CardBody>
            <p className="text-faded">
              {t('aiIntegrationsConfig.empty', 'No AI & integrations configuration definitions found. Ensure Phase 7 definitions have been seeded.')}
            </p>
          </CardBody>
        </Card>
      </PageContainer>
    );
  }

  return (
    <PageContainer>
      <PageHeader
        title={t('aiIntegrationsConfig.title', 'AI & Integrations Configuration')}
        subtitle={t('aiIntegrationsConfig.subtitle', 'Manage AI providers, models, budgets, connectors, and integration governance')}
      />

      {/* ── Section Tabs ──────────────────────────────────────────────── */}
      <div className="mb-6">
        <Tabs
          variant="pill"
          size="sm"
          items={SECTIONS.map((section) => ({
            id: section.key,
            label: t(`aiIntegrationsConfig.sections.${section.key}`, section.key),
            icon: section.icon,
          }))}
          activeId={activeSection}
          onChange={(id) => setActiveSection(id as AiIntSection)}
        />
      </div>

      {/* ── Search & Filters ──────────────────────────────────────────── */}
      <div className="flex gap-4 mb-4">
        <div className="flex-1">
          <TextField
            size="sm"
            leadingIcon={<Search className="w-4 h-4" />}
            value={searchQuery}
            onChange={(e) => setSearchQuery(e.target.value)}
            placeholder={t('aiIntegrationsConfig.searchPlaceholder', 'Search configuration...')}
          />
        </div>
        <Button
          variant={showEffective ? 'primary' : 'outline'}
          size="sm"
          onClick={() => setShowEffective(!showEffective)}
        >
          <Layers className="w-4 h-4 mr-2" />
          {t('aiIntegrationsConfig.effectiveSettings', 'Effective Settings')}
        </Button>
      </div>

      {/* ── Definitions List ──────────────────────────────────────────── */}
      <div className="space-y-3">
        {sectionDefs.map((def: ConfigurationDefinitionDto) => {
          const eff = getEffective(def.key);
          const isExpanded = expandedDef === def.key;
          const isEditing = editingDef === def.key;

          return (
            <Card key={def.key}>
              <CardBody>
                <div className="flex items-start justify-between">
                  <div className="flex-1">
                    <div className="flex items-center gap-2">
                      <button
                        onClick={() => setExpandedDef(isExpanded ? null : def.key)}
                        className="flex items-center gap-1 text-left"
                      >
                        {isExpanded ? <ChevronUp className="w-4 h-4" /> : <ChevronDown className="w-4 h-4" />}
                        <span className="font-medium text-sm">{def.displayName}</span>
                      </button>
                      {!def.isInheritable && (
                        <Badge variant="warning" className="text-xs">
                          <Lock className="w-3 h-3 mr-1" />
                          {t('aiIntegrationsConfig.mandatory', 'Mandatory')}
                        </Badge>
                      )}
                    </div>
                    <p className="text-xs text-faded mt-1 ml-5">{def.key}</p>
                  </div>

                  <div className="flex items-center gap-3">
                    {showEffective && eff ? (
                      <div className="text-right">
                        <div className="text-xs text-muted">{eff.resolvedScope}</div>
                        {renderValuePreview(eff.effectiveValue ?? '', t)}
                      </div>
                    ) : (
                      renderValuePreview(def.defaultValue ?? '', t)
                    )}
                    {def.isEditable !== false && (
                      <IconButton
                        variant="ghost"
                        size="sm"
                        onClick={() => startEdit(def)}
                        label={t('common.edit', 'Edit')}
                        icon={<Pencil className="w-4 h-4" />}
                      />
                    )}
                  </div>
                </div>

                {/* ── Expanded Detail ────────────────────────────────── */}
                {isExpanded && (
                  <div className="mt-4 pt-4 border-t border-edge space-y-3">
                    <div className="grid grid-cols-2 md:grid-cols-4 gap-3 text-xs">
                      <div>
                        <span className="text-muted">{t('aiIntegrationsConfig.detail.type', 'Type')}</span>
                        <p className="font-medium">{def.valueType}</p>
                      </div>
                      <div>
                        <span className="text-muted">{t('aiIntegrationsConfig.detail.scopes', 'Scopes')}</span>
                        <p className="font-medium">{def.allowedScopes?.join(', ')}</p>
                      </div>
                      <div>
                        <span className="text-muted">{t('aiIntegrationsConfig.detail.editor', 'Editor')}</span>
                        <p className="font-medium">{def.uiEditorType}</p>
                      </div>
                      <div>
                        <span className="text-muted">{t('aiIntegrationsConfig.detail.inheritable', 'Inheritable')}</span>
                        <p className="font-medium">{def.isInheritable ? 'Yes' : 'No'}</p>
                      </div>
                    </div>
                    {def.description && (
                      <p className="text-xs text-faded">{def.description}</p>
                    )}
                    <div>
                      <span className="text-xs text-muted">{t('aiIntegrationsConfig.detail.defaultValue', 'Default Value')}</span>
                      <pre className="mt-1 p-2 bg-subtle rounded text-xs overflow-x-auto">
                        {def.defaultValue}
                      </pre>
                    </div>
                    {showEffective && eff && (
                      <div className="p-3 bg-accent/10 rounded-lg">
                        <div className="flex items-center gap-2 text-xs text-accent mb-1">
                          <Layers className="w-3 h-3" />
                          {t('aiIntegrationsConfig.effectiveValue', 'Effective Value')}
                          <Badge variant="info" className="text-xs">{eff.resolvedScope}</Badge>
                        </div>
                        <pre className="text-xs overflow-x-auto">{eff.effectiveValue}</pre>
                      </div>
                    )}
                  </div>
                )}

                {/* ── Inline Editor ──────────────────────────────────── */}
                {isEditing && (
                  <div className="mt-4 pt-4 border-t border-edge">
                    {def.uiEditorType === 'toggle' ? (
                      <Checkbox
                        checked={editValue === 'true'}
                        onChange={(e) => setEditValue(e.target.checked ? 'true' : 'false')}
                        label={editValue === 'true' ? t('advancedConfig.badges.enabled', 'Enabled') : t('advancedConfig.badges.disabled', 'Disabled')}
                      />
                    ) : def.uiEditorType === 'select' ? (
                      <Select
                        size="sm"
                        value={editValue}
                        onChange={(e) => setEditValue(e.target.value)}
                        options={(() => {
                          if (!def.validationRules) return [];
                          try {
                            const rules = JSON.parse(def.validationRules);
                            return (rules.enum ?? []).map((opt: string) => ({ value: opt, label: opt }));
                          } catch { return []; }
                        })()}
                      />
                    ) : def.uiEditorType === 'json-editor' ? (
                      <TextArea
                        textareaClassName="font-mono"
                        value={editValue}
                        onChange={(e) => setEditValue(e.target.value)}
                        rows={6}
                      />
                    ) : (
                      <TextField
                        size="sm"
                        value={editValue}
                        onChange={(e) => setEditValue(e.target.value)}
                      />
                    )}
                    <div className="flex gap-2 mt-3">
                      <Button variant="primary" size="sm" onClick={() => saveEdit(def.key)}>
                        <Check className="w-3 h-3 mr-1" />
                        {t('common.save', 'Save')}
                      </Button>
                      <Button variant="secondary" size="sm" onClick={cancelEdit}>
                        <X className="w-3 h-3 mr-1" />
                        {t('common.cancel', 'Cancel')}
                      </Button>
                    </div>
                  </div>
                )}
              </CardBody>
            </Card>
          );
        })}

        {sectionDefs.length === 0 && (
          <Card>
            <CardBody>
              <div className="text-center py-8 text-faded">
                <Filter className="w-8 h-8 mx-auto mb-2 opacity-50" />
                <p>{t('aiIntegrationsConfig.noResults', 'No matching configuration definitions found.')}</p>
              </div>
            </CardBody>
          </Card>
        )}
      </div>

      {/* ── Audit History ─────────────────────────────────────────────── */}
      {auditHistory && auditHistory.length > 0 && (
        <div className="mt-8">
          <h3 className="text-lg font-semibold mb-4 flex items-center gap-2">
            <RefreshCw className="w-5 h-5" />
            {t('aiIntegrationsConfig.auditHistory', 'Recent Changes')}
          </h3>
          <Card>
            <CardBody>
              <div className="space-y-2">
                {auditHistory.slice(0, 10).map((entry) => (
                  <div key={`${entry.changedAt}-${entry.key}`} className="flex items-center justify-between py-2 border-b border-edge last:border-0">
                    <div>
                      <span className="text-sm font-medium">{entry.key}</span>
                      <span className="text-xs text-muted ml-2">{entry.changedBy}</span>
                    </div>
                    <span className="text-xs text-muted">{new Date(entry.changedAt).toLocaleString()}</span>
                  </div>
                ))}
              </div>
            </CardBody>
          </Card>
        </div>
      )}
    </PageContainer>
  );
}
