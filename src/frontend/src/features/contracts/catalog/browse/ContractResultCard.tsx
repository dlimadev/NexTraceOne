/**
 * ContractResultCard — cartão de resultado para a vista de catálogo de contratos.
 *
 * Anatomy (comfortable):
 * - Scan line: nome do contrato (heading h3) + dot de lifecycle (aria-label localizado)
 *   + ServiceTypeBadge por catalogServiceType.
 * - Versão (semVer).
 * - Linha de contexto: domínio · equipa · dono técnico — honest-null por campo.
 * - Badges: criticality + exposure + approvalState — honest-null, tokens semânticos.
 *
 * density='compact' → variante linha única condensada (sem contexto, sem badges).
 *
 * Design system only — zero cores hardcoded, tokens semânticos.
 */
import { useTranslation } from 'react-i18next';
import { Card, CardBody } from '../../../../components/Card';
import { Badge } from '../../../../components/Badge';
import { cn } from '../../../../lib/cn';
import { ServiceTypeBadge } from '../../shared/components/ServiceTypeBadge';
import type { CatalogItem } from '../types';
import type { ContractDensity } from './contractBrowseTypes';

/* ─── Lifecycle dot — token classes ─────────────────────────────────────────── */

/** Mapeia lifecycleState para token de cor do dot (bg-*). */
const lifecycleDotClass: Record<string, string> = {
  Draft:      'bg-muted',
  InReview:   'bg-warning',
  Approved:   'bg-success',
  Locked:     'bg-success',
  Deprecated: 'bg-warning',
  Sunset:     'bg-critical',
  Retired:    'bg-muted',
};

/* ─── Badge variant mappings ─────────────────────────────────────────────────── */

/** Variante de Badge por criticidade. */
const criticalityVariant: Record<string, NonNullable<React.ComponentProps<typeof Badge>['variant']>> = {
  Critical: 'danger',
  High:     'danger',
  Medium:   'warning',
  Low:      'success',
};

/** Variante de Badge por exposição. */
const exposureVariant: Record<string, NonNullable<React.ComponentProps<typeof Badge>['variant']>> = {
  Public:   'success',
  Internal: 'info',
  Partner:  'warning',
};

/** Variante de Badge por estado de aprovação. */
const approvalVariant: Record<string, NonNullable<React.ComponentProps<typeof Badge>['variant']>> = {
  Approved:  'success',
  Pending:   'warning',
  InReview:  'info',
  Rejected:  'danger',
  Escalated: 'warning',
};

/* ─── Props ──────────────────────────────────────────────────────────────────── */

export interface ContractResultCardProps {
  /** Item do catálogo de contratos. */
  item:    CatalogItem;
  /** Densidade visual da lista. */
  density: ContractDensity;
  /** Callback ao abrir o item. */
  onOpen:  (item: CatalogItem) => void;
}

/* ─── ContractResultCard ─────────────────────────────────────────────────────── */

export function ContractResultCard({ item, density, onOpen }: ContractResultCardProps) {
  const { t } = useTranslation();

  const handleClick = () => onOpen(item);

  const lifecycleLabel = t(
    `contracts.catalog.browse.lifecycle.${item.lifecycleState.toLowerCase()}`,
  );

  const dotClass = lifecycleDotClass[item.lifecycleState] ?? 'bg-muted';

  /* ── Compact — linha única condensada ── */
  if (density === 'compact') {
    return (
      <Card
        variant="interactive"
        onClick={handleClick}
        onKeyDown={(e) => {
          if (e.key === 'Enter' || e.key === ' ') {
            e.preventDefault();
            onOpen(item);
          }
        }}
        tabIndex={0}
      >
        <div className="flex items-center gap-2 px-4 py-2 flex-wrap">
          <h3 className="text-sm font-medium text-heading">{item.name}</h3>
          <span
            role="img"
            aria-label={lifecycleLabel}
            className={cn('w-2 h-2 rounded-full flex-shrink-0', dotClass)}
          />
          <ServiceTypeBadge type={item.catalogServiceType} size="sm" />
          {item.semVer && (
            <span className="text-xs text-muted font-mono">{item.semVer}</span>
          )}
        </div>
      </Card>
    );
  }

  /* ── Comfortable — anatomia completa ── */
  const hasContextLine = Boolean(item.domain || item.team || item.technicalOwner);

  return (
    <Card
      variant="interactive"
      onClick={handleClick}
      onKeyDown={(e) => {
        if (e.key === 'Enter' || e.key === ' ') {
          e.preventDefault();
          onOpen(item);
        }
      }}
      tabIndex={0}
    >
      <CardBody>

        {/* ── Scan line: nome + dot lifecycle + badge de tipo ── */}
        <div className="flex items-center gap-2 flex-wrap">
          <h3 className="text-sm font-semibold text-heading">{item.name}</h3>
          <span
            role="img"
            aria-label={lifecycleLabel}
            className={cn('w-2 h-2 rounded-full flex-shrink-0', dotClass)}
          />
          <ServiceTypeBadge type={item.catalogServiceType} size="sm" />
        </div>

        {/* ── Versão ── */}
        <p className="mt-0.5 text-xs text-muted font-mono">{item.semVer}</p>

        {/* ── Linha de contexto: domínio · equipa · dono (honest-null) ── */}
        {hasContextLine && (
          <div className="mt-1 flex items-center gap-1 text-xs text-muted flex-wrap">
            {item.domain && <span>{item.domain}</span>}
            {Boolean(item.domain && item.team) && (
              <span aria-hidden="true">·</span>
            )}
            {item.team && <span>{item.team}</span>}
            {Boolean((item.domain || item.team) && item.technicalOwner) && (
              <span aria-hidden="true">·</span>
            )}
            {item.technicalOwner && <span>{item.technicalOwner}</span>}
          </div>
        )}

        {/* ── Badges: criticality + exposure + approvalState (honest-null) ── */}
        {(item.criticality || item.exposure || item.approvalState) && (
          <div className="mt-2 flex flex-wrap gap-1">
            {item.criticality && (
              <Badge
                variant={criticalityVariant[item.criticality] ?? 'neutral'}
                size="sm"
              >
                {item.criticality}
              </Badge>
            )}
            {item.exposure && (
              <Badge
                variant={exposureVariant[item.exposure] ?? 'info'}
                size="sm"
              >
                {item.exposure}
              </Badge>
            )}
            {item.approvalState && (
              <Badge
                variant={approvalVariant[item.approvalState] ?? 'neutral'}
                size="sm"
              >
                {item.approvalState}
              </Badge>
            )}
          </div>
        )}

      </CardBody>
    </Card>
  );
}
