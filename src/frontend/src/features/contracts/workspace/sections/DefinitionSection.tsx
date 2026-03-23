import { useState, useEffect } from 'react';
import { useTranslation } from 'react-i18next';
import { Save, FileText, Building2, Shield, Link as LinkIcon, Calendar } from 'lucide-react';
import { Card, CardBody, CardHeader } from '../../../../components/Card';
import { SERVICE_TYPES, LIFECYCLE_STATES } from '../../shared/constants';
import type { StudioContract } from '../studioTypes';

interface DefinitionSectionProps {
  contract: StudioContract;
  isReadOnly?: boolean;
  onSave?: (fields: DefinitionFields) => void;
  className?: string;
}

export interface DefinitionFields {
  technicalName: string;
  friendlyName: string;
  functionalDescription: string;
  technicalDescription: string;
  serviceType: string;
  domain: string;
  capability: string;
  product: string;
  owner: string;
  team: string;
  visibility: string;
  criticality: string;
  dataClassification: string;
  tags: string;
  lifecycle: string;
  sla: string;
  slo: string;
  externalLinks: string;
  createdAt: string;
}

/**
 * Secção de definição do studio — formulário forte de metadados do serviço/contrato.
 * Contém nome técnico, nome amigável, descrições, organização, classificação,
 * SLA/SLO, lifecycle, datas relevantes e links externos.
 * NUNCA exige GUID ou IDs técnicos ao utilizador.
 */
