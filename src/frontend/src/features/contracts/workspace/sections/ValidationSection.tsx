import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import {
  ScanSearch,
  RefreshCw,
  CheckCircle2,
  XCircle,
  AlertTriangle,
  Info,
  Ban,
  ChevronDown,
  ChevronRight,
  Sparkles,
} from 'lucide-react';
import { Card, CardBody, CardHeader } from '../../../../components/Card';
import { EmptyState } from '../../../../components/EmptyState';
import { useValidationSummary, useExecuteValidation } from '../../hooks';
import { SEVERITY_COLORS } from '../../shared/constants';
import type { ValidationSeverity } from '../../types';

interface ValidationSectionProps {
  contractVersionId: string;
  className?: string;
}

const SEVERITY_ICON: Record<ValidationSeverity, React.ComponentType<{ size?: number; className?: string }>> = {
  Error: XCircle,
  Warning: AlertTriangle,
  Info: Info,
  Hint: Sparkles,
  Blocked: Ban,
};

const SEVERITY_ORDER: ValidationSeverity[] = ['Blocked', 'Error', 'Warning', 'Info', 'Hint'];

/**
 * Secção de validação — Spectral linting, checks internos, canonical adherence e publish readiness.
 * Integra-se no workspace do contrato como secção de governança.
 */
