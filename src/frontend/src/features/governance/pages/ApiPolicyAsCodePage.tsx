import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useMutation } from '@tanstack/react-query';
import { Shield } from 'lucide-react';
import { Card, CardBody, CardHeader } from '../../../components/Card';
import { Badge } from '../../../components/Badge';
import { PageContainer, PageSection } from '../../../components/shell';
import { PageHeader } from '../../../components/PageHeader';
import { Button } from '../../../components/Button';
import { TextField } from '../../../components/TextField';
import { Select } from '../../../components/Select';
import { TextArea } from '../../../components/TextArea';
import client from '../../../api/client';

// ── Types ──────────────────────────────────────────────────────────────────

interface RegisterPolicyRequest {
  name: string;
  displayName: string;
  description?: string;
  version: string;
  format: string;
  definitionContent: string;
  enforcementMode: string;
  registeredBy: string;
}

interface PolicyResponse {
  name: string;
  displayName: string;
  description?: string;
  version: string;
  format: string;
  enforcementMode: string;
  registeredBy: string;
  createdAt: string;
}

interface SimulatePolicyRequest {
  resourceType: string;
  resourceId: string;
  context: string;
}

interface SimulatePolicyResponse {
  isCompliant: boolean;
  violationMessages: string[];
}

// ── Constants ──────────────────────────────────────────────────────────────

const FORMATS = ['Yaml', 'Json'];
const ENFORCEMENT_MODES = ['Disabled', 'AuditOnly', 'Enforce'];

const EXAMPLE_YAML = `# Example API Policy
rules:
  - id: require-versioning
    description: All APIs must include a version prefix
    severity: error
    given: "$.paths"
    then:
      function: pattern
      functionOptions:
        match: "^/v[0-9]+"
  - id: require-description
    description: All operations must have a description
    severity: warning
    given: "$.paths[*][*]"
    then:
      field: description
      function: truthy`;

// ── Page ───────────────────────────────────────────────────────────────────

const initialRegisterForm: RegisterPolicyRequest = {
  name: '',
  displayName: '',
  description: '',
  version: '1.0.0',
  format: 'Yaml',
  definitionContent: EXAMPLE_YAML,
  enforcementMode: 'AuditOnly',
  registeredBy: '',
};

const initialSimulateForm: SimulatePolicyRequest = {
  resourceType: '',
  resourceId: '',
  context: '{}',
};

