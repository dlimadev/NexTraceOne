/**
 * Motor de sincronização visual ↔ source.
 *
 * Gera YAML/JSON/XML a partir do estado dos builders e, quando possível,
 * reconstrói o estado do builder a partir do source.
 *
 * Limitações conhecidas:
 * - SOAP round-trip é parcial (WSDL é demasiado expressivo para representação visual total)
 * - Extensões OpenAPI customizadas não são preservadas no visual
 * - AsyncAPI com binding configs complexas pode perder atributos no round-trip
 *
 * Estas limitações são comunicadas ao utilizador via SyncResult.warnings.
 */
import type {
  RestBuilderState,
  SoapBuilderState,
  EventBuilderState,
  WorkserviceBuilderState,
  SharedSchemaBuilderState,
  SharedSchemaProperty,
  SchemaProperty,
  WebhookBuilderState,
  LegacyContractBuilderState,
  SyncResult,
} from './builderTypes';

interface RestEndpointParameter {
  name: string;
  in: string;
  required?: boolean;
  type?: string;
  description?: string;
  constraints?: import('./builderTypes').PropertyConstraints;
}

interface RestEndpointRequestBody {
  required?: boolean;
  contentType?: string;
  schema?: string;
  properties?: SchemaProperty[];
}

interface RestEndpointResponse {
  statusCode: string | number;
  description?: string;
  contentType?: string;
  schema?: string;
  properties?: SchemaProperty[];
}

interface RestEndpointShape {
  path: string;
  method: string;
  operationId?: string;
  summary?: string;
  description?: string;
  tags: string[];
  deprecated?: boolean;
  parameters: RestEndpointParameter[];
  requestBody?: RestEndpointRequestBody;
  responses: RestEndpointResponse[];
  authScopes: string[];
  rateLimit?: string;
  idempotencyKey?: string;
  observabilityNotes?: string;
  deprecationNote?: string;
}

// ── REST → OpenAPI YAML ───────────────────────────────────────────────────────

