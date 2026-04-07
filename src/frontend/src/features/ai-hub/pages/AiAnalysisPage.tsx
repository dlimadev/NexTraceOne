import * as React from 'react';
import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { AlertTriangle, ArrowUpCircle, ArrowLeftRight, Loader2, XCircle } from 'lucide-react';
import { PageContainer } from '../../../components/shell';
import { Badge } from '../../../components/Badge';
import { Button } from '../../../components/Button';
import { useAuth } from '../../../contexts/AuthContext';
import { useEnvironment } from '../../../contexts/EnvironmentContext';
import { aiGovernanceApi } from '../api/aiGovernance';

// ── Types ────────────────────────────────────────────────────────────────────

type AnalysisTab = 'non-prod' | 'compare' | 'readiness';

interface AnalysisFinding {
  severity: string;
  category: string;
  description: string;
}

interface NonProdAnalysisResult {
  overallRiskLevel: string;
  recommendation: string;
  findings: AnalysisFinding[];
  isFallback: boolean;
  correlationId: string;
}

interface EnvironmentDivergence {
  severity: string;
  dimension: string;
  description: string;
}

interface CompareResult {
  promotionRecommendation: string;
  summary: string;
  divergences: EnvironmentDivergence[];
  isFallback: boolean;
  correlationId: string;
}

interface ReadinessIssue {
  type: string;
  category: string;
  description: string;
}

interface ReadinessResult {
  readinessScore: number;
  readinessLevel: string;
  blockers: ReadinessIssue[];
  warnings: ReadinessIssue[];
  shouldBlock: boolean;
  summary: string;
  isFallback: boolean;
  correlationId: string;
}

// ── Helpers ──────────────────────────────────────────────────────────────────

function riskColor(level: string): string {
  switch (level?.toUpperCase()) {
    case 'HIGH': return 'text-critical';
    case 'MEDIUM': return 'text-warning';
    case 'LOW': return 'text-success';
    default: return 'text-muted';
  }
}

function severityBadgeVariant(severity: string): 'danger' | 'warning' | 'info' {
  switch (severity?.toUpperCase()) {
    case 'HIGH': return 'danger';
    case 'MEDIUM': return 'warning';
    default: return 'info';
  }
}

function recommendationColor(rec: string): string {
  switch (rec?.toUpperCase()) {
    case 'BLOCK_PROMOTION':
    case 'NOT_READY': return 'text-critical';
    case 'REVIEW_REQUIRED':
    case 'NEEDS_REVIEW': return 'text-warning';
    case 'SAFE_TO_PROMOTE':
    case 'READY': return 'text-success';
    default: return 'text-muted';
  }
}

// ── Sub-components ────────────────────────────────────────────────────────────

function FallbackBadge({ show, label }: { show: boolean; label: string }) {
  if (!show) return null;
  return <Badge variant="warning">{label}</Badge>;
}

function CorrelationIdRow({ label, id }: { label: string; id: string }) {
  return (
    <p className="text-xs text-faded mt-2">
      {label}: <span className="font-mono">{id}</span>
    </p>
  );
}

// ── Non-Prod Tab ──────────────────────────────────────────────────────────────

