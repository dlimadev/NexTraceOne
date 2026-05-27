// src/frontend/src/features/contracts/lib/contractVariants.ts

import type { BadgeProps } from '../../../components/Badge';

type BadgeVariant = NonNullable<BadgeProps['variant']>;

/** Estados do ciclo de vida de contrato → variante de Badge. */
export type ContractLifecycleState =
  | 'Draft'
  | 'InReview'
  | 'Approved'
  | 'Locked'
  | 'Deprecated'
  | 'Archived';

const STATE_VARIANT_MAP: Record<ContractLifecycleState, BadgeVariant> = {
  Draft:      'default',
  InReview:   'warning',
  Approved:   'success',
  Locked:     'info',
  Deprecated: 'danger',
  Archived:   'neutral',
};

/**
 * Converte um estado de ciclo de vida de contrato para a variante de Badge correspondente.
 * @example stateToVariant('Approved') // → 'success'
 */
export function stateToVariant(state: string): BadgeVariant {
  return STATE_VARIANT_MAP[state as ContractLifecycleState] ?? 'default';
}

/** Mapeamento estado → classe CSS de cor legacy (para gradual migration). */
export const STATE_COLOR_CLASSES: Record<string, string> = {
  Draft:      'bg-elevated text-body border-edge',
  InReview:   'bg-warning/12 text-warning border-warning/25',
  Approved:   'bg-success/12 text-success border-success/25',
  Locked:     'bg-info/12 text-info border-info/25',
  Deprecated: 'bg-critical/12 text-critical border-critical/25',
  Archived:   'bg-elevated text-muted border-edge',
};
