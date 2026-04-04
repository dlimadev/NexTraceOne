import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';
import { AiIntegrationsConfigurationPage } from '../../features/ai-hub/pages/AiIntegrationsConfigurationPage';

vi.mock('../../features/configuration/hooks/useConfiguration', () => ({
  useConfigurationDefinitions: vi.fn(),
  useEffectiveSettings: vi.fn(),
  useSetConfigurationValue: vi.fn(),
  useAuditHistory: vi.fn(),
}));

import {
  useConfigurationDefinitions,
  useEffectiveSettings,
  useSetConfigurationValue,
  useAuditHistory,
} from '../../features/configuration/hooks/useConfiguration';

const mockDefinitions = [
  {
    key: 'ai.providers.enabled',
    displayName: 'Enabled AI Providers',
    description: 'List of AI providers enabled',
    category: 'Functional',
    allowedScopes: ['System', 'Tenant'],
    defaultValue: '["OpenAI","AzureOpenAI","Internal"]',
    valueType: 'Json',
    isSensitive: false,
    isEditable: true,
    isInheritable: true,
    validationRules: null,
    uiEditorType: 'json-editor',
    sortOrder: 6000,
  },
  {
    key: 'ai.providers.fallback_order',
    displayName: 'Provider Fallback Order',
    description: 'Ordered fallback list of providers',
    category: 'Functional',
    allowedScopes: ['System', 'Tenant'],
    defaultValue: '["AzureOpenAI","OpenAI","Internal"]',
    valueType: 'Json',
    isSensitive: false,
    isEditable: true,
    isInheritable: true,
    validationRules: null,
    uiEditorType: 'json-editor',
    sortOrder: 6040,
  },
  {
    key: 'ai.budget.by_user',
    displayName: 'AI Token Budget by User',
    description: 'Default monthly token budget per user',
    category: 'Functional',
    allowedScopes: ['System', 'Tenant'],
    defaultValue: '{"monthlyTokens":100000,"alertOnExceed":true}',
    valueType: 'Json',
    isSensitive: false,
    isEditable: true,
    isInheritable: true,
    validationRules: null,
    uiEditorType: 'json-editor',
    sortOrder: 6100,
  },
  {
    key: 'ai.budget.exceed_policy',
    displayName: 'Budget Exceed Policy',
    description: 'Behavior when budget is exceeded',
    category: 'Functional',
    allowedScopes: ['System', 'Tenant'],
    defaultValue: 'Warn',
    valueType: 'String',
    isSensitive: false,
    isEditable: true,
    isInheritable: true,
    validationRules: '{"enum":["Warn","Block","Throttle"]}',
    uiEditorType: 'select',
    sortOrder: 6150,
  },
  {
    key: 'ai.prompts.base_by_capability',
    displayName: 'Base Prompts by Capability',
    description: 'Base system prompts for each AI capability',
    category: 'Functional',
    allowedScopes: ['System', 'Tenant'],
    defaultValue: '{"chat":"You are NexTraceOne AI Assistant."}',
    valueType: 'Json',
    isSensitive: false,
    isEditable: true,
    isInheritable: true,
    validationRules: null,
    uiEditorType: 'json-editor',
    sortOrder: 6250,
  },
  {
    key: 'ai.defaults.temperature',
    displayName: 'Default Temperature',
    description: 'Default temperature for AI model inference',
    category: 'Functional',
    allowedScopes: ['System', 'Tenant'],
    defaultValue: '0.7',
    valueType: 'Decimal',
    isSensitive: false,
    isEditable: true,
    isInheritable: true,
    validationRules: '{"min":0.0,"max":2.0}',
    uiEditorType: 'text',
    sortOrder: 6280,
  },
  {
    key: 'integrations.connectors.enabled',
    displayName: 'Enabled Connectors',
    description: 'List of connectors enabled in the platform',
    category: 'Functional',
    allowedScopes: ['System', 'Tenant'],
    defaultValue: '["AzureDevOps","GitHub","Jira","ServiceNow"]',
    valueType: 'Json',
    isSensitive: false,
    isEditable: true,
    isInheritable: true,
    validationRules: null,
    uiEditorType: 'json-editor',
    sortOrder: 6400,
  },
  {
    key: 'integrations.retry.max_attempts',
    displayName: 'Max Retry Attempts',
    description: 'Maximum number of retry attempts',
    category: 'Functional',
    allowedScopes: ['System', 'Tenant'],
    defaultValue: '3',
    valueType: 'Integer',
    isSensitive: false,
    isEditable: true,
    isInheritable: true,
    validationRules: '{"min":0,"max":10}',
    uiEditorType: 'text',
    sortOrder: 6440,
  },
  {
    key: 'integrations.sync.filter_policy',
    displayName: 'Sync Filter Policy',
    description: 'Default filters for sync operations',
    category: 'Functional',
    allowedScopes: ['System', 'Tenant'],
    defaultValue: '{"excludeArchived":true,"excludeDeleted":true}',
    valueType: 'Json',
    isSensitive: false,
    isEditable: true,
    isInheritable: true,
    validationRules: null,
    uiEditorType: 'json-editor',
    sortOrder: 6500,
  },
  {
    key: 'integrations.import.policy',
    displayName: 'Import Policy',
    description: 'Default import behavior',
    category: 'Functional',
    allowedScopes: ['System', 'Tenant'],
    defaultValue: '{"allowOverwrite":false,"requireValidation":true}',
    valueType: 'Json',
    isSensitive: false,
    isEditable: true,
    isInheritable: true,
    validationRules: null,
    uiEditorType: 'json-editor',
    sortOrder: 6520,
  },
  {
    key: 'integrations.failure.notification_policy',
    displayName: 'Integration Failure Notification Policy',
    description: 'Notification rules for integration failures',
    category: 'Functional',
    allowedScopes: ['System', 'Tenant'],
    defaultValue: '{"notifyOnFirstFailure":true,"notifyOnAuthFailure":true}',
    valueType: 'Json',
    isSensitive: false,
    isEditable: true,
    isInheritable: true,
    validationRules: null,
    uiEditorType: 'json-editor',
    sortOrder: 6600,
  },
  {
    key: 'integrations.governance.blocked_in_production',
    displayName: 'Integration Operations Blocked in Production',
    description: 'Operations permanently blocked in production',
    category: 'Functional',
    allowedScopes: ['System'],
    defaultValue: '["bulkDelete","schemaOverwrite","forceReSync"]',
    valueType: 'Json',
    isSensitive: false,
    isEditable: true,
    isInheritable: false,
    validationRules: null,
    uiEditorType: 'json-editor',
    sortOrder: 6670,
  },
];

