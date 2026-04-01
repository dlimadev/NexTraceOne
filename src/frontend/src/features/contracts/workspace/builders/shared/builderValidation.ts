/**
 * Validações dos visual builders.
 * Cada builder tem a sua função de validação que retorna erros tipados.
 */
import type {
  RestBuilderState,
  SoapBuilderState,
  EventBuilderState,
  WorkserviceBuilderState,
  BuilderValidationResult,
  BuilderValidationError,
} from './builderTypes';

// ── Helpers ───────────────────────────────────────────────────────────────────

/** Extrai nomes de path parameters de um path template (e.g., /users/{id} → ['id']). */
function extractPathParams(path: string): string[] {
  const matches = path.match(/\{([^}]+)\}/g);
  return matches ? matches.map((m) => m.slice(1, -1)) : [];
}

/** Verifica se um path segue a convenção de URI válida. */
function isValidPathSyntax(path: string): boolean {
  if (!path.startsWith('/')) return false;
  // Validação de balanced braces
  let depth = 0;
  for (const ch of path) {
    if (ch === '{') depth++;
    if (ch === '}') depth--;
    if (depth < 0) return false;
  }
  return depth === 0;
}

// ── REST Validation ───────────────────────────────────────────────────────────

export function validateRestBuilder(state: RestBuilderState): BuilderValidationResult {
  const errors: BuilderValidationError[] = [];

  if (!state.title.trim()) {
    errors.push({ field: 'title', messageKey: 'contracts.builder.validation.titleRequired', fallback: 'API title is required' });
  }
  if (!state.basePath.trim()) {
    errors.push({ field: 'basePath', messageKey: 'contracts.builder.validation.basePathRequired', fallback: 'Base path is required' });
  }
  if (state.basePath && !state.basePath.startsWith('/')) {
    errors.push({ field: 'basePath', messageKey: 'contracts.builder.validation.basePathSlash', fallback: 'Base path must start with /' });
  }

  // Validar servers como URLs válidas
  for (let i = 0; i < state.servers.length; i++) {
    const server = state.servers[i];
    if (server && !/^https?:\/\/.+/i.test(server)) {
      errors.push({ field: `servers.${i}`, messageKey: 'contracts.builder.validation.serverUrlInvalid', fallback: `Server URL '${server}' must start with http:// or https://` });
    }
  }

  // Track operationIds para detectar duplicados
  const operationIds = new Set<string>();

  for (const ep of state.endpoints) {
    const epLabel = ep.operationId || ep.id;

    // Path obrigatório
    if (!ep.path.trim()) {
      errors.push({ field: `endpoint.${ep.id}.path`, messageKey: 'contracts.builder.validation.pathRequired', fallback: `Path is required for endpoint ${epLabel}` });
    }

    // Validar sintaxe do path
    if (ep.path && !isValidPathSyntax(ep.path)) {
      errors.push({ field: `endpoint.${ep.id}.path`, messageKey: 'contracts.builder.validation.pathSyntaxInvalid', fallback: `Path '${ep.path}' has invalid syntax (unmatched braces)` });
    }

    // Pelo menos uma response
    if (ep.responses.length === 0) {
      errors.push({ field: `endpoint.${ep.id}.responses`, messageKey: 'contracts.builder.validation.responseRequired', fallback: `At least one response is required for ${ep.method} ${ep.path}` });
    }

    // Detectar status codes duplicados por endpoint
    const statusCodes = new Set<string>();
    for (const resp of ep.responses) {
      if (statusCodes.has(resp.statusCode)) {
        errors.push({ field: `endpoint.${ep.id}.response.${resp.id}`, messageKey: 'contracts.builder.validation.duplicateStatusCode', fallback: `Duplicate status code ${resp.statusCode} in ${ep.method} ${ep.path}` });
      }
      statusCodes.add(resp.statusCode);
    }

    // Validar operationId único
    if (ep.operationId.trim()) {
      if (operationIds.has(ep.operationId)) {
        errors.push({ field: `endpoint.${ep.id}.operationId`, messageKey: 'contracts.builder.validation.duplicateOperationId', fallback: `Duplicate operationId '${ep.operationId}'` });
      }
      operationIds.add(ep.operationId);
    }

    // Validar correlação path ↔ parâmetros
    if (ep.path) {
      const pathParamNames = extractPathParams(ep.path);
      const declaredPathParams = ep.parameters
        .filter((p) => p.in === 'path')
        .map((p) => p.name);

      // Parâmetros no path mas não declarados
      for (const pn of pathParamNames) {
        if (!declaredPathParams.includes(pn)) {
          errors.push({
            field: `endpoint.${ep.id}.param.missing.${pn}`,
            messageKey: 'contracts.builder.validation.pathParamNotDeclared',
            fallback: `Path parameter '{${pn}}' is used in path but not declared as a parameter`,
          });
        }
      }

      // Path parameters declarados devem ser required
      for (const param of ep.parameters) {
        if (param.in === 'path' && !param.required) {
          errors.push({
            field: `endpoint.${ep.id}.param.${param.id}`,
            messageKey: 'contracts.builder.validation.pathParamMustBeRequired',
            fallback: `Path parameter '${param.name}' must be required`,
          });
        }
      }
    }

    // Validar nomes de parâmetros
    for (const param of ep.parameters) {
      if (!param.name.trim()) {
        errors.push({ field: `endpoint.${ep.id}.param.${param.id}`, messageKey: 'contracts.builder.validation.paramNameRequired', fallback: 'Parameter name is required' });
      }
    }

    // Validar deprecated requer nota
    if (ep.deprecated && !ep.deprecationNote.trim()) {
      errors.push({
        field: `endpoint.${ep.id}.deprecationNote`,
        messageKey: 'contracts.builder.validation.deprecationNoteRequired',
        fallback: `Deprecation note is required when endpoint is marked as deprecated`,
      });
    }

    // Validar constraints de parâmetros
    for (const param of ep.parameters) {
      if (param.constraints) {
        if (param.constraints.minLength !== undefined && param.constraints.maxLength !== undefined) {
          if (param.constraints.minLength > param.constraints.maxLength) {
            errors.push({
              field: `endpoint.${ep.id}.param.${param.id}.constraints`,
              messageKey: 'contracts.builder.validation.minGtMax',
              fallback: `Parameter '${param.name}': minLength cannot be greater than maxLength`,
            });
          }
        }
        if (param.constraints.minimum !== undefined && param.constraints.maximum !== undefined) {
          if (param.constraints.minimum > param.constraints.maximum) {
            errors.push({
              field: `endpoint.${ep.id}.param.${param.id}.constraints`,
              messageKey: 'contracts.builder.validation.minValueGtMax',
              fallback: `Parameter '${param.name}': minimum cannot be greater than maximum`,
            });
          }
        }
      }
    }
  }

  return { valid: errors.length === 0, errors };
}

