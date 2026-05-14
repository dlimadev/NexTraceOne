import http from 'k6/http';
import { check, sleep } from 'k6';
import { apiUrl, COMMON_HEADERS, randomString } from '../config/base-config.js';
import { STRESS_THRESHOLDS } from '../config/thresholds.js';

export const options = {
  // Stress test: carga extrema para encontrar ponto de ruptura
  stages: [
    { duration: '2m', target: 100 },   // Ramp up para 100 VUs
    { duration: '5m', target: 100 },   // Manter 100 VUs
    { duration: '2m', target: 200 },   // Aumentar para 200 VUs
    { duration: '5m', target: 200 },   // Manter 200 VUs
    { duration: '2m', target: 300 },   // Aumentar para 300 VUs (stress máximo)
    { duration: '3m', target: 300 },   // Manter stress máximo
    { duration: '5m', target: 0 },     // Ramp down
  ],
  thresholds: STRESS_THRESHOLDS,
};

export default function () {
  // Cenário agressivo: múltiplos endpoints simultâneos
  
  // 1. Health check contínuo
  const healthRes = http.get(apiUrl('/platform/health'));
  check(healthRes, {
    'health check responsive': (r) => r.status === 200 || r.status >= 500,
  });

  // 2. Múltiplas leituras
  const contractsRes = http.get(apiUrl('/contracts/list'), {
    headers: COMMON_HEADERS,
  });

  check(contractsRes, {
    'contracts endpoint accessible': (r) => r.status < 500,
  });

  sleep(0.5);

  // 3. Escritas concorrentes
  const incidentPayload = JSON.stringify({
    title: `Stress Test ${randomString(10)}`,
    description: 'Stress test incident',
    severity: 'High',
  });

  const incidentRes = http.post(apiUrl('/incidents/create'), incidentPayload, {
    headers: COMMON_HEADERS,
  });

  check(incidentRes, {
    'incident creation handled under stress': (r) => r.status < 500 || r.status === 429,
  });

  sleep(1);
}
