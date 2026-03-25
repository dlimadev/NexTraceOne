/**
 * Tipos do módulo de Configuration — contratos alinhados com o backend.
 */

export interface ConfigurationDefinitionDto {
  key: string;
  displayName: string;
  description: string | null;
  category: string;
  allowedScopes: string[];
  defaultValue: string | null;
  valueType: string;
  isSensitive: boolean;
  isEditable: boolean;
  isInheritable: boolean;
  validationRules: string | null;
  uiEditorType: string | null;
  sortOrder: number;
  isDeprecated: boolean;
  deprecatedMessage: string | null;
}

export interface ConfigurationEntryDto {
  id: string;
  definitionKey: string;
  scope: string;
  scopeReferenceId: string | null;
  value: string | null;
  isActive: boolean;
  version: number;
  changeReason: string | null;
  updatedAt: string;
  updatedBy: string;
}

export interface EffectiveConfigurationDto {
  key: string;
  effectiveValue: string | null;
  resolvedScope: string;
  resolvedScopeReferenceId: string | null;
  isInherited: boolean;
  isDefault: boolean;
  definitionKey: string;
  valueType: string;
  isSensitive: boolean;
  version: number;
}

export interface ConfigurationAuditEntryDto {
  key: string;
  scope: string;
  scopeReferenceId: string | null;
  action: string;
  previousValue: string | null;
  newValue: string | null;
  previousVersion: number | null;
  newVersion: number;
  changedBy: string;
  changedAt: string;
  changeReason: string | null;
  isSensitive: boolean;
}

export interface SetConfigurationValueRequest {
  scope: string;
  scopeReferenceId?: string | null;
  value: string;
  changeReason?: string;
}

export interface ToggleConfigurationRequest {
  scope: string;
  scopeReferenceId?: string | null;
  activate: boolean;
  changeReason?: string;
}

export type ConfigurationScope =
  | 'System'
  | 'Tenant'
  | 'Environment'
  | 'Role'
  | 'Team'
  | 'User';

export type ConfigurationCategory =
  | 'Bootstrap'
  | 'SensitiveOperational'
  | 'Functional';

export type ConfigurationView =
  | 'definitions'
  | 'entries'
  | 'effective';