function NonProdTab({
  tenantId,
  environmentId,
  environmentName,
  environmentProfile,
  isProductionLike,
}: {
  tenantId: string;
  environmentId: string;
  environmentName: string;
  environmentProfile: string;
  isProductionLike: boolean;
}) {
  const { t } = useTranslation();
  const [loading, setLoading] = useState(false);
  const [result, setResult] = useState<NonProdAnalysisResult | null>(null);
  const [error, setError] = useState<string | null>(null);

  const runAnalysis = async () => {
    setLoading(true);
    setError(null);
    try {
      const data = await aiGovernanceApi.analyzeNonProdEnvironment({
        tenantId,
        environmentId,
        environmentName,
        environmentProfile,
        observationWindowDays: 7,
      });
      setResult(data);
    } catch {
      setError(t('aiAnalysis.errors.analysisFailedTryAgain'));
    } finally {
      setLoading(false);
    }
  };

  if (isProductionLike) {
    return (
      <div className="rounded-lg border border-edge bg-elevated p-6 text-sm text-muted">
        {t('aiAnalysis.nonProd.productionNotApplicable')}
      </div>
    );
  }

  return (
    <div className="space-y-4">
      <div className="rounded-lg border border-edge bg-elevated p-6">
        <h3 className="text-sm font-semibold text-heading mb-1">{t('aiAnalysis.nonProd.title')}</h3>
        <p className="text-xs text-muted mb-4">
          {t('aiAnalysis.nonProd.description', { environment: environmentName })}
        </p>
        <Button onClick={runAnalysis} disabled={loading} size="sm">
          {loading ? <Loader2 size={14} className="animate-spin mr-1 inline" /> : null}
          {t('aiAnalysis.nonProd.runAnalysis')}
        </Button>
      </div>

      {error && (
        <div className="rounded-lg border border-critical/25 bg-critical/15 p-4 text-sm text-critical flex items-center gap-2">
          <XCircle size={16} />
          {error}
        </div>
      )}

      {result && (
        <div className="rounded-lg border border-edge bg-elevated p-6 space-y-4">
          <div className="flex items-center gap-3">
            <span className="text-xs text-muted uppercase tracking-wide">{t('aiAnalysis.nonProd.overallRisk')}</span>
            <span className={`text-sm font-bold ${riskColor(result.overallRiskLevel)}`}>
              {result.overallRiskLevel}
            </span>
            <FallbackBadge show={result.isFallback} label={t('aiAnalysis.fallback')} />
          </div>

          {result.recommendation && (
            <p className="text-sm text-muted">{result.recommendation}</p>
          )}

          {result.findings.length > 0 && (
            <div>
              <p className="text-xs text-muted uppercase tracking-wide mb-2">{t('aiAnalysis.nonProd.findings')}</p>
              <ul className="space-y-2">
                {result.findings.map((f, i) => (
                  <li key={i} className="flex items-start gap-2 text-sm">
                    <Badge variant={severityBadgeVariant(f.severity)}>{f.severity}</Badge>
                    <span className="text-muted text-xs">[{f.category}]</span>
                    <span className="text-muted">{f.description}</span>
                  </li>
                ))}
              </ul>
            </div>
          )}

          <CorrelationIdRow label={t('aiAnalysis.correlationId')} id={result.correlationId} />
        </div>
      )}
    </div>
  );
}

// ── Compare Tab ───────────────────────────────────────────────────────────────

