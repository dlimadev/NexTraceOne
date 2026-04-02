import { useEffect, useMemo, useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { ArrowUpCircle, CheckCircle, XCircle, Plus } from 'lucide-react';
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

export function PromotionPage() {
  const { t } = useTranslation();
  const { availableEnvironments } = useEnvironment();
  const queryClient = useQueryClient();
  const [showForm, setShowForm] = useState(false);
  const [form, setForm] = useState<CreateForm>(emptyForm);

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

  const availableReleases = releasesQuery.data?.items ?? [];

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

  const requests = data?.items ?? [];
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
      setForm((current) => ({ ...current, sourceEnvironment: source, targetEnvironment: target }));
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
                          onClick={() => promoteMutation.mutate(req.id)}
                          loading={promoteMutation.isPending}
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
                  {/* Gate results */}
                  {req.gateResults.length > 0 && (
                    <ul className="mt-3 space-y-1">
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
    </PageContainer>
  );
}
