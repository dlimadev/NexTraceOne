import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useMutation } from '@tanstack/react-query';
import {
  Shield,
  ShieldAlert,
  ShieldCheck,
  ShieldX,
  Filter,
  AlertTriangle,
  Eye,
  ChevronDown,
  ChevronRight,
  Lock,
} from 'lucide-react';
import client from '../../../api/client';
import { Button, TextField, TextArea, Select, SearchInput } from '../../../shared/ui';

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

// ── API ────────────────────────────────────────────────────────────────────────
// Usa o client da app (baseURL /api/v1, interceptors de auth/tenant/CSRF) — a
// página órfã original usava axios cru, sem cabeçalhos de autenticação.

const scanCode = (body: {
  targetId?: string;
  files: { filePath: string; content: string }[];
  scanProvider?: string;
}): Promise<SecurityScanResult> =>
  client.post<SecurityScanResult>('/governance/security/scan/code', body).then((r) => r.data);

// ── Helpers ────────────────────────────────────────────────────────────────────

// Taxonomia intencional: 'High' não tem token DS (fica entre critical e warning).
const SEVERITY_CONFIG: Record<FindingSeverity, { color: string; bg: string; icon: React.ReactNode }> = {
  Critical: { color: 'text-critical', bg: 'bg-critical/10 border-critical/30', icon: <ShieldX className="h-3.5 w-3.5 text-critical" /> },
  High: { color: 'text-orange-500', bg: 'bg-orange-500/10 border-orange-500/30', icon: <ShieldAlert className="h-3.5 w-3.5 text-orange-500" /> },
  Medium: { color: 'text-warning', bg: 'bg-warning/10 border-warning/30', icon: <AlertTriangle className="h-3.5 w-3.5 text-warning" /> },
  Low: { color: 'text-accent', bg: 'bg-accent/10 border-accent/30', icon: <Shield className="h-3.5 w-3.5 text-accent" /> },
  Info: { color: 'text-muted', bg: 'bg-elevated border-edge', icon: <Eye className="h-3.5 w-3.5 text-muted" /> },
};

const RISK_CONFIG: Record<SecurityRiskLevel, { color: string; icon: React.ReactNode }> = {
  Clean: { color: 'text-success', icon: <ShieldCheck className="h-4 w-4 text-success" /> },
  Low: { color: 'text-accent', icon: <Shield className="h-4 w-4 text-accent" /> },
  Medium: { color: 'text-warning', icon: <AlertTriangle className="h-4 w-4 text-warning" /> },
  High: { color: 'text-orange-500', icon: <ShieldAlert className="h-4 w-4 text-orange-500" /> },
  Critical: { color: 'text-critical', icon: <ShieldX className="h-4 w-4 text-critical" /> },
};

// ── FindingRow ─────────────────────────────────────────────────────────────────

