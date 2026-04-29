import client from '../../../api/client';

// ── Types ─────────────────────────────────────────────────────────────────────

export interface HomeCardDto {
  key: string;
  title: string;
  value: string | null;
  trend: string | null;
  unit: string | null;
  severity: 'info' | 'success' | 'warning' | 'critical';
  linkTo: string | null;
  isSimulated: boolean;
}

export interface QuickActionDto {
  key: string;
  label: string;
  url: string;
  icon: string;
}

export interface PersonaHomeResponse {
  persona: string;
  userId: string;
  cards: HomeCardDto[];
  quickActions: QuickActionDto[];
  isSimulated: boolean;
  simulatedNote: string | null;
}

// ── API client ────────────────────────────────────────────────────────────────

export const personaHomeApi = {
  /**
   * GET /api/v1/governance/persona-home
   * Returns persona-specific home page KPI cards and quick actions.
   */
  getPersonaHome: (userId: string, persona: string, tenantId: string) =>
    client
      .get<PersonaHomeResponse>('/governance/persona-home', {
        params: { userId, persona, tenantId },
      })
      .then((r) => r.data),
};
