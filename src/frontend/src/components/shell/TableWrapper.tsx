import type { ReactNode } from 'react';
import { cn } from '../../lib/cn';

interface TableWrapperProps {
  children: ReactNode;
  className?: string;
}

/**
 * Wrapper responsivo para tabelas de dados — DESIGN-SYSTEM §4.11
 *
 * Garante:
 * - overflow-x-auto em viewports menores (sem overflow horizontal na página)
 * - min-width para manter legibilidade das colunas
 * - border radius e background consistentes via Card
 *
 * Usar sempre à volta de elementos <table> dentro de Card.
 */
export function TableWrapper({ children, className }: TableWrapperProps) {
  return (
    <div className={cn('overflow-x-auto -mx-px', className)}>
      {children}
    </div>
  );
}
