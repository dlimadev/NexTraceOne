/**
 * PA-09: Testes unitários da função validateRestBuilder.
 * São testes puramente de lógica — sem React, sem DOM.
 */
import { describe, it, expect } from 'vitest';
import { validateRestBuilder } from '../../features/contracts/workspace/builders/shared/builderValidation';
import type {
  RestBuilderState,
  RestEndpoint,
  RestResponse,
  SchemaProperty,
} from '../../features/contracts/workspace/builders/shared/builderTypes';

// ── Helpers ────────────────────────────────────────────────────────────────────

function makeResponse(overrides: Partial<RestResponse> = {}): RestResponse {
  return {
    id: 'res-1',
    statusCode: '200',
    description: 'OK',
    contentType: 'application/json',
    schema: '',
    example: '',
    ...overrides,
  };
}

function makeEndpoint(overrides: Partial<RestEndpoint> = {}): RestEndpoint {
  return {
    id: 'ep-1',
    method: 'GET',
    path: '/items',
    operationId: 'listItems',
    summary: '',
    description: '',
    tags: [],
    deprecated: false,
    deprecationNote: '',
    parameters: [],
    requestBody: null,
    responses: [makeResponse()],
    authScopes: [],
    rateLimit: '',
    idempotencyKey: '',
    observabilityNotes: '',
    ...overrides,
  };
}

function makeState(overrides: Partial<RestBuilderState> = {}): RestBuilderState {
  return {
    title: 'Payments API',
    basePath: '/api/v1',
    version: '1.0.0',
    description: '',
    contact: '',
    license: '',
    servers: [],
    endpoints: [makeEndpoint()],
    ...overrides,
  };
}

// ── Tests ──────────────────────────────────────────────────────────────────────

