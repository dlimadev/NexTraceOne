/**
 * Tabela principal do catálogo de contratos.
 *
 * Colunas com sorting, badges semânticos e menu de acções por linha.
 * Dados enriquecidos (domain, team, owner, criticality) vêm do backend real.
 * Responsive via overflow-x horizontal em ecrãs menores.
 */
import { useState, useRef, useEffect, useCallback } from 'react';
import { useTranslation } from 'react-i18next';
import { useNavigate } from 'react-router-dom';
import {
  ChevronUp,
  ChevronDown,
  ChevronsUpDown,
  MoreHorizontal,
  ExternalLink,
  Pencil,
  GitCompare,
  Plus,
  Eye,
  Send,
  CheckCircle,
  Upload,
  AlertTriangle,
  Archive,
  LayoutDashboard,
} from 'lucide-react';
import { cn } from '../../../../lib/cn';
import { LifecycleBadge, ServiceTypeBadge } from '../../shared/components';
import { ApprovalStateBadge, CriticalityBadge } from './CatalogBadges';
import type { CatalogItem } from '../types';
import type { SortConfig, SortField } from '../types';

// ── Score Badge ───────────────────────────────────────────────────────────────

/**
 * Badge de qualidade do contrato (0–100) calculado pelo scorecard.
 * Mostra "–" quando o score ainda não foi gerado (lazy computation).
 */
function ScoreBadge({ score }: { score?: number | null }) {
  const { t } = useTranslation();

  if (score == null) {
    return (
      <span className="text-[10px] text-muted/50">
        {t('contracts.catalog.score.notScored', '–')}
      </span>
    );
  }

  const pct = Math.round(score * 100);
  const color =
    pct >= 80 ? 'text-mint border-mint/40 bg-mint/5' :
    pct >= 60 ? 'text-cyan border-cyan/40 bg-cyan/5' :
    pct >= 40 ? 'text-amber border-amber/40 bg-amber/5' :
               'text-error border-error/40 bg-error/5';

  return (
    <span
      className={`inline-block text-[10px] font-mono font-semibold px-1.5 py-0.5 rounded border ${color}`}
      title={t('contracts.catalog.score.tooltip', 'Contract quality score (last computed)')}
    >
      {pct}
    </span>
  );
}

// ── Props ─────────────────────────────────────────────────────────────────────

interface CatalogTableProps {
  items: CatalogItem[];
  sort: SortConfig;
  onSort: (config: SortConfig) => void;
}

// ── Table ─────────────────────────────────────────────────────────────────────

export function CatalogTable({ items, sort, onSort }: CatalogTableProps) {
  const { t } = useTranslation();

  const toggleSort = (field: SortField) => {
    if (sort.field === field) {
      onSort({ field, direction: sort.direction === 'asc' ? 'desc' : 'asc' });
    } else {
      onSort({ field, direction: 'asc' });
    }
  };

  return (
    <div className="overflow-x-auto">
      <table className="w-full text-xs min-w-[1100px]">
        <thead className="sticky top-0 z-10 bg-panel">
          <tr className="border-b border-edge">
            <SortableHeader
              label={t('contracts.catalog.columns.name', 'Name')}
              field="name"
              sort={sort}
              onToggle={toggleSort}
              className="min-w-[180px]"
            />
            <SortableHeader
              label={t('contracts.catalog.columns.serviceType', 'Type')}
              field="serviceType"
              sort={sort}
              onToggle={toggleSort}
              className="min-w-[100px]"
            />
            <th className="px-3 py-3 text-left font-medium text-muted whitespace-nowrap">
              {t('contracts.catalog.columns.domain', 'Domain')}
            </th>
            <th className="px-3 py-3 text-left font-medium text-muted whitespace-nowrap">
              {t('contracts.catalog.columns.owner', 'Owner')}
            </th>
            <SortableHeader
              label={t('contracts.catalog.columns.version', 'Version')}
              field="semVer"
              sort={sort}
              onToggle={toggleSort}
              className="min-w-[80px]"
            />
            <SortableHeader
              label={t('contracts.catalog.columns.lifecycle', 'Lifecycle')}
              field="lifecycleState"
              sort={sort}
              onToggle={toggleSort}
            />
            <th className="px-3 py-3 text-left font-medium text-muted whitespace-nowrap">
              {t('contracts.catalog.columns.approval', 'Approval')}
            </th>
            <SortableHeader
              label={t('contracts.catalog.columns.criticality', 'Criticality')}
              field="criticality"
              sort={sort}
              onToggle={toggleSort}
            />
            <th className="px-3 py-3 text-left font-medium text-muted whitespace-nowrap">
              {t('contracts.catalog.columns.score', 'Score')}
            </th>
            <SortableHeader
              label={t('contracts.catalog.columns.updatedAt', 'Updated')}
              field="updatedAt"
              sort={sort}
              onToggle={toggleSort}
            />
            <th className="px-3 py-3 w-10" />
          </tr>
        </thead>
        <tbody className="divide-y divide-edge">
          {items.map((item) => (
            <CatalogRow key={item.versionId} item={item} />
          ))}
        </tbody>
      </table>
    </div>
  );
}