// ── SOAP Validation ───────────────────────────────────────────────────────────

export function validateSoapBuilder(state: SoapBuilderState): BuilderValidationResult {
  const errors: BuilderValidationError[] = [];

  if (!state.serviceName.trim()) {
    errors.push({ field: 'serviceName', messageKey: 'contracts.builder.validation.serviceNameRequired', fallback: 'Service name is required' });
  }
  if (!state.targetNamespace.trim()) {
    errors.push({ field: 'targetNamespace', messageKey: 'contracts.builder.validation.namespaceRequired', fallback: 'Target namespace is required' });
  }

  // Validar namespace como URI
  if (state.targetNamespace.trim() && !/^https?:\/\/.+|^urn:.+/i.test(state.targetNamespace)) {
    errors.push({ field: 'targetNamespace', messageKey: 'contracts.builder.validation.namespaceMustBeUri', fallback: 'Target namespace must be a valid URI (http://, https://, or urn:)' });
  }

  // Validar endpoint como URL
  if (state.endpoint.trim() && !/^https?:\/\/.+/i.test(state.endpoint)) {
    errors.push({ field: 'endpoint', messageKey: 'contracts.builder.validation.endpointMustBeUrl', fallback: 'Endpoint must be a valid URL (http:// or https://)' });
  }

  const operationNames = new Set<string>();
  for (const op of state.operations) {
    if (!op.name.trim()) {
      errors.push({ field: `operation.${op.id}.name`, messageKey: 'contracts.builder.validation.operationNameRequired', fallback: 'Operation name is required' });
    }
    // Detectar nomes de operação duplicados
    if (op.name.trim()) {
      if (operationNames.has(op.name)) {
        errors.push({ field: `operation.${op.id}.name`, messageKey: 'contracts.builder.validation.duplicateOperationName', fallback: `Duplicate operation name '${op.name}'` });
      }
      operationNames.add(op.name);
    }
  }

  return { valid: errors.length === 0, errors };
}

