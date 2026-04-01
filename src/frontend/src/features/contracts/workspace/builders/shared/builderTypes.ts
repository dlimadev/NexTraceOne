/**
 * Tipos de modelo para todos os visual builders do módulo de contratos.
 * Define os estados completos de cada builder, validações e serialização.
 */

// ── Property Constraints ──────────────────────────────────────────────────────

/** Constraints de uma propriedade de schema/parâmetro (alinhado com OpenAPI 3.x). */
export interface PropertyConstraints {
  minLength?: number;
  maxLength?: number;
  minimum?: number;
  maximum?: number;
  exclusiveMinimum?: boolean;
  exclusiveMaximum?: boolean;
  pattern?: string;
  format?: string;
  defaultValue?: string;
  readOnly?: boolean;
  writeOnly?: boolean;
  nullable?: boolean;
  enumValues?: string[];
  example?: string;
}

// ── Schema Property ───────────────────────────────────────────────────────────

/** Propriedade de schema com suporte a tipos complexos (objectos, listas, referências). */
export interface SchemaProperty {
  id: string;
  name: string;
  type: 'string' | 'integer' | 'number' | 'boolean' | 'array' | 'object' | '$ref';
  description: string;
  required: boolean;
  constraints: PropertyConstraints;
  /** Referência a schema definido em #/components/schemas/ (quando type === '$ref'). */
  $ref?: string;
  /** Propriedades filhas (quando type === 'object'). */
  properties?: SchemaProperty[];
  /** Tipo dos itens do array (quando type === 'array'). */
  items?: SchemaProperty;
}

// ── REST API Builder ──────────────────────────────────────────────────────────

export interface RestParameter {
  id: string;
  name: string;
  in: 'query' | 'path' | 'header' | 'cookie';
  required: boolean;
  type: string;
  description: string;
  constraints: PropertyConstraints;
}

export interface RestRequestBody {
  contentType: string;
  schema: string;
  required: boolean;
  example: string;
  /** Propriedades do request body (modo visual). */
  properties?: SchemaProperty[];
}

export interface RestResponse {
  id: string;
  statusCode: string;
  description: string;
  contentType: string;
  schema: string;
  example: string;
  /** Propriedades da resposta (modo visual). */
  properties?: SchemaProperty[];
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

/** Role de messaging de um Background Service: None, Producer, Consumer, Both. */
export type MessagingRole = 'None' | 'Producer' | 'Consumer' | 'Both';

/** Tópico/fila consumido ou produzido por um Background Service. */
export interface MessagingTopic {
  id: string;
  topicName: string;
  entityType: string;
  format: 'avro' | 'json' | 'protobuf' | '';
}

/** Serviço consumido por um Background Service. */
export interface ConsumedService {
  id: string;
  serviceName: string;
  protocol: 'REST' | 'gRPC' | 'SOAP' | '';
}

/** Evento produzido por um Background Service. */
export interface ProducedEvent {
  id: string;
  eventName: string;
  targetTopic: string;
}

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
  /** Role de messaging: None, Producer, Consumer, Both. */
  messagingRole: MessagingRole;
  /** Tópicos/filas consumidos pelo processo. */
  consumedTopics: MessagingTopic[];
  /** Tópicos/filas produzidos pelo processo. */
  producedTopics: MessagingTopic[];
  /** Serviços consumidos pelo processo. */
  consumedServices: ConsumedService[];
  /** Eventos produzidos pelo processo. */
  producedEvents: ProducedEvent[];
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
