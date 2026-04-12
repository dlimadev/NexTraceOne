import { Bot, Users, User, Shield, Lock, Globe } from 'lucide-react';

// ── Interfaces ────────────────────────────────────────────────────────────────

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

export interface ExecutionResult {
  executionId: string;
  agentId: string;
  status: string;
  output: string;
  modelUsed: string;
  providerUsed: string;
  promptTokens: number;
  completionTokens: number;
  totalTokens: number;
  durationMs: number;
  artifacts: ArtifactResult[];
}

export interface ArtifactResult {
  artifactId: string;
  title: string;
  artifactType: string;
  format: string;
  content: string;
  reviewStatus: string;
}

export interface CreateAgentDialogProps {
  isOpen: boolean;
  onClose: () => void;
  onCreated: () => void;
  categories: string[];
  defaultCategory: string;
}

export interface ExecuteAgentDialogProps {
  isOpen: boolean;
  agent: AgentListItem | null;
  onClose: () => void;
}

export interface AgentCardProps {
  agent: AgentListItem;
  onView: () => void;
  onExecute: () => void;
  t: (key: string) => string;
}

// ── Constants ─────────────────────────────────────────────────────────────────

export const FALLBACK_AGENT_CATEGORIES = [
  'General',
  'ApiDesign',
  'TestGeneration',
  'EventDesign',
  'Documentation',
  'Analysis',
  'CodeReview',
  'Security',
  'Operations',
];

// ── Helper functions ──────────────────────────────────────────────────────────

export function humanizeEnumValue(value: string): string {
  return value
    .replace(/([a-z0-9])([A-Z])/g, '$1 $2')
    .replace(/_/g, ' ')
    .trim();
}

export function ownershipIcon(type: string) {
  switch (type) {
    case 'System': return <Shield size={12} className="text-accent" />;
    case 'Tenant': return <Users size={12} className="text-info" />;
    case 'User': return <User size={12} className="text-success" />;
    default: return <Bot size={12} className="text-muted" />;
  }
}

export function visibilityIcon(vis: string) {
  switch (vis) {
    case 'Private': return <Lock size={12} className="text-warning" />;
    case 'Team': return <Users size={12} className="text-info" />;
    case 'Tenant': return <Globe size={12} className="text-success" />;
    default: return null;
  }
}

export function statusVariant(status: string): 'success' | 'warning' | 'default' | 'info' {
  switch (status) {
    case 'Published':
    case 'Active': return 'success';
    case 'Draft': return 'default';
    case 'PendingReview': return 'info';
    case 'Archived': return 'warning';
    case 'Blocked': return 'warning';
    default: return 'default';
  }
}
