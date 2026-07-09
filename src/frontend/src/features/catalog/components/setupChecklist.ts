export type SetupItemId = 'ownership' | 'repository' | 'documentation' | 'interface' | 'contract';

export interface SetupItem {
  id: SetupItemId;
  done: boolean;
  applicable: boolean;
}

export interface SetupServiceInput {
  technicalOwner?: string | null;
  repositoryUrl?: string | null;
  gitRepository?: string | null;
  documentationUrl?: string | null;
  apis?: unknown[] | null;
  serviceType?: string | null;
}

const filled = (v?: string | null): boolean => !!v && v.trim().length > 0;

/** Deriva os itens de setup a partir de dados já carregados no detalhe (honest-null). */
export function deriveSetupItems(
  service: SetupServiceInput,
  contractCount: number,
  supportsContracts: (t: string) => boolean,
): SetupItem[] {
  const contractApplicable = service.serviceType ? supportsContracts(service.serviceType) : true;
  return [
    { id: 'ownership', done: filled(service.technicalOwner), applicable: true },
    { id: 'repository', done: filled(service.repositoryUrl) || filled(service.gitRepository), applicable: true },
    { id: 'documentation', done: filled(service.documentationUrl), applicable: true },
    { id: 'interface', done: (service.apis?.length ?? 0) > 0, applicable: true },
    { id: 'contract', done: contractCount > 0, applicable: contractApplicable },
  ];
}

/** Progresso considerando apenas itens aplicáveis (N/A não conta). */
export function setupProgress(items: SetupItem[]): { done: number; total: number; allDone: boolean } {
  const applicable = items.filter((i) => i.applicable);
  const done = applicable.filter((i) => i.done).length;
  return { done, total: applicable.length, allDone: applicable.length > 0 && done === applicable.length };
}
