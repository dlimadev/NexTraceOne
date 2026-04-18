import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { ClipboardCheck, Clock, CheckCircle, XCircle, DollarSign, ChevronDown, ChevronUp } from 'lucide-react';
import { Card, CardBody, CardHeader } from '../../../components/Card';
import { Badge } from '../../../components/Badge';
import { PageLoadingState } from '../../../components/PageLoadingState';
import { PageErrorState } from '../../../components/PageErrorState';
import { PageContainer } from '../../../components/shell';
import { PageHeader } from '../../../components/PageHeader';
import { useEnvironment } from '../../../contexts/EnvironmentContext';
import { finOpsApi, type FinOpsBudgetApprovalDto } from '../api/finOps';
import { queryKeys } from '../../../shared/api/queryKeys';
import { useFinOpsCurrency } from '../hooks/useFinOpsConfig';
import { formatCurrency } from '../utils/finOpsFormatters';

type StatusFilter = 'all' | 'Pending' | 'Approved' | 'Rejected';

export function FinOpsBudgetApprovalsPage() {
  const { t, i18n } = useTranslation();
  const { activeEnvironmentId: _env } = useEnvironment();
  const qc = useQueryClient();
  const currency = useFinOpsCurrency();
  const fmt = (v: number) => formatCurrency(v, i18n.language, currency);

  const [statusFilter, setStatusFilter] = useState<StatusFilter>('all');
  const [expandedId, setExpandedId] = useState<string | null>(null);
  const [commentMap, setCommentMap] = useState<Record<string, string>>({});

  const { data, isLoading, isError, refetch } = useQuery({
    queryKey: queryKeys.governance.finops.budgetApprovals(
      statusFilter === 'all' ? undefined : statusFilter,
    ),
    queryFn: () => finOpsApi.getBudgetApprovals(
      statusFilter === 'all' ? undefined : { status: statusFilter },
    ),
    staleTime: 15_000,
  });

  const resolveMutation = useMutation({
    mutationFn: ({ approvalId, approved, comment }: { approvalId: string; approved: boolean; comment?: string }) =>
      finOpsApi.resolveApproval(approvalId, { approved, resolvedBy: 'current-user', comment }),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: queryKeys.governance.finops.all() });
    },
  });

  const statusBadge = (s: FinOpsBudgetApprovalDto['status']) => {
    if (s === 'Approved') return <Badge variant="success" size="sm">{t('finops.approvals.status.approved')}</Badge>;
    if (s === 'Rejected') return <Badge variant="danger" size="sm">{t('finops.approvals.status.rejected')}</Badge>;
    return <Badge variant="warning" size="sm">{t('finops.approvals.status.pending')}</Badge>;
  };

  const deltaVariant = (pct: number) => pct > 50 ? 'danger' : pct > 20 ? 'warning' : 'default';

  if (isLoading) return <PageLoadingState />;
  if (isError) return <PageErrorState onRetry={refetch} />;

  const items = data?.items ?? [];

  return (
    <PageContainer>
      <PageHeader
        icon={<ClipboardCheck size={22} />}
        title={t('finops.approvals.title')}
        subtitle={t('finops.approvals.subtitle')}
      />

      {/* ── Status filter ───────────────────────────────────── */}
      <div className="flex items-center gap-2 mb-6">
        {(['all', 'Pending', 'Approved', 'Rejected'] as StatusFilter[]).map((s) => (
          <button
            key={s}
            onClick={() => setStatusFilter(s)}
            className={`px-3 py-1.5 text-xs rounded-md border transition-colors ${statusFilter === s ? 'bg-accent text-white border-accent' : 'border-border hover:border-accent/50'}`}
          >
            {s === 'all' ? t('common.all') : t(`finops.approvals.status.${s.toLowerCase()}`)}
          </button>
        ))}
        <span className="ml-auto text-xs text-muted">{items.length} {t('finops.approvals.results')}</span>
      </div>

      {/* ── Empty state ───────────────────────────────────── */}
      {items.length === 0 && (
        <div className="text-center py-16 text-muted">
          <ClipboardCheck size={40} className="mx-auto mb-3 opacity-30" />
          <p className="text-sm">{t('finops.approvals.empty')}</p>
        </div>
      )}

      {/* ── List ─────────────────────────────────────────── */}
      <div className="space-y-3">
        {items.map((item) => (
          <Card key={item.approvalId}>
            <CardHeader>
              <div className="flex items-center gap-3 flex-wrap">
                <button
                  onClick={() => setExpandedId(expandedId === item.approvalId ? null : item.approvalId)}
                  className="flex items-center gap-2 flex-1 text-left"
                  aria-expanded={expandedId === item.approvalId}
                >
                  <DollarSign size={14} className="text-accent flex-shrink-0" />
                  <span className="font-semibold text-sm">{item.serviceName}</span>
                  <span className="text-xs text-muted">/ {item.environment}</span>
                  {statusBadge(item.status)}
                  <Badge variant={deltaVariant(item.costDeltaPct)} size="sm">
                    +{item.costDeltaPct.toFixed(1)}%
                  </Badge>
                  {expandedId === item.approvalId
                    ? <ChevronUp size={14} className="ml-auto text-muted" />
                    : <ChevronDown size={14} className="ml-auto text-muted" />}
                </button>
              </div>
            </CardHeader>

            {expandedId === item.approvalId && (
              <CardBody>
                <div className="grid grid-cols-2 md:grid-cols-4 gap-4 mb-4">
                  <div>
                    <p className="text-xs text-muted">{t('finops.approvals.actualCost')}</p>
                    <p className="text-sm font-semibold text-critical">{fmt(item.actualCost)}</p>
                  </div>
                  <div>
                    <p className="text-xs text-muted">{t('finops.approvals.baselineCost')}</p>
                    <p className="text-sm font-semibold">{fmt(item.baselineCost)}</p>
                  </div>
                  <div>
                    <p className="text-xs text-muted">{t('finops.approvals.requestedBy')}</p>
                    <p className="text-sm">{item.requestedBy}</p>
                  </div>
                  <div>
                    <p className="text-xs text-muted">{t('finops.approvals.requestedAt')}</p>
                    <p className="text-sm">{new Date(item.requestedAt).toLocaleString(i18n.language)}</p>
                  </div>
                </div>

                {item.justification && (
                  <div className="mb-4 p-3 bg-surface-hover rounded text-xs">
                    <p className="text-muted mb-1">{t('finops.approvals.justification')}</p>
                    <p>{item.justification}</p>
                  </div>
                )}

                {item.status === 'Pending' && (
                  <div className="border-t border-border pt-4">
                    <p className="text-xs text-muted mb-2">{t('finops.approvals.comment')}</p>
                    <textarea
                      className="input w-full text-sm min-h-[60px] mb-3"
                      placeholder={t('finops.approvals.commentPlaceholder')}
                      value={commentMap[item.approvalId] ?? ''}
                      onChange={(e) => setCommentMap((m) => ({ ...m, [item.approvalId]: e.target.value }))}
                      aria-label={t('finops.approvals.comment')}
                    />
                    <div className="flex items-center gap-2">
                      <button
                        onClick={() => resolveMutation.mutate({ approvalId: item.approvalId, approved: true, comment: commentMap[item.approvalId] })}
                        disabled={resolveMutation.isPending}
                        className="btn btn-primary btn-sm flex items-center gap-1"
                      >
                        <CheckCircle size={14} />
                        {t('finops.approvals.approve')}
                      </button>
                      <button
                        onClick={() => resolveMutation.mutate({ approvalId: item.approvalId, approved: false, comment: commentMap[item.approvalId] })}
                        disabled={resolveMutation.isPending}
                        className="btn btn-danger btn-sm flex items-center gap-1"
                      >
                        <XCircle size={14} />
                        {t('finops.approvals.reject')}
                      </button>
                    </div>
                  </div>
                )}

                {item.status !== 'Pending' && item.resolvedBy && (
                  <div className="border-t border-border pt-4 flex items-start gap-3">
                    {item.status === 'Approved'
                      ? <CheckCircle size={14} className="text-success mt-0.5" />
                      : <XCircle size={14} className="text-critical mt-0.5" />}
                    <div>
                      <p className="text-xs text-muted">
                        {t(`finops.approvals.status.${item.status.toLowerCase()}`)}{' '}
                        {t('common.by')} {item.resolvedBy}
                        {item.resolvedAt && ` • ${new Date(item.resolvedAt).toLocaleString(i18n.language)}`}
                      </p>
                      {item.comment && <p className="text-sm mt-1">{item.comment}</p>}
                    </div>
                  </div>
                )}
              </CardBody>
            )}
          </Card>
        ))}
      </div>
    </PageContainer>
  );
}
