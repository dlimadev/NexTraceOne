import { type ClassValue, clsx } from 'clsx';
import { twMerge } from 'tailwind-merge';

/**
 * Combina classes condicionais (clsx) com resolução de conflitos Tailwind (twMerge).
 * Usar em todo componente que aceite className externo ou variantes internas.
 */
export function cn(...inputs: ClassValue[]): string {
  return twMerge(clsx(inputs));
}
