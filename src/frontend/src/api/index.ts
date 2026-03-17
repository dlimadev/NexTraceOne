/**
 * Barrel export centralizado — re-exporta APIs de cada bounded context
 * para manter compatibilidade com importações existentes.
 */
export { identityApi } from '../features/identity-access/api/identity';
export { serviceCatalogApi } from '../features/catalog/api/serviceCatalog';
export { contractsApi } from '../features/catalog/api/contracts';
export { changeIntelligenceApi } from '../features/change-governance/api/changeIntelligence';
export { workflowApi } from '../features/change-governance/api/workflow';
export { auditApi } from '../features/audit-compliance/api/audit';
export { promotionApi } from '../features/change-governance/api/promotion';
export { developerPortalApi } from '../features/catalog/api/developerPortal';
export { globalSearchApi } from '../features/catalog/api/globalSearch';
export { organizationGovernanceApi } from '../features/governance/api/organizationGovernance';
export { incidentsApi } from '../features/operations/api/incidents';
export { default as apiClient } from './client';
