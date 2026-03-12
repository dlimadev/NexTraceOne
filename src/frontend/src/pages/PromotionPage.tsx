import { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { ArrowUpCircle, CheckCircle, XCircle, Plus, RefreshCw } from 'lucide-react';
import { useTranslation } from 'react-i18next';
import { Card, CardHeader, CardBody } from '../components/Card';
import { Button } from '../components/Button';
import { Badge } from '../components/Badge';
import { promotionApi } from '../api';
import type { PromotionRequest } from '../types';

type PromotionStatus = PromotionRequest['status'];

function statusVariant(status: PromotionStatus): 'default' | 'success' | 'warning' | 'danger' | 'info' {
  if (status === 'Promoted') return 'success';
  if (status === 'Rejected') return 'danger';
  if (status === 'Approved') return 'info';
  return 'default';
}

const ENVIRONMENTS = ['development', 'staging', 'production'];
const ENV_COLORS: Record<string, string> = {
  development: 'bg-green-500',
  staging: 'bg-yellow-500',
  production: 'bg-red-500',
};

interface CreateForm {
  releaseId: string;
  sourceEnvironment: string;
  targetEnvironment: string;
}

const emptyForm: CreateForm = {
  releaseId: '',
  sourceEnvironment: 'staging',
  targetEnvironment: 'production',
};

export function PromotionPage() {
  const { t } = useTranslation();
  const queryClient = useQueryClient();
  const [showForm, setShowForm] = useState(false);
  const [form, setForm] = useState<CreateForm>(emptyForm);

  const { data, isLoading, isError } = useQuery({
    queryKey: ['promotion', 'requests'],
    queryFn: () => promotionApi.listRequests(1, 20),
    staleTime: 15_000,
  });

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

  return (
    <div className="p-8">
      <div className="flex items-center justify-between mb-6">
        <div>
          <h1 className="text-2xl font-bold text-gray-900">{t('promotion.title')}</h1>
          <p className="text-gray-500 mt-1">{t('promotion.subtitle')}</p>
        </div>
        <Button onClick={() => setShowForm((v) => !v)}>
          <Plus size={16} /> {t('promotion.newRequest')}
        </Button>
      </div>

      {/* Create Form */}
      {showForm && (
        <Card className="mb-6">
          <CardHeader>
            <h2 className="font-semibold text-gray-800">{t('promotion.createRequest')}</h2>
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
                <label className="block text-sm font-medium text-gray-700 mb-1">{t('promotion.releaseId')}</label>
                <input
                  type="text"
                  value={form.releaseId}
                  onChange={(e) => setForm((f) => ({ ...f, releaseId: e.target.value }))}
                  required
                  placeholder={t('promotion.releaseIdPlaceholder')}
                  className="w-full rounded-md border border-gray-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-indigo-500"
                />
              </div>
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">{t('promotion.sourceEnvironment')}</label>
                <select
                  value={form.sourceEnvironment}
                  onChange={(e) => setForm((f) => ({ ...f, sourceEnvironment: e.target.value }))}
                  className="w-full rounded-md border border-gray-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-indigo-500"
                >
                  {ENVIRONMENTS.map((env) => (
                    <option key={env} value={env}>{env}</option>
                  ))}
                </select>
              </div>
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">{t('promotion.targetEnvironment')}</label>
                <select
                  value={form.targetEnvironment}
                  onChange={(e) => setForm((f) => ({ ...f, targetEnvironment: e.target.value }))}
                  className="w-full rounded-md border border-gray-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-indigo-500"
                >
                  {ENVIRONMENTS.map((env) => (
                    <option key={env} value={env}>{env}</option>
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
          <h2 className="font-semibold text-gray-800">{t('promotion.environmentPipeline')}</h2>
        </CardHeader>
        <CardBody>
          <div className="flex items-center gap-4">
            {ENVIRONMENTS.map((env, i) => (
              <div key={env} className="flex items-center gap-4">
                <div className="flex flex-col items-center gap-2">
                  <div className={`w-3 h-3 rounded-full ${ENV_COLORS[env] ?? 'bg-gray-400'}`} />
                  <span className="text-sm font-medium text-gray-700 capitalize">{env}</span>
                </div>
                {i < ENVIRONMENTS.length - 1 && (
                  <ArrowUpCircle size={20} className="text-gray-300 rotate-90" />
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
            <h2 className="font-semibold text-gray-800">{t('promotion.pendingRequests')}</h2>
          </CardHeader>
          <CardBody className="p-0">
            <ul className="divide-y divide-gray-100">
              {pending.map((req) => (
                <li key={req.id} className="px-6 py-4">
                  <div className="flex items-center justify-between gap-3">
                    <div>
                      <p className="text-sm font-medium text-gray-800 capitalize">
                        {req.sourceEnvironment} → {req.targetEnvironment}
                      </p>
                      <p className="text-xs text-gray-400 font-mono mt-0.5">
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
                        onClick={() => rejectMutation.mutate({ id: req.id, reason: 'Rejected via UI' })}
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
                            <CheckCircle size={12} className="text-green-500 shrink-0" />
                          ) : (
                            <XCircle size={12} className="text-red-400 shrink-0" />
                          )}
                          <span className="text-xs text-gray-600">{gate.gateName}</span>
                          {gate.message && (
                            <span className="text-xs text-gray-400">— {gate.message}</span>
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
            <h2 className="font-semibold text-gray-800">{t('promotion.allRequests')}</h2>
            {data && (
              <span className="text-sm text-gray-500">{data.totalCount} total</span>
            )}
          </div>
        </CardHeader>
        <div className="overflow-x-auto">
          {isLoading ? (
            <div className="flex items-center justify-center py-12">
              <RefreshCw size={20} className="animate-spin text-gray-400" />
            </div>
          ) : isError ? (
            <p className="px-6 py-12 text-sm text-red-500 text-center">
              {t('promotion.loadFailed')}
            </p>
          ) : !requests.length ? (
            <p className="px-6 py-12 text-sm text-gray-400 text-center">
              {t('promotion.noRequests')}
            </p>
          ) : (
            <table className="min-w-full text-sm">
              <thead>
                <tr className="border-b border-gray-200 bg-gray-50 text-left">
                  <th className="px-6 py-3 font-medium text-gray-500">{t('promotion.route')}</th>
                  <th className="px-6 py-3 font-medium text-gray-500">{t('promotion.release')}</th>
                  <th className="px-6 py-3 font-medium text-gray-500">{t('promotion.status')}</th>
                  <th className="px-6 py-3 font-medium text-gray-500">{t('promotion.gates')}</th>
                  <th className="px-6 py-3 font-medium text-gray-500">{t('promotion.created')}</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-gray-100">
                {requests.map((req) => {
                  const passed = req.gateResults.filter((g) => g.passed).length;
                  const total = req.gateResults.length;
                  return (
                    <tr key={req.id} className="hover:bg-gray-50">
                      <td className="px-6 py-3 text-gray-700 capitalize">
                        {req.sourceEnvironment} → {req.targetEnvironment}
                      </td>
                      <td className="px-6 py-3 font-mono text-xs text-gray-500">
                        {req.releaseId.slice(0, 8)}…
                      </td>
                      <td className="px-6 py-3">
                        <Badge variant={statusVariant(req.status)}>{req.status}</Badge>
                      </td>
                      <td className="px-6 py-3 text-sm text-gray-600">
                        {total > 0 ? t('promotion.gatesPassed', { passed, total }) : '—'}
                      </td>
                      <td className="px-6 py-3 text-xs text-gray-500">
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
    </div>
  );
}
