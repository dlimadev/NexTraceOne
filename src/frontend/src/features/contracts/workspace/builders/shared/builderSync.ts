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
  SyncResult,
} from './builderTypes';

interface RestEndpointParameter {
  name: string;
  in: string;
  required?: boolean;
  type?: string;
  description?: string;
}

interface RestEndpointRequestBody {
  required?: boolean;
  contentType?: string;
  schema?: string;
}

interface RestEndpointResponse {
  statusCode: string | number;
  description?: string;
  contentType?: string;
  schema?: string;
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
            if (p.type) yaml += `          schema:\n            type: ${p.type}\n`;
            if (p.description) yaml += `          description: "${esc(p.description)}"\n`;
          }
        }

        if (ep.requestBody) {
          yaml += `      requestBody:\n`;
          if (ep.requestBody.required) yaml += `        required: true\n`;
          yaml += `        content:\n`;
          yaml += `          ${ep.requestBody.contentType || 'application/json'}:\n`;
          if (ep.requestBody.schema) {
            yaml += `            schema:\n              $ref: "${esc(ep.requestBody.schema)}"\n`;
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
