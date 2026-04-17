import { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { useTranslation } from 'react-i18next';
import { ShieldCheck, Plus, AlertTriangle } from 'lucide-react';
import { Card, CardHeader, CardBody } from '../../../components/Card';
import { Badge } from '../../../components/Badge';
import { PageContainer } from '../../../components/shell';
import { PageHeader } from '../../../components/PageHeader';
import { PageLoadingState } from '../../../components/PageLoadingState';
import { PageErrorState } from '../../../components/PageErrorState';
import { changeIntelligenceApi } from '../api/changeIntelligence';
import { useEnvironment } from '../../../contexts/EnvironmentContext';

const INPUT_CLS =
  'w-full rounded-md bg-canvas border border-edge px-3 py-2 text-sm text-heading placeholder:text-muted focus:outline-none focus:ring-2 focus:ring-accent focus:border-accent transition-colors';

function approvalStatusVariant(
  status: string,
): 'default' | 'info' | 'success' | 'warning' | 'danger' {
  switch (status) {
    case 'Approved':
      return 'success';
    case 'Rejected':
      return 'danger';
    case 'Pending':
      return 'warning';
    case 'Expired':
      return 'danger';
    case 'AutoApproved':
      return 'info';
    default:
      return 'default';
  }
}

/**
 * ReleaseApprovalGatewayPage — gestão de approval gates de releases.
 *
 * Permite criar pedidos de aprovação (internos, external webhook, ServiceNow)
 * e acompanhar o estado de cada pedido, incluindo os callbacks inbound de
 * sistemas externos.
 */
export function ReleaseApprovalGatewayPage() {
  const { t } = useTranslation();
  const { activeEnvironmentId } = useEnvironment();
  const queryClient = useQueryClient();
  const [releaseId, setReleaseId] = useState('');

  // Form state for new approval request
  const [approvalType, setApprovalType] = useState('Internal');
  const [targetEnvironment, setTargetEnvironment] = useState('Production');
  const [externalSystem, setExternalSystem] = useState('');
  const [webhookUrl, setWebhookUrl] = useState('');
  const [tokenExpiryHours, setTokenExpiryHours] = useState(48);

  const approvalsQuery = useQuery({
    queryKey: ['release-approvals', releaseId, activeEnvironmentId],
    queryFn: () => changeIntelligenceApi.listApprovalRequests(releaseId),
    enabled: !!releaseId,
  });

  const requestApprovalMutation = useMutation({
    mutationFn: () =>
      changeIntelligenceApi.requestExternalApproval(releaseId, {
        approvalType,
        targetEnvironment,
        externalSystem: externalSystem || undefined,
        webhookUrl: webhookUrl || undefined,
        tokenExpiryHours,
      }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['release-approvals', releaseId] });
      setWebhookUrl('');
    },
  });

  const approvals = approvalsQuery.data?.approvalRequests ?? [];

  return (
    <PageContainer>
      <PageHeader
        title={t('approvalGateway.title')}
        subtitle={t('approvalGateway.subtitle')}
      />

      {/* Release ID input */}
      <Card className="mb-6">
        <CardBody>
          <div>
            <label className="block text-xs font-medium text-muted mb-1">
              {t('approvalGateway.releaseIdLabel')}
            </label>
            <input
              type="text"
              value={releaseId}
              onChange={(e) => setReleaseId(e.target.value)}
              placeholder={t('approvalGateway.releaseIdPlaceholder')}
              className={INPUT_CLS}
            />
          </div>
        </CardBody>
      </Card>

      {/* Create approval request */}
      <Card className="mb-6">
        <CardHeader>
          <h2 className="text-sm font-semibold text-heading">
            {t('approvalGateway.newRequest')}
          </h2>
        </CardHeader>
        <CardBody>
          <div className="grid grid-cols-2 gap-3 mb-3">
            <div>
              <label className="block text-xs font-medium text-muted mb-1">
                {t('approvalGateway.approvalType')}
              </label>
              <select
                value={approvalType}
                onChange={(e) => setApprovalType(e.target.value)}
                className={INPUT_CLS}
              >
                <option value="Internal">{t('approvalGateway.typeInternal')}</option>
                <option value="ExternalWebhook">{t('approvalGateway.typeExternalWebhook')}</option>
                <option value="ServiceNow">ServiceNow</option>
                <option value="AutoApprove">{t('approvalGateway.typeAutoApprove')}</option>
              </select>
            </div>
            <div>
              <label className="block text-xs font-medium text-muted mb-1">
                {t('approvalGateway.targetEnvironment')}
              </label>
              <select
                value={targetEnvironment}
                onChange={(e) => setTargetEnvironment(e.target.value)}
                className={INPUT_CLS}
              >
                <option value="Production">Production</option>
                <option value="PreProduction">PreProduction</option>
                <option value="Staging">Staging</option>
              </select>
            </div>
            {approvalType === 'ExternalWebhook' && (
              <>
                <div>
                  <label className="block text-xs font-medium text-muted mb-1">
                    {t('approvalGateway.externalSystem')}
                  </label>
                  <input
                    type="text"
                    value={externalSystem}
                    onChange={(e) => setExternalSystem(e.target.value)}
                    placeholder="ServiceNow, Teams, Slack..."
                    className={INPUT_CLS}
                  />
                </div>
                <div>
                  <label className="block text-xs font-medium text-muted mb-1">
                    {t('approvalGateway.webhookUrl')}
                  </label>
                  <input
                    type="url"
                    value={webhookUrl}
                    onChange={(e) => setWebhookUrl(e.target.value)}
                    placeholder="https://external-system.example.com/approval-request"
                    className={INPUT_CLS}
                  />
                </div>
              </>
            )}
            <div>
              <label className="block text-xs font-medium text-muted mb-1">
                {t('approvalGateway.tokenExpiry')}
              </label>
              <input
                type="number"
                min={1}
                max={168}
                value={tokenExpiryHours}
                onChange={(e) => setTokenExpiryHours(Number(e.target.value))}
                className={INPUT_CLS}
              />
            </div>
          </div>

          {approvalType === 'ExternalWebhook' && (
            <div className="flex items-start gap-2 p-3 rounded-md bg-warning/10 border border-warning/30 mb-3">
              <AlertTriangle className="w-4 h-4 text-warning mt-0.5 shrink-0" />
              <p className="text-xs text-muted">{t('approvalGateway.webhookNote')}</p>
            </div>
          )}

          <button
            onClick={() => requestApprovalMutation.mutate()}
            disabled={!releaseId || requestApprovalMutation.isPending}
            className="flex items-center gap-2 px-4 py-2 text-sm font-medium rounded-md bg-accent text-white hover:bg-accent/90 disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
          >
            <Plus className="w-4 h-4" />
            {t('approvalGateway.submitRequest')}
          </button>
        </CardBody>
      </Card>

      {/* Approval requests list */}
      {approvalsQuery.isLoading && <PageLoadingState />}
      {approvalsQuery.isError && <PageErrorState />}
      {!approvalsQuery.isLoading && !approvalsQuery.isError && releaseId && (
        <Card>
          <CardHeader>
            <h2 className="text-sm font-semibold text-heading">
              {t('approvalGateway.requestsTitle', { count: approvals.length })}
            </h2>
          </CardHeader>
          <CardBody>
            {approvals.length === 0 ? (
              <p className="text-sm text-muted text-center py-8">
                {t('approvalGateway.noRequests')}
              </p>
            ) : (
              <div className="space-y-3">
                {approvals.map((a) => (
                  <div
                    key={a.id}
                    className="flex items-start gap-3 p-3 rounded-md bg-surface border border-edge"
                  >
                    <ShieldCheck className="w-4 h-4 text-muted mt-0.5 shrink-0" />
                    <div className="flex-1 min-w-0">
                      <div className="flex items-center gap-2 flex-wrap">
                        <Badge variant={approvalStatusVariant(a.status)}>{a.status}</Badge>
                        <Badge variant="default">{a.approvalType}</Badge>
                        {a.externalSystem && (
                          <Badge variant="info">{a.externalSystem}</Badge>
                        )}
                        <span className="text-xs text-muted">→ {a.targetEnvironment}</span>
                      </div>
                      <div className="text-xs text-muted mt-1">
                        {t('approvalGateway.requestedAt')}:{' '}
                        {new Date(a.requestedAt).toLocaleString()}
                        {a.respondedAt && (
                          <> · {t('approvalGateway.respondedAt')}:{' '}
                            {new Date(a.respondedAt).toLocaleString()}
                          </>
                        )}
                      </div>
                      {a.comments && (
                        <p className="text-xs text-muted mt-1 italic">"{a.comments}"</p>
                      )}
                      <p className="text-xs text-muted mt-0.5">
                        {t('approvalGateway.tokenExpires')}:{' '}
                        {new Date(a.callbackTokenExpiresAt).toLocaleString()}
                      </p>
                    </div>
                  </div>
                ))}
              </div>
            )}
          </CardBody>
        </Card>
      )}
    </PageContainer>
  );
}
