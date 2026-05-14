import { useTranslation } from 'react-i18next';
import { useState } from 'react';
import {
  Eye, ShieldCheck, AlertTriangle, CheckCircle, XCircle, Users, Activity,
} from 'lucide-react';
import { Card, CardBody, CardHeader } from '../../../components/Card';
import { Badge } from '../../../components/Badge';
import { PageContainer } from '../../../components/shell';
import { PageHeader } from '../../../components/PageHeader';
import { PageErrorState } from '../../../components/PageErrorState';

/**
 * GovernanceGatesPage — interface para avaliar gates de governança:
 *  - Four Eyes Principle (princípio dos quatro olhos)
 *  - Change Advisory Board (CAB)
 *  - Error Budget Gate
 *
 * Cada gate é avaliado com base em parâmetros configuráveis (seed parametrizado).
 * Pilar: Governance + Change Intelligence
 */
export function GovernanceGatesPage() {
  const { t } = useTranslation();
  const [fourEyesResult, setFourEyesResult] = useState<FourEyesResult | null>(null);
  const [cabResult, setCabResult] = useState<CabResult | null>(null);
  const [errorBudgetResult, setErrorBudgetResult] = useState<ErrorBudgetResult | null>(null);
  const [gateError, setGateError] = useState<string | null>(null);

  const evaluateFourEyes = async (actionCode: string, requestedBy: string, approvedBy?: string) => {
    setGateError(null);
    try {
      const params = new URLSearchParams({ actionCode, requestedBy });
      if (approvedBy) params.set('approvedBy', approvedBy);
      const resp = await fetch(`/api/v1/governance/gates/four-eyes?${params}`);
      if (!resp.ok) throw new Error(`HTTP ${resp.status}`);
      setFourEyesResult(await resp.json());
    } catch {
      // Erro tratado via mensagem de UI - logging estruturado deve ser feito pelo backend
      setGateError(t('governance.gates.errors.fourEyesFailed', 'Failed to evaluate Four Eyes gate'));
    }
  };

  const evaluateCab = async (serviceName: string, environment: string, criticality: string, blastRadius: string) => {
    setGateError(null);
    try {
      const params = new URLSearchParams({ serviceName, environment, criticality, blastRadius });
      const resp = await fetch(`/api/v1/governance/gates/cab?${params}`);
      if (!resp.ok) throw new Error(`HTTP ${resp.status}`);
      setCabResult(await resp.json());
    } catch {
      // Erro tratado via mensagem de UI - logging estruturado deve ser feito pelo backend
      setGateError(t('governance.gates.errors.cabFailed', 'Failed to evaluate CAB gate'));
    }
  };

  const evaluateErrorBudget = async (serviceName: string, environment: string, errorBudgetRemainingPct: string) => {
    setGateError(null);
    try {
      const params = new URLSearchParams({ serviceName, environment, errorBudgetRemainingPct });
      const resp = await fetch(`/api/v1/governance/gates/error-budget?${params}`);
      if (!resp.ok) throw new Error(`HTTP ${resp.status}`);
      setErrorBudgetResult(await resp.json());
    } catch {
      // Erro tratado via mensagem de UI - logging estruturado deve ser feito pelo backend
      setGateError(t('governance.gates.errors.errorBudgetFailed', 'Failed to evaluate Error Budget gate'));
    }
  };

  return (
    <PageContainer>
      <PageHeader
        title={t('governance.gates.title', 'Governance Gates')}
        subtitle={t('governance.gates.subtitle', 'Evaluate governance gates before critical actions')}
        icon={<ShieldCheck size={24} />}
      />

      {gateError && (
        <div className="mt-4">
          <PageErrorState
            message={gateError}
            onRetry={() => setGateError(null)}
          />
        </div>
      )}

      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6 mt-6">
        {/* Four Eyes Principle */}
        <Card>
          <CardHeader className="flex items-center gap-2">
            <Eye size={18} />
            <span>{t('governance.gates.fourEyes.title')}</span>
          </CardHeader>
          <CardBody>
            <p className="text-sm text-muted mb-4">
              {t('governance.gates.fourEyes.description')}
            </p>
            <FourEyesForm onEvaluate={evaluateFourEyes} />
            {fourEyesResult && <FourEyesResultCard result={fourEyesResult} />}
          </CardBody>
        </Card>

        {/* Change Advisory Board */}
        <Card>
          <CardHeader className="flex items-center gap-2">
            <Users size={18} />
            <span>{t('governance.gates.cab.title')}</span>
          </CardHeader>
          <CardBody>
            <p className="text-sm text-muted mb-4">
              {t('governance.gates.cab.description')}
            </p>
            <CabForm onEvaluate={evaluateCab} />
            {cabResult && <CabResultCard result={cabResult} />}
          </CardBody>
        </Card>

        {/* Error Budget Gate */}
        <Card>
          <CardHeader className="flex items-center gap-2">
            <Activity size={18} />
            <span>{t('governance.gates.errorBudget.title', 'Error Budget Gate')}</span>
          </CardHeader>
          <CardBody>
            <p className="text-sm text-muted mb-4">
              {t('governance.gates.errorBudget.description', 'Evaluate if error budget allows deployment')}
            </p>
            <ErrorBudgetForm onEvaluate={evaluateErrorBudget} />
            {errorBudgetResult && <ErrorBudgetResultCard result={errorBudgetResult} />}
          </CardBody>
        </Card>
      </div>
    </PageContainer>
  );
}