function FindingRow({ finding }: { finding: SecurityFinding }) {
  const { t } = useTranslation('securityGate');
  const [expanded, setExpanded] = useState(false);
  const cfg = SEVERITY_CONFIG[finding.severity];

  return (
    <div className={`rounded-md border ${cfg.bg} overflow-hidden`}>
      <Button
        variant="ghost"
        onClick={() => setExpanded(!expanded)}
        className="w-full h-auto flex items-start justify-start gap-3 px-4 py-3 text-left rounded-none hover:bg-white/5"
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
      </Button>

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

function SummaryCard({ label, value, color }: { label: string; value: number; color: string }) {
  return (
    <div className="flex flex-col gap-1 rounded-lg border border-edge bg-elevated px-4 py-3">
      <span className="text-xs text-muted">{label}</span>
      <span className={`text-2xl font-bold ${color}`}>{value}</span>
    </div>
  );
}

/**
 * Aba de scan de segurança (SAST) de um serviço específico, embutida no detalhe
 * do serviço. Substitui a parte "scan" da antiga página órfã
 * (<c>SecurityGateDashboardPage</c>); o dashboard de portefólio (agregados
 * cross-serviço) foi descartado — é concern de governança, não do detalhe do
 * serviço. O scan é associado ao serviço via <c>targetId</c>.
 */
export function ServiceSecurityScanTab({ serviceId }: { serviceId: string }) {
  const { t } = useTranslation('securityGate');
  const [codeInput, setCodeInput] = useState('');
  const [filePathInput, setFilePathInput] = useState('Program.cs');
  const [searchQuery, setSearchQuery] = useState('');
  const [severityFilter, setSeverityFilter] = useState<FindingSeverity | 'All'>('All');

  const scanMutation = useMutation({
    mutationFn: () =>
      scanCode({
        targetId: serviceId,
        files: [{ filePath: filePathInput, content: codeInput }],
        scanProvider: 'internal',
      }),
  });

  const canScan = codeInput.trim().length > 0 && filePathInput.trim().length > 0;

  const findings = scanMutation.data?.findings ?? [];
  const filtered = findings.filter((f) => {
    const matchesSev = severityFilter === 'All' || f.severity === severityFilter;
    const matchesSearch =
      !searchQuery ||
      f.description.toLowerCase().includes(searchQuery.toLowerCase()) ||
      f.ruleId.toLowerCase().includes(searchQuery.toLowerCase()) ||
      f.category.toLowerCase().includes(searchQuery.toLowerCase());
    return matchesSev && matchesSearch;
  });

  return (
    <div className="grid grid-cols-1 xl:grid-cols-2 gap-6">
      {/* Code input */}
      <div className="flex flex-col gap-3">
        <TextField
          size="sm"
          label={t('filePath')}
          value={filePathInput}
          onChange={(e) => setFilePathInput(e.target.value)}
          placeholder={t('catalog.security.placeholder.filePath', 'src/MyController.cs')}
          className="font-mono"
        />
        <TextArea
          label={t('sourceCode')}
          value={codeInput}
          onChange={(e) => setCodeInput(e.target.value)}
          placeholder={t('sourceCodePlaceholder')}
          textareaClassName="min-h-80 text-xs font-mono resize-none"
        />
        <Button
          variant="danger"
          onClick={() => scanMutation.mutate()}
          disabled={!canScan}
          loading={scanMutation.isPending}
          icon={<Shield className="h-4 w-4" />}
        >
          {scanMutation.isPending ? t('scanning') : t('runScan')}
        </Button>
      </div>

      {/* Results */}
      <div className="flex flex-col gap-4">
        {scanMutation.isError && (
          <div className="flex items-center gap-2 rounded-md border border-critical/30 bg-critical/10 px-4 py-3 text-sm text-critical">
            <ShieldX className="h-4 w-4" />
            {t('dashboardError', 'Scan failed. Please try again.')}
          </div>
        )}

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
                  {t('riskLevel')}:{' '}
                  <span className={(RISK_CONFIG[scanMutation.data.overallRisk] ?? RISK_CONFIG.Clean).color}>
                    {scanMutation.data.overallRisk}
                  </span>
                </p>
              </div>
              <div className="ml-auto flex gap-2">
                {(RISK_CONFIG[scanMutation.data.overallRisk] ?? RISK_CONFIG.Clean).icon}
              </div>
            </div>

            {/* Severity summary */}
            <div className="grid grid-cols-3 gap-2">
              <SummaryCard label={t('critical')} value={scanMutation.data.summary?.criticalCount ?? 0} color="text-critical" />
              <SummaryCard label={t('high')} value={scanMutation.data.summary?.highCount ?? 0} color="text-orange-500" />
              <SummaryCard label={t('medium')} value={scanMutation.data.summary?.mediumCount ?? 0} color="text-warning" />
            </div>

            {/* Filters */}
            {findings.length > 0 && (
              <div className="flex gap-2 flex-wrap">
                <SearchInput
                  size="sm"
                  value={searchQuery}
                  onChange={(e) => setSearchQuery(e.target.value)}
                  placeholder={t('searchFindings')}
                  className="flex-1 min-w-48"
                />
                <Select
                  size="sm"
                  value={severityFilter}
                  onChange={(e) => setSeverityFilter(e.target.value as FindingSeverity | 'All')}
                  options={[
                    { value: 'All', label: t('allSeverities') },
                    ...(['Critical', 'High', 'Medium', 'Low', 'Info'] as FindingSeverity[]).map((s) => ({ value: s, label: s })),
                  ]}
                />
              </div>
            )}

            {/* Findings list */}
            {filtered.length > 0 ? (
              <div className="flex flex-col gap-2 max-h-[60vh] overflow-y-auto pr-1">
                {filtered.map((f) => <FindingRow key={f.findingId} finding={f} />)}
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
  );
}