function renderPage() {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false } },
  });
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter>
        <AiIntegrationsConfigurationPage />
      </MemoryRouter>
    </QueryClientProvider>
  );
}

describe('AiIntegrationsConfigurationPage', () => {
  beforeEach(() => {
    vi.mocked(useConfigurationDefinitions).mockReturnValue({
      data: mockDefinitions,
      isLoading: false,
      isError: false,
      refetch: vi.fn(),
      // eslint-disable-next-line @typescript-eslint/no-explicit-any
    } as any);
    vi.mocked(useEffectiveSettings).mockReturnValue({
      data: [],
      isLoading: false,
      // eslint-disable-next-line @typescript-eslint/no-explicit-any
    } as any);
    vi.mocked(useSetConfigurationValue).mockReturnValue({
      mutateAsync: vi.fn(),
      // eslint-disable-next-line @typescript-eslint/no-explicit-any
    } as any);
    vi.mocked(useAuditHistory).mockReturnValue({
      data: [],
      // eslint-disable-next-line @typescript-eslint/no-explicit-any
    } as any);
  });

  it('renders the page title', () => {
    renderPage();
    expect(screen.getByText('AI & Integrations Configuration')).toBeInTheDocument();
  });

  it('renders all section tabs', () => {
    renderPage();
    expect(screen.getByText('Providers & Models')).toBeInTheDocument();
    expect(screen.getByText('Budgets & Quotas')).toBeInTheDocument();
    expect(screen.getByText('Prompts & Retrieval')).toBeInTheDocument();
    expect(screen.getByText('Connectors & Schedules')).toBeInTheDocument();
    expect(screen.getByText('Filters & Sync')).toBeInTheDocument();
    expect(screen.getByText('Failure & Governance')).toBeInTheDocument();
  });

  it('shows providers & models section by default', () => {
    renderPage();
    expect(screen.getByText('Enabled AI Providers')).toBeInTheDocument();
    expect(screen.getByText('Provider Fallback Order')).toBeInTheDocument();
  });

  it('shows budgets & quotas section when clicked', async () => {
    const user = userEvent.setup();
    renderPage();
    await user.click(screen.getByText('Budgets & Quotas'));
    expect(screen.getByText('AI Token Budget by User')).toBeInTheDocument();
    expect(screen.getByText('Budget Exceed Policy')).toBeInTheDocument();
  });

  it('shows prompts & retrieval section when clicked', async () => {
    const user = userEvent.setup();
    renderPage();
    await user.click(screen.getByText('Prompts & Retrieval'));
    expect(screen.getByText('Base Prompts by Capability')).toBeInTheDocument();
    expect(screen.getByText('Default Temperature')).toBeInTheDocument();
  });

  it('shows connectors & schedules section when clicked', async () => {
    const user = userEvent.setup();
    renderPage();
    await user.click(screen.getByText('Connectors & Schedules'));
    expect(screen.getByText('Enabled Connectors')).toBeInTheDocument();
    expect(screen.getByText('Max Retry Attempts')).toBeInTheDocument();
  });

  it('shows filters & sync section when clicked', async () => {
    const user = userEvent.setup();
    renderPage();
    await user.click(screen.getByText('Filters & Sync'));
    expect(screen.getByText('Sync Filter Policy')).toBeInTheDocument();
    expect(screen.getByText('Import Policy')).toBeInTheDocument();
  });

  it('shows failure & governance section when clicked', async () => {
    const user = userEvent.setup();
    renderPage();
    await user.click(screen.getByText('Failure & Governance'));
    expect(screen.getByText('Integration Failure Notification Policy')).toBeInTheDocument();
    expect(screen.getByText('Integration Operations Blocked in Production')).toBeInTheDocument();
  });

  it('renders loading state', () => {
    vi.mocked(useConfigurationDefinitions).mockReturnValue({
      data: undefined,
      isLoading: true,
      isError: false,
      refetch: vi.fn(),
      // eslint-disable-next-line @typescript-eslint/no-explicit-any
    } as any);
    renderPage();
    expect(screen.getByText(/loading/i)).toBeInTheDocument();
  });

  it('renders error state', () => {
    vi.mocked(useConfigurationDefinitions).mockReturnValue({
      data: undefined,
      isLoading: false,
      isError: true,
      refetch: vi.fn(),
      // eslint-disable-next-line @typescript-eslint/no-explicit-any
    } as any);
    renderPage();
    expect(screen.getByText(/failed to load/i)).toBeInTheDocument();
  });

  it('renders empty state when no definitions match', () => {
    vi.mocked(useConfigurationDefinitions).mockReturnValue({
      data: [],
      isLoading: false,
      isError: false,
      refetch: vi.fn(),
      // eslint-disable-next-line @typescript-eslint/no-explicit-any
    } as any);
    renderPage();
    expect(screen.getByText(/no ai.*integrations/i)).toBeInTheDocument();
  });

  it('filters definitions by search query', async () => {
    const user = userEvent.setup();
    renderPage();
    const searchInput = screen.getByPlaceholderText('Search configuration...');
    await user.type(searchInput, 'fallback');
    expect(screen.getByText('Provider Fallback Order')).toBeInTheDocument();
  });

  it('toggles effective settings view', async () => {
    const user = userEvent.setup();
    renderPage();
    const effectiveButton = screen.getByText('Effective Settings');
    await user.click(effectiveButton);
    // The button should now be highlighted (has brand class)
    expect(effectiveButton.closest('button')).toHaveClass('bg-brand-50');
  });
});