// ── Event Validation ──────────────────────────────────────────────────────────

export function validateEventBuilder(state: EventBuilderState): BuilderValidationResult {
  const errors: BuilderValidationError[] = [];

  if (!state.title.trim()) {
    errors.push({ field: 'title', messageKey: 'contracts.builder.validation.titleRequired', fallback: 'Event API title is required' });
  }

  const topicNames = new Set<string>();
  for (const ch of state.channels) {
    if (!ch.topicName.trim()) {
      errors.push({ field: `channel.${ch.id}.topicName`, messageKey: 'contracts.builder.validation.topicRequired', fallback: 'Topic name is required' });
    }
    if (!ch.producer.trim() && !ch.consumer.trim()) {
      errors.push({ field: `channel.${ch.id}.actors`, messageKey: 'contracts.builder.validation.actorRequired', fallback: 'At least one producer or consumer is required' });
    }

    // Validar nome de tópico Kafka (alfanumérico + '.', '-', '_'; max 249 chars; cannot be '.' or '..')
    if (ch.topicName.trim()) {
      if (!/^[a-zA-Z0-9._-]+$/.test(ch.topicName)) {
        errors.push({ field: `channel.${ch.id}.topicName`, messageKey: 'contracts.builder.validation.topicNameInvalid', fallback: `Topic name '${ch.topicName}' must contain only alphanumeric characters, dots, hyphens, and underscores` });
      }
      if (ch.topicName === '.' || ch.topicName === '..') {
        errors.push({ field: `channel.${ch.id}.topicName`, messageKey: 'contracts.builder.validation.topicNameReserved', fallback: `Topic name cannot be '.' or '..'` });
      }
      if (ch.topicName.length > 249) {
        errors.push({ field: `channel.${ch.id}.topicName`, messageKey: 'contracts.builder.validation.topicNameTooLong', fallback: 'Topic name cannot exceed 249 characters' });
      }
    }

    // Detectar tópicos duplicados
    if (ch.topicName.trim()) {
      if (topicNames.has(ch.topicName)) {
        errors.push({ field: `channel.${ch.id}.topicName`, messageKey: 'contracts.builder.validation.duplicateTopic', fallback: `Duplicate topic name '${ch.topicName}'` });
      }
      topicNames.add(ch.topicName);
    }

    // Validar partitions como número
    if (ch.partitions.trim() && !/^\d+$/.test(ch.partitions)) {
      errors.push({ field: `channel.${ch.id}.partitions`, messageKey: 'contracts.builder.validation.partitionsMustBeNumber', fallback: 'Partitions must be a positive number' });
    }
  }

  return { valid: errors.length === 0, errors };
}

// ── Workservice Validation ────────────────────────────────────────────────────

export function validateWorkserviceBuilder(state: WorkserviceBuilderState): BuilderValidationResult {
  const errors: BuilderValidationError[] = [];

  if (!state.name.trim()) {
    errors.push({ field: 'name', messageKey: 'contracts.builder.validation.nameRequired', fallback: 'Service name is required' });
  }
  if (state.trigger === 'Cron' && !state.schedule.trim()) {
    errors.push({ field: 'schedule', messageKey: 'contracts.builder.validation.scheduleRequired', fallback: 'Schedule is required for Cron trigger' });
  }

  // Validar cron expression (5 campos Unix, 6 campos Quartz com seconds, 7 campos Quartz com seconds+year)
  if (state.trigger === 'Cron' && state.schedule.trim()) {
    const fields = state.schedule.trim().split(/\s+/);
    if (fields.length < 5 || fields.length > 7) {
      errors.push({ field: 'schedule', messageKey: 'contracts.builder.validation.cronInvalid', fallback: 'Cron expression must have 5 fields (Unix) or 6-7 fields (Quartz)' });
    }
  }

  // Validar timeout como duration
  if (state.timeout.trim() && !/^\d+[smhd]?$/i.test(state.timeout)) {
    errors.push({ field: 'timeout', messageKey: 'contracts.builder.validation.timeoutInvalid', fallback: 'Timeout must be a duration (e.g., "30s", "5m", "1h")' });
  }

  // Validar retries como número
  if (state.retries.trim() && !/^\d+$/.test(state.retries)) {
    errors.push({ field: 'retries', messageKey: 'contracts.builder.validation.retriesMustBeNumber', fallback: 'Retries must be a positive number' });
  }

  return { valid: errors.length === 0, errors };
}
