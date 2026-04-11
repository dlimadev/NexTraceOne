import { useState, useMemo } from 'react';
import { useTranslation } from 'react-i18next';
import { Shield, ShieldCheck, ShieldAlert, Lock, Key, Eye, Save } from 'lucide-react';
import { Card, CardBody, CardHeader } from '../../../../components/Card';

// ── Local types ───────────────────────────────────────────────────────────────

interface SecurityFields {
  authType: string;
  authorizationModel: string;
  scopes: string;
  roles: string;
  claims: string;
  dataClassification: string;
  maskingRequirements: string;
  auditRequirements: string;
  penTestState: string;
  securityReviewState: string;
  evidenceNotes: string;
}

interface SecuritySectionProps {
  specContent: string;
  protocol: string;
  isReadOnly?: boolean;
  onSave?: (fields: SecurityFields) => void;
  className?: string;
}

const AUTH_TYPES = ['None', 'JWT', 'OAuth2', 'OIDC', 'API Key', 'mTLS', 'Basic', 'SAML', 'Custom'] as const;
const REVIEW_STATES = ['Not Started', 'In Progress', 'Passed', 'Failed', 'Waived'] as const;
const DATA_CLASSIFICATIONS = ['Public', 'Internal', 'Confidential', 'Restricted'] as const;

/**
 * Secção de Segurança do workspace.
 * Permite definir/visualizar auth type, scopes, classificação de dados,
 * estado de revisão de segurança e requisitos de auditoria.
 */
