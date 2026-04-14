/**
 * Reverse parsers: source (YAML/JSON/XML) → visual builder state.
 *
 * Complementa o builderSync.ts que faz visual → source.
 * Permite carregar conteúdo existente no modo Visual do Contract Studio.
 *
 * Limitações conhecidas (comunicadas via warnings):
 * - OpenAPI extensions (x-*) são ignoradas exceto x-nto-metadata
 * - SOAP round-trip parcial (WSDL é demasiado expressivo)
 * - AsyncAPI binding configs complexas podem perder atributos
 * - SharedSchema Avro/Protobuf: apenas metadados são extraídos
 */
import yaml from 'js-yaml';
import type {
  RestBuilderState,
  RestEndpoint,
  RestParameter,
  RestResponse,
  RestRequestBody,
  SchemaProperty,
  SoapBuilderState,
  SoapOperation,
  EventBuilderState,
  EventChannel,
  CompatibilityMode,
  WorkserviceBuilderState,
  WorkserviceDependency,
  MessagingTopic,
  ConsumedService,
  ProducedEvent,
  TriggerType,
  MessagingRole,
  SharedSchemaBuilderState,
  SharedSchemaProperty,
  WebhookBuilderState,
  WebhookHeader,
  LegacyContractBuilderState,
  LegacyContractKind,
  LegacyField,
  LegacyFieldType,
  LegacyEncoding,
} from './builderTypes';

// ── Helpers ──────────────────────────────────────────────────────────────────

let _nextId = 1;
function genId(prefix: string) { return `${prefix}-parse-${_nextId++}`; }

function str(v: unknown): string {
  if (v === null || v === undefined) return '';
  return String(v);
}

function arr(v: unknown): unknown[] {
  return Array.isArray(v) ? v : [];
}

function obj(v: unknown): Record<string, unknown> {
  return v && typeof v === 'object' && !Array.isArray(v) ? (v as Record<string, unknown>) : {};
}

function parseContent(content: string, format: string): unknown {
  if (!content.trim()) return null;
  if (format === 'json') return JSON.parse(content);
  if (format === 'xml') return content; // XML kept as string for SOAP
  // Default: treat as YAML
  return yaml.load(content);
}

// ── Schema resolution & property extraction ──────────────────────────────────

/**
 * Resolve a $ref path to the actual schema object within the document.
 * Supports "#/components/schemas/Name" (OpenAPI 3.x) and "#/definitions/Name" (Swagger 2.x).
 */
function resolveRef(doc: Record<string, unknown>, ref: string): Record<string, unknown> | null {
  if (!ref || !ref.startsWith('#/')) return null;
  const parts = ref.slice(2).split('/');
  let current: unknown = doc;
  for (const part of parts) {
    current = obj(current)[part];
    if (!current) return null;
  }
  return obj(current);
}

/**
 * Extract SchemaProperty[] from a JSON Schema object, resolving $ref when needed.
 * Used to populate the visual builder properties from $ref or inline schemas.
 */
function extractSchemaProps(
  schemaObj: Record<string, unknown>,
  doc: Record<string, unknown>,
  visited: Set<string> = new Set(),
): SchemaProperty[] {
  // Follow $ref if present
  const ref = str(schemaObj.$ref);
  if (ref) {
    if (visited.has(ref)) return []; // circular ref guard
    const resolved = resolveRef(doc, ref);
    if (!resolved) return [];
    return extractSchemaProps(resolved, doc, new Set([...visited, ref]));
  }

  // Array schema — extract from items
  if (str(schemaObj.type) === 'array') {
    const items = obj(schemaObj.items);
    return extractSchemaProps(items, doc, visited);
  }

  // allOf — merge all sub-schemas
  if (Array.isArray(schemaObj.allOf)) {
    const merged: SchemaProperty[] = [];
    const seenNames = new Set<string>();
    for (const sub of arr(schemaObj.allOf)) {
      const props = extractSchemaProps(obj(sub), doc, visited);
      for (const p of props) {
        if (!seenNames.has(p.name)) {
          seenNames.add(p.name);
          merged.push(p);
        }
      }
    }
    return merged;
  }

  const properties = obj(schemaObj.properties);
  if (Object.keys(properties).length === 0) return [];

  const requiredFields = new Set(arr(schemaObj.required).map(String));
  const result: SchemaProperty[] = [];

  for (const [name, propValue] of Object.entries(properties)) {
    result.push(buildSchemaProperty(name, obj(propValue), requiredFields.has(name), doc, visited));
  }

  return result;
}

