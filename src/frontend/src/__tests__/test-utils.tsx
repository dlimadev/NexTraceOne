import { type ReactElement, type ReactNode } from 'react';
import { render, type RenderOptions, type RenderResult } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter, type MemoryRouterProps } from 'react-router-dom';
import { I18nextProvider } from 'react-i18next';
import i18n from '../i18n';
import { ToastProvider } from '../components/Toast';
import { ThemeProvider } from '../contexts/ThemeContext';

/**
 * Options for the custom render function.
 * Extends @testing-library/react RenderOptions with additional provider config.
 */
interface CustomRenderOptions extends Omit<RenderOptions, 'wrapper'> {
  /** Initial route entries for MemoryRouter (default: ['/']) */
  routerProps?: MemoryRouterProps;
  /** Custom QueryClient instance (auto-created with retry disabled if omitted) */
  queryClient?: QueryClient;
}

/**
 * Creates a fresh QueryClient configured for tests (no retries, no refetch on mount).
 */
function createTestQueryClient(): QueryClient {
  return new QueryClient({
    defaultOptions: {
      queries: {
        retry: false,
        gcTime: Infinity,
      },
      mutations: {
        retry: false,
      },
    },
  });
}

/**
 * Wrapper component that provides all necessary providers for component testing.
 */
// eslint-disable-next-line react-refresh/only-export-components
function AllProviders({
  children,
  queryClient,
  routerProps,
}: {
  children: ReactNode;
  queryClient: QueryClient;
  routerProps?: MemoryRouterProps;
}) {
  return (
    <QueryClientProvider client={queryClient}>
      <ThemeProvider>
        <I18nextProvider i18n={i18n}>
          <ToastProvider>
            <MemoryRouter {...routerProps}>
              {children}
            </MemoryRouter>
          </ToastProvider>
        </I18nextProvider>
      </ThemeProvider>
    </QueryClientProvider>
  );
}

/**
 * Custom render function that wraps the component with all necessary providers:
 * - QueryClientProvider (TanStack Query)
 * - ThemeProvider (theme context)
 * - I18nextProvider (i18n translations)
 * - ToastProvider (toast notifications)
 * - MemoryRouter (React Router)
 *
 * @example
 * ```tsx
 * import { renderWithProviders, screen } from '../test-utils';
 *
 * test('renders page title', () => {
 *   renderWithProviders(<MyPage />);
 *   expect(screen.getByText('Title')).toBeInTheDocument();
 * });
 * ```
 */
export function renderWithProviders(
  ui: ReactElement,
  options: CustomRenderOptions = {},
): RenderResult & { queryClient: QueryClient } {
  const { queryClient = createTestQueryClient(), routerProps, ...renderOptions } = options;

  const result = render(ui, {
    wrapper: ({ children }) => (
      <AllProviders queryClient={queryClient} routerProps={routerProps}>
        {children}
      </AllProviders>
    ),
    ...renderOptions,
  });

  return { ...result, queryClient };
}

// Re-export everything from @testing-library/react for convenience
// eslint-disable-next-line react-refresh/only-export-components
export * from '@testing-library/react';
// Override the default render with our custom one
export { renderWithProviders as render };
