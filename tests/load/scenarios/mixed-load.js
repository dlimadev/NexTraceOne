import http from 'k6/http';
import { sleep } from 'k6';
import { defaultThresholds } from '../config/thresholds.js';
import { getEnvironment } from '../config/environments.js';
import { login, authHeaders } from '../helpers/auth.js';
import { checkGetSuccess } from '../helpers/checks.js';

export const options = {
  thresholds: defaultThresholds,
  scenarios: {
    average: {
      executor: 'constant-vus',
      vus: 20,
      duration: '5m',
      tags: { scenario: 'average' },
    },
    peak: {
      executor: 'constant-vus',
      vus: 40,
      duration: '2m',
      startTime: '5m10s',
      tags: { scenario: 'peak' },
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

  // Simulate realistic user journey: catalog → contracts → governance

  // Step 1: User opens service catalog
  const servicesRes = http.get(
    `${baseUrl}/api/v1/catalog/services`,
    Object.assign({}, params, { tags: { name: 'GET /catalog/services' } }),
  );
  checkGetSuccess(servicesRes, 'mixed: catalog services');
  sleep(Math.random() * 2 + 1);

  // Step 2: User checks contracts
  const contractsRes = http.get(
    `${baseUrl}/api/v1/contracts/list`,
    Object.assign({}, params, { tags: { name: 'GET /contracts/list' } }),
  );
  checkGetSuccess(contractsRes, 'mixed: contracts list');
  sleep(Math.random() * 2 + 1);

  // Step 3: User checks contract summary
  const summaryRes = http.get(
    `${baseUrl}/api/v1/contracts/summary`,
    Object.assign({}, params, { tags: { name: 'GET /contracts/summary' } }),
  );
  checkGetSuccess(summaryRes, 'mixed: contracts summary');
  sleep(Math.random() * 2 + 1);

  // Step 4: User checks governance
  const healthRes = http.get(
    `${baseUrl}/api/v1/governance/platform-health`,
    Object.assign({}, params, { tags: { name: 'GET /governance/platform-health' } }),
  );
  checkGetSuccess(healthRes, 'mixed: platform health');
  sleep(Math.random() * 2 + 1);

  // Step 5: User checks engineering graph
  const graphRes = http.get(
    `${baseUrl}/api/v1/catalog/graph`,
    Object.assign({}, params, { tags: { name: 'GET /catalog/graph' } }),
  );
  checkGetSuccess(graphRes, 'mixed: catalog graph');
  sleep(Math.random() * 3 + 1);
}