export function DefinitionSection({ contract, isReadOnly = false, onSave, className = '' }: DefinitionSectionProps) {
  const { t } = useTranslation();
  const [fields, setFields] = useState<DefinitionFields>(() => ({
    technicalName: contract.technicalName,
    friendlyName: contract.friendlyName,
    functionalDescription: contract.functionalDescription,
    technicalDescription: contract.technicalDescription,
    serviceType: contract.serviceType,
    domain: contract.domain,
    capability: contract.capability,
    product: contract.product,
    owner: contract.owner,
    team: contract.team,
    visibility: contract.visibility,
    criticality: contract.criticality,
    dataClassification: contract.dataClassification,
    tags: contract.tags.join(', '),
    lifecycle: contract.lifecycleState,
    sla: contract.sla,
    slo: contract.slo,
    externalLinks: contract.externalLinks.join('\n'),
    createdAt: contract.createdAt,
  }));

  useEffect(() => {
    // eslint-disable-next-line react-hooks/set-state-in-effect -- sync local form state from contract prop is intentional
    setFields({
      technicalName: contract.technicalName,
      friendlyName: contract.friendlyName,
      functionalDescription: contract.functionalDescription,
      technicalDescription: contract.technicalDescription,
      serviceType: contract.serviceType,
      domain: contract.domain,
      capability: contract.capability,
      product: contract.product,
      owner: contract.owner,
      team: contract.team,
      visibility: contract.visibility,
      criticality: contract.criticality,
      dataClassification: contract.dataClassification,
      tags: contract.tags.join(', '),
      lifecycle: contract.lifecycleState,
      sla: contract.sla,
      slo: contract.slo,
      externalLinks: contract.externalLinks.join('\n'),
      createdAt: contract.createdAt,
    });
  }, [contract]);

  const update = (key: keyof DefinitionFields, value: string) => {
    setFields((prev) => ({ ...prev, [key]: value }));
  };

  return (
    <div className={`space-y-6 ${className}`}>
      {/* ── Identity ── */}
      <Card>
        <CardHeader>
          <div className="flex items-center gap-2">
            <FileText size={14} className="text-accent" />
            <h3 className="text-xs font-semibold text-heading">
              {t('contracts.studio.definition.identity', 'Service Identity')}
            </h3>
          </div>
        </CardHeader>
        <CardBody className="space-y-3">
          <div className="grid grid-cols-2 gap-3">
            <Field label={t('contracts.studio.definition.technicalName', 'Technical Name')} value={fields.technicalName} onChange={(v) => update('technicalName', v)} disabled={isReadOnly} />
            <Field label={t('contracts.studio.definition.friendlyName', 'Friendly Name')} value={fields.friendlyName} onChange={(v) => update('friendlyName', v)} disabled={isReadOnly} placeholder="e.g., User Management API" />
          </div>
          <FieldArea label={t('contracts.studio.definition.functionalDescription', 'Functional Description')} value={fields.functionalDescription} onChange={(v) => update('functionalDescription', v)} disabled={isReadOnly} placeholder="What business capability does this service provide?" />
          <FieldArea label={t('contracts.studio.definition.technicalDescription', 'Technical Description')} value={fields.technicalDescription} onChange={(v) => update('technicalDescription', v)} disabled={isReadOnly} placeholder="Technical architecture, stack, integration notes..." />
          <div className="grid grid-cols-2 gap-3">
            <SelectField
              label={t('contracts.studio.definition.serviceType', 'Service Type')}
              value={fields.serviceType}
              onChange={(v) => update('serviceType', v)}
              disabled={isReadOnly}
              options={SERVICE_TYPES.map((st) => st.value)}
            />
            <SelectField
              label={t('contracts.studio.definition.lifecycle', 'Lifecycle State')}
              value={fields.lifecycle}
              onChange={(v) => update('lifecycle', v)}
              disabled={true}
              options={[...LIFECYCLE_STATES]}
            />
          </div>
        </CardBody>
      </Card>

      {/* ── Organization ── */}
      <Card>
        <CardHeader>
          <div className="flex items-center gap-2">
            <Building2 size={14} className="text-accent" />
            <h3 className="text-xs font-semibold text-heading">
              {t('contracts.studio.definition.organization', 'Organization & Ownership')}
            </h3>
          </div>
        </CardHeader>
        <CardBody className="space-y-3">
          <div className="grid grid-cols-3 gap-3">
            <Field label={t('contracts.studio.definition.domain', 'Domain')} value={fields.domain} onChange={(v) => update('domain', v)} disabled={isReadOnly} placeholder="Payments" />
            <Field label={t('contracts.studio.definition.capability', 'Capability')} value={fields.capability} onChange={(v) => update('capability', v)} disabled={isReadOnly} placeholder="Payment Processing" />
            <Field label={t('contracts.studio.definition.product', 'Product')} value={fields.product} onChange={(v) => update('product', v)} disabled={isReadOnly} placeholder="Checkout Platform" />
          </div>
          <div className="grid grid-cols-2 gap-3">
            <Field label={t('contracts.studio.definition.owner', 'Owner')} value={fields.owner} onChange={(v) => update('owner', v)} disabled={isReadOnly} placeholder="john.doe@company.com" />
            <Field label={t('contracts.studio.definition.team', 'Team')} value={fields.team} onChange={(v) => update('team', v)} disabled={isReadOnly} placeholder="Team Payments" />
          </div>
        </CardBody>
      </Card>

      {/* ── Classification ── */}
      <Card>
        <CardHeader>
          <div className="flex items-center gap-2">
            <Shield size={14} className="text-accent" />
            <h3 className="text-xs font-semibold text-heading">
              {t('contracts.studio.definition.classification', 'Classification & Governance')}
            </h3>
          </div>
        </CardHeader>
        <CardBody className="space-y-3">
          <div className="grid grid-cols-3 gap-3">
            <SelectField label={t('contracts.studio.definition.visibility', 'Visibility')} value={fields.visibility} onChange={(v) => update('visibility', v)} disabled={isReadOnly} options={['Public', 'Internal', 'Private', 'Partner']} />
            <SelectField label={t('contracts.studio.definition.criticality', 'Criticality')} value={fields.criticality} onChange={(v) => update('criticality', v)} disabled={isReadOnly} options={['Critical', 'High', 'Medium', 'Low']} />
            <SelectField label={t('contracts.studio.definition.dataClassification', 'Data Classification')} value={fields.dataClassification} onChange={(v) => update('dataClassification', v)} disabled={isReadOnly} options={['Public', 'Internal', 'Confidential', 'Restricted']} />
          </div>
          <Field label={t('contracts.studio.definition.tags', 'Tags')} value={fields.tags} onChange={(v) => update('tags', v)} disabled={isReadOnly} placeholder="payments, checkout, pci (comma separated)" />
        </CardBody>
      </Card>

      {/* ── SLA/SLO & Links ── */}
      <Card>
        <CardHeader>
          <div className="flex items-center gap-2">
            <LinkIcon size={14} className="text-accent" />
            <h3 className="text-xs font-semibold text-heading">
              {t('contracts.studio.definition.slaLinks', 'SLA / SLO & External Links')}
            </h3>
          </div>
        </CardHeader>
        <CardBody className="space-y-3">
          <div className="grid grid-cols-2 gap-3">
            <Field label={t('contracts.studio.definition.sla', 'SLA')} value={fields.sla} onChange={(v) => update('sla', v)} disabled={isReadOnly} placeholder="99.9% uptime" />
            <Field label={t('contracts.studio.definition.slo', 'SLO')} value={fields.slo} onChange={(v) => update('slo', v)} disabled={isReadOnly} placeholder="p99 < 200ms" />
          </div>
          <FieldArea label={t('contracts.studio.definition.externalLinks', 'External Links')} value={fields.externalLinks} onChange={(v) => update('externalLinks', v)} disabled={isReadOnly} placeholder="Runbook, wiki, monitoring dashboard URLs (one per line)" rows={2} />
        </CardBody>
      </Card>

      {/* ── Dates ── */}
      <Card>
        <CardHeader>
          <div className="flex items-center gap-2">
            <Calendar size={14} className="text-accent" />
            <h3 className="text-xs font-semibold text-heading">
              {t('contracts.studio.definition.dates', 'Relevant Dates')}
            </h3>
          </div>
        </CardHeader>
        <CardBody>
          <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
            <DateInfo label={t('contracts.createdAt', 'Created')} value={contract.createdAt} />
            {contract.lockedAt && <DateInfo label={t('contracts.locked', 'Locked')} value={contract.lockedAt} />}
            {contract.signedAt && <DateInfo label={t('contracts.signed', 'Signed')} value={contract.signedAt} />}
            {contract.sunsetDate && <DateInfo label={t('contracts.sunsetDate', 'Sunset')} value={contract.sunsetDate} />}
          </div>
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

// ── Form primitives ───────────────────────────────────────────────────────────

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

function DateInfo({ label, value }: { label: string; value: string }) {
  let formatted = value;
  try {
    formatted = new Date(value).toLocaleDateString(undefined, {
      year: 'numeric', month: 'short', day: 'numeric',
    });
  } catch { /* keep raw value */ }

  return (
    <div>
      <p className="text-[10px] text-muted mb-0.5">{label}</p>
      <p className="text-xs text-body font-medium">{formatted}</p>
    </div>
  );
}
