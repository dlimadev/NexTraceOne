import { memo } from 'react';
import { Link } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { ArrowRight } from 'lucide-react';
import { Card, CardBody, CardHeader } from '../../../components/Card';
import { EmptyState } from '../../../components/EmptyState';
import { LifecycleBadge } from '../shared/components';
import { cn } from '../../../lib/cn';
import type { ContractListItem } from '../types';

// ── KpiCard ───────────────────────────────────────────────────────────────────

interface KpiCardProps {
  label: string;
  value: number;
  icon: React.ReactNode;
  color: string;
}

export const KpiCard = memo(function KpiCard({ label, value, icon, color }: KpiCardProps) {
  return (
    <Card>
      <CardBody className="flex items-center gap-3 py-3 px-4">
        <div className={color}>{icon}</div>
        <div>
          <p className="text-[10px] text-muted uppercase tracking-wider">{label}</p>
          <p className="text-lg font-bold text-heading">{value}</p>
        </div>
      </CardBody>
    </Card>
  );
});

// ── PolicyStat ────────────────────────────────────────────────────────────────

interface PolicyStatProps {
  label: string;
  count: number;
  color: string;
  bg: string;
}

export const PolicyStat = memo(function PolicyStat({ label, count, color, bg }: PolicyStatProps) {
  return (
    <div className={cn('rounded-md p-3 text-center', bg)}>
      <p className={cn('text-lg font-bold', color)}>{count}</p>
      <p className="text-[10px] text-muted">{label}</p>
    </div>
  );
});

// ── InsightCard ───────────────────────────────────────────────────────────────

interface InsightCardProps {
  icon: React.ReactNode;
  iconColor: string;
  title: string;
  count: number;
  items: ContractListItem[];
}

export const InsightCard = memo(function InsightCard({
  icon, iconColor, title, count, items,
}: InsightCardProps) {
  const { t } = useTranslation();

  return (
    <Card>
      <CardHeader className="py-3 px-4">
        <div className="flex items-center justify-between">
          <div className="flex items-center gap-2">
            <span className={iconColor}>{icon}</span>
            <h3 className="text-xs font-semibold text-heading">{title}</h3>
          </div>
          <span className={cn('text-sm font-bold', count > 0 ? iconColor : 'text-muted')}>{count}</span>
        </div>
      </CardHeader>
      <CardBody className="p-0">
        {count === 0 ? (
          <EmptyState size="compact" title={t('contracts.governance.noIssues', 'No issues found')} />
        ) : (
          <ContractList items={items.slice(0, 8)} />
        )}
      </CardBody>
    </Card>
  );
});

// ── ContractList ──────────────────────────────────────────────────────────────

interface ContractListProps {
  items: ContractListItem[];
}

export const ContractList = memo(function ContractList({ items }: ContractListProps) {
  return (
    <div className="divide-y divide-edge max-h-48 overflow-y-auto">
      {items.map((item) => (
        <Link
          key={item.versionId}
          to={`/contracts/${item.versionId}`}
          className="flex items-center justify-between px-4 py-2 text-xs hover:bg-elevated/30 transition-colors"
        >
          <div className="flex items-center gap-2 min-w-0">
            <span className="text-body font-medium truncate">{item.apiAssetId}</span>
            <LifecycleBadge state={item.lifecycleState} size="sm" />
          </div>
          <ArrowRight size={10} className="text-muted flex-shrink-0" />
        </Link>
      ))}
    </div>
  );
});
