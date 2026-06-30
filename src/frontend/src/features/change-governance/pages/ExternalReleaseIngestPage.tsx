import { useState } from 'react';
import { useMutation } from '@tanstack/react-query';
import { useTranslation } from 'react-i18next';
import { Download, Plus, AlertCircle } from 'lucide-react';
import { Card, CardHeader, CardBody } from '../../../components/Card';
import { Badge } from '../../../components/Badge';
import { Button } from '../../../components/Button';
import { TextField } from '../../../components/TextField';
import { TextArea } from '../../../components/TextArea';
import { Select } from '../../../components/Select';
import { Checkbox } from '../../../components/Checkbox';
import { PageContainer } from '../../../components/shell';
import { PageHeader } from '../../../components/PageHeader';
import { changeIntelligenceApi } from '../api/changeIntelligence';

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
                <Select
                  size="sm"
                  label={t('externalReleaseIngest.externalSystem')}
                  value={externalSystem}
                  onChange={(e) => setExternalSystem(e.target.value)}
                  options={EXTERNAL_SYSTEMS.map((s) => ({ value: s, label: s }))}
                />
                <TextField
                  size="sm"
                  label={t('externalReleaseIngest.externalReleaseId')}
                  value={externalReleaseId}
                  onChange={(e) => setExternalReleaseId(e.target.value)}
                  placeholder="ADO-RELEASE-2024.1"
                />
                <TextField
                  size="sm"
                  label={t('externalReleaseIngest.serviceName')}
                  value={serviceName}
                  onChange={(e) => setServiceName(e.target.value)}
                  placeholder="payment-service"
                />
                <TextField
                  size="sm"
                  label={t('externalReleaseIngest.version')}
                  value={version}
                  onChange={(e) => setVersion(e.target.value)}
                  placeholder="2.1.0"
                />
                <Select
                  size="sm"
                  label={t('externalReleaseIngest.targetEnvironment')}
                  value={targetEnvironment}
                  onChange={(e) => setTargetEnvironment(e.target.value)}
                  options={[
                    { value: 'PreProduction', label: 'PreProduction' },
                    { value: 'Production', label: 'Production' },
                    { value: 'Staging', label: 'Staging' },
                  ]}
                />
                <TextField
                  size="sm"
                  label={t('externalReleaseIngest.description')}
                  value={description}
                  onChange={(e) => setDescription(e.target.value)}
                  placeholder={t('externalReleaseIngest.descriptionPlaceholder')}
                />
              </div>

              <TextArea
                label={t('externalReleaseIngest.commitShas')}
                value={commitShas}
                onChange={(e) => setCommitShas(e.target.value)}
                placeholder={t('externalReleaseIngest.commitShasPlaceholder')}
                rows={3}
              />

              <Checkbox
                checked={triggerPromotion}
                onChange={(e) => setTriggerPromotion(e.target.checked)}
                label={t('externalReleaseIngest.triggerPromotion')}
              />

              <Button
                variant="primary"
                size="sm"
                onClick={() => ingestMutation.mutate()}
                disabled={!externalReleaseId || !serviceName || !version || ingestMutation.isPending}
                loading={ingestMutation.isPending}
              >
                <Download className="w-4 h-4 mr-2" />
                {t('externalReleaseIngest.ingestBtn')}
              </Button>
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
