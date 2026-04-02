export { VisualRestBuilder } from './VisualRestBuilder';
export { VisualSoapBuilder } from './VisualSoapBuilder';
export { VisualEventBuilder } from './VisualEventBuilder';
export { VisualWorkserviceBuilder } from './VisualWorkserviceBuilder';
export { VisualSharedSchemaBuilder } from './VisualSharedSchemaBuilder';
export { VisualWebhookBuilder } from './VisualWebhookBuilder';
export { VisualLegacyContractBuilder } from './VisualLegacyContractBuilder';

// Shared builder infrastructure
export type {
  RestBuilderState,
  RestEndpoint,
  SoapBuilderState,
  SoapOperation,
  EventBuilderState,
  EventChannel,
  WorkserviceBuilderState,
  WorkserviceDependency,
  SharedSchemaBuilderState,
  SharedSchemaProperty,
  WebhookBuilderState,
  WebhookHeader,
  LegacyContractBuilderState,
  LegacyContractKind,
  LegacyField,
  BuilderValidationResult,
  SyncResult,
} from './shared/builderTypes';

export {
  restBuilderToYaml,
  soapBuilderToXml,
  eventBuilderToYaml,
  workserviceBuilderToYaml,
  sharedSchemaBuilderToJson,
  webhookBuilderToYaml,
  legacyContractBuilderToYaml,
} from './shared/builderSync';

export {
  validateRestBuilder,
  validateSoapBuilder,
  validateEventBuilder,
  validateWorkserviceBuilder,
  validateSharedSchemaBuilder,
  validateWebhookBuilder,
  validateLegacyContractBuilder,
} from './shared/builderValidation';
