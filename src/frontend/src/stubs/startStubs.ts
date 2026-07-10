/**
 * Arranque do modo stub.
 *
 * Chamado por main.tsx apenas quando VITE_STUB === 'true'. Inicia o service
 * worker do MSW e resolve quando as interceções estão activas — garantindo
 * que nenhuma chamada arranca antes dos handlers estarem prontos.
 */
export async function startStubs(): Promise<void> {
  const { worker } = await import('./browser');

  await worker.start({
    // Endpoints /api/v1 por cobrir são apanhados pelo catch-all; qualquer
    // outro pedido (fontes, assets) segue para a rede normalmente.
    onUnhandledRequest: 'bypass',
    quiet: true,
  });

  console.info(
    '%c[stub] Modo stub ativo — a app corre sem backend (MSW). Login já autenticado.',
    'color:#3b82f6;font-weight:bold',
  );
}