// ── Sortable Header ───────────────────────────────────────────────────────────

function SortableHeader({
  label,
  field,
  sort,
  onToggle,
  className,
}: {
  label: string;
  field: SortField;
  sort: SortConfig;
  onToggle: (field: SortField) => void;
  className?: string;
}) {
  const active = sort.field === field;

  return (
    <th className={cn('px-3 py-3 text-left whitespace-nowrap', className)}>
      <button
        type="button"
        onClick={() => onToggle(field)}
        className={cn(
          'inline-flex items-center gap-1 font-medium transition-colors',
          active ? 'text-cyan' : 'text-muted hover:text-body',
        )}
      >
        {label}
        {active ? (
          sort.direction === 'asc' ? (
            <ChevronUp size={12} />
          ) : (
            <ChevronDown size={12} />
          )
        ) : (
          <ChevronsUpDown size={12} className="opacity-40" />
        )}
      </button>
    </th>
  );
}

// ── Row ───────────────────────────────────────────────────────────────────────

function CatalogRow({ item }: { item: CatalogItem }) {
  const navigate = useNavigate();

  return (
    <tr
      className="hover:bg-elevated/40 transition-colors cursor-pointer group"
      style={{ transitionDuration: 'var(--nto-motion-fast)' }}
      onClick={() => navigate(`/contracts/${item.versionId}`)}
    >
      {/* Name + domain */}
      <td className="px-3 py-3">
        <div className="min-w-0">
          <p className="font-medium text-heading truncate max-w-[220px] group-hover:text-cyan transition-colors">
            {item.name}
          </p>
          <p className="text-[10px] text-muted truncate max-w-[220px]">{item.domain}</p>
        </div>
      </td>

      {/* Service type */}
      <td className="px-3 py-3">
        <ServiceTypeBadge type={item.catalogServiceType} size="sm" />
      </td>

      {/* Domain */}
      <td className="px-3 py-3 text-body">{item.domain}</td>

      {/* Owner */}
      <td className="px-3 py-3">
        <div className="min-w-0">
          <p className="text-body truncate max-w-[120px]">{item.technicalOwner || '-'}</p>
          <p className="text-[10px] text-muted truncate max-w-[120px]">{item.team}</p>
        </div>
      </td>

      {/* Version */}
      <td className="px-3 py-3">
        <span className="font-mono font-medium text-heading">v{item.semVer}</span>
      </td>

      {/* Lifecycle */}
      <td className="px-3 py-3">
        <LifecycleBadge state={item.lifecycleState} size="sm" />
      </td>

      {/* Approval */}
      <td className="px-3 py-3">
        <ApprovalStateBadge state={item.approvalState} size="sm" />
      </td>

      {/* Criticality */}
      <td className="px-3 py-3">
        <CriticalityBadge
          level={toCriticalityLevel(item.criticality)}
        />
      </td>

      {/* Score */}
      <td className="px-3 py-3">
        <ScoreBadge score={item.overallScore} />
      </td>

      {/* Updated */}
      <td className="px-3 py-3 text-muted whitespace-nowrap">
        {formatRelativeDate(item.updatedAt)}
      </td>

      {/* Actions */}
      <td className="px-3 py-3" onClick={(e) => e.stopPropagation()}>
        <RowActionMenu item={item} />
      </td>
    </tr>
  );
}

// ── Row Action Menu ───────────────────────────────────────────────────────────

