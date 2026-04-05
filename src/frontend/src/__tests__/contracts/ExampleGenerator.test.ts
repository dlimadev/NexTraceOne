import { describe, it, expect } from 'vitest';
import {
  generateExampleFromSchema,
  generateValueForProperty,
  generateStringExample,
  generateIntegerExample,
  generateNumberExample,
  formatExample,
} from '../../features/contracts/workspace/builders/shared/ExampleGenerator';
import type { SchemaProperty } from '../../features/contracts/workspace/builders/shared/builderTypes';

// ── Helper ─────────────────────────────────────────────────────────────────────

function makeProp(overrides: Partial<SchemaProperty> = {}): SchemaProperty {
  return {
    id: 'test-id',
    name: 'testProp',
    type: 'string',
    description: '',
    required: false,
    constraints: {},
    ...overrides,
  };
}

// ── generateExampleFromSchema ──────────────────────────────────────────────────

describe('generateExampleFromSchema', () => {
  it('returns empty object for empty properties array', () => {
    expect(generateExampleFromSchema([])).toEqual({});
  });

  it('skips properties without a name', () => {
    const props: SchemaProperty[] = [makeProp({ name: '' })];
    expect(generateExampleFromSchema(props)).toEqual({});
  });

  it('generates an object with keys matching property names', () => {
    const props: SchemaProperty[] = [
      makeProp({ name: 'firstName', type: 'string' }),
      makeProp({ name: 'age', type: 'integer' }),
    ];
    const result = generateExampleFromSchema(props);
    expect(result).toHaveProperty('firstName');
    expect(result).toHaveProperty('age');
  });

  it('handles nested objects', () => {
    const props: SchemaProperty[] = [
      makeProp({
        name: 'address',
        type: 'object',
        properties: [
          makeProp({ name: 'street', type: 'string' }),
          makeProp({ name: 'zip', type: 'string' }),
        ],
      }),
    ];
    const result = generateExampleFromSchema(props);
    expect(result.address).toEqual({ street: 'string', zip: '10001' });
  });

  it('handles arrays with items', () => {
    const props: SchemaProperty[] = [
      makeProp({
        name: 'tags',
        type: 'array',
        items: makeProp({ name: 'item', type: 'string' }),
      }),
    ];
    const result = generateExampleFromSchema(props);
    expect(result.tags).toEqual(['string']);
  });
});

// ── generateStringExample ──────────────────────────────────────────────────────

describe('generateStringExample', () => {
  it('returns email for email format', () => {
    expect(generateStringExample(makeProp({ constraints: { format: 'email' } }))).toBe('user@example.com');
  });

  it('returns uuid for uuid format', () => {
    expect(generateStringExample(makeProp({ constraints: { format: 'uuid' } }))).toBe('550e8400-e29b-41d4-a716-446655440000');
  });

  it('returns date-time for date-time format', () => {
    expect(generateStringExample(makeProp({ constraints: { format: 'date-time' } }))).toBe('2024-01-15T10:30:00.000Z');
  });

  it('returns date for date format', () => {
    expect(generateStringExample(makeProp({ constraints: { format: 'date' } }))).toBe('2024-01-15');
  });

  it('returns uri for uri format', () => {
    expect(generateStringExample(makeProp({ constraints: { format: 'uri' } }))).toBe('https://example.com/resource');
  });

  it('returns hostname for hostname format', () => {
    expect(generateStringExample(makeProp({ constraints: { format: 'hostname' } }))).toBe('api.example.com');
  });

  it('returns ipv4 for ipv4 format', () => {
    expect(generateStringExample(makeProp({ constraints: { format: 'ipv4' } }))).toBe('192.168.1.1');
  });

  it('returns password mask for password format', () => {
    expect(generateStringExample(makeProp({ constraints: { format: 'password' } }))).toBe('********');
  });

  it('uses property name for context: email', () => {
    expect(generateStringExample(makeProp({ name: 'userEmail' }))).toBe('user@example.com');
  });

  it('uses property name for context: name', () => {
    expect(generateStringExample(makeProp({ name: 'displayName' }))).toBe('Example Name');
  });

  it('uses property name for context: city', () => {
    expect(generateStringExample(makeProp({ name: 'city' }))).toBe('New York');
  });

  it('applies minLength padding', () => {
    const result = generateStringExample(makeProp({ name: 'token', constraints: { minLength: 20 } }));
    expect(result.length).toBeGreaterThanOrEqual(20);
  });

  it('applies maxLength truncation', () => {
    const result = generateStringExample(makeProp({ name: 'token', constraints: { maxLength: 3 } }));
    expect(result.length).toBeLessThanOrEqual(3);
  });

  it('returns default "string" for generic property', () => {
    expect(generateStringExample(makeProp({ name: 'data' }))).toBe('string');
  });
});