export function restBuilderToYaml(state: RestBuilderState): SyncResult {
  const warnings: string[] = [];
  const unsupported: string[] = [];

  let yaml = `openapi: "3.0.3"\n`;
  yaml += `info:\n`;
  yaml += `  title: "${esc(state.title || 'Untitled API')}"\n`;
  yaml += `  version: "${esc(state.version || '1.0.0')}"\n`;
  if (state.description) yaml += `  description: "${esc(state.description)}"\n`;
  if (state.contact) yaml += `  contact:\n    name: "${esc(state.contact)}"\n`;
  if (state.license) yaml += `  license:\n    name: "${esc(state.license)}"\n`;

  if (state.servers.length > 0) {
    yaml += `servers:\n`;
    for (const s of state.servers) {
      yaml += `  - url: "${esc(s)}"\n`;
    }
  }

  yaml += `paths:\n`;

  if (state.endpoints.length === 0) {
    yaml += `  {} # No endpoints defined\n`;
  } else {
    const grouped = groupByPath(state.endpoints as RestEndpointShape[]);
    for (const [path, eps] of Object.entries(grouped)) {
      yaml += `  ${state.basePath}${path}:\n`;
      for (const ep of eps) {
        yaml += `    ${ep.method.toLowerCase()}:\n`;
        if (ep.operationId) yaml += `      operationId: ${ep.operationId}\n`;
        if (ep.summary) yaml += `      summary: "${esc(ep.summary)}"\n`;
        if (ep.description) yaml += `      description: "${esc(ep.description)}"\n`;
        if (ep.tags.length > 0) {
          yaml += `      tags:\n`;
          for (const tag of ep.tags) yaml += `        - ${tag}\n`;
        }
        if (ep.deprecated) yaml += `      deprecated: true\n`;

        if (ep.parameters.length > 0) {
          yaml += `      parameters:\n`;
          for (const p of ep.parameters) {
            yaml += `        - name: ${p.name}\n`;
            yaml += `          in: ${p.in}\n`;
            if (p.required) yaml += `          required: true\n`;
            if (p.description) yaml += `          description: "${esc(p.description)}"\n`;
            if (p.type) {
              yaml += `          schema:\n            type: ${p.type}\n`;
              // Emit constraints when present
              const c = p.constraints;
              if (c) {
                if (c.format) yaml += `            format: ${c.format}\n`;
                if (c.minLength !== undefined) yaml += `            minLength: ${c.minLength}\n`;
                if (c.maxLength !== undefined) yaml += `            maxLength: ${c.maxLength}\n`;
                if (c.minimum !== undefined) yaml += `            minimum: ${c.minimum}\n`;
                if (c.maximum !== undefined) yaml += `            maximum: ${c.maximum}\n`;
                if (c.pattern) yaml += `            pattern: "${esc(c.pattern)}"\n`;
                if (c.defaultValue !== undefined) yaml += `            default: ${c.defaultValue}\n`;
                if (c.readOnly) yaml += `            readOnly: true\n`;
                if (c.writeOnly) yaml += `            writeOnly: true\n`;
                if (c.nullable) yaml += `            nullable: true\n`;
                if (c.enumValues && c.enumValues.length > 0) {
                  yaml += `            enum:\n`;
                  for (const v of c.enumValues) yaml += `              - ${v}\n`;
                }
              }
            }
          }
        }

        if (ep.requestBody) {
          yaml += `      requestBody:\n`;
          if (ep.requestBody.required) yaml += `        required: true\n`;
          yaml += `        content:\n`;
          yaml += `          ${ep.requestBody.contentType || 'application/json'}:\n`;
          if (ep.requestBody.schema) {
            yaml += `            schema:\n              $ref: "${esc(ep.requestBody.schema)}"\n`;
          } else if (ep.requestBody.properties && ep.requestBody.properties.length > 0) {
            yaml += `            schema:\n`;
            yaml += emitInlineSchema(ep.requestBody.properties as SchemaProperty[], 14);
          }
        }

        if (ep.responses.length > 0) {
          yaml += `      responses:\n`;
          for (const r of ep.responses) {
            yaml += `        "${r.statusCode}":\n`;
            yaml += `          description: "${esc(r.description || 'Response')}"\n`;
            if (r.schema) {
              yaml += `          content:\n`;
              yaml += `            ${r.contentType || 'application/json'}:\n`;
              yaml += `              schema:\n                $ref: "${esc(r.schema)}"\n`;
            } else if (r.properties && r.properties.length > 0) {
              yaml += `          content:\n`;
              yaml += `            ${r.contentType || 'application/json'}:\n`;
              yaml += `              schema:\n`;
              yaml += emitInlineSchema(r.properties as SchemaProperty[], 16);
            }
          }
        } else {
          yaml += `      responses:\n        "200":\n          description: OK\n`;
        }

        if (ep.authScopes.length > 0) {
          yaml += `      security:\n        - oauth2: [${ep.authScopes.join(', ')}]\n`;
        }

        if (ep.rateLimit || ep.idempotencyKey || ep.observabilityNotes) {
          yaml += `      x-nto-metadata:\n`;
          if (ep.rateLimit) yaml += `        rateLimit: "${esc(ep.rateLimit)}"\n`;
          if (ep.idempotencyKey) yaml += `        idempotencyKey: "${esc(ep.idempotencyKey)}"\n`;
          if (ep.observabilityNotes) yaml += `        observability: "${esc(ep.observabilityNotes)}"\n`;
          if (ep.deprecationNote) yaml += `        deprecationNote: "${esc(String(ep.deprecationNote))}"\n`;
        }
      }
    }
  }

  return { success: true, content: yaml, warnings, unsupportedFeatures: unsupported };
}

// ── Inline Schema Emitter (SchemaProperty[] → OpenAPI YAML) ──────────────────

function emitInlineSchema(properties: SchemaProperty[], baseIndent: number): string {
  const indent = (n: number) => ' '.repeat(n);
  let yaml = '';
  yaml += `${indent(baseIndent)}type: object\n`;

  const requiredProps = properties.filter((p) => p.required);
  if (requiredProps.length > 0) {
    yaml += `${indent(baseIndent)}required:\n`;
    for (const p of requiredProps) {
      yaml += `${indent(baseIndent + 2)}- ${p.name}\n`;
    }
  }

  yaml += `${indent(baseIndent)}properties:\n`;
  for (const prop of properties) {
    yaml += emitSchemaPropertyYaml(prop, baseIndent + 2);
  }
  return yaml;
}

