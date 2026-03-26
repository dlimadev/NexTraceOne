import { useState } from 'react';
import { Link } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import {
  Globe, Eye, EyeOff, AlertTriangle, Clock,
  CheckCircle2, XCircle, RefreshCw, Plus,
} from 'lucide-react';
import { Card, CardBody, CardHeader } from '../../../components/Card';
import { EmptyState } from '../../../components/EmptyState';
import { PageContainer } from '../../../components/shell';
import { LoadingState } from '../shared/components/StateIndicators';
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
      <div className="flex items-center justify-between mb-4">
        <div className="flex items-center gap-2">
          <Globe size={16} className="text-accent" />
          <h1 className="text-sm font-semibold text-primary">
            {t('contracts.publication.title', 'Publication Center')}
          </h1>
        </div>
        <Link
          to="/contracts/studio/new"
          className="inline-flex items-center gap-1.5 px-3 py-1.5 text-xs font-medium rounded bg-accent text-bg hover:bg-accent/90 transition-colors"
        >
          <Plus size={12} />
          {t('contracts.publication.newContract', 'New Contract')}
        </Link>
      </div>

      <p className="text-xs text-muted mb-4">
        {t(
          'contracts.publication.subtitle',
          'Manage which contract versions are visible in the Developer Portal. Only Approved or Locked contracts can be published.',
        )}
      </p>

      {/* Status filter tabs */}
      <div className="flex gap-1 mb-4">
        {filterOptions.map((opt) => (
          <button
            key={opt.value ?? 'all'}
            type="button"
            onClick={() => setStatusFilter(opt.value)}
            className={cn(
              'px-3 py-1 rounded text-xs font-medium transition-colors',
              statusFilter === opt.value
                ? 'bg-accent text-bg'
                : 'bg-card text-muted hover:text-primary border border-edge',
            )}
          >
            {opt.label}
          </button>
        ))}
      </div>

      {/* Entries list */}
      {entriesQuery.isLoading ? (
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
                    <td className="px-4 py-3 font-medium text-primary">{entry.contractTitle}</td>
                    <td className="px-4 py-3 text-muted font-mono">{entry.semVer}</td>
                    <td className="px-4 py-3">
                      <PublicationStatusBadge status={entry.status} />
                    </td>
                    <td className="px-4 py-3 text-muted">{entry.visibility}</td>
                    <td className="px-4 py-3 text-muted">{entry.publishedBy}</td>
                    <td className="px-4 py-3 text-right">
                      {entry.status === 'Published' && (
                        <button
                          type="button"
                          onClick={() => setWithdrawTarget(entry)}
                          className="inline-flex items-center gap-1 px-2 py-1 text-xs rounded border border-edge text-muted hover:text-primary hover:border-accent transition-colors"
                        >
                          <EyeOff size={11} />
                          {t('contracts.publication.withdraw', 'Withdraw')}
                        </button>
                      )}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </CardBody>
        </Card>
      )}

      {/* Withdraw confirmation dialog */}
      {withdrawTarget && (
        <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50">
          <div className="bg-card rounded-lg border border-edge p-6 w-full max-w-md">
            <div className="flex items-center gap-2 mb-3">
              <AlertTriangle size={16} className="text-warning" />
              <h2 className="text-sm font-semibold text-primary">
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
            <label className="block text-xs font-medium text-muted mb-1">
              {t('contracts.publication.withdrawModal.reason', 'Reason (optional)')}
            </label>
            <input
              type="text"
              value={withdrawReason}
              onChange={(e) => setWithdrawReason(e.target.value)}
              placeholder={t('contracts.publication.withdrawModal.reasonPlaceholder', 'e.g. Replaced by v2.0.0')}
              className="w-full px-3 py-2 text-xs rounded border border-edge bg-bg text-primary mb-4"
            />
            <div className="flex justify-end gap-2">
              <button
                type="button"
                onClick={() => { setWithdrawTarget(null); setWithdrawReason(''); }}
                className="px-3 py-1.5 text-xs rounded border border-edge text-muted hover:text-primary transition-colors"
              >
                {t('common.cancel', 'Cancel')}
              </button>
              <button
                type="button"
                onClick={() => handleWithdraw(withdrawTarget)}
                disabled={withdrawMutation.isPending}
                className="px-3 py-1.5 text-xs rounded bg-accent text-bg hover:bg-accent/90 transition-colors disabled:opacity-50 flex items-center gap-1"
              >
                {withdrawMutation.isPending && <RefreshCw size={11} className="animate-spin" />}
                {t('contracts.publication.withdrawModal.confirm', 'Withdraw')}
              </button>
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
    Published: { label: t('contracts.publication.status.published', 'Published'), icon: <CheckCircle2 size={11} />, cls: 'text-green' },
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
