/**
 * Cliente HTTP centralizado da aplicação — ponto único de comunicação com a API.
 *
 * Medidas de segurança implementadas:
 * - Tokens lidos de sessionStorage (access) e memória (refresh), nunca de localStorage.
 * - Interceptor de refresh tenta renovar o access token antes de forçar re-login.
 * - Nenhum dado sensível é logado no console em caso de erro.
 * - Evento customizado 'auth:session-expired' permite que o AuthContext reaja
 *   sem acoplamento direto (evita import circular).
 *
 * @see src/utils/tokenStorage.ts para detalhes da estratégia de armazenamento.
 */
import axios from 'axios';
import type { AxiosError, InternalAxiosRequestConfig } from 'axios';
import {
  getAccessToken,
  getCsrfToken,
  getTenantId,
  getEnvironmentId,
  getRefreshToken,
  storeTokens,
  clearAllTokens,
  clearCsrfToken,
} from '../utils/tokenStorage';

const apiClient = axios.create({
  baseURL: '/api/v1',
  withCredentials: true,
  headers: { 'Content-Type': 'application/json' },
});

/**
 * Interceptor de request: injeta access token e tenant ID de forma segura.
 * Tokens são obtidos do módulo tokenStorage (sessionStorage + memória).
 */
apiClient.interceptors.request.use((config) => {
  const token = getAccessToken();
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }

  const tenantId = getTenantId();
  if (tenantId) {
    config.headers['X-Tenant-Id'] = tenantId;
  }

  const environmentId = getEnvironmentId();
  if (environmentId) {
    config.headers['X-Environment-Id'] = environmentId;
  }

  const method = config.method?.toUpperCase();
  const csrfToken = getCsrfToken();
  if (csrfToken && method && ['POST', 'PUT', 'PATCH', 'DELETE'].includes(method)) {
    config.headers['X-Csrf-Token'] = csrfToken;
  }

  return config;
});

/**
 * Flag para evitar múltiplas tentativas simultâneas de refresh.
 * Quando um refresh está em andamento, requests subsequentes aguardam o resultado.
 */
let isRefreshing = false;
let refreshSubscribers: Array<(token: string) => void> = [];

function onRefreshComplete(newToken: string): void {
  refreshSubscribers.forEach((cb) => cb(newToken));
  refreshSubscribers = [];
}

function addRefreshSubscriber(callback: (token: string) => void): void {
  refreshSubscribers.push(callback);
}

/**
 * Interceptor de response: tenta refresh do token antes de invalidar sessão.
 *
 * Fluxo de 401:
 * 1. Se existe refresh token em memória, tenta renová-lo.
 * 2. Se o refresh for bem-sucedido, re-executa o request original com o novo token.
 * 3. Se o refresh falhar, limpa sessão e dispara evento 'auth:session-expired'.
 * 4. Não faz window.location.href diretamente para evitar race conditions.
 */
apiClient.interceptors.response.use(
  (response) => response,
  async (error: AxiosError) => {
    const originalRequest = error.config as InternalAxiosRequestConfig & { _retry?: boolean };

    if (error.response?.status === 401 && originalRequest && !originalRequest._retry) {
      const refreshToken = getRefreshToken();

      if (refreshToken) {
        if (isRefreshing) {
          return new Promise((resolve) => {
            addRefreshSubscriber((newToken: string) => {
              originalRequest.headers.Authorization = `Bearer ${newToken}`;
              resolve(apiClient(originalRequest));
            });
          });
        }

        originalRequest._retry = true;
        isRefreshing = true;

        try {
          const { data } = await axios.post('/api/v1/identity/auth/refresh', { refreshToken }, { withCredentials: true });

          // Segurança: validar estrutura da resposta antes de armazenar tokens.
          // Previne armazenamento de valores malformados ou vazios em caso de
          // resposta inesperada do backend.
          const newAccessToken = data?.accessToken;
          const newRefreshToken = data?.refreshToken;

          if (
            typeof newAccessToken !== 'string' || newAccessToken.length === 0 ||
            typeof newRefreshToken !== 'string' || newRefreshToken.length === 0
          ) {
            throw new Error('Invalid token response structure');
          }

          storeTokens(newAccessToken, newRefreshToken);
          onRefreshComplete(newAccessToken);
          originalRequest.headers.Authorization = `Bearer ${newAccessToken}`;
          return apiClient(originalRequest);
        } catch {
          clearCsrfToken();
          clearAllTokens();
          window.dispatchEvent(new CustomEvent('auth:session-expired'));
          return Promise.reject(error);
        } finally {
          isRefreshing = false;
        }
      }

      clearCsrfToken();
      clearAllTokens();
      window.dispatchEvent(new CustomEvent('auth:session-expired'));
    }

    return Promise.reject(error);
  }
);

export default apiClient;
