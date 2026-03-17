/**
 * Tipos de modelo para todos os visual builders do módulo de contratos.
 * Define os estados completos de cada builder, validações e serialização.
 */

// ── REST API Builder ──────────────────────────────────────────────────────────

export interface RestParameter {
  id: string;
  name: string;
  in: 'query' | 'path' | 'header' | 'cookie';
  required: boolean;
  type: string;
  description: string;
}

export interface RestRequestBody {
  contentType: string;
  schema: string;
  required: boolean;
  example: string;
}

export interface RestResponse {
  id: string;
  statusCode: string;
  description: string;
  contentType: string;
  schema: string;
  example: string;
}

export interface RestEndpoint {
  id: string;
  method: 'GET' | 'POST' | 'PUT' | 'PATCH' | 'DELETE' | 'HEAD' | 'OPTIONS';
  path: string;
  operationId: string;
  summary: string;
  description: string;
  tags: string[];
  deprecated: boolean;
  deprecationNote: string;
  parameters: RestParameter[];
  requestBody: RestRequestBody | null;
  responses: RestResponse[];
  authScopes: string[];
  rateLimit: string;
  idempotencyKey: string;
  observabilityNotes: string;
}

export interface RestBuilderState {
  basePath: string;
  title: string;
  version: string;
  description: string;
  contact: string;
  license: string;
  servers: string[];
  endpoints: RestEndpoint[];
}

// ── SOAP Builder ──────────────────────────────────────────────────────────────

export interface SoapMessage {
  id: string;
  name: string;
  parts: string;
}

export interface SoapOperation {
  id: string;
  name: string;
  soapAction: string;
  inputMessage: string;
  outputMessage: string;
  faultMessage: string;
  description: string;
}

export interface SoapBuilderState {
  serviceName: string;
  targetNamespace: string;
  endpoint: string;
  binding: 'SOAP 1.1' | 'SOAP 1.2';
  description: string;
  securityPolicy: string;
  namespaces: string[];
  operations: SoapOperation[];
}

// ── Event API Builder ─────────────────────────────────────────────────────────

export type CompatibilityMode = 'BACKWARD' | 'FORWARD' | 'FULL' | 'NONE';

export interface EventChannel {
  id: string;
  topicName: string;
  eventName: string;
  version: string;
  keySchema: string;
  payloadSchema: string;
  headers: string;
  producer: string;
  consumer: string;
  compatibility: CompatibilityMode;
  retention: string;
  partitions: string;
  ordering: string;
  retries: string;
  dlq: string;
  idempotent: boolean;
  description: string;
  owner: string;
  observabilityNotes: string;
}

export interface EventBuilderState {
  title: string;
  version: string;
  description: string;
  defaultBroker: string;
  channels: EventChannel[];
}

// ── Workservice Builder ───────────────────────────────────────────────────────

export type TriggerType = 'Cron' | 'Queue' | 'Event' | 'Manual' | 'Webhook';

export interface WorkserviceDependency {
  id: string;
  name: string;
  type: 'Service' | 'Database' | 'Queue' | 'ExternalApi' | 'Cache' | 'Storage';
  required: boolean;
}

export interface WorkserviceBuilderState {
  name: string;
  trigger: TriggerType;
  schedule: string;
  description: string;
  inputs: string;
  outputs: string;
  dependencies: WorkserviceDependency[];
  retries: string;
  timeout: string;
  errorHandling: string;
  sideEffects: string;
  owner: string;
  observabilityNotes: string;
  healthCheck: string;
}

// ── Validation ────────────────────────────────────────────────────────────────

export interface BuilderValidationError {
  field: string;
  messageKey: string;
  fallback: string;
}

export interface BuilderValidationResult {
  valid: boolean;
  errors: BuilderValidationError[];
}

// ── Sync ──────────────────────────────────────────────────────────────────────

export type SyncDirection = 'visual-to-source' | 'source-to-visual';

export interface SyncResult {
  success: boolean;
  content: string;
  warnings: string[];
  unsupportedFeatures: string[];
}
