import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { ClipboardList, Search, Filter, Clock, User, Hash } from 'lucide-react';
import { cn } from '../../../../lib/cn';
import { Card, CardBody, CardHeader } from '../../../../components/Card';
import { EmptyState } from '../../../../components/EmptyState';
import type { AuditEntry } from '../../types/domain';

interface AuditSectionProps {
  contractVersionId?: string;
  className?: string;
}

/** Mock audit entries para demonstração. */
const MOCK_AUDIT: AuditEntry[] = [
  {
    id: 'aud-1',
    action: 'contract.version.created',
    performedBy: 'john.silva',
    performedAt: new Date(Date.now() - 86400000 * 1).toISOString(),
    details: 'Version 1.2.0 created from visual builder',
    correlationId: 'corr-abc-123',
  },
  {
    id: 'aud-2',
    action: 'contract.lifecycle.transitioned',
    performedBy: 'maria.santos',
    performedAt: new Date(Date.now() - 86400000 * 2).toISOString(),
    details: 'State changed from Draft to InReview',
    correlationId: 'corr-def-456',
  },
  {
    id: 'aud-3',
    action: 'contract.approval.approved',
    performedBy: 'carlos.mendes',
    performedAt: new Date(Date.now() - 86400000 * 3).toISOString(),
    details: 'Architecture approval granted',
    correlationId: 'corr-ghi-789',
  },
  {
    id: 'aud-4',
    action: 'contract.spec.updated',
    performedBy: 'john.silva',
    performedAt: new Date(Date.now() - 86400000 * 4).toISOString(),
    details: 'Specification content modified via source editor',
    correlationId: 'corr-jkl-012',
  },
  {
    id: 'aud-5',
    action: 'contract.policy.checked',
    performedBy: 'system',
    performedAt: new Date(Date.now() - 86400000 * 5).toISOString(),
    details: 'Automated policy check completed: 4/5 passed',
    correlationId: 'corr-mno-345',
  },
  {
    id: 'aud-6',
    action: 'contract.version.locked',
    performedBy: 'ana.oliveira',
    performedAt: new Date(Date.now() - 86400000 * 7).toISOString(),
    details: 'Version locked for production release',
    correlationId: 'corr-pqr-678',
  },
  {
    id: 'aud-7',
    action: 'contract.signature.signed',
    performedBy: 'pedro.costa',
    performedAt: new Date(Date.now() - 86400000 * 8).toISOString(),
    details: 'Contract signed with SHA-256 fingerprint',
    correlationId: 'corr-stu-901',
  },
];

const ACTION_COLORS: Record<string, string> = {
  'contract.version.created': 'text-mint',
  'contract.lifecycle.transitioned': 'text-cyan',
  'contract.approval.approved': 'text-mint',
  'contract.spec.updated': 'text-accent',
  'contract.policy.checked': 'text-warning',
  'contract.version.locked': 'text-accent',
  'contract.signature.signed': 'text-mint',
};

/**
 * Secção de Audit do studio — trilha de auditoria completa.
 * Mostra todas as acções realizadas sobre o contrato/versão,
 * com actor, timestamp, correlationId e detalhes.
 */
