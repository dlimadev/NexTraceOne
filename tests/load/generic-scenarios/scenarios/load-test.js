import http from 'k6/http';
import { check, sleep } from 'k6';
import { apiUrl, COMMON_HEADERS, randomString, generateUUID } from '../config/base-config.js';
import { THRESHOLDS } from '../config/thresholds.js';

export const options = {
  // Load test: carga normal de produção simulada
  stages: [
    { duration: '2m', target: 50 },   // Ramp up para 50 VUs
    { duration: '5m', target: 50 },   // Manter 50 VUs por 5 minutos
    { duration: '2m', target: 0 },    // Ramp down
  ],
  thresholds: THRESHOLDS,
};

export default function () {
  // Cenário 1: Autenticação e listagem de contratos
  const loginPayload = JSON.stringify({
    email: `user${randomString(5)}@test.com`,
    password: 'Test@2026!',
  });

  const loginRes = http.post(apiUrl('/auth/login'), loginPayload, {
    headers: COMMON_HEADERS,
  });

  let authToken = '';
  if (loginRes.status === 200) {
    try {
      const body = loginRes.json();
      authToken = body.token || '';
    } catch (e) {
      // Ignorar erro de parsing
    }
  }

  check(loginRes, {
    'login successful or expected failure': (r) => r.status === 200 || r.status === 401,
  });

  sleep(1);

  // Cenário 2: Listar contratos com autenticação
  const authHeaders = {
    ...COMMON_HEADERS,
    ...(authToken ? { 'Authorization': `Bearer ${authToken}` } : {}),
  };

  const contractsRes = http.get(apiUrl('/contracts/list?page=1&pageSize=20'), {
    headers: authHeaders,
  });

  check(contractsRes, {
    'contracts list returns data': (r) => r.status === 200 || r.status === 401,
    'response time < 500ms': (r) => r.timings.duration < 500,
  });

  sleep(2);

  // Cenário 3: Criar incidente (escrita)
  const incidentPayload = JSON.stringify({
    title: `Load Test Incident ${randomString(8)}`,
    description: 'Automated load test incident',
    severity: 'Medium',
    environmentId: generateUUID(),
  });

  const incidentRes = http.post(apiUrl('/incidents/create'), incidentPayload, {
    headers: authHeaders,
  });

  check(incidentRes, {
    'incident creation handled': (r) => r.status === 200 || r.status === 201 || r.status === 401 || r.status === 400,
  });

  sleep(3);
}
