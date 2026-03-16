import client from '../../../api/client';
import type {
  TeamSummary,
  TeamDetail,
  DomainSummary,
  DomainDetail,
  GovernanceSummary,
  ScopedContext,
  DelegatedAdminDto,
  CrossTeamDependencies,
  CrossDomainDependencies,
  CreateTeamRequest,
  CreateDomainRequest,
  CreateDelegationRequest,
} from '../../../types';

/** Cliente de API para governança multi-equipa e multi-domínio. */
export const organizationGovernanceApi = {
  // Teams
  listTeams: () =>
    client.get<{ teams: TeamSummary[] }>('/teams').then((r) => r.data),
  getTeamDetail: (teamId: string) =>
    client.get<TeamDetail>(`/teams/${teamId}`).then((r) => r.data),
  createTeam: (data: CreateTeamRequest) =>
    client.post<{ teamId: string }>('/teams', data).then((r) => r.data),
  updateTeam: (teamId: string, data: Partial<CreateTeamRequest>) =>
    client.patch(`/teams/${teamId}`, data).then((r) => r.data),
  getTeamGovernanceSummary: (teamId: string) =>
    client.get<GovernanceSummary>(`/teams/${teamId}/governance-summary`).then((r) => r.data),
  getCrossTeamDependencies: (teamId: string) =>
    client.get<CrossTeamDependencies>(`/teams/${teamId}/dependencies/cross-team`).then((r) => r.data),

  // Domains
  listDomains: () =>
    client.get<{ domains: DomainSummary[] }>('/domains').then((r) => r.data),
  getDomainDetail: (domainId: string) =>
    client.get<DomainDetail>(`/domains/${domainId}`).then((r) => r.data),
  createDomain: (data: CreateDomainRequest) =>
    client.post<{ domainId: string }>('/domains', data).then((r) => r.data),
  updateDomain: (domainId: string, data: Partial<CreateDomainRequest>) =>
    client.patch(`/domains/${domainId}`, data).then((r) => r.data),
  getDomainGovernanceSummary: (domainId: string) =>
    client.get<GovernanceSummary>(`/domains/${domainId}/governance-summary`).then((r) => r.data),
  getCrossDomainDependencies: (domainId: string) =>
    client.get<CrossDomainDependencies>(`/domains/${domainId}/dependencies/cross-domain`).then((r) => r.data),

  // Scoped Context
  getScopedContext: () =>
    client.get<ScopedContext>('/me/context').then((r) => r.data),

  // Delegated Administration
  listDelegations: () =>
    client.get<{ delegations: DelegatedAdminDto[] }>('/admin/delegations').then((r) => r.data),
  createDelegation: (data: CreateDelegationRequest) =>
    client.post<{ delegationId: string }>('/admin/delegations', data).then((r) => r.data),
};
