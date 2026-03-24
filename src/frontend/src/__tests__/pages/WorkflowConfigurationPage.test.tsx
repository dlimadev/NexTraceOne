import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';
import { WorkflowConfigurationPage } from '../../features/change-governance/pages/WorkflowConfigurationPage';

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
    key: 'workflow.types.enabled',
    displayName: 'Enabled Workflow Types',
    description: 'List of enabled workflow types',
    category: 'Functional',
    allowedScopes: ['System', 'Tenant'],
    defaultValue: '["ReleaseApproval","PromotionApproval"]',
    valueType: 'Json',
    isSensitive: false,
    isEditable: true,
    isInheritable: true,
    validationRules: null,
    uiEditorType: 'json-editor',
    sortOrder: 2000,
  },
  {
    key: 'workflow.templates.default',
    displayName: 'Default Workflow Template',
    description: 'Default workflow template definition',
    category: 'Functional',
    allowedScopes: ['System', 'Tenant', 'Environment'],
    defaultValue: '{}',
    valueType: 'Json',
    isSensitive: false,
    isEditable: true,
    isInheritable: true,
    validationRules: null,
    uiEditorType: 'json-editor',
    sortOrder: 2010,
  },
  {
    key: 'workflow.quorum.default_rule',
    displayName: 'Default Quorum Rule',
    description: 'Default quorum rule for approval stages',
    category: 'Functional',
    allowedScopes: ['System', 'Tenant', 'Environment'],
    defaultValue: 'SingleApprover',
    valueType: 'String',
    isSensitive: false,
    isEditable: true,
    isInheritable: true,
    validationRules: '{"enum":["SingleApprover","Majority","Unanimous"]}',
    uiEditorType: 'select',
    sortOrder: 2120,
  },
  {
    key: 'workflow.approvers.self_approval_allowed',
    displayName: 'Self-Approval Allowed',
    description: 'Whether the requester can approve their own workflow',
    category: 'Functional',
    allowedScopes: ['System', 'Tenant', 'Environment'],
    defaultValue: 'false',
    valueType: 'Boolean',
    isSensitive: false,
    isEditable: true,
    isInheritable: true,
    validationRules: null,
    uiEditorType: 'toggle',
    sortOrder: 2220,
  },
  {
    key: 'workflow.sla.default_hours',
    displayName: 'Default Workflow SLA (Hours)',
    description: 'Default SLA in hours for workflow completion',
    category: 'Functional',
    allowedScopes: ['System', 'Tenant', 'Environment'],
    defaultValue: '48',
    valueType: 'Integer',
    isSensitive: false,
    isEditable: true,
    isInheritable: true,
    validationRules: '{"min":1,"max":720}',
    uiEditorType: 'text',
    sortOrder: 2300,
  },
  {
    key: 'workflow.gates.enabled',
    displayName: 'Gates Enabled',
    description: 'Whether gate evaluations are enforced',
    category: 'Functional',
    allowedScopes: ['System', 'Tenant', 'Environment'],
    defaultValue: 'true',
    valueType: 'Boolean',
    isSensitive: false,
    isEditable: true,
    isInheritable: true,
    validationRules: null,
    uiEditorType: 'toggle',
    sortOrder: 2400,
  },
  {
    key: 'promotion.paths.allowed',
    displayName: 'Allowed Promotion Paths',
    description: 'Allowed source→target environment promotion paths',
    category: 'Functional',
    allowedScopes: ['System', 'Tenant'],
    defaultValue: '[]',
    valueType: 'Json',
    isSensitive: false,
    isEditable: true,
    isInheritable: true,
    validationRules: null,
    uiEditorType: 'json-editor',
    sortOrder: 2500,
  },
  {
    key: 'promotion.freeze.enabled',
    displayName: 'Freeze Policy Enabled',
    description: 'Whether freeze windows are enforced',
    category: 'Functional',
    allowedScopes: ['System', 'Tenant', 'Environment'],
    defaultValue: 'false',
    valueType: 'Boolean',
    isSensitive: false,
    isEditable: true,
    isInheritable: true,
    validationRules: null,
    uiEditorType: 'toggle',
    sortOrder: 2620,
  },
  {
    key: 'promotion.freeze.override_allowed',
    displayName: 'Freeze Override Allowed',
    description: 'Whether freeze windows can be overridden',
    category: 'Functional',
    allowedScopes: ['System'],
    defaultValue: 'false',
    valueType: 'Boolean',
    isSensitive: false,
    isEditable: true,
    isInheritable: false,
    validationRules: null,
    uiEditorType: 'toggle',
    sortOrder: 2640,
  },
];