function buildSchemaProperty(
  name: string,
  prop: Record<string, unknown>,
  isRequired: boolean,
  doc: Record<string, unknown>,
  visited: Set<string>,
): SchemaProperty {
  const ref = str(prop.$ref);
  if (ref) {
    return {
      id: genId('sp'),
      name,
      type: '$ref',
      description: str(prop.description),
      required: isRequired,
      constraints: {},
      $ref: ref,
    };
  }

  const propType = str(prop.type) || 'string';

  // Handle composition types
  for (const compType of ['oneOf', 'anyOf', 'allOf'] as const) {
    if (Array.isArray(prop[compType])) {
      const schemas = arr(prop[compType]).map((s) => {
        const so = obj(s);
        const sRef = str(so.$ref);
        if (sRef) {
          return { id: genId('sp'), name: '', type: '$ref' as const, description: '', required: false, constraints: {}, $ref: sRef };
        }
        const childProps = extractSchemaProps(so, doc, visited);
        return {
          id: genId('sp'), name: '', type: (str(so.type) || 'object') as SchemaProperty['type'],
          description: str(so.description), required: false, constraints: {},
          properties: childProps.length > 0 ? childProps : undefined,
        };
      });
      return {
        id: genId('sp'), name, type: compType, description: str(prop.description),
        required: isRequired, constraints: {}, compositionSchemas: schemas,
      };
    }
  }

  const base: SchemaProperty = {
    id: genId('sp'),
    name,
    type: propType as SchemaProperty['type'],
    description: str(prop.description),
    required: isRequired,
    constraints: {
      format: str(prop.format) || undefined,
      minLength: typeof prop.minLength === 'number' ? prop.minLength : undefined,
      maxLength: typeof prop.maxLength === 'number' ? prop.maxLength : undefined,
      minimum: typeof prop.minimum === 'number' ? prop.minimum : undefined,
      maximum: typeof prop.maximum === 'number' ? prop.maximum : undefined,
      pattern: str(prop.pattern) || undefined,
      defaultValue: prop.default !== undefined ? str(prop.default) : undefined,
      readOnly: Boolean(prop.readOnly) || undefined,
      writeOnly: Boolean(prop.writeOnly) || undefined,
      nullable: Boolean(prop.nullable) || undefined,
      enumValues: Array.isArray(prop.enum) ? prop.enum.map(String) : undefined,
      example: prop.example !== undefined ? (typeof prop.example === 'object' ? JSON.stringify(prop.example) : str(prop.example)) : undefined,
      minItems: typeof prop.minItems === 'number' ? prop.minItems : undefined,
      maxItems: typeof prop.maxItems === 'number' ? prop.maxItems : undefined,
      uniqueItems: Boolean(prop.uniqueItems) || undefined,
    },
  };

  // Nested object
  if (propType === 'object' && prop.properties) {
    base.properties = extractSchemaProps(prop as Record<string, unknown>, doc, visited);
  }

  // Array items
  if (propType === 'array' && prop.items) {
    const itemsObj = obj(prop.items);
    const itemRef = str(itemsObj.$ref);
    if (itemRef) {
      base.items = { id: genId('sp'), name: 'items', type: '$ref', description: '', required: false, constraints: {}, $ref: itemRef };
    } else {
      const itemType = str(itemsObj.type) || 'string';
      base.items = {
        id: genId('sp'), name: 'items', type: itemType as SchemaProperty['type'],
        description: str(itemsObj.description), required: false, constraints: {},
      };
      if (itemType === 'object' && itemsObj.properties) {
        base.items.properties = extractSchemaProps(itemsObj, doc, visited);
      }
    }
  }

  return base;
}

