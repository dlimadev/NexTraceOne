import type { ContractVersionDetail, ContractLifecycleState } from '../types';
import type { ApprovalState, PolicyCheckResult } from '../types/domain';
import type { StudioActivityItem, StudioContract, StudioRelationship } from './studioTypes';

function mapLifecycleToApprovalState(lifecycleState: ContractLifecycleState): ApprovalState | undefined {
  switch (lifecycleState) {
    case 'InReview':
      return 'InReview';
    case 'Approved':
    case 'Locked':
    case 'Deprecated':
    case 'Sunset':
    case 'Retired':
      return 'Approved';
    default:
      return undefined;
  }
}

function buildPolicyChecks(detail: ContractVersionDetail): PolicyCheckResult[] {
  return (detail.ruleViolations ?? []).map((violation, index) => ({
    policyId: `${detail.id}-rule-${index}`,
    policyName: violation.ruleName,
    passed: false,
    severity: violation.severity === 'Error' ? 'Error' : violation.severity === 'Warning' ? 'Warning' : 'Info',
    message: violation.message,
  }));
}

function buildRecentActivity(detail: ContractVersionDetail): StudioActivityItem[] {
  const activity: StudioActivityItem[] = [
    {
      id: `${detail.id}-created`,
      action: 'Contract version created',
      actor: detail.provenance?.importedBy || detail.technicalOwner || 'system',
      timestamp: detail.createdAt,
    },
  ];

  if (detail.signedAt) {
    activity.push({
      id: `${detail.id}-signed`,
      action: 'Contract version signed',
      actor: detail.signedBy || 'system',
      timestamp: detail.signedAt,
    });
  }

  if (detail.lockedAt) {
    activity.push({
      id: `${detail.id}-locked`,
      action: 'Contract version locked',
      actor: detail.lockedBy || 'system',
      timestamp: detail.lockedAt,
    });
  }

  if (detail.deprecationDate) {
    activity.push({
      id: `${detail.id}-deprecated`,
      action: 'Contract version deprecated',
      actor: detail.technicalOwner || 'system',
      timestamp: detail.deprecationDate,
      detail: detail.deprecationNotice,
    });
  }

  return activity.sort((a, b) => new Date(b.timestamp).getTime() - new Date(a.timestamp).getTime());
}

function buildConsumers(detail: ContractVersionDetail): StudioRelationship[] {
  return (detail.consumers ?? []).map((consumer) => ({
    id: consumer.id,
    name: consumer.name,
    type: consumer.kind || 'Consumer',
    registeredAt: consumer.lastObservedAt,
    confidenceScore: consumer.confidenceScore,
    environment: consumer.environment || undefined,
    sourceType: consumer.kind || undefined,
  }));
}

export function toStudioContract(detail: ContractVersionDetail): StudioContract {
  const policyChecks = buildPolicyChecks(detail);

  return {
    id: detail.id,
    apiAssetId: detail.apiAssetId,
    technicalName: detail.apiName || detail.serviceName || detail.apiAssetId,
    friendlyName: detail.serviceDisplayName || detail.apiName || detail.serviceName || detail.apiAssetId,
    functionalDescription: detail.serviceDescription || detail.routePattern || '',
    technicalDescription: detail.routePattern || '',
    semVer: detail.semVer,
    format: detail.format,
    protocol: detail.protocol,
    specContent: detail.specContent,
    lifecycleState: detail.lifecycleState,
    isLocked: detail.isLocked,
    lockedAt: detail.lockedAt,
    lockedBy: detail.lockedBy,
    signedBy: detail.signedBy,
    signedAt: detail.signedAt,
    fingerprint: detail.fingerprint,
    algorithm: detail.algorithm,
    serviceType: detail.serviceType || detail.protocol,
    domain: detail.domain || '',
    capability: detail.systemArea || '',
    product: '',
    owner: detail.technicalOwner || '',
    team: detail.teamName || '',
    visibility: detail.visibility || detail.exposureType || '',
    criticality: detail.criticality || '',
    dataClassification: '',
    tags: [],
    sla: '',
    slo: '',
    externalLinks: [detail.documentationUrl, detail.repositoryUrl].filter((value): value is string => !!value),
    approvalState: mapLifecycleToApprovalState(detail.lifecycleState),
    complianceScore: null,
    approvalChecklist: [],
    policyChecks,
    importedFrom: detail.importedFrom,
    provenance: undefined,
    deprecationNotice: detail.deprecationNotice,
    sunsetDate: detail.sunsetDate,
    createdAt: detail.createdAt,
    risks: [],
    recentActivity: buildRecentActivity(detail),
    consumers: buildConsumers(detail),
    producers: [],
    dependencies: [],
  };
}