function emitSchemaPropertyYaml(prop: SchemaProperty, indentLevel: number): string {
  const indent = (n: number) => ' '.repeat(n);
  let yaml = '';

  if (!prop.name) return yaml;

  yaml += `${indent(indentLevel)}${prop.name}:\n`;

  if (prop.type === '$ref' && prop.$ref) {
    yaml += `${indent(indentLevel + 2)}$ref: "${esc(prop.$ref)}"\n`;
    return yaml;
  }

  yaml += `${indent(indentLevel + 2)}type: ${prop.type}\n`;
  if (prop.description) yaml += `${indent(indentLevel + 2)}description: "${esc(prop.description)}"\n`;

  // Constraints
  const c = prop.constraints;
  if (c) {
    if (c.format) yaml += `${indent(indentLevel + 2)}format: ${c.format}\n`;
    if (c.minLength !== undefined) yaml += `${indent(indentLevel + 2)}minLength: ${c.minLength}\n`;
    if (c.maxLength !== undefined) yaml += `${indent(indentLevel + 2)}maxLength: ${c.maxLength}\n`;
    if (c.minimum !== undefined) yaml += `${indent(indentLevel + 2)}minimum: ${c.minimum}\n`;
    if (c.maximum !== undefined) yaml += `${indent(indentLevel + 2)}maximum: ${c.maximum}\n`;
    if (c.exclusiveMinimum) yaml += `${indent(indentLevel + 2)}exclusiveMinimum: true\n`;
    if (c.exclusiveMaximum) yaml += `${indent(indentLevel + 2)}exclusiveMaximum: true\n`;
    if (c.pattern) yaml += `${indent(indentLevel + 2)}pattern: "${esc(c.pattern)}"\n`;
    if (c.defaultValue !== undefined) yaml += `${indent(indentLevel + 2)}default: ${c.defaultValue}\n`;
    if (c.readOnly) yaml += `${indent(indentLevel + 2)}readOnly: true\n`;
    if (c.writeOnly) yaml += `${indent(indentLevel + 2)}writeOnly: true\n`;
    if (c.nullable) yaml += `${indent(indentLevel + 2)}nullable: true\n`;
    if (c.example !== undefined) yaml += `${indent(indentLevel + 2)}example: ${c.example}\n`;
    if (c.enumValues && c.enumValues.length > 0) {
      yaml += `${indent(indentLevel + 2)}enum:\n`;
      for (const v of c.enumValues) yaml += `${indent(indentLevel + 4)}- ${v}\n`;
    }
  }

  // Nested object properties
  if (prop.type === 'object' && prop.properties && prop.properties.length > 0) {
    const requiredChildren = prop.properties.filter((p) => p.required);
    if (requiredChildren.length > 0) {
      yaml += `${indent(indentLevel + 2)}required:\n`;
      for (const p of requiredChildren) {
        yaml += `${indent(indentLevel + 4)}- ${p.name}\n`;
      }
    }
    yaml += `${indent(indentLevel + 2)}properties:\n`;
    for (const child of prop.properties) {
      yaml += emitSchemaPropertyYaml(child, indentLevel + 4);
    }
  }

  // Array items
  if (prop.type === 'array' && prop.items) {
    yaml += `${indent(indentLevel + 2)}items:\n`;
    if (prop.items.type === '$ref' && prop.items.$ref) {
      yaml += `${indent(indentLevel + 4)}$ref: "${esc(prop.items.$ref)}"\n`;
    } else if (prop.items.type === 'object' && prop.items.properties && prop.items.properties.length > 0) {
      yaml += `${indent(indentLevel + 4)}type: object\n`;
      const reqItems = prop.items.properties.filter((p) => p.required);
      if (reqItems.length > 0) {
        yaml += `${indent(indentLevel + 4)}required:\n`;
        for (const p of reqItems) yaml += `${indent(indentLevel + 6)}- ${p.name}\n`;
      }
      yaml += `${indent(indentLevel + 4)}properties:\n`;
      for (const child of prop.items.properties) {
        yaml += emitSchemaPropertyYaml(child, indentLevel + 6);
      }
    } else {
      yaml += `${indent(indentLevel + 4)}type: ${prop.items.type}\n`;
      const ic = prop.items.constraints;
      if (ic) {
        if (ic.format) yaml += `${indent(indentLevel + 4)}format: ${ic.format}\n`;
        if (ic.minLength !== undefined) yaml += `${indent(indentLevel + 4)}minLength: ${ic.minLength}\n`;
        if (ic.maxLength !== undefined) yaml += `${indent(indentLevel + 4)}maxLength: ${ic.maxLength}\n`;
        if (ic.minimum !== undefined) yaml += `${indent(indentLevel + 4)}minimum: ${ic.minimum}\n`;
        if (ic.maximum !== undefined) yaml += `${indent(indentLevel + 4)}maximum: ${ic.maximum}\n`;
      }
    }
  }

  return yaml;
}

