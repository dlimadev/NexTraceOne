import { useState, useMemo, useCallback } from 'react';
import { useTranslation } from 'react-i18next';
import {
  Bell,
  Mail,
  MessageSquare,
  Shield,
  Clock,
  AlertTriangle,
  ChevronDown,
  ChevronUp,
  Edit3,
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

type NotificationSection =
  | 'types'
  | 'channels'
  | 'templates'
  | 'routing'
  | 'consumption'
  | 'escalation';

interface SectionMeta {
  key: NotificationSection;
  icon: React.ReactNode;
  prefixes: string[];
}

const SECTIONS: SectionMeta[] = [
  {
    key: 'types',
    icon: <Bell className="w-4 h-4" />,
    prefixes: [
      'notifications.types.',
      'notifications.categories.',
      'notifications.severity.',
      'notifications.mandatory.',
    ],
  },
  {
    key: 'channels',
    icon: <Mail className="w-4 h-4" />,
    prefixes: [
      'notifications.channels.',
      'notifications.email.enabled',
      'notifications.teams.enabled',
      'notifications.enabled',
    ],
  },
  {
    key: 'templates',
    icon: <MessageSquare className="w-4 h-4" />,
    prefixes: ['notifications.templates.'],
  },
  {
    key: 'routing',
    icon: <Shield className="w-4 h-4" />,
    prefixes: ['notifications.routing.'],
  },
  {
    key: 'consumption',
    icon: <Clock className="w-4 h-4" />,
    prefixes: [
      'notifications.preferences.',
      'notifications.quiet_hours.',
      'notifications.digest.',
      'notifications.suppress.',
      'notifications.acknowledge.',
    ],
  },
  {
    key: 'escalation',
    icon: <AlertTriangle className="w-4 h-4" />,
    prefixes: [
      'notifications.dedup.',
      'notifications.escalation.',
      'notifications.incident_linkage.',
      'notifications.grouping.',
    ],
  },
];

const SCOPES: ConfigurationScope[] = ['System', 'Tenant', 'Environment', 'Role', 'Team', 'User'];

// ── Component ──────────────────────────────────────────────────────────

export function NotificationConfigurationPage() {
  const { t } = useTranslation();
  const [activeSection, setActiveSection] = useState<NotificationSection>('types');
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

  // Filter definitions to only notification-related ones
  const notificationDefinitions = useMemo(() => {
    if (!definitions) return [];
    return definitions.filter((d) => d.key.startsWith('notifications.'));
  }, [definitions]);

  // Get definitions for the active section
  const sectionDefinitions = useMemo(() => {
    const section = SECTIONS.find((s) => s.key === activeSection);
    if (!section || !notificationDefinitions.length) return [];
    return notificationDefinitions.filter((d) =>
      section.prefixes.some((p) => d.key.startsWith(p) || d.key === p)
    );
  }, [notificationDefinitions, activeSection]);

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
          title={t('notificationConfig.error.title', 'Error loading configuration')}
          message={t(
            'notificationConfig.error.message',
            'Could not load notification configuration definitions.'
          )}
          action={
            <button
              onClick={() => refetchDefinitions()}
              className="inline-flex items-center gap-2 px-4 py-2 text-sm font-medium text-white bg-accent rounded-lg hover:bg-info/15"
            >
              <RefreshCw className="w-4 h-4" />
              {t('notificationConfig.error.retry', 'Retry')}
            </button>
          }
        />
      </PageContainer>
    );
  }

  return (
    <PageContainer>
      <PageHeader
        title={t('notificationConfig.title', 'Notification Configuration')}
        subtitle={t(
          'notificationConfig.subtitle',
          'Manage notification behavior, channels, templates, routing and intelligence policies'
        )}
      />

      {/* ── Section Tabs ─────────────────────────────────────────── */}
      <div className="flex flex-wrap gap-2 mb-6">
        {SECTIONS.map((section) => (
          <button
            key={section.key}
            onClick={() => setActiveSection(section.key)}
            className={`inline-flex items-center gap-2 px-4 py-2 text-sm font-medium rounded-lg border transition-colors ${
              activeSection === section.key
                ? 'bg-info/15 border-info/25 text-info'
                : 'bg-card border-edge text-faded hover:bg-subtle'
            }`}
          >
            {section.icon}
            {t(`notificationConfig.sections.${section.key}`, section.key)}
          </button>
        ))}
      </div>

      {/* ── Scope & Search Controls ──────────────────────────────── */}
      <Card className="mb-6">
        <CardBody>
          <div className="flex flex-wrap gap-4 items-end">
            <div className="flex-1 min-w-[200px]">
              <label className="block text-sm font-medium text-body mb-1">
                {t('notificationConfig.scope', 'Scope')}
              </label>
              <select
                value={scope}
                onChange={(e) => setScope(e.target.value as ConfigurationScope)}
                className="w-full rounded-lg border border-edge bg-card px-3 py-2 text-sm"
              >
                {SCOPES.map((s) => (
                  <option key={s} value={s}>
                    {t(`configuration.scope.${s.toLowerCase()}`, s)}
                  </option>
                ))}
              </select>
            </div>
            {scope !== 'System' && (
              <div className="flex-1 min-w-[200px]">
                <label className="block text-sm font-medium text-body mb-1">
                  {t('notificationConfig.scopeReference', 'Scope Reference ID')}
                </label>
                <input
                  type="text"
                  value={scopeReferenceId}
                  onChange={(e) => setScopeReferenceId(e.target.value)}
                  placeholder={t('notificationConfig.scopeReferencePlaceholder', 'Enter tenant/environment ID...')}
                  className="w-full rounded-lg border border-edge bg-card px-3 py-2 text-sm"
                />
              </div>
            )}
            <div className="flex-1 min-w-[200px]">
              <label className="block text-sm font-medium text-body mb-1">
                <Search className="w-3 h-3 inline mr-1" />
                {t('notificationConfig.search', 'Search')}
              </label>
              <input
                type="text"
                value={search}
                onChange={(e) => setSearch(e.target.value)}
                placeholder={t('notificationConfig.searchPlaceholder', 'Search by key or name...')}
                className="w-full rounded-lg border border-edge bg-card px-3 py-2 text-sm"
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
                {t('notificationConfig.empty.title', 'No definitions found')}
              </p>
              <p className="text-sm mt-1">
                {t(
                  'notificationConfig.empty.description',
                  'No notification configuration definitions match the current filters.'
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
                            {t('notificationConfig.badges.sensitive', 'Sensitive')}
                          </Badge>
                        )}
                        {!def.isInheritable && (
                          <Badge variant="default">
                            {t('notificationConfig.badges.nonInheritable', 'Non-inheritable')}
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
                              {t('notificationConfig.effectiveValue', 'Effective Value')}
                            </span>
                            {eff.isDefault && (
                              <Badge variant="default">
                                {t('notificationConfig.badges.default', 'Default')}
                              </Badge>
                            )}
                            {eff.isInherited && (
                              <Badge variant="info">
                                <Layers className="w-3 h-3 mr-1" />
                                {t('notificationConfig.badges.inherited', 'Inherited')}
                              </Badge>
                            )}
                            {!eff.isDefault && !eff.isInherited && (
                              <Badge variant="success">
                                {t('notificationConfig.badges.override', 'Override')}
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
                            {t('notificationConfig.resolvedFrom', 'Resolved from')}: {eff.resolvedScope}
                            {eff.resolvedScopeReferenceId ? ` (${eff.resolvedScopeReferenceId})` : ''}
                          </div>
                        </div>
                      )}

                      {/* Editing Form */}
                      {isEditing && (
                        <div className="mt-3 p-3 rounded-lg bg-info/15 border border-info/25">
                          <label className="block text-xs font-medium text-body mb-1">
                            {t('notificationConfig.edit.value', 'Value')}
                          </label>
                          {def.valueType === 'Boolean' ? (
                            <button
                              onClick={() => setEditValue(editValue === 'true' ? 'false' : 'true')}
                              className={`px-4 py-2 rounded-lg text-sm font-medium ${
                                editValue === 'true'
                                  ? 'bg-success/15 text-success'
                                  : 'bg-critical/15 text-critical'
                              }`}
                            >
                              {editValue === 'true' ? 'Enabled' : 'Disabled'}
                            </button>
                          ) : def.valueType === 'Json' ? (
                            <textarea
                              value={editValue}
                              onChange={(e) => setEditValue(e.target.value)}
                              rows={6}
                              className="w-full rounded-lg border border-edge bg-card px-3 py-2 text-sm font-mono"
                            />
                          ) : (
                            <input
                              type={def.valueType === 'Integer' || def.valueType === 'Decimal' ? 'number' : 'text'}
                              value={editValue}
                              onChange={(e) => setEditValue(e.target.value)}
                              className="w-full rounded-lg border border-edge bg-card px-3 py-2 text-sm"
                            />
                          )}
                          <label className="block text-xs font-medium text-body mt-2 mb-1">
                            {t('notificationConfig.edit.reason', 'Change Reason')}
                          </label>
                          <input
                            type="text"
                            value={editReason}
                            onChange={(e) => setEditReason(e.target.value)}
                            placeholder={t('notificationConfig.edit.reasonPlaceholder', 'Optional reason for this change...')}
                            className="w-full rounded-lg border border-edge bg-card px-3 py-2 text-sm"
                          />
                          <div className="flex gap-2 mt-3">
                            <button
                              onClick={handleSave}
                              disabled={setValueMutation.isPending}
                              className="inline-flex items-center gap-1 px-3 py-1.5 text-sm font-medium text-white bg-accent rounded-lg hover:bg-info/15 disabled:opacity-50"
                            >
                              <Check className="w-3 h-3" />
                              {t('notificationConfig.edit.save', 'Save')}
                            </button>
                            <button
                              onClick={handleCancelEdit}
                              className="inline-flex items-center gap-1 px-3 py-1.5 text-sm font-medium text-faded bg-subtle rounded-lg hover:bg-subtle"
                            >
                              <X className="w-3 h-3" />
                              {t('notificationConfig.edit.cancel', 'Cancel')}
                            </button>
                          </div>
                        </div>
                      )}
                    </div>

                    {/* Actions */}
                    <div className="flex items-center gap-1 shrink-0">
                      {def.isEditable && !isEditing && (
                        <button
                          onClick={() => handleEdit(def)}
                          className="p-2 rounded-lg text-muted hover:text-info hover:bg-info/15 transition-colors"
                          title={t('notificationConfig.actions.edit', 'Edit')}
                        >
                          <Edit3 className="w-4 h-4" />
                        </button>
                      )}
                      <button
                        onClick={() =>
                          setExpandedAudit(isAuditExpanded ? null : def.key)
                        }
                        className="p-2 rounded-lg text-muted hover:text-warning hover:bg-warning/15 transition-colors"
                        title={t('notificationConfig.actions.audit', 'Audit History')}
                      >
                        {isAuditExpanded ? (
                          <ChevronUp className="w-4 h-4" />
                        ) : (
                          <ChevronDown className="w-4 h-4" />
                        )}
                      </button>
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
        {t('notificationConfig.footer', '{{count}} notification definitions configured', {
          count: notificationDefinitions.length,
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
        <p className="text-xs text-muted">{t('notificationConfig.audit.loading', 'Loading audit history...')}</p>
      </div>
    );
  }

  if (!audits || audits.length === 0) {
    return (
      <div className="mt-3 pt-3 border-t border-edge">
        <p className="text-xs text-muted">{t('notificationConfig.audit.empty', 'No audit history available.')}</p>
      </div>
    );
  }

  return (
    <div className="mt-3 pt-3 border-t border-edge">
      <h4 className="text-xs font-semibold text-faded mb-2">
        {t('notificationConfig.audit.title', 'Audit History')}
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
                {t('notificationConfig.audit.by', 'By')}: {audit.changedBy}
              </p>
            )}
            {audit.changeReason && (
              <p className="text-faded italic">
                {t('notificationConfig.audit.reason', 'Reason')}: {audit.changeReason}
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
    return formatted.length > 200 ? formatted.substring(0, 200) + '...' : formatted;
  } catch {
    return value.length > 200 ? value.substring(0, 200) + '...' : value;
  }
}
