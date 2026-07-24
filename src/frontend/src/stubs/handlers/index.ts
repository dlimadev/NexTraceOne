/**
 * Agregação dos handlers MSW do modo stub.
 *
 * A ordem importa: o catch-all tem de ser SEMPRE o último, para só apanhar
 * o que os handlers específicos não trataram.
 */
import { authHandlers } from './auth';
import { catalogHandlers } from './catalog';
import { serviceTabsHandlers } from './serviceTabs';
import { contractsHandlers } from './contracts';
import { catalogContractsExtrasHandlers } from './catalogContractsExtras';
import { sourceOfTruthHandlers } from './sourceOfTruth';
import { runtimeHandlers } from './runtime';
import { changeGovernanceHandlers } from './changeGovernance';
import { catchAllHandlers } from './catchAll';

export const handlers = [
  ...authHandlers,
  ...catalogHandlers,
  ...serviceTabsHandlers,
  ...contractsHandlers,
  ...catalogContractsExtrasHandlers,
  ...sourceOfTruthHandlers,
  ...runtimeHandlers,
  ...changeGovernanceHandlers,
  ...catchAllHandlers,
];
