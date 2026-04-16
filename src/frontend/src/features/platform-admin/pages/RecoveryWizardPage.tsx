import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useQuery, useMutation } from '@tanstack/react-query';
import {
  RotateCcw,
  CheckCircle,
  XCircle,
  AlertTriangle,
  ChevronRight,
  Database,
  Clock,
  Shield,
} from 'lucide-react';
import {
  platformAdminApi,
  type RestorePoint,
  type RecoveryScope,
  type RecoveryResponse,
} from '../api/platformAdmin';

type WizardStep = 1 | 2 | 3 | 4 | 5;

interface WizardState {
  step: WizardStep;
  selectedRestorePoint: RestorePoint | null;
  scope: RecoveryScope;
  selectedSchemas: string[];
  dryRun: boolean;
  recoveryResult: RecoveryResponse | null;
}

const ALL_SCHEMAS = [
  'nextraceone_identity',
  'nextraceone_catalog',
  'nextraceone_operations',
  'nextraceone_ai',
];

export function RecoveryWizardPage() {
  const { t } = useTranslation('recoveryWizard');

  const [state, setState] = useState<WizardState>({
    step: 1,
    selectedRestorePoint: null,
    scope: 'Full',
    selectedSchemas: [...ALL_SCHEMAS],
    dryRun: true,
    recoveryResult: null,
  });

  const { data, isLoading, isError } = useQuery({
    queryKey: ['restore-points'],
    queryFn: platformAdminApi.getRestorePoints,
  });

  const recoverMutation = useMutation({
    mutationFn: platformAdminApi.initiateRecovery,
    onSuccess: (result) => {
      setState((s) => ({ ...s, step: 5, recoveryResult: result }));
    },
  });

  function goToStep(step: WizardStep) {
    setState((s) => ({ ...s, step }));
  }

  function selectRestorePoint(rp: RestorePoint) {
    setState((s) => ({ ...s, selectedRestorePoint: rp }));
  }

  function toggleSchema(schema: string) {
    setState((s) => ({
      ...s,
      selectedSchemas: s.selectedSchemas.includes(schema)
        ? s.selectedSchemas.filter((sc) => sc !== schema)
        : [...s.selectedSchemas, schema],
    }));
  }

  function executeRecovery() {
    if (!state.selectedRestorePoint) return;
    recoverMutation.mutate({
      restorePointId: state.selectedRestorePoint.id,
      scope: state.scope,
      schemas: state.scope === 'Partial' ? state.selectedSchemas : undefined,
      dryRun: state.dryRun,
    });
    goToStep(4);
  }

  const steps = [
    t('step1Label'),
    t('step2Label'),
    t('step3Label'),
    t('step4Label'),
    t('step5Label'),
  ];

  return (
    <div className="p-6 space-y-6">
      {/* Header */}
      <div className="flex items-center gap-3">
        <RotateCcw size={24} className="text-indigo-600" />
        <div>
          <h1 className="text-2xl font-semibold text-slate-900">{t('title')}</h1>
          <p className="mt-1 text-sm text-slate-500">{t('subtitle')}</p>
        </div>
      </div>

      {/* Warning Banner */}
      <div className="flex items-start gap-3 p-4 bg-amber-50 border border-amber-200 rounded-lg">
        <AlertTriangle size={18} className="text-amber-600 mt-0.5 shrink-0" />
        <p className="text-sm text-amber-800">{t('warningBanner')}</p>
      </div>

      {/* Step Indicator */}
      <StepIndicator steps={steps} currentStep={state.step} />

      {/* Step Content */}
      {isLoading && state.step === 1 && (
        <div className="flex items-center justify-center h-48 text-slate-400 text-sm">
          {t('loading')}
        </div>
      )}

      {isError && state.step === 1 && (
        <div className="flex items-center gap-3 p-4 bg-red-50 border border-red-200 rounded-lg text-red-700 text-sm">
          <XCircle size={18} />
          {t('error')}
        </div>
      )}

      {state.step === 1 && data && (
        <Step1ChooseRestorePoint
          restorePoints={data.restorePoints}
          selected={state.selectedRestorePoint}
          onSelect={selectRestorePoint}
          onNext={() => goToStep(2)}
          t={t}
        />
      )}

      {state.step === 2 && (
        <Step2ChooseScope
          scope={state.scope}
          selectedSchemas={state.selectedSchemas}
          onScopeChange={(scope) => setState((s) => ({ ...s, scope }))}
          onToggleSchema={toggleSchema}
          onBack={() => goToStep(1)}
          onNext={() => goToStep(3)}
          t={t}
        />
      )}

      {state.step === 3 && state.selectedRestorePoint && (
        <Step3Confirm
          restorePoint={state.selectedRestorePoint}
          scope={state.scope}
          schemas={state.selectedSchemas}
          dryRun={state.dryRun}
          onDryRunChange={(dryRun) => setState((s) => ({ ...s, dryRun }))}
          onBack={() => goToStep(2)}
          onExecute={executeRecovery}
          t={t}
        />
      )}

      {state.step === 4 && (
        <Step4Executing
          isRunning={recoverMutation.isPending}
          isError={recoverMutation.isError}
          t={t}
        />
      )}

      {state.step === 5 && state.recoveryResult && (
        <Step5Verify
          result={state.recoveryResult}
          onRestart={() =>
            setState({
              step: 1,
              selectedRestorePoint: null,
              scope: 'Full',
              selectedSchemas: [...ALL_SCHEMAS],
              dryRun: true,
              recoveryResult: null,
            })
          }
          t={t}
        />
      )}
    </div>
  );
}

