import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, fireEvent } from '@testing-library/react';
import { AdvancedConfigurationConsolePage } from '../../features/configuration/pages/AdvancedConfigurationConsolePage';

// ── Mocks ───────────────────────────────────────────────────────────────

vi.mock('react-i18next', () => ({
  useTranslation: () => ({
    t: (_key: string, fallback?: string) => fallback ?? _key,
  }),
}));

const mockDefinitions = [
  {
    key: 'ai.providers.enabled',
    displayName: 'AI Providers Enabled',
    description: 'List of enabled AI providers',
    category: 'Functional',
    allowedScopes: ['System', 'Tenant'],
    defaultValue: '["openai","azure"]',
    valueType: 'Json',
    isSensitive: false,
    isEditable: true,
    isInheritable: true,
    validationRules: null,
    uiEditorType: 'json',
    sortOrder: 6000,
  },
  {
    key: 'integrations.connectors.enabled',
    displayName: 'Connectors Enabled',
    description: 'List of enabled connectors',
    category: 'Functional',
    allowedScopes: ['System', 'Tenant', 'Environment'],
    defaultValue: '["jira","github","azure-devops"]',
    valueType: 'Json',
    isSensitive: false,
    isEditable: true,
    isInheritable: true,
    validationRules: null,
    uiEditorType: 'json',
    sortOrder: 6400,
  },
  {
    key: 'ai.budget.api.key.ref',
    displayName: 'AI API Key Reference',
    description: 'Reference to API key vault path',
    category: 'Functional',
    allowedScopes: ['System'],
    defaultValue: null,
    valueType: 'String',
    isSensitive: true,
    isEditable: false,
    isInheritable: false,
    validationRules: null,
    uiEditorType: 'text',
    sortOrder: 6005,
  },
  {
    key: 'notifications.email.enabled',
    displayName: 'Email Notifications',
    description: 'Enable email notifications',
    category: 'Functional',
    allowedScopes: ['System', 'Tenant'],
    defaultValue: 'true',
    valueType: 'Boolean',
    isSensitive: false,
    isEditable: true,
    isInheritable: true,
    validationRules: null,
    uiEditorType: 'toggle',
    sortOrder: 150,
  },
];

const mockEffective = [
  {
    key: 'ai.providers.enabled',
    effectiveValue: '["openai"]',
    resolvedScope: 'Tenant',
    resolvedScopeReferenceId: null,
    isInherited: false,
    isDefault: false,
    definitionKey: 'ai.providers.enabled',
    valueType: 'Json',
    isSensitive: false,
    version: 2,
  },
  {
    key: 'integrations.connectors.enabled',
    effectiveValue: '["jira","github","azure-devops"]',
    resolvedScope: 'System',
    resolvedScopeReferenceId: null,
    isInherited: true,
    isDefault: true,
    definitionKey: 'integrations.connectors.enabled',
    valueType: 'Json',
    isSensitive: false,
    version: 1,
  },
];

const mockAuditData = [
  {
    key: 'ai.providers.enabled',
    scope: 'System',
    scopeReferenceId: null,
    action: 'Set',
    previousValue: '["openai","azure"]',
    newValue: '["openai"]',
    previousVersion: 1,
    newVersion: 2,
    changedBy: 'admin@nextraceone.com',
    changedAt: '2026-03-20T10:00:00Z',
    changeReason: 'Disabled Azure provider',
    isSensitive: false,
  },
];

vi.mock('../../features/configuration/hooks/useConfiguration', () => ({
  useConfigurationDefinitions: vi.fn(() => ({
    data: mockDefinitions,
    isLoading: false,
    isError: false,
    refetch: vi.fn(),
  })),
  useEffectiveSettings: vi.fn(() => ({
    data: mockEffective,
    isLoading: false,
  })),
  useAuditHistory: vi.fn(() => ({
    data: mockAuditData,
  })),
}));

// ── Tests ───────────────────────────────────────────────────────────────

