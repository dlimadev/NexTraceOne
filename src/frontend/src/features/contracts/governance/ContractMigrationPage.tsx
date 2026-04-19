import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useMutation, useQuery } from '@tanstack/react-query';
import { ArrowLeftRight, Code2, Copy, ChevronDown, ChevronUp, AlertTriangle, Info, CheckCircle2 } from 'lucide-react';
import { PageHeader } from '../../../components/PageHeader';
import { PageContainer } from '../../../components/shell';
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
    if (s === 'high') return <AlertTriangle className="h-4 w-4 text-red-500" />;
    if (s === 'medium') return <Info className="h-4 w-4 text-yellow-500" />;
    return <CheckCircle2 className="h-4 w-4 text-green-500" />;
  };

  const severityBadge = (s: MigrationSuggestion['severity']) => {
    const base = 'inline-flex items-center gap-1 rounded px-2 py-0.5 text-xs font-medium';
    if (s === 'high') return cn(base, 'bg-red-100 text-red-700 dark:bg-red-900/30 dark:text-red-300');
    if (s === 'medium') return cn(base, 'bg-yellow-100 text-yellow-700 dark:bg-yellow-900/30 dark:text-yellow-300');
    return cn(base, 'bg-green-100 text-green-700 dark:bg-green-900/30 dark:text-green-300');
  };

  function SuggestionCard({ suggestion, idx, side }: { suggestion: MigrationSuggestion; idx: number; side: string }) {
    const hintKey = `${side}-${idx}`;
    const isExpanded = expandedHints.has(hintKey);
    return (
      <div className="rounded-lg border border-gray-200 bg-white p-4 dark:border-gray-700 dark:bg-gray-800/60">
        <div className="flex items-start gap-3">
          <div className="mt-0.5 shrink-0">{severityIcon(suggestion.severity)}</div>
          <div className="flex-1 min-w-0">
            <div className="flex flex-wrap items-center gap-2 mb-1">
              <span className={severityBadge(suggestion.severity)}>
                {t(`contracts.migrationSeverity${suggestion.severity.charAt(0).toUpperCase() + suggestion.severity.slice(1) as 'High' | 'Medium' | 'Low'}`)}
              </span>
              <span className="rounded bg-gray-100 px-2 py-0.5 text-xs text-gray-600 dark:bg-gray-700 dark:text-gray-300">
                {suggestion.kind}
              </span>
            </div>
            <p className="text-sm text-gray-800 dark:text-gray-200">{suggestion.description}</p>
            {suggestion.codeHint && (
              <div className="mt-2">
                <button
                  type="button"
                  onClick={() => toggleHint(hintKey)}
                  className="flex items-center gap-1 text-xs text-blue-600 hover:text-blue-700 dark:text-blue-400"
                >
                  <Code2 className="h-3.5 w-3.5" />
                  {t('contracts.migrationCodeHint')}
                  {isExpanded ? <ChevronUp className="h-3.5 w-3.5" /> : <ChevronDown className="h-3.5 w-3.5" />}
                </button>
                {isExpanded && (
                  <div className="mt-2 relative">
                    <pre className="overflow-x-auto rounded bg-gray-950 p-3 text-xs text-green-300 whitespace-pre-wrap">
                      {suggestion.codeHint}
                    </pre>
                    <button
                      type="button"
                      title={t('contracts.migrationCopyHint')}
                      onClick={() => copyHint(suggestion.codeHint!)}
                      className="absolute top-2 right-2 rounded bg-gray-700 p-1 text-gray-300 hover:bg-gray-600 hover:text-white"
                    >
                      <Copy className="h-3.5 w-3.5" />
                    </button>
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
        <div className="rounded-lg border border-gray-200 bg-white p-5 dark:border-gray-700 dark:bg-gray-800/60">
          <h3 className="mb-4 text-sm font-semibold text-gray-700 dark:text-gray-200">
            {t('contracts.migrationBaseVersion')}
          </h3>
          <select
            aria-label={t('contracts.migrationSelectBaseVersion')}
            value={baseVersionId}
            onChange={(e) => setBaseVersionId(e.target.value)}
            className="w-full rounded-md border border-gray-300 bg-white px-3 py-2 text-sm text-gray-900 focus:border-blue-500 focus:outline-none dark:border-gray-600 dark:bg-gray-700 dark:text-gray-100"
          >
            <option value="">{t('contracts.migrationSelectBaseVersion')}</option>
            {contracts.map((c) => (
              <option key={c.id} value={c.id}>
                {c.apiAssetId} — v{c.version} [{c.protocol}]
              </option>
            ))}
          </select>
        </div>

        <div className="rounded-lg border border-gray-200 bg-white p-5 dark:border-gray-700 dark:bg-gray-800/60">
          <h3 className="mb-4 text-sm font-semibold text-gray-700 dark:text-gray-200">
            {t('contracts.migrationTargetVersion')}
          </h3>
          <select
            aria-label={t('contracts.migrationSelectTargetVersion')}
            value={targetVersionId}
            onChange={(e) => setTargetVersionId(e.target.value)}
            className="w-full rounded-md border border-gray-300 bg-white px-3 py-2 text-sm text-gray-900 focus:border-blue-500 focus:outline-none dark:border-gray-600 dark:bg-gray-700 dark:text-gray-100"
          >
            <option value="">{t('contracts.migrationSelectTargetVersion')}</option>
            {contracts.map((c) => (
              <option key={c.id} value={c.id}>
                {c.apiAssetId} — v{c.version} [{c.protocol}]
              </option>
            ))}
          </select>
        </div>

        <div className="rounded-lg border border-gray-200 bg-white p-5 dark:border-gray-700 dark:bg-gray-800/60">
          <h3 className="mb-4 text-sm font-semibold text-gray-700 dark:text-gray-200">
            {t('contracts.migrationTargetSide')}
          </h3>
          <div className="flex flex-wrap gap-2">
            {(['all', 'provider', 'consumer'] as const).map((side) => (
              <button
                key={side}
                type="button"
                onClick={() => setTargetSide(side)}
                className={cn(
                  'rounded-md px-4 py-2 text-sm font-medium transition-colors',
                  targetSide === side
                    ? 'bg-blue-600 text-white'
                    : 'border border-gray-200 bg-white text-gray-700 hover:bg-gray-50 dark:border-gray-600 dark:bg-gray-700 dark:text-gray-200 dark:hover:bg-gray-600',
                )}
              >
                {t(`contracts.migrationSide${side.charAt(0).toUpperCase() + side.slice(1) as 'All' | 'Provider' | 'Consumer'}`)}
              </button>
            ))}
          </div>
        </div>

        <div className="rounded-lg border border-gray-200 bg-white p-5 dark:border-gray-700 dark:bg-gray-800/60">
          <h3 className="mb-4 text-sm font-semibold text-gray-700 dark:text-gray-200">
            {t('contracts.migrationLanguage')}
          </h3>
          <div className="flex flex-wrap gap-2">
            {['C#', 'TypeScript', 'JavaScript', 'Java', 'Python'].map((lang) => (
              <button
                key={lang}
                type="button"
                onClick={() => setLanguage(lang)}
                className={cn(
                  'rounded-md px-4 py-2 text-sm font-medium transition-colors',
                  language === lang
                    ? 'bg-blue-600 text-white'
                    : 'border border-gray-200 bg-white text-gray-700 hover:bg-gray-50 dark:border-gray-600 dark:bg-gray-700 dark:text-gray-200 dark:hover:bg-gray-600',
                )}
              >
                {lang}
              </button>
            ))}
          </div>
        </div>
      </div>

      {/* Generate Button */}
      <div className="mt-6 flex justify-end">
        <button
          type="button"
          disabled={!canGenerate || migrationMutation.isPending}
          onClick={() => migrationMutation.mutate()}
          className={cn(
            'inline-flex items-center gap-2 rounded-lg px-6 py-2.5 text-sm font-semibold text-white transition-colors',
            canGenerate && !migrationMutation.isPending
              ? 'bg-blue-600 hover:bg-blue-700'
              : 'cursor-not-allowed bg-gray-300 dark:bg-gray-600',
          )}
        >
          <ArrowLeftRight className="h-4 w-4" />
          {migrationMutation.isPending ? t('contracts.migrationLoading') : t('contracts.migrationGenerate')}
        </button>
      </div>

      {/* Error */}
      {migrationMutation.isError && (
        <div className="mt-4 rounded-lg bg-red-50 border border-red-200 p-4 text-sm text-red-700 dark:bg-red-900/20 dark:border-red-800 dark:text-red-300">
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
              <div key={label} className="rounded-lg border border-gray-200 bg-white p-4 dark:border-gray-700 dark:bg-gray-800/60">
                <p className="text-xs text-gray-500 dark:text-gray-400">{label}</p>
                <p className="mt-1 text-lg font-semibold text-gray-900 dark:text-gray-100">{value}</p>
              </div>
            ))}
          </div>

          {/* Provider Suggestions */}
          {patch.providerSuggestions.length > 0 && (
            <section>
              <h2 className="mb-3 text-base font-semibold text-gray-800 dark:text-gray-100">
                {t('contracts.migrationProviderSuggestions')}
                <span className="ml-2 text-sm font-normal text-gray-500">({patch.providerSuggestions.length})</span>
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
              <h2 className="mb-3 text-base font-semibold text-gray-800 dark:text-gray-100">
                {t('contracts.migrationConsumerSuggestions')}
                <span className="ml-2 text-sm font-normal text-gray-500">({patch.consumerSuggestions.length})</span>
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
            <div className="rounded-lg border border-gray-200 bg-white p-8 text-center dark:border-gray-700 dark:bg-gray-800/60">
              <CheckCircle2 className="mx-auto mb-3 h-8 w-8 text-green-500" />
              <p className="text-sm text-gray-600 dark:text-gray-400">{t('contracts.migrationEmpty')}</p>
            </div>
          )}

          <p className="text-xs text-gray-400">
            {t('contracts.migrationGeneratedAt')}: {new Date(patch.generatedAt).toLocaleString()}
          </p>
        </div>
      )}
    </PageContainer>
  );
}
