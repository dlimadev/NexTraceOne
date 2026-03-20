import { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { CheckSquare, Clock } from 'lucide-react';
import { useTranslation } from 'react-i18next';
import { Card, CardHeader, CardBody } from '../../../components/Card';
import { Button } from '../../../components/Button';
import { Badge } from '../../../components/Badge';
import { PageLoadingState } from '../../../components/PageLoadingState';
import { PageErrorState } from '../../../components/PageErrorState';
import { workflowApi } from '../api';
import type { WorkflowInstance } from '../../../types';
import { PageContainer } from '../../../components/shell';

type InstanceStatus = WorkflowInstance['status'];

function statusVariant(status: InstanceStatus): 'default' | 'success' | 'warning' | 'danger' | 'info' {
  if (status === 'Approved') return 'success';
  if (status === 'Rejected') return 'danger';
  if (status === 'InProgress') return 'info';
  return 'default';
}

const TEMPLATE_LEVEL_KEYS = [
  'releases.changeLevels.operational',
  'releases.changeLevels.nonBreaking',
  'releases.changeLevels.additive',
  'releases.changeLevels.breaking',
  'releases.changeLevels.publication',
] as const;
const TEMPLATE_LEVEL_VARIANTS = ['default', 'success', 'info', 'danger', 'warning'] as const;

export function WorkflowPage() {
  const { t } = useTranslation();
  const queryClient = useQueryClient();
  const [rejectReason, setRejectReason] = useState('');
  const [rejectTarget, setRejectTarget] = useState<{ instanceId: string; stageId: string } | null>(null);

  const {
    data: templates,
    isLoading: templatesLoading,
  } = useQuery({
    queryKey: ['workflow', 'templates'],
    queryFn: () => workflowApi.listTemplates(),
    staleTime: 60_000,
  });

  const {
    data: instances,
    isLoading: instancesLoading,
    isError: instancesError,
  } = useQuery({
    queryKey: ['workflow', 'instances'],
    queryFn: () => workflowApi.listInstances(1, 20),
    staleTime: 15_000,
  });

  const approveMutation = useMutation({
    mutationFn: ({ instanceId, stageId }: { instanceId: string; stageId: string }) =>
      workflowApi.approve(instanceId, stageId),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['workflow', 'instances'] }),
  });

  const rejectMutation = useMutation({
    mutationFn: ({ instanceId, stageId, reason }: { instanceId: string; stageId: string; reason: string }) =>
      workflowApi.reject(instanceId, stageId, reason),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['workflow', 'instances'] });
      setRejectTarget(null);
      setRejectReason('');
    },
  });

  const pending = instances?.items?.filter((i) => i.status === 'Pending' || i.status === 'InProgress') ?? [];

  return (
    <PageContainer>
      <div className="mb-6">
        <h1 className="text-2xl font-bold text-heading">{t('workflow.title')}</h1>
        <p className="text-muted mt-1">{t('workflow.subtitle')}</p>
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        {/* Pending Approvals */}
        <Card>
          <CardHeader>
            <div className="flex items-center gap-2">
              <Clock size={16} className="text-muted" />
              <h2 className="font-semibold text-heading">{t('workflow.pendingApprovals')}</h2>
              {pending.length > 0 && (
                <Badge variant="warning">{pending.length}</Badge>
              )}
            </div>
          </CardHeader>
          <CardBody className="p-0">
            {instancesLoading ? (
              <PageLoadingState size="sm" />
            ) : instancesError ? (
              <PageErrorState message={t('workflow.loadFailed')} />
            ) : pending.length === 0 ? (
              <p className="text-sm text-muted text-center py-8">{t('workflow.noPending')}</p>
            ) : (
              <ul className="divide-y divide-edge">
                {pending.map((inst) => (
                  <li key={inst.id} className="px-6 py-4">
                    <div className="flex items-start justify-between gap-3">
                      <div className="min-w-0">
                        <p className="text-sm font-medium text-heading font-mono truncate">
                          {inst.id.slice(0, 8)}…
                        </p>
                        {inst.currentStage && (
                          <p className="text-xs text-muted mt-0.5">{t('workflow.stage')}: {inst.currentStage}</p>
                        )}
                        <p className="text-xs text-faded mt-0.5">
                          {new Date(inst.createdAt).toLocaleString()}
                        </p>
                      </div>
                      <div className="flex items-center gap-2 shrink-0">
                        <Badge variant={statusVariant(inst.status)}>{inst.status}</Badge>
                        {inst.currentStage && (
                          <>
                            {(() => {
                              const stageId = inst.currentStage;
                              return (
                                <>
                                  <Button
                                    variant="secondary"
                                    onClick={() =>
                                      approveMutation.mutate({
                                        instanceId: inst.id,
                                        stageId,
                                      })
                                    }
                                    loading={approveMutation.isPending}
                                  >
                                    {t('workflow.approve')}
                                  </Button>
                                  <Button
                                    variant="danger"
                                    onClick={() =>
                                      setRejectTarget({ instanceId: inst.id, stageId })
                                    }
                                  >
                                    {t('workflow.reject')}
                                  </Button>
                                </>
                              );
                            })()}
                          </>
                        )}
                      </div>
                    </div>

                    {/* Reject form */}
                    {rejectTarget?.instanceId === inst.id && (
                      <div className="mt-3 space-y-2">
                        <textarea
                          value={rejectReason}
                          onChange={(e) => setRejectReason(e.target.value)}
                          placeholder={t('workflow.rejectPlaceholder')}
                          rows={2}
                          className="w-full rounded-md bg-canvas border border-edge px-3 py-2 text-sm text-heading placeholder:text-muted focus:outline-none focus:ring-2 focus:ring-critical focus:border-critical transition-colors"
                        />
                        <div className="flex gap-2 justify-end">
                          <Button
                            variant="secondary"
                            onClick={() => {
                              setRejectTarget(null);
                              setRejectReason('');
                            }}
                          >
                            {t('common.cancel')}
                          </Button>
                          <Button
                            variant="danger"
                            disabled={!rejectReason.trim()}
                            loading={rejectMutation.isPending}
                            onClick={() => {
                              if (!rejectTarget) {
                                return;
                              }

                              rejectMutation.mutate({
                                instanceId: rejectTarget.instanceId,
                                stageId: rejectTarget.stageId,
                                reason: rejectReason,
                              });
                            }}
                          >
                            {t('workflow.confirmReject')}
                          </Button>
                        </div>
                      </div>
                    )}
                  </li>
                ))}
              </ul>
            )}
          </CardBody>
        </Card>

        {/* Workflow Templates */}
        <Card>
          <CardHeader>
            <div className="flex items-center gap-2">
              <CheckSquare size={16} className="text-muted" />
              <h2 className="font-semibold text-heading">{t('workflow.workflowTemplates')}</h2>
            </div>
          </CardHeader>
          <CardBody className="p-0">
            {templatesLoading ? (
              <PageLoadingState size="sm" />
            ) : templates?.length ? (
              <ul className="divide-y divide-edge">
                {templates.map((tmpl) => (
                  <li key={tmpl.id} className="px-6 py-3 flex items-center justify-between hover:bg-hover transition-colors">
                    <div>
                      <p className="text-sm font-medium text-body">{tmpl.name}</p>
                      <p className="text-xs text-muted">
                        {t('workflow.stageCount', { count: tmpl.stages.length })}
                      </p>
                    </div>
                    <Badge variant={TEMPLATE_LEVEL_VARIANTS[tmpl.changeLevel] ?? 'default'}>
                      {t(TEMPLATE_LEVEL_KEYS[tmpl.changeLevel] ?? 'releases.changeLevels.unknown')}
                    </Badge>
                  </li>
                ))}
              </ul>
            ) : (
              <ul className="divide-y divide-edge">
                {[
                  { key: 'workflow.defaultTemplates.standard', level: 1 },
                  { key: 'workflow.defaultTemplates.breakingChange', level: 3 },
                  { key: 'workflow.defaultTemplates.hotfix', level: 0 },
                ].map(({ key, level }) => (
                  <li key={key} className="px-6 py-3 flex items-center justify-between hover:bg-hover transition-colors">
                    <span className="text-sm text-body">{t(key)}</span>
                    <Badge variant={TEMPLATE_LEVEL_VARIANTS[level] ?? 'default'}>
                      {t(TEMPLATE_LEVEL_KEYS[level] ?? 'releases.changeLevels.unknown')}
                    </Badge>
                  </li>
                ))}
              </ul>
            )}
          </CardBody>
        </Card>
      </div>

      {/* All Instances Table */}
      {instances && instances.items.length > 0 && (
        <Card className="mt-6">
          <CardHeader>
            <h2 className="font-semibold text-heading">{t('workflow.allInstances')}</h2>
          </CardHeader>
          <div className="overflow-x-auto">
            <table className="min-w-full text-sm">
              <thead>
                <tr className="border-b border-edge bg-panel text-left">
                   <th className="px-6 py-3 font-medium text-muted">{t('workflow.instanceId')}</th>
                  <th className="px-6 py-3 font-medium text-muted">{t('workflow.releaseId')}</th>
                  <th className="px-6 py-3 font-medium text-muted">{t('workflow.status')}</th>
                  <th className="px-6 py-3 font-medium text-muted">{t('workflow.currentStage')}</th>
                  <th className="px-6 py-3 font-medium text-muted">{t('workflow.created')}</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-edge">
                {instances.items.map((inst) => (
                  <tr key={inst.id} className="hover:bg-hover transition-colors">
                    <td className="px-6 py-3 font-mono text-xs text-body">{inst.id.slice(0, 8)}…</td>
                    <td className="px-6 py-3 font-mono text-xs text-body">{(inst.releaseId ?? '—').slice(0, 8)}{inst.releaseId ? '…' : ''}</td>
                    <td className="px-6 py-3">
                      <Badge variant={statusVariant(inst.status)}>{inst.status}</Badge>
                    </td>
                    <td className="px-6 py-3 text-body">{inst.currentStage ?? '—'}</td>
                    <td className="px-6 py-3 text-xs text-muted">
                      {new Date(inst.createdAt).toLocaleString()}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </Card>
      )}
    </PageContainer>
  );
}
