import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { GitBranch, GitCommit, CheckCircle2, AlertTriangle, RefreshCw, Code } from 'lucide-react';
import { PageContainer, PageSection } from '../../../components/shell';
import { PageHeader } from '../../../components/PageHeader';
import { Card, CardBody } from '../../../components/Card';
import { Badge } from '../../../components/Badge';
import { Button } from '../../../components/Button';
import { PageLoadingState } from '../../../components/PageLoadingState';
import client from '../../../api/client';

interface DacRepository {
  id: string;
  name: string;
  branch: string;
  lastSyncAt: string;
  syncStatus: 'Synced' | 'Pending' | 'Conflict' | 'Error';
  dashboardCount: number;
  pendingChanges: number;
}

interface DacChange {
  id: string;
  dashboardName: string;
  changeType: 'Added' | 'Modified' | 'Deleted';
  commitSha: string;
  author: string;
  committedAt: string;
  status: 'Applied' | 'Pending' | 'Failed';
}

const useDashboardsAsCode = () =>
  useQuery({
    queryKey: ['dashboards-as-code'],
    queryFn: () =>
      client
        .get<{ repositories: DacRepository[]; recentChanges: DacChange[]; isSimulated: boolean }>(
          '/api/v1/governance/dashboards-as-code',
          { params: { tenantId: 'default' } }
        )
        .then((r) => r.data),
  });

const SYNC_CONFIG = {
  Synced: { badge: 'success' as const, icon: <CheckCircle2 size={12} /> },
  Pending: { badge: 'warning' as const, icon: <RefreshCw size={12} /> },
  Conflict: { badge: 'destructive' as const, icon: <AlertTriangle size={12} /> },
  Error: { badge: 'destructive' as const, icon: <AlertTriangle size={12} /> },
};

const CHANGE_TYPE = {
  Added: 'success' as const,
  Modified: 'warning' as const,
  Deleted: 'destructive' as const,
};

export function DashboardsAsCodePage() {
  const { t } = useTranslation();
  const [activeRepo, setActiveRepo] = useState<string | null>(null);
  const qc = useQueryClient();
  const { data, isLoading } = useDashboardsAsCode();

  const syncRepo = useMutation({
    mutationFn: (id: string) =>
      client.post(`/api/v1/governance/dashboards-as-code/repositories/${id}/sync`, { tenantId: 'default' }),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['dashboards-as-code'] }),
  });

  const repos = data?.repositories ?? [];
  const changes = activeRepo
    ? (data?.recentChanges ?? []).filter((c) => c.dashboardName.startsWith(activeRepo))
    : (data?.recentChanges ?? []);

  return (
    <PageContainer>
      <PageHeader
        title={t('dacGitOps.title')}
        subtitle={t('dacGitOps.subtitle')}
      />
      <PageSection>
        <div className="grid grid-cols-1 lg:grid-cols-3 gap-4">
          {/* Repositories */}
          <div className="lg:col-span-1 space-y-2">
            <h3 className="text-sm font-semibold mb-3 flex items-center gap-2">
              <GitBranch size={14} className="text-accent" />
              {t('dacGitOps.repositories')}
            </h3>
            {isLoading ? (
              <PageLoadingState />
            ) : (
              repos.map((repo) => {
                const cfg = SYNC_CONFIG[repo.syncStatus] ?? SYNC_CONFIG.Synced;
                return (
                  <Card
                    key={repo.id}
                    className={`cursor-pointer transition-colors ${activeRepo === repo.id ? 'border-accent/60' : ''}`}
                    onClick={() => setActiveRepo(activeRepo === repo.id ? null : repo.id)}
                  >
                    <CardBody className="p-3">
                      <div className="flex items-start justify-between gap-2 mb-2">
                        <div>
                          <p className="text-sm font-semibold">{repo.name}</p>
                          <p className="text-xs font-mono text-muted-foreground">{repo.branch}</p>
                        </div>
                        <Badge variant={cfg.badge} className="flex items-center gap-1 text-xs">
                          {cfg.icon}
                          {repo.syncStatus}
                        </Badge>
                      </div>
                      <div className="flex items-center justify-between text-xs text-muted-foreground">
                        <span>{repo.dashboardCount} {t('dacGitOps.dashboards')}</span>
                        {repo.pendingChanges > 0 && (
                          <Badge variant="warning" className="text-xs">{repo.pendingChanges} pending</Badge>
                        )}
                      </div>
                      <div className="flex gap-2 mt-2">
                        <Button
                          size="sm"
                          variant="ghost"
                          className="flex-1 text-xs"
                          onClick={(e) => { e.stopPropagation(); syncRepo.mutate(repo.id); }}
                          disabled={syncRepo.isPending}
                        >
                          <RefreshCw size={10} className="mr-1" />
                          {t('dacGitOps.sync')}
                        </Button>
                      </div>
                    </CardBody>
                  </Card>
                );
              })
            )}
          </div>

          {/* Change log */}
          <div className="lg:col-span-2">
            <h3 className="text-sm font-semibold mb-3 flex items-center gap-2">
              <GitCommit size={14} className="text-muted-foreground" />
              {t('dacGitOps.recentChanges')}
            </h3>
            <div className="space-y-2">
              {changes.map((change) => (
                <Card key={change.id}>
                  <CardBody className="p-3">
                    <div className="flex items-center justify-between gap-3">
                      <div className="flex-1 min-w-0">
                        <div className="flex items-center gap-2 mb-1">
                          <Badge variant={CHANGE_TYPE[change.changeType] ?? 'secondary'} className="text-xs w-16 justify-center">
                            {change.changeType}
                          </Badge>
                          <span className="text-sm font-medium truncate">{change.dashboardName}</span>
                        </div>
                        <div className="flex items-center gap-3 text-xs text-muted-foreground">
                          <Code size={10} />
                          <span className="font-mono">{change.commitSha.slice(0, 7)}</span>
                          <span>{change.author}</span>
                          <span>{new Date(change.committedAt).toLocaleString()}</span>
                        </div>
                      </div>
                      <Badge
                        variant={change.status === 'Applied' ? 'success' : change.status === 'Failed' ? 'destructive' : 'warning'}
                        className="text-xs shrink-0"
                      >
                        {change.status}
                      </Badge>
                    </div>
                  </CardBody>
                </Card>
              ))}
              {changes.length === 0 && (
                <div className="text-center p-8 text-muted-foreground text-sm">
                  {t('dacGitOps.noChanges')}
                </div>
              )}
            </div>
          </div>
        </div>

        <div className="mt-4 p-3 rounded-lg bg-muted/40 text-xs text-muted-foreground">
          {t('sotCenter.simulatedBanner')}
        </div>
      </PageSection>
    </PageContainer>
  );
}