// ── Step Indicator ────────────────────────────────────────────────────────────

function StepIndicator({
  steps,
  currentStep,
}: {
  steps: string[];
  currentStep: WizardStep;
}) {
  return (
    <div className="flex items-center gap-2">
      {steps.map((label, idx) => {
        const stepNum = (idx + 1) as WizardStep;
        const isActive = stepNum === currentStep;
        const isDone = stepNum < currentStep;
        return (
          <div key={stepNum} className="flex items-center gap-2">
            <div
              className={`flex items-center justify-center w-7 h-7 rounded-full text-xs font-semibold ${
                isDone
                  ? 'bg-emerald-500 text-white'
                  : isActive
                  ? 'bg-indigo-600 text-white'
                  : 'bg-slate-200 text-slate-500'
              }`}
            >
              {isDone ? <CheckCircle size={14} /> : stepNum}
            </div>
            <span
              className={`text-xs hidden sm:block ${
                isActive ? 'text-indigo-600 font-medium' : 'text-slate-500'
              }`}
            >
              {label}
            </span>
            {idx < steps.length - 1 && (
              <ChevronRight size={14} className="text-slate-300 ml-1" />
            )}
          </div>
        );
      })}
    </div>
  );
}

// ── Step 1: Choose Restore Point ──────────────────────────────────────────────

function Step1ChooseRestorePoint({
  restorePoints,
  selected,
  onSelect,
  onNext,
  t,
}: {
  restorePoints: RestorePoint[];
  selected: RestorePoint | null;
  onSelect: (rp: RestorePoint) => void;
  onNext: () => void;
  t: (k: string) => string;
}) {
  return (
    <div className="space-y-4">
      <h2 className="text-lg font-medium text-slate-800">{t('step1Title')}</h2>
      {restorePoints.length === 0 ? (
        <div className="flex items-center justify-center h-32 border border-slate-200 rounded-lg text-slate-400 text-sm">
          {t('noRestorePoints')}
        </div>
      ) : (
        <div className="space-y-2">
          {restorePoints.map((rp) => (
            <button
              key={rp.id}
              onClick={() => onSelect(rp)}
              className={`w-full text-left p-4 border rounded-lg transition-colors ${
                selected?.id === rp.id
                  ? 'border-indigo-400 bg-indigo-50'
                  : 'border-slate-200 hover:border-slate-300 hover:bg-slate-50'
              }`}
            >
              <div className="flex items-center justify-between">
                <div className="flex items-center gap-3">
                  <Database size={16} className="text-slate-500" />
                  <div>
                    <div className="font-medium text-slate-800">
                      {new Date(rp.timestamp).toLocaleString()}
                    </div>
                    <div className="text-xs text-slate-500">
                      {t('version')} {rp.version} · {(rp.sizeMb / 1024).toFixed(1)} GB ·{' '}
                      {rp.schemasIncluded.length} {t('schemas')}
                    </div>
                  </div>
                </div>
                <RestorePointStatusBadge status={rp.status} />
              </div>
            </button>
          ))}
        </div>
      )}
      <div className="flex justify-end">
        <button
          onClick={onNext}
          disabled={!selected}
          className="px-5 py-2 bg-indigo-600 text-white text-sm rounded-lg hover:bg-indigo-700 disabled:opacity-50"
        >
          {t('next')}
        </button>
      </div>
    </div>
  );
}

