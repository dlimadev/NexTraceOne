import { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { Plus, Search, RefreshCw } from 'lucide-react';
import { useTranslation } from 'react-i18next';
import { Card, CardHeader, CardBody } from '../../../components/Card';
import { Button } from '../../../components/Button';
import { Badge } from '../../../components/Badge';
import { changeIntelligenceApi } from '../api';
import type { ChangeLevel, DeploymentState } from '../../../types';

const CHANGE_LEVEL_KEYS = [
  'releases.changeLevels.operational',
  'releases.changeLevels.nonBreaking',
  'releases.changeLevels.additive',
  'releases.changeLevels.breaking',
  'releases.changeLevels.publication',
] as const;

function changeLevelVariant(level: ChangeLevel): 'default' | 'success' | 'warning' | 'danger' | 'info' {
  if (level === 0) return 'default';
  if (level === 1) return 'success';
  if (level === 2) return 'info';
  if (level === 3) return 'danger';
  return 'warning';
}

function stateVariant(state: DeploymentState): 'default' | 'success' | 'warning' | 'danger' | 'info' {
  if (state === 'Succeeded') return 'success';
  if (state === 'Failed' || state === 'RolledBack') return 'danger';
  if (state === 'Running') return 'info';
  return 'default';
}

interface NotifyForm {
  apiAssetId: string;
  version: string;
  environment: string;
  commitSha: string;
}

const emptyForm: NotifyForm = { apiAssetId: '', version: '', environment: 'production', commitSha: '' };

export function ReleasesPage() {
  const { t } = useTranslation();
  const queryClient = useQueryClient();
  const [apiAssetId, setApiAssetId] = useState('');
  const [showForm, setShowForm] = useState(false);
  const [form, setForm] = useState<NotifyForm>(emptyForm);
  const [page] = useState(1);

  const { data, isLoading, isError } = useQuery({
    queryKey: ['releases', apiAssetId, page],
    queryFn: () =>
      apiAssetId ? changeIntelligenceApi.listReleases(apiAssetId, page, 20) : null,
    enabled: !!apiAssetId,
  });

  const notifyMutation = useMutation({
    mutationFn: changeIntelligenceApi.notifyDeployment,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['releases'] });
      setShowForm(false);
      setForm(emptyForm);
    },
  });

  const handleNotify = (e: React.FormEvent) => {
    e.preventDefault();
    notifyMutation.mutate(form);
  };

  return (
    <div className="p-6 lg:p-8 animate-fade-in">
      <div className="flex items-center justify-between mb-6">
        <div>
          <h1 className="text-2xl font-bold text-heading">{t('releases.title')}</h1>
          <p className="text-muted mt-1">{t('releases.subtitle')}</p>
        </div>
        <Button onClick={() => setShowForm((v) => !v)}>
          <Plus size={16} />
          {t('releases.notifyDeployment')}
        </Button>
      </div>

      {/* Notify Deployment Form */}
      {showForm && (
        <Card className="mb-6">
          <CardHeader>
            <h2 className="text-base font-semibold text-heading">{t('releases.notifyNewDeployment')}</h2>
          </CardHeader>
          <CardBody>
            <form onSubmit={handleNotify} className="grid grid-cols-2 gap-4">
              <div>
                <label className="block text-sm font-medium text-body mb-1">{t('releases.apiAssetId')}</label>
                <input
                  type="text"
                  value={form.apiAssetId}
                  onChange={(e) => setForm((f) => ({ ...f, apiAssetId: e.target.value }))}
                  required
                  className="w-full rounded-md bg-canvas border border-edge px-3 py-2 text-sm text-heading placeholder:text-muted focus:outline-none focus:ring-2 focus:ring-accent focus:border-accent transition-colors"
                  placeholder={t('releases.apiAssetPlaceholder')}
                />
              </div>
              <div>
                <label className="block text-sm font-medium text-body mb-1">{t('releases.version')}</label>
                <input
                  type="text"
                  value={form.version}
                  onChange={(e) => setForm((f) => ({ ...f, version: e.target.value }))}
                  required
                  className="w-full rounded-md bg-canvas border border-edge px-3 py-2 text-sm text-heading placeholder:text-muted focus:outline-none focus:ring-2 focus:ring-accent focus:border-accent transition-colors"
                  placeholder={t('releases.versionPlaceholder')}
                />
              </div>
              <div>
                <label className="block text-sm font-medium text-body mb-1">{t('releases.environment')}</label>
                <select
                  value={form.environment}
                  onChange={(e) => setForm((f) => ({ ...f, environment: e.target.value }))}
                  className="w-full rounded-md bg-canvas border border-edge px-3 py-2 text-sm text-heading focus:outline-none focus:ring-2 focus:ring-accent focus:border-accent transition-colors"
                >
                  <option value="development">Development</option>
                  <option value="staging">Staging</option>
                  <option value="production">Production</option>
                </select>
              </div>
              <div>
                <label className="block text-sm font-medium text-body mb-1">{t('releases.commitSha')}</label>
                <input
                  type="text"
                  value={form.commitSha}
                  onChange={(e) => setForm((f) => ({ ...f, commitSha: e.target.value }))}
                  className="w-full rounded-md bg-canvas border border-edge px-3 py-2 text-sm text-heading placeholder:text-muted focus:outline-none focus:ring-2 focus:ring-accent focus:border-accent transition-colors"
                  placeholder={t('releases.commitPlaceholder')}
                />
              </div>
              <div className="col-span-2 flex gap-2 justify-end">
                <Button variant="secondary" type="button" onClick={() => setShowForm(false)}>
                  {t('common.cancel')}
                </Button>
                <Button type="submit" loading={notifyMutation.isPending}>
                  {t('releases.submit')}
                </Button>
              </div>
            </form>
          </CardBody>
        </Card>
      )}

      {/* Search by API Asset */}
      <Card className="mb-6">
        <CardBody>
          <div className="flex gap-3 items-center">
            <Search size={16} className="text-muted shrink-0" />
            <input
              type="text"
              value={apiAssetId}
              onChange={(e) => setApiAssetId(e.target.value)}
              placeholder={t('releases.filterPlaceholder')}
              className="flex-1 text-sm bg-transparent text-heading placeholder:text-muted focus:outline-none"
            />
          </div>
        </CardBody>
      </Card>

      {/* Releases Table */}
      <Card>
        <CardHeader>
          <div className="flex items-center justify-between">
            <h2 className="text-base font-semibold text-heading">{t('releases.releaseHistory')}</h2>
            {data && (
              <span className="text-sm text-muted">{data.totalCount} total</span>
            )}
          </div>
        </CardHeader>
        <div className="overflow-x-auto">
          {!apiAssetId ? (
            <p className="px-6 py-12 text-sm text-muted text-center">
              {t('releases.enterApiAssetId')}
            </p>
          ) : isLoading ? (
            <div className="flex items-center justify-center py-12">
              <RefreshCw size={20} className="animate-spin text-muted" />
            </div>
          ) : isError ? (
            <p className="px-6 py-12 text-sm text-critical text-center">
              {t('releases.loadFailed')}
            </p>
          ) : !data?.items?.length ? (
            <p className="px-6 py-12 text-sm text-muted text-center">
              {t('releases.noReleases')}
            </p>
          ) : (
            <table className="min-w-full text-sm">
              <thead>
                <tr className="border-b border-edge bg-panel text-left">
                  <th className="px-6 py-3 font-medium text-muted">{t('releases.version')}</th>
                  <th className="px-6 py-3 font-medium text-muted">{t('releases.environment')}</th>
                  <th className="px-6 py-3 font-medium text-muted">{t('releases.changeLevel')}</th>
                  <th className="px-6 py-3 font-medium text-muted">{t('releases.state')}</th>
                  <th className="px-6 py-3 font-medium text-muted">{t('releases.riskScore')}</th>
                  <th className="px-6 py-3 font-medium text-muted">{t('releases.date')}</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-edge">
                {data.items.map((r) => (
                  <tr key={r.id} className="hover:bg-hover transition-colors">
                    <td className="px-6 py-3 font-mono text-xs text-body">{r.version}</td>
                    <td className="px-6 py-3 text-body capitalize">{r.environment}</td>
                    <td className="px-6 py-3">
                      <Badge variant={changeLevelVariant(r.changeLevel)}>
                        {t(CHANGE_LEVEL_KEYS[r.changeLevel] ?? 'releases.changeLevels.unknown')}
                      </Badge>
                    </td>
                    <td className="px-6 py-3">
                      <Badge variant={stateVariant(r.deploymentState)}>{r.deploymentState}</Badge>
                    </td>
                    <td className="px-6 py-3 text-body">
                      {r.riskScore != null ? (r.riskScore * 100).toFixed(0) + '%' : '—'}
                    </td>
                    <td className="px-6 py-3 text-muted text-xs">
                      {new Date(r.createdAt).toLocaleString()}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          )}
        </div>
      </Card>
    </div>
  );
}