// ── SOAP → WSDL-like XML ─────────────────────────────────────────────────────

export function soapBuilderToXml(state: SoapBuilderState): SyncResult {
  const warnings = ['contracts.builder.sync.soapPartialRoundtrip'];
  const unsupported: string[] = [];

  let xml = `<?xml version="1.0" encoding="UTF-8"?>\n`;
  xml += `<definitions\n`;
  xml += `  xmlns="http://schemas.xmlsoap.org/wsdl/"\n`;
  xml += `  xmlns:soap="${state.binding === 'SOAP 1.2' ? 'http://schemas.xmlsoap.org/wsdl/soap12/' : 'http://schemas.xmlsoap.org/wsdl/soap/'}"\n`;
  xml += `  xmlns:tns="${esc(state.targetNamespace)}"\n`;
  xml += `  targetNamespace="${esc(state.targetNamespace)}"\n`;
  xml += `  name="${esc(state.serviceName || 'Service')}">\n\n`;

  if (state.description) {
    xml += `  <documentation>${esc(state.description)}</documentation>\n\n`;
  }

  for (const op of state.operations) {
    if (op.inputMessage) {
      xml += `  <message name="${esc(op.inputMessage)}">\n`;
      xml += `    <part name="parameters" element="tns:${esc(op.inputMessage)}" />\n`;
      xml += `  </message>\n`;
    }
    if (op.outputMessage) {
      xml += `  <message name="${esc(op.outputMessage)}">\n`;
      xml += `    <part name="parameters" element="tns:${esc(op.outputMessage)}" />\n`;
      xml += `  </message>\n`;
    }
    if (op.faultMessage) {
      xml += `  <message name="${esc(op.faultMessage)}">\n`;
      xml += `    <part name="fault" element="tns:${esc(op.faultMessage)}" />\n`;
      xml += `  </message>\n`;
    }
  }

  xml += `\n  <portType name="${esc(state.serviceName || 'Service')}PortType">\n`;
  for (const op of state.operations) {
    xml += `    <operation name="${esc(op.name)}">\n`;
    if (op.description) xml += `      <documentation>${esc(op.description)}</documentation>\n`;
    if (op.inputMessage) xml += `      <input message="tns:${esc(op.inputMessage)}" />\n`;
    if (op.outputMessage) xml += `      <output message="tns:${esc(op.outputMessage)}" />\n`;
    if (op.faultMessage) xml += `      <fault name="fault" message="tns:${esc(op.faultMessage)}" />\n`;
    xml += `    </operation>\n`;
  }
  xml += `  </portType>\n\n`;

  xml += `  <binding name="${esc(state.serviceName || 'Service')}Binding" type="tns:${esc(state.serviceName || 'Service')}PortType">\n`;
  xml += `    <soap:binding style="document" transport="http://schemas.xmlsoap.org/soap/http" />\n`;
  for (const op of state.operations) {
    xml += `    <operation name="${esc(op.name)}">\n`;
    if (op.soapAction) xml += `      <soap:operation soapAction="${esc(op.soapAction)}" />\n`;
    xml += `      <input><soap:body use="literal" /></input>\n`;
    xml += `      <output><soap:body use="literal" /></output>\n`;
    xml += `    </operation>\n`;
  }
  xml += `  </binding>\n\n`;

  xml += `  <service name="${esc(state.serviceName || 'Service')}">\n`;
  xml += `    <port name="${esc(state.serviceName || 'Service')}Port" binding="tns:${esc(state.serviceName || 'Service')}Binding">\n`;
  xml += `      <soap:address location="${esc(state.endpoint || 'http://localhost:8080/service')}" />\n`;
  xml += `    </port>\n`;
  xml += `  </service>\n`;
  xml += `</definitions>\n`;

  return { success: true, content: xml, warnings, unsupportedFeatures: unsupported };
}

// ── Event → AsyncAPI YAML ─────────────────────────────────────────────────────

