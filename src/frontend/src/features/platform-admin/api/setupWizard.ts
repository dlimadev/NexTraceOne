import client from '../../../api/client';

// ── Types ─────────────────────────────────────────────────────────────────────

export interface CompletedStepDto {
  stepId: string;
  dataJson: string;
  completedAt: string;
}

export interface SetupWizardStatusResponse {
  completedSteps: CompletedStepDto[];
  isFullyConfigured: boolean;
}

export interface SaveStepRequest {
  tenantId: string;
  stepId: string;
  dataJson: string;
}

export interface SaveStepResponse {
  stepId: string;
  savedAt: string;
  isNew: boolean;
}

// ── API client ────────────────────────────────────────────────────────────────

export const setupWizardApi = {
  /**
   * GET /api/v1/admin/setup/status
   * Returns the completed wizard steps for the tenant.
   */
  getStatus: (tenantId: string) =>
    client
      .get<SetupWizardStatusResponse>('/admin/setup/status', { params: { tenantId } })
      .then((r) => r.data),

  /**
   * POST /api/v1/admin/setup/steps/{stepId}
   * Persists the configuration for a wizard step.
   */
  saveStep: (stepId: string, body: SaveStepRequest) =>
    client
      .post<SaveStepResponse>(`/admin/setup/steps/${stepId}`, body)
      .then((r) => r.data),
};
