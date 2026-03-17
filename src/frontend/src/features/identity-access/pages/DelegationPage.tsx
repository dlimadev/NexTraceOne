import { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { useTranslation } from 'react-i18next';
import { UserCheck, RefreshCw } from 'lucide-react';
import { Card, CardHeader, CardBody } from '../../../components/Card';
import { Button } from '../../../components/Button';
import { Badge } from '../../../components/Badge';
import { identityApi } from '../api';
import { useAuth } from '../../../contexts/AuthContext';
import type { DelegationInfo } from '../../../types';
import { PageContainer } from '../../../components/shell';

/**
 * Página de gestão de delegações de permissões do módulo Identity.
 *
 * Permite visualizar todas as delegações existentes, criar novas delegações
 * (com delegatário, lista de permissões, motivo e período de validade),
 * e revogar delegações ativas.
 *
 * O status de cada delegação é exibido com Badge colorido (Active, Expired, Revoked).
 * Todos os textos são resolvidos via i18n (chaves em identity.delegation.* e common.*).
 */
export function DelegationPage() {
  const { t } = useTranslation();
  const queryClient = useQueryClient();
  const { tenantId: authTenantId } = useAuth();
  const tenantId = authTenantId ?? '';
  const [showForm, setShowForm] = useState(false);
  const [form, setForm] = useState({
    delegateeId: '',
    permissions: '',
    reason: '',
    validFrom: '',
    validUntil: '',
  });

  /** Consulta as delegações existentes. */
  const { data: delegations, isLoading } = useQuery({
    queryKey: ['delegations', tenantId],
    queryFn: () => identityApi.listDelegations(),
    enabled: !!tenantId,
  });

  /** Mutação para criar nova delegação com permissões, motivo e período de validade. */
  const createMutation = useMutation({
    mutationFn: () =>
      identityApi.createDelegation({
        delegateeId: form.delegateeId,
        permissions: form.permissions.split(',').map((p) => p.trim()).filter(Boolean),
        reason: form.reason,
        validFrom: form.validFrom,
        validUntil: form.validUntil,
      }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['delegations'] });
      setShowForm(false);
      setForm({ delegateeId: '', permissions: '', reason: '', validFrom: '', validUntil: '' });
    },
  });

  /** Mutação para revogar uma delegação ativa. */
  const revokeMutation = useMutation({
    mutationFn: (id: string) => identityApi.revokeDelegation(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['delegations'] });
    },
  });

  /** Mapeia o status da delegação para a variante de Badge adequada. */
  const statusVariant = (status: DelegationInfo['status']) => {
    switch (status) {
      case 'Active': return 'success' as const;
      case 'Expired': return 'default' as const;
      case 'Revoked': return 'danger' as const;
    }
  };

  return (
    <PageContainer>
      <div className="flex items-center justify-between mb-6">
        <div>
          <h1 className="text-2xl font-bold text-heading">{t('identity.delegation.title')}</h1>
          <p className="text-muted mt-1">{t('identity.delegation.subtitle')}</p>
        </div>
        <Button onClick={() => setShowForm((v) => !v)}>
          <UserCheck size={16} /> {t('identity.delegation.create')}
        </Button>
      </div>

      {/* Formulário de criação de delegação */}
      {showForm && (
        <Card className="mb-6">
          <CardHeader>
            <h2 className="font-semibold text-heading">{t('identity.delegation.createFormTitle')}</h2>
          </CardHeader>
          <CardBody>
            <form
              onSubmit={(e) => { e.preventDefault(); createMutation.mutate(); }}
              className="grid grid-cols-1 md:grid-cols-2 gap-4"
            >
              <div>
                <label className="block text-sm font-medium text-body mb-1">
                  {t('identity.delegation.delegateeId')}
                </label>
                <input
                  type="text"
                  value={form.delegateeId}
                  onChange={(e) => setForm((f) => ({ ...f, delegateeId: e.target.value }))}
                  required
                  placeholder={t('identity.delegation.delegateeIdPlaceholder')}
                  className="w-full rounded-md bg-canvas border border-edge px-3 py-2 text-sm text-heading placeholder:text-muted focus:outline-none focus:ring-2 focus:ring-accent focus:border-accent transition-colors"
                />
              </div>
              <div>
                <label className="block text-sm font-medium text-body mb-1">
                  {t('identity.delegation.permissions')}
                </label>
                <input
                  type="text"
                  value={form.permissions}
                  onChange={(e) => setForm((f) => ({ ...f, permissions: e.target.value }))}
                  required
                  placeholder={t('identity.delegation.permissionsPlaceholder')}
                  className="w-full rounded-md bg-canvas border border-edge px-3 py-2 text-sm text-heading placeholder:text-muted focus:outline-none focus:ring-2 focus:ring-accent focus:border-accent transition-colors"
                />
              </div>
              <div>
                <label className="block text-sm font-medium text-body mb-1">
                  {t('identity.delegation.reason')}
                </label>
                <input
                  type="text"
                  value={form.reason}
                  onChange={(e) => setForm((f) => ({ ...f, reason: e.target.value }))}
                  required
                  maxLength={500}
                  placeholder={t('identity.delegation.reasonPlaceholder')}
                  className="w-full rounded-md bg-canvas border border-edge px-3 py-2 text-sm text-heading placeholder:text-muted focus:outline-none focus:ring-2 focus:ring-accent focus:border-accent transition-colors"
                />
              </div>
              <div className="grid grid-cols-2 gap-4">
                <div>
                  <label className="block text-sm font-medium text-body mb-1">
                    {t('identity.delegation.validFrom')}
                  </label>
                  <input
                    type="datetime-local"
                    value={form.validFrom}
                    onChange={(e) => setForm((f) => ({ ...f, validFrom: e.target.value }))}
                    required
                    className="w-full rounded-md bg-canvas border border-edge px-3 py-2 text-sm text-heading focus:outline-none focus:ring-2 focus:ring-accent focus:border-accent transition-colors"
                  />
                </div>
                <div>
                  <label className="block text-sm font-medium text-body mb-1">
                    {t('identity.delegation.validUntil')}
                  </label>
                  <input
                    type="datetime-local"
                    value={form.validUntil}
                    onChange={(e) => setForm((f) => ({ ...f, validUntil: e.target.value }))}
                    required
                    className="w-full rounded-md bg-canvas border border-edge px-3 py-2 text-sm text-heading focus:outline-none focus:ring-2 focus:ring-accent focus:border-accent transition-colors"
                  />
                </div>
              </div>
              <div className="md:col-span-2 flex gap-2 justify-end">
                <Button variant="secondary" type="button" onClick={() => setShowForm(false)}>
                  {t('common.cancel')}
                </Button>
                <Button type="submit" loading={createMutation.isPending}>
                  {t('common.create')}
                </Button>
              </div>
            </form>
          </CardBody>
        </Card>
      )}

      {/* Tabela de delegações */}
      <Card>
        <CardHeader>
          <h2 className="text-base font-semibold text-heading">{t('identity.delegation.delegationsList')}</h2>
        </CardHeader>
        <div className="overflow-x-auto">
          {isLoading ? (
            <div className="flex items-center justify-center py-12">
              <RefreshCw size={20} className="animate-spin text-muted" />
            </div>
          ) : !delegations?.length ? (
            <p className="px-6 py-12 text-sm text-muted text-center">
              {t('identity.delegation.noDelegations')}
            </p>
          ) : (
            <table className="min-w-full text-sm">
              <thead>
                <tr className="border-b border-edge bg-panel text-left">
                  <th className="px-6 py-3 font-medium text-muted">{t('identity.delegation.delegateeId')}</th>
                  <th className="px-6 py-3 font-medium text-muted">{t('identity.delegation.permissions')}</th>
                  <th className="px-6 py-3 font-medium text-muted">{t('identity.delegation.reason')}</th>
                  <th className="px-6 py-3 font-medium text-muted">{t('identity.delegation.status')}</th>
                  <th className="px-6 py-3 font-medium text-muted">{t('identity.delegation.validFrom')}</th>
                  <th className="px-6 py-3 font-medium text-muted">{t('identity.delegation.validUntil')}</th>
                  <th className="px-6 py-3 font-medium text-muted">{t('common.actions')}</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-edge">
                {delegations.map((del) => (
                  <tr key={del.id} className="hover:bg-hover transition-colors">
                    <td className="px-6 py-3 text-body font-mono text-xs">{del.delegateeId}</td>
                    <td className="px-6 py-3">
                      <div className="flex flex-wrap gap-1">
                        {del.permissions.map((perm) => (
                          <Badge key={perm} variant="info">{perm}</Badge>
                        ))}
                      </div>
                    </td>
                    <td className="px-6 py-3 text-body max-w-xs truncate">{del.reason}</td>
                    <td className="px-6 py-3">
                      <Badge variant={statusVariant(del.status)}>{del.status}</Badge>
                    </td>
                    <td className="px-6 py-3 text-body">
                      {new Date(del.validFrom).toLocaleString()}
                    </td>
                    <td className="px-6 py-3 text-body">
                      {new Date(del.validUntil).toLocaleString()}
                    </td>
                    <td className="px-6 py-3">
                      {del.status === 'Active' && (
                        <Button
                          variant="danger"
                          size="sm"
                          onClick={() => revokeMutation.mutate(del.id)}
                          loading={revokeMutation.isPending}
                        >
                          {t('identity.delegation.revoke')}
                        </Button>
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
