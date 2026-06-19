import type React from 'react';
import { Globe, Server, Zap, Cog, Database, FileCode, MessageSquare, AlignJustify, Terminal, Webhook, Columns, Upload, Sparkles } from 'lucide-react';
import type { ContractTypeValue } from '../shared/constants';

/** Mapeia as chaves de card do hub (ContractStudioPage) para o enum ContractType do wizard. */
export const HUB_KEY_TO_CONTRACT_TYPE: Record<string, ContractTypeValue> = {
  'rest-openapi': 'RestApi',
  'asyncapi': 'Event',
  'soap-wsdl': 'Soap',
  'graphql': 'RestApi',
  'protobuf': 'RestApi',
  'shared-schema': 'SharedSchema',
};

/** Chave i18n da linha "Best for" por tipo de contrato. */
export const BEST_FOR_KEY = (type: ContractTypeValue | string): string => `contracts.create.bestFor.${type}`;

/** Ícone por ContractType (para galeria do TypeModeTab e cartão de identidade). */
export const TYPE_ICONS: Record<string, React.ComponentType<{ size?: number; className?: string }>> = {
  RestApi: Globe,
  Soap: Server,
  Event: Zap,
  BackgroundService: Cog,
  SharedSchema: Database,
  Copybook: FileCode,
  MqMessage: MessageSquare,
  FixedLayout: AlignJustify,
  CicsCommarea: Terminal,
  Webhook: Webhook,
};

export type CreationMode = 'visual' | 'import' | 'ai';

export const CREATION_MODES: { id: CreationMode; labelKey: string; descriptionKey: string; Icon: React.ComponentType<{ size?: number; className?: string }> }[] = [
  { id: 'visual', labelKey: 'contracts.create.modeVisual', descriptionKey: 'contracts.create.modeVisualDesc', Icon: Columns },
  { id: 'import', labelKey: 'contracts.create.modeImport', descriptionKey: 'contracts.create.modeImportDesc', Icon: Upload },
  { id: 'ai', labelKey: 'contracts.create.modeAi', descriptionKey: 'contracts.create.modeAiDesc', Icon: Sparkles },
];

export type FormTab = 'service' | 'typeMode' | 'details' | 'confirm';

export const FORM_TABS: FormTab[] = ['service', 'typeMode', 'details', 'confirm'];

export const FORM_TAB_LABEL_KEY: Record<FormTab, string> = {
  service: 'contracts.create.tabService',
  typeMode: 'contracts.create.tabTypeMode',
  details: 'contracts.create.tabDetails',
  confirm: 'contracts.create.tabConfirm',
};
