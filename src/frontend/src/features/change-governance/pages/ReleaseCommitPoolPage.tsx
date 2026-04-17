import { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { useTranslation } from 'react-i18next';
import { GitCommit, Plus, Trash2, Tag } from 'lucide-react';
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

function statusVariant(
  status: string,
): 'default' | 'info' | 'success' | 'warning' | 'danger' {
  switch (status) {
    case 'Included':
      return 'success';
    case 'Candidate':
      return 'info';
    case 'Excluded':
      return 'danger';
    default:
      return 'default';
  }
}

/**
 * ReleaseCommitPoolPage — gestão do commit pool de uma release.
 *
 * Permite ao PO/PM visualizar os commits associados a uma release,
 * ingerir novos commits do CI/CD e gerir os work items associados.
 * Inclui tabs: Commits | Work Items.
 */
export function ReleaseCommitPoolPage() {
  const { t } = useTranslation();
  const { activeEnvironmentId } = useEnvironment();
  const queryClient = useQueryClient();
  const [releaseId, setReleaseId] = useState('');
  const [activeTab, setActiveTab] = useState<'commits' | 'workitems'>('commits');

  // Work item form
  const [wiExternalId, setWiExternalId] = useState('');
  const [wiSystem, setWiSystem] = useState('Jira');
  const [wiTitle, setWiTitle] = useState('');
  const [wiType, setWiType] = useState('Story');

  const commitsQuery = useQuery({
    queryKey: ['release-commits', releaseId, activeEnvironmentId],
    queryFn: () => changeIntelligenceApi.listCommitsByRelease(releaseId),
    enabled: !!releaseId,
  });

  const workItemsQuery = useQuery({
    queryKey: ['release-work-items', releaseId, activeEnvironmentId],
    queryFn: () => changeIntelligenceApi.listWorkItemsByRelease(releaseId),
    enabled: !!releaseId,
  });

  const addWorkItemMutation = useMutation({
    mutationFn: () =>
      changeIntelligenceApi.addWorkItemToRelease(releaseId, {
        externalWorkItemId: wiExternalId,
        externalSystem: wiSystem,
        title: wiTitle,
        workItemType: wiType,
      }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['release-work-items', releaseId] });
      setWiExternalId('');
      setWiTitle('');
    },
  });

  const removeWorkItemMutation = useMutation({
    mutationFn: (workItemAssociationId: string) =>
      changeIntelligenceApi.removeWorkItemFromRelease(workItemAssociationId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['release-work-items', releaseId] });
    },
  });

  const commits = commitsQuery.data?.commits ?? [];
  const workItems = workItemsQuery.data?.workItems ?? [];

  return (
    <PageContainer>
      <PageHeader
        title={t('commitPool.title')}
        subtitle={t('commitPool.subtitle')}
      />

      {/* Release ID input */}
      <Card className="mb-6">
        <CardBody>
          <div className="flex gap-3 items-end">
            <div className="flex-1">
              <label className="block text-xs font-medium text-muted mb-1">
                {t('commitPool.releaseIdLabel')}
              </label>
              <input
                type="text"
                value={releaseId}
                onChange={(e) => setReleaseId(e.target.value)}
                placeholder={t('commitPool.releaseIdPlaceholder')}
                className={INPUT_CLS}
              />
            </div>
          </div>
        </CardBody>
      </Card>

      {/* Tabs */}
      <div className="flex gap-2 mb-4 border-b border-edge">
        <button
          onClick={() => setActiveTab('commits')}
          className={`px-4 py-2 text-sm font-medium transition-colors ${
            activeTab === 'commits'
              ? 'border-b-2 border-accent text-accent'
              : 'text-muted hover:text-heading'
          }`}
        >
          <span className="flex items-center gap-2">
            <GitCommit className="w-4 h-4" />
            {t('commitPool.tabCommits')}
          </span>
        </button>
        <button
          onClick={() => setActiveTab('workitems')}
          className={`px-4 py-2 text-sm font-medium transition-colors ${
            activeTab === 'workitems'
              ? 'border-b-2 border-accent text-accent'
              : 'text-muted hover:text-heading'
          }`}
        >
          <span className="flex items-center gap-2">
            <Tag className="w-4 h-4" />
            {t('commitPool.tabWorkItems')}
          </span>
        </button>
      </div>

      {activeTab === 'commits' && (
        <>
          {commitsQuery.isLoading && <PageLoadingState />}
          {commitsQuery.isError && <PageErrorState />}
          {!commitsQuery.isLoading && !commitsQuery.isError && (
            <Card>
              <CardHeader>
                <h2 className="text-sm font-semibold text-heading">
                  {t('commitPool.commitsCount', { count: commits.length })}
                </h2>
              </CardHeader>
              <CardBody>
                {commits.length === 0 ? (
                  <p className="text-sm text-muted text-center py-8">
                    {t('commitPool.noCommits')}
                  </p>
                ) : (
                  <div className="space-y-3">
                    {commits.map((c) => (
                      <div
                        key={c.id}
                        className="flex items-start gap-3 p-3 rounded-md bg-surface border border-edge"
                      >
                        <GitCommit className="w-4 h-4 text-muted mt-0.5 shrink-0" />
                        <div className="flex-1 min-w-0">
                          <div className="flex items-center gap-2 flex-wrap">
                            <code className="text-xs font-mono text-accent">
                              {c.commitSha.slice(0, 8)}
                            </code>
                            <Badge variant={statusVariant(c.assignmentStatus)}>
                              {c.assignmentStatus}
                            </Badge>
                            <span className="text-xs text-muted">{c.branchName}</span>
                          </div>
                          <p className="text-sm text-heading mt-1 truncate">{c.commitMessage}</p>
                          <p className="text-xs text-muted mt-0.5">
                            {c.commitAuthor} · {new Date(c.committedAt).toLocaleString()}
                          </p>
                          {c.extractedWorkItemRefs && (
                            <p className="text-xs text-info mt-0.5">
                              {t('commitPool.workItemRefs')}: {c.extractedWorkItemRefs}
                            </p>
                          )}
                        </div>
                      </div>
                    ))}
                  </div>
                )}
              </CardBody>
            </Card>
          )}
        </>
      )}

      {activeTab === 'workitems' && (
        <>
          {/* Add work item form */}
          <Card className="mb-4">
            <CardHeader>
              <h2 className="text-sm font-semibold text-heading">
                {t('commitPool.addWorkItem')}
              </h2>
            </CardHeader>
            <CardBody>
              <div className="grid grid-cols-2 gap-3 mb-3">
                <div>
                  <label className="block text-xs font-medium text-muted mb-1">
                    {t('commitPool.wiId')}
                  </label>
                  <input
                    type="text"
                    value={wiExternalId}
                    onChange={(e) => setWiExternalId(e.target.value)}
                    placeholder="PROJ-1234"
                    className={INPUT_CLS}
                  />
                </div>
                <div>
                  <label className="block text-xs font-medium text-muted mb-1">
                    {t('commitPool.wiSystem')}
                  </label>
                  <select
                    value={wiSystem}
                    onChange={(e) => setWiSystem(e.target.value)}
                    className={INPUT_CLS}
                  >
                    <option>Jira</option>
                    <option>AzureDevOps</option>
                    <option>GitHub</option>
                    <option>Linear</option>
                    <option>Custom</option>
                  </select>
                </div>
                <div>
                  <label className="block text-xs font-medium text-muted mb-1">
                    {t('commitPool.wiTitle')}
                  </label>
                  <input
                    type="text"
                    value={wiTitle}
                    onChange={(e) => setWiTitle(e.target.value)}
                    placeholder={t('commitPool.wiTitlePlaceholder')}
                    className={INPUT_CLS}
                  />
                </div>
                <div>
                  <label className="block text-xs font-medium text-muted mb-1">
                    {t('commitPool.wiType')}
                  </label>
                  <select
                    value={wiType}
                    onChange={(e) => setWiType(e.target.value)}
                    className={INPUT_CLS}
                  >
                    <option>Story</option>
                    <option>Bug</option>
                    <option>Feature</option>
                    <option>Task</option>
                    <option>Epic</option>
                  </select>
                </div>
              </div>
              <button
                onClick={() => addWorkItemMutation.mutate()}
                disabled={!releaseId || !wiExternalId || !wiTitle || addWorkItemMutation.isPending}
                className="flex items-center gap-2 px-4 py-2 text-sm font-medium rounded-md bg-accent text-white hover:bg-accent/90 disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
              >
                <Plus className="w-4 h-4" />
                {t('commitPool.addWorkItemBtn')}
              </button>
            </CardBody>
          </Card>

          {workItemsQuery.isLoading && <PageLoadingState />}
          {workItemsQuery.isError && <PageErrorState />}
          {!workItemsQuery.isLoading && !workItemsQuery.isError && (
            <Card>
              <CardBody>
                {workItems.length === 0 ? (
                  <p className="text-sm text-muted text-center py-8">
                    {t('commitPool.noWorkItems')}
                  </p>
                ) : (
                  <div className="space-y-2">
                    {workItems.map((wi) => (
                      <div
                        key={wi.id}
                        className="flex items-center gap-3 p-3 rounded-md bg-surface border border-edge"
                      >
                        <Tag className="w-4 h-4 text-muted shrink-0" />
                        <div className="flex-1 min-w-0">
                          <div className="flex items-center gap-2">
                            <code className="text-xs font-mono text-accent">
                              {wi.externalWorkItemId}
                            </code>
                            <Badge variant="default">{wi.externalSystem}</Badge>
                            <Badge variant="info">{wi.workItemType}</Badge>
                          </div>
                          <p className="text-sm text-heading mt-0.5 truncate">{wi.title}</p>
                        </div>
                        <button
                          onClick={() => removeWorkItemMutation.mutate(wi.id)}
                          disabled={removeWorkItemMutation.isPending}
                          className="text-muted hover:text-danger transition-colors"
                          aria-label={t('commitPool.removeWorkItem')}
                        >
                          <Trash2 className="w-4 h-4" />
                        </button>
                      </div>
                    ))}
                  </div>
                )}
              </CardBody>
            </Card>
          )}
        </>
      )}
    </PageContainer>
  );
}