export function ValidationSection({ contractVersionId, className = '' }: ValidationSectionProps) {
  const { t } = useTranslation();
  const [expandedSeverity, setExpandedSeverity] = useState<string | null>(null);

  const summaryQuery = useValidationSummary(contractVersionId);
  const executeMutation = useExecuteValidation(contractVersionId);

  const summary = summaryQuery.data;

  const handleRunValidation = () => {
    executeMutation.mutate();
  };

  const issues = executeMutation.data?.issues ?? [];
  const hasResults = !!summary || issues.length > 0;

  const severityCounts: Record<ValidationSeverity, number> = {
    Blocked: summary?.blockedCount ?? 0,
    Error: summary?.errorCount ?? 0,
    Warning: summary?.warningCount ?? 0,
    Info: summary?.infoCount ?? 0,
    Hint: summary?.hintCount ?? 0,
  };

  const toggleSeverity = (severity: string) => {
    setExpandedSeverity(expandedSeverity === severity ? null : severity);
  };

  return (
    <div className={`space-y-6 ${className}`}>
      {/* Header with run button */}
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-2">
          <ScanSearch size={16} className="text-accent" />
          <h3 className="text-sm font-semibold text-heading">
            {t('contracts.validation.title', 'Validation & Linting')}
          </h3>
        </div>
        <button
          onClick={handleRunValidation}
          disabled={executeMutation.isPending}
          className="flex items-center gap-1.5 px-3 py-1.5 text-xs font-medium rounded-md bg-accent/15 text-accent border border-accent/25 hover:bg-accent/25 transition-colors disabled:opacity-50"
        >
          <RefreshCw size={12} className={executeMutation.isPending ? 'animate-spin' : ''} />
          {executeMutation.isPending
            ? t('contracts.validation.running', 'Running...')
            : t('contracts.validation.runValidation', 'Run Validation')}
        </button>
      </div>

      {/* Publish readiness */}
      {summary && (
        <Card>
          <CardBody>
            <div className="flex items-center justify-between">
              <div className="flex items-center gap-3">
                {summary.isPublishReady ? (
                  <CheckCircle2 size={20} className="text-mint" />
                ) : (
                  <XCircle size={20} className="text-danger" />
                )}
                <div>
                  <p className="text-sm font-medium text-heading">
                    {summary.isPublishReady
                      ? t('contracts.validation.publishReady', 'Ready to publish')
                      : t('contracts.validation.notPublishReady', 'Not ready to publish')}
                  </p>
                  <p className="text-xs text-muted">
                    {t('contracts.validation.totalIssues', '{{count}} issue(s) found', { count: summary.totalIssues })}
                    {summary.sources.length > 0 && ` · ${summary.sources.join(', ')}`}
                  </p>
                </div>
              </div>
              <div className="flex items-center gap-2">
                <StatusBadge
                  label={t('contracts.validation.review', 'Review')}
                  ready={summary.isReviewReady}
                />
                <StatusBadge
                  label={t('contracts.validation.publish', 'Publish')}
                  ready={summary.isPublishReady}
                />
              </div>
            </div>
          </CardBody>
        </Card>
      )}

      {/* Severity counters */}
      {hasResults && (
        <div className="grid grid-cols-5 gap-3">
          {SEVERITY_ORDER.map((severity) => {
            const count = severityCounts[severity];
            const colors = SEVERITY_COLORS[severity] ?? 'bg-muted/15 text-muted border border-muted/25';
            const Icon = SEVERITY_ICON[severity];
            return (
              <button
                key={severity}
                onClick={() => toggleSeverity(severity)}
                className={`flex items-center gap-2 p-3 rounded-lg text-left transition-all ${colors} ${
                  expandedSeverity === severity ? 'ring-1 ring-accent/40' : ''
                }`}
              >
                <Icon size={14} />
                <div>
                  <p className="text-lg font-bold leading-none">{count}</p>
                  <p className="text-[10px] opacity-70 mt-0.5">{t(`contracts.validation.severity.${severity.toLowerCase()}`, severity)}</p>
                </div>
              </button>
            );
          })}
        </div>
      )}

      {/* Issues list by severity */}
      {issues.length > 0 && (
        <Card>
          <CardHeader>
            <h3 className="text-sm font-semibold text-heading">
              {t('contracts.validation.issues', 'Issues')}
              <span className="ml-2 text-xs font-normal text-muted">({issues.length})</span>
            </h3>
          </CardHeader>
          <CardBody>
            <div className="space-y-1 max-h-[400px] overflow-y-auto">
              {SEVERITY_ORDER.map((severity) => {
                const filtered = issues.filter((i) => i.severity === severity);
                if (filtered.length === 0) return null;
                const isExpanded = expandedSeverity === severity || expandedSeverity === null;
                const Icon = SEVERITY_ICON[severity];

                return (
                  <div key={severity}>
                    <button
                      onClick={() => toggleSeverity(severity)}
                      className="flex items-center gap-2 w-full px-2 py-1.5 text-xs font-medium text-heading hover:bg-elevated/50 rounded transition-colors"
                    >
                      {isExpanded ? <ChevronDown size={12} /> : <ChevronRight size={12} />}
                      <Icon size={12} />
                      <span>{severity}</span>
                      <span className="text-muted">({filtered.length})</span>
                    </button>
                    {isExpanded && (
                      <div className="ml-6 space-y-1 mb-2">
                        {filtered.map((issue, idx) => (
                          <div
                            key={`${issue.ruleId}-${idx}`}
                            className="flex items-start gap-2 px-2 py-1.5 text-xs rounded bg-elevated/30 border border-edge/10"
                          >
                            <div className="flex-1 min-w-0">
                              <p className="text-heading font-medium truncate">{issue.message}</p>
                              <div className="flex items-center gap-2 mt-0.5 text-muted">
                                <span>{issue.ruleName}</span>
                                {issue.path && <span>· {issue.path}</span>}
                                {issue.line != null && <span>· L{issue.line}</span>}
                                {issue.source && <span className="text-accent/70">· {issue.source}</span>}
                              </div>
                              {issue.suggestedFix && (
                                <p className="mt-1 text-mint/80 italic">{issue.suggestedFix}</p>
                              )}
                            </div>
                          </div>
                        ))}
                      </div>
                    )}
                  </div>
                );
              })}
            </div>
          </CardBody>
        </Card>
      )}

      {/* Empty state */}
      {!hasResults && !summaryQuery.isLoading && !executeMutation.isPending && (
        <EmptyState
          icon="ScanSearch"
          title={t('contracts.validation.emptyTitle', 'No validation results')}
          description={t('contracts.validation.emptyDescription', 'Run validation to check this contract against Spectral rules and internal governance checks.')}
        />
      )}
    </div>
  );
}

function StatusBadge({ label, ready }: { label: string; ready: boolean }) {
  return (
    <span
      className={`inline-flex items-center gap-1 px-2 py-0.5 text-[10px] font-medium rounded-full ${
        ready
          ? 'bg-mint/15 text-mint border border-mint/25'
          : 'bg-danger/15 text-danger border border-danger/25'
      }`}
    >
      {ready ? <CheckCircle2 size={10} /> : <XCircle size={10} />}
      {label}
    </span>
  );
}