export function eventBuilderToYaml(state: EventBuilderState): SyncResult {
  const warnings: string[] = [];
  const unsupported: string[] = [];

  let yaml = `asyncapi: "2.6.0"\n`;
  yaml += `info:\n`;
  yaml += `  title: "${esc(state.title || 'Untitled Event API')}"\n`;
  yaml += `  version: "${esc(state.version || '1.0.0')}"\n`;
  if (state.description) yaml += `  description: "${esc(state.description)}"\n`;

  if (state.defaultBroker) {
    yaml += `servers:\n`;
    yaml += `  default:\n`;
    yaml += `    url: "${esc(state.defaultBroker)}"\n`;
    yaml += `    protocol: kafka\n`;
  }

  yaml += `channels:\n`;

  if (state.channels.length === 0) {
    yaml += `  {} # No channels defined\n`;
  } else {
    for (const ch of state.channels) {
      yaml += `  ${ch.topicName || 'unnamed-topic'}:\n`;
      if (ch.description) yaml += `    description: "${esc(ch.description)}"\n`;

      if (ch.producer) {
        yaml += `    publish:\n`;
        yaml += `      operationId: publish${capitalize(ch.eventName || ch.topicName || 'event')}\n`;
        if (ch.payloadSchema) {
          yaml += `      message:\n`;
          yaml += `        name: ${ch.eventName || 'Event'}\n`;
          yaml += `        payload:\n          $ref: "${esc(ch.payloadSchema)}"\n`;
        }
      }

      if (ch.consumer) {
        yaml += `    subscribe:\n`;
        yaml += `      operationId: consume${capitalize(ch.eventName || ch.topicName || 'event')}\n`;
        if (ch.payloadSchema && !ch.producer) {
          yaml += `      message:\n`;
          yaml += `        name: ${ch.eventName || 'Event'}\n`;
          yaml += `        payload:\n          $ref: "${esc(ch.payloadSchema)}"\n`;
        }
      }

      yaml += `    x-nto-metadata:\n`;
      if (ch.owner) yaml += `      owner: "${esc(ch.owner)}"\n`;
      if (ch.compatibility) yaml += `      compatibility: ${ch.compatibility}\n`;
      if (ch.retention) yaml += `      retention: "${esc(ch.retention)}"\n`;
      if (ch.partitions) yaml += `      partitions: ${ch.partitions}\n`;
      if (ch.ordering) yaml += `      ordering: "${esc(ch.ordering)}"\n`;
      if (ch.retries) yaml += `      retries: ${ch.retries}\n`;
      if (ch.dlq) yaml += `      dlq: "${esc(ch.dlq)}"\n`;
      if (ch.idempotent) yaml += `      idempotent: true\n`;
      if (ch.observabilityNotes) yaml += `      observability: "${esc(ch.observabilityNotes)}"\n`;
    }
  }

  return { success: true, content: yaml, warnings, unsupportedFeatures: unsupported };
}

// ── Workservice → NTO YAML ────────────────────────────────────────────────────

