import { useTranslation } from 'react-i18next';
import { useQuery } from '@tanstack/react-query';
import { CheckCircle, XCircle, AlertTriangle, ShieldCheck } from 'lucide-react';
import { Card, CardBody, CardHeader } from '../../../../components/Card';
import { EmptyState } from '../../../../components/EmptyState';
import { LoadingState, ErrorState } from '../../shared/components/StateIndicators';
import { contractsApi } from '../../api/contracts';
import type { ContractRuleViolation } from '../../types';

const SEVERITY_CONFIG: Record<string, { color: string; bg: string; Icon: React.ComponentType<{ size?: number; className?: string }> }> = {
  Critical: { color: 'text-critical', bg: 'bg-critical/15', Icon: XCircle },
  Error: { color: 'text-critical', bg: 'bg-critical/15', Icon: XCircle },
  Warning: { color: 'text-warning', bg: 'bg-warning/15', Icon: AlertTriangle },
  Info: { color: 'text-info', bg: 'bg-info/15', Icon: CheckCircle },
};

interface ComplianceSectionProps {
  contractVersionId: string;
  className?: string;
}

export function ComplianceSection({ contractVersionId, className = '' }: ComplianceSectionProps) {
  const { t } = useTranslation();

  const violationsQuery = useQuery({
    queryKey: ['contract-violations', contractVersionId],
    queryFn: () => contractsApi.listRuleViolations(contractVersionId),
    enabled: !!contractVersionId,
  });

  const integrityQuery = useQuery({
    queryKey: ['contract-integrity', contractVersionId],
    queryFn: () => contractsApi.validateIntegrity(contractVersionId),
    enabled: !!contractVersionId,
  });

  const violations: ContractRuleViolation[] = violationsQuery.data ?? [];
  const integrity = integrityQuery.data;
  const integrityErrors = integrity?.errors ?? [];
  const integrityWarnings = integrity?.warnings ?? [];

  const criticalCount = violations.filter((v) => v.severity === 'Critical' || v.severity === 'Error').length;
  const warningCount = violations.filter((v) => v.severity === 'Warning').length;

  return (
    <div className={`space-y-6 ${className}`}>
      {/* Compliance summary */}
      <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
        <SummaryCard
          label={t('contracts.violations', 'Violations')}
          value={violations.length}
          variant={violations.length === 0 ? 'success' : 'danger'}
        />
        <SummaryCard
          label={t('contracts.compliance.critical', 'Critical')}
          value={criticalCount}
          variant={criticalCount > 0 ? 'danger' : 'success'}
        />
        <SummaryCard
          label={t('contracts.compliance.warnings', 'Warnings')}
          value={warningCount + integrityWarnings.length}
          variant={warningCount + integrityWarnings.length > 0 ? 'warning' : 'success'}
        />
        <SummaryCard
          label={t('contracts.validateIntegrity.title', 'Integrity')}
          value={integrity?.isValid ? t('contracts.validateIntegrity.valid', 'Valid') : integrity ? t('contracts.validateIntegrity.invalid', 'Invalid') : '—'}
          variant={integrity?.isValid ? 'success' : integrity ? 'danger' : 'neutral'}
        />
      </div>

      {/* Structural integrity */}
      {integrity && (
        <Card>
          <CardHeader>
            <div className="flex items-center gap-2">
              <ShieldCheck size={14} className="text-accent" />
              <h3 className="text-sm font-semibold text-heading">
                {t('contracts.validateIntegrity.title', 'Structural Integrity')}
              </h3>
            </div>
          </CardHeader>
          <CardBody className="space-y-4">
            <div className="grid grid-cols-3 gap-4 text-xs">
              <div>
                <p className="text-muted mb-0.5">{t('contracts.validateIntegrity.errors', 'Errors')}</p>
                <p className="text-heading font-medium">{integrityErrors.length}</p>
              </div>
              <div>
                <p className="text-muted mb-0.5">{t('contracts.validateIntegrity.warnings', 'Warnings')}</p>
                <p className="text-heading font-medium">{integrityWarnings.length}</p>
              </div>
              <div>
                <p className="text-muted mb-0.5">{t('contracts.validateIntegrity.status', 'Status')}</p>
                <p className="text-heading font-medium">
                  {integrity.isValid ? t('contracts.validateIntegrity.valid', 'Valid') : t('contracts.validateIntegrity.invalid', 'Invalid')}
                </p>
              </div>
            </div>

            {integrityErrors.length > 0 && (
              <div className="space-y-2">
                {integrityErrors.map((error, index) => (
                  <div key={`integrity-error-${index}`} className="text-xs text-critical bg-critical/15 border border-critical/25 rounded p-2">
                    {error}
                  </div>
                ))}
              </div>
            )}
          </CardBody>
        </Card>
      )}

      {/* Violations list */}
      <Card>
        <CardHeader>
          <div className="flex items-center justify-between">
            <div className="flex items-center gap-2">
              <AlertTriangle size={14} className="text-accent" />
              <h3 className="text-sm font-semibold text-heading">
                {t('contracts.violationsTitle', 'Rule Violations')}
              </h3>
              <span className="text-xs text-muted">({violations.length})</span>
            </div>
          </div>
        </CardHeader>
        <CardBody className="p-0">
          {violationsQuery.isLoading && <LoadingState size="sm" />}
          {violationsQuery.isError && <ErrorState onRetry={() => violationsQuery.refetch()} />}

          {!violationsQuery.isLoading && violations.length === 0 && (
            <EmptyState
              title={t('contracts.noViolations', 'No violations')}
              description={t('contracts.noViolationsFound', 'No rule violations found for this version')}
              icon={<CheckCircle size={18} className="text-success" />}
              size="compact"
            />
          )}

          {violations.length > 0 && (
            <div className="divide-y divide-edge">
              {violations.map((v, index) => {
                const fallbackConfig = SEVERITY_CONFIG.Info;
                const config = SEVERITY_CONFIG[v.severity] ?? fallbackConfig;
                const Icon = config?.Icon;

                if (!config || !Icon) {
                  return null;
                }

                return (
                  <div key={`${v.ruleName}-${v.path}-${index}`} className="flex items-start gap-3 px-4 py-3 text-xs">
                    <Icon size={14} className={`flex-shrink-0 mt-0.5 ${config.color}`} />
                    <div className="flex-1 min-w-0">
                      <div className="flex items-center gap-2 mb-0.5">
                        <span className="font-medium text-heading">{v.ruleName}</span>
                        <span className={`px-1.5 py-0.5 text-[10px] rounded ${config.bg} ${config.color}`}>
                          {v.severity}
                        </span>
                      </div>
                      <p className="text-body mb-0.5">{v.message}</p>
                      {v.path && (
                        <p className="font-mono text-[10px] text-muted truncate">{v.path}</p>
                      )}
                    </div>
                  </div>
                );
              })}
            </div>
          )}
        </CardBody>
      </Card>
    </div>
  );
}

// ── Summary Card ──────────────────────────────────────────────────────────────

function SummaryCard({
  label,
  value,
  variant,
}: {
  label: string;
  value: string | number;
  variant: 'success' | 'warning' | 'danger' | 'neutral';
}) {
  const colors = {
    success: 'border-success/25 bg-success/15 text-success',
    warning: 'border-warning/25 bg-warning/15 text-warning',
    danger: 'border-critical/25 bg-critical/15 text-critical',
    neutral: 'border-edge bg-card text-muted',
  };

  return (
    <div className={`rounded-lg border px-4 py-3 ${colors[variant]}`}>
      <p className="text-[10px] uppercase tracking-wider opacity-80 mb-0.5">{label}</p>
      <p className="text-lg font-bold">{value}</p>
    </div>
  );
}
