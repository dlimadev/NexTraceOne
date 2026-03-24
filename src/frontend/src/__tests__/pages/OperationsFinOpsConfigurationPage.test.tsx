import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';
import { OperationsFinOpsConfigurationPage } from '../../features/operational-intelligence/pages/OperationsFinOpsConfigurationPage';

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
    key: 'incidents.taxonomy.categories',
    displayName: 'Incident Categories',
    description: 'Defined incident categories',
    category: 'Functional',
    allowedScopes: ['System', 'Tenant'],
    defaultValue: '["Infrastructure","Application","Security","Data","Network","ThirdParty"]',
    valueType: 'Json',
    isSensitive: false,
    isEditable: true,
    isInheritable: true,
    validationRules: null,
    uiEditorType: 'json-editor',
    sortOrder: 5000,
  },
  {
    key: 'incidents.sla.by_severity',
    displayName: 'SLA by Severity',
    description: 'SLA targets in minutes by severity',
    category: 'Functional',
    allowedScopes: ['System', 'Tenant', 'Environment'],
    defaultValue: '{"Critical":{"acknowledgementMinutes":5}}',
    valueType: 'Json',
    isSensitive: false,
    isEditable: true,
    isInheritable: true,
    validationRules: null,
    uiEditorType: 'json-editor',
    sortOrder: 5060,
  },
  {
    key: 'incidents.owner.fallback',
    displayName: 'Fallback Incident Owner',
    description: 'Fallback owner when no specific owner can be determined',
    category: 'Functional',
    allowedScopes: ['System', 'Tenant'],
    defaultValue: 'platform-admin',
    valueType: 'String',
    isSensitive: false,
    isEditable: true,
    isInheritable: true,
    validationRules: null,
    uiEditorType: 'text',
    sortOrder: 5110,
  },
  {
    key: 'incidents.auto_creation.enabled',
    displayName: 'Auto-Incident Creation Enabled',
    description: 'Whether incidents can be automatically created from alerts',
    category: 'Functional',
    allowedScopes: ['System', 'Tenant', 'Environment'],
    defaultValue: 'true',
    valueType: 'Boolean',
    isSensitive: false,
    isEditable: true,
    isInheritable: true,
    validationRules: null,
    uiEditorType: 'toggle',
    sortOrder: 5140,
  },
  {
    key: 'operations.playbook.defaults_by_type',
    displayName: 'Default Playbook by Incident Type',
    description: 'Default playbook identifier per incident type',
    category: 'Functional',
    allowedScopes: ['System', 'Tenant'],
    defaultValue: '{"Outage":"playbook-outage-standard"}',
    valueType: 'Json',
    isSensitive: false,
    isEditable: true,
    isInheritable: true,
    validationRules: null,
    uiEditorType: 'json-editor',
    sortOrder: 5200,
  },
  {
    key: 'finops.budget.default_currency',
    displayName: 'Default Budget Currency',
    description: 'Default currency for FinOps budgets',
    category: 'Functional',
    allowedScopes: ['System', 'Tenant'],
    defaultValue: 'USD',
    valueType: 'String',
    isSensitive: false,
    isEditable: true,
    isInheritable: true,
    validationRules: '{"maxLength":3}',
    uiEditorType: 'text',
    sortOrder: 5300,
  },
  {
    key: 'finops.anomaly.detection_enabled',
    displayName: 'Cost Anomaly Detection Enabled',
    description: 'Whether cost anomaly detection is active',
    category: 'Functional',
    allowedScopes: ['System', 'Tenant'],
    defaultValue: 'true',
    valueType: 'Boolean',
    isSensitive: false,
    isEditable: true,
    isInheritable: true,
    validationRules: null,
    uiEditorType: 'toggle',
    sortOrder: 5400,
  },
  {
    key: 'benchmarking.score.weights',
    displayName: 'Benchmarking Score Weights',
    description: 'Weights for each benchmarking dimension',
    category: 'Functional',
    allowedScopes: ['System', 'Tenant'],
    defaultValue: '{"reliability":25,"performance":20}',
    valueType: 'Json',
    isSensitive: false,
    isEditable: true,
    isInheritable: true,
    validationRules: null,
    uiEditorType: 'json-editor',
    sortOrder: 5500,
  },
  {
    key: 'operations.health.anomaly_thresholds',
    displayName: 'Operational Health Anomaly Thresholds',
    description: 'Functional thresholds for operational health anomaly detection',
    category: 'Functional',
    allowedScopes: ['System', 'Tenant', 'Environment'],
    defaultValue: '{"errorRateWarning":1.0}',
    valueType: 'Json',
    isSensitive: false,
    isEditable: true,
    isInheritable: true,
    validationRules: null,
    uiEditorType: 'json-editor',
    sortOrder: 5600,
  },
];