export function workserviceBuilderToYaml(state: WorkserviceBuilderState): SyncResult {
  const warnings: string[] = [];
  const unsupported: string[] = [];

  let yaml = `# NexTraceOne Workservice Definition\n`;
  yaml += `kind: BackgroundService\n`;
  yaml += `metadata:\n`;
  yaml += `  name: "${esc(state.name || 'unnamed-service')}"\n`;
  if (state.owner) yaml += `  owner: "${esc(state.owner)}"\n`;
  if (state.description) yaml += `  description: "${esc(state.description)}"\n`;
  yaml += `\n`;

  yaml += `trigger:\n`;
  yaml += `  type: ${state.trigger}\n`;
  if (state.schedule) yaml += `  schedule: "${esc(state.schedule)}"\n`;
  yaml += `\n`;

  yaml += `behavior:\n`;
  yaml += `  retries: ${state.retries || '3'}\n`;
  yaml += `  timeout: "${esc(state.timeout || '300')}s"\n`;
  if (state.errorHandling) yaml += `  errorHandling: "${esc(state.errorHandling)}"\n`;
  yaml += `\n`;

  if (state.inputs || state.outputs) {
    yaml += `io:\n`;
    if (state.inputs) yaml += `  inputs: "${esc(state.inputs)}"\n`;
    if (state.outputs) yaml += `  outputs: "${esc(state.outputs)}"\n`;
    yaml += `\n`;
  }

  if (state.dependencies.length > 0) {
    yaml += `dependencies:\n`;
    for (const dep of state.dependencies) {
      yaml += `  - name: "${esc(dep.name)}"\n`;
      yaml += `    type: ${dep.type}\n`;
      if (dep.required) yaml += `    required: true\n`;
    }
    yaml += `\n`;
  }

  if (state.sideEffects) yaml += `sideEffects: "${esc(state.sideEffects)}"\n`;
  if (state.healthCheck) yaml += `healthCheck: "${esc(state.healthCheck)}"\n`;

  // ── Messaging Role ────────────────────────────────────────────────────────
  if (state.messagingRole && state.messagingRole !== 'None') {
    yaml += `\nmessaging:\n`;
    yaml += `  role: ${state.messagingRole}\n`;

    if ((state.messagingRole === 'Consumer' || state.messagingRole === 'Both') && state.consumedTopics.length > 0) {
      yaml += `  consumedTopics:\n`;
      for (const t of state.consumedTopics) {
        yaml += `    - topicName: "${esc(t.topicName)}"\n`;
        if (t.entityType) yaml += `      entityType: "${esc(t.entityType)}"\n`;
        if (t.format) yaml += `      format: ${t.format}\n`;
      }
    }

    if ((state.messagingRole === 'Producer' || state.messagingRole === 'Both') && state.producedTopics.length > 0) {
      yaml += `  producedTopics:\n`;
      for (const t of state.producedTopics) {
        yaml += `    - topicName: "${esc(t.topicName)}"\n`;
        if (t.entityType) yaml += `      entityType: "${esc(t.entityType)}"\n`;
        if (t.format) yaml += `      format: ${t.format}\n`;
      }
    }

    if ((state.messagingRole === 'Consumer' || state.messagingRole === 'Both') && state.consumedServices.length > 0) {
      yaml += `  consumedServices:\n`;
      for (const s of state.consumedServices) {
        yaml += `    - serviceName: "${esc(s.serviceName)}"\n`;
        if (s.protocol) yaml += `      protocol: ${s.protocol}\n`;
      }
    }

    if ((state.messagingRole === 'Producer' || state.messagingRole === 'Both') && state.producedEvents.length > 0) {
      yaml += `  producedEvents:\n`;
      for (const e of state.producedEvents) {
        yaml += `    - eventName: "${esc(e.eventName)}"\n`;
        if (e.targetTopic) yaml += `      targetTopic: "${esc(e.targetTopic)}"\n`;
      }
    }
  }

  if (state.observabilityNotes) {
    yaml += `\nobservability:\n`;
    yaml += `  notes: "${esc(state.observabilityNotes)}"\n`;
  }

  return { success: true, content: yaml, warnings, unsupportedFeatures: unsupported };
}

// ── Helpers ───────────────────────────────────────────────────────────────────