export function SecuritySection({ specContent, protocol, isReadOnly = false, onSave, className = '' }: SecuritySectionProps) {
  const { t } = useTranslation();

  const extractedSecurity = useMemo(() => extractSecurityFromSpec(specContent, protocol), [specContent, protocol]);

  const [fields, setFields] = useState<SecurityFields>({
    authType: extractedSecurity.authType || 'None',
    authorizationModel: extractedSecurity.authorizationModel || '',
    scopes: extractedSecurity.scopes || '',
    roles: '',
    claims: '',
    dataClassification: 'Internal',
    maskingRequirements: '',
    auditRequirements: '',
    penTestState: 'Not Started',
    securityReviewState: 'Not Started',
    evidenceNotes: '',
  });

  const update = (key: keyof SecurityFields, value: string) => {
    setFields((prev) => ({ ...prev, [key]: value }));
  };

  const securityScore = useMemo(() => computeSecurityScore(fields), [fields]);

  return (
    <div className={`space-y-6 ${className}`}>
      {/* Security Score Banner */}
      <div className="flex items-center gap-4 p-4 rounded-lg bg-panel border border-edge">
        <div className="flex-shrink-0">
          {securityScore >= 80 ? (
            <ShieldCheck size={28} className="text-success" />
          ) : securityScore >= 50 ? (
            <Shield size={28} className="text-warning" />
          ) : (
            <ShieldAlert size={28} className="text-critical" />
          )}
        </div>
        <div className="flex-1">
          <div className="flex items-center gap-2">
            <h3 className="text-xs font-semibold text-heading">
              {t('contracts.security.score', 'Security Score')}
            </h3>
            <span className={`text-sm font-bold ${
              securityScore >= 80 ? 'text-success' : securityScore >= 50 ? 'text-warning' : 'text-critical'
            }`}>
              {securityScore}%
            </span>
          </div>
          <p className="text-[10px] text-muted mt-0.5">
            {securityScore >= 80
              ? t('contracts.security.scoreGood', 'Security posture is strong.')
              : securityScore >= 50
                ? t('contracts.security.scoreModerate', 'Some security aspects need attention.')
                : t('contracts.security.scoreLow', 'Significant security gaps detected.')}
          </p>
        </div>
        {/* Progress bar */}
        <div className="w-32">
          <div className="h-1.5 bg-elevated rounded-full overflow-hidden">
            <div
              className={`h-full rounded-full transition-all ${
                securityScore >= 80 ? 'bg-success/15' : securityScore >= 50 ? 'bg-warning/15' : 'bg-critical/15'
              }`}
              style={{ width: `${securityScore}%` }}
            />
          </div>
        </div>
      </div>

      {/* Authentication & Authorization */}
      <Card>
        <CardHeader>
          <div className="flex items-center gap-2">
            <Key size={14} className="text-accent" />
            <h3 className="text-xs font-semibold text-heading">
              {t('contracts.security.authTitle', 'Authentication & Authorization')}
            </h3>
          </div>
        </CardHeader>
        <CardBody className="space-y-3">
          <div className="grid grid-cols-2 gap-3">
            <SelectField
              label={t('contracts.security.authType', 'Auth Type')}
              value={fields.authType}
              onChange={(v) => update('authType', v)}
              disabled={isReadOnly}
              options={[...AUTH_TYPES]}
            />
            <Field
              label={t('contracts.security.authorizationModel', 'Authorization Model')}
              value={fields.authorizationModel}
              onChange={(v) => update('authorizationModel', v)}
              disabled={isReadOnly}
              placeholder={t('contracts.security.placeholder.authMechanisms', 'RBAC, ABAC, Scope-based...')}
            />
          </div>
          <Field
            label={t('contracts.security.scopes', 'Scopes')}
            value={fields.scopes}
            onChange={(v) => update('scopes', v)}
            disabled={isReadOnly}
            placeholder={t('contracts.security.placeholder.scopes', 'read:users, write:users, admin')}
          />
          <div className="grid grid-cols-2 gap-3">
            <Field
              label={t('contracts.security.roles', 'Roles')}
              value={fields.roles}
              onChange={(v) => update('roles', v)}
              disabled={isReadOnly}
              placeholder={t('contracts.security.placeholder.roles', 'Admin, Editor, Viewer')}
            />
            <Field
              label={t('contracts.security.claims', 'Claims')}
              value={fields.claims}
              onChange={(v) => update('claims', v)}
              disabled={isReadOnly}
              placeholder={t('contracts.security.placeholder.claims', 'sub, aud, tenant_id')}
            />
          </div>
        </CardBody>
      </Card>

      {/* Data Classification & Privacy */}
      <Card>
        <CardHeader>
          <div className="flex items-center gap-2">
            <Eye size={14} className="text-accent" />
            <h3 className="text-xs font-semibold text-heading">
              {t('contracts.security.dataTitle', 'Data Classification & Privacy')}
            </h3>
          </div>
        </CardHeader>
        <CardBody className="space-y-3">
          <SelectField
            label={t('contracts.security.dataClassification', 'Data Classification')}
            value={fields.dataClassification}
            onChange={(v) => update('dataClassification', v)}
            disabled={isReadOnly}
            options={[...DATA_CLASSIFICATIONS]}
          />
          <FieldArea
            label={t('contracts.security.maskingRequirements', 'Masking Requirements')}
            value={fields.maskingRequirements}
            onChange={(v) => update('maskingRequirements', v)}
            disabled={isReadOnly}
            placeholder={t('contracts.security.maskingPlaceholder', 'Fields that require masking in logs and responses (e.g., email, SSN, card number)')}
          />
          <FieldArea
            label={t('contracts.security.auditRequirements', 'Audit Requirements')}
            value={fields.auditRequirements}
            onChange={(v) => update('auditRequirements', v)}
            disabled={isReadOnly}
            placeholder={t('contracts.security.auditPlaceholder', 'What operations must be audited? Retention requirements?')}
          />
        </CardBody>
      </Card>

      {/* Review State & Evidence */}
      <Card>
        <CardHeader>
          <div className="flex items-center gap-2">
            <Lock size={14} className="text-accent" />
            <h3 className="text-xs font-semibold text-heading">
              {t('contracts.security.reviewTitle', 'Security Review & Evidence')}
            </h3>
          </div>
        </CardHeader>
        <CardBody className="space-y-3">
          <div className="grid grid-cols-2 gap-3">
            <SelectField
              label={t('contracts.security.penTestState', 'Penetration Test')}
              value={fields.penTestState}
              onChange={(v) => update('penTestState', v)}
              disabled={isReadOnly}
              options={[...REVIEW_STATES]}
            />
            <SelectField
              label={t('contracts.security.securityReviewState', 'Security Review')}
              value={fields.securityReviewState}
              onChange={(v) => update('securityReviewState', v)}
              disabled={isReadOnly}
              options={[...REVIEW_STATES]}
            />
          </div>

          {/* State indicators */}
          <div className="flex items-center gap-3">
            <StateIndicator label={t('contracts.security.penTestState', 'Pen Test')} state={fields.penTestState} />
            <StateIndicator label={t('contracts.security.securityReviewState', 'Security Review')} state={fields.securityReviewState} />
          </div>

          <FieldArea
            label={t('contracts.security.evidenceNotes', 'Evidence & Notes')}
            value={fields.evidenceNotes}
            onChange={(v) => update('evidenceNotes', v)}
            disabled={isReadOnly}
            placeholder={t('contracts.security.evidencePlaceholder', 'Links to reports, JIRA tickets, compliance documents...')}
          />
        </CardBody>
      </Card>

      {!isReadOnly && onSave && (
        <div className="flex justify-end">
          <button
            onClick={() => onSave(fields)}
            className="inline-flex items-center gap-1.5 px-4 py-2 text-xs font-medium rounded-md bg-accent text-white hover:bg-accent/90 transition-colors"
          >
            <Save size={12} /> {t('common.save', 'Save')}
          </button>
        </div>
      )}
    </div>
  );
}

// ── Reusable primitives ───────────────────────────────────────────────────────

function Field({ label, value, onChange, placeholder, disabled }: { label: string; value: string; onChange: (v: string) => void; placeholder?: string; disabled?: boolean }) {
  return (
    <div>
      <label className="block text-[10px] font-medium text-muted mb-0.5">{label}</label>
      <input type="text" value={value} onChange={(e) => onChange(e.target.value)} placeholder={placeholder} disabled={disabled} className="w-full text-xs bg-elevated border border-edge rounded px-2 py-1.5 text-body placeholder:text-muted/40 focus:outline-none focus:ring-1 focus:ring-accent disabled:opacity-50 disabled:cursor-not-allowed" />
    </div>
  );
}