// ── OpenAPI / Swagger → RestBuilderState ─────────────────────────────────────

export function parseOpenApiToRest(content: string, format: string): { state: RestBuilderState; warnings: string[] } {
  const warnings: string[] = [];
  const doc = obj(parseContent(content, format));
  if (!doc) return { state: defaultRestState(), warnings: ['contracts.builder.parse.emptyContent'] };

  const info = obj(doc.info);
  const contact = obj(info.contact);
  const license = obj(info.license);

  const servers: string[] = arr(doc.servers).map((s) => str(obj(s).url)).filter(Boolean);

  // Extract base path from first endpoint or common prefix
  const pathsObj = obj(doc.paths);
  const pathKeys = Object.keys(pathsObj);
  const basePath = extractBasePath(pathKeys);

  const endpoints: RestEndpoint[] = [];
  const httpMethods = new Set(['get', 'post', 'put', 'patch', 'delete', 'head', 'options']);

  for (const [path, pathItem] of Object.entries(pathsObj)) {
    const pItem = obj(pathItem);
    for (const [method, opValue] of Object.entries(pItem)) {
      if (!httpMethods.has(method)) continue;
      const op = obj(opValue);

      const parameters: RestParameter[] = arr(op.parameters).map((p) => {
        const param = obj(p);
        const schema = obj(param.schema);
        return {
          id: genId('prm'),
          name: str(param.name),
          in: (str(param.in) || 'query') as RestParameter['in'],
          required: Boolean(param.required),
          type: str(schema.type) || 'string',
          description: str(param.description),
          constraints: {
            format: str(schema.format) || undefined,
            minLength: typeof schema.minLength === 'number' ? schema.minLength : undefined,
            maxLength: typeof schema.maxLength === 'number' ? schema.maxLength : undefined,
            minimum: typeof schema.minimum === 'number' ? schema.minimum : undefined,
            maximum: typeof schema.maximum === 'number' ? schema.maximum : undefined,
            pattern: str(schema.pattern) || undefined,
            defaultValue: schema.default !== undefined ? str(schema.default) : undefined,
            readOnly: Boolean(schema.readOnly) || undefined,
            writeOnly: Boolean(schema.writeOnly) || undefined,
            nullable: Boolean(schema.nullable) || undefined,
            enumValues: Array.isArray(schema.enum) ? schema.enum.map(String) : undefined,
          },
        };
      });

      let requestBody: RestRequestBody | null = null;
      if (op.requestBody) {
        const rb = obj(op.requestBody);
        const contentMap = obj(rb.content);
        const firstContentType = Object.keys(contentMap)[0] || 'application/json';
        const media = obj(contentMap[firstContentType]);
        const schemaObj = obj(media.schema);
        // Capture $ref from root or from items when schema is type: array
        const ref = str(schemaObj.$ref)
          || (str(schemaObj.type) === 'array' ? str(obj(schemaObj.items).$ref) : '');
        // Always try to resolve the schema — extractSchemaProps handles $ref internally.
        const rbProps = extractSchemaProps(schemaObj, doc);
        // When $ref resolves to properties, clear schema so inline mode is used.
        // When no $ref exists, default to Visual Properties mode (properties=[]).
        // Only keep $ref mode (properties=undefined) when there's an unresolvable $ref.
        requestBody = {
          contentType: firstContentType,
          schema: rbProps.length > 0 ? '' : (ref || ''),
          required: Boolean(rb.required),
          example: media.example ? JSON.stringify(media.example, null, 2) : '',
          properties: rbProps.length > 0 ? rbProps : (ref ? undefined : []),
        };
      }

      const responses: RestResponse[] = [];
      for (const [statusCode, resValue] of Object.entries(obj(op.responses))) {
        const res = obj(resValue);
        const resContent = obj(res.content);
        const firstCt = Object.keys(resContent)[0] || '';
        const resMedia = firstCt ? obj(resContent[firstCt]) : {};
        const resSchema = obj(resMedia.schema);
        // Capture $ref from root or from items when schema is type: array
        const resRef = str(resSchema.$ref)
          || (str(resSchema.type) === 'array' ? str(obj(resSchema.items).$ref) : '');
        // Always try to resolve the schema — extractSchemaProps handles $ref internally.
        const resProps = Object.keys(obj(resSchema)).length > 0
          ? extractSchemaProps(resSchema, doc)
          : [];
        // When $ref resolves to properties, clear schema so inline mode is used.
        // When no $ref exists, default to Visual Properties mode (properties=[]).
        responses.push({
          id: genId('res'),
          statusCode,
          description: str(res.description),
          contentType: firstCt || 'application/json',
          schema: resProps.length > 0 ? '' : resRef,
          example: resMedia.example ? JSON.stringify(resMedia.example, null, 2) : '',
          properties: resProps.length > 0 ? resProps : (resRef ? undefined : []),
        });
      }

      const ntoMeta = obj(op['x-nto-metadata']);
      const tags = arr(op.tags).map(String);

      const strippedPath = basePath ? path.replace(basePath, '') || '/' : path;

      endpoints.push({
        id: genId('ep'),
        method: method.toUpperCase() as RestEndpoint['method'],
        path: strippedPath,
        operationId: str(op.operationId),
        summary: str(op.summary),
        description: str(op.description),
        tags,
        deprecated: Boolean(op.deprecated),
        deprecationNote: str(ntoMeta.deprecationNote),
        parameters,
        requestBody,
        responses,
        authScopes: extractAuthScopes(op.security),
        rateLimit: str(ntoMeta.rateLimit),
        idempotencyKey: str(ntoMeta.idempotencyKey),
        observabilityNotes: str(ntoMeta.observability),
      });
    }
  }

  // Extract components/schemas as reusable RestComponentSchema[]
  const schemasObj = obj(obj(doc.components).schemas);
  const schemas: import('./builderTypes').RestComponentSchema[] = [];
  for (const [schemaName, schemaValue] of Object.entries(schemasObj)) {
    const schemaDef = obj(schemaValue);
    const schemaProps = extractSchemaProps(schemaDef, doc);
    schemas.push({
      id: genId('cs'),
      name: schemaName,
      description: str(schemaDef.description),
      properties: schemaProps,
    });
  }

  return {
    state: {
      basePath: basePath || '/api/v1',
      title: str(info.title),
      version: str(info.version) || '1.0.0',
      description: str(info.description),
      contact: str(contact.name),
      license: str(license.name),
      servers,
      endpoints,
      schemas: schemas.length > 0 ? schemas : undefined,
    },
    warnings,
  };
}

