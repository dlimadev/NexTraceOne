/**
 * Gera um operationId sugerido a partir do método HTTP e path.
 * Exemplos: POST /users → createUser, GET /users/{id} → getUserById
 */
export function generateOperationId(method: string, path: string): string {
  const m = method.toUpperCase();

  // Extract last meaningful path segment (ignore trailing slash)
  const segments = path.replace(/\/$/, '').split('/').filter(Boolean);
  const last = segments[segments.length - 1] ?? 'resource';
  const hasIdParam = last.startsWith('{') && last.endsWith('}');
  const resourceSegment = hasIdParam
    ? segments[segments.length - 2] ?? 'resource'
    : last;

  // Capitalise the resource name, removing braces if present
  const resource = resourceSegment.replace(/[{}]/g, '');
  const capitalised = resource.charAt(0).toUpperCase() + resource.slice(1);

  const suffix = hasIdParam ? 'ById' : '';

  switch (m) {
    case 'GET': return hasIdParam ? `get${capitalised}ById` : `list${capitalised}`;
    case 'POST': return `create${capitalised}`;
    case 'PUT': return `update${capitalised}${suffix}`;
    case 'PATCH': return `patch${capitalised}${suffix}`;
    case 'DELETE': return `delete${capitalised}${suffix}`;
    case 'HEAD': return `head${capitalised}${suffix}`;
    case 'OPTIONS': return `options${capitalised}${suffix}`;
    default: return `${m.toLowerCase()}${capitalised}${suffix}`;
  }
}