function CompareTab({
  tenantId,
  environmentId,
  environmentName,
  environmentProfile,
  availableEnvironments,
}: {
  tenantId: string;
  environmentId: string;
  environmentName: string;
  environmentProfile: string;
  availableEnvironments: Array<{ id: string; name: string; profile: string }>;
}) {
  const { t } = useTranslation();
  const [referenceId, setReferenceId] = useState('');
  const [loading, setLoading] = useState(false);
  const [result, setResult] = useState<CompareResult | null>(null);
  const [error, setError] = useState<string | null>(null);

  const otherEnvs = availableEnvironments.filter(e => e.id !== environmentId);

  const runComparison = async () => {
    if (!referenceId) return;
    const refEnv = availableEnvironments.find(e => e.id === referenceId);
    if (!refEnv) return;

    setLoading(true);
    setError(null);
    try {
      const data = await aiGovernanceApi.compareEnvironments({
        tenantId,
        subjectEnvironmentId: environmentId,
        subjectEnvironmentName: environmentName,
        subjectEnvironmentProfile: environmentProfile,
        referenceEnvironmentId: refEnv.id,
        referenceEnvironmentName: refEnv.name,
        referenceEnvironmentProfile: refEnv.profile,
      });
      setResult(data);
    } catch {
      setError(t('aiAnalysis.errors.comparisonFailed'));
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="space-y-4">
      <div className="rounded-lg border border-edge bg-elevated p-6">
        <h3 className="text-sm font-semibold text-heading mb-1">{t('aiAnalysis.compare.title')}</h3>
        <p className="text-xs text-muted mb-4">{t('aiAnalysis.compare.description')}</p>

        <div className="flex flex-col gap-3 sm:flex-row sm:items-end">
          <div className="flex-1">
            <label className="block text-xs text-muted mb-1">{t('aiAnalysis.compare.subjectLabel')}</label>
            <div className="rounded border border-edge bg-elevated px-3 py-2 text-sm text-muted">
              {environmentName}
            </div>
          </div>
          <div className="flex-1">
            <label className="block text-xs text-muted mb-1">{t('aiAnalysis.compare.referenceLabel')}</label>
            <select
              className="w-full rounded border border-edge bg-elevated px-3 py-2 text-sm text-muted focus:border-accent focus:outline-none"
              value={referenceId}
              onChange={e => setReferenceId(e.target.value)}
            >
              <option value="">{t('aiAnalysis.compare.selectReference')}</option>
              {otherEnvs.map(e => (
                <option key={e.id} value={e.id}>{e.name}</option>
              ))}
            </select>
          </div>
          <Button onClick={runComparison} disabled={loading || !referenceId} size="sm">
            {loading ? <Loader2 size={14} className="animate-spin mr-1 inline" /> : null}
            {t('aiAnalysis.compare.runComparison')}
          </Button>
        </div>
      </div>

      {error && (
        <div className="rounded-lg border border-critical/25 bg-critical/15 p-4 text-sm text-critical flex items-center gap-2">
          <XCircle size={16} />
          {error}
        </div>
      )}

      {result && (
        <div className="rounded-lg border border-edge bg-elevated p-6 space-y-4">
          <div className="flex items-center gap-3">
            <span className={`text-sm font-bold ${recommendationColor(result.promotionRecommendation)}`}>
              {result.promotionRecommendation}
            </span>
            <FallbackBadge show={result.isFallback} label={t('aiAnalysis.fallback')} />
          </div>

          {result.summary && (
            <p className="text-sm text-muted">{result.summary}</p>
          )}

          {result.divergences.length > 0 && (
            <div>
              <p className="text-xs text-muted uppercase tracking-wide mb-2">{t('aiAnalysis.compare.divergences')}</p>
              <ul className="space-y-2">
                {result.divergences.map((d, i) => (
                  <li key={i} className="flex items-start gap-2 text-sm">
                    <Badge variant={severityBadgeVariant(d.severity)}>{d.severity}</Badge>
                    <span className="text-muted text-xs">[{d.dimension}]</span>
                    <span className="text-muted">{d.description}</span>
                  </li>
                ))}
              </ul>
            </div>
          )}

          <CorrelationIdRow label={t('aiAnalysis.correlationId')} id={result.correlationId} />
        </div>
      )}
    </div>
  );
}

// ── Readiness Tab ─────────────────────────────────────────────────────────────

function ReadinessTab({
  tenantId,
  environmentId,
  environmentName,
  availableEnvironments,
}: {
  tenantId: string;
  environmentId: string;
  environmentName: string;
  environmentProfile: string;
  availableEnvironments: Array<{ id: string; name: string; profile: string }>;
}) {
  const { t } = useTranslation();
  const [serviceName, setServiceName] = useState('');
  const [version, setVersion] = useState('');
  const [targetId, setTargetId] = useState('');
  const [loading, setLoading] = useState(false);
  const [result, setResult] = useState<ReadinessResult | null>(null);
  const [error, setError] = useState<string | null>(null);

  const otherEnvs = availableEnvironments.filter(e => e.id !== environmentId);

  const assess = async () => {
    if (!serviceName || !version || !targetId) return;
    const targetEnv = availableEnvironments.find(e => e.id === targetId);
    if (!targetEnv) return;

    setLoading(true);
    setError(null);
    try {
      const data = await aiGovernanceApi.assessPromotionReadiness({
        tenantId,
        sourceEnvironmentId: environmentId,
        sourceEnvironmentName: environmentName,
        targetEnvironmentId: targetEnv.id,
        targetEnvironmentName: targetEnv.name,
        serviceName,
        version,
        observationWindowDays: 7,
      });
      setResult(data);
    } catch {
      setError(t('aiAnalysis.errors.readinessFailed'));
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="space-y-4">
      <div className="rounded-lg border border-edge bg-elevated p-6">
        <h3 className="text-sm font-semibold text-heading mb-1">{t('aiAnalysis.readiness.title')}</h3>
        <p className="text-xs text-muted mb-4">{t('aiAnalysis.readiness.description')}</p>

        <div className="grid grid-cols-1 gap-3 sm:grid-cols-2">
          <div>
            <label className="block text-xs text-muted mb-1">{t('aiAnalysis.readiness.serviceName')}</label>
            <input
              className="w-full rounded border border-edge bg-elevated px-3 py-2 text-sm text-muted focus:border-accent focus:outline-none"
              value={serviceName}
              onChange={e => setServiceName(e.target.value)}
              placeholder="e.g. payment-service"
            />
          </div>
          <div>
            <label className="block text-xs text-muted mb-1">{t('aiAnalysis.readiness.version')}</label>
            <input
              className="w-full rounded border border-edge bg-elevated px-3 py-2 text-sm text-muted focus:border-accent focus:outline-none"
              value={version}
              onChange={e => setVersion(e.target.value)}
              placeholder="e.g. 2.1.0"
            />
          </div>
          <div className="sm:col-span-2">
            <label className="block text-xs text-muted mb-1">{t('aiAnalysis.readiness.targetEnvironment')}</label>
            <select
              className="w-full rounded border border-edge bg-elevated px-3 py-2 text-sm text-muted focus:border-accent focus:outline-none"
              value={targetId}
              onChange={e => setTargetId(e.target.value)}
            >
              <option value="">{t('aiAnalysis.readiness.selectTarget')}</option>
              {otherEnvs.map(e => (
                <option key={e.id} value={e.id}>{e.name}</option>
              ))}
            </select>
          </div>
        </div>

        <div className="mt-4">
          <Button onClick={assess} disabled={loading || !serviceName || !version || !targetId} size="sm">
            {loading ? <Loader2 size={14} className="animate-spin mr-1 inline" /> : null}
            {t('aiAnalysis.readiness.assess')}
          </Button>
        </div>
      </div>

      {error && (
        <div className="rounded-lg border border-critical/25 bg-critical/15 p-4 text-sm text-critical flex items-center gap-2">
          <XCircle size={16} />
          {error}
        </div>
      )}

      {result && (
        <div className="rounded-lg border border-edge bg-elevated p-6 space-y-4">
          <div className="flex items-center gap-4">
            <div className="flex items-center gap-2">
              <span className="text-xs text-muted">{t('aiAnalysis.readiness.score')}</span>
              <span className={`text-2xl font-bold ${recommendationColor(result.readinessLevel)}`}>
                {result.readinessScore}
              </span>
            </div>
            <span className={`text-sm font-semibold ${recommendationColor(result.readinessLevel)}`}>
              {result.readinessLevel}
            </span>
            {result.shouldBlock && (
              <Badge variant="danger">{t('aiAnalysis.readiness.blocked')}</Badge>
            )}
            <FallbackBadge show={result.isFallback} label={t('aiAnalysis.fallback')} />
          </div>

          {result.summary && (
            <p className="text-sm text-muted">{result.summary}</p>
          )}

          {result.blockers.length > 0 && (
            <div>
              <p className="text-xs text-muted uppercase tracking-wide mb-2 flex items-center gap-1">
                <XCircle size={12} className="text-critical" />
                {t('aiAnalysis.readiness.blockers')}
              </p>
              <ul className="space-y-2">
                {result.blockers.map((b, i) => (
                  <li key={i} className="flex items-start gap-2 text-sm">
                    <Badge variant="danger">{t('aiAnalysis.readiness.blocker')}</Badge>
                    <span className="text-muted text-xs">[{b.category}]</span>
                    <span className="text-muted">{b.description}</span>
                  </li>
                ))}
              </ul>
            </div>
          )}

          {result.warnings.length > 0 && (
            <div>
              <p className="text-xs text-muted uppercase tracking-wide mb-2 flex items-center gap-1">
                <AlertTriangle size={12} className="text-warning" />
                {t('aiAnalysis.readiness.warnings')}
              </p>
              <ul className="space-y-2">
                {result.warnings.map((w, i) => (
                  <li key={i} className="flex items-start gap-2 text-sm">
                    <Badge variant="warning">{t('aiAnalysis.readiness.warning')}</Badge>
                    <span className="text-muted text-xs">[{w.category}]</span>
                    <span className="text-muted">{w.description}</span>
                  </li>
                ))}
              </ul>
            </div>
          )}

          <CorrelationIdRow label={t('aiAnalysis.correlationId')} id={result.correlationId} />
        </div>
      )}
    </div>
  );
}

// ── Page ──────────────────────────────────────────────────────────────────────

export function AiAnalysisPage() {
  const { t } = useTranslation();
  const { tenantId } = useAuth();
  const { activeEnvironment, availableEnvironments } = useEnvironment();
  const [activeTab, setActiveTab] = useState<AnalysisTab>('non-prod');

  const tabs: Array<{ key: AnalysisTab; label: string; icon: React.ReactNode }> = [
    { key: 'non-prod', label: t('aiAnalysis.tabs.non-prod'), icon: <AlertTriangle size={14} /> },
    { key: 'compare', label: t('aiAnalysis.tabs.compare'), icon: <ArrowLeftRight size={14} /> },
    { key: 'readiness', label: t('aiAnalysis.tabs.readiness'), icon: <ArrowUpCircle size={14} /> },
  ];

  if (!tenantId || !activeEnvironment) {
    return (
      <PageContainer>
        <div className="rounded-lg border border-edge bg-elevated p-8 text-center">
          <p className="text-sm text-muted">{t('aiAnalysis.noContextAvailable')}</p>
        </div>
      </PageContainer>
    );
  }

  return (
    <PageContainer>
      {/* Header */}
      <div className="mb-6">
        <div className="flex items-center gap-2 mb-1">
          <h1 className="text-xl font-semibold text-heading">{t('aiAnalysis.title')}</h1>
          {!activeEnvironment.isProductionLike && (
            <Badge variant="info">{t('aiAnalysis.nonProductionMode')}</Badge>
          )}
        </div>
        <p className="text-sm text-muted">
          {t('aiAnalysis.analyzingContext', { environment: activeEnvironment.name })}
        </p>
        <p className="text-xs text-faded mt-1">{t('aiAnalysis.subtitle')}</p>
      </div>

      {/* Tabs */}
      <div className="flex gap-1 mb-6 border-b border-edge">
        {tabs.map(tab => (
          <button
            key={tab.key}
            onClick={() => setActiveTab(tab.key)}
            className={`flex items-center gap-1.5 px-4 py-2 text-sm font-medium border-b-2 transition-colors ${
              activeTab === tab.key
                ? 'border-accent text-accent'
                : 'border-transparent text-muted hover:text-muted'
            }`}
          >
            {tab.icon}
            {tab.label}
          </button>
        ))}
      </div>

      {/* Tab content */}
      {activeTab === 'non-prod' && (
        <NonProdTab
          tenantId={tenantId}
          environmentId={activeEnvironment.id}
          environmentName={activeEnvironment.name}
          environmentProfile={activeEnvironment.profile}
          isProductionLike={activeEnvironment.isProductionLike}
        />
      )}
      {activeTab === 'compare' && (
        <CompareTab
          tenantId={tenantId}
          environmentId={activeEnvironment.id}
          environmentName={activeEnvironment.name}
          environmentProfile={activeEnvironment.profile}
          availableEnvironments={availableEnvironments}
        />
      )}
      {activeTab === 'readiness' && (
        <ReadinessTab
          tenantId={tenantId}
          environmentId={activeEnvironment.id}
          environmentName={activeEnvironment.name}
          environmentProfile={activeEnvironment.profile}
          availableEnvironments={availableEnvironments}
        />
      )}
    </PageContainer>
  );
}