export function ApiPolicyAsCodePage() {
  const { t } = useTranslation();
  const [registerForm, setRegisterForm] = useState<RegisterPolicyRequest>(initialRegisterForm);
  const [simulatePolicyName, setSimulatePolicyName] = useState('');
  const [simulateForm, setSimulateForm] = useState<SimulatePolicyRequest>(initialSimulateForm);
  const [registeredPolicy, setRegisteredPolicy] = useState<PolicyResponse | null>(null);
  const [simulateResult, setSimulateResult] = useState<SimulatePolicyResponse | null>(null);

  const registerMutation = useMutation({
    mutationFn: (data: RegisterPolicyRequest) =>
      client
        .post<PolicyResponse>('/governance/policy-as-code', data)
        .then((r) => r.data),
    onSuccess: (data) => setRegisteredPolicy(data),
  });

  const simulateMutation = useMutation({
    mutationFn: ({ policyName, data }: { policyName: string; data: SimulatePolicyRequest }) =>
      client
        .post<SimulatePolicyResponse>(`/governance/policy-as-code/${policyName}/simulate`, data)
        .then((r) => r.data),
    onSuccess: (data) => setSimulateResult(data),
  });

  return (
    <PageContainer>
      <PageHeader
        title={t('apiPolicyAsCode.title')}
        subtitle={t('apiPolicyAsCode.subtitle')}
        icon={<Shield size={24} />}
      />

      {/* Info section */}
      <PageSection title={t('apiPolicyAsCode.overview')}>
        <Card>
          <CardBody>
            <p className="text-sm text-body leading-relaxed">
              {t('apiPolicyAsCode.subtitle')}
            </p>
            {/* Badges de modo de aplicação — substituem spans artesanais */}
            <div className="mt-3 flex flex-wrap gap-2">
              {ENFORCEMENT_MODES.map((mode) => (
                <Badge key={mode} variant="secondary">
                  {t(`apiPolicyAsCode.${mode.charAt(0).toLowerCase() + mode.slice(1)}` as never, {
                    defaultValue: mode,
                  })}
                </Badge>
              ))}
            </div>
          </CardBody>
        </Card>
      </PageSection>

      {/* Register Policy */}
      <PageSection title={t('apiPolicyAsCode.registerPolicy')}>
        {/* Banner de sucesso com tokens semânticos */}
        {registeredPolicy && (
          <div className="mb-4 flex items-center gap-2 rounded border border-success/30 bg-success/10 px-4 py-2">
            <Badge variant="success">{t('apiPolicyAsCode.createSuccess')}</Badge>
            <span className="text-sm font-medium text-success">{registeredPolicy.name}</span>
          </div>
        )}

        <Card>
          <CardBody>
            <form
              onSubmit={(e) => {
                e.preventDefault();
                registerMutation.mutate(registerForm);
              }}
              className="space-y-4"
            >
              {/* Campos de registo — raw inputs substituídos por DS TextField/Select/TextArea */}
              <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
                <TextField
                  size="sm"
                  label={t('apiPolicyAsCode.policyName')}
                  value={registerForm.name}
                  placeholder={t('governance.policyAsCode.placeholder.policyName', 'my-api-policy')}
                  onChange={(e) => setRegisterForm((f) => ({ ...f, name: e.target.value }))}
                  required
                />
                <TextField
                  size="sm"
                  label={t('apiPolicyAsCode.displayName')}
                  value={registerForm.displayName}
                  onChange={(e) => setRegisterForm((f) => ({ ...f, displayName: e.target.value }))}
                  required
                />
                <TextField
                  size="sm"
                  label={t('apiPolicyAsCode.version')}
                  value={registerForm.version}
                  placeholder={t('governance.policyAsCode.placeholder.version', '1.0.0')}
                  onChange={(e) => setRegisterForm((f) => ({ ...f, version: e.target.value }))}
                  required
                />
                <Select
                  size="sm"
                  label={t('apiPolicyAsCode.format')}
                  value={registerForm.format}
                  options={FORMATS.map((fmt) => ({
                    value: fmt,
                    label: t(`apiPolicyAsCode.${fmt.toLowerCase()}` as never, { defaultValue: fmt }),
                  }))}
                  onChange={(e) => setRegisterForm((f) => ({ ...f, format: e.target.value }))}
                />
                <Select
                  size="sm"
                  label={t('apiPolicyAsCode.enforcementMode')}
                  value={registerForm.enforcementMode}
                  options={ENFORCEMENT_MODES.map((mode) => ({
                    value: mode,
                    label: t(`apiPolicyAsCode.${mode.charAt(0).toLowerCase() + mode.slice(1)}` as never, {
                      defaultValue: mode,
                    }),
                  }))}
                  onChange={(e) => setRegisterForm((f) => ({ ...f, enforcementMode: e.target.value }))}
                />
                <TextField
                  size="sm"
                  label={t('apiPolicyAsCode.registeredBy')}
                  value={registerForm.registeredBy}
                  onChange={(e) => setRegisterForm((f) => ({ ...f, registeredBy: e.target.value }))}
                  required
                />
                <div className="sm:col-span-2">
                  <TextArea
                    label={t('apiPolicyAsCode.description')}
                    value={registerForm.description}
                    rows={2}
                    onChange={(e) => setRegisterForm((f) => ({ ...f, description: e.target.value }))}
                  />
                </div>
                <div className="sm:col-span-2">
                  <TextArea
                    label={t('apiPolicyAsCode.definitionContent')}
                    value={registerForm.definitionContent}
                    rows={10}
                    className="font-mono text-xs"
                    onChange={(e) => setRegisterForm((f) => ({ ...f, definitionContent: e.target.value }))}
                    required
                  />
                </div>
              </div>

              {registerMutation.isError && (
                <p className="text-sm text-critical">{t('apiPolicyAsCode.createError')}</p>
              )}

              <div className="flex justify-end">
                <Button type="submit" disabled={registerMutation.isPending}>
                  {registerMutation.isPending ? t('apiPolicyAsCode.loading') : t('apiPolicyAsCode.submit')}
                </Button>
              </div>
            </form>
          </CardBody>
        </Card>
      </PageSection>

      {/* Simulate Policy */}
      <PageSection title={t('apiPolicyAsCode.simulatePolicy')}>
        <Card>
          <CardBody>
            <form
              onSubmit={(e) => {
                e.preventDefault();
                simulateMutation.mutate({
                  policyName: simulatePolicyName,
                  data: simulateForm,
                });
              }}
              className="space-y-4"
            >
              {/* Campos de simulação — raw inputs substituídos por DS TextField/TextArea */}
              <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
                <TextField
                  size="sm"
                  label={t('apiPolicyAsCode.policyName')}
                  value={simulatePolicyName}
                  onChange={(e) => setSimulatePolicyName(e.target.value)}
                  required
                />
                <TextField
                  size="sm"
                  label={t('apiPolicyAsCode.resourceType')}
                  value={simulateForm.resourceType}
                  onChange={(e) => setSimulateForm((f) => ({ ...f, resourceType: e.target.value }))}
                  required
                />
                <TextField
                  size="sm"
                  label={t('apiPolicyAsCode.resourceId')}
                  value={simulateForm.resourceId}
                  onChange={(e) => setSimulateForm((f) => ({ ...f, resourceId: e.target.value }))}
                  required
                />
                <div className="sm:col-span-2">
                  <TextArea
                    label={t('apiPolicyAsCode.context')}
                    value={simulateForm.context}
                    rows={4}
                    className="font-mono text-xs"
                    onChange={(e) => setSimulateForm((f) => ({ ...f, context: e.target.value }))}
                  />
                </div>
              </div>

              {simulateMutation.isError && (
                <p className="text-sm text-critical">{t('apiPolicyAsCode.error')}</p>
              )}

              <div className="flex justify-end">
                <Button type="submit" disabled={simulateMutation.isPending}>
                  {simulateMutation.isPending ? t('apiPolicyAsCode.loading') : t('apiPolicyAsCode.simulate')}
                </Button>
              </div>
            </form>
          </CardBody>
        </Card>

        {simulateResult && (
          <Card>
            <CardHeader>
              <div className="flex items-center gap-2">
                <span className="text-sm font-medium text-heading">
                  {t('apiPolicyAsCode.isCompliant')}
                </span>
                <Badge variant={simulateResult.isCompliant ? 'success' : 'danger'}>
                  {simulateResult.isCompliant ? t('apiPolicyAsCode.simulateSuccess') : t('apiPolicyAsCode.error')}
                </Badge>
              </div>
            </CardHeader>
            <CardBody>
              {/* Resultado de violações com tokens semânticos */}
              {simulateResult.violationMessages.length === 0 ? (
                <p className="text-sm text-success">{t('apiPolicyAsCode.noViolations')}</p>
              ) : (
                <div>
                  <p className="mb-2 text-xs font-medium text-body">
                    {t('apiPolicyAsCode.violationMessages')}
                  </p>
                  <ul className="space-y-1">
                    {simulateResult.violationMessages.map((msg, i) => (
                      <li
                        key={i}
                        className="flex items-center gap-2 rounded bg-critical/10 px-3 py-1.5"
                      >
                        <span className="h-1.5 w-1.5 rounded-full bg-critical shrink-0" />
                        <span className="text-xs text-critical">{msg}</span>
                      </li>
                    ))}
                  </ul>
                </div>
              )}
            </CardBody>
          </Card>
        )}
      </PageSection>
    </PageContainer>
  );
}
