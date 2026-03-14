import client from '../../../api/client';
import type {
  LicenseStatus,
  LicenseHealthResult,
  CapabilityStatus,
  LicenseThresholdAlert,
} from '../../../types';

/** Tipos para operações de vendor ops. */
export interface VendorLicenseItem {
  licenseId: string;
  licenseKey: string;
  customerName: string;
  isActive: boolean;
  licenseType: string;
  edition: string;
  deploymentModel: string;
  status: string;
  issuedAt: string;
  expiresAt: string;
  activationCount: number;
  isTrial: boolean;
  trialConverted: boolean;
}

export interface VendorLicenseList {
  items: VendorLicenseItem[];
  totalCount: number;
  page: number;
  pageSize: number;
}

export interface IssueLicenseRequest {
  customerName: string;
  durationDays: number;
  maxActivations: number;
  type?: number;
  edition?: number;
  gracePeriodDays?: number;
  deploymentModel?: number;
  activationMode?: number;
  commercialModel?: number;
  meteringMode?: number;
}

export interface IssueLicenseResponse {
  licenseId: string;
  licenseKey: string;
  customerName: string;
  issuedAt: string;
  expiresAt: string;
  licenseType: string;
  edition: string;
  deploymentModel: string;
}

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

  // ─── Vendor Operations (backoffice interno) ─────────────────────

  vendorListLicenses: (page: number = 1, pageSize: number = 20) =>
    client
      .get<VendorLicenseList>('/licensing/vendor/licenses', { params: { page, pageSize } })
      .then((r) => r.data),

  vendorIssueLicense: (data: IssueLicenseRequest) =>
    client
      .post<IssueLicenseResponse>('/licensing/vendor/issue', data)
      .then((r) => r.data),

  vendorRevokeLicense: (licenseKey: string) =>
    client
      .post<{ licenseId: string; licenseKey: string; customerName: string }>(
        '/licensing/vendor/revoke',
        { licenseKey },
      )
      .then((r) => r.data),

  vendorRehostLicense: (licenseKey: string) =>
    client
      .post<{ licenseId: string; licenseKey: string; customerName: string }>(
        '/licensing/vendor/rehost',
        { licenseKey },
      )
      .then((r) => r.data),
};
