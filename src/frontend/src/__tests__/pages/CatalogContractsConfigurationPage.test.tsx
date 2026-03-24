import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';
import { CatalogContractsConfigurationPage } from '../../features/catalog/pages/CatalogContractsConfigurationPage';

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
    key: 'catalog.contract.types_enabled',
    displayName: 'Enabled Contract Types',
    description: 'Contract types supported and enabled',
    category: 'Functional',
    allowedScopes: ['System', 'Tenant'],
    defaultValue: '["REST","SOAP","GraphQL","gRPC","AsyncAPI"]',
    valueType: 'Json',
    isSensitive: false,
    isEditable: true,
    isInheritable: true,
    validationRules: null,
    uiEditorType: 'json-editor',
    sortOrder: 4000,
  },
  {
    key: 'catalog.contract.versioning_policy',
    displayName: 'Contract Versioning Policy',
    description: 'Versioning strategy per contract type',
    category: 'Functional',
    allowedScopes: ['System', 'Tenant'],
    defaultValue: '{"REST":"SemVer","SOAP":"Sequential"}',
    valueType: 'Json',
    isSensitive: false,
    isEditable: true,
    isInheritable: true,
    validationRules: null,
    uiEditorType: 'json-editor',
    sortOrder: 4020,
  },
  {
    key: 'catalog.contract.breaking_change_severity',
    displayName: 'Breaking Change Default Severity',
    description: 'Default severity for detected breaking changes',
    category: 'Functional',
    allowedScopes: ['System', 'Tenant'],
    defaultValue: 'High',
    valueType: 'String',
    isSensitive: false,
    isEditable: true,
    isInheritable: true,
    validationRules: '{"enum":["Critical","High","Medium","Low"]}',
    uiEditorType: 'select',
    sortOrder: 4040,
  },
  {
    key: 'catalog.validation.rulesets_by_contract_type',
    displayName: 'Rulesets by Contract Type',
    description: 'Validation ruleset bindings per contract type',
    category: 'Functional',
    allowedScopes: ['System', 'Tenant'],
    defaultValue: '{"REST":["openapi-standard"]}',
    valueType: 'Json',
    isSensitive: false,
    isEditable: true,
    isInheritable: true,
    validationRules: null,
    uiEditorType: 'json-editor',
    sortOrder: 4110,
  },
  {
    key: 'catalog.requirements.owner_required',
    displayName: 'Owner Required',
    description: 'Whether a service/contract must have an assigned owner',
    category: 'Functional',
    allowedScopes: ['System', 'Tenant'],
    defaultValue: 'true',
    valueType: 'Boolean',
    isSensitive: false,
    isEditable: true,
    isInheritable: true,
    validationRules: null,
    uiEditorType: 'toggle',
    sortOrder: 4200,
  },
  {
    key: 'catalog.publication.pre_publish_review',
    displayName: 'Pre-Publication Review Required',
    description: 'Whether contracts require review/approval before publication',
    category: 'Functional',
    allowedScopes: ['System', 'Tenant', 'Environment'],
    defaultValue: 'true',
    valueType: 'Boolean',
    isSensitive: false,
    isEditable: true,
    isInheritable: true,
    validationRules: null,
    uiEditorType: 'toggle',
    sortOrder: 4300,
  },
  {
    key: 'catalog.import.overwrite_policy',
    displayName: 'Import Overwrite Policy',
    description: 'Behavior when importing a contract that already exists',
    category: 'Functional',
    allowedScopes: ['System', 'Tenant'],
    defaultValue: 'AskUser',
    valueType: 'String',
    isSensitive: false,
    isEditable: true,
    isInheritable: true,
    validationRules: '{"enum":["Merge","Overwrite","Block","AskUser"]}',
    uiEditorType: 'select',
    sortOrder: 4420,
  },
  {
    key: 'catalog.export.types_allowed',
    displayName: 'Allowed Export Types',
    description: 'Export formats allowed for contracts',
    category: 'Functional',
    allowedScopes: ['System', 'Tenant'],
    defaultValue: '["OpenAPI-JSON","OpenAPI-YAML"]',
    valueType: 'Json',
    isSensitive: false,
    isEditable: true,
    isInheritable: true,
    validationRules: null,
    uiEditorType: 'json-editor',
    sortOrder: 4410,
  },
  {
    key: 'change.types_enabled',
    displayName: 'Enabled Change Types',
    description: 'Types of changes supported',
    category: 'Functional',
    allowedScopes: ['System', 'Tenant'],
    defaultValue: '["Feature","Bugfix","Hotfix","Rollback"]',
    valueType: 'Json',
    isSensitive: false,
    isEditable: true,
    isInheritable: true,
    validationRules: null,
    uiEditorType: 'json-editor',
    sortOrder: 4500,
  },
  {
    key: 'change.blast_radius.thresholds',
    displayName: 'Blast Radius Thresholds',
    description: 'Blast radius score thresholds for classification',
    category: 'Functional',
    allowedScopes: ['System', 'Tenant', 'Environment'],
    defaultValue: '{"Critical":90,"High":70,"Medium":40,"Low":0}',
    valueType: 'Json',
    isSensitive: false,
    isEditable: true,
    isInheritable: true,
    validationRules: null,
    uiEditorType: 'json-editor',
    sortOrder: 4530,
  },
  {
    key: 'change.release_score.weights',
    displayName: 'Release Confidence Score Weights',
    description: 'Weights for each factor in the release confidence score',
    category: 'Functional',
    allowedScopes: ['System', 'Tenant'],
    defaultValue: '{"testCoverage":20,"codeReview":15}',
    valueType: 'Json',
    isSensitive: false,
    isEditable: true,
    isInheritable: true,
    validationRules: null,
    uiEditorType: 'json-editor',
    sortOrder: 4600,
  },
  {
    key: 'change.evidence_pack.required',
    displayName: 'Evidence Pack Required',
    description: 'Whether an evidence pack is required for releases',
    category: 'Functional',
    allowedScopes: ['System', 'Tenant', 'Environment'],
    defaultValue: 'true',
    valueType: 'Boolean',
    isSensitive: false,
    isEditable: true,
    isInheritable: true,
    validationRules: null,
    uiEditorType: 'toggle',
    sortOrder: 4620,
  },
  {
    key: 'change.rollback.recommendation_policy',
    displayName: 'Rollback Recommendation Policy',
    description: 'Policy for when rollback is recommended',
    category: 'Functional',
    allowedScopes: ['System', 'Tenant'],
    defaultValue: '{"autoRecommendOnScoreBelow":40}',
    valueType: 'Json',
    isSensitive: false,
    isEditable: true,
    isInheritable: true,
    validationRules: null,
    uiEditorType: 'json-editor',
    sortOrder: 4650,
  },
  {
    key: 'change.incident_correlation.enabled',
    displayName: 'Release-to-Incident Correlation Enabled',
    description: 'Whether release-to-incident correlation is active',
    category: 'Functional',
    allowedScopes: ['System', 'Tenant'],
    defaultValue: 'true',
    valueType: 'Boolean',
    isSensitive: false,
    isEditable: true,
    isInheritable: true,
    validationRules: null,
    uiEditorType: 'toggle',
    sortOrder: 4680,
  },
];

