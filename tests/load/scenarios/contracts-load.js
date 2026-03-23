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
      vus: 10,
      duration: '2m',
      startTime: '35s',
      tags: { scenario: 'load' },
    },
    stress: {
      executor: 'constant-vus',
      vus: 20,
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

  // GET /api/v1/contracts/list
  const listRes = http.get(
    `${baseUrl}/api/v1/contracts/list`,
    Object.assign({}, params, { tags: { name: 'GET /contracts/list' } }),
  );
  checkGetSuccess(listRes, 'contracts list');
  sleep(Math.random() * 2 + 1);

  // GET /api/v1/contracts/summary
  const summaryRes = http.get(
    `${baseUrl}/api/v1/contracts/summary`,
    Object.assign({}, params, { tags: { name: 'GET /contracts/summary' } }),
  );
  checkGetSuccess(summaryRes, 'contracts summary');
  sleep(Math.random() * 2 + 1);
}