const mockEffective = [
  {
    key: 'workflow.types.enabled',
    effectiveValue: '["ReleaseApproval","PromotionApproval"]',
    resolvedScope: 'System',
    resolvedScopeReferenceId: null,
    isInherited: false,
    isDefault: true,
    definitionKey: 'workflow.types.enabled',
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
        <WorkflowConfigurationPage />
      </MemoryRouter>
    </QueryClientProvider>
  );
}

describe('WorkflowConfigurationPage', () => {
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
    expect(screen.getByRole('heading', { name: /Workflow & Promotion Governance/i })).toBeInTheDocument();
    expect(screen.getByText(/Configure approval workflows/i)).toBeInTheDocument();
  });

  it('renders all 7 section tabs', () => {
    renderPage();
    expect(screen.getByText('Types & Templates')).toBeInTheDocument();
    expect(screen.getByText('Stages & Quorum')).toBeInTheDocument();
    expect(screen.getByText('Approvers & Escalation')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /SLA/ })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /Gates/ })).toBeInTheDocument();
    expect(screen.getByText('Promotion Governance')).toBeInTheDocument();
    expect(screen.getByText(/Release Windows/)).toBeInTheDocument();
  });

  it('shows templates section definitions by default', () => {
    renderPage();
    expect(screen.getByText('Enabled Workflow Types')).toBeInTheDocument();
    expect(screen.getByText('Default Workflow Template')).toBeInTheDocument();
  });

  it('switches to stages section when clicked', async () => {
    renderPage();
    const user = userEvent.setup();
    await user.click(screen.getByText('Stages & Quorum'));
    expect(screen.getByText('Default Quorum Rule')).toBeInTheDocument();
  });

  it('switches to approvers section when clicked', async () => {
    renderPage();
    const user = userEvent.setup();
    await user.click(screen.getByText('Approvers & Escalation'));
    expect(screen.getByText('Self-Approval Allowed')).toBeInTheDocument();
  });

  it('switches to SLA section when clicked', async () => {
    renderPage();
    const user = userEvent.setup();
    await user.click(screen.getByRole('button', { name: /SLA/ }));
    expect(screen.getByText('Default Workflow SLA (Hours)')).toBeInTheDocument();
  });

  it('switches to gates section when clicked', async () => {
    renderPage();
    const user = userEvent.setup();
    await user.click(screen.getByRole('button', { name: /Gates/ }));
    expect(screen.getByText('Gates Enabled')).toBeInTheDocument();
  });

  it('switches to promotion section when clicked', async () => {
    renderPage();
    const user = userEvent.setup();
    await user.click(screen.getByText('Promotion Governance'));
    expect(screen.getByText('Allowed Promotion Paths')).toBeInTheDocument();
  });

  it('switches to freeze section when clicked', async () => {
    renderPage();
    const user = userEvent.setup();
    await user.click(screen.getByText(/Release Windows/));
    expect(screen.getByText('Freeze Policy Enabled')).toBeInTheDocument();
  });

  it('shows non-inheritable badge for system-only freeze override', async () => {
    renderPage();
    const user = userEvent.setup();
    await user.click(screen.getByText(/Release Windows/));
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
    // PageLoadingState should render (no definitions content)
    expect(screen.queryByText('Enabled Workflow Types')).not.toBeInTheDocument();
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
    expect(screen.getByText(/workflow & promotion governance definitions configured/i)).toBeInTheDocument();
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
    await user.type(searchInput, 'Template');
    expect(screen.getByText('Default Workflow Template')).toBeInTheDocument();
    expect(screen.queryByText('Enabled Workflow Types')).not.toBeInTheDocument();
  });
});
