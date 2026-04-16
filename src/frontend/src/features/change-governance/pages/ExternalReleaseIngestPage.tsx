import { useState } from 'react';
import { useMutation } from '@tanstack/react-query';
import { useTranslation } from 'react-i18next';
import { Download, Plus, AlertCircle } from 'lucide-react';
import { Card, CardHeader, CardBody } from '../../../components/Card';
import { Badge } from '../../../components/Badge';
import { PageContainer } from '../../../components/shell';
import { PageHeader } from '../../../components/PageHeader';
import { changeIntelligenceApi } from '../api/changeIntelligence';

const INPUT_CLS =
  'w-full rounded-md bg-canvas border border-edge px-3 py-2 text-sm text-heading placeholder:text-muted focus:outline-none focus:ring-2 focus:ring-accent focus:border-accent transition-colors';

const EXTERNAL_SYSTEMS = ['AzureDevOps', 'Jira', 'Jenkins', 'GitLab', 'GitHub', 'Custom'];

/**
 * ExternalReleaseIngestPage — ingestão de release de sistema externo.
 *
 * Permite que releases criadas em sistemas externos (AzureDevOps, Jira, Jenkins)
 * sejam registadas no NexTraceOne como origem externa, preservando metadados,
 * commits e work items associados.
 *
 * Segue o padrão: sistemas externos são first-class citizens no NexTraceOne.
 */
