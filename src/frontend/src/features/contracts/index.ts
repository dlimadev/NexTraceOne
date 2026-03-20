// ── Pages ─────────────────────────────────────────────────────────────────────
export { ContractCatalogPage } from './catalog/ContractCatalogPage';
export { CreateServicePage } from './create/CreateServicePage';
export { ContractWorkspacePage } from './workspace/ContractWorkspacePage';

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
export type { WorkspaceSectionId, ServiceKind, AuthoringMode, SpecFormat } from './types/index';
