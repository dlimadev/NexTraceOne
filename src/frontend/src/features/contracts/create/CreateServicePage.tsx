/**
 * @deprecated Use CreateContractPage instead.
 * This re-export keeps legacy imports stable while the Contract Governance
 * flow was consolidated into a single entry-point route (`/contracts/create`).
 *
 * Rationale:
 * - avoid duplicate creation flows for service contracts
 * - preserve compatibility for older internal links/imports
 * - keep migration non-breaking while callers move to CreateContractPage
 */
export { CreateContractPage as CreateServicePage } from './CreateContractPage';
