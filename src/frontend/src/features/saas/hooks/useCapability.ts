import { useMemo } from 'react';
import { getAccessToken } from '../../../utils/tokenStorage';

function parseJwtPayload(token: string): Record<string, unknown> | null {
  try {
    const parts = token.split('.');
    if (parts.length !== 3) return null;
    const payload = parts[1].replace(/-/g, '+').replace(/_/g, '/');
    const padded = payload + '==='.slice((payload.length + 3) % 4 === 0 ? 3 : (payload.length + 3) % 4);
    return JSON.parse(atob(padded)) as Record<string, unknown>;
  } catch {
    return null;
  }
}

function getCapabilitiesFromToken(): string[] {
  const token = getAccessToken();
  if (!token) return [];
  const payload = parseJwtPayload(token);
  if (!payload) return [];
  const caps = payload['capabilities'];
  if (Array.isArray(caps)) return caps.filter((c): c is string => typeof c === 'string');
  if (typeof caps === 'string') return [caps];
  return [];
}

export function useCapabilities(): string[] {
  return useMemo(() => getCapabilitiesFromToken(), []);
}

export function useCapability(capability: string): boolean {
  const caps = useCapabilities();
  return caps.includes(capability);
}
