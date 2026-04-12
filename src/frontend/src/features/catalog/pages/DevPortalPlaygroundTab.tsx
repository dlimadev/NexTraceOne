/**
 * Tab de Playground do Developer Portal.
 *
 * Extraído de DeveloperPortalPage para reduzir complexidade.
 * Contém formulário de execução, resultado e histórico.
 */
import { useTranslation } from 'react-i18next';
import { Play } from 'lucide-react';
import { Card, CardHeader, CardBody } from '../../../components/Card';
import { Button } from '../../../components/Button';
import { Badge } from '../../../components/Badge';
import type { PlaygroundResult, PlaygroundHistoryItem } from '../../../types';
import type { PlaygroundForm } from './DeveloperPortalPage';

const HTTP_METHODS = ['GET', 'POST', 'PUT', 'PATCH', 'DELETE'];

export interface DevPortalPlaygroundTabProps {
  playForm: PlaygroundForm;
  onPlayFormChange: (form: PlaygroundForm) => void;
  onExecute: () => void;
  isExecuting: boolean;
  playResult: PlaygroundResult | null;
  historyItems: PlaygroundHistoryItem[] | undefined;
  historyLoading: boolean;
  fieldClass: string;
}

export function DevPortalPlaygroundTab({
  playForm,
  onPlayFormChange,
  onExecute,
  isExecuting,
  playResult,
  historyItems,
  historyLoading,
  fieldClass,
}: DevPortalPlaygroundTabProps) {
  const { t } = useTranslation();

  return (
    <div className="space-y-4">
      <Card>
        <CardHeader>
          <h2 className="text-lg font-semibold text-heading">
            {t('developerPortal.playground.title')}
          </h2>
        </CardHeader>
        <CardBody>
          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            <div>
              <label className="block text-sm font-medium text-body mb-1">
                {t('developerPortal.playground.form.apiAssetId')}
              </label>
              <input
                className={fieldClass}
                value={playForm.apiAssetId}
                onChange={(e) => onPlayFormChange({ ...playForm, apiAssetId: e.target.value })}
              />
            </div>
            <div>
              <label className="block text-sm font-medium text-body mb-1">
                {t('developerPortal.playground.form.apiName')}
              </label>
              <input
                className={fieldClass}
                value={playForm.apiName}
                onChange={(e) => onPlayFormChange({ ...playForm, apiName: e.target.value })}
              />
            </div>
            <div>
              <label className="block text-sm font-medium text-body mb-1">
                {t('developerPortal.playground.form.httpMethod')}
              </label>
              <select
                className={fieldClass}
                value={playForm.httpMethod}
                onChange={(e) => onPlayFormChange({ ...playForm, httpMethod: e.target.value })}
              >
                {HTTP_METHODS.map((m) => (
                  <option key={m} value={m}>
                    {m}
                  </option>
                ))}
              </select>
            </div>
            <div>
              <label className="block text-sm font-medium text-body mb-1">
                {t('developerPortal.playground.form.requestPath')}
              </label>
              <input
                className={fieldClass}
                value={playForm.requestPath}
                onChange={(e) => onPlayFormChange({ ...playForm, requestPath: e.target.value })}
              />
            </div>
            <div className="md:col-span-2">
              <label className="block text-sm font-medium text-body mb-1">
                {t('developerPortal.playground.form.requestBody')}
              </label>
              <textarea
                className={`${fieldClass} h-24 font-mono`}
                value={playForm.requestBody}
                onChange={(e) => onPlayFormChange({ ...playForm, requestBody: e.target.value })}
              />
            </div>
            <div>
              <label className="block text-sm font-medium text-body mb-1">
                {t('developerPortal.playground.form.requestHeaders')}
              </label>
              <input
                className={fieldClass}
                value={playForm.requestHeaders}
                onChange={(e) => onPlayFormChange({ ...playForm, requestHeaders: e.target.value })}
              />
            </div>
            <div>
              <label className="block text-sm font-medium text-body mb-1">
                {t('developerPortal.playground.form.environment')}
              </label>
              <input
                className={fieldClass}
                value={playForm.environment}
                onChange={(e) => onPlayFormChange({ ...playForm, environment: e.target.value })}
              />
            </div>
          </div>
          <div className="mt-4">
            <Button onClick={onExecute} disabled={isExecuting}>
              <Play size={16} className="mr-1" />
              {t('developerPortal.playground.execute')}
            </Button>
          </div>
        </CardBody>
      </Card>

      {playResult && (
        <Card>
          <CardHeader>
            <div className="flex justify-between items-center">
              <h3 className="font-semibold text-heading">
                {t('developerPortal.playground.result.responseBody')}
              </h3>
              <Badge
                variant={(playResult.responseStatusCode ?? playResult.statusCode) < 400 ? 'success' : 'danger'}
              >
                {t('developerPortal.playground.result.statusCode')}: {playResult.responseStatusCode ?? playResult.statusCode}
              </Badge>
            </div>
          </CardHeader>
          <CardBody>
            <pre className="bg-surface p-3 rounded-md text-xs font-mono overflow-x-auto max-h-64">
              {playResult.responseBody}
            </pre>
            <div className="flex gap-4 mt-2 text-xs text-muted">
              <span>
                {t('developerPortal.playground.result.duration')}: {playResult.durationMs}ms
              </span>
              <span>
                {t('developerPortal.playground.result.executedAt')}:{' '}
                {playResult.executedAt ? new Date(playResult.executedAt).toLocaleString() : '-'}
              </span>
            </div>
          </CardBody>
        </Card>
      )}

      {/* Histórico do playground */}
      <Card>
        <CardHeader>
          <h3 className="font-semibold text-heading">
            {t('developerPortal.playground.history')}
          </h3>
        </CardHeader>
        <CardBody>
          {historyLoading && (
            <p className="text-muted text-sm">{t('common.loading')}</p>
          )}
          {historyItems && historyItems.length === 0 && (
            <p className="text-muted text-sm">
              {t('developerPortal.playground.noHistory')}
            </p>
          )}
          {historyItems && historyItems.length > 0 && (
            <div className="overflow-x-auto">
              <table className="w-full text-sm">
                <thead className="sticky top-0 z-10 bg-panel">
                  <tr className="border-b border-edge text-left text-muted">
                    <th className="py-2 px-3">{t('sourceOfTruth.table.api')}</th>
                    <th className="py-2 px-3">
                      {t('developerPortal.playground.form.httpMethod')}
                    </th>
                    <th className="py-2 px-3">
                      {t('developerPortal.playground.form.requestPath')}
                    </th>
                    <th className="py-2 px-3">
                      {t('developerPortal.playground.result.statusCode')}
                    </th>
                    <th className="py-2 px-3">
                      {t('developerPortal.playground.result.duration')}
                    </th>
                  </tr>
                </thead>
                <tbody>
                  {historyItems.map((h) => (
                    <tr key={h.sessionId} className="border-b border-edge/50">
                      <td className="py-2 px-3 text-body">{h.apiName}</td>
                      <td className="py-2 px-3">
                        <Badge variant="info">{h.httpMethod}</Badge>
                      </td>
                      <td className="py-2 px-3 text-muted font-mono text-xs">
                        {h.requestPath}
                      </td>
                      <td className="py-2 px-3">
                        <Badge variant={(h.responseStatusCode ?? h.statusCode) < 400 ? 'success' : 'danger'}>
                          {h.responseStatusCode ?? h.statusCode}
                        </Badge>
                      </td>
                      <td className="py-2 px-3 text-muted">{h.durationMs}ms</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </CardBody>
      </Card>
    </div>
  );
}