describe('validateRestBuilder', () => {
  it('returns valid result when state is complete and correct', () => {
    const result = validateRestBuilder(makeState());
    expect(result.valid).toBe(true);
    expect(result.errors).toHaveLength(0);
  });

  it('returns error for missing title', () => {
    const result = validateRestBuilder(makeState({ title: '' }));
    expect(result.valid).toBe(false);
    const err = result.errors.find((e) => e.field === 'title');
    expect(err).toBeDefined();
    expect(err?.messageKey).toBe('contracts.builder.validation.titleRequired');
  });

  it('treats whitespace-only title as missing', () => {
    const result = validateRestBuilder(makeState({ title: '   ' }));
    expect(result.valid).toBe(false);
    expect(result.errors.some((e) => e.field === 'title')).toBe(true);
  });

  it('returns error for missing basePath', () => {
    const result = validateRestBuilder(makeState({ basePath: '' }));
    expect(result.valid).toBe(false);
    const err = result.errors.find((e) => e.field === 'basePath');
    expect(err).toBeDefined();
    expect(err?.messageKey).toBe('contracts.builder.validation.basePathRequired');
  });

  it('returns error when basePath does not start with "/"', () => {
    const result = validateRestBuilder(makeState({ basePath: 'api/v1' }));
    expect(result.valid).toBe(false);
    const err = result.errors.find(
      (e) => e.field === 'basePath' && e.messageKey === 'contracts.builder.validation.basePathSlash',
    );
    expect(err).toBeDefined();
  });

  it('does not return version error when version is empty (version is not validated by REST builder)', () => {
    // RestBuilderState includes a version field but validateRestBuilder does not enforce it.
    const result = validateRestBuilder(makeState({ version: '' }));
    expect(result.errors.every((e) => e.field !== 'version')).toBe(true);
  });

  it('returns error when endpoint path is empty', () => {
    const ep = makeEndpoint({ path: '' });
    const result = validateRestBuilder(makeState({ endpoints: [ep] }));
    expect(result.valid).toBe(false);
    expect(result.errors.some((e) => e.field.includes('path') && e.messageKey === 'contracts.builder.validation.pathRequired')).toBe(true);
  });

  it('returns pathSyntaxInvalid error when path does not start with "/"', () => {
    const ep = makeEndpoint({ path: 'items/list' });
    const result = validateRestBuilder(makeState({ endpoints: [ep] }));
    expect(result.valid).toBe(false);
    expect(result.errors.some((e) => e.messageKey === 'contracts.builder.validation.pathSyntaxInvalid')).toBe(true);
  });

  it('returns pathSyntaxInvalid error when path has unbalanced braces', () => {
    const ep = makeEndpoint({ path: '/items/{id' });
    const result = validateRestBuilder(makeState({ endpoints: [ep] }));
    expect(result.valid).toBe(false);
    expect(result.errors.some((e) => e.messageKey === 'contracts.builder.validation.pathSyntaxInvalid')).toBe(true);
  });

  it('returns pathParamNotDeclared error when path uses {param} not in parameters list', () => {
    const ep = makeEndpoint({ path: '/items/{id}', parameters: [] });
    const result = validateRestBuilder(makeState({ endpoints: [ep] }));
    expect(result.valid).toBe(false);
    expect(result.errors.some((e) => e.messageKey === 'contracts.builder.validation.pathParamNotDeclared')).toBe(true);
  });

  it('returns pathParamMustBeRequired error when path param is declared but not required', () => {
    const ep = makeEndpoint({
      path: '/items/{id}',
      parameters: [
        {
          id: 'p-1',
          name: 'id',
          in: 'path',
          required: false,
          type: 'string',
          description: '',
          constraints: {},
        },
      ],
    });
    const result = validateRestBuilder(makeState({ endpoints: [ep] }));
    expect(result.valid).toBe(false);
    expect(result.errors.some((e) => e.messageKey === 'contracts.builder.validation.pathParamMustBeRequired')).toBe(true);
  });

  it('path param declared and required does not produce param errors', () => {
    const ep = makeEndpoint({
      path: '/items/{id}',
      parameters: [
        {
          id: 'p-1',
          name: 'id',
          in: 'path',
          required: true,
          type: 'string',
          description: '',
          constraints: {},
        },
      ],
    });
    const result = validateRestBuilder(makeState({ endpoints: [ep] }));
    expect(result.errors.some((e) =>
      e.messageKey === 'contracts.builder.validation.pathParamNotDeclared' ||
      e.messageKey === 'contracts.builder.validation.pathParamMustBeRequired',
    )).toBe(false);
  });

  it('method field is present in type but not explicitly validated by REST builder', () => {
    // The REST builder enforces method via TypeScript union type; no runtime check is emitted.
    const ep = makeEndpoint({ method: 'POST' });
    const result = validateRestBuilder(makeState({ endpoints: [ep] }));
    expect(result.valid).toBe(true);
  });

  it('returns responseRequired error when endpoint has no responses', () => {
    const ep = makeEndpoint({ responses: [] });
    const result = validateRestBuilder(makeState({ endpoints: [ep] }));
    expect(result.valid).toBe(false);
    expect(result.errors.some((e) => e.messageKey === 'contracts.builder.validation.responseRequired')).toBe(true);
  });

  it('returns duplicateOperationId error when two endpoints share the same operationId', () => {
    const ep1 = makeEndpoint({ id: 'ep-1', operationId: 'getItem', path: '/a' });
    const ep2 = makeEndpoint({ id: 'ep-2', operationId: 'getItem', path: '/b' });
    const result = validateRestBuilder(makeState({ endpoints: [ep1, ep2] }));
    expect(result.valid).toBe(false);
    expect(result.errors.some((e) => e.messageKey === 'contracts.builder.validation.duplicateOperationId')).toBe(true);
  });

  it('allows two endpoints with different operationIds', () => {
    const ep1 = makeEndpoint({ id: 'ep-1', operationId: 'listItems', path: '/items' });
    const ep2 = makeEndpoint({ id: 'ep-2', operationId: 'createItem', path: '/items/create' });
    const result = validateRestBuilder(makeState({ endpoints: [ep1, ep2] }));
    expect(result.errors.some((e) => e.messageKey === 'contracts.builder.validation.duplicateOperationId')).toBe(false);
  });

  it('returns duplicateStatusCode error when same status code appears twice in one endpoint', () => {
    const ep = makeEndpoint({
      responses: [
        makeResponse({ id: 'r1', statusCode: '200' }),
        makeResponse({ id: 'r2', statusCode: '200' }),
      ],
    });
    const result = validateRestBuilder(makeState({ endpoints: [ep] }));
    expect(result.valid).toBe(false);
    expect(result.errors.some((e) => e.messageKey === 'contracts.builder.validation.duplicateStatusCode')).toBe(true);
  });

  it('allows different status codes in the same endpoint', () => {
    const ep = makeEndpoint({
      responses: [
        makeResponse({ id: 'r1', statusCode: '200' }),
        makeResponse({ id: 'r2', statusCode: '404' }),
      ],
    });
    const result = validateRestBuilder(makeState({ endpoints: [ep] }));
    expect(result.errors.some((e) => e.messageKey === 'contracts.builder.validation.duplicateStatusCode')).toBe(false);
  });

  it('returns propNameDuplicate error for duplicate property names in response body', () => {
    const makeProp = (id: string, name: string): SchemaProperty => ({
      id,
      name,
      type: 'string',
      description: '',
      required: false,
      constraints: {},
    });
    const ep = makeEndpoint({
      responses: [
        {
          ...makeResponse({ id: 'r1' }),
          properties: [makeProp('p1', 'userId'), makeProp('p2', 'userId')],
        },
      ],
    });
    const result = validateRestBuilder(makeState({ endpoints: [ep] }));
    expect(result.valid).toBe(false);
    expect(result.errors.some((e) => e.messageKey === 'contracts.builder.validation.propNameDuplicate')).toBe(true);
  });

  it('returns propNameDuplicate error for duplicate nested property names in request body', () => {
    const makeProp = (id: string, name: string): SchemaProperty => ({
      id,
      name,
      type: 'string',
      description: '',
      required: false,
      constraints: {},
    });
    const ep = makeEndpoint({
      requestBody: {
        contentType: 'application/json',
        schema: '',
        required: true,
        example: '',
        properties: [makeProp('p1', 'amount'), makeProp('p2', 'amount')],
      },
    });
    const result = validateRestBuilder(makeState({ endpoints: [ep] }));
    expect(result.valid).toBe(false);
    expect(result.errors.some((e) => e.messageKey === 'contracts.builder.validation.propNameDuplicate')).toBe(true);
  });

  it('treats whitespace-only basePath as missing', () => {
    const result = validateRestBuilder(makeState({ basePath: '   ' }));
    expect(result.valid).toBe(false);
    expect(result.errors.some((e) => e.field === 'basePath' && e.messageKey === 'contracts.builder.validation.basePathRequired')).toBe(true);
  });

  it('accumulates multiple errors in a single pass', () => {
    const result = validateRestBuilder(
      makeState({ title: '', basePath: '', endpoints: [] }),
    );
    expect(result.valid).toBe(false);
    expect(result.errors.length).toBeGreaterThanOrEqual(2);
  });
});
