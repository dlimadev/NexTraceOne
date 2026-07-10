/**
 * Worker MSW para o browser (modo stub).
 *
 * Só é importado dinamicamente quando VITE_STUB === 'true' (npm run stub),
 * pelo que nunca entra no bundle de dev/produção normal.
 */
import { setupWorker } from 'msw/browser';
import { handlers } from './handlers';

export const worker = setupWorker(...handlers);
