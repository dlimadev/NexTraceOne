// ── Pages ─────────────────────────────────────────────────────────────────────
export { ContractCatalogPage } from './catalog/ContractCatalogPage';
export { CreateContractPage } from './create/CreateContractPage';
/** @deprecated Use CreateContractPage instead. */
export { CreateContractPage as CreateServicePage } from './create/CreateContractPage';
export { ContractWorkspacePage } from './workspace/ContractWorkspacePage';
export { ContractGovernancePage } from './governance/ContractGovernancePage';
export { ContractPortalPage } from './portal/ContractPortalPage';
export { SpectralRulesetManagerPage } from './spectral/SpectralRulesetManagerPage';
export { CanonicalEntityCatalogPage } from './canonical/CanonicalEntityCatalogPage';
export { DraftStudioPage } from './studio/DraftStudioPage';

// ── API ───────────────────────────────────────────────────────────────────────
export { contractsApi } from './api/contracts';
export { contractStudioApi } from './api/contractStudio';

// ── Hooks ─────────────────────────────────────────────────────────────────────
export {
  useContractDetail,
  useContractList,
  useContractsSummary,
  useContractViolations,
  useContractTransition,
  useContractExport,
  useContractHistory,
  useContractDiff,
  useCreateDraft,
  useSubmitForReview,
  usePublishDraft,
  contractQueryKeys,
} from './hooks/index';

// ── Types ─────────────────────────────────────────────────────────────────────
export type { WorkspaceSectionId, ContractKind, ServiceKind, AuthoringMode, SpecFormat } from './types/index';

// ── Publication Center ─────────────────────────────────────────────────────────
export { PublicationCenterPage } from './publication/PublicationCenterPage';
export { publicationCenterApi } from './api/publicationCenter';
export {
  usePublishContractToPortal,
  useWithdrawContractFromPortal,
  usePublicationCenterEntries,
  useContractPublicationStatus,
  publicationCenterKeys,
} from './hooks/usePublicationCenter';
