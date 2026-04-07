/**
 * FUTURE-ROADMAP 6.1 — Testes unitários da função validateSoapBuilder.
 * São testes puramente de lógica — sem React, sem DOM.
 */
import { describe, it, expect } from 'vitest';
import { validateSoapBuilder } from '../../features/contracts/workspace/builders/shared/builderValidation';
import type {
  SoapBuilderState,
  SoapOperation,
} from '../../features/contracts/workspace/builders/shared/builderTypes';

// ── Helpers ────────────────────────────────────────────────────────────────────

function makeOperation(overrides: Partial<SoapOperation> = {}): SoapOperation {
  return {
    id: 'op-1',
    name: 'GetUser',
    soapAction: 'GetUser',
    inputMessage: 'GetUserRequest',
    outputMessage: 'GetUserResponse',
    faultMessage: '',
    description: '',
    ...overrides,
  };
}

function makeState(overrides: Partial<SoapBuilderState> = {}): SoapBuilderState {
  return {
    serviceName: 'UserService',
    targetNamespace: 'http://example.com/user-service',
    endpoint: 'http://example.com/service',
    binding: 'SOAP 1.1',
    operations: [makeOperation()],
    securityPolicies: [],
    documentationUrl: '',
    description: '',
    version: '1.0',
    ...overrides,
  };
}

// ── Tests ──────────────────────────────────────────────────────────────────────

describe('validateSoapBuilder', () => {
  it('returns valid when state is complete and correct', () => {
    const result = validateSoapBuilder(makeState());
    expect(result.valid).toBe(true);
    expect(result.errors).toHaveLength(0);
  });

  it('returns error for missing serviceName', () => {
    const result = validateSoapBuilder(makeState({ serviceName: '' }));
    expect(result.valid).toBe(false);
    expect(result.errors.some((e) => e.field === 'serviceName')).toBe(true);
  });

  it('treats whitespace-only serviceName as missing', () => {
    const result = validateSoapBuilder(makeState({ serviceName: '   ' }));
    expect(result.valid).toBe(false);
    expect(result.errors.some((e) => e.field === 'serviceName')).toBe(true);
  });

  it('returns error for missing targetNamespace', () => {
    const result = validateSoapBuilder(makeState({ targetNamespace: '' }));
    expect(result.valid).toBe(false);
    expect(result.errors.some((e) => e.field === 'targetNamespace')).toBe(true);
  });

  it('returns error when targetNamespace is not a valid URI', () => {
    const result = validateSoapBuilder(makeState({ targetNamespace: 'not-a-uri' }));
    expect(result.valid).toBe(false);
    const err = result.errors.find((e) => e.field === 'targetNamespace');
    expect(err?.messageKey).toBe('contracts.builder.validation.namespaceMustBeUri');
  });

  it('accepts urn: namespace as valid URI', () => {
    const result = validateSoapBuilder(makeState({ targetNamespace: 'urn:com:example:service' }));
    expect(result.valid).toBe(true);
  });

  it('returns error when endpoint is not a valid URL', () => {
    const result = validateSoapBuilder(makeState({ endpoint: 'ftp://bad-endpoint' }));
    expect(result.valid).toBe(false);
    const err = result.errors.find((e) => e.field === 'endpoint');
    expect(err?.messageKey).toBe('contracts.builder.validation.endpointMustBeUrl');
  });

  it('allows empty endpoint (optional field)', () => {
    const result = validateSoapBuilder(makeState({ endpoint: '' }));
    expect(result.valid).toBe(true);
  });

  it('returns error for operation with missing name', () => {
    const result = validateSoapBuilder(makeState({
      operations: [makeOperation({ name: '' })],
    }));
    expect(result.valid).toBe(false);
    expect(result.errors.some((e) => e.field.includes('op-1') && e.field.includes('name'))).toBe(true);
  });

  it('returns duplicateOperationName error when two operations share the same name', () => {
    const result = validateSoapBuilder(makeState({
      operations: [
        makeOperation({ id: 'op-1', name: 'GetUser' }),
        makeOperation({ id: 'op-2', name: 'GetUser' }),
      ],
    }));
    expect(result.valid).toBe(false);
    expect(result.errors.some((e) => e.messageKey === 'contracts.builder.validation.duplicateOperationName')).toBe(true);
  });

  it('allows two operations with different names', () => {
    const result = validateSoapBuilder(makeState({
      operations: [
        makeOperation({ id: 'op-1', name: 'GetUser' }),
        makeOperation({ id: 'op-2', name: 'CreateUser' }),
      ],
    }));
    expect(result.valid).toBe(true);
  });

  it('is valid with no operations (operations are optional)', () => {
    const result = validateSoapBuilder(makeState({ operations: [] }));
    expect(result.valid).toBe(true);
  });
});
