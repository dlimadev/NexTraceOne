/**
 * NexTraceOne — Shared API Layer
 *
 * Re-export do cliente HTTP centralizado e barrel de APIs por domínio.
 *
 * @see src/api/client.ts para detalhes do interceptor de autenticação.
 */
export { default as apiClient } from '../../api/client';
