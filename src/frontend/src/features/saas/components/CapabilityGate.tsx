import type { ReactNode } from 'react';
import { useCapability } from '../hooks/useCapability';

interface CapabilityGateProps {
  capability: string;
  fallback?: ReactNode;
  children: ReactNode;
}

export function CapabilityGate({ capability, fallback = null, children }: CapabilityGateProps) {
  const hasCapability = useCapability(capability);
  return hasCapability ? <>{children}</> : <>{fallback}</>;
}
