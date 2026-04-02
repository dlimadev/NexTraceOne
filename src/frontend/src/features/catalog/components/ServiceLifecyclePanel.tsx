import { useState } from 'react';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { useTranslation } from 'react-i18next';
import { GitBranch, ChevronRight } from 'lucide-react';
import { Card, CardHeader, CardBody } from '../../../components/Card';
import { Badge } from '../../../components/Badge';
import { Button } from '../../../components/Button';
import { ConfirmDialog } from '../../../components/ConfirmDialog';
import { useToast } from '../../../components/Toast';
import { serviceCatalogApi } from '../api';
import type { LifecycleStatus } from '../../../types';

/** Transições válidas da máquina de estados do domínio (espelha ServiceAsset.TransitionTo). */
const ALLOWED_TRANSITIONS: Record<LifecycleStatus, LifecycleStatus[]> = {
  Planning: ['Development'],
  Development: ['Staging'],
  Staging: ['Active', 'Development'],
  Active: ['Deprecating'],
  Deprecating: ['Deprecated', 'Active'],
  Deprecated: ['Retired'],
  Retired: [],
};

/** Ordem linear para o stepper visual de ciclo de vida. */
const LIFECYCLE_ORDER: LifecycleStatus[] = [
  'Planning',
  'Development',
  'Staging',
  'Active',
  'Deprecating',
  'Deprecated',
  'Retired',
];

/** Mapeia estado do ciclo de vida para variante do Badge. */
const lifecycleBadgeVariant = (
  status: LifecycleStatus
): 'success' | 'info' | 'warning' | 'default' => {
  switch (status) {
    case 'Active':
      return 'success';
    case 'Planning':
    case 'Development':
    case 'Staging':
      return 'info';
    case 'Deprecating':
    case 'Deprecated':
      return 'warning';
    case 'Retired':
      return 'default';
    default:
      return 'default';
  }
};

interface ServiceLifecyclePanelProps {
  serviceId: string;
  serviceName: string;
  currentStatus: LifecycleStatus;
}

/**
 * Painel de transição de ciclo de vida de serviço.
 * Apresenta o estado atual, os estados permitidos pelo domínio e botões de ação.
 * Usa o endpoint PATCH /api/v1/catalog/services/{id}/lifecycle.
 */
export function ServiceLifecyclePanel({
  serviceId,
  serviceName,
  currentStatus,
}: ServiceLifecyclePanelProps) {
  const { t } = useTranslation();
  const queryClient = useQueryClient();
  const { toastSuccess, toastError } = useToast();

  const [pendingTarget, setPendingTarget] = useState<LifecycleStatus | null>(null);

  const mutation = useMutation({
    mutationFn: (newStatus: LifecycleStatus) =>
      serviceCatalogApi.transitionLifecycle(serviceId, { newStatus }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['catalog-service-detail', serviceId] });
      setPendingTarget(null);
      toastSuccess(t('catalog.detail.lifecycleTransition.success'));
    },
    onError: () => {
      setPendingTarget(null);
      toastError(t('catalog.detail.lifecycleTransition.errorInvalid'));
    },
  });

  const allowedTargets = ALLOWED_TRANSITIONS[currentStatus] ?? [];

  return (
    <>
      <Card>
        <CardHeader>
          <div className="flex items-center gap-2">
            <GitBranch size={16} className="text-accent" aria-hidden="true" />
            <h2 className="text-base font-semibold text-heading">
              {t('catalog.detail.lifecycleTransition.title')}
            </h2>
          </div>
        </CardHeader>
        <CardBody>
          {/* Current status */}
          <div className="mb-4">
            <p className="text-xs text-muted mb-1.5">
              {t('catalog.detail.lifecycleTransition.currentStatus')}
            </p>
            <Badge variant={lifecycleBadgeVariant(currentStatus)}>
              {t(
                `catalog.detail.lifecycleTransition.statuses.${currentStatus}`,
                currentStatus
              )}
            </Badge>
          </div>

          {/* Visual stepper */}
          <ol
            className="flex flex-wrap items-center gap-1 mb-4"
            aria-label={t('catalog.detail.lifecycleTransition.title')}
          >
            {LIFECYCLE_ORDER.map((step, idx) => {
              const isCurrent = step === currentStatus;
              const isPast =
                LIFECYCLE_ORDER.indexOf(currentStatus) > idx;
              return (
                <li key={step} className="flex items-center gap-1">
                  <span
                    className={[
                      'text-xs px-2 py-0.5 rounded-full font-medium transition-colors',
                      isCurrent
                        ? 'bg-accent text-white'
                        : isPast
                        ? 'bg-elevated text-muted line-through'
                        : 'text-muted',
                    ].join(' ')}
                  >
                    {t(
                      `catalog.detail.lifecycleTransition.statuses.${step}`,
                      step
                    )}
                  </span>
                  {idx < LIFECYCLE_ORDER.length - 1 && (
                    <ChevronRight size={12} className="text-muted shrink-0" aria-hidden="true" />
                  )}
                </li>
              );
            })}
          </ol>

          {/* Transition buttons */}
          {allowedTargets.length === 0 ? (
            <p className="text-xs text-muted italic">
              {t('catalog.detail.lifecycleTransition.noTransitions')}
            </p>
          ) : (
            <div className="flex flex-col gap-2">
              <p className="text-xs text-muted mb-1">
                {t('catalog.detail.lifecycleTransition.transitionTo')}
              </p>
              <div className="flex flex-wrap gap-2">
                {allowedTargets.map((target) => (
                  <Button
                    key={target}
                    variant="secondary"
                    size="sm"
                    onClick={() => setPendingTarget(target)}
                    disabled={mutation.isPending}
                  >
                    {t(
                      `catalog.detail.lifecycleTransition.statuses.${target}`,
                      target
                    )}
                  </Button>
                ))}
              </div>
            </div>
          )}
        </CardBody>
      </Card>

      {/* Confirmation dialog */}
      <ConfirmDialog
        open={pendingTarget !== null}
        onClose={() => setPendingTarget(null)}
        onConfirm={() => {
          if (pendingTarget) mutation.mutate(pendingTarget);
        }}
        title={t('catalog.detail.lifecycleTransition.confirmTitle')}
        description={t('catalog.detail.lifecycleTransition.confirmDescription', {
          serviceName,
          from: t(
            `catalog.detail.lifecycleTransition.statuses.${currentStatus}`,
            currentStatus
          ),
          to: t(
            `catalog.detail.lifecycleTransition.statuses.${pendingTarget ?? ''}`,
            pendingTarget ?? ''
          ),
        })}
        confirmLabel={t('catalog.detail.lifecycleTransition.confirm')}
        cancelLabel={t('catalog.detail.lifecycleTransition.cancel')}
        variant="warning"
        loading={mutation.isPending}
      />
    </>
  );
}
