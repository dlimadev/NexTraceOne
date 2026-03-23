import http from 'k6/http';
import { sleep } from 'k6';
import { defaultThresholds } from '../config/thresholds.js';
import { getEnvironment } from '../config/environments.js';
import { checkPostSuccess, checkGetSuccess } from '../helpers/checks.js';

export const options = {
  thresholds: defaultThresholds,
  scenarios: {
    smoke: {
      executor: 'constant-vus',
      vus: 1,
      duration: '30s',
      tags: { scenario: 'smoke' },
    },
    load: {
      executor: 'constant-vus',
      vus: 10,
      duration: '2m',
      startTime: '35s',
      tags: { scenario: 'load' },
    },
    stress: {
      executor: 'constant-vus',
      vus: 25,
      duration: '1m',
      startTime: '2m40s',
      tags: { scenario: 'stress' },
    },
  },
};

export default function () {
  const env = getEnvironment();
  const baseUrl = env.baseUrl;
  const jsonHeaders = { headers: { 'Content-Type': 'application/json' } };

  // POST /api/v1/identity/auth/login
  const loginPayload = JSON.stringify({
    username: env.username,
    password: env.password,
    tenantId: env.tenantId,
  });

  const loginRes = http.post(
    `${baseUrl}/api/v1/identity/auth/login`,
    loginPayload,
    Object.assign({}, jsonHeaders, { tags: { name: 'POST /auth/login' } }),
  );
  checkPostSuccess(loginRes, 'login');
  sleep(1);

  // Extract token for subsequent requests
  let token = '';
  if (loginRes.status === 200) {
    try {
      const body = loginRes.json();
      token = body.token || body.accessToken || '';
    } catch (_) {
      // token stays empty
    }
  }

  const authParams = {
    headers: {
      'Content-Type': 'application/json',
      Authorization: `Bearer ${token}`,
    },
  };

  // POST /api/v1/identity/auth/refresh
  const refreshRes = http.post(
    `${baseUrl}/api/v1/identity/auth/refresh`,
    JSON.stringify({ token }),
    Object.assign({}, authParams, { tags: { name: 'POST /auth/refresh' } }),
  );
  checkPostSuccess(refreshRes, 'refresh');
  sleep(1);

  // GET /api/v1/identity/auth/me
  const meRes = http.get(
    `${baseUrl}/api/v1/identity/auth/me`,
    Object.assign({}, authParams, { tags: { name: 'GET /auth/me' } }),
  );
  checkGetSuccess(meRes, 'me');
  sleep(1);
}