// ── Types ────────────────────────────────────────────────

interface FourEyesResult {
  actionCode: string;
  fourEyesRequired: boolean;
  isCompliant: boolean;
  reason: string;
  requiresSecondApprover: boolean;
}

interface CabResult {
  cabRequired: boolean;
  isApproved: boolean;
  reason: string;
  triggerConditions: string[];
  members: string[];
}

interface ErrorBudgetResult {
  serviceName: string;
  environment: string;
  errorBudgetRemainingPct: number;
  blockThresholdPct: number;
  isBlocked: boolean;
  reason: string;
}

// ── Sub-components ────────────────────────────────────────

function FourEyesForm({ onEvaluate }: { onEvaluate: (a: string, r: string, ap?: string) => void }) {
  const { t } = useTranslation();
  const [action, setAction] = useState('production_deploy');
  const [requester, setRequester] = useState('');
  const [approver, setApprover] = useState('');

  return (
    <div className="space-y-2">
      <select value={action} onChange={e => setAction(e.target.value)} className="input w-full" aria-label={t('governance.gates.fourEyes.action', 'Action')}>
        <option value="production_deploy">production_deploy</option>
        <option value="security_config_change">security_config_change</option>
        <option value="privileged_access_grant">privileged_access_grant</option>
        <option value="compliance_waiver">compliance_waiver</option>
        <option value="break_glass">break_glass</option>
      </select>
      <input value={requester} onChange={e => setRequester(e.target.value)} placeholder={t('governance.gates.fourEyes.requester', 'Requester')} className="input w-full" />
      <input value={approver} onChange={e => setApprover(e.target.value)} placeholder={t('governance.gates.fourEyes.approver', 'Approver (optional)')} className="input w-full" />
      <button onClick={() => onEvaluate(action, requester, approver || undefined)} className="btn btn-primary w-full" disabled={!requester}>
        {t('governance.gates.evaluate', 'Evaluate')}
      </button>
    </div>
  );
}

function FourEyesResultCard({ result }: { result: FourEyesResult }) {
  return (
    <div className="mt-4 p-3 rounded bg-surface-secondary">
      <div className="flex items-center gap-2 mb-2">
        {result.isCompliant ? <CheckCircle size={16} className="text-success" /> : <XCircle size={16} className="text-critical" />}
        <Badge variant={result.isCompliant ? 'success' : 'critical'}>{result.isCompliant ? 'Compliant' : 'Not Compliant'}</Badge>
      </div>
      <p className="text-sm">{result.reason}</p>
    </div>
  );
}