function RestorePointStatusBadge({ status }: { status: string }) {
  const map: Record<string, string> = {
    Available: 'bg-emerald-100 text-emerald-700',
    Corrupted: 'bg-red-100 text-red-700',
    Expired: 'bg-slate-100 text-slate-500',
  };
  return (
    <span className={`px-2 py-0.5 text-xs rounded-full font-medium ${map[status] ?? 'bg-slate-100 text-slate-600'}`}>
      {status}
    </span>
  );
}

// ── Step 2: Choose Scope ──────────────────────────────────────────────────────

function Step2ChooseScope({
  scope,
  selectedSchemas,
  onScopeChange,
  onToggleSchema,
  onBack,
  onNext,
  t,
}: {
  scope: RecoveryScope;
  selectedSchemas: string[];
  onScopeChange: (s: RecoveryScope) => void;
  onToggleSchema: (schema: string) => void;
  onBack: () => void;
  onNext: () => void;
  t: (k: string) => string;
}) {
  return (
    <div className="space-y-4">
      <h2 className="text-lg font-medium text-slate-800">{t('step2Title')}</h2>
      <div className="space-y-3">
        {(['Full', 'Partial'] as RecoveryScope[]).map((s) => (
          <label
            key={s}
            className={`flex items-start gap-3 p-4 border rounded-lg cursor-pointer transition-colors ${
              scope === s ? 'border-indigo-400 bg-indigo-50' : 'border-slate-200 hover:bg-slate-50'
            }`}
          >
            <input
              type="radio"
              name="scope"
              value={s}
              checked={scope === s}
              onChange={() => onScopeChange(s)}
              className="mt-1"
            />
            <div>
              <div className="font-medium text-slate-800">{t(`scope${s}`)}</div>
              <div className="text-xs text-slate-500">{t(`scope${s}Desc`)}</div>
            </div>
          </label>
        ))}
      </div>

      {scope === 'Partial' && (
        <div className="space-y-2">
          <p className="text-sm font-medium text-slate-700">{t('selectSchemas')}</p>
          {ALL_SCHEMAS.map((schema) => (
            <label key={schema} className="flex items-center gap-3 p-3 border border-slate-200 rounded-lg cursor-pointer hover:bg-slate-50">
              <input
                type="checkbox"
                checked={selectedSchemas.includes(schema)}
                onChange={() => onToggleSchema(schema)}
              />
              <span className="text-sm font-mono text-slate-700">{schema}</span>
            </label>
          ))}
        </div>
      )}

      <div className="flex justify-between">
        <button
          onClick={onBack}
          className="px-5 py-2 text-sm border border-slate-300 rounded-lg hover:bg-slate-50"
        >
          {t('back')}
        </button>
        <button
          onClick={onNext}
          className="px-5 py-2 bg-indigo-600 text-white text-sm rounded-lg hover:bg-indigo-700"
        >
          {t('next')}
        </button>
      </div>
    </div>
  );
}

// ── Step 3: Confirm ───────────────────────────────────────────────────────────

function Step3Confirm({
  restorePoint,
  scope,
  schemas,
  dryRun,
  onDryRunChange,
  onBack,
  onExecute,
  t,
}: {
  restorePoint: RestorePoint;
  scope: RecoveryScope;
  schemas: string[];
  dryRun: boolean;
  onDryRunChange: (v: boolean) => void;
  onBack: () => void;
  onExecute: () => void;
  t: (k: string) => string;
}) {
  return (
    <div className="space-y-4">
      <h2 className="text-lg font-medium text-slate-800">{t('step3Title')}</h2>

      <div className="border border-slate-200 rounded-lg divide-y divide-slate-100">
        <ConfirmRow label={t('restorePoint')} value={new Date(restorePoint.timestamp).toLocaleString()} />
        <ConfirmRow label={t('version')} value={restorePoint.version} />
        <ConfirmRow label={t('size')} value={`${(restorePoint.sizeMb / 1024).toFixed(1)} GB`} />
        <ConfirmRow label={t('scope')} value={t(`scope${scope}`)} />
        {scope === 'Partial' && (
          <ConfirmRow label={t('schemas')} value={schemas.join(', ')} />
        )}
      </div>

      <label className="flex items-center gap-3 p-4 border border-amber-200 bg-amber-50 rounded-lg cursor-pointer">
        <input
          type="checkbox"
          checked={dryRun}
          onChange={(e) => onDryRunChange(e.target.checked)}
        />
        <div>
          <div className="font-medium text-amber-800">{t('dryRunLabel')}</div>
          <div className="text-xs text-amber-700">{t('dryRunDesc')}</div>
        </div>
      </label>

      <div className="flex items-start gap-3 p-4 bg-red-50 border border-red-200 rounded-lg">
        <Shield size={16} className="text-red-600 mt-0.5 shrink-0" />
        <p className="text-xs text-red-700">{t('dataLossWarning')}</p>
      </div>

      <div className="flex justify-between">
        <button
          onClick={onBack}
          className="px-5 py-2 text-sm border border-slate-300 rounded-lg hover:bg-slate-50"
        >
          {t('back')}
        </button>
        <button
          onClick={onExecute}
          className={`px-5 py-2 text-sm rounded-lg text-white ${
            dryRun ? 'bg-amber-600 hover:bg-amber-700' : 'bg-red-600 hover:bg-red-700'
          }`}
        >
          {dryRun ? t('executeDryRun') : t('executeRecovery')}
        </button>
      </div>
    </div>
  );
}

