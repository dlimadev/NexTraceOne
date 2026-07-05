/**
 * ApiResultRow — linha de resultado para o modo de vista "APIs" do catálogo.
 *
 * Anatomy: nome da API · padrão de rota (mono, honest-null) · exposição (honest-null) ·
 * versão (honest-null) · badge 📄 (honest-null) · "Ver contrato" (honest-null).
 *
 * Clicar na linha → onOpenApi(id).
 * "Ver contrato" → onViewContract(id) (stopPropagation para não disparar onOpenApi).
 *
 * Design system only — zero cores hardcoded, zero strings hardcoded.
 */
import { useTranslation } from 'react-i18next';
import { Badge } from '../../../components/Badge';
import { Button } from '../../../components/Button';
import type { ApiVM, Exposure } from './catalogTypes';

/* ─── Constantes ─────────────────────────────────────────────────────────────── */

const exposureBadgeVariant: Record<Exposure, 'success' | 'info' | 'warning'> = {
  Public:   'success',
  Internal: 'info',
  Partner:  'warning',
};

/* ─── Props ──────────────────────────────────────────────────────────────────── */

export interface ApiResultRowProps {
  api:            ApiVM;
  onOpenApi:      (id: string) => void;
  onViewContract: (apiId: string) => void;
}

/* ─── Componente ─────────────────────────────────────────────────────────────── */

export function ApiResultRow({ api, onOpenApi, onViewContract }: ApiResultRowProps) {
  const { t } = useTranslation();

  return (
    <div
      role="button"
      aria-label={api.name}
      className="flex items-center gap-3 px-4 py-3 bg-card rounded-lg border border-edge cursor-pointer hover:border-edge-strong transition-colors duration-[var(--nto-motion-base)] flex-wrap"
      onClick={() => onOpenApi(api.id)}
      onKeyDown={(e) => {
        if (e.key === 'Enter' || e.key === ' ') {
          e.preventDefault();
          onOpenApi(api.id);
        }
      }}
      tabIndex={0}
    >
      <span className="font-medium text-sm text-heading flex-shrink-0">
        {api.name}
      </span>

      {api.routePattern && (
        <span className="font-mono text-xs text-muted truncate">
          {api.routePattern}
        </span>
      )}

      {api.exposure && (
        <Badge variant={exposureBadgeVariant[api.exposure]} size="sm">
          {t(`serviceCatalog.browse.exposure.${api.exposure.toLowerCase()}`)}
        </Badge>
      )}

      {api.version && (
        <Badge variant="muted" size="sm">{api.version}</Badge>
      )}

      {api.hasContract && (
        <Badge variant="muted" size="sm">📄</Badge>
      )}

      <div className="ml-auto flex items-center">
        {api.hasContract && (
          <Button
            variant="ghost"
            size="xs"
            onClick={(e) => { e.stopPropagation(); onViewContract(api.id); }}
          >
            {t('serviceCatalog.browse.card.viewContract')}
          </Button>
        )}
      </div>
    </div>
  );
}
