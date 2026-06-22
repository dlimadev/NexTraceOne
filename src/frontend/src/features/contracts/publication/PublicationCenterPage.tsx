import { useState } from 'react';
import { Link } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import {
  EyeOff, AlertTriangle, Clock,
  CheckCircle2, XCircle, Plus,
} from 'lucide-react';
import { Card, CardBody, CardHeader } from '../../../components/Card';
import { EmptyState } from '../../../components/EmptyState';
import { PageContainer } from '../../../components/shell';
import { PageErrorState } from '../../../components/PageErrorState';
import { LoadingState } from '../shared/components/StateIndicators';
import { PageHeader } from '../../../components/PageHeader';
import { Button, TextField } from '../../../shared/ui';
import {
  usePublicationCenterEntries,
  useWithdrawContractFromPortal,
} from '../hooks/usePublicationCenter';
import type { ContractPublicationEntry, ContractPublicationStatus } from '../../../types';
import { cn } from '../../../lib/cn';

/**
 * Publication Center — página de gestão de visibilidade de contratos no Developer Portal.
 * Mostra todas as entradas de publicação (publicadas, pendentes, retiradas, deprecated).
 * Permite retirar publicações ativas com motivo opcional.
 * Segmentação: TechLead / Platform Admin (contracts:write + developer-portal:read).
 */