function CabForm({ onEvaluate }: { onEvaluate: (s: string, e: string, c: string, b: string) => void }) {
  const { t } = useTranslation();
  const [service, setService] = useState('');
  const [env, setEnv] = useState('production');
  const [crit, setCrit] = useState('High');
  const [blast, setBlast] = useState('Medium');

  return (
    <div className="space-y-2">
      <input value={service} onChange={e => setService(e.target.value)} placeholder={t('governance.gates.cab.serviceName', 'Service name')} className="input w-full" />
      <select value={env} onChange={e => setEnv(e.target.value)} className="input w-full" aria-label={t('governance.gates.cab.environment', 'Environment')}>
        <option value="production">{t('environment.profile.production', 'Production')}</option>
        <option value="staging">{t('environment.profile.staging', 'Staging')}</option>
        <option value="development">{t('environment.profile.development', 'Development')}</option>
      </select>
      <select value={crit} onChange={e => setCrit(e.target.value)} className="input w-full" aria-label={t('governance.gates.cab.criticality', 'Criticality')}>
        <option value="Low">Low</option>
        <option value="Medium">Medium</option>
        <option value="High">High</option>
        <option value="Critical">Critical</option>
      </select>
      <select value={blast} onChange={e => setBlast(e.target.value)} className="input w-full" aria-label={t('governance.gates.cab.blastRadius', 'Blast Radius')}>
        <option value="None">None</option>
        <option value="Low">Low</option>
        <option value="Medium">Medium</option>
        <option value="High">High</option>
      </select>
      <button onClick={() => onEvaluate(service, env, crit, blast)} className="btn btn-primary w-full" disabled={!service}>
        {t('governance.gates.evaluate', 'Evaluate')}
      </button>
    </div>
  );
}

function CabResultCard({ result }: { result: CabResult }) {
  return (
    <div className="mt-4 p-3 rounded bg-surface-secondary">
      <div className="flex items-center gap-2 mb-2">
        {result.cabRequired ? <AlertTriangle size={16} className="text-warning" /> : <CheckCircle size={16} className="text-success" />}
        <Badge variant={result.cabRequired ? 'warning' : 'success'}>{result.cabRequired ? 'CAB Required' : 'No CAB'}</Badge>
      </div>
      <p className="text-sm">{result.reason}</p>
      {result.triggerConditions.length > 0 && (
        <ul className="mt-2 text-xs space-y-1">
          {result.triggerConditions.map((tc, i) => <li key={i}>• {tc}</li>)}
        </ul>
      )}
    </div>
  );
}

function ErrorBudgetForm({ onEvaluate }: { onEvaluate: (s: string, e: string, b: string) => void }) {
  const { t } = useTranslation();
  const [service, setService] = useState('');
  const [env, setEnv] = useState('production');
  const [budget, setBudget] = useState('50');

  return (
    <div className="space-y-2">
      <input value={service} onChange={e => setService(e.target.value)} placeholder={t('governance.gates.errorBudget.serviceName', 'Service name')} className="input w-full" />
      <select value={env} onChange={e => setEnv(e.target.value)} className="input w-full" aria-label={t('governance.gates.errorBudget.environment', 'Environment')}>
        <option value="production">{t('environment.profile.production', 'Production')}</option>
        <option value="staging">{t('environment.profile.staging', 'Staging')}</option>
      </select>
      <input type="number" value={budget} onChange={e => setBudget(e.target.value)} placeholder={t('governance.gates.errorBudget.remainingPct', 'Budget remaining %')} className="input w-full" min="0" max="100" />
      <button onClick={() => onEvaluate(service, env, budget)} className="btn btn-primary w-full" disabled={!service}>
        {t('governance.gates.evaluate', 'Evaluate')}
      </button>
    </div>
  );
}

function ErrorBudgetResultCard({ result }: { result: ErrorBudgetResult }) {
  return (
    <div className="mt-4 p-3 rounded bg-surface-secondary">
      <div className="flex items-center gap-2 mb-2">
        {result.isBlocked ? <XCircle size={16} className="text-critical" /> : <CheckCircle size={16} className="text-success" />}
        <Badge variant={result.isBlocked ? 'critical' : 'success'}>{result.isBlocked ? 'Blocked' : 'Allowed'}</Badge>
      </div>
      <p className="text-sm">{result.reason}</p>
      <div className="mt-2 text-xs text-muted">
        {`Budget: ${result.errorBudgetRemainingPct}% | Threshold: ${result.blockThresholdPct}%`}
      </div>
    </div>
  );
}

export default GovernanceGatesPage;
