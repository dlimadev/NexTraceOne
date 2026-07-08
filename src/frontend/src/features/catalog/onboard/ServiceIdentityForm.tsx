import { useTranslation } from 'react-i18next';
import { TextField, TextArea, Select } from '../../../shared/ui';
import {
  type ServiceIdentityValues,
  SERVICE_TYPE_OPTIONS,
  CRITICALITY_OPTIONS,
  EXPOSURE_OPTIONS,
  type SelectOptionKey,
} from './onboardValidation';

interface ServiceIdentityFormProps {
  values: ServiceIdentityValues;
  errors: Partial<Record<keyof ServiceIdentityValues, string>>;
  onChange: <K extends keyof ServiceIdentityValues>(key: K, value: ServiceIdentityValues[K]) => void;
}

/**
 * Formulário controlado de Identidade & Ownership (passo 1 do onboarding).
 * Campos alinhados com serviceCatalogApi.registerService.
 */
export function ServiceIdentityForm({ values, errors, onChange }: ServiceIdentityFormProps) {
  const { t } = useTranslation();
  const opts = (list: SelectOptionKey[]) => list.map((o) => ({ value: o.value, label: t(o.labelKey) }));

  return (
    <div className="space-y-5 max-w-2xl">
      <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
        <TextField
          label={t('onboard.identity.name')}
          value={values.name}
          onChange={(e) => onChange('name', e.target.value)}
          required
          autoFocus
          error={errors.name}
          size="sm"
        />
        <TextField
          label={t('onboard.identity.domain')}
          value={values.domain}
          onChange={(e) => onChange('domain', e.target.value)}
          required
          error={errors.domain}
          size="sm"
        />
      </div>

      <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
        <TextField
          label={t('onboard.identity.team')}
          value={values.teamName}
          onChange={(e) => onChange('teamName', e.target.value)}
          required
          error={errors.teamName}
          size="sm"
        />
        <Select
          label={t('onboard.identity.serviceType')}
          value={values.serviceType}
          onChange={(e) => onChange('serviceType', e.target.value)}
          options={opts(SERVICE_TYPE_OPTIONS)}
          size="sm"
        />
      </div>

      <TextArea
        label={t('onboard.identity.description')}
        value={values.description}
        onChange={(e) => onChange('description', e.target.value)}
        rows={3}
      />

      <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
        <Select
          label={t('onboard.identity.criticality')}
          value={values.criticality}
          onChange={(e) => onChange('criticality', e.target.value)}
          options={opts(CRITICALITY_OPTIONS)}
          size="sm"
        />
        <Select
          label={t('onboard.identity.exposure')}
          value={values.exposureType}
          onChange={(e) => onChange('exposureType', e.target.value)}
          options={opts(EXPOSURE_OPTIONS)}
          size="sm"
        />
      </div>

      <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
        <TextField
          label={t('onboard.identity.technicalOwner')}
          value={values.technicalOwner}
          onChange={(e) => onChange('technicalOwner', e.target.value)}
          size="sm"
        />
        <TextField
          label={t('onboard.identity.businessOwner')}
          value={values.businessOwner}
          onChange={(e) => onChange('businessOwner', e.target.value)}
          size="sm"
        />
      </div>

      <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
        <TextField
          label={t('onboard.identity.documentationUrl')}
          type="url"
          value={values.documentationUrl}
          onChange={(e) => onChange('documentationUrl', e.target.value)}
          className="font-mono"
          size="sm"
        />
        <TextField
          label={t('onboard.identity.repositoryUrl')}
          type="url"
          value={values.repositoryUrl}
          onChange={(e) => onChange('repositoryUrl', e.target.value)}
          className="font-mono"
          size="sm"
        />
      </div>
    </div>
  );
}
