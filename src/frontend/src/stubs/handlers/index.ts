/**
 * Agregação dos handlers MSW do modo stub.
 *
 * A ordem importa: o catch-all tem de ser SEMPRE o último, para só apanhar
 * o que os handlers específicos não trataram.
 */
import { authHandlers } from './auth';
import { catalogHandlers } from './catalog';
import { contractsHandlers } from './contracts';
import { sourceOfTruthHandlers } from './sourceOfTruth';
import { runtimeHandlers } from './runtime';
import { catchAllHandlers } from './catchAll';

export const handlers = [
  ...authHandlers,
  ...catalogHandlers,
  ...contractsHandlers,
  ...sourceOfTruthHandlers,
  ...runtimeHandlers,
  ...catchAllHandlers,
];
