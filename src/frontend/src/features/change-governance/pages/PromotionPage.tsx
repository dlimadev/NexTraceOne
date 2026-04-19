import { useEffect, useMemo, useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { ArrowUpCircle, CheckCircle, XCircle, Plus, ChevronDown, ChevronUp, ShieldAlert, AlertTriangle, Ban, ClipboardList } from 'lucide-react';
import { useTranslation } from 'react-i18next';
import { Card, CardHeader, CardBody } from '../../../components/Card';
import { Button } from '../../../components/Button';
import { Badge } from '../../../components/Badge';
import { PageLoadingState } from '../../../components/PageLoadingState';
import { PageErrorState } from '../../../components/PageErrorState';
import { promotionApi, changeIntelligenceApi } from '../api';
import type { PromotionRequest } from '../../../types';
import { PageContainer } from '../../../components/shell';
import { PageHeader } from '../../../components/PageHeader';
import { useEnvironment } from '../../../contexts/EnvironmentContext';
import { finOpsApi, type EvaluateReleaseBudgetGateResponse } from '../../governance/api/finOps';

type PromotionStatus = PromotionRequest['status'];

function statusVariant(status: PromotionStatus): 'default' | 'success' | 'warning' | 'danger' | 'info' {
  if (status === 'Promoted') return 'success';
  if (status === 'Rejected') return 'danger';
  if (status === 'Approved') return 'info';
  return 'default';
}

interface CreateForm {
  releaseId: string;
  sourceEnvironment: string;
  targetEnvironment: string;
}

const emptyForm: CreateForm = {
  releaseId: '',
  sourceEnvironment: '',
  targetEnvironment: '',
};

function environmentColor(profile?: string): string {
  if (profile === 'production') return 'bg-critical';
  if (profile === 'staging') return 'bg-warning';
  if (profile === 'development' || profile === 'qa' || profile === 'sandbox' || profile === 'uat') return 'bg-success';
  return 'bg-muted';
}

function translateEnvironment(t: (key: string) => string, env: string): string {
  const normalized = env.toLowerCase();
  const key = `releases.environments.${normalized}`;
  const translated = t(key);
  return translated === key ? env : translated;
}

// ─── Gate Evaluation Breakdown ───────────────────────────────────────────────

function GateEvaluationBreakdown({
  requestId,
  onOverride,
}: {
  requestId: string;
  onOverride: (evaluationId: string) => void;
}) {
  const { t } = useTranslation();
  const { data, isLoading } = useQuery({
    queryKey: ['gate-evaluations', requestId],
    queryFn: () => promotionApi.getGateEvaluations(requestId),
    staleTime: 15_000,
  });

  if (isLoading) return <p className="text-xs text-muted py-2">{t('common.loading')}</p>;
  if (!data || data.evaluations.length === 0) {
    return <p className="text-xs text-muted py-2">{t('promotion.noGateEvaluations')}</p>;
  }

  return (
    <ul className="mt-2 space-y-2">
      {data.evaluations.map((ev) => (
        <li
          key={ev.evaluationId}
          className="p-3 rounded-md border border-edge bg-elevated flex items-start gap-3"
        >
          {ev.passed ? (
            <CheckCircle size={14} className="text-success mt-0.5 shrink-0" />
          ) : (
            <XCircle size={14} className="text-critical mt-0.5 shrink-0" />
          )}
          <div className="flex-1">
            <div className="flex items-center gap-2">
              <span className="text-xs font-mono text-muted">{ev.gateId.slice(0, 8)}…</span>
              <span className="text-xs text-body">{ev.evaluatedBy}</span>
              <span className="text-xs text-faded">{new Date(ev.evaluatedAt).toLocaleString()}</span>
            </div>
            {ev.details && <p className="text-xs text-body mt-0.5">{ev.details}</p>}
            {ev.overrideJustification && (
              <p className="text-xs text-warning mt-0.5">
                <ShieldAlert size={10} className="inline mr-1" />
                {t('promotion.overrideWith')}: {ev.overrideJustification}
              </p>
            )}
          </div>
          {!ev.passed && !ev.overrideJustification && (
            <button
              type="button"
              onClick={() => onOverride(ev.evaluationId)}
              className="text-xs text-accent hover:underline shrink-0"
            >
              {t('promotion.override')}
            </button>
          )}
        </li>
      ))}
    </ul>
  );
}

export function PromotionPage() {
  const { t } = useTranslation();
  const { availableEnvironments } = useEnvironment();
  const queryClient = useQueryClient();
  const [showForm, setShowForm] = useState(false);
  const [form, setForm] = useState<CreateForm>(emptyForm);
  const [expandedGates, setExpandedGates] = useState<Set<string>>(new Set());
  const [overrideTarget, setOverrideTarget] = useState<{ evaluationId: string; requestId: string } | null>(null);
  const [overrideJustification, setOverrideJustification] = useState('');

  // ── Budget Gate state ───────────────────────────────────────────────────────
  type BudgetGateDialogState =
    | { kind: 'warn'; requestId: string; evaluation: EvaluateReleaseBudgetGateResponse }
    | { kind: 'block'; evaluation: EvaluateReleaseBudgetGateResponse }
    | { kind: 'approval_sent'; approvalId: string; evaluation: EvaluateReleaseBudgetGateResponse }
    | null;
  const [budgetGateDialog, setBudgetGateDialog] = useState<BudgetGateDialogState>(null);

  const evaluateBudgetGateMutation = useMutation({
    mutationFn: finOpsApi.evaluateReleaseBudgetGate,
  });

  const createBudgetApprovalMutation = useMutation({
    mutationFn: finOpsApi.createBudgetApproval,
  });

  /** Called when user clicks "Promote". Evaluates the FinOps budget gate first. */
  const handlePromote = async (req: PromotionRequest) => {
    // Use a best-effort evaluation: if no cost data is available (baseline = 0, actual = 0)
    // the backend will return Allow (no meaningful data to gate on).
    let gateResult: EvaluateReleaseBudgetGateResponse | null = null;
    try {
      gateResult = await evaluateBudgetGateMutation.mutateAsync({
        releaseId: req.releaseId,
        serviceName: req.serviceName ?? req.releaseId,
        environment: req.targetEnvironment,
        actualCostPerDay: 0,    // placeholder; real data comes from telemetry/FinOps context
        baselineCostPerDay: 0,
        measurementDays: 7,
      });
    } catch {
      // Gate evaluation failed → allow promotion (fail-open for resilience)
      promoteMutation.mutate(req.id);
      return;
    }

    switch (gateResult.action) {
      case 'Allow':
        promoteMutation.mutate(req.id);
        break;
      case 'Warn':
        setBudgetGateDialog({ kind: 'warn', requestId: req.id, evaluation: gateResult });
        break;
      case 'Block':
        setBudgetGateDialog({ kind: 'block', evaluation: gateResult });
        break;
      case 'RequireApproval': {
        // Auto-create a budget approval request
        try {
          const approval = await createBudgetApprovalMutation.mutateAsync({
            releaseId: req.releaseId,
            serviceName: req.serviceName ?? req.releaseId,
            environment: req.targetEnvironment,
            actualCost: gateResult.actualTotalCost,
            baselineCost: gateResult.baselineTotalCost,
            costDeltaPct: gateResult.costDeltaPct,
            currency: gateResult.currency,
            requestedBy: 'current-user',
            justification: t('promotion.budgetGate.autoJustification'),
          }) as { approvalId: string };
          setBudgetGateDialog({ kind: 'approval_sent', approvalId: approval.approvalId, evaluation: gateResult });
        } catch {
          // Fall back to block dialog if approval creation fails
          setBudgetGateDialog({ kind: 'block', evaluation: gateResult });
        }
        break;
      }
    }
  };

  const { data, isLoading, isError } = useQuery({
    queryKey: ['promotion', 'requests'],
    queryFn: () => promotionApi.listRequests(1, 20),
    staleTime: 15_000,
  });

  const releasesQuery = useQuery({
    queryKey: ['releases', 'recent'],
    queryFn: () => changeIntelligenceApi.listRecentReleases(1, 50),
    staleTime: 30_000,
  });

  const availableReleases = useMemo(() => releasesQuery.data?.items ?? [], [releasesQuery.data?.items]);

  const createMutation = useMutation({
    mutationFn: promotionApi.createRequest,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['promotion'] });
      setShowForm(false);
      setForm(emptyForm);
    },
  });

  const promoteMutation = useMutation({
    mutationFn: (requestId: string) => promotionApi.promote(requestId),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['promotion'] }),
  });

  const rejectMutation = useMutation({
    mutationFn: ({ id, reason }: { id: string; reason: string }) =>
      promotionApi.reject(id, reason),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['promotion'] }),
  });

  const overrideMutation = useMutation({
    mutationFn: ({ gateEvaluationId, justification }: { gateEvaluationId: string; justification: string }) =>
      promotionApi.overrideGate(gateEvaluationId, justification),
    onSuccess: (_, vars) => {
      queryClient.invalidateQueries({ queryKey: ['promotion'] });
      if (overrideTarget) {
        queryClient.invalidateQueries({ queryKey: ['gate-evaluations', overrideTarget.requestId] });
      }
      setOverrideTarget(null);
      setOverrideJustification('');
    },
  });

  const toggleGates = (requestId: string) => {
    setExpandedGates((prev) => {
      const next = new Set(prev);
      if (next.has(requestId)) {
        next.delete(requestId);
      } else {
        next.add(requestId);
      }
      return next;
    });
  };

  const requests = useMemo(() => data?.items ?? [], [data?.items]);
  const pending = requests.filter((r) => r.status === 'Pending' || r.status === 'Approved');

  const environmentProfiles = useMemo(() => {
    const map = new Map<string, string>();
    for (const env of availableEnvironments) {
      map.set(env.name.toLowerCase(), env.profile);
    }
    return map;
  }, [availableEnvironments]);

  const environmentOptions = useMemo(() => {
    const set = new Set<string>();

    for (const env of availableEnvironments) {
      if (env.name) set.add(env.name);
    }

    for (const req of requests) {
      if (req.sourceEnvironment) set.add(req.sourceEnvironment);
      if (req.targetEnvironment) set.add(req.targetEnvironment);
    }

    for (const rel of availableReleases) {
      if (rel.environment) set.add(rel.environment);
    }

    const list = Array.from(set);
    return list.sort((a, b) => a.localeCompare(b));
  }, [availableEnvironments, requests, availableReleases]);

  useEffect(() => {
    if (!environmentOptions.length) return;

    const source = environmentOptions.includes(form.sourceEnvironment)
      ? form.sourceEnvironment
      : environmentOptions[0];
    const target = environmentOptions.includes(form.targetEnvironment)
      ? form.targetEnvironment
      : (environmentOptions.find((env) => env !== source) ?? source);

    if (source !== form.sourceEnvironment || target !== form.targetEnvironment) {
      // eslint-disable-next-line react-hooks/set-state-in-effect
      setForm((current) => ({ ...current, sourceEnvironment: source ?? '', targetEnvironment: target ?? '' }));
    }
  }, [environmentOptions, form.sourceEnvironment, form.targetEnvironment]);

  return (
    <PageContainer>
      <PageHeader
        title={t('promotion.title')}
        subtitle={t('promotion.subtitle')}
        actions={
          <Button onClick={() => setShowForm((v) => !v)}>
            <Plus size={16} /> {t('promotion.newRequest')}
          </Button>
        }
      />

      {/* Create Form */}
      {showForm && (
        <Card className="mb-6">
          <CardHeader>
            <h2 className="font-semibold text-heading">{t('promotion.createRequest')}</h2>
          </CardHeader>
          <CardBody>
            <form
              onSubmit={(e) => {
                e.preventDefault();
                createMutation.mutate(form);
              }}
              className="grid grid-cols-3 gap-4"
            >
              <div>
                <label className="block text-sm font-medium text-body mb-1">{t('promotion.releaseId')}</label>
                <select
                  value={form.releaseId}
                  onChange={(e) => setForm((f) => ({ ...f, releaseId: e.target.value }))}
                  required
                  className="w-full rounded-md bg-canvas border border-edge px-3 py-2 text-sm text-heading focus:outline-none focus:ring-2 focus:ring-accent focus:border-accent transition-colors"
                >
                  <option value="">{t('promotion.selectRelease')}</option>
                  {availableReleases.map((rel) => (
                    <option key={rel.id} value={rel.id}>
                      {rel.apiAssetId} — v{rel.version} ({rel.environment})
                    </option>
                  ))}
                </select>
              </div>
              <div>
                <label className="block text-sm font-medium text-body mb-1">{t('promotion.sourceEnvironment')}</label>
                <select
                  value={form.sourceEnvironment}
                  onChange={(e) => setForm((f) => ({ ...f, sourceEnvironment: e.target.value }))}
                  className="w-full rounded-md bg-canvas border border-edge px-3 py-2 text-sm text-heading focus:outline-none focus:ring-2 focus:ring-accent focus:border-accent transition-colors"
                >
                  {environmentOptions.map((env) => (
                    <option key={env} value={env}>{translateEnvironment(t, env)}</option>
                  ))}
                </select>
              </div>
              <div>
                <label className="block text-sm font-medium text-body mb-1">{t('promotion.targetEnvironment')}</label>
                <select
                  value={form.targetEnvironment}
                  onChange={(e) => setForm((f) => ({ ...f, targetEnvironment: e.target.value }))}
                  className="w-full rounded-md bg-canvas border border-edge px-3 py-2 text-sm text-heading focus:outline-none focus:ring-2 focus:ring-accent focus:border-accent transition-colors"
                >
                  {environmentOptions.map((env) => (
                    <option key={env} value={env}>{translateEnvironment(t, env)}</option>
                  ))}
                </select>
              </div>
              <div className="col-span-3 flex gap-2 justify-end">
                <Button variant="secondary" type="button" onClick={() => setShowForm(false)}>
                  {t('common.cancel')}
                </Button>
                <Button type="submit" loading={createMutation.isPending}>
                  {t('promotion.createButton')}
                </Button>
              </div>
            </form>
          </CardBody>
        </Card>
      )}

      {/* Environment pipeline */}
      <Card className="mb-6">
        <CardHeader>
          <h2 className="font-semibold text-heading">{t('promotion.environmentPipeline')}</h2>
        </CardHeader>
        <CardBody>
          <div className="flex items-center gap-4">
            {environmentOptions.map((env, i) => (
              <div key={env} className="flex items-center gap-4">
                <div className="flex flex-col items-center gap-2">
                  <div className={`w-3 h-3 rounded-full ${environmentColor(environmentProfiles.get(env.toLowerCase()))}`} />
                  <span className="text-sm font-medium text-body">{translateEnvironment(t, env)}</span>
                </div>
                {i < environmentOptions.length - 1 && (
                  <ArrowUpCircle size={20} className="text-edge-strong rotate-90" />
                )}
              </div>
            ))}
          </div>
        </CardBody>
      </Card>

      {/* Pending requests */}
      {pending.length > 0 && (
        <Card className="mb-6">
          <CardHeader>
            <h2 className="font-semibold text-heading">{t('promotion.pendingRequests')}</h2>
          </CardHeader>
          <CardBody className="p-0">
            <ul className="divide-y divide-edge">
              {pending.map((req) => (
                <li key={req.id} className="px-6 py-4">
                  <div className="flex items-center justify-between gap-3">
                    <div>
                      <p className="text-sm font-medium text-heading capitalize">
                        {req.sourceEnvironment} → {req.targetEnvironment}
                      </p>
                      <p className="text-xs text-faded font-mono mt-0.5">
                        Release: {req.releaseId.slice(0, 8)}…
                      </p>
                    </div>
                    <div className="flex items-center gap-2">
                      <Badge variant={statusVariant(req.status)}>{req.status}</Badge>
                      {req.status === 'Approved' && (
                        <Button
                          onClick={() => handlePromote(req)}
                          loading={promoteMutation.isPending || evaluateBudgetGateMutation.isPending}
                        >
                          {t('promotion.promote')}
                        </Button>
                      )}
                      <Button
                        variant="danger"
                        onClick={() => rejectMutation.mutate({ id: req.id, reason: t('promotion.rejectedViaUi') })}
                        loading={rejectMutation.isPending}
                      >
                        {t('workflow.reject')}
                      </Button>
                    </div>
                  </div>
                  {/* Gate results summary + expand for detailed breakdown */}
                  <div className="mt-3">
                    {req.gateResults.length > 0 && (
                      <ul className="space-y-1 mb-2">
                        {req.gateResults.map((gate) => (
                          <li key={gate.gateName} className="flex items-center gap-2">
                            {gate.passed ? (
                              <CheckCircle size={12} className="text-success shrink-0" />
                            ) : (
                              <XCircle size={12} className="text-critical shrink-0" />
                            )}
                            <span className="text-xs text-body">{gate.gateName}</span>
                            {gate.message && (
                              <span className="text-xs text-muted">— {gate.message}</span>
                            )}
                          </li>
                        ))}
                      </ul>
                    )}
                    <button
                      type="button"
                      onClick={() => toggleGates(req.id)}
                      className="flex items-center gap-1 text-xs text-accent hover:underline"
                    >
                      {expandedGates.has(req.id) ? <ChevronUp size={12} /> : <ChevronDown size={12} />}
                      {expandedGates.has(req.id) ? t('promotion.hideGateDetails') : t('promotion.showGateDetails')}
                    </button>
                    {expandedGates.has(req.id) && (
                      <GateEvaluationBreakdown
                        requestId={req.id}
                        onOverride={(evalId) => {
                          setOverrideTarget({ evaluationId: evalId, requestId: req.id });
                          setOverrideJustification('');
                        }}
                      />
                    )}
                  </div>
                </li>
              ))}
            </ul>
          </CardBody>
        </Card>
      )}

      {/* All Requests Table */}
      <Card>
        <CardHeader>
          <div className="flex items-center justify-between">
            <h2 className="font-semibold text-heading">{t('promotion.allRequests')}</h2>
            {data && (
              <span className="text-sm text-muted">{data.totalCount} {t('common.total')}</span>
            )}
          </div>
        </CardHeader>
        <div className="overflow-x-auto">
          {isLoading ? (
            <PageLoadingState />
          ) : isError ? (
            <PageErrorState message={t('promotion.loadFailed')} />
          ) : !requests.length ? (
            <p className="px-6 py-12 text-sm text-muted text-center">
              {t('promotion.noRequests')}
            </p>
          ) : (
            <table className="min-w-full text-sm">
              <thead>
                <tr className="border-b border-edge bg-panel text-left">
                  <th className="px-6 py-3 font-medium text-muted">{t('promotion.route')}</th>
                  <th className="px-6 py-3 font-medium text-muted">{t('promotion.release')}</th>
                  <th className="px-6 py-3 font-medium text-muted">{t('promotion.status')}</th>
                  <th className="px-6 py-3 font-medium text-muted">{t('promotion.gates')}</th>
                  <th className="px-6 py-3 font-medium text-muted">{t('promotion.created')}</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-edge">
                {requests.map((req) => {
                  const passed = req.gateResults.filter((g) => g.passed).length;
                  const total = req.gateResults.length;
                  return (
                    <tr key={req.id} className="hover:bg-hover transition-colors">
                      <td className="px-6 py-3 text-body capitalize">
                        {req.sourceEnvironment} → {req.targetEnvironment}
                      </td>
                      <td className="px-6 py-3 font-mono text-xs text-muted">
                        {req.releaseId.slice(0, 8)}…
                      </td>
                      <td className="px-6 py-3">
                        <Badge variant={statusVariant(req.status)}>{req.status}</Badge>
                      </td>
                      <td className="px-6 py-3 text-sm text-body">
                        {total > 0 ? t('promotion.gatesPassed', { passed, total }) : '—'}
                      </td>
                      <td className="px-6 py-3 text-xs text-muted">
                        {new Date(req.createdAt).toLocaleString()}
                      </td>
                    </tr>
                  );
                })}
              </tbody>
            </table>
          )}
        </div>
      </Card>

      {/* Override Gate Dialog */}
      {overrideTarget && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50">
          <div className="bg-canvas rounded-lg border border-edge shadow-xl p-6 w-full max-w-md">
            <h2 className="text-base font-semibold text-heading mb-1">
              {t('promotion.overrideGateTitle')}
            </h2>
            <p className="text-xs text-muted mb-4">{t('promotion.overrideGateSubtitle')}</p>
            <textarea
              value={overrideJustification}
              onChange={(e) => setOverrideJustification(e.target.value)}
              rows={4}
              placeholder={t('promotion.overrideJustificationPlaceholder')}
              className="w-full rounded-md bg-elevated border border-edge px-3 py-2 text-sm text-heading placeholder:text-muted focus:outline-none focus:ring-2 focus:ring-accent mb-4 resize-none"
            />
            <div className="flex justify-end gap-3">
              <Button
                variant="secondary"
                onClick={() => { setOverrideTarget(null); setOverrideJustification(''); }}
              >
                {t('common.cancel')}
              </Button>
              <Button
                variant="danger"
                loading={overrideMutation.isPending}
                disabled={!overrideJustification.trim()}
                onClick={() =>
                  overrideMutation.mutate({
                    gateEvaluationId: overrideTarget.evaluationId,
                    justification: overrideJustification,
                  })
                }
              >
                {t('promotion.confirmOverride')}
              </Button>
            </div>
          </div>
        </div>
      )}

      {/* Budget Gate — Warn Dialog (proceed with warning) */}
      {budgetGateDialog?.kind === 'warn' && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50" data-testid="budget-gate-warn-dialog">
          <div className="bg-canvas rounded-lg border border-warning shadow-xl p-6 w-full max-w-lg">
            <div className="flex items-center gap-3 mb-3">
              <AlertTriangle size={20} className="text-warning shrink-0" />
              <h2 className="text-base font-semibold text-heading">{t('promotion.budgetGate.warnTitle')}</h2>
            </div>
            <p className="text-sm text-body mb-2">{budgetGateDialog.evaluation.reason}</p>
            <div className="bg-elevated rounded-md px-4 py-3 mb-4 text-xs text-muted space-y-1">
              <div className="flex justify-between">
                <span>{t('promotion.budgetGate.costDelta')}</span>
                <span className="text-warning font-medium">+{budgetGateDialog.evaluation.costDeltaPct.toFixed(1)}%</span>
              </div>
              <div className="flex justify-between">
                <span>{t('promotion.budgetGate.environment')}</span>
                <span className="text-heading">{budgetGateDialog.evaluation.environment}</span>
              </div>
            </div>
            <p className="text-xs text-muted mb-4">{t('promotion.budgetGate.warnConfirmMessage')}</p>
            <div className="flex justify-end gap-3">
              <Button variant="secondary" onClick={() => setBudgetGateDialog(null)}>
                {t('common.cancel')}
              </Button>
              <Button
                variant="warning"
                onClick={() => {
                  promoteMutation.mutate(budgetGateDialog.requestId);
                  setBudgetGateDialog(null);
                }}
                loading={promoteMutation.isPending}
              >
                {t('promotion.budgetGate.confirmPromoteAnyway')}
              </Button>
            </div>
          </div>
        </div>
      )}

      {/* Budget Gate — Block Dialog (hard block, no approval path) */}
      {budgetGateDialog?.kind === 'block' && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50" data-testid="budget-gate-block-dialog">
          <div className="bg-canvas rounded-lg border border-critical shadow-xl p-6 w-full max-w-lg">
            <div className="flex items-center gap-3 mb-3">
              <Ban size={20} className="text-critical shrink-0" />
              <h2 className="text-base font-semibold text-heading">{t('promotion.budgetGate.blockTitle')}</h2>
            </div>
            <p className="text-sm text-body mb-2">{budgetGateDialog.evaluation.reason}</p>
            <div className="bg-elevated rounded-md px-4 py-3 mb-4 text-xs text-muted space-y-1">
              <div className="flex justify-between">
                <span>{t('promotion.budgetGate.costDelta')}</span>
                <span className="text-critical font-medium">+{budgetGateDialog.evaluation.costDeltaPct.toFixed(1)}%</span>
              </div>
              <div className="flex justify-between">
                <span>{t('promotion.budgetGate.environment')}</span>
                <span className="text-heading">{budgetGateDialog.evaluation.environment}</span>
              </div>
            </div>
            <p className="text-xs text-muted mb-4">{t('promotion.budgetGate.blockMessage')}</p>
            <div className="flex justify-end gap-3">
              <Button variant="secondary" onClick={() => setBudgetGateDialog(null)}>
                {t('common.close')}
              </Button>
              <a href="/governance/finops/configuration">
                <Button variant="default">
                  {t('promotion.budgetGate.goToFinOpsConfig')}
                </Button>
              </a>
            </div>
          </div>
        </div>
      )}

      {/* Budget Gate — RequireApproval Dialog (approval request sent) */}
      {budgetGateDialog?.kind === 'approval_sent' && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50" data-testid="budget-gate-approval-dialog">
          <div className="bg-canvas rounded-lg border border-accent shadow-xl p-6 w-full max-w-lg">
            <div className="flex items-center gap-3 mb-3">
              <ClipboardList size={20} className="text-accent shrink-0" />
              <h2 className="text-base font-semibold text-heading">{t('promotion.budgetGate.approvalSentTitle')}</h2>
            </div>
            <p className="text-sm text-body mb-2">{budgetGateDialog.evaluation.reason}</p>
            <div className="bg-elevated rounded-md px-4 py-3 mb-4 text-xs text-muted space-y-1">
              <div className="flex justify-between">
                <span>{t('promotion.budgetGate.costDelta')}</span>
                <span className="text-warning font-medium">+{budgetGateDialog.evaluation.costDeltaPct.toFixed(1)}%</span>
              </div>
              <div className="flex justify-between">
                <span>{t('promotion.budgetGate.environment')}</span>
                <span className="text-heading">{budgetGateDialog.evaluation.environment}</span>
              </div>
              <div className="flex justify-between">
                <span>{t('promotion.budgetGate.approvalId')}</span>
                <span className="font-mono text-heading">{budgetGateDialog.approvalId.slice(0, 8)}…</span>
              </div>
            </div>
            <p className="text-xs text-muted mb-4">{t('promotion.budgetGate.approvalSentMessage')}</p>
            <div className="flex justify-end gap-3">
              <Button variant="secondary" onClick={() => setBudgetGateDialog(null)}>
                {t('common.close')}
              </Button>
              <a href="/governance/finops/approvals">
                <Button variant="default">
                  {t('promotion.budgetGate.goToApprovals')}
                </Button>
              </a>
            </div>
          </div>
        </div>
      )}
    </PageContainer>
  );
}
