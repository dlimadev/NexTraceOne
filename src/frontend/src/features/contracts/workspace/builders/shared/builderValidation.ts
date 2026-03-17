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

export function validateRestBuilder(state: RestBuilderState): BuilderValidationResult {
  const errors: BuilderValidationError[] = [];

  if (!state.title.trim()) {
    errors.push({ field: 'title', messageKey: 'contracts.builder.validation.titleRequired', fallback: 'API title is required' });
  }
  if (!state.basePath.trim()) {
    errors.push({ field: 'basePath', messageKey: 'contracts.builder.validation.basePathRequired', fallback: 'Base path is required' });
  }

  for (const ep of state.endpoints) {
    if (!ep.path.trim()) {
      errors.push({ field: `endpoint.${ep.id}.path`, messageKey: 'contracts.builder.validation.pathRequired', fallback: `Path is required for endpoint ${ep.operationId || ep.id}` });
    }
    if (ep.responses.length === 0) {
      errors.push({ field: `endpoint.${ep.id}.responses`, messageKey: 'contracts.builder.validation.responseRequired', fallback: `At least one response is required for ${ep.method} ${ep.path}` });
    }
    for (const param of ep.parameters) {
      if (!param.name.trim()) {
        errors.push({ field: `endpoint.${ep.id}.param.${param.id}`, messageKey: 'contracts.builder.validation.paramNameRequired', fallback: 'Parameter name is required' });
      }
    }
  }

  return { valid: errors.length === 0, errors };
}

export function validateSoapBuilder(state: SoapBuilderState): BuilderValidationResult {
  const errors: BuilderValidationError[] = [];

  if (!state.serviceName.trim()) {
    errors.push({ field: 'serviceName', messageKey: 'contracts.builder.validation.serviceNameRequired', fallback: 'Service name is required' });
  }
  if (!state.targetNamespace.trim()) {
    errors.push({ field: 'targetNamespace', messageKey: 'contracts.builder.validation.namespaceRequired', fallback: 'Target namespace is required' });
  }

  for (const op of state.operations) {
    if (!op.name.trim()) {
      errors.push({ field: `operation.${op.id}.name`, messageKey: 'contracts.builder.validation.operationNameRequired', fallback: 'Operation name is required' });
    }
  }

  return { valid: errors.length === 0, errors };
}

export function validateEventBuilder(state: EventBuilderState): BuilderValidationResult {
  const errors: BuilderValidationError[] = [];

  if (!state.title.trim()) {
    errors.push({ field: 'title', messageKey: 'contracts.builder.validation.titleRequired', fallback: 'Event API title is required' });
  }

  for (const ch of state.channels) {
    if (!ch.topicName.trim()) {
      errors.push({ field: `channel.${ch.id}.topicName`, messageKey: 'contracts.builder.validation.topicRequired', fallback: 'Topic name is required' });
    }
    if (!ch.producer.trim() && !ch.consumer.trim()) {
      errors.push({ field: `channel.${ch.id}.actors`, messageKey: 'contracts.builder.validation.actorRequired', fallback: 'At least one producer or consumer is required' });
    }
  }

  return { valid: errors.length === 0, errors };
}

export function validateWorkserviceBuilder(state: WorkserviceBuilderState): BuilderValidationResult {
  const errors: BuilderValidationError[] = [];

  if (!state.name.trim()) {
    errors.push({ field: 'name', messageKey: 'contracts.builder.validation.nameRequired', fallback: 'Service name is required' });
  }
  if (state.trigger === 'Cron' && !state.schedule.trim()) {
    errors.push({ field: 'schedule', messageKey: 'contracts.builder.validation.scheduleRequired', fallback: 'Schedule is required for Cron trigger' });
  }

  return { valid: errors.length === 0, errors };
}
