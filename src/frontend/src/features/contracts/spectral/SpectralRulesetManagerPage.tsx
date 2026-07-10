import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import {
  Plus,
  ToggleLeft,
  ToggleRight,
  Trash2,
  ChevronDown,
  ChevronRight,
  Star,
  FileCode,
  ScanSearch,
} from 'lucide-react';
import { Card, CardBody } from '../../../components/Card';
import { EmptyState } from '../../../components/EmptyState';
import { PageHeader } from '../../../components/PageHeader';
import { PageContainer, StatsGrid } from '../../../components/shell';
import { Button, IconButton, Modal } from '../../../shared/ui';
import { LoadingState, ErrorState } from '../shared/components/StateIndicators';
import { useSpectralRulesets, useToggleSpectralRuleset, useDeleteSpectralRuleset, useCreateSpectralRuleset } from '../hooks';
import { CreateRulesetModal } from './CreateRulesetModal';
import type { SpectralRuleset } from '../types';

/**
 * Página de gestão de rulesets Spectral.
 * Permite listar, criar, ativar/desativar e eliminar rulesets.
 */
export function SpectralRulesetManagerPage() {
  const { t } = useTranslation();
  const [expandedId, setExpandedId] = useState<string | null>(null);
  const [isCreateOpen, setIsCreateOpen] = useState(false);
  const [createError, setCreateError] = useState<string | null>(null);
  const [deleteTarget, setDeleteTarget] = useState<SpectralRuleset | null>(null);

  const rulesetsQuery = useSpectralRulesets();
  const toggleMutation = useToggleSpectralRuleset();
  const deleteMutation = useDeleteSpectralRuleset();
  const createMutation = useCreateSpectralRuleset();

  const rulesets = rulesetsQuery.data?.items ?? [];

  if (rulesetsQuery.isLoading) return <PageContainer><LoadingState /></PageContainer>;
  if (rulesetsQuery.isError) return <PageContainer><ErrorState onRetry={() => rulesetsQuery.refetch()} /></PageContainer>;

  const handleToggle = (ruleset: SpectralRuleset) => {
    toggleMutation.mutate({ rulesetId: ruleset.id, isActive: !ruleset.isActive });
  };

  return (
    <PageContainer>
      <PageHeader
        title={t('contracts.spectral.manager.title', 'Spectral Rulesets')}
        subtitle={t('contracts.spectral.manager.subtitle', 'Manage linting rulesets for contract validation and governance.')}
        actions={
          <Button
            variant="primary"
            size="sm"
            icon={<Plus size={14} />}
            onClick={() => setIsCreateOpen(true)}
          >
            {t('contracts.spectral.manager.create', 'Add Ruleset')}
          </Button>
        }
      />

      {/* Stats */}
      <StatsGrid columns={3}>
        <StatCard label={t('contracts.spectral.manager.total', 'Total')} value={rulesets.length} />
        <StatCard label={t('contracts.spectral.manager.active', 'Active')} value={rulesets.filter((r) => r.isActive).length} variant="success" />
        <StatCard label={t('contracts.spectral.manager.inactive', 'Inactive')} value={rulesets.filter((r) => !r.isActive).length} variant="warning" />
      </StatsGrid>
      {rulesets.length === 0 && !rulesetsQuery.isLoading && (
        <EmptyState
          icon={<ScanSearch size={24} aria-hidden="true" />}
          title={t('contracts.spectral.manager.emptyTitle', 'No rulesets found')}
          description={t('contracts.spectral.manager.emptyDescription', 'Create or import Spectral rulesets to start validating contracts.')}
        />
      )}

      <div className="space-y-3">
        {rulesets.map((ruleset) => {
          const isExpanded = expandedId === ruleset.id;

          return (
            <Card key={ruleset.id}>
              <CardBody>
                <div className="flex items-center gap-3">
                  {/* Expand toggle */}
                  <IconButton
                    variant="ghost"
                    size="sm"
                    icon={isExpanded ? <ChevronDown size={14} /> : <ChevronRight size={14} />}
                    label={isExpanded ? t('common.collapse', 'Collapse') : t('common.expand', 'Expand')}
                    onClick={() => setExpandedId(isExpanded ? null : ruleset.id)}
                  />

                  {/* Icon */}
                  <FileCode size={16} className="text-accent" />

                  {/* Name & description */}
                  <div className="flex-1 min-w-0">
                    <div className="flex items-center gap-2">
                      <p className="text-sm font-medium text-heading truncate">{ruleset.name}</p>
                      {ruleset.isDefault && (
                        <Star size={12} className="text-warning fill-warning" />
                      )}
                      <span className={`px-2 py-0.5 text-[10px] rounded-full ${
                        ruleset.rulesetType === 'Default'
                          ? 'bg-accent/10 text-accent border border-accent/20'
                          : 'bg-elevated/50 text-muted border border-edge/20'
                      }`}>
                        {ruleset.rulesetType}
                      </span>
                    </div>
                    <p className="text-xs text-muted truncate">{ruleset.description}</p>
                  </div>

                  {/* Active toggle */}
                  <IconButton
                    variant="ghost"
                    size="sm"
                    icon={ruleset.isActive ? <ToggleRight size={20} className="text-mint" /> : <ToggleLeft size={20} className="text-muted" />}
                    label={ruleset.isActive ? t('contracts.spectral.manager.deactivate', 'Deactivate') : t('contracts.spectral.manager.activate', 'Activate')}
                    title={ruleset.isActive ? t('contracts.spectral.manager.deactivate', 'Deactivate') : t('contracts.spectral.manager.activate', 'Activate')}
                    onClick={() => handleToggle(ruleset)}
                    disabled={toggleMutation.isPending}
                  />

                  {/* Delete */}
                  <IconButton
                    variant="ghost"
                    size="sm"
                    icon={<Trash2 size={14} />}
                    label={t('common.delete', 'Delete')}
                    title={t('common.delete', 'Delete')}
                    onClick={() => setDeleteTarget(ruleset)}
                    disabled={deleteMutation.isPending}
                    className="hover:text-danger"
                  />
                </div>

                {/* Expanded detail */}
                {isExpanded && (
                  <div className="mt-4 pt-4 border-t border-edge/10 space-y-4 text-xs">
                    <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
                      <div>
                        <p className="text-muted mb-1">{t('contracts.spectral.manager.rulesetType', 'Type')}</p>
                        <p className="text-heading">{ruleset.rulesetType}</p>
                      </div>
                      <div>
                        <p className="text-muted mb-1">{t('contracts.spectral.manager.status', 'Status')}</p>
                        <p className={ruleset.isActive ? 'text-mint' : 'text-warning'}>
                          {ruleset.isActive
                            ? t('contracts.spectral.manager.active', 'Active')
                            : t('contracts.spectral.manager.inactive', 'Inactive')}
                        </p>
                      </div>
                      <div>
                        <p className="text-muted mb-1">{t('contracts.spectral.manager.createdAt', 'Created')}</p>
                        <p className="text-heading">{new Date(ruleset.createdAt).toLocaleDateString()}</p>
                      </div>
                    </div>
                    {ruleset.content && (
                      <div>
                        <p className="text-muted mb-1">{t('contracts.spectral.manager.contentPreview', 'Ruleset Content')}</p>
                        <pre className="p-3 rounded-lg bg-panel/50 border border-edge/10 text-[11px] text-heading font-mono overflow-x-auto max-h-[200px]">
                          {ruleset.content.slice(0, 800)}
                          {ruleset.content.length > 800 && '...'}
                        </pre>
                      </div>
                    )}
                  </div>
                )}
              </CardBody>
            </Card>
          );
        })}
      </div>

      {/* Delete confirmation */}
      <Modal
        open={!!deleteTarget}
        onClose={() => setDeleteTarget(null)}
        title={t('contracts.spectral.manager.deleteTitle', 'Delete ruleset')}
        size="sm"
        footer={
          <>
            <Button variant="outline" size="sm" onClick={() => setDeleteTarget(null)}>
              {t('common.cancel', 'Cancel')}
            </Button>
            <Button
              variant="danger"
              size="sm"
              loading={deleteMutation.isPending}
              disabled={deleteMutation.isPending}
              onClick={() => {
                if (deleteTarget) {
                  deleteMutation.mutate(deleteTarget.id, { onSuccess: () => setDeleteTarget(null) });
                }
              }}
            >
              {t('common.delete', 'Delete')}
            </Button>
          </>
        }
      >
        <p className="text-xs text-muted">
          {t('contracts.spectral.manager.deleteConfirm', 'Delete ruleset "{{name}}"? This cannot be undone.', { name: deleteTarget?.name ?? '' })}
        </p>
      </Modal>

      {/* Create Modal */}
      <CreateRulesetModal
        isOpen={isCreateOpen}
        onClose={() => { setIsCreateOpen(false); setCreateError(null); }}
        isSubmitting={createMutation.isPending}
        error={createError}
        onSubmit={(data) => {
          setCreateError(null);
          createMutation.mutate(data, {
            onSuccess: () => { setIsCreateOpen(false); setCreateError(null); },
            onError: () => setCreateError(t('contracts.spectral.form.createError', 'Failed to create ruleset. Please try again.')),
          });
        }}
      />
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
