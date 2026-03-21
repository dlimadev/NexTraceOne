/**
 * Tipos específicos do workspace de contratos.
 * Define as secções, ícones e configuração de navegação.
 */

/** Secção do workspace de contrato. */
export type WorkspaceSectionId =
  | 'summary'
  | 'definition'
  | 'contract'
  | 'operations'
  | 'schemas'
  | 'security'
  | 'versioning'
  | 'changelog'
  | 'approvals'
  | 'compliance'
  | 'validation'
  | 'consumers'
  | 'dependencies'
  | 'ai-agents';

/** Grupo funcional de secções no workspace. */
export type WorkspaceSectionGroup =
  | 'overview'
  | 'contract'
  | 'governance'
  | 'relationships';

/** Definição de uma secção do workspace. */
export interface WorkspaceSectionDef {
  id: WorkspaceSectionId;
  labelKey: string;
  icon: string;
  group: WorkspaceSectionGroup;
}
