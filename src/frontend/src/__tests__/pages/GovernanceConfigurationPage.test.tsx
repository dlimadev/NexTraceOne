import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';
import { GovernanceConfigurationPage } from '../../features/governance/pages/GovernanceConfigurationPage';

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
    key: 'governance.policies.enabled',
    displayName: 'Enabled Governance Policies',
    description: 'List of governance policy IDs enabled for evaluation',
    category: 'Functional',
    allowedScopes: ['System', 'Tenant'],
    defaultValue: '["SecurityBaseline","ApiVersioning"]',
    valueType: 'Json',
    isSensitive: false,
    isEditable: true,
    isInheritable: true,
    validationRules: null,
    uiEditorType: 'json-editor',
    sortOrder: 3000,
  },
  {
    key: 'governance.policies.severity',
    displayName: 'Policy Severity Map',
    description: 'Severity level per policy',
    category: 'Functional',
    allowedScopes: ['System', 'Tenant'],
    defaultValue: '{"SecurityBaseline":"Critical"}',
    valueType: 'Json',
    isSensitive: false,
    isEditable: true,
    isInheritable: true,
    validationRules: null,
    uiEditorType: 'json-editor',
    sortOrder: 3010,
  },
  {
    key: 'governance.compliance.profiles.default',
    displayName: 'Default Compliance Profile',
    description: 'Default compliance profile',
    category: 'Functional',
    allowedScopes: ['System', 'Tenant', 'Environment'],
    defaultValue: 'Standard',
    valueType: 'String',
    isSensitive: false,
    isEditable: true,
    isInheritable: true,
    validationRules: '{"enum":["Standard","Enhanced","Strict"]}',
    uiEditorType: 'select',
    sortOrder: 3060,
  },
  {
    key: 'governance.evidence.expiry_days',
    displayName: 'Evidence Default Expiry (Days)',
    description: 'Default number of days before evidence expires',
    category: 'Functional',
    allowedScopes: ['System', 'Tenant', 'Environment'],
    defaultValue: '90',
    valueType: 'Integer',
    isSensitive: false,
    isEditable: true,
    isInheritable: true,
    validationRules: '{"min":1,"max":730}',
    uiEditorType: 'text',
    sortOrder: 3120,
  },
  {
    key: 'governance.waiver.require_approval',
    displayName: 'Waiver Requires Approval',
    description: 'Whether waiver requests require explicit approval',
    category: 'Functional',
    allowedScopes: ['System', 'Tenant'],
    defaultValue: 'true',
    valueType: 'Boolean',
    isSensitive: false,
    isEditable: true,
    isInheritable: true,
    validationRules: null,
    uiEditorType: 'toggle',
    sortOrder: 3240,
  },
  {
    key: 'governance.waiver.blocked_environments',
    displayName: 'Waiver Blocked Environments',
    description: 'Environments where waivers are never allowed',
    category: 'Functional',
    allowedScopes: ['System'],
    defaultValue: '["Production"]',
    valueType: 'Json',
    isSensitive: false,
    isEditable: true,
    isInheritable: false,
    validationRules: null,
    uiEditorType: 'json-editor',
    sortOrder: 3270,
  },
  {
    key: 'governance.packs.enabled',
    displayName: 'Enabled Governance Packs',
    description: 'List of enabled governance pack identifiers',
    category: 'Functional',
    allowedScopes: ['System', 'Tenant'],
    defaultValue: '["CoreGovernance","ApiGovernance"]',
    valueType: 'Json',
    isSensitive: false,
    isEditable: true,
    isInheritable: true,
    validationRules: null,
    uiEditorType: 'json-editor',
    sortOrder: 3300,
  },
  {
    key: 'governance.packs.overlap_resolution',
    displayName: 'Pack Overlap Resolution Strategy',
    description: 'How to resolve policy conflicts between overlapping packs',
    category: 'Functional',
    allowedScopes: ['System', 'Tenant'],
    defaultValue: 'MostRestrictive',
    valueType: 'String',
    isSensitive: false,
    isEditable: true,
    isInheritable: true,
    validationRules: '{"enum":["MostRestrictive","MostSpecific","Merge"]}',
    uiEditorType: 'select',
    sortOrder: 3350,
  },
  {
    key: 'governance.scorecard.enabled',
    displayName: 'Governance Scorecard Enabled',
    description: 'Whether governance scorecards are active',
    category: 'Functional',
    allowedScopes: ['System', 'Tenant'],
    defaultValue: 'true',
    valueType: 'Boolean',
    isSensitive: false,
    isEditable: true,
    isInheritable: true,
    validationRules: null,
    uiEditorType: 'toggle',
    sortOrder: 3400,
  },
  {
    key: 'governance.risk.matrix',
    displayName: 'Risk Matrix Definition',
    description: 'Risk matrix mapping likelihood x impact to risk level',
    category: 'Functional',
    allowedScopes: ['System', 'Tenant'],
    defaultValue: '{"High_High":"Critical"}',
    valueType: 'Json',
    isSensitive: false,
    isEditable: true,
    isInheritable: true,
    validationRules: null,
    uiEditorType: 'json-editor',
    sortOrder: 3440,
  },
  {
    key: 'governance.requirements.by_system_type',
    displayName: 'Minimum Requirements by System Type',
    description: 'Minimum governance requirements per system type',
    category: 'Functional',
    allowedScopes: ['System', 'Tenant'],
    defaultValue: '{"REST":{"mandatoryPolicies":["SecurityBaseline"]}}',
    valueType: 'Json',
    isSensitive: false,
    isEditable: true,
    isInheritable: true,
    validationRules: null,
    uiEditorType: 'json-editor',
    sortOrder: 3500,
  },
  {
    key: 'governance.requirements.promotion_gates',
    displayName: 'Governance Promotion Gates',
    description: 'Minimum governance gates required for promotion',
    category: 'Functional',
    allowedScopes: ['System', 'Tenant', 'Environment'],
    defaultValue: '{"Production":{"minScore":70}}',
    valueType: 'Json',
    isSensitive: false,
    isEditable: true,
    isInheritable: true,
    validationRules: null,
    uiEditorType: 'json-editor',
    sortOrder: 3540,
  },
];

