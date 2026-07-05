/**
 * ServiceResultCard — unidade de descoberta âncora do catálogo.
 *
 * Anatomy (comfortable):
 * - Scan line: nome do serviço + dot de lifecycle (aria-label) + badge de exposição
 * - Descrição (honest-null)
 * - Linha de contexto: domínio · equipa · dono + saúde (honest-null por campo)
 * - Separador + chips de API com badge 📄 e "Ver contrato"
 *
 * density=compact → variante linha única condensada (sem descrição, sem separador).
 *
 * Design system only — zero cores hardcoded, zero strings hardcoded.
 */
import { useTranslation } from 'react-i18next';
import { Card, CardBody } from '../../../components/Card';
import { Badge } from '../../../components/Badge';
import { Button } from '../../../components/Button';
import { cn } from '../../../lib/cn';
import type { ServiceVM, ApiVM, Density, Lifecycle, Exposure } from './catalogTypes';

/* ─── Constantes de mapeamento ───────────────────────────────────────────────── */

/** Tokens de cor do dot de lifecycle — obrigatórios pelo spec. */
const lifecycleDotClass: Record<Lifecycle, string> = {
  Stable:     'bg-success',
  Beta:       'bg-warning',
  Deprecated: 'bg-critical',
  Unknown:    'bg-muted',
};

/** Variante de Badge por nível de exposição. */
const exposureBadgeVariant: Record<Exposure, 'success' | 'info' | 'warning'> = {
  Public:   'success',
  Internal: 'info',
  Partner:  'warning',
};

/** Variante de Badge por estado de saúde. */
const healthBadgeVariant: Record<NonNullable<ServiceVM['health']>, 'success' | 'warning' | 'danger'> = {
  Ok:   'success',
  Warn: 'warning',
  Down: 'danger',
};

/** Número máximo de chips de API visíveis antes do colapso +N. */
const MAX_VISIBLE_APIS = 3;

/* ─── Props ──────────────────────────────────────────────────────────────────── */

export interface ServiceResultCardProps {
  service:        ServiceVM;
  density:        Density;
  onOpenService:  (id: string) => void;
  onOpenApi:      (apiId: string) => void;
  onViewContract: (apiId: string) => void;
}

/* ─── ApiChip (interno) ──────────────────────────────────────────────────────── */

interface ApiChipProps {
  api:            ApiVM;
  onOpenApi:      (id: string) => void;
  onViewContract: (id: string) => void;
}

function ApiChip({ api, onOpenApi, onViewContract }: ApiChipProps) {
  const { t } = useTranslation();
  return (
    <span className="inline-flex items-center gap-0.5 rounded-full border border-edge bg-elevated">
      <Button
        variant="ghost"
        size="xs"
        onClick={(e) => { e.stopPropagation(); onOpenApi(api.id); }}
      >
        {api.name}
      </Button>
      {api.hasContract && (
        <>
          <Badge size="xs" variant="muted">📄</Badge>
          <Button
            variant="ghost"
            size="xs"
            onClick={(e) => { e.stopPropagation(); onViewContract(api.id); }}
          >
            {t('serviceCatalog.browse.card.viewContract')}
          </Button>
        </>
      )}
    </span>
  );
}

/* ─── ServiceResultCard ──────────────────────────────────────────────────────── */

export function ServiceResultCard({
  service,
  density,
  onOpenService,
  onOpenApi,
  onViewContract,
}: ServiceResultCardProps) {
  const { t } = useTranslation();

  const handleCardClick = () => onOpenService(service.id);

  const lifecycleLabel = t(
    `serviceCatalog.browse.lifecycle.${service.lifecycle.toLowerCase()}`,
  );

  /* ── Compact ── */
  if (density === 'compact') {
    return (
      <Card
        variant="interactive"
        onClick={handleCardClick}
        onKeyDown={(e) => {
          if (e.key === 'Enter' || e.key === ' ') {
            e.preventDefault();
            onOpenService(service.id);
          }
        }}
        tabIndex={0}
      >
        <div className="flex items-center gap-2 px-4 py-2 flex-wrap">
          <h3 className="text-sm font-medium text-heading">{service.name}</h3>
          <span
            role="img"
            aria-label={lifecycleLabel}
            className={cn(
              'w-2 h-2 rounded-full flex-shrink-0',
              lifecycleDotClass[service.lifecycle],
            )}
          />
          {service.exposure && (
            <Badge variant={exposureBadgeVariant[service.exposure]} size="xs">
              {t(`serviceCatalog.browse.exposure.${service.exposure.toLowerCase()}`)}
            </Badge>
          )}
          {service.domain && (
            <span className="text-xs text-muted">{service.domain}</span>
          )}
        </div>
      </Card>
    );
  }

  /* ── Comfortable ── */
  const visibleApis   = service.apis.slice(0, MAX_VISIBLE_APIS);
  const overflowCount = Math.max(0, service.apis.length - MAX_VISIBLE_APIS);
  const hasContextLine =
    service.domain ?? service.team ?? service.owner ?? service.health;

  return (
    <Card
      variant="interactive"
      onClick={handleCardClick}
      onKeyDown={(e) => {
        if (e.key === 'Enter' || e.key === ' ') {
          e.preventDefault();
          onOpenService(service.id);
        }
      }}
      tabIndex={0}
    >
      <CardBody>

        {/* ── Scan line: nome + dot lifecycle + exposição ── */}
        <div className="flex items-center gap-2 flex-wrap">
          <h3 className="text-sm font-semibold text-heading">{service.name}</h3>
          <span
            role="img"
            aria-label={lifecycleLabel}
            className={cn(
              'w-2 h-2 rounded-full flex-shrink-0',
              lifecycleDotClass[service.lifecycle],
            )}
          />
          {service.exposure && (
            <Badge variant={exposureBadgeVariant[service.exposure]} size="sm">
              {t(`serviceCatalog.browse.exposure.${service.exposure.toLowerCase()}`)}
            </Badge>
          )}
        </div>

        {/* ── Descrição (honest-null) ── */}
        {service.description && (
          <p className="mt-1 text-xs text-muted line-clamp-1">
            {service.description}
          </p>
        )}

        {/* ── Linha de contexto: domínio · equipa · dono + saúde (honest-null) ── */}
        {hasContextLine && (
          <div className="mt-1 flex items-center gap-1 text-xs text-muted flex-wrap">
            {service.domain && <span>{service.domain}</span>}
            {service.domain && service.team && (
              <span aria-hidden="true">·</span>
            )}
            {service.team && <span>{service.team}</span>}
            {(service.domain ?? service.team) && service.owner && (
              <span aria-hidden="true">·</span>
            )}
            {service.owner && <span>{service.owner}</span>}
            {service.health && (
              <Badge
                variant={healthBadgeVariant[service.health]}
                size="xs"
                className="ml-1"
              >
                {t(`serviceCatalog.browse.health.${service.health.toLowerCase()}`)}
              </Badge>
            )}
          </div>
        )}

        {/* ── Separador + chips de API ── */}
        {service.apis.length > 0 && (
          <>
            <hr className="my-2 border-edge/60" />
            <div className="flex flex-wrap gap-1.5">
              {visibleApis.map((api) => (
                <ApiChip
                  key={api.id}
                  api={api}
                  onOpenApi={onOpenApi}
                  onViewContract={onViewContract}
                />
              ))}
              {overflowCount > 0 && (
                <span className="inline-flex items-center text-xs text-muted px-2 py-0.5">
                  +{overflowCount}
                </span>
              )}
            </div>
          </>
        )}

      </CardBody>
    </Card>
  );
}
