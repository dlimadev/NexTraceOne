import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import {
  ScanSearch,
  Plus,
  ToggleLeft,
  ToggleRight,
  Trash2,
  ChevronDown,
  ChevronRight,
  Globe,
  Building2,
  Users,
  Download,
  ExternalLink,
  Star,
} from 'lucide-react';
import { Card, CardBody, CardHeader } from '../../../components/Card';
import { EmptyState } from '../../../components/EmptyState';
import { PageHeader } from '../../../components/PageHeader';
import { PageContainer, StatsGrid } from '../../../components/shell';
import { useSpectralRulesets, useToggleSpectralRuleset, useDeleteSpectralRuleset } from '../hooks';
import { SEVERITY_COLORS } from '../shared/constants';
import type { SpectralRuleset, SpectralRulesetOrigin } from '../types';

const ORIGIN_CONFIG: Record<SpectralRulesetOrigin, { icon: React.ComponentType<{ size?: number; className?: string }>; color: string }> = {
  Platform: { icon: Globe, color: 'text-mint' },
  Organization: { icon: Building2, color: 'text-accent' },
  Team: { icon: Users, color: 'text-cyan' },
  Imported: { icon: Download, color: 'text-warning' },
  ExternalRepository: { icon: ExternalLink, color: 'text-muted' },
};

/**
 * Página de gestão de rulesets Spectral.
 * Permite listar, criar, ativar/desativar e eliminar rulesets.
 */