function extractBasePath(paths: string[]): string {
  if (paths.length === 0) return '';
  const segments = paths[0]?.split('/').filter(Boolean) ?? [];
  let common = '';
  for (const seg of segments) {
    if (seg.startsWith('{')) break;
    const candidate = common + '/' + seg;
    if (paths.every((p) => p.startsWith(candidate))) {
      common = candidate;
    } else {
      break;
    }
  }
  return common;
}

function extractAuthScopes(security: unknown): string[] {
  const scopes: string[] = [];
  for (const item of arr(security)) {
    const secObj = obj(item);
    for (const values of Object.values(secObj)) {
      for (const scope of arr(values)) {
        scopes.push(String(scope));
      }
    }
  }
  return scopes;
}

function defaultRestState(): RestBuilderState {
  return {
    basePath: '/api/v1', title: '', version: '1.0.0', description: '',
    contact: '', license: '', servers: [], endpoints: [],
  };
}

// ── WSDL → SoapBuilderState ──────────────────────────────────────────────────

export function parseWsdlToSoap(content: string): { state: SoapBuilderState; warnings: string[] } {
  const warnings: string[] = ['contracts.builder.parse.soapPartialRoundtrip'];

  const parser = new DOMParser();
  const doc = parser.parseFromString(content, 'application/xml');
  const root = doc.documentElement;

  if (!root || root.tagName === 'parsererror') {
    return { state: defaultSoapState(), warnings: [...warnings, 'contracts.builder.parse.invalidXml'] };
  }

  const serviceName = root.getAttribute('name') || '';
  const targetNamespace = root.getAttribute('targetNamespace') || '';

  // Extract documentation
  const docEl = root.querySelector('documentation');
  const description = docEl?.textContent?.trim() || '';

  // Detect binding
  const bindingEl = root.querySelector('[style="document"]') ??
    root.querySelector('binding > *[transport]');
  const isSoap12 = root.innerHTML.includes('soap12');
  const binding: SoapBuilderState['binding'] = isSoap12 ? 'SOAP 1.2' : 'SOAP 1.1';

  // Extract endpoint
  const addressEl = root.querySelector('[location]');
  const endpoint = addressEl?.getAttribute('location') || '';

  // Extract operations from portType
  const operations: SoapOperation[] = [];
  const portTypeOps = root.querySelectorAll('portType > operation');
  portTypeOps.forEach((opEl) => {
    const opDoc = opEl.querySelector('documentation');
    const inputEl = opEl.querySelector('input');
    const outputEl = opEl.querySelector('output');
    const faultEl = opEl.querySelector('fault');

    // Find soapAction from binding
    const opName = opEl.getAttribute('name') || '';
    const bindingOpEl = root.querySelector(`binding > operation[name="${opName}"] > *[soapAction]`);
    const soapAction = bindingOpEl?.getAttribute('soapAction') || '';

    operations.push({
      id: genId('sop'),
      name: opName,
      soapAction,
      inputMessage: extractMsgName(inputEl?.getAttribute('message')),
      outputMessage: extractMsgName(outputEl?.getAttribute('message')),
      faultMessage: extractMsgName(faultEl?.getAttribute('message')),
      description: opDoc?.textContent?.trim() || '',
    });
  });

  return {
    state: {
      serviceName,
      targetNamespace,
      endpoint,
      binding,
      description,
      securityPolicy: '',
      namespaces: [],
      operations,
    },
    warnings,
  };
}

