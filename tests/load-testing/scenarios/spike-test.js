import http from 'k6/http';
import { check, sleep } from 'k6';
import { apiUrl, COMMON_HEADERS } from '../config/base-config.js';
import { THRESHOLDS } from '../config/thresholds.js';

export const options = {
  // Spike test: picos súbitos de tráfego
  stages: [
    { duration: '1m', target: 20 },    // Linha base: 20 VUs
    { duration: '30s', target: 200 },  // Spike súbito: 200 VUs em 30 segundos
    { duration: '1m', target: 200 },   // Manter pico
    { duration: '30s', target: 20 },   // Queda súbita
    { duration: '2m', target: 20 },    // Recuperar
    { duration: '30s', target: 300 },  // Segundo spike ainda maior
    { duration: '1m', target: 300 },   // Manter
    { duration: '30s', target: 20 },   // Queda
    { duration: '2m', target: 0 },     // Ramp down
  ],
  thresholds: THRESHOLDS,
};

export default function () {
  // Teste rápido durante spikes
  const healthRes = http.get(apiUrl('/platform/health'));
  check(healthRes, {
    'system responsive during spike': (r) => r.status < 500,
    'response time acceptable': (r) => r.timings.duration < 1000,
  });

  sleep(0.5);

  const contractsRes = http.get(apiUrl('/contracts/list'), {
    headers: COMMON_HEADERS,
  });

  check(contractsRes, {
    'contracts accessible during spike': (r) => r.status < 500,
  });

  sleep(1);
}