export function SpectralRulesetManagerPage() {
  const { t } = useTranslation();
  const [originFilter, setOriginFilter] = useState<SpectralRulesetOrigin | ''>('');
  const [expandedId, setExpandedId] = useState<string | null>(null);

  const rulesetsQuery = useSpectralRulesets(
    originFilter ? { origin: originFilter as SpectralRulesetOrigin } : undefined,
  );
  const toggleMutation = useToggleSpectralRuleset();
  const deleteMutation = useDeleteSpectralRuleset();

  const rulesets = rulesetsQuery.data?.items ?? [];

  const handleToggle = (ruleset: SpectralRuleset) => {
    toggleMutation.mutate({ rulesetId: ruleset.id, isActive: !ruleset.isActive });
  };

  const handleDelete = (rulesetId: string) => {
    deleteMutation.mutate(rulesetId);
  };

  return (
    <PageContainer>
      <PageHeader
        title={t('contracts.spectral.manager.title', 'Spectral Rulesets')}
        subtitle={t('contracts.spectral.manager.subtitle', 'Manage linting rulesets for contract validation and governance.')}
        actions={
          <button type="button" className="flex items-center gap-1.5 px-4 py-2 text-sm font-medium rounded-lg bg-accent/15 text-accent border border-accent/25 hover:bg-accent/25 transition-colors">
            <Plus size={14} />
            {t('contracts.spectral.manager.create', 'Add Ruleset')}
          </button>
        }
      />

      {/* Filters */}
      <div className="flex items-center gap-3">
        <label className="text-xs text-muted">{t('contracts.spectral.manager.filterOrigin', 'Origin:')}</label>
        {(['', 'Platform', 'Organization', 'Team', 'Imported', 'ExternalRepository'] as const).map((origin) => (
          <button type="button"
            key={origin || 'all'}
            onClick={() => setOriginFilter(origin)}
            className={`px-3 py-1 text-xs rounded-full border transition-colors ${
              originFilter === origin
                ? 'bg-accent/15 text-accent border-accent/25'
                : 'bg-elevated/50 text-muted border-edge/20 hover:border-accent/30'
            }`}
          >
            {origin || t('common.viewAll', 'All')}
          </button>
        ))}
      </div>

      {/* Stats */}
      <StatsGrid columns={4}>
        <StatCard label={t('contracts.spectral.manager.total', 'Total')} value={rulesets.length} />
        <StatCard label={t('contracts.spectral.manager.active', 'Active')} value={rulesets.filter((r) => r.isActive).length} variant="success" />
        <StatCard label={t('contracts.spectral.manager.inactive', 'Inactive')} value={rulesets.filter((r) => !r.isActive).length} variant="warning" />
        <StatCard label={t('contracts.spectral.manager.defaults', 'Default')} value={rulesets.filter((r) => r.isDefault).length} variant="accent" />
      </StatsGrid>
      {rulesets.length === 0 && !rulesetsQuery.isLoading && (
        <EmptyState
          icon="ScanSearch"
          title={t('contracts.spectral.manager.emptyTitle', 'No rulesets found')}
          description={t('contracts.spectral.manager.emptyDescription', 'Create or import Spectral rulesets to start validating contracts.')}
        />
      )}

      <div className="space-y-3">
        {rulesets.map((ruleset) => {
          const isExpanded = expandedId === ruleset.id;
          const OriginIcon = ORIGIN_CONFIG[ruleset.origin]?.icon ?? Globe;
          const originColor = ORIGIN_CONFIG[ruleset.origin]?.color ?? 'text-muted';

          return (
            <Card key={ruleset.id}>
              <CardBody>
                <div className="flex items-center gap-3">
                  {/* Expand toggle */}
                  <button type="button"
                    onClick={() => setExpandedId(isExpanded ? null : ruleset.id)}
                    className="text-muted hover:text-heading transition-colors"
                  >
                    {isExpanded ? <ChevronDown size={14} /> : <ChevronRight size={14} />}
                  </button>

                  {/* Origin icon */}
                  <OriginIcon size={16} className={originColor} />

                  {/* Name & version */}
                  <div className="flex-1 min-w-0">
                    <div className="flex items-center gap-2">
                      <p className="text-sm font-medium text-heading truncate">{ruleset.name}</p>
                      {ruleset.isDefault && (
                        <Star size={12} className="text-warning fill-warning" />
                      )}
                      <span className="text-[10px] text-muted">v{ruleset.version}</span>
                    </div>
                    <p className="text-xs text-muted truncate">{ruleset.description}</p>
                  </div>

                  {/* Metadata badges */}
                  <span className={`px-2 py-0.5 text-[10px] rounded-full ${SEVERITY_COLORS.Info}`}>
                    {ruleset.defaultExecutionMode}
                  </span>
                  <span className={`px-2 py-0.5 text-[10px] rounded-full ${
                    ruleset.enforcementBehavior.includes('Blocking')
                      ? SEVERITY_COLORS.Error
                      : SEVERITY_COLORS.Warning
                  }`}>
                    {ruleset.enforcementBehavior}
                  </span>

                  {/* Active toggle */}
                  <button type="button"
                    onClick={() => handleToggle(ruleset)}
                    disabled={toggleMutation.isPending}
                    className="text-muted hover:text-heading transition-colors"
                    title={ruleset.isActive
                      ? t('contracts.spectral.manager.deactivate', 'Deactivate')
                      : t('contracts.spectral.manager.activate', 'Activate')}
                  >
                    {ruleset.isActive ? (
                      <ToggleRight size={20} className="text-mint" />
                    ) : (
                      <ToggleLeft size={20} className="text-muted" />
                    )}
                  </button>

                  {/* Delete */}
                  <button type="button"
                    onClick={() => handleDelete(ruleset.id)}
                    disabled={deleteMutation.isPending}
                    className="text-muted hover:text-danger transition-colors"
                    title={t('common.delete', 'Delete')}
                  >
                    <Trash2 size={14} />
                  </button>
                </div>

                {/* Expanded detail */}
                {isExpanded && (
                  <div className="mt-4 pt-4 border-t border-edge/10 grid grid-cols-1 md:grid-cols-2 xl:grid-cols-3 gap-4 text-xs">
                    <div>
                      <p className="text-muted mb-1">{t('contracts.spectral.manager.origin', 'Origin')}</p>
                      <p className="text-heading">{ruleset.origin}</p>
                    </div>
                    {ruleset.owner && (
                      <div>
                        <p className="text-muted mb-1">{t('contracts.spectral.manager.owner', 'Owner')}</p>
                        <p className="text-heading">{ruleset.owner}</p>
                      </div>
                    )}
                    {ruleset.domain && (
                      <div>
                        <p className="text-muted mb-1">{t('contracts.spectral.manager.domain', 'Domain')}</p>
                        <p className="text-heading">{ruleset.domain}</p>
                      </div>
                    )}
                    {ruleset.applicableServiceType && (
                      <div>
                        <p className="text-muted mb-1">{t('contracts.spectral.manager.serviceType', 'Service Type')}</p>
                        <p className="text-heading">{ruleset.applicableServiceType}</p>
                      </div>
                    )}
                    {ruleset.applicableProtocols && (
                      <div>
                        <p className="text-muted mb-1">{t('contracts.spectral.manager.protocols', 'Protocols')}</p>
                        <p className="text-heading">{ruleset.applicableProtocols}</p>
                      </div>
                    )}
                    {ruleset.sourceUrl && (
                      <div>
                        <p className="text-muted mb-1">{t('contracts.spectral.manager.sourceUrl', 'Source URL')}</p>
                        <a href={ruleset.sourceUrl} target="_blank" rel="noopener noreferrer" className="text-accent hover:underline truncate block">
                          {ruleset.sourceUrl}
                        </a>
                      </div>
                    )}
                    <div>
                      <p className="text-muted mb-1">{t('contracts.spectral.manager.updatedAt', 'Updated')}</p>
                      <p className="text-heading">{new Date(ruleset.updatedAt).toLocaleDateString()}</p>
                    </div>
                    <div className="col-span-3">
                      <p className="text-muted mb-1">{t('contracts.spectral.manager.contentPreview', 'Ruleset Content')}</p>
                      <pre className="p-3 rounded-lg bg-panel/50 border border-edge/10 text-[11px] text-heading font-mono overflow-x-auto max-h-[200px]">
                        {ruleset.content.slice(0, 800)}
                        {ruleset.content.length > 800 && '...'}
                      </pre>
                    </div>
                  </div>
                )}
              </CardBody>
            </Card>
          );
        })}
      </div>
    </PageContainer>
  );
}

function StatCard({ label, value, variant = 'neutral' }: { label: string; value: number; variant?: string }) {
  const colors: Record<string, string> = {
    neutral: 'bg-elevated/50 border-edge/20',
    success: 'bg-mint/10 border-mint/20',
    warning: 'bg-warning/10 border-warning/20',
    accent: 'bg-accent/10 border-accent/20',
  };
  return (
    <div className={`rounded-lg border p-4 ${colors[variant] ?? colors.neutral}`}>
      <p className="text-xs text-muted">{label}</p>
      <p className="text-2xl font-bold text-heading mt-1">{value}</p>
    </div>
  );
}