function extractMsgName(qualified: string | null | undefined): string {
  if (!qualified) return '';
  return qualified.includes(':') ? qualified.split(':').pop() || '' : qualified;
}

function defaultSoapState(): SoapBuilderState {
  return {
    serviceName: '', targetNamespace: '', endpoint: '', binding: 'SOAP 1.1',
    description: '', securityPolicy: '', namespaces: [], operations: [],
  };
}

// ── AsyncAPI → EventBuilderState ─────────────────────────────────────────────

export function parseAsyncApiToEvent(content: string, format: string): { state: EventBuilderState; warnings: string[] } {
  const warnings: string[] = [];
  const doc = obj(parseContent(content, format));
  if (!doc) return { state: defaultEventState(), warnings: ['contracts.builder.parse.emptyContent'] };

  const info = obj(doc.info);

  // Extract default broker
  const serversObj = obj(doc.servers);
  const serverEntries = Object.values(serversObj);
  const firstServer = serverEntries.length > 0 ? obj(serverEntries[0]) : {};
  const defaultBroker = str(firstServer.url);

  // Extract channels
  const channelsObj = obj(doc.channels);
  const channels: EventChannel[] = [];

  for (const [topicName, chValue] of Object.entries(channelsObj)) {
    const ch = obj(chValue);
    const pubOp = obj(ch.publish);
    const subOp = obj(ch.subscribe);
    const ntoMeta = obj(ch['x-nto-metadata']);

    const pubMsg = obj(pubOp.message);
    const subMsg = obj(subOp.message);
    const eventName = str(pubMsg.name) || str(subMsg.name) || '';
    const payloadRef = str(obj(pubMsg.payload)?.$ref) || str(obj(subMsg.payload)?.$ref) || '';

    channels.push({
      id: genId('evt'),
      topicName,
      eventName,
      version: '1.0.0',
      keySchema: '',
      payloadSchema: payloadRef,
      headers: '',
      producer: pubOp.operationId ? str(pubOp.operationId) : '',
      consumer: subOp.operationId ? str(subOp.operationId) : '',
      compatibility: (str(ntoMeta.compatibility) || 'BACKWARD') as CompatibilityMode,
      retention: str(ntoMeta.retention) || '7d',
      partitions: str(ntoMeta.partitions) || '3',
      ordering: str(ntoMeta.ordering),
      retries: str(ntoMeta.retries) || '3',
      dlq: str(ntoMeta.dlq),
      idempotent: Boolean(ntoMeta.idempotent),
      description: str(ch.description),
      owner: str(ntoMeta.owner),
      observabilityNotes: str(ntoMeta.observability),
    });
  }

  return {
    state: {
      title: str(info.title),
      version: str(info.version) || '1.0.0',
      description: str(info.description),
      defaultBroker,
      channels,
    },
    warnings,
  };
}