export function ExternalReleaseIngestPage() {
  const { t } = useTranslation();
  const [externalReleaseId, setExternalReleaseId] = useState('');
  const [externalSystem, setExternalSystem] = useState('AzureDevOps');
  const [serviceName, setServiceName] = useState('');
  const [version, setVersion] = useState('');
  const [targetEnvironment, setTargetEnvironment] = useState('PreProduction');
  const [description, setDescription] = useState('');
  const [commitShas, setCommitShas] = useState('');
  const [triggerPromotion, setTriggerPromotion] = useState(false);

  const [result, setResult] = useState<{
    releaseId: string;
    externalReleaseId: string;
    isNew: boolean;
    status: string;
  } | null>(null);

  const ingestMutation = useMutation({
    mutationFn: () =>
      changeIntelligenceApi.ingestExternalRelease({
        externalReleaseId,
        externalSystem,
        serviceName,
        version,
        targetEnvironment,
        description: description || undefined,
        commitShas: commitShas ? commitShas.split('\n').map((s) => s.trim()).filter(Boolean) : undefined,
        triggerPromotion,
      }),
    onSuccess: (data) => setResult(data),
  });

  return (
    <PageContainer>
      <PageHeader
        title={t('externalReleaseIngest.title')}
        subtitle={t('externalReleaseIngest.subtitle')}
      />

      <div className="max-w-2xl space-y-6">
        {/* Info banner */}
        <div className="flex items-start gap-3 p-4 rounded-md bg-info/10 border border-info/30">
          <AlertCircle className="w-4 h-4 text-info mt-0.5 shrink-0" />
          <p className="text-sm text-muted">{t('externalReleaseIngest.info')}</p>
        </div>

        <Card>
          <CardHeader>
            <h2 className="text-sm font-semibold text-heading">
              {t('externalReleaseIngest.formTitle')}
            </h2>
          </CardHeader>
          <CardBody>
            <div className="space-y-4">
              <div className="grid grid-cols-2 gap-3">
                <div>
                  <label className="block text-xs font-medium text-muted mb-1">
                    {t('externalReleaseIngest.externalSystem')}
                  </label>
                  <select
                    value={externalSystem}
                    onChange={(e) => setExternalSystem(e.target.value)}
                    className={INPUT_CLS}
                  >
                    {EXTERNAL_SYSTEMS.map((s) => (
                      <option key={s} value={s}>{s}</option>
                    ))}
                  </select>
                </div>
                <div>
                  <label className="block text-xs font-medium text-muted mb-1">
                    {t('externalReleaseIngest.externalReleaseId')}
                  </label>
                  <input
                    type="text"
                    value={externalReleaseId}
                    onChange={(e) => setExternalReleaseId(e.target.value)}
                    placeholder="ADO-RELEASE-2024.1"
                    className={INPUT_CLS}
                  />
                </div>
                <div>
                  <label className="block text-xs font-medium text-muted mb-1">
                    {t('externalReleaseIngest.serviceName')}
                  </label>
                  <input
                    type="text"
                    value={serviceName}
                    onChange={(e) => setServiceName(e.target.value)}
                    placeholder="payment-service"
                    className={INPUT_CLS}
                  />
                </div>
                <div>
                  <label className="block text-xs font-medium text-muted mb-1">
                    {t('externalReleaseIngest.version')}
                  </label>
                  <input
                    type="text"
                    value={version}
                    onChange={(e) => setVersion(e.target.value)}
                    placeholder="2.1.0"
                    className={INPUT_CLS}
                  />
                </div>
                <div>
                  <label className="block text-xs font-medium text-muted mb-1">
                    {t('externalReleaseIngest.targetEnvironment')}
                  </label>
                  <select
                    value={targetEnvironment}
                    onChange={(e) => setTargetEnvironment(e.target.value)}
                    className={INPUT_CLS}
                  >
                    <option value="PreProduction">PreProduction</option>
                    <option value="Production">Production</option>
                    <option value="Staging">Staging</option>
                  </select>
                </div>
                <div>
                  <label className="block text-xs font-medium text-muted mb-1">
                    {t('externalReleaseIngest.description')}
                  </label>
                  <input
                    type="text"
                    value={description}
                    onChange={(e) => setDescription(e.target.value)}
                    placeholder={t('externalReleaseIngest.descriptionPlaceholder')}
                    className={INPUT_CLS}
                  />
                </div>
              </div>

              <div>
                <label className="block text-xs font-medium text-muted mb-1">
                  {t('externalReleaseIngest.commitShas')}
                </label>
                <textarea
                  value={commitShas}
                  onChange={(e) => setCommitShas(e.target.value)}
                  placeholder={t('externalReleaseIngest.commitShasPlaceholder')}
                  rows={3}
                  className={INPUT_CLS}
                />
              </div>

              <label className="flex items-center gap-2 cursor-pointer">
                <input
                  type="checkbox"
                  checked={triggerPromotion}
                  onChange={(e) => setTriggerPromotion(e.target.checked)}
                  className="rounded border-edge"
                />
                <span className="text-sm text-heading">
                  {t('externalReleaseIngest.triggerPromotion')}
                </span>
              </label>

              <button
                onClick={() => ingestMutation.mutate()}
                disabled={
                  !externalReleaseId ||
                  !serviceName ||
                  !version ||
                  ingestMutation.isPending
                }
                className="flex items-center gap-2 px-4 py-2 text-sm font-medium rounded-md bg-accent text-white hover:bg-accent/90 disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
              >
                <Download className="w-4 h-4" />
                {t('externalReleaseIngest.ingestBtn')}
              </button>
            </div>
          </CardBody>
        </Card>

        {/* Result */}
        {result && (
          <Card>
            <CardHeader>
              <h2 className="text-sm font-semibold text-heading">
                {result.isNew
                  ? t('externalReleaseIngest.resultNew')
                  : t('externalReleaseIngest.resultExisting')}
              </h2>
            </CardHeader>
            <CardBody>
              <div className="space-y-2 text-sm">
                <div className="flex items-center gap-2">
                  <span className="text-muted">{t('externalReleaseIngest.resultReleaseId')}:</span>
                  <code className="text-xs font-mono text-accent">{result.releaseId}</code>
                </div>
                <div className="flex items-center gap-2">
                  <span className="text-muted">{t('externalReleaseIngest.resultExtId')}:</span>
                  <code className="text-xs font-mono text-muted">{result.externalReleaseId}</code>
                </div>
                <div className="flex items-center gap-2">
                  <span className="text-muted">{t('externalReleaseIngest.resultStatus')}:</span>
                  <Badge variant="info">{result.status}</Badge>
                </div>
              </div>
            </CardBody>
          </Card>
        )}
      </div>
    </PageContainer>
  );
}
