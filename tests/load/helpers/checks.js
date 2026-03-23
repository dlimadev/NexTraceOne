import { check } from 'k6';

/**
 * Checks that a response has the expected HTTP status code.
 */
export function checkStatus(res, expectedStatus, name) {
  const label = name || `status is ${expectedStatus}`;
  return check(res, {
    [label]: (r) => r.status === expectedStatus,
  });
}

/**
 * Checks common success conditions for a GET request.
 */
export function checkGetSuccess(res, name) {
  const prefix = name ? `${name}: ` : '';
  return check(res, {
    [`${prefix}status is 200`]: (r) => r.status === 200,
    [`${prefix}response time < 5s`]: (r) => r.timings.duration < 5000,
    [`${prefix}body is not empty`]: (r) => r.body && r.body.length > 0,
  });
}

/**
 * Checks common success conditions for a POST request.
 */
export function checkPostSuccess(res, name) {
  const prefix = name ? `${name}: ` : '';
  return check(res, {
    [`${prefix}status is 200 or 201`]: (r) => r.status === 200 || r.status === 201,
    [`${prefix}response time < 5s`]: (r) => r.timings.duration < 5000,
  });
}

/**
 * Checks that a response body contains valid JSON.
 */
export function checkJsonBody(res, name) {
  const prefix = name ? `${name}: ` : '';
  return check(res, {
    [`${prefix}valid JSON response`]: (r) => {
      try {
        r.json();
        return true;
      } catch (_) {
        return false;
      }
    },
  });
}