function FieldArea({ label, value, onChange, placeholder, disabled, rows = 3 }: { label: string; value: string; onChange: (v: string) => void; placeholder?: string; disabled?: boolean; rows?: number }) {
  return (
    <div>
      <label className="block text-[10px] font-medium text-muted mb-0.5">{label}</label>
      <textarea value={value} onChange={(e) => onChange(e.target.value)} placeholder={placeholder} disabled={disabled} rows={rows} className="w-full text-xs bg-elevated border border-edge rounded px-2 py-1.5 text-body placeholder:text-muted/40 focus:outline-none focus:ring-1 focus:ring-accent resize-none disabled:opacity-50 disabled:cursor-not-allowed" />
    </div>
  );
}

function SelectField({ label, value, onChange, options, disabled }: { label: string; value: string; onChange: (v: string) => void; options: string[]; disabled?: boolean }) {
  return (
    <div>
      <label className="block text-[10px] font-medium text-muted mb-0.5">{label}</label>
      <select value={value} onChange={(e) => onChange(e.target.value)} disabled={disabled} className="w-full text-xs bg-elevated border border-edge rounded px-2 py-1.5 text-body focus:outline-none focus:ring-1 focus:ring-accent disabled:opacity-50 disabled:cursor-not-allowed">
        {options.map((o) => <option key={o} value={o}>{o}</option>)}
      </select>
    </div>
  );
}

function StateIndicator({ label, state }: { label: string; state: string }) {
  const color =
    state === 'Passed' ? 'text-success' :
    state === 'In Progress' ? 'text-info' :
    state === 'Failed' ? 'text-critical' :
    state === 'Waived' ? 'text-warning' :
    'text-muted';

  const bg =
    state === 'Passed' ? 'bg-success/15 border-success/25' :
    state === 'In Progress' ? 'bg-info/15 border-info/25' :
    state === 'Failed' ? 'bg-critical/15 border-critical/25' :
    state === 'Waived' ? 'bg-warning/15 border-warning/25' :
    'bg-elevated border-edge';

  return (
    <span className={`inline-flex items-center gap-1.5 px-2.5 py-1 text-[10px] font-medium rounded border ${color} ${bg}`}>
      <span className={`w-1.5 h-1.5 rounded-full ${color.replace('text-', 'bg-')}`} />
      {label}: {state}
    </span>
  );
}

// ── Helpers ───────────────────────────────────────────────────────────────────

// eslint-disable-next-line @typescript-eslint/no-unused-vars
function extractSecurityFromSpec(specContent: string, _protocol: string): { authType: string; authorizationModel: string; scopes: string } {
  if (!specContent?.trim()) return { authType: '', authorizationModel: '', scopes: '' };

  try {
    const trimmed = specContent.trim();
    if (!trimmed.startsWith('{')) return { authType: '', authorizationModel: '', scopes: '' };

    const parsed = JSON.parse(trimmed);
    const securitySchemes = parsed.components?.securitySchemes ?? parsed.securityDefinitions ?? {};

    let authType = '';
    const scopes: string[] = [];

    for (const [, scheme] of Object.entries(securitySchemes)) {
      const s = scheme as Record<string, unknown>;
      if (s.type === 'oauth2' || s.type === 'openIdConnect') {
        authType = s.type === 'openIdConnect' ? 'OIDC' : 'OAuth2';
        const flows = s.flows as Record<string, Record<string, unknown>> | undefined;
        if (flows) {
          for (const flow of Object.values(flows)) {
            if (flow.scopes && typeof flow.scopes === 'object') {
              scopes.push(...Object.keys(flow.scopes as Record<string, unknown>));
            }
          }
        }
      } else if (s.type === 'http') {
        authType = (s.scheme as string)?.toLowerCase() === 'bearer' ? 'JWT' : 'Basic';
      } else if (s.type === 'apiKey') {
        authType = 'API Key';
      }
    }

    return {
      authType,
      authorizationModel: authType ? 'Scope-based' : '',
      scopes: scopes.join(', '),
    };
  } catch {
    return { authType: '', authorizationModel: '', scopes: '' };
  }
}

function computeSecurityScore(fields: SecurityFields): number {
  let score = 0;
  const total = 10;

  if (fields.authType && fields.authType !== 'None') score += 2;
  if (fields.authorizationModel) score += 1;
  if (fields.scopes) score += 1;
  if (fields.dataClassification && fields.dataClassification !== 'Public') score += 1;
  if (fields.maskingRequirements) score += 1;
  if (fields.auditRequirements) score += 1;
  if (fields.penTestState === 'Passed') score += 1;
  if (fields.securityReviewState === 'Passed') score += 1;
  if (fields.evidenceNotes) score += 1;

  return Math.round((score / total) * 100);
}