function defaultEventState(): EventBuilderState {
  return { title: '', version: '1.0.0', description: '', defaultBroker: '', channels: [] };
}

// ── NTO YAML → WorkserviceBuilderState ───────────────────────────────────────

export function parseWorkserviceYaml(content: string, format: string): { state: WorkserviceBuilderState; warnings: string[] } {
  const warnings: string[] = [];
  const doc = obj(parseContent(content, format));
  if (!doc) return { state: defaultWorkserviceState(), warnings: ['contracts.builder.parse.emptyContent'] };

  const meta = obj(doc.metadata);
  const trigger = obj(doc.trigger);
  const behavior = obj(doc.behavior);
  const io = obj(doc.io);
  const messaging = obj(doc.messaging);
  const observability = obj(doc.observability);

  const dependencies: WorkserviceDependency[] = arr(doc.dependencies).map((d) => {
    const dep = obj(d);
    return {
      id: genId('dep'),
      name: str(dep.name),
      type: (str(dep.type) || 'Service') as WorkserviceDependency['type'],
      required: dep.required !== false,
    };
  });

  const consumedTopics: MessagingTopic[] = arr(messaging.consumedTopics).map((t) => {
    const topic = obj(t);
    return {
      id: genId('ct'),
      topicName: str(topic.topicName),
      entityType: str(topic.entityType),
      format: (str(topic.format) || '') as MessagingTopic['format'],
    };
  });

  const producedTopics: MessagingTopic[] = arr(messaging.producedTopics).map((t) => {
    const topic = obj(t);
    return {
      id: genId('pt'),
      topicName: str(topic.topicName),
      entityType: str(topic.entityType),
      format: (str(topic.format) || '') as MessagingTopic['format'],
    };
  });

  const consumedServices: ConsumedService[] = arr(messaging.consumedServices).map((s) => {
    const svc = obj(s);
    return {
      id: genId('cs'),
      serviceName: str(svc.serviceName),
      protocol: (str(svc.protocol) || '') as ConsumedService['protocol'],
    };
  });

  const producedEvents: ProducedEvent[] = arr(messaging.producedEvents).map((e) => {
    const evt = obj(e);
    return {
      id: genId('pe'),
      eventName: str(evt.eventName),
      targetTopic: str(evt.targetTopic),
    };
  });

  const timeoutRaw = str(behavior.timeout).replace(/s$/, '');

  return {
    state: {
      name: str(meta.name),
      trigger: (str(trigger.type) || 'Manual') as TriggerType,
      schedule: str(trigger.schedule),
      description: str(meta.description),
      inputs: str(io.inputs),
      outputs: str(io.outputs),
      dependencies,
      retries: str(behavior.retries) || '3',
      timeout: timeoutRaw || '300',
      errorHandling: str(behavior.errorHandling),
      sideEffects: str(doc.sideEffects),
      owner: str(meta.owner),
      observabilityNotes: str(observability.notes),
      healthCheck: str(doc.healthCheck),
      messagingRole: (str(messaging.role) || 'None') as MessagingRole,
      consumedTopics,
      producedTopics,
      consumedServices,
      producedEvents,
    },
    warnings,
  };
}

