/**
 * Shared threshold configurations for NexTraceOne k6 load tests.
 */

export const defaultThresholds = {
  http_req_duration: ['p(95)<2000', 'p(99)<5000'],
  http_req_failed: ['rate<0.05'],
  http_reqs: ['rate>10'],
};

export const strictThresholds = {
  http_req_duration: ['p(95)<1000', 'p(99)<3000'],
  http_req_failed: ['rate<0.02'],
  http_reqs: ['rate>20'],
};

export const relaxedThresholds = {
  http_req_duration: ['p(95)<5000', 'p(99)<10000'],
  http_req_failed: ['rate<0.10'],
  http_reqs: ['rate>5'],
};
