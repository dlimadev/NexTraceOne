import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useMutation, useQuery } from '@tanstack/react-query';
import {
  Shield,
  ShieldAlert,
  ShieldCheck,
  ShieldX,
  Search,
  Filter,
  AlertTriangle,
  CheckCircle,
  XCircle,
  Eye,
  Loader2,
  FileCode,
  ChevronDown,
  ChevronRight,
  BarChart3,
  Lock,
} from 'lucide-react';
import axios from 'axios';
import { useEnvironment } from '../../../contexts/EnvironmentContext';
import { PageContainer } from '../../../components/shell';
import { PageHeader } from '../../../components/PageHeader';

// ── Types ──────────────────────────────────────────────────────────────────────

type FindingSeverity = 'Critical' | 'High' | 'Medium' | 'Low' | 'Info';
type SecurityRiskLevel = 'Clean' | 'Low' | 'Medium' | 'High' | 'Critical';
type FindingStatus = 'Open' | 'Acknowledged' | 'Mitigated' | 'FalsePositive';

interface SecurityFinding {
  findingId: string;
  ruleId: string;
  category: string;
  severity: FindingSeverity;
  filePath: string;
  lineNumber?: number;
  description: string;
  remediation: string;
  cweId?: string;
  owaspCategory?: string;
  isAiGenerated: boolean;
  status: FindingStatus;
}

interface SecurityScanSummary {
  totalFindings: number;
  criticalCount: number;
  highCount: number;
  mediumCount: number;
  lowCount: number;
  infoCount: number;
  topCategories: string[];
}

interface SecurityScanResult {
  scanId: string;
  targetType: string;
  scannedAt: string;
  scanProvider: string;
  overallRisk: SecurityRiskLevel;
  passedGate: boolean;
  findings: SecurityFinding[];
  summary: SecurityScanSummary;
}

interface GateEvaluation {
  scanId: string;
  passedGate: boolean;
  overallRisk: SecurityRiskLevel;
  evaluationReason: string;
  violatedPolicies: string[];
}

// ── API ────────────────────────────────────────────────────────────────────────

const securityApi = {
  scanCode: (body: {
    targetId?: string;
    files: { filePath: string; content: string }[];
    scanProvider?: string;
  }): Promise<SecurityScanResult> =>
    axios.post('/api/v1/governance/security/scan/code', body).then(r => r.data),

  evaluateGate: (body: {
    scanId: string;
    maxCritical?: number;
    maxHigh?: number;
  }): Promise<GateEvaluation> =>
    axios.post('/api/v1/governance/security/gate/evaluate', body).then(r => r.data),

  getDashboard: (): Promise<{
    totalScans: number;
    passedScans: number;
    failedScans: number;
    criticalFindings: number;
    highFindings: number;
    topVulnerableCategories: string[];
    overallSecurityScore: number;
    recentScans: { scanId: string; scannedAt: string; overallRisk: SecurityRiskLevel; passedGate: boolean }[];
  }> =>
    axios.get('/api/v1/governance/security/dashboard').then(r => r.data),
};

// ── Helpers ────────────────────────────────────────────────────────────────────

const SEVERITY_CONFIG: Record<FindingSeverity, { color: string; bg: string; icon: React.ReactNode }> = {
  Critical: {
    color: 'text-critical',
    bg: 'bg-critical/10 border-critical/30',
    icon: <ShieldX className="h-3.5 w-3.5 text-critical" />,
  },
  High: {
    color: 'text-orange-400',
    bg: 'bg-orange-500/10 border-orange-500/30',
    icon: <ShieldAlert className="h-3.5 w-3.5 text-orange-400" />,
  },
  Medium: {
    color: 'text-warning',
    bg: 'bg-warning/10 border-warning/30',
    icon: <AlertTriangle className="h-3.5 w-3.5 text-warning" />,
  },
  Low: {
    color: 'text-accent',
    bg: 'bg-accent/10 border-accent/30',
    icon: <Shield className="h-3.5 w-3.5 text-accent" />,
  },
  Info: {
    color: 'text-muted',
    bg: 'bg-elevated border-edge',
    icon: <Eye className="h-3.5 w-3.5 text-muted" />,
  },
};