function defaultWorkserviceState(): WorkserviceBuilderState {
  return {
    name: '', trigger: 'Manual', schedule: '', description: '', inputs: '', outputs: '',
    dependencies: [], retries: '3', timeout: '300', errorHandling: '', sideEffects: '',
    owner: '', observabilityNotes: '', healthCheck: '', messagingRole: 'None',
    consumedTopics: [], producedTopics: [], consumedServices: [], producedEvents: [],
  };
}

// ── JSON Schema → SharedSchemaBuilderState ───────────────────────────────────

export function parseJsonSchemaToSharedSchema(content: string, format: string): { state: SharedSchemaBuilderState; warnings: string[] } {
  const warnings: string[] = [];
  const doc = obj(parseContent(content, format));
  if (!doc) return { state: defaultSharedSchemaState(), warnings: ['contracts.builder.parse.emptyContent'] };

  const ntoMeta = obj(doc['x-nto-metadata']);
  const propsObj = obj(doc.properties);

  const properties: SharedSchemaProperty[] = [];
  const requiredFields = new Set(arr(doc.required).map(String));

  for (const [name, propValue] of Object.entries(propsObj)) {
    properties.push(parseSharedSchemaProperty(name, obj(propValue), requiredFields.has(name)));
  }

  return {
    state: {
      name: str(doc.title) || str(doc.$id)?.split('/').pop() || '',
      version: str(ntoMeta.version) || '1.0.0',
      description: str(doc.description),
      namespace: str(ntoMeta.namespace),
      format: (str(ntoMeta.format) || 'json-schema') as SharedSchemaBuilderState['format'],
      compatibility: (str(ntoMeta.compatibility) || 'BACKWARD') as SharedSchemaBuilderState['compatibility'],
      owner: str(ntoMeta.owner),
      tags: arr(ntoMeta.tags).map(String),
      properties,
      example: '',
    },
    warnings,
  };
}

function parseSharedSchemaProperty(name: string, prop: Record<string, unknown>, required: boolean): SharedSchemaProperty {
  const type = (str(prop.type) || 'string') as SharedSchemaProperty['type'];
  const childRequired = new Set(arr(prop.required).map(String));

  let properties: SharedSchemaProperty[] | undefined;
  if (type === 'object' && prop.properties) {
    properties = Object.entries(obj(prop.properties)).map(([n, v]) =>
      parseSharedSchemaProperty(n, obj(v), childRequired.has(n)),
    );
  }

  let items: SharedSchemaProperty | undefined;
  if (type === 'array' && prop.items) {
    const itemObj = obj(prop.items);
    items = parseSharedSchemaProperty('item', itemObj, false);
  }

  return {
    id: genId('ssp'),
    name,
    type: prop.$ref ? '$ref' : type,
    description: str(prop.description),
    required,
    constraints: {
      format: str(prop.format) || undefined,
      minLength: typeof prop.minLength === 'number' ? prop.minLength : undefined,
      maxLength: typeof prop.maxLength === 'number' ? prop.maxLength : undefined,
      minimum: typeof prop.minimum === 'number' ? prop.minimum : undefined,
      maximum: typeof prop.maximum === 'number' ? prop.maximum : undefined,
      pattern: str(prop.pattern) || undefined,
      defaultValue: prop.default !== undefined ? str(prop.default) : undefined,
      enumValues: Array.isArray(prop.enum) ? prop.enum.map(String) : undefined,
    },
    $ref: str(prop.$ref) || undefined,
    properties,
    items,
  };
}

function defaultSharedSchemaState(): SharedSchemaBuilderState {
  return {
    name: '', version: '1.0.0', description: '', namespace: '', format: 'json-schema',
    compatibility: 'BACKWARD', owner: '', tags: [], properties: [], example: '',
  };
}

// ── NTO YAML → WebhookBuilderState ───────────────────────────────────────────

