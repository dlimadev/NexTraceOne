import type { ReactNode } from 'react';
import { PreviewBanner } from './PreviewBanner';

/**
 * Gate de preview — envolve uma página com o banner de aviso de preview.
 *
 * Usado nas rotas de módulos não homologáveis (Fase 5) para indicar
 * visualmente que o módulo não faz parte do aceite.
 */
export function PreviewGate({ children }: { children: ReactNode }) {
  return (
    <>
      <div className="px-6 pt-4">
        <PreviewBanner />
      </div>
      {children}
    </>
  );
}