const RISK_CONFIG: Record<SecurityRiskLevel, { color: string; icon: React.ReactNode }> = {
  Clean: { color: 'text-success', icon: <ShieldCheck className="h-4 w-4 text-success" /> },
  Low: { color: 'text-accent', icon: <Shield className="h-4 w-4 text-accent" /> },
  Medium: { color: 'text-warning', icon: <AlertTriangle className="h-4 w-4 text-warning" /> },
  High: { color: 'text-orange-400', icon: <ShieldAlert className="h-4 w-4 text-orange-400" /> },
  Critical: { color: 'text-critical', icon: <ShieldX className="h-4 w-4 text-critical" /> },
};

// ── FindingRow ─────────────────────────────────────────────────────────────────

function FindingRow({ finding }: { finding: SecurityFinding }) {
  const { t } = useTranslation('securityGate');
  const [expanded, setExpanded] = useState(false);
  const cfg = SEVERITY_CONFIG[finding.severity];

  return (
    <div className={`rounded-md border ${cfg.bg} overflow-hidden`}>
      <button
        onClick={() => setExpanded(!expanded)}
        className="w-full flex items-start gap-3 px-4 py-3 text-left hover:bg-white/5 transition-colors"
      >
        <span className="mt-0.5 shrink-0">{cfg.icon}</span>
        <div className="flex-1 min-w-0">
          <div className="flex items-center gap-2 flex-wrap">
            <span className={`text-xs font-semibold ${cfg.color}`}>{finding.severity}</span>
            <span className="text-xs text-muted">{finding.ruleId}</span>
            {finding.cweId && (
              <span className="rounded bg-elevated px-1.5 py-0.5 text-xs text-muted">{finding.cweId}</span>
            )}
            {finding.owaspCategory && (
              <span className="rounded bg-elevated px-1.5 py-0.5 text-xs text-muted">{finding.owaspCategory}</span>
            )}
            <span className={`ml-auto rounded-full px-2 py-0.5 text-xs border ${
              finding.status === 'Open'
                ? 'border-critical/30 bg-critical/10 text-critical'
                : finding.status === 'Acknowledged'
                ? 'border-warning/30 bg-warning/10 text-warning'
                : 'border-success/30 bg-success/10 text-success'
            }`}>
              {finding.status}
            </span>
          </div>
          <p className="mt-1 text-sm text-body">{finding.description}</p>
          <p className="mt-0.5 text-xs text-muted font-mono truncate">{finding.filePath}{finding.lineNumber ? `:${finding.lineNumber}` : ''}</p>
        </div>
        {expanded ? <ChevronDown className="h-4 w-4 text-muted shrink-0 mt-0.5" /> : <ChevronRight className="h-4 w-4 text-muted shrink-0 mt-0.5" />}
      </button>

      {expanded && (
        <div className="px-4 pb-4 pt-2 border-t border-edge/50">
          <div className="flex items-start gap-2 rounded-md bg-elevated/40 px-3 py-2.5">
            <Lock className="h-4 w-4 text-success mt-0.5 shrink-0" />
            <div>
              <p className="text-xs font-medium text-success mb-1">{t('remediation')}</p>
              <p className="text-xs text-body">{finding.remediation}</p>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}

// ── Summary Cards ──────────────────────────────────────────────────────────────

function SummaryCard({ label, value, color }: { label: string; value: number; color: string }) {
  return (
    <div className="flex flex-col gap-1 rounded-lg border border-edge bg-elevated px-4 py-3">
      <span className="text-xs text-muted">{label}</span>
      <span className={`text-2xl font-bold ${color}`}>{value}</span>
    </div>
  );
}

// ── Main Page ──────────────────────────────────────────────────────────────────

export function SecurityGateDashboardPage() {
  const { t } = useTranslation('securityGate');
  const { activeEnvironmentId } = useEnvironment();
  const [activeTab, setActiveTab] = useState<'scan' | 'dashboard'>('scan');
  const [codeInput, setCodeInput] = useState('');
  const [filePathInput, setFilePathInput] = useState('Program.cs');
  const [searchQuery, setSearchQuery] = useState('');
  const [severityFilter, setSeverityFilter] = useState<FindingSeverity | 'All'>('All');

  // Dashboard data
  const dashboardQuery = useQuery({
    queryKey: ['security-dashboard', activeEnvironmentId],
    queryFn: securityApi.getDashboard,
    enabled: activeTab === 'dashboard',
    retry: false,
  });

  // Scan mutation
  const scanMutation = useMutation({
    mutationFn: () =>
      securityApi.scanCode({
        files: [{ filePath: filePathInput, content: codeInput }],
        scanProvider: 'internal',
      }),
  });

  const canScan = codeInput.trim().length > 0 && filePathInput.trim().length > 0;

  // Filter findings
  const findings = scanMutation.data?.findings ?? [];
  const filtered = findings.filter(f => {
    const matchesSev = severityFilter === 'All' || f.severity === severityFilter;
    const matchesSearch =
      !searchQuery ||
      f.description.toLowerCase().includes(searchQuery.toLowerCase()) ||
      f.ruleId.toLowerCase().includes(searchQuery.toLowerCase()) ||
      f.category.toLowerCase().includes(searchQuery.toLowerCase());
    return matchesSev && matchesSearch;
  });

  return (
    <PageContainer>
      <PageHeader
        title={t('catalog.securityGate.title', 'Security Gate Dashboard')}
        subtitle={t('catalog.securityGate.subtitle', 'SAST security scanning and gate evaluation for your services')}
      />
      {/* Engine badge */}
      <div className="flex">
        <div className="flex items-center gap-2 rounded-full border border-success/30 bg-success/10 px-3 py-1.5">
          <Shield className="h-4 w-4 text-success" />
          <span className="text-xs text-success">{t('sastEngine')}</span>
        </div>
      </div>

      {/* Tabs */}
      <div className="flex gap-1 rounded-lg border border-edge bg-elevated/40 p-1 w-fit">
        {(['scan', 'dashboard'] as const).map(tab => (
          <button
            key={tab}
            onClick={() => setActiveTab(tab)}
            className={`rounded-md px-4 py-1.5 text-sm transition-colors ${
              activeTab === tab
                ? 'bg-elevated text-body'
                : 'text-muted hover:text-body'
            }`}
          >
            {t(`tabs.${tab}`)}
          </button>
        ))}
      </div>

      {/* ── Scan Tab ── */}
      {activeTab === 'scan' && (
        <div className="grid grid-cols-1 xl:grid-cols-2 gap-6">
          {/* Code input */}
          <div className="flex flex-col gap-3">
            <div className="flex flex-col gap-1.5">
              <label className="text-xs font-medium text-muted">{t('filePath')}</label>
              <input
                type="text"
                value={filePathInput}
                onChange={e => setFilePathInput(e.target.value)}
                placeholder={t('catalog.security.placeholder.filePath', 'src/MyController.cs')}
                className="rounded-md border border-edge bg-elevated px-3 py-2 text-sm text-body placeholder-muted focus:border-accent focus:outline-none font-mono"
              />
            </div>
            <div className="flex flex-col gap-1.5 flex-1">
              <label className="text-xs font-medium text-muted">{t('sourceCode')}</label>
              <textarea
                value={codeInput}
                onChange={e => setCodeInput(e.target.value)}
                placeholder={t('sourceCodePlaceholder')}
                className="min-h-80 rounded-md border border-edge bg-elevated px-3 py-2 text-xs font-mono text-body placeholder-muted focus:border-accent focus:outline-none resize-none"
              />
            </div>
            <button
              onClick={() => scanMutation.mutate()}
              disabled={!canScan || scanMutation.isPending}
              className="flex items-center justify-center gap-2 rounded-md bg-critical hover:bg-critical/90 disabled:opacity-50 disabled:cursor-not-allowed px-4 py-2.5 text-sm font-medium text-white transition-colors"
            >
              {scanMutation.isPending ? (
                <><Loader2 className="h-4 w-4 animate-spin" /> {t('scanning')}</>
              ) : (
                <><Shield className="h-4 w-4" /> {t('runScan')}</>
              )}
            </button>
          </div>

          {/* Results */}
          <div className="flex flex-col gap-4">
            {scanMutation.isSuccess && scanMutation.data && (
              <>
                {/* Gate status */}
                <div className={`flex items-center gap-3 rounded-lg border px-4 py-3 ${
                  scanMutation.data.passedGate
                    ? 'border-success/30 bg-success/10'
                    : 'border-critical/30 bg-critical/10'
                }`}>
                  {scanMutation.data.passedGate
                    ? <ShieldCheck className="h-5 w-5 text-success" />
                    : <ShieldX className="h-5 w-5 text-critical" />}
                  <div>
                    <p className={`text-sm font-medium ${scanMutation.data.passedGate ? 'text-success' : 'text-critical'}`}>
                      {scanMutation.data.passedGate ? t('gatePassed') : t('gateFailed')}
                    </p>
                    <p className="text-xs text-muted">
                      {t('riskLevel')}: {' '}
                      <span className={RISK_CONFIG[scanMutation.data.overallRisk].color}>
                        {scanMutation.data.overallRisk}
                      </span>
                    </p>
                  </div>
                  <div className="ml-auto flex gap-2">
                    {RISK_CONFIG[scanMutation.data.overallRisk].icon}
                  </div>
                </div>

                {/* Severity summary */}
                <div className="grid grid-cols-3 gap-2">
                  <SummaryCard label={t('critical')} value={scanMutation.data.summary.criticalCount} color="text-critical" />
                  <SummaryCard label={t('high')} value={scanMutation.data.summary.highCount} color="text-orange-400" />
                  <SummaryCard label={t('medium')} value={scanMutation.data.summary.mediumCount} color="text-warning" />
                </div>

                {/* Filters */}
                {findings.length > 0 && (
                  <div className="flex gap-2 flex-wrap">
                    <div className="relative flex-1 min-w-48">
                      <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-3.5 w-3.5 text-muted" />
                      <input
                        type="text"
                        value={searchQuery}
                        onChange={e => setSearchQuery(e.target.value)}
                        placeholder={t('searchFindings')}
                        className="w-full rounded-md border border-edge bg-elevated pl-9 pr-3 py-2 text-xs text-body placeholder-muted focus:border-accent focus:outline-none"
                      />
                    </div>
                    <select
                      value={severityFilter}
                      onChange={e => setSeverityFilter(e.target.value as FindingSeverity | 'All')}
                      className="rounded-md border border-edge bg-elevated px-3 py-2 text-xs text-body focus:border-accent focus:outline-none"
                    >
                      <option value="All">{t('allSeverities')}</option>
                      {(['Critical', 'High', 'Medium', 'Low', 'Info'] as FindingSeverity[]).map(s => (
                        <option key={s} value={s}>{s}</option>
                      ))}
                    </select>
                  </div>
                )}

                {/* Findings list */}
                {filtered.length > 0 ? (
                  <div className="flex flex-col gap-2 max-h-[60vh] overflow-y-auto pr-1">
                    {filtered.map(f => <FindingRow key={f.findingId} finding={f} />)}
                  </div>
                ) : findings.length > 0 ? (
                  <div className="flex items-center gap-2 rounded-md border border-edge px-4 py-3 text-sm text-muted">
                    <Filter className="h-4 w-4" />
                    {t('noMatchingFindings')}
                  </div>
                ) : (
                  <div className="flex flex-col items-center gap-2 rounded-lg border border-success/20 bg-success/5 py-8">
                    <ShieldCheck className="h-8 w-8 text-success" />
                    <p className="text-sm text-success">{t('noFindings')}</p>
                  </div>
                )}
              </>
            )}

            {!scanMutation.isSuccess && !scanMutation.isPending && (
              <div className="flex flex-col items-center gap-3 rounded-lg border border-dashed border-edge py-12 text-center">
                <ShieldAlert className="h-8 w-8 text-muted" />
                <p className="text-sm text-muted">{t('emptyState')}</p>
              </div>
            )}
          </div>
        </div>
      )}

      {/* ── Dashboard Tab ── */}
      {activeTab === 'dashboard' && (
        <div className="flex flex-col gap-6">
          {dashboardQuery.isLoading && (
            <div className="flex items-center justify-center py-16">
              <Loader2 className="h-8 w-8 animate-spin text-muted" />
            </div>
          )}

          {dashboardQuery.isError && (
            <div className="flex items-center gap-2 rounded-md border border-critical/30 bg-critical/10 px-4 py-3 text-sm text-critical">
              <XCircle className="h-4 w-4" />
              {t('dashboardError')}
            </div>
          )}

          {dashboardQuery.isSuccess && dashboardQuery.data && (
            <>
              {/* KPI cards */}
              <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
                <SummaryCard label={t('totalScans')} value={dashboardQuery.data.totalScans} color="text-body" />
                <SummaryCard label={t('passedScans')} value={dashboardQuery.data.passedScans} color="text-success" />
                <SummaryCard label={t('criticalFindings')} value={dashboardQuery.data.criticalFindings} color="text-critical" />
                <SummaryCard label={t('securityScore')} value={dashboardQuery.data.overallSecurityScore} color="text-accent" />
              </div>

              {/* Top categories */}
              {dashboardQuery.data.topVulnerableCategories.length > 0 && (
                <div className="rounded-lg border border-edge bg-elevated p-4">
                  <div className="flex items-center gap-2 mb-3">
                    <BarChart3 className="h-4 w-4 text-muted" />
                    <h3 className="text-sm font-medium text-body">{t('topCategories')}</h3>
                  </div>
                  <div className="flex flex-wrap gap-2">
                    {dashboardQuery.data.topVulnerableCategories.map(c => (
                      <span key={c} className="rounded-full border border-edge bg-elevated px-3 py-1 text-xs text-body">
                        {c}
                      </span>
                    ))}
                  </div>
                </div>
              )}

              {/* Recent scans */}
              {dashboardQuery.data.recentScans.length > 0 && (
                <div className="rounded-lg border border-edge bg-elevated overflow-hidden">
                  <div className="flex items-center gap-2 px-4 py-3 border-b border-edge">
                    <FileCode className="h-4 w-4 text-muted" />
                    <h3 className="text-sm font-medium text-body">{t('recentScans')}</h3>
                  </div>
                  <div className="divide-y divide-neutral-800">
                    {dashboardQuery.data.recentScans.map(s => (
                      <div key={s.scanId} className="flex items-center gap-4 px-4 py-3">
                        {RISK_CONFIG[s.overallRisk].icon}
                        <div className="flex-1 min-w-0">
                          <p className="text-xs text-body font-mono truncate">{s.scanId}</p>
                          <p className="text-xs text-muted">{new Date(s.scannedAt).toLocaleString()}</p>
                        </div>
                        <span className={`text-xs font-medium ${RISK_CONFIG[s.overallRisk].color}`}>
                          {s.overallRisk}
                        </span>
                        {s.passedGate ? (
                          <CheckCircle className="h-4 w-4 text-success" />
                        ) : (
                          <XCircle className="h-4 w-4 text-critical" />
                        )}
                      </div>
                    ))}
                  </div>
                </div>
              )}
            </>
          )}

          {dashboardQuery.isSuccess && dashboardQuery.data?.totalScans === 0 && (
            <div className="flex flex-col items-center gap-3 rounded-lg border border-dashed border-edge py-16 text-center">
              <Shield className="h-10 w-10 text-muted" />
              <p className="text-sm text-muted">{t('dashboardEmpty')}</p>
            </div>
          )}
        </div>
      )}
    </PageContainer>
  );
}
