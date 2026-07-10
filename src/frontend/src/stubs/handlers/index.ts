/**
 * Agregação dos handlers MSW do modo stub.
 *
 * A ordem importa: o catch-all tem de ser SEMPRE o último, para só apanhar
 * o que os handlers específicos não trataram.
 */
import { authHandlers } from './auth';
import { catalogHandlers } from './catalog';
import { catchAllHandlers } from './catchAll';

export const handlers = [
  ...authHandlers,
  ...catalogHandlers,
  ...catchAllHandlers,
];
