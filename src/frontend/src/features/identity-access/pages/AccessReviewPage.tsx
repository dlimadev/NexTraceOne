import { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { useTranslation } from 'react-i18next';
import { ClipboardCheck, RefreshCw, ChevronRight } from 'lucide-react';
import { Card, CardHeader, CardBody } from '../../../components/Card';
import { Button } from '../../../components/Button';
import { Badge } from '../../../components/Badge';
import { identityApi } from '../api';
import { useAuth } from '../../../contexts/AuthContext';
import type { AccessReviewCampaign } from '../../../types';
import { PageContainer } from '../../../components/shell';
import { PageHeader } from '../../../components/PageHeader';

/**
 * Página de gestão de campanhas de revisão de acessos (Access Review) do módulo Identity.
 *
 * Permite listar campanhas existentes, iniciar novas campanhas de revisão,
 * visualizar detalhes de uma campanha com itens pendentes, e decidir (confirmar/revogar)
 * cada item de revisão individualmente.
 *
 * Todos os textos são resolvidos via i18n (chaves em identity.accessReview.* e common.*).
 * Funcionalidade essencial para compliance e auditoria de acessos enterprise.
 */
export function AccessReviewPage() {
  const { t } = useTranslation();
  const queryClient = useQueryClient();
  const { tenantId: authTenantId } = useAuth();
  const tenantId = authTenantId ?? '';
  const [showForm, setShowForm] = useState(false);
  const [selectedCampaignId, setSelectedCampaignId] = useState<string | null>(null);
  const [form, setForm] = useState({ name: '', scope: '', reviewerIds: '' });
  const [decisionComments, setDecisionComments] = useState<Record<string, string>>({});

  /** Consulta as campanhas de revisão de acessos existentes. */
  const { data: campaigns, isLoading: isLoadingCampaigns } = useQuery({
    queryKey: ['access-reviews', tenantId],
    queryFn: () => identityApi.listAccessReviewCampaigns(),
    enabled: !!tenantId,
  });

  /** Consulta os detalhes de uma campanha específica com seus itens de revisão. */
  const { data: campaignDetail, isLoading: isLoadingDetail } = useQuery({
    queryKey: ['access-review-detail', selectedCampaignId],
    queryFn: () => identityApi.getAccessReviewCampaign(selectedCampaignId!),
    enabled: !!selectedCampaignId,
  });

  /** Mutação para iniciar uma nova campanha de revisão de acessos. */
  const startMutation = useMutation({
    mutationFn: () =>
      identityApi.startAccessReviewCampaign({
        name: form.name,
        scope: form.scope,
        reviewerIds: form.reviewerIds.split(',').map((id) => id.trim()).filter(Boolean),
      }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['access-reviews'] });
      setShowForm(false);
      setForm({ name: '', scope: '', reviewerIds: '' });
    },
  });

  /** Mutação para decidir (confirmar/revogar) um item individual da revisão. */
  const decideMutation = useMutation({
    mutationFn: ({ itemId, approve }: { itemId: string; approve: boolean }) =>
      identityApi.decideAccessReviewItem(selectedCampaignId!, itemId, approve, decisionComments[itemId] || undefined),
    onSuccess: (_data, variables) => {
      queryClient.invalidateQueries({ queryKey: ['access-review-detail', selectedCampaignId] });
      queryClient.invalidateQueries({ queryKey: ['access-reviews'] });
      setDecisionComments((prev) => {
        const next = { ...prev };
        delete next[variables.itemId];
        return next;
      });
    },
  });

  /** Mapeia o status da campanha para a variante de Badge adequada. */
  const campaignStatusVariant = (status: AccessReviewCampaign['status']) => {
    switch (status) {
      case 'Open': return 'info' as const;
      case 'InProgress': return 'warning' as const;
      case 'Completed': return 'success' as const;
      case 'Cancelled': return 'default' as const;
    }
  };

  /** Mapeia a decisão do item para a variante de Badge adequada. */
  const decisionVariant = (decision: 'Pending' | 'Confirmed' | 'Revoked' | null) => {
    switch (decision) {
      case 'Confirmed': return 'success' as const;
      case 'Revoked': return 'danger' as const;
      default: return 'warning' as const;
    }
  };

  return (
    <PageContainer>
      <PageHeader
        title={t('identity.accessReview.title')}
        subtitle={t('identity.accessReview.subtitle')}
        actions={
          <Button onClick={() => setShowForm((v) => !v)}>
            <ClipboardCheck size={16} /> {t('identity.accessReview.startCampaign')}
          </Button>
        }
      />

      {/* Formulário de criação de campanha */}
      {showForm && (
        <Card className="mb-6">
          <CardHeader>
            <h2 className="font-semibold text-heading">{t('identity.accessReview.newCampaignTitle')}</h2>
          </CardHeader>
          <CardBody>
            <form
              onSubmit={(e) => { e.preventDefault(); startMutation.mutate(); }}
              className="grid grid-cols-1 md:grid-cols-3 gap-4"
            >
              <div>
                <label className="block text-sm font-medium text-body mb-1">
                  {t('identity.accessReview.campaignName')}
                </label>
                <input
                  type="text"
                  value={form.name}
                  onChange={(e) => setForm((f) => ({ ...f, name: e.target.value }))}
                  required
                  maxLength={200}
                  placeholder={t('identity.accessReview.campaignNamePlaceholder')}
                  className="w-full rounded-md bg-canvas border border-edge px-3 py-2 text-sm text-heading placeholder:text-muted focus:outline-none focus:ring-2 focus:ring-accent focus:border-accent transition-colors"
                />
              </div>
              <div>
                <label className="block text-sm font-medium text-body mb-1">
                  {t('identity.accessReview.scope')}
                </label>
                <input
                  type="text"
                  value={form.scope}
                  onChange={(e) => setForm((f) => ({ ...f, scope: e.target.value }))}
                  required
                  maxLength={200}
                  placeholder={t('identity.accessReview.scopePlaceholder')}
                  className="w-full rounded-md bg-canvas border border-edge px-3 py-2 text-sm text-heading placeholder:text-muted focus:outline-none focus:ring-2 focus:ring-accent focus:border-accent transition-colors"
                />
              </div>
              <div>
                <label className="block text-sm font-medium text-body mb-1">
                  {t('identity.accessReview.reviewerIds')}
                </label>
                <input
                  type="text"
                  value={form.reviewerIds}
                  onChange={(e) => setForm((f) => ({ ...f, reviewerIds: e.target.value }))}
                  required
                  placeholder={t('identity.accessReview.reviewerIdsPlaceholder')}
                  className="w-full rounded-md bg-canvas border border-edge px-3 py-2 text-sm text-heading placeholder:text-muted focus:outline-none focus:ring-2 focus:ring-accent focus:border-accent transition-colors"
                />
              </div>
              <div className="md:col-span-3 flex gap-2 justify-end">
                <Button variant="secondary" type="button" onClick={() => setShowForm(false)}>
                  {t('common.cancel')}
                </Button>
                <Button type="submit" loading={startMutation.isPending}>
                  {t('identity.accessReview.startCampaign')}
                </Button>
              </div>
            </form>
          </CardBody>
        </Card>
      )}

      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
        {/* Lista de campanhas */}
        <div className="lg:col-span-1">
          <Card>
            <CardHeader>
              <h2 className="text-base font-semibold text-heading">{t('identity.accessReview.campaigns')}</h2>
            </CardHeader>
            <div>
              {isLoadingCampaigns ? (
                <div className="flex items-center justify-center py-12">
                  <RefreshCw size={20} className="animate-spin text-muted" />
                </div>
              ) : !campaigns?.length ? (
                <p className="px-6 py-12 text-sm text-muted text-center">
                  {t('identity.accessReview.noCampaigns')}
                </p>
              ) : (
                <ul className="divide-y divide-edge">
                  {campaigns.map((campaign) => (
                    <li key={campaign.id}>
                      <button
                        onClick={() => setSelectedCampaignId(campaign.id)}
                        className={`w-full px-6 py-3 text-left hover:bg-hover transition-colors flex items-center justify-between ${
                          selectedCampaignId === campaign.id ? 'bg-accent/5 border-l-2 border-accent' : ''
                        }`}
                      >
                        <div className="min-w-0 flex-1">
                          <p className="font-medium text-heading text-sm truncate">{campaign.name}</p>
                          <div className="flex items-center gap-2 mt-1">
                            <Badge variant={campaignStatusVariant(campaign.status)}>
                              {campaign.status}
                            </Badge>
                            <span className="text-xs text-muted">
                              {campaign.decidedItems}/{campaign.totalItems}
                            </span>
                          </div>
                        </div>
                        <ChevronRight size={14} className="text-muted shrink-0" />
                      </button>
                    </li>
                  ))}
                </ul>
              )}
            </div>
          </Card>
        </div>

        {/* Detalhe da campanha selecionada */}
        <div className="lg:col-span-2">
          <Card>
            <CardHeader>
              <h2 className="text-base font-semibold text-heading">
                {campaignDetail
                  ? campaignDetail.name
                  : t('identity.accessReview.selectCampaign')}
              </h2>
            </CardHeader>
            <div className="overflow-x-auto">
              {!selectedCampaignId ? (
                <p className="px-6 py-12 text-sm text-muted text-center">
                  {t('identity.accessReview.selectCampaignHint')}
                </p>
              ) : isLoadingDetail ? (
                <div className="flex items-center justify-center py-12">
                  <RefreshCw size={20} className="animate-spin text-muted" />
                </div>
              ) : !campaignDetail?.items?.length ? (
                <p className="px-6 py-12 text-sm text-muted text-center">
                  {t('identity.accessReview.noItems')}
                </p>
              ) : (
                <table className="min-w-full text-sm">
                  <thead>
                    <tr className="border-b border-edge bg-panel text-left">
                      <th className="px-6 py-3 font-medium text-muted">{t('identity.accessReview.userEmail')}</th>
                      <th className="px-6 py-3 font-medium text-muted">{t('identity.accessReview.role')}</th>
                      <th className="px-6 py-3 font-medium text-muted">{t('identity.accessReview.tenant')}</th>
                      <th className="px-6 py-3 font-medium text-muted">{t('identity.accessReview.decision')}</th>
                      <th className="px-6 py-3 font-medium text-muted">{t('common.actions')}</th>
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-edge">
                    {campaignDetail.items.map((item) => (
                      <tr key={item.id} className="hover:bg-hover transition-colors">
                        <td className="px-6 py-3 text-body">{item.userEmail}</td>
                        <td className="px-6 py-3">
                          <Badge variant="info">{item.roleName}</Badge>
                        </td>
                        <td className="px-6 py-3 text-body">{item.tenantName}</td>
                        <td className="px-6 py-3">
                          <Badge variant={decisionVariant(item.decision)}>
                            {item.decision ?? t('identity.accessReview.pending')}
                          </Badge>
                        </td>
                        <td className="px-6 py-3">
                          {(!item.decision || item.decision === 'Pending') && (
                            <div className="flex gap-2 items-center">
                              <input
                                type="text"
                                placeholder={t('identity.accessReview.commentPlaceholder')}
                                value={decisionComments[item.id] ?? ''}
                                onChange={(e) => setDecisionComments((prev) => ({ ...prev, [item.id]: e.target.value }))}
                                maxLength={500}
                                className="rounded-md bg-canvas border border-edge px-2 py-1 text-xs text-heading placeholder:text-muted focus:outline-none focus:ring-1 focus:ring-accent w-32"
                              />
                              <Button
                                size="sm"
                                onClick={() => decideMutation.mutate({ itemId: item.id, approve: true })}
                                loading={decideMutation.isPending}
                              >
                                {t('identity.accessReview.confirm')}
                              </Button>
                              <Button
                                variant="danger"
                                size="sm"
                                onClick={() => decideMutation.mutate({ itemId: item.id, approve: false })}
                                loading={decideMutation.isPending}
                              >
                                {t('identity.accessReview.revokeAccess')}
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
        </div>
      </div>
    </PageContainer>
  );
}
