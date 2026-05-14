// Tipos compartilhados para AI Agents - arquivo puro TypeScript sem JSX
export interface AgentListItem {
  agentId: string;
  name: string;
  displayName: string;
  slug: string;
  description: string;
  category: string;
  isOfficial: boolean;
  isActive: boolean;
  capabilities: string;
  targetPersona: string;
  icon: string;
  preferredModelId: string | null;
  ownershipType: string;
  visibility: string;
  publicationStatus: string;
  version: number;
  executionCount: number;
}

export interface AgentsResponse {
  items: AgentListItem[];
  totalCount: number;
}
