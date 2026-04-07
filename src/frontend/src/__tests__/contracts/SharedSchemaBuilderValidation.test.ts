/**
 * FUTURE-ROADMAP 6.1 — Testes unitários da função validateSharedSchemaBuilder.
 * São testes puramente de lógica — sem React, sem DOM.
 */
import { describe, it, expect } from 'vitest';
import { validateSharedSchemaBuilder } from '../../features/contracts/workspace/builders/shared/builderValidation';
import type {
  SharedSchemaBuilderState,
  SharedSchemaProperty,
} from '../../features/contracts/workspace/builders/shared/builderTypes';

// ── Helpers ────────────────────────────────────────────────────────────────────

function makeProp(overrides: Partial<SharedSchemaProperty> = {}): SharedSchemaProperty {
  return {
    id: 'prop-1',
    name: 'userId',
    type: 'string',
    description: '',
    required: true,
    constraints: {},
    ...overrides,
  };
}

function makeState(overrides: Partial<SharedSchemaBuilderState> = {}): SharedSchemaBuilderState {
  return {
    name: 'UserDto',
    version: '1.0.0',
    description: '',
    namespace: '',
    format: 'json-schema',
    compatibility: 'BACKWARD',
    owner: '',
    tags: [],
    properties: [makeProp()],
    example: '',
    ...overrides,
  };
}

// ── Tests ──────────────────────────────────────────────────────────────────────

describe('validateSharedSchemaBuilder', () => {
  it('returns valid when state is complete and correct', () => {
    const result = validateSharedSchemaBuilder(makeState());
    expect(result.valid).toBe(true);
    expect(result.errors).toHaveLength(0);
  });

  it('returns error for missing name', () => {
    const result = validateSharedSchemaBuilder(makeState({ name: '' }));
    expect(result.valid).toBe(false);
    expect(result.errors.some((e) => e.field === 'name')).toBe(true);
  });

  it('treats whitespace-only name as missing', () => {
    const result = validateSharedSchemaBuilder(makeState({ name: '  ' }));
    expect(result.valid).toBe(false);
    expect(result.errors.some((e) => e.field === 'name')).toBe(true);
  });

  it('returns error for missing version', () => {
    const result = validateSharedSchemaBuilder(makeState({ version: '' }));
    expect(result.valid).toBe(false);
    expect(result.errors.some((e) => e.field === 'version')).toBe(true);
  });

  it('returns propertiesRequired error when properties array is empty', () => {
    const result = validateSharedSchemaBuilder(makeState({ properties: [] }));
    expect(result.valid).toBe(false);
    const err = result.errors.find((e) => e.field === 'properties');
    expect(err?.messageKey).toBe('contracts.builder.validation.propertiesRequired');
  });

  it('returns error for property with missing name', () => {
    const result = validateSharedSchemaBuilder(makeState({
      properties: [makeProp({ name: '' })],
    }));
    expect(result.valid).toBe(false);
    expect(result.errors.some((e) => e.field.includes('prop-1') && e.field.includes('name'))).toBe(true);
  });

  it('returns duplicatePropertyName error when two properties share the same name', () => {
    const result = validateSharedSchemaBuilder(makeState({
      properties: [
        makeProp({ id: 'prop-1', name: 'userId' }),
        makeProp({ id: 'prop-2', name: 'userId' }),
      ],
    }));
    expect(result.valid).toBe(false);
    expect(result.errors.some((e) => e.messageKey === 'contracts.builder.validation.duplicatePropertyName')).toBe(true);
  });

  it('allows two properties with different names', () => {
    const result = validateSharedSchemaBuilder(makeState({
      properties: [
        makeProp({ id: 'prop-1', name: 'userId' }),
        makeProp({ id: 'prop-2', name: 'email' }),
      ],
    }));
    expect(result.valid).toBe(true);
  });

  it('accumulates multiple errors', () => {
    const result = validateSharedSchemaBuilder(makeState({ name: '', version: '', properties: [] }));
    expect(result.valid).toBe(false);
    expect(result.errors.length).toBeGreaterThanOrEqual(3);
  });
});
