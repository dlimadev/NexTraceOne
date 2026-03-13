import client from './client';
import type {
  LicenseStatus,
  LicenseHealthResult,
  CapabilityStatus,
  LicenseThresholdAlert,
} from '../types';

/** API do módulo de Licensing — ativação, verificação, quotas e trial. */
export const licensingApi = {
  activate: (data: { licenseKey: string; hardwareFingerprint: string }) =>
    client
      .post<{ licenseId: string; activatedAt: string }>('/licensing/activate', data)
      .then((r) => r.data),

  verify: (licenseKey: string, hardwareFingerprint: string) =>
    client
      .get<{ isValid: boolean; reason?: string }>('/licensing/verify', {
        params: { licenseKey, hardwareFingerprint },
      })
      .then((r) => r.data),

  getStatus: (licenseKey: string) =>
    client
      .get<LicenseStatus>('/licensing/status', { params: { licenseKey } })
      .then((r) => r.data),

  getCapability: (capabilityCode: string, licenseKey: string, hardwareFingerprint: string) =>
    client
      .get<CapabilityStatus>(`/licensing/capabilities/${capabilityCode}`, {
        params: { licenseKey, hardwareFingerprint },
      })
      .then((r) => r.data),

  trackUsage: (data: {
    licenseKey: string;
    hardwareFingerprint: string;
    metricCode: string;
    quantity: number;
  }) => client.post<void>('/licensing/usage', data).then((r) => r.data),

  getThresholds: (licenseKey: string) =>
    client
      .get<LicenseThresholdAlert[]>('/licensing/thresholds', { params: { licenseKey } })
      .then((r) => r.data),

  startTrial: (data: { customerName: string; email: string }) =>
    client
      .post<{ licenseKey: string; expiresAt: string }>('/licensing/trial/start', data)
      .then((r) => r.data),

  extendTrial: (data: { licenseKey: string; additionalDays: number }) =>
    client
      .post<{ newExpiresAt: string }>('/licensing/trial/extend', data)
      .then((r) => r.data),

  convertTrial: (data: {
    licenseKey: string;
    edition: string;
    expiresAt: string;
    maxActivations: number;
    gracePeriodDays: number;
  }) =>
    client
      .post<{ licenseId: string; convertedAt: string }>('/licensing/trial/convert', data)
      .then((r) => r.data),

  getHealth: (licenseKey: string) =>
    client
      .get<LicenseHealthResult>('/licensing/health', { params: { licenseKey } })
      .then((r) => r.data),
};