function ConfirmRow({ label, value }: { label: string; value: string }) {
  return (
    <div className="flex px-4 py-3 gap-4">
      <span className="text-sm text-slate-500 w-36 shrink-0">{label}</span>
      <span className="text-sm font-medium text-slate-800">{value}</span>
    </div>
  );
}

// ── Step 4: Executing ─────────────────────────────────────────────────────────

function Step4Executing({
  isRunning,
  isError,
  t,
}: {
  isRunning: boolean;
  isError: boolean;
  t: (k: string) => string;
}) {
  return (
    <div className="flex flex-col items-center justify-center gap-4 py-16">
      {isError ? (
        <>
          <XCircle size={48} className="text-red-500" />
          <p className="text-slate-700 font-medium">{t('executionError')}</p>
        </>
      ) : isRunning ? (
        <>
          <Clock size={48} className="text-indigo-500 animate-pulse" />
          <p className="text-slate-700 font-medium">{t('executing')}</p>
          <p className="text-xs text-slate-400">{t('executingNote')}</p>
        </>
      ) : (
        <>
          <CheckCircle size={48} className="text-emerald-500" />
          <p className="text-slate-700 font-medium">{t('executionDone')}</p>
        </>
      )}
    </div>
  );
}

// ── Step 5: Verify ────────────────────────────────────────────────────────────

function Step5Verify({
  result,
  onRestart,
  t,
}: {
  result: RecoveryResponse;
  onRestart: () => void;
  t: (k: string) => string;
}) {
  const isSuccess = result.status === 'Completed' || result.status === 'Pending';
  return (
    <div className="space-y-4">
      <h2 className="text-lg font-medium text-slate-800">{t('step5Title')}</h2>

      <div
        className={`flex items-start gap-3 p-4 rounded-lg border ${
          isSuccess ? 'bg-emerald-50 border-emerald-200' : 'bg-red-50 border-red-200'
        }`}
      >
        {isSuccess ? (
          <CheckCircle size={20} className="text-emerald-600 mt-0.5" />
        ) : (
          <XCircle size={20} className="text-red-600 mt-0.5" />
        )}
        <div>
          <p className={`font-medium ${isSuccess ? 'text-emerald-800' : 'text-red-800'}`}>
            {result.dryRun ? t('dryRunComplete') : t('recoveryComplete')}
          </p>
          <p className="text-xs mt-1 text-slate-600">{t('recoveryId')}: {result.recoveryId}</p>
          {result.dataLossWarning && (
            <p className="text-xs mt-1 text-amber-700">{result.dataLossWarning}</p>
          )}
        </div>
      </div>

      <div className="border border-slate-200 rounded-lg divide-y divide-slate-100">
        <ConfirmRow label={t('status')} value={result.status} />
        <ConfirmRow label={t('dryRunLabel')} value={result.dryRun ? t('yes') : t('no')} />
        <ConfirmRow label={t('schemasAffected')} value={result.schemasAffected.join(', ')} />
        {result.estimatedDurationSeconds != null && (
          <ConfirmRow
            label={t('estimatedDuration')}
            value={`${result.estimatedDurationSeconds}s`}
          />
        )}
      </div>

      <div className="flex justify-end">
        <button
          onClick={onRestart}
          className="px-5 py-2 text-sm border border-slate-300 rounded-lg hover:bg-slate-50"
        >
          {t('startOver')}
        </button>
      </div>
    </div>
  );
}