function esc(str: string): string {
  return str.replace(/\\/g, '\\\\').replace(/"/g, '\\"').replace(/\n/g, '\\n');
}

function capitalize(s: string): string {
  return s.charAt(0).toUpperCase() + s.slice(1);
}

function groupByPath(endpoints: RestEndpointShape[]): Record<string, RestEndpointShape[]> {
  const map: Record<string, RestEndpointShape[]> = {};
  for (const ep of endpoints) {
    const bucket = map[ep.path] ?? [];
    bucket.push(ep);
    map[ep.path] = bucket;
  }
  return map;
}

// ── SharedSchema → JSON Schema ────────────────────────────────────────────────

function emitSchemaProperty(prop: SharedSchemaProperty, indent: string): string {
  let out = '';
  if (prop.type === '$ref' && prop.$ref) {
    out += `${indent}"${esc(prop.name)}": { "$ref": "${esc(prop.$ref)}" }`;
  } else if (prop.type === 'object' && prop.properties && prop.properties.length > 0) {
    out += `${indent}"${esc(prop.name)}": {\n`;
    out += `${indent}  "type": "object",\n`;
    if (prop.description) out += `${indent}  "description": "${esc(prop.description)}",\n`;
    out += `${indent}  "properties": {\n`;
    const childProps = prop.properties.map((p) => emitSchemaProperty(p, indent + '    '));
    out += childProps.join(',\n') + '\n';
    out += `${indent}  }\n`;
    out += `${indent}}`;
  } else if (prop.type === 'array' && prop.items) {
    out += `${indent}"${esc(prop.name)}": {\n`;
    out += `${indent}  "type": "array",\n`;
    if (prop.description) out += `${indent}  "description": "${esc(prop.description)}",\n`;
    out += `${indent}  "items": { "type": "${esc(prop.items.type)}" }\n`;
    out += `${indent}}`;
  } else {
    out += `${indent}"${esc(prop.name)}": {\n`;
    out += `${indent}  "type": "${esc(prop.type)}"`;
    if (prop.description) out += `,\n${indent}  "description": "${esc(prop.description)}"`;
    const c = prop.constraints;
    if (c) {
      if (c.format) out += `,\n${indent}  "format": "${esc(c.format)}"`;
      if (c.minLength !== undefined) out += `,\n${indent}  "minLength": ${c.minLength}`;
      if (c.maxLength !== undefined) out += `,\n${indent}  "maxLength": ${c.maxLength}`;
      if (c.minimum !== undefined) out += `,\n${indent}  "minimum": ${c.minimum}`;
      if (c.maximum !== undefined) out += `,\n${indent}  "maximum": ${c.maximum}`;
      if (c.pattern) out += `,\n${indent}  "pattern": "${esc(c.pattern)}"`;
      if (c.defaultValue !== undefined) out += `,\n${indent}  "default": "${esc(c.defaultValue)}"`;
      if (c.enumValues && c.enumValues.length > 0) {
        out += `,\n${indent}  "enum": [${c.enumValues.map((v) => `"${esc(v)}"`).join(', ')}]`;
      }
    }
    out += `\n${indent}}`;
  }
  return out;
}

export function sharedSchemaBuilderToJson(state: SharedSchemaBuilderState): SyncResult {
  const warnings: string[] = [];
  const unsupported: string[] = [];

  if (state.format !== 'json-schema') {
    warnings.push('contracts.builder.sync.schemaFormatLimitation');
  }

  let json = '{\n';
  json += `  "$schema": "https://json-schema.org/draft/2020-12/schema",\n`;
  json += `  "$id": "${esc(state.namespace ? state.namespace + '/' + state.name : state.name)}",\n`;
  json += `  "title": "${esc(state.name)}",\n`;
  json += `  "description": "${esc(state.description)}",\n`;
  json += `  "type": "object",\n`;

  json += `  "x-nto-metadata": {\n`;
  json += `    "version": "${esc(state.version)}",\n`;
  json += `    "namespace": "${esc(state.namespace)}",\n`;
  json += `    "format": "${esc(state.format)}",\n`;
  json += `    "compatibility": "${esc(state.compatibility)}",\n`;
  json += `    "owner": "${esc(state.owner)}"`;
  if (state.tags.length > 0) {
    json += `,\n    "tags": [${state.tags.map((t) => `"${esc(t)}"`).join(', ')}]`;
  }
  json += `\n  },\n`;

  json += `  "properties": {\n`;
  const props = state.properties.map((p) => emitSchemaProperty(p, '    '));
  json += props.join(',\n') + '\n';
  json += `  }`;

  const required = state.properties.filter((p) => p.required).map((p) => p.name);
  if (required.length > 0) {
    json += `,\n  "required": [${required.map((r) => `"${esc(r)}"`).join(', ')}]`;
  }

  json += '\n}\n';

  return { success: true, content: json, warnings, unsupportedFeatures: unsupported };
}

// ── Webhook → NTO YAML ────────────────────────────────────────────────────────

export function webhookBuilderToYaml(state: WebhookBuilderState): SyncResult {
  const warnings: string[] = [];
  const unsupported: string[] = [];

  let yaml = `# NexTraceOne Webhook Definition\n`;
  yaml += `kind: Webhook\n`;
  yaml += `metadata:\n`;
  yaml += `  name: "${esc(state.name || 'unnamed-webhook')}"\n`;
  if (state.owner) yaml += `  owner: "${esc(state.owner)}"\n`;
  if (state.description) yaml += `  description: "${esc(state.description)}"\n`;
  yaml += `\n`;

  yaml += `spec:\n`;
  yaml += `  method: ${state.method}\n`;
  yaml += `  urlPattern: "${esc(state.urlPattern)}"\n`;
  yaml += `  contentType: "${esc(state.contentType || 'application/json')}"\n`;
  yaml += `\n`;

  yaml += `  authentication:\n`;
  yaml += `    type: ${state.authentication}\n`;
  if (state.secretHeaderName) yaml += `    secretHeaderName: "${esc(state.secretHeaderName)}"\n`;
  yaml += `\n`;

  if (state.headers.length > 0) {
    yaml += `  headers:\n`;
    for (const h of state.headers) {
      yaml += `    - name: "${esc(h.name)}"\n`;
      yaml += `      value: "${esc(h.value)}"\n`;
      if (h.required) yaml += `      required: true\n`;
    }
    yaml += `\n`;
  }

  if (state.payloadSchema) {
    yaml += `  payloadSchema: |\n`;
    for (const line of state.payloadSchema.split('\n')) {
      yaml += `    ${line}\n`;
    }
    yaml += `\n`;
  }

  yaml += `  retry:\n`;
  yaml += `    count: ${state.retryCount || '3'}\n`;
  yaml += `    timeout: "${esc(state.timeout || '30')}s"\n`;
  if (state.retryPolicy) yaml += `    policy: "${esc(state.retryPolicy)}"\n`;
  yaml += `\n`;

  if (state.events.length > 0) {
    yaml += `  events:\n`;
    for (const ev of state.events) {
      yaml += `    - ${ev}\n`;
    }
    yaml += `\n`;
  }

  if (state.observabilityNotes) {
    yaml += `observability:\n`;
    yaml += `  notes: "${esc(state.observabilityNotes)}"\n`;
  }

  return { success: true, content: yaml, warnings, unsupportedFeatures: unsupported };
}

// ── Legacy Contract → NTO YAML ────────────────────────────────────────────────

export function legacyContractBuilderToYaml(state: LegacyContractBuilderState): SyncResult {
  const warnings: string[] = [];
  const unsupported: string[] = [];

  let yaml = `# NexTraceOne Legacy Contract Definition\n`;
  yaml += `kind: ${state.kind}\n`;
  yaml += `metadata:\n`;
  yaml += `  name: "${esc(state.name || 'unnamed-contract')}"\n`;
  if (state.version) yaml += `  version: "${esc(state.version)}"\n`;
  if (state.owner) yaml += `  owner: "${esc(state.owner)}"\n`;
  if (state.description) yaml += `  description: "${esc(state.description)}"\n`;
  yaml += `\n`;

  yaml += `spec:\n`;
  yaml += `  encoding: ${state.encoding}\n`;
  if (state.totalLength) yaml += `  totalLength: ${state.totalLength}\n`;

  if (state.kind === 'Copybook' && state.programName) {
    yaml += `  programName: "${esc(state.programName)}"\n`;
  }
  if (state.kind === 'MqMessage') {
    if (state.queueManager) yaml += `  queueManager: "${esc(state.queueManager)}"\n`;
    if (state.queueName) yaml += `  queueName: "${esc(state.queueName)}"\n`;
    if (state.messageFormat) yaml += `  messageFormat: "${esc(state.messageFormat)}"\n`;
  }
  if (state.kind === 'CicsCommarea') {
    if (state.transactionId) yaml += `  transactionId: "${esc(state.transactionId)}"\n`;
    if (state.commareaLength) yaml += `  commareaLength: ${state.commareaLength}\n`;
  }
  yaml += `\n`;

  if (state.fields.length > 0) {
    yaml += `fields:\n`;
    for (const f of state.fields) {
      yaml += `  - name: "${esc(f.name)}"\n`;
      if (f.level) yaml += `    level: ${f.level}\n`;
      yaml += `    type: ${f.type}\n`;
      if (f.length) yaml += `    length: ${f.length}\n`;
      if (f.offset) yaml += `    offset: ${f.offset}\n`;
      if (f.picture) yaml += `    picture: "${esc(f.picture)}"\n`;
      if (f.description) yaml += `    description: "${esc(f.description)}"\n`;
      if (f.occurs) yaml += `    occurs: ${f.occurs}\n`;
      if (f.redefines) yaml += `    redefines: "${esc(f.redefines)}"\n`;
    }
    yaml += `\n`;
  }

  if (state.observabilityNotes) {
    yaml += `observability:\n`;
    yaml += `  notes: "${esc(state.observabilityNotes)}"\n`;
  }

  return { success: true, content: yaml, warnings, unsupportedFeatures: unsupported };
}
