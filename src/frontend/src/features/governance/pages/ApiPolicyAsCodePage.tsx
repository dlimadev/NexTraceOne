import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useMutation } from '@tanstack/react-query';
import { Shield } from 'lucide-react';
import { Card, CardBody, CardHeader } from '../../../components/Card';
import { Badge } from '../../../components/Badge';
import { PageContainer, PageSection } from '../../../components/shell';
import { PageHeader } from '../../../components/PageHeader';
import { Button } from '../../../components/Button';
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

  const inputClass =
    'w-full rounded border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 px-3 py-2 text-sm text-gray-900 dark:text-white focus:outline-none focus:ring-2 focus:ring-blue-400';
  const labelClass = 'mb-1 block text-xs font-medium text-gray-700 dark:text-gray-300';
  const codeClass = `${inputClass} font-mono text-xs`;

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
            <p className="text-sm text-gray-700 dark:text-gray-300 leading-relaxed">
              {t('apiPolicyAsCode.subtitle')}
            </p>
            <div className="mt-3 flex flex-wrap gap-2">
              {ENFORCEMENT_MODES.map((mode) => (
                <span
                  key={mode}
                  className="rounded-full border border-gray-200 dark:border-gray-700 px-3 py-0.5 text-xs text-gray-600 dark:text-gray-400"
                >
                  {t(`apiPolicyAsCode.${mode.charAt(0).toLowerCase() + mode.slice(1)}` as never, {
                    defaultValue: mode,
                  })}
                </span>
              ))}
            </div>
          </CardBody>
        </Card>
      </PageSection>

      {/* Register Policy */}
      <PageSection title={t('apiPolicyAsCode.registerPolicy')}>
        {registeredPolicy && (
          <div className="mb-4 flex items-center gap-2 rounded border border-green-300 dark:border-green-700 bg-green-50 dark:bg-green-900/20 px-4 py-2">
            <Badge variant="success">{t('apiPolicyAsCode.createSuccess')}</Badge>
            <span className="text-sm font-medium text-green-800 dark:text-green-300">{registeredPolicy.name}</span>
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
              <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
                <div>
                  <label className={labelClass}>{t('apiPolicyAsCode.policyName')}</label>
                  <input
                    type="text"
                    className={inputClass}
                    value={registerForm.name}
                    placeholder={t('governance.policyAsCode.placeholder.policyName', 'my-api-policy')}
                    onChange={(e) => setRegisterForm((f) => ({ ...f, name: e.target.value }))}
                    required
                  />
                </div>
                <div>
                  <label className={labelClass}>{t('apiPolicyAsCode.displayName')}</label>
                  <input
                    type="text"
                    className={inputClass}
                    value={registerForm.displayName}
                    onChange={(e) => setRegisterForm((f) => ({ ...f, displayName: e.target.value }))}
                    required
                  />
                </div>
                <div>
                  <label className={labelClass}>{t('apiPolicyAsCode.version')}</label>
                  <input
                    type="text"
                    className={inputClass}
                    value={registerForm.version}
                    placeholder={t('governance.policyAsCode.placeholder.version', '1.0.0')}
                    onChange={(e) => setRegisterForm((f) => ({ ...f, version: e.target.value }))}
                    required
                  />
                </div>
                <div>
                  <label className={labelClass}>{t('apiPolicyAsCode.format')}</label>
                  <select
                    className={inputClass}
                    value={registerForm.format}
                    onChange={(e) => setRegisterForm((f) => ({ ...f, format: e.target.value }))}
                  >
                    {FORMATS.map((fmt) => (
                      <option key={fmt} value={fmt}>
                        {t(`apiPolicyAsCode.${fmt.toLowerCase()}` as never, { defaultValue: fmt })}
                      </option>
                    ))}
                  </select>
                </div>
                <div>
                  <label className={labelClass}>{t('apiPolicyAsCode.enforcementMode')}</label>
                  <select
                    className={inputClass}
                    value={registerForm.enforcementMode}
                    onChange={(e) => setRegisterForm((f) => ({ ...f, enforcementMode: e.target.value }))}
                  >
                    {ENFORCEMENT_MODES.map((mode) => (
                      <option key={mode} value={mode}>
                        {t(`apiPolicyAsCode.${mode.charAt(0).toLowerCase() + mode.slice(1)}` as never, {
                          defaultValue: mode,
                        })}
                      </option>
                    ))}
                  </select>
                </div>
                <div>
                  <label className={labelClass}>{t('apiPolicyAsCode.registeredBy')}</label>
                  <input
                    type="text"
                    className={inputClass}
                    value={registerForm.registeredBy}
                    onChange={(e) => setRegisterForm((f) => ({ ...f, registeredBy: e.target.value }))}
                    required
                  />
                </div>
                <div className="sm:col-span-2">
                  <label className={labelClass}>{t('apiPolicyAsCode.description')}</label>
                  <textarea
                    className={inputClass}
                    value={registerForm.description}
                    rows={2}
                    onChange={(e) => setRegisterForm((f) => ({ ...f, description: e.target.value }))}
                  />
                </div>
                <div className="sm:col-span-2">
                  <label className={labelClass}>{t('apiPolicyAsCode.definitionContent')}</label>
                  <textarea
                    className={codeClass}
                    value={registerForm.definitionContent}
                    rows={10}
                    onChange={(e) => setRegisterForm((f) => ({ ...f, definitionContent: e.target.value }))}
                    required
                  />
                </div>
              </div>

              {registerMutation.isError && (
                <p className="text-sm text-red-600 dark:text-red-400">{t('apiPolicyAsCode.createError')}</p>
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
              <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
                <div>
                  <label className={labelClass}>{t('apiPolicyAsCode.policyName')}</label>
                  <input
                    type="text"
                    className={inputClass}
                    value={simulatePolicyName}
                    onChange={(e) => setSimulatePolicyName(e.target.value)}
                    required
                  />
                </div>
                <div>
                  <label className={labelClass}>{t('apiPolicyAsCode.resourceType')}</label>
                  <input
                    type="text"
                    className={inputClass}
                    value={simulateForm.resourceType}
                    onChange={(e) => setSimulateForm((f) => ({ ...f, resourceType: e.target.value }))}
                    required
                  />
                </div>
                <div>
                  <label className={labelClass}>{t('apiPolicyAsCode.resourceId')}</label>
                  <input
                    type="text"
                    className={inputClass}
                    value={simulateForm.resourceId}
                    onChange={(e) => setSimulateForm((f) => ({ ...f, resourceId: e.target.value }))}
                    required
                  />
                </div>
                <div className="sm:col-span-2">
                  <label className={labelClass}>{t('apiPolicyAsCode.context')}</label>
                  <textarea
                    className={codeClass}
                    value={simulateForm.context}
                    rows={4}
                    onChange={(e) => setSimulateForm((f) => ({ ...f, context: e.target.value }))}
                  />
                </div>
              </div>

              {simulateMutation.isError && (
                <p className="text-sm text-red-600 dark:text-red-400">{t('apiPolicyAsCode.error')}</p>
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
                <span className="text-sm font-medium text-gray-900 dark:text-white">
                  {t('apiPolicyAsCode.isCompliant')}
                </span>
                <Badge variant={simulateResult.isCompliant ? 'success' : 'danger'}>
                  {simulateResult.isCompliant ? t('apiPolicyAsCode.simulateSuccess') : t('apiPolicyAsCode.error')}
                </Badge>
              </div>
            </CardHeader>
            <CardBody>
              {simulateResult.violationMessages.length === 0 ? (
                <p className="text-sm text-green-600 dark:text-green-400">{t('apiPolicyAsCode.noViolations')}</p>
              ) : (
                <div>
                  <p className="mb-2 text-xs font-medium text-gray-700 dark:text-gray-300">
                    {t('apiPolicyAsCode.violationMessages')}
                  </p>
                  <ul className="space-y-1">
                    {simulateResult.violationMessages.map((msg, i) => (
                      <li
                        key={i}
                        className="flex items-center gap-2 rounded bg-red-50 dark:bg-red-900/20 px-3 py-1.5"
                      >
                        <span className="h-1.5 w-1.5 rounded-full bg-red-500 shrink-0" />
                        <span className="text-xs text-red-700 dark:text-red-300">{msg}</span>
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
