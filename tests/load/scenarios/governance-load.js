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

  // GET /api/v1/governance/platform-health
  const healthRes = http.get(
    `${baseUrl}/api/v1/governance/platform-health`,
    Object.assign({}, params, { tags: { name: 'GET /governance/platform-health' } }),
  );
  checkGetSuccess(healthRes, 'platform health');
  sleep(Math.random() * 2 + 1);

  // GET /api/v1/governance/executive-drill-down
  const execRes = http.get(
    `${baseUrl}/api/v1/governance/executive-drill-down`,
    Object.assign({}, params, { tags: { name: 'GET /governance/executive-drill-down' } }),
  );
  checkGetSuccess(execRes, 'executive drill-down');
  sleep(Math.random() * 2 + 1);

  // GET /api/v1/governance/finops-summary
  const finopsRes = http.get(
    `${baseUrl}/api/v1/governance/finops-summary`,
    Object.assign({}, params, { tags: { name: 'GET /governance/finops-summary' } }),
  );
  checkGetSuccess(finopsRes, 'finops summary');
  sleep(Math.random() * 2 + 1);

  // GET /api/v1/governance/compliance-checks
  const complianceRes = http.get(
    `${baseUrl}/api/v1/governance/compliance-checks`,
    Object.assign({}, params, { tags: { name: 'GET /governance/compliance-checks' } }),
  );
  checkGetSuccess(complianceRes, 'compliance checks');
  sleep(Math.random() * 2 + 1);
}
