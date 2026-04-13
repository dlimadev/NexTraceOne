/**
 * Constantes e funções auxiliares para o VisualRestBuilder.
 *
 * Centraliza a criação de entidades (endpoints, parâmetros, respostas),
 * as constantes de método HTTP, tipos e status codes,
 * e a lógica de geração de respostas RFC 7807 Problem Details.
 */
import type {
  RestEndpoint,
  RestParameter,
  RestResponse,
  SchemaProperty,
} from './shared/builderTypes';

export const HTTP_METHODS = ['GET', 'POST', 'PUT', 'PATCH', 'DELETE', 'HEAD', 'OPTIONS'] as const;
export const PARAM_LOCATIONS = ['query', 'path', 'header', 'cookie'] as const;
export const PARAM_TYPES = ['string', 'integer', 'number', 'boolean', 'array', 'object'] as const;
export const STATUS_CODES = ['200', '201', '204', '400', '401', '403', '404', '409', '422', '500', '502', '503'] as const;

export const METHOD_COLORS: Record<string, string> = {
  GET: 'bg-mint/15 text-mint border border-mint/25',
  POST: 'bg-cyan/15 text-cyan border border-cyan/25',
  PUT: 'bg-warning/15 text-warning border border-warning/25',
  PATCH: 'bg-accent/15 text-accent border border-accent/25',
  DELETE: 'bg-danger/15 text-danger border border-danger/25',
  HEAD: 'bg-muted/15 text-muted border border-muted/25',
  OPTIONS: 'bg-muted/10 text-muted/60 border border-muted/15',
};

export const FORMAT_OPTIONS = ['', 'date', 'date-time', 'email', 'uri', 'uuid', 'hostname', 'ipv4', 'ipv6', 'byte', 'binary', 'password', 'int32', 'int64', 'float', 'double'] as const;

/** Gera IDs únicos por instância do builder usando crypto.randomUUID(). */
export function genId(prefix: string) {
  return `${prefix}-${crypto.randomUUID()}`;
}

/** Cria as propriedades RFC 7807 Problem Details para um conjunto de campos. */
function problemDetailsProps(fields: string[]): SchemaProperty[] {
  const typeMap: Record<string, SchemaProperty['type']> = {
    type: 'string',
    title: 'string',
    status: 'integer',
    detail: 'string',
    instance: 'string',
  };
  return fields.map((name) => ({
    id: genId('pd'),
    name,
    type: typeMap[name] ?? 'string',
    description: '',
    required: false,
    constraints: {},
  }));
}

/** Resposta HTTP com schema RFC 7807. */
function createProblemResponse(statusCode: string, description: string, fields: string[]): RestResponse {
  return {
    id: genId('res'),
    statusCode,
    description,
    contentType: 'application/problem+json',
    schema: '',
    example: '',
    properties: problemDetailsProps(fields),
  };
}

/** Colecção de respostas de erro comuns RFC 7807. */
export const COMMON_ERROR_RESPONSES: RestResponse[] = [
  createProblemResponse('400', 'Bad Request', ['type', 'title', 'status', 'detail', 'instance']),
  createProblemResponse('401', 'Unauthorized', ['type', 'title', 'status']),
  createProblemResponse('403', 'Forbidden', ['type', 'title', 'status']),
  createProblemResponse('404', 'Not Found', ['type', 'title', 'status']),
  createProblemResponse('500', 'Internal Server Error', ['type', 'title', 'status', 'detail']),
];

export function createEndpoint(): RestEndpoint {
  return {
    id: genId('ep'),
    method: 'GET',
    path: '/resource',
    operationId: '',
    summary: '',
    description: '',
    tags: [],
    deprecated: false,
    deprecationNote: '',
    parameters: [],
    requestBody: null,
    responses: [{ id: genId('res'), statusCode: '200', description: 'OK', contentType: 'application/json', schema: '', example: '', properties: [] }],
    authScopes: [],
    rateLimit: '',
    idempotencyKey: '',
    observabilityNotes: '',
  };
}

export function createParameter(): RestParameter {
  return { id: genId('param'), name: '', in: 'query', required: false, type: 'string', description: '', constraints: {} };
}

export function createResponse(): RestResponse {
  return { id: genId('res'), statusCode: '200', description: '', contentType: 'application/json', schema: '', example: '', properties: [] };
}
