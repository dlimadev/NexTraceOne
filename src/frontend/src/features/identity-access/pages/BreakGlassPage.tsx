import { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { useTranslation } from 'react-i18next';
import { AlertTriangle, RefreshCw } from 'lucide-react';
import { Card, CardHeader, CardBody } from '../../../components/Card';
import { Button } from '../../../components/Button';
import { Badge } from '../../../components/Badge';
import { PageLoadingState } from '../../../components/PageLoadingState';
import { PageErrorState } from '../../../components/PageErrorState';
import { identityApi } from '../api';
import { useAuth } from '../../../contexts/AuthContext';
import type { BreakGlassRequest } from '../../../types';
import { PageContainer } from '../../../components/shell';
import { PageHeader } from '../../../components/PageHeader';

/**
 * Página de gestão de acessos emergenciais (Break Glass) do módulo Identity.
 *
 * Permite visualizar solicitações de acesso emergencial, solicitar novo acesso
 * com justificativa obrigatória, e revogar acessos ativos.
 * O status de cada solicitação é exibido com Badge colorido (Active, Expired, Revoked, PostMortemCompleted).
 *
 * Todos os textos são resolvidos via i18n (chaves em identity.breakGlass.* e common.*).
 * O tenantId é obtido do AuthContext para garantir consistência com o estado de autenticação.
 */
export function BreakGlassPage() {
  const { t } = useTranslation();
  const queryClient = useQueryClient();
  const { tenantId: authTenantId } = useAuth();
  const tenantId = authTenantId ?? '';
  const [showForm, setShowForm] = useState(false);
  const [justification, setJustification] = useState('');

  /** Consulta as solicitações de Break Glass existentes. */
  const { data: requests, isLoading, isError } = useQuery({
    queryKey: ['break-glass', tenantId],
    queryFn: () => identityApi.listBreakGlassRequests(),
    enabled: !!tenantId,
  });

  /** Mutação para solicitar acesso emergencial com justificativa. */
  const requestMutation = useMutation({
    mutationFn: (j: string) => identityApi.requestBreakGlass(j),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['break-glass'] });
      setShowForm(false);
      setJustification('');
    },
  });

  /** Mutação para revogar um acesso emergencial ativo. */
  const revokeMutation = useMutation({
    mutationFn: (id: string) => identityApi.revokeBreakGlass(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['break-glass'] });
    },
  });

  /** Mapeia o status de Break Glass para a variante de Badge adequada. */
  const statusVariant = (status: BreakGlassRequest['status']) => {
    switch (status) {
      case 'Active': return 'warning' as const;
      case 'Expired': return 'default' as const;
      case 'Revoked': return 'danger' as const;
      case 'PostMortemCompleted': return 'success' as const;
    }
  };

  return (
    <PageContainer>
      <PageHeader
        title={t('identity.breakGlass.title')}
        subtitle={t('identity.breakGlass.subtitle')}
        actions={
          <Button onClick={() => setShowForm((v) => !v)}>
            <AlertTriangle size={16} /> {t('identity.breakGlass.request')}
          </Button>
        }
      />

      {/* Formulário de solicitação de acesso emergencial */}
      {showForm && (
        <Card className="mb-6">
          <CardHeader>
            <h2 className="font-semibold text-heading">{t('identity.breakGlass.requestFormTitle')}</h2>
          </CardHeader>
          <CardBody>
            <form
              onSubmit={(e) => { e.preventDefault(); requestMutation.mutate(justification); }}
              className="space-y-4"
            >
              <div>
                <label className="block text-sm font-medium text-body mb-1">
                  {t('identity.breakGlass.justification')}
                </label>
                <textarea
                  value={justification}
                  onChange={(e) => setJustification(e.target.value)}
                  required
                  minLength={20}
                  maxLength={2000}
                  rows={3}
                  placeholder={t('identity.breakGlass.justificationPlaceholder')}
                  className="w-full rounded-md bg-canvas border border-edge px-3 py-2 text-sm text-heading placeholder:text-muted focus:outline-none focus:ring-2 focus:ring-accent focus:border-accent transition-colors"
                />
              </div>
              <div className="flex gap-2 justify-end">
                <Button variant="secondary" type="button" onClick={() => setShowForm(false)}>
                  {t('common.cancel')}
                </Button>
                <Button variant="danger" type="submit" loading={requestMutation.isPending}>
                  {t('identity.breakGlass.confirmRequest')}
                </Button>
              </div>
            </form>
          </CardBody>
        </Card>
      )}

      {/* Tabela de solicitações de Break Glass */}
      <Card>
        <CardHeader>
          <h2 className="text-base font-semibold text-heading">{t('identity.breakGlass.requestsList')}</h2>
        </CardHeader>
        <div className="overflow-x-auto">
          {isLoading ? (
            <PageLoadingState />
          ) : isError ? (
            <PageErrorState />
          ) : !requests?.length ? (
            <p className="px-6 py-12 text-sm text-muted text-center">
              {t('identity.breakGlass.noRequests')}
            </p>
          ) : (
            <table className="min-w-full text-sm">
              <thead>
                <tr className="border-b border-edge bg-panel text-left">
                  <th className="px-6 py-3 font-medium text-muted">{t('identity.breakGlass.justification')}</th>
                  <th className="px-6 py-3 font-medium text-muted">{t('identity.breakGlass.status')}</th>
                  <th className="px-6 py-3 font-medium text-muted">{t('identity.breakGlass.requestedAt')}</th>
                  <th className="px-6 py-3 font-medium text-muted">{t('identity.breakGlass.expiresAt')}</th>
                  <th className="px-6 py-3 font-medium text-muted">{t('common.actions')}</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-edge">
                {requests.map((req) => (
                  <tr key={req.id} className="hover:bg-hover transition-colors">
                    <td className="px-6 py-3 text-body max-w-xs truncate">{req.justification}</td>
                    <td className="px-6 py-3">
                      <Badge variant={statusVariant(req.status)}>{req.status}</Badge>
                    </td>
                    <td className="px-6 py-3 text-body">
                      {new Date(req.requestedAt).toLocaleString()}
                    </td>
                    <td className="px-6 py-3 text-body">
                      {req.expiresAt ? new Date(req.expiresAt).toLocaleString() : '—'}
                    </td>
                    <td className="px-6 py-3">
                      {req.status === 'Active' && (
                        <Button
                          variant="danger"
                          size="sm"
                          onClick={() => revokeMutation.mutate(req.id)}
                          loading={revokeMutation.isPending}
                        >
                          {t('identity.breakGlass.revoke')}
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