const mockEffective = [
  {
    key: 'governance.policies.enabled',
    effectiveValue: '["SecurityBaseline","ApiVersioning"]',
    resolvedScope: 'System',
    resolvedScopeReferenceId: null,
    isInherited: false,
    isDefault: true,
    definitionKey: 'governance.policies.enabled',
    valueType: 'Json',
    isSensitive: false,
    version: 1,
  },
];

function renderPage() {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false } },
  });
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter>
        <GovernanceConfigurationPage />
      </MemoryRouter>
    </QueryClientProvider>
  );
}

describe('GovernanceConfigurationPage', () => {
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

  it('renders page title and subtitle', () => {
    renderPage();
    expect(screen.getByRole('heading', { name: /Governance & Compliance Configuration/i })).toBeInTheDocument();
    expect(screen.getByText(/Configure policies/i)).toBeInTheDocument();
  });

  it('renders all 6 section tabs', () => {
    renderPage();
    expect(screen.getByText('Policies & Profiles')).toBeInTheDocument();
    expect(screen.getByText('Evidence')).toBeInTheDocument();
    expect(screen.getByText('Waivers')).toBeInTheDocument();
    expect(screen.getByText('Packs & Bindings')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /Scorecards/ })).toBeInTheDocument();
    expect(screen.getByText('Minimum Requirements')).toBeInTheDocument();
  });

  it('shows policies section definitions by default', () => {
    renderPage();
    expect(screen.getByText('Enabled Governance Policies')).toBeInTheDocument();
    expect(screen.getByText('Policy Severity Map')).toBeInTheDocument();
    expect(screen.getByText('Default Compliance Profile')).toBeInTheDocument();
  });

  it('switches to evidence section when clicked', async () => {
    renderPage();
    const user = userEvent.setup();
    await user.click(screen.getByText('Evidence'));
    expect(screen.getByText('Evidence Default Expiry (Days)')).toBeInTheDocument();
  });

  it('switches to waivers section when clicked', async () => {
    renderPage();
    const user = userEvent.setup();
    await user.click(screen.getByText('Waivers'));
    expect(screen.getByText('Waiver Requires Approval')).toBeInTheDocument();
  });

  it('switches to packs section when clicked', async () => {
    renderPage();
    const user = userEvent.setup();
    await user.click(screen.getByText('Packs & Bindings'));
    expect(screen.getByText('Enabled Governance Packs')).toBeInTheDocument();
  });

  it('switches to scorecards section when clicked', async () => {
    renderPage();
    const user = userEvent.setup();
    await user.click(screen.getByRole('button', { name: /Scorecards/ }));
    expect(screen.getByText('Governance Scorecard Enabled')).toBeInTheDocument();
    expect(screen.getByText('Risk Matrix Definition')).toBeInTheDocument();
  });

  it('switches to requirements section when clicked', async () => {
    renderPage();
    const user = userEvent.setup();
    await user.click(screen.getByText('Minimum Requirements'));
    expect(screen.getByText('Minimum Requirements by System Type')).toBeInTheDocument();
    expect(screen.getByText('Governance Promotion Gates')).toBeInTheDocument();
  });

  it('shows non-inheritable badge for waiver blocked environments', async () => {
    renderPage();
    const user = userEvent.setup();
    await user.click(screen.getByText('Waivers'));
    expect(screen.getByText('Non-inheritable')).toBeInTheDocument();
  });

  it('shows loading state', () => {
    (useConfigurationDefinitions as ReturnType<typeof vi.fn>).mockReturnValue({
      data: undefined,
      isLoading: true,
      error: null,
      refetch: vi.fn(),
    });
    renderPage();
    expect(screen.queryByText('Enabled Governance Policies')).not.toBeInTheDocument();
  });

  it('shows error state with retry button', () => {
    (useConfigurationDefinitions as ReturnType<typeof vi.fn>).mockReturnValue({
      data: undefined,
      isLoading: false,
      error: new Error('Network error'),
      refetch: vi.fn(),
    });
    renderPage();
    expect(screen.getByText(/Error loading configuration/i)).toBeInTheDocument();
    expect(screen.getByText('Retry')).toBeInTheDocument();
  });

  it('shows footer with definition count', () => {
    renderPage();
    expect(screen.getByText(/governance & compliance definitions configured/i)).toBeInTheDocument();
  });

  it('displays effective value with default badge', () => {
    renderPage();
    expect(screen.getByText('Effective Value')).toBeInTheDocument();
    expect(screen.getByText('Default')).toBeInTheDocument();
  });

  it('filters definitions by search term', async () => {
    renderPage();
    const user = userEvent.setup();
    const searchInput = screen.getByPlaceholderText('Search by key or name...');
    await user.type(searchInput, 'Severity');
    expect(screen.getByText('Policy Severity Map')).toBeInTheDocument();
    expect(screen.queryByText('Enabled Governance Policies')).not.toBeInTheDocument();
  });
});