export function parseWebhookYaml(content: string, format: string): { state: WebhookBuilderState; warnings: string[] } {
  const warnings: string[] = [];
  const doc = obj(parseContent(content, format));
  if (!doc) return { state: defaultWebhookState(), warnings: ['contracts.builder.parse.emptyContent'] };

  const meta = obj(doc.metadata);
  const spec = obj(doc.spec);
  const auth = obj(spec.authentication);
  const retry = obj(spec.retry);
  const observability = obj(doc.observability);

  const headers: WebhookHeader[] = arr(spec.headers).map((h) => {
    const hdr = obj(h);
    return {
      id: genId('whk'),
      name: str(hdr.name),
      value: str(hdr.value),
      required: Boolean(hdr.required),
    };
  });

  return {
    state: {
      name: str(meta.name),
      description: str(meta.description),
      method: (str(spec.method) || 'POST') as WebhookBuilderState['method'],
      urlPattern: str(spec.urlPattern),
      contentType: str(spec.contentType) || 'application/json',
      payloadSchema: str(spec.payloadSchema),
      headers,
      authentication: (str(auth.type) || 'none') as WebhookBuilderState['authentication'],
      secretHeaderName: str(auth.secretHeaderName),
      retryPolicy: str(retry.policy),
      retryCount: str(retry.count) || '3',
      timeout: str(retry.timeout).replace(/s$/, '') || '30',
      events: arr(spec.events).map(String),
      owner: str(meta.owner),
      observabilityNotes: str(observability.notes),
    },
    warnings,
  };
}

function defaultWebhookState(): WebhookBuilderState {
  return {
    name: '', description: '', method: 'POST', urlPattern: '', contentType: 'application/json',
    payloadSchema: '', headers: [], authentication: 'none', secretHeaderName: '',
    retryPolicy: '', retryCount: '3', timeout: '30', events: [], owner: '', observabilityNotes: '',
  };
}

// ── NTO YAML → LegacyContractBuilderState ────────────────────────────────────

export function parseLegacyContractYaml(content: string, format: string, kind: LegacyContractKind): { state: LegacyContractBuilderState; warnings: string[] } {
  const warnings: string[] = [];
  const doc = obj(parseContent(content, format));
  if (!doc) return { state: defaultLegacyState(kind), warnings: ['contracts.builder.parse.emptyContent'] };

  const meta = obj(doc.metadata);
  const spec = obj(doc.spec);
  const observability = obj(doc.observability);

  const detectedKind = str(doc.kind) as LegacyContractKind;
  const resolvedKind = (['Copybook', 'MqMessage', 'FixedLayout', 'CicsCommarea'].includes(detectedKind) ? detectedKind : kind);

  const fields: LegacyField[] = arr(doc.fields).map((f) => {
    const field = obj(f);
    return {
      id: genId('lgc'),
      name: str(field.name),
      level: str(field.level) || '05',
      type: (str(field.type) || 'alphanumeric') as LegacyFieldType,
      length: str(field.length),
      offset: str(field.offset),
      picture: str(field.picture),
      description: str(field.description),
      occurs: str(field.occurs),
      redefines: str(field.redefines),
    };
  });

  return {
    state: {
      kind: resolvedKind,
      name: str(meta.name),
      version: str(meta.version) || '1.0.0',
      description: str(meta.description),
      encoding: (str(spec.encoding) || 'EBCDIC') as LegacyEncoding,
      totalLength: str(spec.totalLength),
      owner: str(meta.owner),
      programName: str(spec.programName),
      queueManager: str(spec.queueManager),
      queueName: str(spec.queueName),
      messageFormat: str(spec.messageFormat),
      transactionId: str(spec.transactionId),
      commareaLength: str(spec.commareaLength),
      fields,
      observabilityNotes: str(observability.notes),
    },
    warnings,
  };
}

function defaultLegacyState(kind: LegacyContractKind): LegacyContractBuilderState {
  return {
    kind, name: '', version: '1.0.0', description: '', encoding: 'EBCDIC',
    totalLength: '', owner: '', programName: '', queueManager: '', queueName: '',
    messageFormat: '', transactionId: '', commareaLength: '', fields: [], observabilityNotes: '',
  };
}