export function PublicationCenterPage() {
  const { t } = useTranslation();
  const [statusFilter, setStatusFilter] = useState<string | undefined>();
  const [withdrawTarget, setWithdrawTarget] = useState<ContractPublicationEntry | null>(null);
  const [withdrawReason, setWithdrawReason] = useState('');

  const entriesQuery = usePublicationCenterEntries({ status: statusFilter });
  const withdrawMutation = useWithdrawContractFromPortal();

  const handleWithdraw = async (entry: ContractPublicationEntry) => {
    await withdrawMutation.mutateAsync({
      entryId: entry.publicationEntryId,
      reason: withdrawReason.trim() || undefined,
    });
    setWithdrawTarget(null);
    setWithdrawReason('');
  };

  const filterOptions: Array<{ value: string | undefined; label: string }> = [
    { value: undefined, label: t('contracts.publication.filter.all', 'All') },
    { value: 'Published', label: t('contracts.publication.filter.published', 'Published') },
    { value: 'PendingPublication', label: t('contracts.publication.filter.pending', 'Pending') },
    { value: 'Withdrawn', label: t('contracts.publication.filter.withdrawn', 'Withdrawn') },
    { value: 'Deprecated', label: t('contracts.publication.filter.deprecated', 'Deprecated') },
  ];

  return (
    <PageContainer>
      <PageHeader
        title={t('contracts.publication.title', 'Publication Center')}
        subtitle={t(
          'contracts.publication.subtitle',
          'Manage which contract versions are visible in the Developer Portal. Only Approved or Locked contracts can be published.',
        )}
        actions={
          <Link to="/contracts/studio/new">
            <Button variant="primary" size="sm" icon={<Plus size={14} />}>
              {t('contracts.publication.newContract', 'New Contract')}
            </Button>
          </Link>
        }
      />

      {/* Status filter tabs */}
      <div className="flex gap-1 mb-4">
        {filterOptions.map((opt) => (
          <Button
            key={opt.value ?? 'all'}
            variant={statusFilter === opt.value ? 'primary' : 'outline'}
            size="xs"
            onClick={() => setStatusFilter(opt.value)}
          >
            {opt.label}
          </Button>
        ))}
      </div>

      {/* Entries list */}
      {entriesQuery.isError ? (
        <PageErrorState onRetry={() => entriesQuery.refetch()} />
      ) : entriesQuery.isLoading ? (
        <LoadingState />
      ) : !entriesQuery.data?.items.length ? (
        <EmptyState
          title={t('contracts.publication.empty.title', 'No publications found')}
          description={t(
            'contracts.publication.empty.description',
            'No contracts have been published to the Developer Portal yet.',
          )}
        />
      ) : (
        <Card>
          <CardHeader>
            <span className="text-xs text-muted">
              {entriesQuery.data.totalCount}{' '}
              {t('contracts.publication.entries', 'entries')}
            </span>
          </CardHeader>
          <CardBody className="p-0">
            <table className="w-full text-xs">
              <thead>
                <tr className="border-b border-edge">
                  <th className="text-left px-4 py-2 text-muted font-medium">
                    {t('contracts.publication.col.contract', 'Contract')}
                  </th>
                  <th className="text-left px-4 py-2 text-muted font-medium">
                    {t('contracts.publication.col.version', 'Version')}
                  </th>
                  <th className="text-left px-4 py-2 text-muted font-medium">
                    {t('contracts.publication.col.status', 'Status')}
                  </th>
                  <th className="text-left px-4 py-2 text-muted font-medium">
                    {t('contracts.publication.col.visibility', 'Visibility')}
                  </th>
                  <th className="text-left px-4 py-2 text-muted font-medium">
                    {t('contracts.publication.col.publishedBy', 'Published By')}
                  </th>
                  <th className="text-right px-4 py-2 text-muted font-medium">
                    {t('contracts.publication.col.actions', 'Actions')}
                  </th>
                </tr>
              </thead>
              <tbody>
                {entriesQuery.data.items.map((entry) => (
                  <tr key={entry.publicationEntryId} className="border-b border-edge last:border-0 hover:bg-card/50">
                    <td className="px-4 py-3 font-medium text-accent">{entry.contractTitle}</td>
                    <td className="px-4 py-3 text-muted font-mono">{entry.semVer}</td>
                    <td className="px-4 py-3">
                      <PublicationStatusBadge status={entry.status} />
                    </td>
                    <td className="px-4 py-3 text-muted">{entry.visibility}</td>
                    <td className="px-4 py-3 text-muted">{entry.publishedBy}</td>
                    <td className="px-4 py-3 text-right">
                      {entry.status === 'Published' && (
                        <Button
                          variant="outline"
                          size="xs"
                          icon={<EyeOff size={11} />}
                          onClick={() => setWithdrawTarget(entry)}
                        >
                          {t('contracts.publication.withdraw', 'Withdraw')}
                        </Button>
                      )}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </CardBody>
        </Card>
      )}
      {withdrawTarget && (
        <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50">
          <div className="bg-card rounded-lg border border-edge p-6 w-full max-w-md">
            <div className="flex items-center gap-2 mb-3">
              <AlertTriangle size={16} className="text-warning" />
              <h2 className="text-sm font-semibold text-accent">
                {t('contracts.publication.withdrawModal.title', 'Withdraw Publication')}
              </h2>
            </div>
            <p className="text-xs text-muted mb-4">
              {t(
                'contracts.publication.withdrawModal.description',
                'Contract {{title}} (v{{version}}) will be removed from the Developer Portal.',
                {
                  title: withdrawTarget.contractTitle,
                  version: withdrawTarget.semVer,
                },
              )}
            </p>
            <div className="mb-4">
              <TextField
                label={t('contracts.publication.withdrawModal.reason', 'Reason (optional)')}
                value={withdrawReason}
                onChange={(e) => setWithdrawReason(e.target.value)}
                placeholder={t('contracts.publication.withdrawModal.reasonPlaceholder', 'e.g. Replaced by v2.0.0')}
                size="sm"
              />
            </div>
            <div className="flex justify-end gap-2">
              <Button
                variant="outline"
                size="sm"
                onClick={() => { setWithdrawTarget(null); setWithdrawReason(''); }}
              >
                {t('common.cancel', 'Cancel')}
              </Button>
              <Button
                variant="danger"
                size="sm"
                loading={withdrawMutation.isPending}
                disabled={withdrawMutation.isPending}
                onClick={() => handleWithdraw(withdrawTarget)}
              >
                {t('contracts.publication.withdrawModal.confirm', 'Withdraw')}
              </Button>
            </div>
          </div>
        </div>
      )}
    </PageContainer>
  );
}

function PublicationStatusBadge({ status }: { status: ContractPublicationStatus }) {
  const { t } = useTranslation();

  const config: Record<ContractPublicationStatus, { label: string; icon: React.ReactNode; cls: string }> = {
    Published: { label: t('contracts.publication.status.published', 'Published'), icon: <CheckCircle2 size={11} />, cls: 'text-success' },
    PendingPublication: { label: t('contracts.publication.status.pending', 'Pending'), icon: <Clock size={11} />, cls: 'text-accent' },
    Withdrawn: { label: t('contracts.publication.status.withdrawn', 'Withdrawn'), icon: <EyeOff size={11} />, cls: 'text-muted' },
    Deprecated: { label: t('contracts.publication.status.deprecated', 'Deprecated'), icon: <AlertTriangle size={11} />, cls: 'text-warning' },
    NotPublished: { label: t('contracts.publication.status.notPublished', 'Not Published'), icon: <XCircle size={11} />, cls: 'text-muted' },
  };

  const { label, icon, cls } = config[status] ?? config.NotPublished;

  return (
    <span className={cn('inline-flex items-center gap-1 font-medium', cls)}>
      {icon}
      {label}
    </span>
  );
}
