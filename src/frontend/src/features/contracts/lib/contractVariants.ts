// src/frontend/src/features/contracts/lib/contractVariants.ts

import type { BadgeProps } from '../../../components/Badge';

type BadgeVariant = NonNullable<BadgeProps['variant']>;

/**
 * Estados do ciclo de vida de contrato → variante de Badge.
 * Estados: Draft, InReview, Approved, Locked, Deprecated, Sunset, Retired
 */
export type ContractLifecycleState =
  | 'Draft'
  | 'InReview'
  | 'Approved'
  | 'Locked'
  | 'Deprecated'
  | 'Sunset'
  | 'Retired';

const STATE_VARIANT_MAP: Record<ContractLifecycleState, BadgeVariant> = {
  Draft:      'default',
  InReview:   'warning',
  Approved:   'success',
  Locked:     'info',
  Deprecated: 'danger',
  Sunset:     'warning',
  Retired:    'neutral',
};

/**
 * Converte um estado de ciclo de vida de contrato para a variante de Badge correspondente.
 * @example stateToVariant('Approved') // → 'success'
 */
export function stateToVariant(state: string): BadgeVariant {
  return STATE_VARIANT_MAP[state as ContractLifecycleState] ?? 'default';
}
