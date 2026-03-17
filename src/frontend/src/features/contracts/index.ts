// ── Pages ─────────────────────────────────────────────────────────────────────
export { ContractCatalogPage } from './catalog/ContractCatalogPage';
export { CreateServicePage } from './create/CreateServicePage';
export { ContractWorkspacePage } from './workspace/ContractWorkspacePage';
export { ContractPortalPage } from './portal/ContractPortalPage';
export { ContractGovernancePage } from './governance/ContractGovernancePage';

// ── API ───────────────────────────────────────────────────────────────────────
export { contractsApi, contractStudioApi } from './api';

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
  useGenerateFromAi,
  useSubmitForReview,
  usePublishDraft,
  contractQueryKeys,
} from './hooks';

// ── Types ─────────────────────────────────────────────────────────────────────
export type { WorkspaceSectionId, ServiceKind, AuthoringMode, SpecFormat } from './types';