const mockEffective = [
  {
    key: 'catalog.contract.types_enabled',
    effectiveValue: '["REST","SOAP","GraphQL","gRPC","AsyncAPI"]',
    resolvedScope: 'System',
    resolvedScopeReferenceId: null,
    isInherited: false,
    isDefault: true,
    definitionKey: 'catalog.contract.types_enabled',
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
        <CatalogContractsConfigurationPage />
      </MemoryRouter>
    </QueryClientProvider>
  );
}

describe('CatalogContractsConfigurationPage', () => {
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
    expect(screen.getByRole('heading', { name: /Catalog, Contracts & Change Governance Configuration/i })).toBeInTheDocument();
    expect(screen.getByText(/Configure contract types/i)).toBeInTheDocument();
  });

  it('renders all 7 section tabs', () => {
    renderPage();
    expect(screen.getByText('Contract Types & Versioning')).toBeInTheDocument();
    expect(screen.getByText('Validation & Rulesets')).toBeInTheDocument();
    expect(screen.getByText('Minimum Requirements')).toBeInTheDocument();
    expect(screen.getByText('Publication & Promotion')).toBeInTheDocument();
    expect(screen.getByText('Import / Export')).toBeInTheDocument();
    expect(screen.getByText('Change Types & Blast Radius')).toBeInTheDocument();
    expect(screen.getByText('Release Scoring & Rollback')).toBeInTheDocument();
  });

  it('shows contracts section definitions by default', () => {
    renderPage();
    expect(screen.getByText('Enabled Contract Types')).toBeInTheDocument();
    expect(screen.getByText('Contract Versioning Policy')).toBeInTheDocument();
    expect(screen.getByText('Breaking Change Default Severity')).toBeInTheDocument();
  });

  it('switches to validation section when clicked', async () => {
    renderPage();
    const user = userEvent.setup();
    await user.click(screen.getByText('Validation & Rulesets'));
    expect(screen.getByText('Rulesets by Contract Type')).toBeInTheDocument();
  });

  it('switches to requirements section when clicked', async () => {
    renderPage();
    const user = userEvent.setup();
    await user.click(screen.getByText('Minimum Requirements'));
    expect(screen.getByText('Owner Required')).toBeInTheDocument();
  });

  it('switches to publication section when clicked', async () => {
    renderPage();
    const user = userEvent.setup();
    await user.click(screen.getByText('Publication & Promotion'));
    expect(screen.getByText('Pre-Publication Review Required')).toBeInTheDocument();
  });

  it('switches to import/export section when clicked', async () => {
    renderPage();
    const user = userEvent.setup();
    await user.click(screen.getByText('Import / Export'));
    expect(screen.getByText('Import Overwrite Policy')).toBeInTheDocument();
    expect(screen.getByText('Allowed Export Types')).toBeInTheDocument();
  });

  it('switches to change types section when clicked', async () => {
    renderPage();
    const user = userEvent.setup();
    await user.click(screen.getByText('Change Types & Blast Radius'));
    expect(screen.getByText('Enabled Change Types')).toBeInTheDocument();
    expect(screen.getByText('Blast Radius Thresholds')).toBeInTheDocument();
  });

  it('switches to release scoring section when clicked', async () => {
    renderPage();
    const user = userEvent.setup();
    await user.click(screen.getByText('Release Scoring & Rollback'));
    expect(screen.getByText('Release Confidence Score Weights')).toBeInTheDocument();
    expect(screen.getByText('Evidence Pack Required')).toBeInTheDocument();
    expect(screen.getByText('Rollback Recommendation Policy')).toBeInTheDocument();
    expect(screen.getByText('Release-to-Incident Correlation Enabled')).toBeInTheDocument();
  });

  it('shows loading state', () => {
    (useConfigurationDefinitions as ReturnType<typeof vi.fn>).mockReturnValue({
      data: undefined,
      isLoading: true,
      error: null,
      refetch: vi.fn(),
    });
    renderPage();
    expect(screen.queryByText('Enabled Contract Types')).not.toBeInTheDocument();
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
    expect(screen.getByText(/catalog, contracts & change governance definitions configured/i)).toBeInTheDocument();
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
    await user.type(searchInput, 'Versioning');
    expect(screen.getByText('Contract Versioning Policy')).toBeInTheDocument();
    expect(screen.queryByText('Enabled Contract Types')).not.toBeInTheDocument();
  });
});