describe('AdvancedConfigurationConsolePage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('renders the page title', () => {
    render(<AdvancedConfigurationConsolePage />);
    expect(screen.getByText('Advanced Configuration Console')).toBeDefined();
  });

  it('renders all 6 tabs', () => {
    render(<AdvancedConfigurationConsolePage />);
    expect(screen.getByText('Effective Explorer')).toBeDefined();
    expect(screen.getByText('Diff & Compare')).toBeDefined();
    expect(screen.getByText('Import / Export')).toBeDefined();
    expect(screen.getByText('Rollback & Restore')).toBeDefined();
    expect(screen.getByText('History & Timeline')).toBeDefined();
    expect(screen.getByText('Health & Troubleshooting')).toBeDefined();
  });

  it('renders domain navigation buttons', () => {
    render(<AdvancedConfigurationConsolePage />);
    // t(`advancedConfig.domains.${domain.key}`, domain.key) → returns domain.key as fallback
    expect(screen.getByText('all')).toBeDefined();
    expect(screen.getByText('ai')).toBeDefined();
    expect(screen.getByText('integrations')).toBeDefined();
    expect(screen.getByText('notifications')).toBeDefined();
  });

  it('renders search input', () => {
    render(<AdvancedConfigurationConsolePage />);
    expect(screen.getByPlaceholderText('Search by key or name...')).toBeDefined();
  });

  it('shows definitions in the effective explorer tab by default', () => {
    render(<AdvancedConfigurationConsolePage />);
    expect(screen.getByText('AI Providers Enabled')).toBeDefined();
    expect(screen.getByText('Connectors Enabled')).toBeDefined();
  });

  it('filters definitions by domain when clicking domain button', () => {
    render(<AdvancedConfigurationConsolePage />);
    const aiBtn = screen.getByText('ai');
    fireEvent.click(aiBtn);
    expect(screen.getByText('AI Providers Enabled')).toBeDefined();
    expect(screen.queryByText('Email Notifications')).toBeNull();
  });

  it('filters definitions by search query', () => {
    render(<AdvancedConfigurationConsolePage />);
    const searchInput = screen.getByPlaceholderText('Search by key or name...');
    fireEvent.change(searchInput, { target: { value: 'providers' } });
    expect(screen.getByText('AI Providers Enabled')).toBeDefined();
    expect(screen.queryByText('Email Notifications')).toBeNull();
  });

  it('switches to diff tab and shows comparison controls', () => {
    render(<AdvancedConfigurationConsolePage />);
    fireEvent.click(screen.getByText('Diff & Compare'));
    expect(screen.getByText('Left Scope')).toBeDefined();
    expect(screen.getByText('Right Scope')).toBeDefined();
  });

  it('switches to import/export tab and shows export/import sections', () => {
    render(<AdvancedConfigurationConsolePage />);
    fireEvent.click(screen.getByText('Import / Export'));
    expect(screen.getByText('Export Configuration')).toBeDefined();
    expect(screen.getByText('Import Configuration')).toBeDefined();
  });

  it('switches to rollback tab and shows rollback section', () => {
    render(<AdvancedConfigurationConsolePage />);
    fireEvent.click(screen.getByText('Rollback & Restore'));
    expect(screen.getByText('Configuration Rollback')).toBeDefined();
  });

  it('switches to history tab and shows timeline section', () => {
    render(<AdvancedConfigurationConsolePage />);
    fireEvent.click(screen.getByText('History & Timeline'));
    expect(screen.getByText('Configuration Change Timeline')).toBeDefined();
  });

  it('switches to health tab and shows health checks and governance', () => {
    render(<AdvancedConfigurationConsolePage />);
    fireEvent.click(screen.getByText('Health & Troubleshooting'));
    expect(screen.getByText('Configuration Platform Health')).toBeDefined();
    expect(screen.getByText('Definition Governance')).toBeDefined();
    expect(screen.getByText('Domain Breakdown')).toBeDefined();
  });

  it('shows sensitive badge for sensitive definitions', () => {
    render(<AdvancedConfigurationConsolePage />);
    const badges = screen.getAllByText('Sensitive');
    expect(badges.length).toBeGreaterThan(0);
  });
});