const mockEffective = [
  {
    key: 'incidents.taxonomy.categories',
    effectiveValue: '["Infrastructure","Application","Security","Data","Network","ThirdParty"]',
    resolvedScope: 'System',
    resolvedScopeReferenceId: null,
    isDefault: true,
    isInherited: false,
  },
  {
    key: 'incidents.sla.by_severity',
    effectiveValue: '{"Critical":{"acknowledgementMinutes":5}}',
    resolvedScope: 'System',
    resolvedScopeReferenceId: null,
    isDefault: true,
    isInherited: false,
  },
  {
    key: 'incidents.owner.fallback',
    effectiveValue: 'platform-admin',
    resolvedScope: 'System',
    resolvedScopeReferenceId: null,
    isDefault: true,
    isInherited: false,
  },
  {
    key: 'incidents.auto_creation.enabled',
    effectiveValue: 'true',
    resolvedScope: 'System',
    resolvedScopeReferenceId: null,
    isDefault: true,
    isInherited: false,
  },
];

function renderPage() {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter>
        <OperationsFinOpsConfigurationPage />
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

describe('OperationsFinOpsConfigurationPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    (useConfigurationDefinitions as ReturnType<typeof vi.fn>).mockReturnValue({
      data: mockDefinitions,
      isLoading: false,
      error: null,
      refetch: vi.fn(),
    });
    (useEffectiveSettings as ReturnType<typeof vi.fn>).mockReturnValue({
      data: mockEffective,
      isLoading: false,
      refetch: vi.fn(),
    });
    (useSetConfigurationValue as ReturnType<typeof vi.fn>).mockReturnValue({
      mutateAsync: vi.fn(),
      isPending: false,
    });
    (useAuditHistory as ReturnType<typeof vi.fn>).mockReturnValue({
      data: [],
      isLoading: false,
    });
  });

  it('renders page title', () => {
    renderPage();
    expect(screen.getByText(/Operations, Incidents, FinOps & Benchmarking Configuration/i)).toBeInTheDocument();
  });

  it('renders all section tabs', () => {
    renderPage();
    expect(screen.getByText('Incident Taxonomy & SLA')).toBeInTheDocument();
    expect(screen.getByText('Owners & Correlation')).toBeInTheDocument();
    expect(screen.getByText('Playbooks & Automation')).toBeInTheDocument();
    expect(screen.getByText('Budgets & Thresholds')).toBeInTheDocument();
    expect(screen.getByText('Anomaly, Waste & Health')).toBeInTheDocument();
    expect(screen.getByText('Benchmarking & Scores')).toBeInTheDocument();
  });

  it('shows incident taxonomy definitions by default', () => {
    renderPage();
    expect(screen.getByText('Incident Categories')).toBeInTheDocument();
    expect(screen.getByText('SLA by Severity')).toBeInTheDocument();
  });

  it('shows scope selector', () => {
    renderPage();
    expect(screen.getByText('Scope')).toBeInTheDocument();
  });

  it('shows search input', () => {
    renderPage();
    expect(screen.getByPlaceholderText('Search by key or name...')).toBeInTheDocument();
  });

  it('shows owners & correlation section when clicked', async () => {
    const user = userEvent.setup();
    renderPage();
    await user.click(screen.getByText('Owners & Correlation'));
    expect(screen.getByText('Fallback Incident Owner')).toBeInTheDocument();
    expect(screen.getByText('Auto-Incident Creation Enabled')).toBeInTheDocument();
  });

  it('shows playbooks & automation section when clicked', async () => {
    const user = userEvent.setup();
    renderPage();
    await user.click(screen.getByText('Playbooks & Automation'));
    expect(screen.getByText('Default Playbook by Incident Type')).toBeInTheDocument();
  });

  it('shows budgets section when clicked', async () => {
    const user = userEvent.setup();
    renderPage();
    await user.click(screen.getByText('Budgets & Thresholds'));
    expect(screen.getByText('Default Budget Currency')).toBeInTheDocument();
  });

  it('shows anomaly & waste section when clicked', async () => {
    const user = userEvent.setup();
    renderPage();
    await user.click(screen.getByText('Anomaly, Waste & Health'));
    expect(screen.getByText('Cost Anomaly Detection Enabled')).toBeInTheDocument();
  });

  it('shows benchmarking section when clicked', async () => {
    const user = userEvent.setup();
    renderPage();
    await user.click(screen.getByText('Benchmarking & Scores'));
    expect(screen.getByText('Benchmarking Score Weights')).toBeInTheDocument();
  });

  it('renders loading state', () => {
    (useConfigurationDefinitions as ReturnType<typeof vi.fn>).mockReturnValue({
      data: null,
      isLoading: true,
      error: null,
      refetch: vi.fn(),
    });
    renderPage();
    // PageLoadingState renders a spinner/loading indicator
    expect(document.querySelector('.animate-spin, .animate-pulse')).toBeTruthy();
  });

  it('renders error state', () => {
    (useConfigurationDefinitions as ReturnType<typeof vi.fn>).mockReturnValue({
      data: null,
      isLoading: false,
      error: new Error('Network error'),
      refetch: vi.fn(),
    });
    renderPage();
    expect(screen.getByText('Retry')).toBeInTheDocument();
  });

  it('renders empty state when no definitions match', async () => {
    const user = userEvent.setup();
    renderPage();
    const searchInput = screen.getByPlaceholderText('Search by key or name...');
    await user.type(searchInput, 'zzzznonexistent');
    expect(screen.getByText('No definitions found')).toBeInTheDocument();
  });
});
