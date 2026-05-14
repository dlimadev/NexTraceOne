import http from 'k6/http';
import { check, sleep } from 'k6';
import { apiUrl, COMMON_HEADERS } from '../config/base-config.js';
import { THRESHOLDS } from '../config/thresholds.js';

export const options = {
  // Smoke test: poucos VUs por pouco tempo para validar que o sistema está funcionando
  vus: 5,
  duration: '30s',
  thresholds: THRESHOLDS,
};

export default function () {
  // Teste 1: Health Check
  const healthRes = http.get(apiUrl('/platform/health'));
  check(healthRes, {
    'health check status is 200': (r) => r.status === 200,
    'health check response time < 200ms': (r) => r.timings.duration < 200,
  });

  sleep(1);

  // Teste 2: Login (autenticação básica)
  const loginPayload = JSON.stringify({
    email: 'loadtest@nextraceone.com',
    password: 'LoadTest@2026!',
  });

  const loginRes = http.post(apiUrl('/auth/login'), loginPayload, {
    headers: COMMON_HEADERS,
  });

  check(loginRes, {
    'login status is 200 or 401': (r) => r.status === 200 || r.status === 401,
  });

  sleep(1);

  // Teste 3: Listar contratos (endpoint comum)
  const contractsRes = http.get(apiUrl('/contracts/list'), {
    headers: COMMON_HEADERS,
  });

  check(contractsRes, {
    'contracts list status is 200 or 401': (r) => r.status === 200 || r.status === 401,
  });

  sleep(1);
}
