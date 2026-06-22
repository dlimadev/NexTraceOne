import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useMutation, useQuery } from '@tanstack/react-query';
import { ArrowLeftRight, Code2, Copy, ChevronDown, ChevronUp, AlertTriangle, Info, CheckCircle2 } from 'lucide-react';
import { PageHeader } from '../../../components/PageHeader';
import { PageContainer } from '../../../components/shell';
import { Button, IconButton, Select } from '../../../shared/ui';
import { contractsApi } from '../api/contracts';
import { cn } from '../../../lib/cn';
import type { MigrationPatchResult, MigrationSuggestion } from '../../../types';

/**
 * ContractMigrationPage — página de geração de patch de migração de contrato.
 *
 * Permite ao utilizador selecionar duas versões de contrato, o lado alvo (provider/consumer/all)
 * e a linguagem de implementação, e receber sugestões de código contextualizadas para migrar
 * tanto a implementação do provedor quanto os clientes consumidores.
 *
 * Pilar: Contract Governance + AI-assisted Engineering
 * Persona: Engineer, Tech Lead, Architect
 */
export function ContractMigrationPage() {
  const { t } = useTranslation();

  const [baseVersionId, setBaseVersionId] = useState('');
  const [targetVersionId, setTargetVersionId] = useState('');
  const [targetSide, setTargetSide] = useState<'all' | 'provider' | 'consumer'>('all');
  const [language, setLanguage] = useState('C#');
  const [expandedHints, setExpandedHints] = useState<Set<string>>(new Set());

  const contractsQuery = useQuery({
    queryKey: ['contracts-list-migration'],
    queryFn: () => contractsApi.listContracts({ pageSize: 200 }),
  });

  const contracts = contractsQuery.data?.items ?? [];

  const migrationMutation = useMutation({
    mutationFn: () =>
      contractsApi.generateMigrationPatch({
        baseVersionId,
        targetVersionId,
        target: targetSide,
        language,
      }),
  });

  const patch = migrationMutation.data as MigrationPatchResult | undefined;

  const toggleHint = (id: string) => {
    setExpandedHints((prev) => {
      const next = new Set(prev);
      if (next.has(id)) next.delete(id);
      else next.add(id);
      return next;
    });
  };

  const copyHint = (hint: string) => {
    navigator.clipboard.writeText(hint).catch(() => {});
  };

  const severityIcon = (s: MigrationSuggestion['severity']) => {
    if (s === 'high') return <AlertTriangle className="h-4 w-4 text-critical" />;
    if (s === 'medium') return <Info className="h-4 w-4 text-warning" />;
    return <CheckCircle2 className="h-4 w-4 text-success" />;
  };

  const severityBadge = (s: MigrationSuggestion['severity']) => {
    const base = 'inline-flex items-center gap-1 rounded px-2 py-0.5 text-xs font-medium';
    if (s === 'high') return cn(base, 'bg-critical-muted text-critical');
    if (s === 'medium') return cn(base, 'bg-warning-muted text-warning');
    return cn(base, 'bg-success-muted text-success');
  };

  function SuggestionCard({ suggestion, idx, side }: { suggestion: MigrationSuggestion; idx: number; side: string }) {
    const hintKey = `${side}-${idx}`;
    const isExpanded = expandedHints.has(hintKey);
    return (
      <div className="rounded-lg border border-edge bg-card p-4">
        <div className="flex items-start gap-3">
          <div className="mt-0.5 shrink-0">{severityIcon(suggestion.severity)}</div>
          <div className="flex-1 min-w-0">
            <div className="flex flex-wrap items-center gap-2 mb-1">
              <span className={severityBadge(suggestion.severity)}>
                {t(`contracts.migrationSeverity${suggestion.severity.charAt(0).toUpperCase() + suggestion.severity.slice(1) as 'High' | 'Medium' | 'Low'}`)}
              </span>
              <span className="rounded-sm bg-elevated px-2 py-0.5 text-xs text-muted">
                {suggestion.kind}
              </span>
            </div>
            <p className="text-sm text-body">{suggestion.description}</p>
            {suggestion.codeHint && (
              <div className="mt-2">
                <Button
                  type="button"
                  variant="ghost"
                  size="xs"
                  className="text-accent hover:text-accent/80"
                  onClick={() => toggleHint(hintKey)}
                >
                  <Code2 className="h-3.5 w-3.5" />
                  {t('contracts.migrationCodeHint')}
                  {isExpanded ? <ChevronUp className="h-3.5 w-3.5" /> : <ChevronDown className="h-3.5 w-3.5" />}
                </Button>
                {isExpanded && (
                  <div className="mt-2 relative">
                    <pre className="overflow-x-auto rounded bg-canvas p-3 text-xs text-success whitespace-pre-wrap">
                      {suggestion.codeHint}
                    </pre>
                    <IconButton
                      icon={<Copy className="h-3.5 w-3.5" />}
                      label={t('contracts.migrationCopyHint')}
                      title={t('contracts.migrationCopyHint')}
                      variant="ghost"
                      size="sm"
                      className="absolute top-2 right-2"
                      onClick={() => copyHint(suggestion.codeHint!)}
                    />
                  </div>
                )}
              </div>
            )}
          </div>
        </div>
      </div>
    );
  }

  const canGenerate = baseVersionId.length > 0 && targetVersionId.length > 0 && baseVersionId !== targetVersionId;

  return (
    <PageContainer>
      <PageHeader
        title={t('contracts.migrationTitle')}
        subtitle={t('contracts.migrationDescription')}
        icon={<ArrowLeftRight className="h-5 w-5" />}
      />

      {/* Configuration Panel */}
      <div className="grid grid-cols-1 gap-6 md:grid-cols-2">
        <div className="rounded-lg border border-edge bg-card p-5">
          <h3 className="mb-4 text-sm font-semibold text-body">
            {t('contracts.migrationBaseVersion')}
          </h3>
          <Select
            aria-label={t('contracts.migrationSelectBaseVersion')}
            value={baseVersionId}
            onChange={(e) => setBaseVersionId(e.target.value)}
            options={[
              { value: '', label: t('contracts.migrationSelectBaseVersion') },
              ...contracts.map((c) => ({
                value: c.id ?? '',
                label: `${c.apiAssetId} — v${c.version} [${c.protocol}]`,
              })),
            ]}
          />
        </div>

        <div className="rounded-lg border border-edge bg-card p-5">
          <h3 className="mb-4 text-sm font-semibold text-body">
            {t('contracts.migrationTargetVersion')}
          </h3>
          <Select
            aria-label={t('contracts.migrationSelectTargetVersion')}
            value={targetVersionId}
            onChange={(e) => setTargetVersionId(e.target.value)}
            options={[
              { value: '', label: t('contracts.migrationSelectTargetVersion') },
              ...contracts.map((c) => ({
                value: c.id ?? '',
                label: `${c.apiAssetId} — v${c.version} [${c.protocol}]`,
              })),
            ]}
          />
        </div>

        <div className="rounded-lg border border-edge bg-card p-5">
          <h3 className="mb-4 text-sm font-semibold text-body">
            {t('contracts.migrationTargetSide')}
          </h3>
          <div className="flex flex-wrap gap-2">
            {(['all', 'provider', 'consumer'] as const).map((side) => (
              <Button
                key={side}
                type="button"
                size="sm"
                variant={targetSide === side ? 'primary' : 'secondary'}
                onClick={() => setTargetSide(side)}
              >
                {t(`contracts.migrationSide${side.charAt(0).toUpperCase() + side.slice(1) as 'All' | 'Provider' | 'Consumer'}`)}
              </Button>
            ))}
          </div>
        </div>

        <div className="rounded-lg border border-edge bg-card p-5">
          <h3 className="mb-4 text-sm font-semibold text-body">
            {t('contracts.migrationLanguage')}
          </h3>
          <div className="flex flex-wrap gap-2">
            {['C#', 'TypeScript', 'JavaScript', 'Java', 'Python'].map((lang) => (
              <Button
                key={lang}
                type="button"
                size="sm"
                variant={language === lang ? 'primary' : 'secondary'}
                onClick={() => setLanguage(lang)}
              >
                {lang}
              </Button>
            ))}
          </div>
        </div>
      </div>

      {/* Generate Button */}
      <div className="mt-6 flex justify-end">
        <Button
          type="button"
          variant="primary"
          size="lg"
          disabled={!canGenerate}
          loading={migrationMutation.isPending}
          icon={<ArrowLeftRight className="h-4 w-4" />}
          onClick={() => migrationMutation.mutate()}
        >
          {migrationMutation.isPending ? t('contracts.migrationLoading') : t('contracts.migrationGenerate')}
        </Button>
      </div>

      {/* Error */}
      {migrationMutation.isError && (
        <div className="mt-4 rounded-lg bg-critical-muted border border-critical/25 p-4 text-sm text-critical">
          {(migrationMutation.error as Error)?.message ?? 'An error occurred.'}
        </div>
      )}

      {/* Results */}
      {patch && (
        <div className="mt-8 space-y-6">
          {/* Summary */}
          <div className="grid grid-cols-2 gap-4 sm:grid-cols-4">
            {[
              { label: t('contracts.migrationProtocol'), value: patch.protocol },
              { label: t('contracts.migrationLanguage'), value: patch.language },
              { label: t('contracts.migrationChangeLevel'), value: patch.changeLevel },
              { label: t('contracts.migrationBreakingCount'), value: String(patch.breakingChangeCount) },
            ].map(({ label, value }) => (
              <div key={label} className="rounded-lg border border-edge bg-card p-4">
                <p className="text-xs text-muted">{label}</p>
                <p className="mt-1 text-lg font-semibold text-heading">{value}</p>
              </div>
            ))}
          </div>

          {/* Provider Suggestions */}
          {patch.providerSuggestions.length > 0 && (
            <section>
              <h2 className="mb-3 text-base font-semibold text-heading">
                {t('contracts.migrationProviderSuggestions')}
                <span className="ml-2 text-sm font-normal text-muted">({patch.providerSuggestions.length})</span>
              </h2>
              <div className="space-y-3">
                {patch.providerSuggestions.map((s, i) => (
                  <SuggestionCard key={`provider-${i}`} suggestion={s} idx={i} side="provider" />
                ))}
              </div>
            </section>
          )}

          {/* Consumer Suggestions */}
          {patch.consumerSuggestions.length > 0 && (
            <section>
              <h2 className="mb-3 text-base font-semibold text-heading">
                {t('contracts.migrationConsumerSuggestions')}
                <span className="ml-2 text-sm font-normal text-muted">({patch.consumerSuggestions.length})</span>
              </h2>
              <div className="space-y-3">
                {patch.consumerSuggestions.map((s, i) => (
                  <SuggestionCard key={`consumer-${i}`} suggestion={s} idx={i} side="consumer" />
                ))}
              </div>
            </section>
          )}

          {/* Empty state */}
          {patch.providerSuggestions.length === 0 && patch.consumerSuggestions.length === 0 && (
            <div className="rounded-lg border border-edge bg-card p-8 text-center">
              <CheckCircle2 className="mx-auto mb-3 h-8 w-8 text-success" />
              <p className="text-sm text-muted">{t('contracts.migrationEmpty')}</p>
            </div>
          )}

          <p className="text-xs text-faded">
            {t('contracts.migrationGeneratedAt')}: {new Date(patch.generatedAt).toLocaleString()}
          </p>
        </div>
      )}
    </PageContainer>
  );
}
