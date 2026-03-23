/**
 * Environment-specific settings for NexTraceOne k6 load tests.
 */

export const environments = {
  local: {
    baseUrl: 'http://localhost:5187',
    defaultUsername: 'admin@nextrace.local',
    defaultPassword: 'Admin123!',
    defaultTenantId: 'default-tenant',
  },
  staging: {
    baseUrl: 'https://staging-api.nextrace.example.com',
    defaultUsername: '',
    defaultPassword: '',
    defaultTenantId: '',
  },
  production: {
    baseUrl: 'https://api.nextrace.example.com',
    defaultUsername: '',
    defaultPassword: '',
    defaultTenantId: '',
  },
};

export function getEnvironment() {
  const envName = __ENV.K6_ENVIRONMENT || 'local';
  const env = environments[envName];
  if (!env) {
    throw new Error(`Unknown environment: ${envName}. Use: local, staging, production`);
  }
  return {
    baseUrl: __ENV.K6_BASE_URL || env.baseUrl,
    username: __ENV.K6_USERNAME || env.defaultUsername,
    password: __ENV.K6_PASSWORD || env.defaultPassword,
    tenantId: __ENV.K6_TENANT_ID || env.defaultTenantId,
  };
}
