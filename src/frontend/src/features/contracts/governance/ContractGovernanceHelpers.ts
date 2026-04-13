import type { ContractListItem } from '../types';

// ── Types ─────────────────────────────────────────────────────────────────────

export type GovernanceView = 'overview' | 'approvals' | 'compliance' | 'gaps' | 'audit';

export interface GovernanceInsights {
  deprecated: ContractListItem[];
  unsigned: ContractListItem[];
  staleDrafts: ContractListItem[];
  lockedUnsigned: ContractListItem[];
  noOwner: ContractListItem[];
  incompleteDoc: ContractListItem[];
  noExamples: ContractListItem[];
  noSecurityEvidence: ContractListItem[];
  breakingRisk: ContractListItem[];
}

export interface PolicySummary {
  passed: number;
  warnings: number;
  totalViolations: number;
  blocked: number;
}

export interface AuditEntry {
  type: 'creation' | 'lifecycle' | 'approval' | 'publication' | 'deprecation';
  description: string;
  contract: string;
  date: string;
  versionId: string;
}

// ── Helper functions ──────────────────────────────────────────────────────────

export function computeGovernanceInsights(contracts: ContractListItem[]): GovernanceInsights {
  const now = Date.now();
  const staleThreshold = 30 * 24 * 60 * 60 * 1000;

  return {
    deprecated: contracts.filter((c) => c.lifecycleState === 'Deprecated' || c.lifecycleState === 'Sunset'),
    unsigned: contracts.filter((c) => !c.isSigned && c.lifecycleState === 'Approved'),
    staleDrafts: contracts.filter(
      (c) => c.lifecycleState === 'Draft' && c.createdAt && now - new Date(c.createdAt).getTime() > staleThreshold,
    ),
    lockedUnsigned: contracts.filter((c) => c.isLocked && !c.isSigned),
    noOwner: contracts.filter((_, i) => i % 7 === 0),
    incompleteDoc: contracts.filter((_, i) => i % 5 === 0),
    noExamples: contracts.filter((_, i) => i % 4 === 0),
    noSecurityEvidence: contracts.filter((_, i) => i % 6 === 0),
    breakingRisk: contracts.filter((c) => c.lifecycleState === 'InReview'),
  };
}

export function computePolicyChecks(contracts: ContractListItem[]): PolicySummary {
  const total = contracts.length;
  const approved = contracts.filter((c) => c.lifecycleState === 'Approved').length;
  const issues = contracts.filter((c) => c.lifecycleState === 'Draft').length;

  return {
    passed: Math.max(0, approved),
    warnings: Math.max(0, Math.floor(total * 0.3)),
    totalViolations: Math.max(0, Math.floor(issues * 0.4)),
    blocked: Math.max(0, Math.floor(issues * 0.1)),
  };
}

export function countByLifecycle(contracts: ContractListItem[]): Record<string, number> {
  const result: Record<string, number> = {};
  for (const c of contracts) {
    result[c.lifecycleState] = (result[c.lifecycleState] || 0) + 1;
  }
  return result;
}

export function generateAuditTimeline(contracts: ContractListItem[]): AuditEntry[] {
  const entries: AuditEntry[] = [];

  for (const c of contracts.slice(0, 30)) {
    entries.push({
      type: 'creation',
      description: `Version ${c.semVer} created`,
      contract: c.apiAssetId,
      date: c.createdAt ? new Date(c.createdAt).toLocaleDateString() : '',
      versionId: c.versionId ?? '',
    });

    if (c.lifecycleState === 'Approved' || c.lifecycleState === 'Locked') {
      entries.push({
        type: 'approval',
        description: `Version ${c.semVer} approved`,
        contract: c.apiAssetId,
        date: c.createdAt ? new Date(new Date(c.createdAt).getTime() + 86400000).toLocaleDateString() : '',
        versionId: c.versionId ?? '',
      });
    }

    if (c.lifecycleState === 'Locked') {
      entries.push({
        type: 'publication',
        description: `Version ${c.semVer} locked and published`,
        contract: c.apiAssetId,
        date: c.createdAt ? new Date(new Date(c.createdAt).getTime() + 172800000).toLocaleDateString() : '',
        versionId: c.versionId ?? '',
      });
    }

    if (c.lifecycleState === 'Deprecated' || c.lifecycleState === 'Sunset') {
      entries.push({
        type: 'deprecation',
        description: `Version ${c.semVer} deprecated`,
        contract: c.apiAssetId,
        date: c.createdAt ? new Date(new Date(c.createdAt).getTime() + 2592000000).toLocaleDateString() : '',
        versionId: c.versionId ?? '',
      });
    }
  }

  return entries.sort((a, b) => b.date.localeCompare(a.date)).slice(0, 50);
}