export function AuditSection({ className = '' }: AuditSectionProps) {
  const { t } = useTranslation();
  const [search, setSearch] = useState('');
  const [filterAction, setFilterAction] = useState<string>('all');

  const uniqueActions = [...new Set(MOCK_AUDIT.map((a) => a.action))];

  const filtered = MOCK_AUDIT.filter((entry) => {
    if (filterAction !== 'all' && entry.action !== filterAction) return false;
    if (!search.trim()) return true;
    const q = search.toLowerCase();
    return (
      entry.action.toLowerCase().includes(q) ||
      entry.performedBy.toLowerCase().includes(q) ||
      entry.details?.toLowerCase().includes(q) ||
      entry.correlationId?.toLowerCase().includes(q)
    );
  });

  return (
    <div className={`space-y-4 ${className}`}>
      {/* ── Header ── */}
      <div className="flex items-center justify-between flex-wrap gap-2">
        <div className="flex items-center gap-2">
          <ClipboardList size={14} className="text-accent" />
          <h3 className="text-xs font-semibold text-heading">
            {t('contracts.studio.audit.title', 'Audit Trail')}
          </h3>
          <span className="text-[10px] text-muted px-2 py-0.5 rounded-full bg-elevated border border-edge">
            {MOCK_AUDIT.length} {t('contracts.studio.audit.entries', 'entries')}
          </span>
        </div>

        <div className="flex items-center gap-2">
          <select
            value={filterAction}
            onChange={(e) => setFilterAction(e.target.value)}
            className="text-xs bg-elevated border border-edge rounded px-2 py-1.5 text-body focus:outline-none focus:ring-1 focus:ring-accent"
          >
            <option value="all">{t('contracts.studio.audit.allActions', 'All actions')}</option>
            {uniqueActions.map((action) => (
              <option key={action} value={action}>{action.split('.').pop()}</option>
            ))}
          </select>

          <div className="relative">
            <Search size={12} className="absolute left-2 top-1/2 -translate-y-1/2 text-muted" />
            <input
              type="text"
              value={search}
              onChange={(e) => setSearch(e.target.value)}
              placeholder={t('contracts.studio.audit.searchPlaceholder', 'Search audit log...')}
              className="text-xs bg-elevated border border-edge rounded pl-7 pr-2 py-1.5 text-body placeholder:text-muted/40 focus:outline-none focus:ring-1 focus:ring-accent w-48"
            />
          </div>
        </div>
      </div>

      {/* ── Audit Log ── */}
      {filtered.length === 0 ? (
        <EmptyState
          title={t('contracts.studio.audit.emptyTitle', 'No audit entries')}
          description={t('contracts.studio.audit.emptyDescription', 'Audit trail entries will appear here as actions are performed on this contract.')}
          icon={<ClipboardList size={24} />}
        />
      ) : (
        <Card>
          <CardBody className="p-0">
            <div className="divide-y divide-edge">
              {filtered.map((entry) => {
                const actionColor = ACTION_COLORS[entry.action] ?? 'text-body';
                const dotColor = actionColor.replace('text-', 'bg-');

                return (
                  <div key={entry.id} className="flex items-start gap-3 px-5 py-3.5 hover:bg-elevated/30 transition-colors">
                    <div className={cn(
                      'w-2 h-2 rounded-full mt-1.5 flex-shrink-0',
                      dotColor || 'bg-muted',
                    )} />

                    <div className="flex-1 min-w-0">
                      <div className="flex items-center gap-2 mb-0.5">
                        <span className={cn(
                          'text-xs font-medium',
                          actionColor,
                        )}>
                          {entry.action}
                        </span>
                      </div>

                      {entry.details && (
                        <p className="text-xs text-body mb-1">{entry.details}</p>
                      )}

                      <div className="flex items-center gap-3 flex-wrap">
                        <span className="inline-flex items-center gap-1 text-[10px] text-muted">
                          <User size={9} /> {entry.performedBy}
                        </span>
                        <span className="inline-flex items-center gap-1 text-[10px] text-muted">
                          <Clock size={9} /> {formatDateTime(entry.performedAt)}
                        </span>
                        {entry.correlationId && (
                          <span className="inline-flex items-center gap-1 text-[10px] text-muted font-mono">
                            <Hash size={9} /> {entry.correlationId}
                          </span>
                        )}
                      </div>
                    </div>
                  </div>
                );
              })}
            </div>
          </CardBody>
        </Card>
      )}
    </div>
  );
}

function formatDateTime(dateStr: string): string {
  try {
    return new Date(dateStr).toLocaleString(undefined, {
      year: 'numeric',
      month: 'short',
      day: 'numeric',
      hour: '2-digit',
      minute: '2-digit',
    });
  } catch {
    return dateStr;
  }
}
