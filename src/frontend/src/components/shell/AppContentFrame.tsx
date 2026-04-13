import type { ReactNode } from 'react';
import { cn } from '../../lib/cn';

interface AppContentFrameProps {
  children: ReactNode;
  className?: string;
}

export function AppContentFrame({ children, className }: AppContentFrameProps) {
  return (
    <main
      className={cn('h-full overflow-y-auto overflow-x-hidden', className)}
      id="main-content"
      role="main"
    >
      {children}
    </main>
  );
}
