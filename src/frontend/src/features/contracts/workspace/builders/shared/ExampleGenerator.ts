/**
 * Gera exemplos JSON realistas a partir de propriedades de schema.
 * Usa constraints (format, enum, min/max) para gerar valores contextualmente adequados.
 */
import type { SchemaProperty } from './builderTypes';

/**
 * Gera um exemplo JSON realista a partir de propriedades de schema.
 * Usa constraints (format, enum, min/max) para gerar valores contextualmente adequados.
 */
export function generateExampleFromSchema(properties: SchemaProperty[]): Record<string, unknown> {
  const result: Record<string, unknown> = {};
  for (const prop of properties) {
    if (!prop.name) continue;
    result[prop.name] = generateValueForProperty(prop);
  }
  return result;
}

export function generateValueForProperty(prop: SchemaProperty): unknown {
  // Handle $ref — return a placeholder
  if (prop.type === '$ref') {
    return { $ref: prop.$ref || '#/components/schemas/Unknown' };
  }

  // Handle enum
  if (prop.constraints?.enumValues && prop.constraints.enumValues.length > 0) {
    return prop.constraints.enumValues[0];
  }

  // Handle default value
  if (prop.constraints?.defaultValue) {
    return parseDefaultValue(prop.type, prop.constraints.defaultValue);
  }

  switch (prop.type) {
    case 'string':
      return generateStringExample(prop);
    case 'integer':
      return generateIntegerExample(prop);
    case 'number':
      return generateNumberExample(prop);
    case 'boolean':
      return true;
    case 'array':
      return generateArrayExample(prop);
    case 'object':
      return generateObjectExample(prop);
    default:
      return null;
  }
}

export function generateStringExample(prop: SchemaProperty): string {
  const format = prop.constraints?.format;
  if (format) {
    switch (format) {
      case 'email': return 'user@example.com';
      case 'uuid': return '550e8400-e29b-41d4-a716-446655440000';
      case 'date-time': return '2024-01-15T10:30:00.000Z';
      case 'date': return '2024-01-15';
      case 'time': return '14:30:00Z';
      case 'uri': return 'https://example.com/resource';
      case 'hostname': return 'api.example.com';
      case 'ipv4': return '192.168.1.1';
      case 'ipv6': return '2001:0db8:85a3:0000:0000:8a2e:0370:7334';
      case 'password': return '********';
      case 'byte': return 'U3dhZ2dlciByb2Nrcw==';
      case 'binary': return '<binary>';
      default: break;
    }
  }

  // Use property name for context-aware examples
  const name = prop.name.toLowerCase();
  if (name.includes('email')) return 'user@example.com';
  if (name.includes('phone')) return '+1-555-0123';
  if (name.includes('url') || name.includes('uri') || name.includes('link')) return 'https://example.com';
  if (name.includes('name')) return 'Example Name';
  if (name.includes('description') || name.includes('desc')) return 'A sample description';
  if (name.includes('id')) return 'abc-123';
  if (name.includes('status')) return 'active';
  if (name.includes('type')) return 'default';
  if (name.includes('code')) return 'CODE_001';
  if (name.includes('country')) return 'US';
  if (name.includes('currency')) return 'USD';
  if (name.includes('address')) return '123 Main Street';
  if (name.includes('city')) return 'New York';
  if (name.includes('state')) return 'NY';
  if (name.includes('zip') || name.includes('postal')) return '10001';

  // Apply length constraints
  const minLen = prop.constraints?.minLength ?? 0;
  const maxLen = prop.constraints?.maxLength ?? 0;

  let example = 'string';
  if (minLen > example.length) {
    example = example.padEnd(minLen, 'x');
  }
  if (maxLen > 0 && example.length > maxLen) {
    example = example.substring(0, maxLen);
  }

  return example;
}

export function generateIntegerExample(prop: SchemaProperty): number {
  const min = prop.constraints?.minimum ?? 0;
  const max = prop.constraints?.maximum ?? 100;
  return Math.max(min, Math.min(Math.floor((min + max) / 2), max));
}

export function generateNumberExample(prop: SchemaProperty): number {
  const min = prop.constraints?.minimum ?? 0;
  const max = prop.constraints?.maximum ?? 100;
  return Math.round(((min + max) / 2) * 100) / 100;
}

function generateArrayExample(prop: SchemaProperty): unknown[] {
  if (prop.items) {
    const itemProp: SchemaProperty = {
      ...prop.items,
      name: prop.items.name || 'item',
      id: prop.items.id || 'example-item',
    };
    return [generateValueForProperty(itemProp)];
  }
  return ['item1'];
}

function generateObjectExample(prop: SchemaProperty): Record<string, unknown> {
  if (prop.properties && prop.properties.length > 0) {
    return generateExampleFromSchema(prop.properties);
  }
  return {};
}

function parseDefaultValue(type: string, value: string): unknown {
  switch (type) {
    case 'integer': return parseInt(value, 10) || 0;
    case 'number': return parseFloat(value) || 0;
    case 'boolean': return value === 'true';
    default: return value;
  }
}

/**
 * Formata o exemplo JSON gerado para exibição com indentação.
 */
export function formatExample(example: Record<string, unknown>): string {
  return JSON.stringify(example, null, 2);
}
