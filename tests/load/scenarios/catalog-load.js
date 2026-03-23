import http from 'k6/http';
import { sleep } from 'k6';
import { defaultThresholds } from '../config/thresholds.js';
import { getEnvironment } from '../config/environments.js';
import { login, authHeaders } from '../helpers/auth.js';
import { checkGetSuccess } from '../helpers/checks.js';

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
      vus: 15,
      duration: '2m',
      startTime: '35s',
      tags: { scenario: 'load' },
    },
    stress: {
      executor: 'constant-vus',
      vus: 30,
      duration: '1m',
      startTime: '2m40s',
      tags: { scenario: 'stress' },
    },
  },
};

export function setup() {
  const token = login();
  return { token };
}

export default function (data) {
  const env = getEnvironment();
  const baseUrl = env.baseUrl;
  const params = authHeaders(data.token);

  // GET /api/v1/catalog/services
  const servicesRes = http.get(
    `${baseUrl}/api/v1/catalog/services`,
    Object.assign({}, params, { tags: { name: 'GET /catalog/services' } }),
  );
  checkGetSuccess(servicesRes, 'catalog services');
  sleep(Math.random() * 2 + 1);

  // GET /api/v1/catalog/services/summary
  const summaryRes = http.get(
    `${baseUrl}/api/v1/catalog/services/summary`,
    Object.assign({}, params, { tags: { name: 'GET /catalog/services/summary' } }),
  );
  checkGetSuccess(summaryRes, 'catalog summary');
  sleep(Math.random() * 2 + 1);

  // GET /api/v1/catalog/graph
  const graphRes = http.get(
    `${baseUrl}/api/v1/catalog/graph`,
    Object.assign({}, params, { tags: { name: 'GET /catalog/graph' } }),
  );
  checkGetSuccess(graphRes, 'catalog graph');
  sleep(Math.random() * 2 + 1);
}