// ── generateIntegerExample ─────────────────────────────────────────────────────

describe('generateIntegerExample', () => {
  it('returns 50 with no constraints (midpoint of 0-100)', () => {
    expect(generateIntegerExample(makeProp({ type: 'integer' }))).toBe(50);
  });

  it('respects minimum constraint', () => {
    const result = generateIntegerExample(makeProp({ type: 'integer', constraints: { minimum: 10 } }));
    expect(result).toBeGreaterThanOrEqual(10);
  });

  it('respects maximum constraint', () => {
    const result = generateIntegerExample(makeProp({ type: 'integer', constraints: { maximum: 20 } }));
    expect(result).toBeLessThanOrEqual(20);
  });

  it('returns midpoint between min and max', () => {
    const result = generateIntegerExample(makeProp({ type: 'integer', constraints: { minimum: 10, maximum: 20 } }));
    expect(result).toBe(15);
  });

  it('returns integer (no decimals)', () => {
    const result = generateIntegerExample(makeProp({ type: 'integer', constraints: { minimum: 1, maximum: 4 } }));
    expect(Number.isInteger(result)).toBe(true);
  });
});

// ── generateNumberExample ──────────────────────────────────────────────────────

describe('generateNumberExample', () => {
  it('returns 50 with no constraints', () => {
    expect(generateNumberExample(makeProp({ type: 'number' }))).toBe(50);
  });

  it('returns midpoint rounded to 2 decimals', () => {
    const result = generateNumberExample(makeProp({ type: 'number', constraints: { minimum: 0, maximum: 1 } }));
    expect(result).toBe(0.5);
  });
});

// ── generateValueForProperty ───────────────────────────────────────────────────

describe('generateValueForProperty', () => {
  it('returns $ref placeholder for $ref type', () => {
    const prop = makeProp({ type: '$ref', $ref: '#/components/schemas/Address' });
    expect(generateValueForProperty(prop)).toEqual({ $ref: '#/components/schemas/Address' });
  });

  it('returns first enum value when enumValues set', () => {
    const prop = makeProp({ constraints: { enumValues: ['active', 'inactive', 'pending'] } });
    expect(generateValueForProperty(prop)).toBe('active');
  });

  it('returns parsed default value for integer', () => {
    const prop = makeProp({ type: 'integer', constraints: { defaultValue: '42' } });
    expect(generateValueForProperty(prop)).toBe(42);
  });

  it('returns parsed default value for boolean', () => {
    const prop = makeProp({ type: 'boolean', constraints: { defaultValue: 'true' } });
    expect(generateValueForProperty(prop)).toBe(true);
  });

  it('returns true for boolean type without constraints', () => {
    expect(generateValueForProperty(makeProp({ type: 'boolean' }))).toBe(true);
  });

  it('returns null for unknown type', () => {
    const prop = makeProp({ type: 'unknown' as SchemaProperty['type'] });
    expect(generateValueForProperty(prop)).toBeNull();
  });

  it('returns empty object for object type without properties', () => {
    const prop = makeProp({ type: 'object' });
    expect(generateValueForProperty(prop)).toEqual({});
  });

  it('returns [item1] for array type without items', () => {
    const prop = makeProp({ type: 'array' });
    expect(generateValueForProperty(prop)).toEqual(['item1']);
  });
});

// ── formatExample ──────────────────────────────────────────────────────────────

describe('formatExample', () => {
  it('returns pretty-printed JSON with 2-space indentation', () => {
    const example = { name: 'test', age: 25 };
    const formatted = formatExample(example);
    expect(formatted).toBe(JSON.stringify(example, null, 2));
  });

  it('handles empty object', () => {
    expect(formatExample({})).toBe('{}');
  });

  it('handles nested objects', () => {
    const example = { address: { street: '123 Main' } };
    expect(formatExample(example)).toContain('  "address"');
  });
});
