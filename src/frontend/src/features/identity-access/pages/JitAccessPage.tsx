import { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { useTranslation } from 'react-i18next';
import { Clock, RefreshCw } from 'lucide-react';
import { Card, CardHeader, CardBody } from '../../../components/Card';
import { Button } from '../../../components/Button';
import { Badge } from '../../../components/Badge';
import { identityApi } from '../api';
import { useAuth } from '../../../contexts/AuthContext';
import type { JitAccessRequest } from '../../../types';
import { PageContainer } from '../../../components/shell';

/**
 * Página de gestão de acessos temporários (JIT — Just-In-Time) do módulo Identity.
 *
 * Permite solicitar acesso privilegiado temporário com código de permissão, escopo e justificativa.
 * Administradores podem aprovar ou rejeitar solicitações pendentes diretamente nesta página.
 * O status de cada solicitação é exibido com Badge colorido conforme o estado (Pending, Approved, Rejected, etc.).
 *
 * Todos os textos são resolvidos via i18n (chaves em identity.jitAccess.* e common.*).
 */
export function JitAccessPage() {
  const { t } = useTranslation();
  const queryClient = useQueryClient();
  const { tenantId: authTenantId } = useAuth();
  const tenantId = authTenantId ?? '';
  const [showForm, setShowForm] = useState(false);
  const [form, setForm] = useState({ permissionCode: '', scope: '', justification: '' });

  /** Consulta as solicitações JIT pendentes. */
  const { data: requests, isLoading } = useQuery({
    queryKey: ['jit-access', tenantId],
    queryFn: () => identityApi.listPendingJitRequests(),
    enabled: !!tenantId,
  });

  /** Mutação para solicitar acesso JIT com permissão, escopo e justificativa. */
  const requestMutation = useMutation({
    mutationFn: () =>
      identityApi.requestJitAccess(form.permissionCode, form.scope, form.justification),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['jit-access'] });
      setShowForm(false);
      setForm({ permissionCode: '', scope: '', justification: '' });
    },
  });

  /** Mutação para aprovar ou rejeitar uma solicitação JIT. */
  const decideMutation = useMutation({
    mutationFn: ({ id, approve }: { id: string; approve: boolean }) =>
      identityApi.decideJitAccess(id, approve),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['jit-access'] });
    },
  });

  /** Mapeia o status de JIT para a variante de Badge adequada. */
  const statusVariant = (status: JitAccessRequest['status']) => {
    switch (status) {
      case 'Pending': return 'warning' as const;
      case 'Approved': return 'success' as const;
      case 'Rejected': return 'danger' as const;
      case 'Expired': return 'default' as const;
      case 'Revoked': return 'danger' as const;
    }
  };

  return (
    <PageContainer>
      <div className="flex items-center justify-between mb-6">
        <div>
          <h1 className="text-2xl font-bold text-heading">{t('identity.jitAccess.title')}</h1>
          <p className="text-muted mt-1">{t('identity.jitAccess.subtitle')}</p>
        </div>
        <Button onClick={() => setShowForm((v) => !v)}>
          <Clock size={16} /> {t('identity.jitAccess.request')}
        </Button>
      </div>

      {/* Formulário de solicitação de acesso JIT */}
      {showForm && (
        <Card className="mb-6">
          <CardHeader>
            <h2 className="font-semibold text-heading">{t('identity.jitAccess.requestFormTitle')}</h2>
          </CardHeader>
          <CardBody>
            <form
              onSubmit={(e) => { e.preventDefault(); requestMutation.mutate(); }}
              className="grid grid-cols-1 md:grid-cols-3 gap-4"
            >
              <div>
                <label className="block text-sm font-medium text-body mb-1">
                  {t('identity.jitAccess.permissionCode')}
                </label>
                <input
                  type="text"
                  value={form.permissionCode}
                  onChange={(e) => setForm((f) => ({ ...f, permissionCode: e.target.value }))}
                  required
                  maxLength={100}
                  placeholder={t('identity.jitAccess.permissionCodePlaceholder')}
                  className="w-full rounded-md bg-canvas border border-edge px-3 py-2 text-sm text-heading placeholder:text-muted focus:outline-none focus:ring-2 focus:ring-accent focus:border-accent transition-colors"
                />
              </div>
              <div>
                <label className="block text-sm font-medium text-body mb-1">
                  {t('identity.jitAccess.scope')}
                </label>
                <input
                  type="text"
                  value={form.scope}
                  onChange={(e) => setForm((f) => ({ ...f, scope: e.target.value }))}
                  required
                  maxLength={200}
                  placeholder={t('identity.jitAccess.scopePlaceholder')}
                  className="w-full rounded-md bg-canvas border border-edge px-3 py-2 text-sm text-heading placeholder:text-muted focus:outline-none focus:ring-2 focus:ring-accent focus:border-accent transition-colors"
                />
              </div>
              <div>
                <label className="block text-sm font-medium text-body mb-1">
                  {t('identity.jitAccess.justification')}
                </label>
                <input
                  type="text"
                  value={form.justification}
                  onChange={(e) => setForm((f) => ({ ...f, justification: e.target.value }))}
                  required
                  maxLength={500}
                  placeholder={t('identity.jitAccess.justificationPlaceholder')}
                  className="w-full rounded-md bg-canvas border border-edge px-3 py-2 text-sm text-heading placeholder:text-muted focus:outline-none focus:ring-2 focus:ring-accent focus:border-accent transition-colors"
                />
              </div>
              <div className="md:col-span-3 flex gap-2 justify-end">
                <Button variant="secondary" type="button" onClick={() => setShowForm(false)}>
                  {t('common.cancel')}
                </Button>
                <Button type="submit" loading={requestMutation.isPending}>
                  {t('common.create')}
                </Button>
              </div>
            </form>
          </CardBody>
        </Card>
      )}

      {/* Tabela de solicitações JIT pendentes */}
      <Card>
        <CardHeader>
          <h2 className="text-base font-semibold text-heading">{t('identity.jitAccess.pendingList')}</h2>
        </CardHeader>
        <div className="overflow-x-auto">
          {isLoading ? (
            <div className="flex items-center justify-center py-12">
              <RefreshCw size={20} className="animate-spin text-muted" />
            </div>
          ) : !requests?.length ? (
            <p className="px-6 py-12 text-sm text-muted text-center">
              {t('identity.jitAccess.noRequests')}
            </p>
          ) : (
            <table className="min-w-full text-sm">
              <thead>
                <tr className="border-b border-edge bg-panel text-left">
                  <th className="px-6 py-3 font-medium text-muted">{t('identity.jitAccess.permissionCode')}</th>
                  <th className="px-6 py-3 font-medium text-muted">{t('identity.jitAccess.scope')}</th>
                  <th className="px-6 py-3 font-medium text-muted">{t('identity.jitAccess.justification')}</th>
                  <th className="px-6 py-3 font-medium text-muted">{t('identity.jitAccess.status')}</th>
                  <th className="px-6 py-3 font-medium text-muted">{t('identity.jitAccess.approvalDeadline')}</th>
                  <th className="px-6 py-3 font-medium text-muted">{t('common.actions')}</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-edge">
                {requests.map((req) => (
                  <tr key={req.id} className="hover:bg-hover transition-colors">
                    <td className="px-6 py-3 text-body font-mono text-xs">{req.permissionCode}</td>
                    <td className="px-6 py-3 text-body">{req.scope}</td>
                    <td className="px-6 py-3 text-body max-w-xs truncate">{req.justification}</td>
                    <td className="px-6 py-3">
                      <Badge variant={statusVariant(req.status)}>{req.status}</Badge>
                    </td>
                    <td className="px-6 py-3 text-body">
                      {new Date(req.approvalDeadline).toLocaleString()}
                    </td>
                    <td className="px-6 py-3">
                      {req.status === 'Pending' && (
                        <div className="flex gap-2">
                          <Button
                            size="sm"
                            onClick={() => decideMutation.mutate({ id: req.id, approve: true })}
                            loading={decideMutation.isPending}
                          >
                            {t('identity.jitAccess.approve')}
                          </Button>
                          <Button
                            variant="danger"
                            size="sm"
                            onClick={() => decideMutation.mutate({ id: req.id, approve: false })}
                            loading={decideMutation.isPending}
                          >
                            {t('identity.jitAccess.reject')}
                          </Button>
                        </div>
                      )}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          )}
        </div>
      </Card>
    </PageContainer>
  );
}
