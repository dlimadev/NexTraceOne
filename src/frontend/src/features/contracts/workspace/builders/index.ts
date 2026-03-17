export { VisualRestBuilder } from './VisualRestBuilder';
export { VisualSoapBuilder } from './VisualSoapBuilder';
export { VisualEventBuilder } from './VisualEventBuilder';
export { VisualWorkserviceBuilder } from './VisualWorkserviceBuilder';

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
  BuilderValidationResult,
  SyncResult,
} from './shared/builderTypes';

export {
  restBuilderToYaml,
  soapBuilderToXml,
  eventBuilderToYaml,
  workserviceBuilderToYaml,
} from './shared/builderSync';

export {
  validateRestBuilder,
  validateSoapBuilder,
  validateEventBuilder,
  validateWorkserviceBuilder,
} from './shared/builderValidation';
