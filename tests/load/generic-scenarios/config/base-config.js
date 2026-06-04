// Configuração base para todos os testes de carga
export const BASE_URL = __ENV.BASE_URL || 'http://localhost:5000';
export const API_VERSION = 'v1';

// Credenciais de teste (usar variáveis de ambiente em produção)
export const TEST_USER_EMAIL = __ENV.TEST_USER_EMAIL || 'loadtest@nextraceone.com';
export const TEST_USER_PASSWORD = __ENV.TEST_USER_PASSWORD || 'LoadTest@2026!';

// Headers comuns
export const COMMON_HEADERS = {
  'Content-Type': 'application/json',
  'Accept': 'application/json',
};

// Função utilitária para construir URLs da API
export function apiUrl(path) {
  return `${BASE_URL}/api/${API_VERSION}${path}`;
}

// Função para gerar dados aleatórios
export function randomString(length = 10) {
  const chars = 'ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789';
  let result = '';
  for (let i = 0; i < length; i++) {
    result += chars.charAt(Math.floor(Math.random() * chars.length));
  }
  return result;
}

// Função para gerar UUID
export function generateUUID() {
  return 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, function(c) {
    const r = Math.random() * 16 | 0;
    const v = c === 'x' ? r : (r & 0x3 | 0x8);
    return v.toString(16);
  });
}
