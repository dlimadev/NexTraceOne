import client from '../../../api/client';

// ── Types ─────────────────────────────────────────────────────────────────────

export interface DashboardTemplateDto {
  id: string;
  name: string;
  description: string;
  persona: string;
  category: string;
  tags: string[];
  version: string;
  isSystem: boolean;
  installCount: number;
  createdAt: string;
}

export interface ListDashboardTemplatesResponse {
  items: DashboardTemplateDto[];
  totalCount: number;
  page: number;
  pageSize: number;
}

// ── API client ────────────────────────────────────────────────────────────────

export const dashboardTemplatesApi = {
  /**
   * GET /api/v1/governance/dashboard-templates
   * Lists dashboard templates, optionally filtered by persona and category.
   */
  list: (tenantId: string, persona?: string, category?: string, page = 1, pageSize = 50) =>
    client
      .get<ListDashboardTemplatesResponse>('/governance/dashboard-templates', {
        params: { tenantId, persona, category, page, pageSize },
      })
      .then((r) => r.data),

  /**
   * POST /api/v1/governance/dashboard-templates/{id}/instantiate
   * Creates a new dashboard from a template.
   */
  instantiate: (
    templateId: string,
    body: { tenantId: string; userId: string; customName?: string },
  ) =>
    client
      .post<{ dashboardId: string; dashboardName: string; sourceTemplateId: string }>(
        `/governance/dashboard-templates/${templateId}/instantiate`,
        body,
      )
      .then((r) => r.data),
};
