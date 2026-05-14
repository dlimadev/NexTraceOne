import http from 'k6/http';
import { check, sleep } from 'k6';
import { apiUrl, COMMON_HEADERS, randomString } from '../config/base-config.js';
import { ENDURANCE_THRESHOLDS } from '../config/thresholds.js';

export const options = {
  // Endurance test: teste de longa duração (soak test)
  vus: 30,
  duration: '1h',  // 1 hora de carga sustentada
  thresholds: ENDURANCE_THRESHOLDS,
};

let iterationCount = 0;

export default function () {
  iterationCount++;

  // Cenário variado para simular uso real prolongado
  
  // 1. Autenticação periódica
  if (iterationCount % 10 === 0) {
    const loginPayload = JSON.stringify({
      email: `endurance${randomString(5)}@test.com`,
      password: 'Test@2026!',
    });

    const loginRes = http.post(apiUrl('/auth/login'), loginPayload, {
      headers: COMMON_HEADERS,
    });

    check(loginRes, {
      'login stable over time': (r) => r.status === 200 || r.status === 401,
    });

    sleep(2);
  }

  // 2. Leituras frequentes
  const contractsRes = http.get(apiUrl('/contracts/list'), {
    headers: COMMON_HEADERS,
  });

  check(contractsRes, {
    'contracts endpoint stable': (r) => r.status < 500,
    'no memory leak indicators': (r) => r.timings.duration < 1000,
  });

  sleep(3);

  // 3. Escritas ocasionais
  if (iterationCount % 5 === 0) {
    const notificationPayload = JSON.stringify({
      title: `Endurance Test ${randomString(8)}`,
      message: 'Long-running test notification',
      category: 'Info',
    });

    const notifRes = http.post(apiUrl('/notifications/submit'), notificationPayload, {
      headers: COMMON_HEADERS,
    });

    check(notifRes, {
      'notification creation stable': (r) => r.status < 500,
    });
  }

  sleep(5);

  // Log periódico para monitoramento
  if (iterationCount % 50 === 0) {
    console.log(`Endurance test iteration ${iterationCount} completed`);
  }
}