function RowActionMenu({ item }: { item: CatalogItem }) {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const [open, setOpen] = useState(false);
  const ref = useRef<HTMLDivElement>(null);

  const close = useCallback(() => setOpen(false), []);

  useEffect(() => {
    if (!open) return;
    const handler = (e: MouseEvent) => {
      if (ref.current && !ref.current.contains(e.target as Node)) close();
    };
    document.addEventListener('mousedown', handler);
    return () => document.removeEventListener('mousedown', handler);
  }, [open, close]);

  const actions: { key: string; icon: React.ReactNode; labelKey: string; onClick: () => void; danger?: boolean }[] = [
    {
      key: 'summary',
      icon: <LayoutDashboard size={13} />,
      labelKey: 'contracts.catalog.actions.openSummary',
      onClick: () => navigate(`/contracts/${item.versionId}`),
    },
    {
      key: 'workspace',
      icon: <ExternalLink size={13} />,
      labelKey: 'contracts.catalog.actions.openWorkspace',
      onClick: () => navigate(`/contracts/${item.versionId}`),
    },
    {
      key: 'portal',
      icon: <Eye size={13} />,
      labelKey: 'contracts.catalog.actions.viewPortal',
      onClick: () => navigate(`/contracts/${item.versionId}/portal`),
    },
    {
      key: 'edit',
      icon: <Pencil size={13} />,
      labelKey: 'common.edit',
      onClick: () => navigate(`/contracts/${item.versionId}`),
    },
    {
      key: 'compare',
      icon: <GitCompare size={13} />,
      labelKey: 'contracts.catalog.actions.compareVersions',
      onClick: () => navigate(`/contracts/${item.versionId}`),
    },
    {
      key: 'newVersion',
      icon: <Plus size={13} />,
      labelKey: 'contracts.catalog.actions.createNewVersion',
      onClick: () => navigate(`/contracts/${item.versionId}`),
    },
    {
      key: 'review',
      icon: <Send size={13} />,
      labelKey: 'contracts.catalog.actions.startReview',
      onClick: () => navigate(`/contracts/${item.versionId}`),
    },
    {
      key: 'approve',
      icon: <CheckCircle size={13} />,
      labelKey: 'contracts.catalog.actions.approve',
      onClick: () => navigate(`/contracts/${item.versionId}`),
    },
    {
      key: 'publish',
      icon: <Upload size={13} />,
      labelKey: 'contracts.catalog.actions.publish',
      onClick: () => navigate(`/contracts/${item.versionId}`),
    },
    {
      key: 'deprecate',
      icon: <AlertTriangle size={13} />,
      labelKey: 'contracts.catalog.actions.deprecate',
      onClick: () => navigate(`/contracts/${item.versionId}`),
      danger: true,
    },
    {
      key: 'archive',
      icon: <Archive size={13} />,
      labelKey: 'contracts.catalog.actions.archive',
      onClick: () => navigate(`/contracts/${item.versionId}`),
      danger: true,
    },
  ];

  return (
    <div ref={ref} className="relative">
      <button
        type="button"
        onClick={() => setOpen((v) => !v)}
        className={cn(
          'p-1.5 rounded-md transition-colors',
          open ? 'bg-elevated text-heading' : 'text-muted hover:text-body hover:bg-elevated',
        )}
      >
        <MoreHorizontal size={14} />
      </button>

      {open && (
        <div
          className="absolute right-0 top-full mt-1 z-[var(--z-dropdown)] w-52 rounded-lg border border-edge bg-card shadow-floating py-1 animate-fade-in"
        >
          {actions.map((action, idx) => (
            <div key={action.key}>
              {/* Separator before danger group */}
              {idx === actions.length - 2 && (
                <div className="my-1 border-t border-edge" />
              )}
              <button
                type="button"
                onClick={() => {
                  close();
                  action.onClick();
                }}
                className={cn(
                  'w-full flex items-center gap-2.5 px-3 py-1.5 text-xs transition-colors text-left',
                  action.danger
                    ? 'text-danger hover:bg-danger/10'
                    : 'text-body hover:bg-elevated',
                )}
              >
                {action.icon}
                {t(action.labelKey, action.key)}
              </button>
            </div>
          ))}
        </div>
      )}
    </div>
  );
}

function toCriticalityLevel(level: string): 'Low' | 'Medium' | 'High' | 'Critical' {
  return level === 'Low' || level === 'High' || level === 'Critical' ? level : 'Medium';
}

// ── Helpers ───────────────────────────────────────────────────────────────────

function formatRelativeDate(iso: string): string {
  if (!iso) return '—';
  try {
    const date = new Date(iso);
    const now = new Date();
    const diffMs = now.getTime() - date.getTime();
    const diffDays = Math.floor(diffMs / (1000 * 60 * 60 * 24));

    if (diffDays === 0) return 'Today';
    if (diffDays === 1) return 'Yesterday';
    if (diffDays < 7) return `${diffDays}d ago`;
    if (diffDays < 30) return `${Math.floor(diffDays / 7)}w ago`;
    if (diffDays < 365) return `${Math.floor(diffDays / 30)}mo ago`;
    return `${Math.floor(diffDays / 365)}y ago`;
  } catch {
    return '—';
  }
}
