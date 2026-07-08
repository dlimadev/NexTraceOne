# Onboarding Journey (J2) — Fase 1 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build a single guided end-to-end "Onboard a service" wizard at `/services/onboard` (register service → optional interface → optional contract → review), wire the dead entry points to it, and remove the fragmented dead paths (orphan overlays + unlinked studio routes).

**Architecture:** A full-page stepper (`OnboardWizardPage`) driven by one orchestration hook (`useOnboardWizard`). The service is persisted early (created on leaving Step 1, lifecycle `Planning`); interface and contract steps attach to the already-created service and are individually skippable. Step content reuses existing presentational pieces: a new extracted `ServiceIdentityForm` and `ServiceInterfaceForm`, plus the existing `TypeModeTab`/`DetailsTab` bound to the created service. A small `onCreated` seam on `useContractDraftForm` lets the wizard intercept draft creation (go to Review) instead of the studio redirect.

**Tech Stack:** React 19, TypeScript 5.9, react-router-dom 7 (`useNavigate`), TanStack Query 5, Zod 4, Vitest + Testing Library, Playwright (mock backend), i18next (4 locales: en, es, pt-BR, pt-PT). Design system from `src/frontend/src/shared/ui` barrel (`Button`, `TextField`, `TextArea`, `Select`, `Checkbox`) + `src/frontend/src/components`.

## Global Constraints

- Spec: `docs/superpowers/specs/2026-07-08-catalog-onboarding-journey-j2-design.md`. Fase 1 only — do NOT build Fase 2.
- Route for the wizard is exactly `/services/onboard`. `/services/new` redirects to it.
- Service is created on leaving Step 1 via `serviceCatalogApi.registerService`; new services are `Planning` lifecycle. Interface/contract steps attach to the existing `serviceId`.
- No hardcoded UI strings — every user-facing string is an i18n key present in ALL 4 locales (`en.json`, `es.json`, `pt-BR.json`, `pt-PT.json`). `npm run validate:i18n` must pass.
- Honest-null: never invent data; skipped optional steps render "—" / omitted in the review, never fabricated values.
- DS + semantic tokens only (no raw hex). Reuse `shared/ui` components; match existing dense form idiom (`size="sm"` where the surrounding forms use it).
- Tests live centrally under `src/frontend/src/__tests__/**` (co-located tests are NOT picked up by `vite.config` include). Place all new unit tests there.
- Tooling: run unit tests with `npm run test` (never bare `npx vitest`). Final gate is `npm run build` (catches what `tsc --noEmit` misses). e2e: `npx playwright test --project=chromium <spec>` with `$env:CI=""`, prefixed by `Set-Location "C:\Users\dlima\Documents\GitHub\NexTraceOne\src\frontend"`. Playwright URL globs must use `**` (a `*` stops at `/`).
- All commands run from `src/frontend` unless stated. End every commit message with `Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>`. This is a solo branch — merge direct to `main`, no PR.

---

## File Structure

New feature directory: `src/frontend/src/features/catalog/onboard/`

- `onboardValidation.ts` — Zod schemas + value types + empty constants for identity & interface steps.
- `ServiceIdentityForm.tsx` — controlled presentational form for `registerService` fields (Step 1).
- `ServiceInterfaceForm.tsx` — controlled presentational form for interface fields (Step 2); also consumed by the refactored `CreateServiceInterfacePage`.
- `OnboardIdentityPreview.tsx` — live service-identity preview card (left rail).
- `OnboardWizardShell.tsx` — presentational shell: progress rail + preview + content slot + footer (Back/Skip/Next-Finish).
- `OnboardReviewStep.tsx` — honest-null summary of what was/will be created.
- `useOnboardWizard.ts` — orchestration hook (step state, early service creation, interface/contract mutations, skip, finish).
- `OnboardWizardPage.tsx` — route element wiring the hook + shell + steps.

Modified:
- `src/frontend/src/features/contracts/create/useContractDraftForm.ts` — add optional `onCreated` seam.
- `src/frontend/src/features/catalog/pages/CreateServiceInterfacePage.tsx` — consume `ServiceInterfaceForm`.
- `src/frontend/src/routes/catalogRoutes.tsx` — add `/services/onboard`, redirect `/services/new`.
- `src/frontend/src/routes/contractsRoutes.tsx` — remove dead studio type routes.
- `src/frontend/src/features/catalog/pages/ServiceCatalogPage.tsx` — wire dead CTA.
- `src/frontend/src/features/catalog/pages/ServiceCatalogListPage.tsx` — point CTA at `/services/onboard`.
- `src/frontend/src/features/catalog/browse/ServiceBrowseSurface.tsx` — empty-state CTA (thread `onRegisterService`).
- `src/frontend/src/locales/{en,es,pt-BR,pt-PT}.json` — `onboard.*` keys.

Removed (Task 8):
- `src/frontend/src/features/catalog/components/{ServiceRegistrationOverlay,ContractImportOverlay,ServiceInterfaceOverlay,WizardOverlay}.tsx`
- `src/frontend/src/__tests__/catalog/{ServiceRegistrationOverlay,ContractImportOverlay,ServiceInterfaceOverlay,WizardOverlay}.test.tsx`

---

## Task 1: i18n foundation — `onboard.*` keys in 4 locales

**Files:**
- Modify: `src/frontend/src/locales/en.json`, `es.json`, `pt-BR.json`, `pt-PT.json`

**Interfaces:**
- Produces: the `onboard.*` key namespace consumed by every later task. Exact keys/values below.

- [ ] **Step 1: Add the `onboard` object to each locale**

Add this top-level `"onboard"` object (place alphabetically near other top-level keys). Values per locale:

| key | en | es | pt-BR | pt-PT |
|-----|----|----|-------|-------|
| onboard.title | Onboard a service | Incorporar un servicio | Registrar um serviço | Registar um serviço |
| onboard.subtitle | Register a service and, optionally, its interface and contract in one guided flow. | Registra un servicio y, opcionalmente, su interfaz y contrato en un flujo guiado. | Registre um serviço e, opcionalmente, sua interface e contrato em um fluxo guiado. | Registe um serviço e, opcionalmente, a sua interface e contrato num fluxo guiado. |
| onboard.steps.identity | Identity & ownership | Identidad y responsables | Identidade e ownership | Identidade e ownership |
| onboard.steps.interface | Interface | Interfaz | Interface | Interface |
| onboard.steps.contract | Contract | Contrato | Contrato | Contrato |
| onboard.steps.review | Review | Revisión | Revisão | Revisão |
| onboard.optional | Optional | Opcional | Opcional | Opcional |
| onboard.skip | Skip this step | Omitir este paso | Pular esta etapa | Ignorar este passo |
| onboard.finish | Finish | Finalizar | Concluir | Concluir |
| onboard.exitSaved | Your service is saved. You can finish the remaining steps later from the service page. | Tu servicio está guardado. Puedes completar los pasos restantes más tarde. | Seu serviço foi salvo. Você pode concluir as etapas restantes depois na página do serviço. | O seu serviço foi guardado. Pode concluir os passos restantes depois na página do serviço. |
| onboard.identity.heading | Service identity & ownership | Identidad y responsables | Identidade e ownership | Identidade e ownership |
| onboard.identity.name | Service name | Nombre del servicio | Nome do serviço | Nome do serviço |
| onboard.identity.domain | Domain | Dominio | Domínio | Domínio |
| onboard.identity.team | Team | Equipo | Time | Equipa |
| onboard.identity.description | Description | Descripción | Descrição | Descrição |
| onboard.identity.serviceType | Service type | Tipo de servicio | Tipo de serviço | Tipo de serviço |
| onboard.identity.criticality | Criticality | Criticidad | Criticidade | Criticidade |
| onboard.identity.exposure | Exposure | Exposición | Exposição | Exposição |
| onboard.identity.technicalOwner | Technical owner | Responsable técnico | Owner técnico | Owner técnico |
| onboard.identity.businessOwner | Business owner | Responsable de negocio | Owner de negócio | Owner de negócio |
| onboard.identity.documentationUrl | Documentation URL | URL de documentación | URL de documentação | URL de documentação |
| onboard.identity.repositoryUrl | Repository URL | URL del repositorio | URL do repositório | URL do repositório |
| onboard.interface.heading | Expose an interface (optional) | Exponer una interfaz (opcional) | Expor uma interface (opcional) | Expor uma interface (opcional) |
| onboard.interface.skipHint | You can add interfaces later from the service page. | Puedes añadir interfaces más tarde. | Você pode adicionar interfaces depois. | Pode adicionar interfaces depois. |
| onboard.contract.heading | Define a contract (optional) | Definir un contrato (opcional) | Definir um contrato (opcional) | Definir um contrato (opcional) |
| onboard.contract.skipHint | You can create contracts later from the service page. | Puedes crear contratos más tarde. | Você pode criar contratos depois. | Pode criar contratos depois. |
| onboard.review.heading | Review & create | Revisar y crear | Revisar e criar | Rever e criar |
| onboard.review.service | Service | Servicio | Serviço | Serviço |
| onboard.review.interface | Interface | Interfaz | Interface | Interface |
| onboard.review.contract | Contract | Contrato | Contrato | Contrato |
| onboard.review.skipped | Skipped | Omitido | Pulado | Ignorado |
| onboard.error.create | Could not create the service. Please try again. | No se pudo crear el servicio. | Não foi possível criar o serviço. | Não foi possível criar o serviço. |
| onboard.error.interface | Could not create the interface. Please try again. | No se pudo crear la interfaz. | Não foi possível criar a interface. | Não foi possível criar a interface. |
| onboard.error.contract | Could not create the contract. Please try again. | No se pudo crear el contrato. | Não foi possível criar o contrato. | Não foi possível criar o contrato. |

- [ ] **Step 2: Run i18n validation**

Run: `npm run validate:i18n`
Expected: PASS (all 4 locales in sync, no missing/extra keys).

- [ ] **Step 3: Commit**

```bash
git add src/frontend/src/locales/en.json src/frontend/src/locales/es.json src/frontend/src/locales/pt-BR.json src/frontend/src/locales/pt-PT.json
git commit -m "i18n(catalog): onboard.* keys for onboarding journey (4 locales)"
```

---

## Task 2: `onboardValidation.ts` — schemas, types, constants

**Files:**
- Create: `src/frontend/src/features/catalog/onboard/onboardValidation.ts`
- Test: `src/frontend/src/__tests__/catalog/onboardValidation.test.ts`

**Interfaces:**
- Produces:
  - `ServiceIdentityValues` (object type, all string fields listed below), `EMPTY_IDENTITY: ServiceIdentityValues`.
  - `serviceIdentitySchema` (Zod) — requires `name`, `domain`, `teamName`, `serviceType`.
  - `validateIdentity(values): Partial<Record<keyof ServiceIdentityValues, string>>` — returns field→message map (empty = valid).
  - `ServiceInterfaceValues`, `EMPTY_INTERFACE`, `serviceInterfaceSchema` (requires `name`, `interfaceType`), `validateInterface(values)`.
  - `SERVICE_TYPE_OPTIONS`, `CRITICALITY_OPTIONS`, `EXPOSURE_OPTIONS`, `INTERFACE_TYPE_OPTIONS`, `INTERFACE_EXPOSURE_OPTIONS` — `{ value: string; label: string }[]` (label = i18n key resolved by caller; here store the raw i18n key in `labelKey`). Use `labelKey`, not `label`, so the form component localizes.

- [ ] **Step 1: Write the failing test**

```ts
// src/frontend/src/__tests__/catalog/onboardValidation.test.ts
import { describe, it, expect } from 'vitest';
import {
  EMPTY_IDENTITY,
  validateIdentity,
  EMPTY_INTERFACE,
  validateInterface,
} from '../../features/catalog/onboard/onboardValidation';

describe('validateIdentity', () => {
  it('flags required fields when empty', () => {
    const errors = validateIdentity(EMPTY_IDENTITY);
    expect(errors.name).toBeTruthy();
    expect(errors.domain).toBeTruthy();
    expect(errors.teamName).toBeTruthy();
  });

  it('passes with required fields filled', () => {
    const errors = validateIdentity({
      ...EMPTY_IDENTITY,
      name: 'orders-api',
      domain: 'Commerce',
      teamName: 'Orders',
      serviceType: 'RestApi',
    });
    expect(errors).toEqual({});
  });
});

describe('validateInterface', () => {
  it('flags missing name', () => {
    const errors = validateInterface(EMPTY_INTERFACE);
    expect(errors.name).toBeTruthy();
  });

  it('passes with a name', () => {
    const errors = validateInterface({ ...EMPTY_INTERFACE, name: 'Orders REST v1' });
    expect(errors).toEqual({});
  });
});
```

- [ ] **Step 2: Run test to verify it fails**

Run: `npm run test -- onboardValidation`
Expected: FAIL (module not found / exports undefined).

- [ ] **Step 3: Write the implementation**

```ts
// src/frontend/src/features/catalog/onboard/onboardValidation.ts
import { z } from 'zod';

/** Valores do passo de Identidade & Ownership (campos aceites por registerService). */
export interface ServiceIdentityValues {
  name: string;
  domain: string;
  teamName: string;
  description: string;
  serviceType: string;
  criticality: string;
  exposureType: string;
  technicalOwner: string;
  businessOwner: string;
  documentationUrl: string;
  repositoryUrl: string;
}

export const EMPTY_IDENTITY: ServiceIdentityValues = {
  name: '', domain: '', teamName: '', description: '',
  serviceType: 'RestApi', criticality: 'Medium', exposureType: 'Internal',
  technicalOwner: '', businessOwner: '', documentationUrl: '', repositoryUrl: '',
};

const nonEmpty = z.string().trim().min(1);

export const serviceIdentitySchema = z.object({
  name: nonEmpty,
  domain: nonEmpty,
  teamName: nonEmpty,
  serviceType: nonEmpty,
});

/** Valores do passo de Interface (subconjunto de createServiceInterface). */
export interface ServiceInterfaceValues {
  name: string;
  interfaceType: string;
  description: string;
  exposureScope: string;
  basePath: string;
  topicName: string;
  wsdlNamespace: string;
  grpcServiceName: string;
  scheduleCron: string;
  documentationUrl: string;
  requiresContract: boolean;
}

export const EMPTY_INTERFACE: ServiceInterfaceValues = {
  name: '', interfaceType: 'RestApi', description: '', exposureScope: 'Internal',
  basePath: '', topicName: '', wsdlNamespace: '', grpcServiceName: '',
  scheduleCron: '', documentationUrl: '', requiresContract: false,
};

export const serviceInterfaceSchema = z.object({
  name: nonEmpty,
  interfaceType: nonEmpty,
});

/** Converte issues do Zod num mapa campo→primeira-mensagem. */
function issuesToMap<T extends Record<string, unknown>>(
  result: z.SafeParseReturnType<T, unknown>,
): Partial<Record<keyof T, string>> {
  if (result.success) return {};
  const map: Partial<Record<keyof T, string>> = {};
  for (const issue of result.error.issues) {
    const key = issue.path[0] as keyof T;
    if (key && !map[key]) map[key] = issue.message;
  }
  return map;
}

export function validateIdentity(
  values: ServiceIdentityValues,
): Partial<Record<keyof ServiceIdentityValues, string>> {
  return issuesToMap(serviceIdentitySchema.safeParse(values));
}

export function validateInterface(
  values: ServiceInterfaceValues,
): Partial<Record<keyof ServiceInterfaceValues, string>> {
  return issuesToMap(serviceInterfaceSchema.safeParse(values));
}

/** Opções de select — labelKey é uma chave i18n resolvida pelo componente. */
export interface SelectOptionKey { value: string; labelKey: string; }

export const SERVICE_TYPE_OPTIONS: SelectOptionKey[] = [
  { value: 'RestApi', labelKey: 'catalog.badges.type.RestApi' },
  { value: 'GraphqlApi', labelKey: 'catalog.badges.type.GraphqlApi' },
  { value: 'GrpcService', labelKey: 'catalog.badges.type.GrpcService' },
  { value: 'SoapService', labelKey: 'catalog.badges.type.SoapService' },
  { value: 'KafkaProducer', labelKey: 'catalog.badges.type.KafkaProducer' },
  { value: 'KafkaConsumer', labelKey: 'catalog.badges.type.KafkaConsumer' },
  { value: 'BackgroundService', labelKey: 'catalog.badges.type.BackgroundService' },
  { value: 'IntegrationComponent', labelKey: 'catalog.badges.type.IntegrationComponent' },
  { value: 'ThirdParty', labelKey: 'catalog.badges.type.ThirdParty' },
];

export const CRITICALITY_OPTIONS: SelectOptionKey[] = [
  { value: 'Low', labelKey: 'catalog.badges.criticality.Low' },
  { value: 'Medium', labelKey: 'catalog.badges.criticality.Medium' },
  { value: 'High', labelKey: 'catalog.badges.criticality.High' },
  { value: 'Critical', labelKey: 'catalog.badges.criticality.Critical' },
];

export const EXPOSURE_OPTIONS: SelectOptionKey[] = [
  { value: 'Internal', labelKey: 'catalog.badges.exposure.Internal' },
  { value: 'Partner', labelKey: 'catalog.badges.exposure.Partner' },
  { value: 'External', labelKey: 'catalog.badges.exposure.External' },
];

export const INTERFACE_TYPE_OPTIONS: SelectOptionKey[] = [
  { value: 'RestApi', labelKey: 'serviceInterfaces.typeRestApi' },
  { value: 'GraphqlApi', labelKey: 'serviceInterfaces.typeGraphqlApi' },
  { value: 'GrpcService', labelKey: 'serviceInterfaces.typeGrpcService' },
  { value: 'SoapService', labelKey: 'serviceInterfaces.typeSoapService' },
  { value: 'KafkaProducer', labelKey: 'serviceInterfaces.typeKafkaProducer' },
  { value: 'KafkaConsumer', labelKey: 'serviceInterfaces.typeKafkaConsumer' },
];

export const INTERFACE_EXPOSURE_OPTIONS: SelectOptionKey[] = EXPOSURE_OPTIONS;
```

- [ ] **Step 4: Run test to verify it passes**

Run: `npm run test -- onboardValidation`
Expected: PASS (4 tests).

- [ ] **Step 5: Typecheck + commit**

Run: `npx tsc --noEmit`
Expected: no errors in the new file.

```bash
git add src/frontend/src/features/catalog/onboard/onboardValidation.ts src/frontend/src/__tests__/catalog/onboardValidation.test.ts
git commit -m "feat(catalog): onboarding validation schemas + option sets"
```

---

## Task 3: `ServiceIdentityForm` (Step 1 form)

**Files:**
- Create: `src/frontend/src/features/catalog/onboard/ServiceIdentityForm.tsx`
- Test: `src/frontend/src/__tests__/catalog/ServiceIdentityForm.test.tsx`

**Interfaces:**
- Consumes: `ServiceIdentityValues`, `SERVICE_TYPE_OPTIONS`, `CRITICALITY_OPTIONS`, `EXPOSURE_OPTIONS` from `onboardValidation.ts`.
- Produces: `ServiceIdentityForm` — controlled component.
  ```ts
  interface ServiceIdentityFormProps {
    values: ServiceIdentityValues;
    errors: Partial<Record<keyof ServiceIdentityValues, string>>;
    onChange: <K extends keyof ServiceIdentityValues>(key: K, value: ServiceIdentityValues[K]) => void;
  }
  ```

- [ ] **Step 1: Write the failing test**

```tsx
// src/frontend/src/__tests__/catalog/ServiceIdentityForm.test.tsx
import { describe, it, expect, vi } from 'vitest';
import { render, screen, fireEvent } from '@testing-library/react';
import { ServiceIdentityForm } from '../../features/catalog/onboard/ServiceIdentityForm';
import { EMPTY_IDENTITY } from '../../features/catalog/onboard/onboardValidation';

vi.mock('react-i18next', () => ({
  useTranslation: () => ({ t: (_k: string, d?: string) => d ?? _k }),
}));

describe('ServiceIdentityForm', () => {
  it('renders name field and reports changes', () => {
    const onChange = vi.fn();
    render(<ServiceIdentityForm values={EMPTY_IDENTITY} errors={{}} onChange={onChange} />);
    const name = screen.getByLabelText(/service name/i);
    fireEvent.change(name, { target: { value: 'orders-api' } });
    expect(onChange).toHaveBeenCalledWith('name', 'orders-api');
  });

  it('shows a field error when provided', () => {
    render(
      <ServiceIdentityForm values={EMPTY_IDENTITY} errors={{ name: 'Required' }} onChange={() => {}} />,
    );
    expect(screen.getByText('Required')).toBeInTheDocument();
  });
});
```

- [ ] **Step 2: Run test to verify it fails**

Run: `npm run test -- ServiceIdentityForm`
Expected: FAIL (module not found).

- [ ] **Step 3: Write the implementation**

```tsx
// src/frontend/src/features/catalog/onboard/ServiceIdentityForm.tsx
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
```

- [ ] **Step 4: Run test to verify it passes**

Run: `npm run test -- ServiceIdentityForm`
Expected: PASS (2 tests).

- [ ] **Step 5: Commit**

```bash
git add src/frontend/src/features/catalog/onboard/ServiceIdentityForm.tsx src/frontend/src/__tests__/catalog/ServiceIdentityForm.test.tsx
git commit -m "feat(catalog): ServiceIdentityForm controlled step-1 form"
```

---

## Task 4: `ServiceInterfaceForm` + refactor `CreateServiceInterfacePage`

**Files:**
- Create: `src/frontend/src/features/catalog/onboard/ServiceInterfaceForm.tsx`
- Modify: `src/frontend/src/features/catalog/pages/CreateServiceInterfacePage.tsx`
- Test: `src/frontend/src/__tests__/catalog/ServiceInterfaceForm.test.tsx`

**Interfaces:**
- Consumes: `ServiceInterfaceValues`, `INTERFACE_TYPE_OPTIONS`, `INTERFACE_EXPOSURE_OPTIONS` from `onboardValidation.ts`.
- Produces: `ServiceInterfaceForm` — controlled component.
  ```ts
  interface ServiceInterfaceFormProps {
    values: ServiceInterfaceValues;
    errors: Partial<Record<keyof ServiceInterfaceValues, string>>;
    onChange: <K extends keyof ServiceInterfaceValues>(key: K, value: ServiceInterfaceValues[K]) => void;
  }
  ```

- [ ] **Step 1: Write the failing test**

```tsx
// src/frontend/src/__tests__/catalog/ServiceInterfaceForm.test.tsx
import { describe, it, expect, vi } from 'vitest';
import { render, screen, fireEvent } from '@testing-library/react';
import { ServiceInterfaceForm } from '../../features/catalog/onboard/ServiceInterfaceForm';
import { EMPTY_INTERFACE } from '../../features/catalog/onboard/onboardValidation';

vi.mock('react-i18next', () => ({
  useTranslation: () => ({ t: (_k: string, d?: string) => d ?? _k }),
}));

describe('ServiceInterfaceForm', () => {
  it('reports name changes', () => {
    const onChange = vi.fn();
    render(<ServiceInterfaceForm values={EMPTY_INTERFACE} errors={{}} onChange={onChange} />);
    fireEvent.change(screen.getByLabelText(/interface name/i), { target: { value: 'Orders v1' } });
    expect(onChange).toHaveBeenCalledWith('name', 'Orders v1');
  });

  it('shows base path field for RestApi type', () => {
    render(<ServiceInterfaceForm values={{ ...EMPTY_INTERFACE, interfaceType: 'RestApi' }} errors={{}} onChange={() => {}} />);
    expect(screen.getByLabelText(/base path/i)).toBeInTheDocument();
  });
});
```

- [ ] **Step 2: Run test to verify it fails**

Run: `npm run test -- ServiceInterfaceForm`
Expected: FAIL (module not found).

- [ ] **Step 3: Write `ServiceInterfaceForm`**

```tsx
// src/frontend/src/features/catalog/onboard/ServiceInterfaceForm.tsx
import { useTranslation } from 'react-i18next';
import { TextField, TextArea, Select, Checkbox } from '../../../shared/ui';
import {
  type ServiceInterfaceValues,
  INTERFACE_TYPE_OPTIONS,
  INTERFACE_EXPOSURE_OPTIONS,
  type SelectOptionKey,
} from './onboardValidation';

interface ServiceInterfaceFormProps {
  values: ServiceInterfaceValues;
  errors: Partial<Record<keyof ServiceInterfaceValues, string>>;
  onChange: <K extends keyof ServiceInterfaceValues>(key: K, value: ServiceInterfaceValues[K]) => void;
}

const TYPES_WITH_BASE_PATH = ['RestApi', 'SoapService', 'ZosConnectApi', 'GraphqlApi'];
const TYPES_WITH_WSDL = ['SoapService'];
const TYPES_WITH_TOPIC = ['KafkaProducer', 'KafkaConsumer', 'MqQueue'];
const TYPES_WITH_GRPC = ['GrpcService'];
const TYPES_WITH_CRON = ['BackgroundWorker', 'ScheduledJob'];

/** Formulário controlado de interface de serviço — partilhado pelo onboarding e pela página autónoma. */
export function ServiceInterfaceForm({ values, errors, onChange }: ServiceInterfaceFormProps) {
  const { t } = useTranslation();
  const opts = (list: SelectOptionKey[]) => list.map((o) => ({ value: o.value, label: t(o.labelKey) }));

  const showBasePath = TYPES_WITH_BASE_PATH.includes(values.interfaceType);
  const showWsdl = TYPES_WITH_WSDL.includes(values.interfaceType);
  const showTopic = TYPES_WITH_TOPIC.includes(values.interfaceType);
  const showGrpc = TYPES_WITH_GRPC.includes(values.interfaceType);
  const showCron = TYPES_WITH_CRON.includes(values.interfaceType);

  return (
    <div className="space-y-5 max-w-2xl">
      <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
        <TextField
          label={t('serviceInterfaces.fieldName', 'Interface Name')}
          value={values.name}
          onChange={(e) => onChange('name', e.target.value)}
          placeholder={t('serviceInterfaces.fieldNamePlaceholder', 'Orders REST API v1')}
          required
          error={errors.name}
          size="sm"
        />
        <Select
          label={t('serviceInterfaces.fieldType', 'Interface Type')}
          value={values.interfaceType}
          onChange={(e) => onChange('interfaceType', e.target.value)}
          options={opts(INTERFACE_TYPE_OPTIONS)}
          size="sm"
        />
      </div>

      <TextArea
        label={t('serviceInterfaces.fieldDescription', 'Description')}
        value={values.description}
        onChange={(e) => onChange('description', e.target.value)}
        rows={3}
      />

      <Select
        label={t('serviceInterfaces.fieldExposure', 'Exposure Scope')}
        value={values.exposureScope}
        onChange={(e) => onChange('exposureScope', e.target.value)}
        options={opts(INTERFACE_EXPOSURE_OPTIONS)}
        size="sm"
      />

      {showBasePath && (
        <TextField label={t('serviceInterfaces.fieldBasePath', 'Base Path')} value={values.basePath}
          onChange={(e) => onChange('basePath', e.target.value)}
          placeholder={t('serviceInterfaces.fieldBasePathPlaceholder', '/api/orders')} className="font-mono" size="sm" />
      )}
      {showWsdl && (
        <TextField label={t('serviceInterfaces.fieldWsdlNamespace', 'WSDL Namespace')} value={values.wsdlNamespace}
          onChange={(e) => onChange('wsdlNamespace', e.target.value)} className="font-mono" size="sm" />
      )}
      {showTopic && (
        <TextField label={t('serviceInterfaces.fieldTopicName', 'Topic Name')} value={values.topicName}
          onChange={(e) => onChange('topicName', e.target.value)}
          placeholder={t('serviceInterfaces.fieldTopicNamePlaceholder', 'orders.events.v1')} className="font-mono" size="sm" />
      )}
      {showGrpc && (
        <TextField label={t('serviceInterfaces.fieldGrpcServiceName', 'Proto Service Name')} value={values.grpcServiceName}
          onChange={(e) => onChange('grpcServiceName', e.target.value)} className="font-mono" size="sm" />
      )}
      {showCron && (
        <TextField label={t('serviceInterfaces.fieldScheduleCron', 'Cron Expression')} value={values.scheduleCron}
          onChange={(e) => onChange('scheduleCron', e.target.value)} placeholder="0 */5 * * *" className="font-mono" size="sm" />
      )}

      <TextField
        label={t('serviceInterfaces.fieldDocumentationUrl', 'Documentation URL')}
        type="url"
        value={values.documentationUrl}
        onChange={(e) => onChange('documentationUrl', e.target.value)}
        className="font-mono"
        size="sm"
      />

      <Checkbox
        id="requires-contract"
        checked={values.requiresContract}
        onChange={(e) => onChange('requiresContract', e.target.checked)}
        label={t('serviceInterfaces.fieldRequiresContract', 'Requires Contract')}
      />
    </div>
  );
}
```

- [ ] **Step 4: Refactor `CreateServiceInterfacePage` to consume the form**

In `CreateServiceInterfacePage.tsx`, replace the inline field block (the `<div className="space-y-6 max-w-2xl"> … </div>` that renders Name/Type/Description/Exposure/conditional fields/DocUrl/RequiresContract, but NOT the mutation-error `<p>` and the actions row) with:

```tsx
<ServiceInterfaceForm
  values={form}
  errors={errors}
  onChange={(field, value) => set(field, value)}
/>
```

Add the import: `import { ServiceInterfaceForm } from '../onboard/ServiceInterfaceForm';` and `import { EMPTY_INTERFACE, type ServiceInterfaceValues } from '../onboard/ServiceInterfaceForm';` — wait, those live in `onboardValidation`. Use:
`import { ServiceInterfaceForm } from '../onboard/ServiceInterfaceForm';`
`import { EMPTY_INTERFACE, type ServiceInterfaceValues } from '../onboard/onboardValidation';`

Then change local state to reuse the shared type: replace `interface CreateInterfaceFormData {…}` + `INITIAL_FORM` with `const [form, setForm] = useState<ServiceInterfaceValues>(EMPTY_INTERFACE);` and delete the now-unused local type/constant and the `INTERFACE_*` field-visibility consts and `interfaceTypeOptions`/`exposureScopeOptions` arrays (now owned by the form). Keep `serviceId`, the service query, the `mutation` (it reads `form.*`), `set`, `validate`, `handleSubmit`, the actions row, and the mutation-error paragraph. The `mutation` body already references `form.name`, `form.interfaceType`, etc. — those field names are unchanged in `ServiceInterfaceValues`, so the mutation stays intact.

- [ ] **Step 5: Run tests to verify pass**

Run: `npm run test -- ServiceInterfaceForm ServiceInterfacesTab`
Expected: PASS.
Run: `npx tsc --noEmit`
Expected: no errors (unused imports removed).

- [ ] **Step 6: Commit**

```bash
git add src/frontend/src/features/catalog/onboard/ServiceInterfaceForm.tsx src/frontend/src/features/catalog/pages/CreateServiceInterfacePage.tsx src/frontend/src/__tests__/catalog/ServiceInterfaceForm.test.tsx
git commit -m "feat(catalog): extract ServiceInterfaceForm, reuse in CreateServiceInterfacePage"
```

---

## Task 5: `onCreated` seam on `useContractDraftForm`

**Files:**
- Modify: `src/frontend/src/features/contracts/create/useContractDraftForm.ts`
- Test: `src/frontend/src/__tests__/contracts/useContractDraftForm.onCreated.test.ts` (create; dir `__tests__/contracts/` exists)

**Interfaces:**
- Produces: `UseContractDraftFormArgs.onCreated?: (draftId: string) => void`. When provided, `createMutation.onSuccess` calls `onCreated(draftId)` and does NOT navigate. When absent, behavior is unchanged (navigate to `/contracts/studio/:draftId`).

- [ ] **Step 1: Write the failing test**

```ts
// src/frontend/src/__tests__/contracts/useContractDraftForm.onCreated.test.ts
import { describe, it, expect, vi } from 'vitest';

// Verifica o seam de onSuccess isoladamente: dado um draftId, quando onCreated
// existe chama-o; caso contrário navega para o studio.
function onDraftCreated(
  data: { draftId: string },
  onCreated: ((id: string) => void) | undefined,
  navigate: (path: string) => void,
) {
  if (onCreated) onCreated(data.draftId);
  else navigate(`/contracts/studio/${data.draftId}`);
}

describe('useContractDraftForm onCreated seam', () => {
  it('calls onCreated when provided', () => {
    const onCreated = vi.fn();
    const navigate = vi.fn();
    onDraftCreated({ draftId: 'd1' }, onCreated, navigate);
    expect(onCreated).toHaveBeenCalledWith('d1');
    expect(navigate).not.toHaveBeenCalled();
  });

  it('navigates to studio when onCreated absent', () => {
    const navigate = vi.fn();
    onDraftCreated({ draftId: 'd2' }, undefined, navigate);
    expect(navigate).toHaveBeenCalledWith('/contracts/studio/d2');
  });
});
```

> Note: this test pins the branch logic that Step 3 wires into the hook. It intentionally tests the decision function shape, not the whole hook (which requires a QueryClient + router); the wizard integration test in Task 8 exercises it end-to-end.

- [ ] **Step 2: Run test to verify it passes trivially, then wire the hook**

Run: `npm run test -- useContractDraftForm.onCreated`
Expected: PASS (the helper mirrors the intended logic).

- [ ] **Step 3: Modify the hook**

In `useContractDraftForm.ts`, extend the args interface:

```ts
interface UseContractDraftFormArgs {
  prefilledServiceId?: string;
  initialType?: ContractTypeValue | null;
  initialMode?: CreationMode | null;
  /** Se fornecido, intercepta a criação do draft (ex.: wizard de onboarding) em vez de navegar para o studio. */
  onCreated?: (draftId: string) => void;
}
```

Replace the mutation's `onSuccess`:

```ts
    onSuccess: (data) => {
      if (args.onCreated) {
        args.onCreated(data.draftId);
        return;
      }
      navigate(`/contracts/studio/${data.draftId}`);
    },
```

- [ ] **Step 4: Verify existing behavior unchanged**

Run: `npm run test -- CreateContractPage contract`
Expected: PASS (default path still navigates to studio).
Run: `npx tsc --noEmit`
Expected: no errors.

- [ ] **Step 5: Commit**

```bash
git add src/frontend/src/features/contracts/create/useContractDraftForm.ts src/frontend/src/__tests__/contracts/useContractDraftForm.onCreated.test.ts
git commit -m "feat(contracts): onCreated seam on useContractDraftForm for wizard reuse"
```

---

## Task 6: `OnboardWizardShell` + `OnboardIdentityPreview`

**Files:**
- Create: `src/frontend/src/features/catalog/onboard/OnboardWizardShell.tsx`
- Create: `src/frontend/src/features/catalog/onboard/OnboardIdentityPreview.tsx`
- Test: `src/frontend/src/__tests__/catalog/OnboardWizardShell.test.tsx`

**Interfaces:**
- Produces:
  ```ts
  export type OnboardStep = 'identity' | 'interface' | 'contract' | 'review';
  export interface OnboardStepMeta { id: OnboardStep; label: string; optional?: boolean; }
  interface OnboardWizardShellProps {
    title: string;
    steps: OnboardStepMeta[];
    activeStep: OnboardStep;
    preview: React.ReactNode;
    children: React.ReactNode;
    canGoNext: boolean;
    isFirstStep: boolean;
    isLastStep: boolean;
    canSkip: boolean;
    pending: boolean;
    onBack: () => void;
    onNext: () => void;
    onSkip: () => void;
    onCancel: () => void;
  }
  ```
  `OnboardIdentityPreview` props: `{ values: ServiceIdentityValues }`.

- [ ] **Step 1: Write the failing test**

```tsx
// src/frontend/src/__tests__/catalog/OnboardWizardShell.test.tsx
import { describe, it, expect, vi } from 'vitest';
import { render, screen, fireEvent } from '@testing-library/react';
import { OnboardWizardShell } from '../../features/catalog/onboard/OnboardWizardShell';

vi.mock('react-i18next', () => ({
  useTranslation: () => ({ t: (_k: string, d?: string) => d ?? _k }),
}));

const steps = [
  { id: 'identity' as const, label: 'Identity' },
  { id: 'interface' as const, label: 'Interface', optional: true },
  { id: 'contract' as const, label: 'Contract', optional: true },
  { id: 'review' as const, label: 'Review' },
];

function renderShell(overrides = {}) {
  const props = {
    title: 'Onboard', steps, activeStep: 'interface' as const,
    preview: <div>preview</div>, children: <div>content</div>,
    canGoNext: true, isFirstStep: false, isLastStep: false,
    canSkip: true, pending: false,
    onBack: vi.fn(), onNext: vi.fn(), onSkip: vi.fn(), onCancel: vi.fn(),
    ...overrides,
  };
  render(<OnboardWizardShell {...props} />);
  return props;
}

describe('OnboardWizardShell', () => {
  it('shows Skip on optional steps and fires onSkip', () => {
    const props = renderShell();
    fireEvent.click(screen.getByRole('button', { name: /skip/i }));
    expect(props.onSkip).toHaveBeenCalled();
  });

  it('hides Skip when canSkip is false', () => {
    renderShell({ canSkip: false });
    expect(screen.queryByRole('button', { name: /skip/i })).not.toBeInTheDocument();
  });

  it('disables Next when canGoNext is false', () => {
    renderShell({ canGoNext: false });
    expect(screen.getByRole('button', { name: /next/i })).toBeDisabled();
  });

  it('shows Finish on the last step', () => {
    renderShell({ activeStep: 'review', isLastStep: true, canSkip: false });
    expect(screen.getByRole('button', { name: /finish/i })).toBeInTheDocument();
  });
});
```

- [ ] **Step 2: Run test to verify it fails**

Run: `npm run test -- OnboardWizardShell`
Expected: FAIL (module not found).

- [ ] **Step 3: Write `OnboardIdentityPreview`**

```tsx
// src/frontend/src/features/catalog/onboard/OnboardIdentityPreview.tsx
import { useTranslation } from 'react-i18next';
import { Badge } from '../../../components/Badge';
import { cn } from '../../../lib/cn';
import type { ServiceIdentityValues } from './onboardValidation';

/** Cartão de identidade ao vivo (rail esquerdo do onboarding). Novo serviço é sempre Planning. */
export function OnboardIdentityPreview({ values }: { values: ServiceIdentityValues }) {
  const { t } = useTranslation();
  const initial = values.name.trim().charAt(0).toUpperCase() || '?';

  return (
    <div className="rounded-2xl border border-edge bg-card overflow-hidden shadow-sm">
      <div className="bg-gradient-to-b from-accent/10 to-transparent p-4">
        <div className="flex items-center gap-3">
          <div className={cn(
            'flex items-center justify-center w-11 h-11 rounded-xl font-bold text-lg shrink-0',
            values.name ? 'bg-accent text-on-accent' : 'bg-accent/20 text-accent',
          )}>
            {initial}
          </div>
          <div className="min-w-0">
            <p className={cn('font-mono text-sm font-semibold truncate', values.name ? 'text-heading' : 'text-muted')}>
              {values.name || t('onboard.identity.name')}
            </p>
            <p className="text-xs text-muted truncate mt-0.5">{values.domain || '—'}</p>
          </div>
          <Badge variant="warning" size="sm" className="shrink-0 ml-auto">Planning</Badge>
        </div>
        <div className="flex flex-wrap gap-1.5 mt-3">
          {values.serviceType && <Badge variant="primary" size="sm">{values.serviceType}</Badge>}
          {values.criticality && <Badge variant="default" size="sm">{values.criticality}</Badge>}
          {values.exposureType && <Badge variant="default" size="sm">{values.exposureType}</Badge>}
        </div>
      </div>
      <div className="px-4 py-2 divide-y divide-edge/60">
        <Row label={t('onboard.identity.team')} value={values.teamName || '—'} />
        <Row label={t('onboard.identity.technicalOwner')} value={values.technicalOwner || '—'} />
      </div>
    </div>
  );
}

function Row({ label, value }: { label: string; value: string }) {
  return (
    <div className="flex items-center justify-between py-2 text-xs">
      <span className="text-muted">{label}</span>
      <span className="text-heading font-medium truncate ml-2">{value}</span>
    </div>
  );
}
```

- [ ] **Step 4: Write `OnboardWizardShell`**

```tsx
// src/frontend/src/features/catalog/onboard/OnboardWizardShell.tsx
import type React from 'react';
import { useTranslation } from 'react-i18next';
import { ArrowLeft, ArrowRight, Check, X } from 'lucide-react';
import { PageContainer } from '../../../components/shell';
import { Button } from '../../../shared/ui';
import { cn } from '../../../lib/cn';

export type OnboardStep = 'identity' | 'interface' | 'contract' | 'review';
export interface OnboardStepMeta { id: OnboardStep; label: string; optional?: boolean; }

interface OnboardWizardShellProps {
  title: string;
  steps: OnboardStepMeta[];
  activeStep: OnboardStep;
  preview: React.ReactNode;
  children: React.ReactNode;
  canGoNext: boolean;
  isFirstStep: boolean;
  isLastStep: boolean;
  canSkip: boolean;
  pending: boolean;
  onBack: () => void;
  onNext: () => void;
  onSkip: () => void;
  onCancel: () => void;
}

/** Shell presentacional do wizard de onboarding: rail de progresso + preview + conteúdo + footer. */
export function OnboardWizardShell({
  title, steps, activeStep, preview, children,
  canGoNext, isFirstStep, isLastStep, canSkip, pending,
  onBack, onNext, onSkip, onCancel,
}: OnboardWizardShellProps) {
  const { t } = useTranslation();
  const activeIndex = steps.findIndex((s) => s.id === activeStep);

  return (
    <PageContainer className="animate-fade-in">
      <div className="flex items-center justify-between mb-4">
        <h1 className="text-lg font-bold text-heading">{title}</h1>
        <Button variant="ghost" size="sm" icon={<X size={14} />} onClick={onCancel}>
          {t('common.cancel', 'Cancel')}
        </Button>
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-[300px_minmax(0,1fr)] gap-6 items-start">
        <div className="lg:sticky lg:top-4 space-y-4">
          {preview}
          <ol className="rounded-2xl border border-edge bg-card p-3 space-y-1">
            {steps.map((s, idx) => (
              <li key={s.id} className={cn(
                'flex items-center gap-2 rounded-lg px-2.5 py-2 text-sm',
                s.id === activeStep ? 'bg-accent/10 text-heading' : 'text-muted',
              )}>
                <span className={cn(
                  'w-5 h-5 rounded-full text-[11px] flex items-center justify-center font-bold shrink-0',
                  idx < activeIndex ? 'bg-success text-white'
                    : s.id === activeStep ? 'bg-accent text-on-accent' : 'bg-elevated text-muted',
                )}>
                  {idx < activeIndex ? <Check size={12} /> : idx + 1}
                </span>
                <span className="truncate">{s.label}</span>
                {s.optional && (
                  <span className="ml-auto text-[10px] text-muted">{t('onboard.optional', 'Optional')}</span>
                )}
              </li>
            ))}
          </ol>
        </div>

        <div className="min-w-0">
          <div className="bg-card border border-edge rounded-xl p-5">
            {children}
            <div className="flex items-center justify-between pt-4 mt-4 border-t border-edge">
              <Button variant="ghost" size="sm" icon={<ArrowLeft size={14} />} onClick={onBack} disabled={isFirstStep}>
                {t('common.back', 'Back')}
              </Button>
              <div className="flex items-center gap-2">
                {canSkip && (
                  <Button variant="ghost" size="sm" onClick={onSkip}>
                    {t('onboard.skip', 'Skip this step')}
                  </Button>
                )}
                <Button
                  variant="primary"
                  size="sm"
                  onClick={onNext}
                  disabled={!canGoNext}
                  loading={pending}
                  icon={isLastStep ? <Check size={14} /> : undefined}
                >
                  {isLastStep ? t('onboard.finish', 'Finish') : t('common.next', 'Next')}
                  {!isLastStep && <ArrowRight size={14} />}
                </Button>
              </div>
            </div>
          </div>
        </div>
      </div>
    </PageContainer>
  );
}
```

- [ ] **Step 5: Run test to verify it passes**

Run: `npm run test -- OnboardWizardShell`
Expected: PASS (4 tests).

- [ ] **Step 6: Commit**

```bash
git add src/frontend/src/features/catalog/onboard/OnboardWizardShell.tsx src/frontend/src/features/catalog/onboard/OnboardIdentityPreview.tsx src/frontend/src/__tests__/catalog/OnboardWizardShell.test.tsx
git commit -m "feat(catalog): OnboardWizardShell + live identity preview"
```

---

## Task 7: `useOnboardWizard` hook + `OnboardReviewStep`

**Files:**
- Create: `src/frontend/src/features/catalog/onboard/useOnboardWizard.ts`
- Create: `src/frontend/src/features/catalog/onboard/OnboardReviewStep.tsx`
- Test: `src/frontend/src/__tests__/catalog/useOnboardWizard.test.tsx`

**Interfaces:**
- Consumes: `serviceCatalogApi.registerService`, `serviceCatalogApi.createServiceInterface`, `useContractDraftForm` (with `onCreated`), validation from Task 2.
- Produces: `useOnboardWizard()` returning:
  ```ts
  {
    activeStep: OnboardStep; steps: OnboardStepMeta[];
    isFirstStep: boolean; isLastStep: boolean; canSkip: boolean; canGoNext: boolean; pending: boolean;
    identity: ServiceIdentityValues; identityErrors: Partial<Record<keyof ServiceIdentityValues,string>>;
    setIdentityField: <K extends keyof ServiceIdentityValues>(k: K, v: ServiceIdentityValues[K]) => void;
    interface: ServiceInterfaceValues; interfaceErrors: Partial<Record<keyof ServiceInterfaceValues,string>>;
    setInterfaceField: <K extends keyof ServiceInterfaceValues>(k: K, v: ServiceInterfaceValues[K]) => void;
    interfaceTouched: boolean;
    contractForm: ReturnType<typeof useContractDraftForm>;
    serviceId: string; createdDraftId: string; error: string | null;
    onNext: () => void; onBack: () => void; onSkip: () => void; onCancel: () => void;
  }
  ```
  `OnboardReviewStep` props: `{ identity: ServiceIdentityValues; interfaceValues: ServiceInterfaceValues | null; contractSummary: { title: string; type: string | null } | null; }`.

- [ ] **Step 1: Write the failing test**

```tsx
// src/frontend/src/__tests__/catalog/useOnboardWizard.test.tsx
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { renderHook, act, waitFor } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';
import type { ReactNode } from 'react';
import { useOnboardWizard } from '../../features/catalog/onboard/useOnboardWizard';

const navigate = vi.fn();
vi.mock('react-router-dom', async (orig) => {
  const actual = await orig<typeof import('react-router-dom')>();
  return { ...actual, useNavigate: () => navigate };
});
vi.mock('react-i18next', () => ({ useTranslation: () => ({ t: (_k: string, d?: string) => d ?? _k }) }));

const registerService = vi.fn();
vi.mock('../../features/catalog/api', () => ({
  serviceCatalogApi: {
    registerService: (...a: unknown[]) => registerService(...a),
    createServiceInterface: vi.fn(() => Promise.resolve({})),
    listServices: vi.fn(() => Promise.resolve({ items: [] })),
  },
}));
// useContractDraftForm consome ../../catalog/api/serviceCatalog no path real; garantir stub coerente.
vi.mock('../../features/catalog/api/serviceCatalog', () => ({
  serviceCatalogApi: { listServices: vi.fn(() => Promise.resolve({ items: [] })) },
}));

function wrapper({ children }: { children: ReactNode }) {
  const qc = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return (
    <QueryClientProvider client={qc}>
      <MemoryRouter>{children}</MemoryRouter>
    </QueryClientProvider>
  );
}

describe('useOnboardWizard', () => {
  beforeEach(() => { navigate.mockReset(); registerService.mockReset(); });

  it('blocks Next on step 1 until required fields valid', () => {
    const { result } = renderHook(() => useOnboardWizard(), { wrapper });
    expect(result.current.canGoNext).toBe(false);
    act(() => {
      result.current.setIdentityField('name', 'orders');
      result.current.setIdentityField('domain', 'Commerce');
      result.current.setIdentityField('teamName', 'Orders');
    });
    expect(result.current.canGoNext).toBe(true);
  });

  it('creates the service on leaving step 1 and advances to interface', async () => {
    registerService.mockResolvedValue({ id: 'svc-1' });
    const { result } = renderHook(() => useOnboardWizard(), { wrapper });
    act(() => {
      result.current.setIdentityField('name', 'orders');
      result.current.setIdentityField('domain', 'Commerce');
      result.current.setIdentityField('teamName', 'Orders');
    });
    act(() => { result.current.onNext(); });
    await waitFor(() => expect(registerService).toHaveBeenCalledTimes(1));
    await waitFor(() => expect(result.current.serviceId).toBe('svc-1'));
    expect(result.current.activeStep).toBe('interface');
  });

  it('finishes from review by navigating to the service page', async () => {
    registerService.mockResolvedValue({ id: 'svc-9' });
    const { result } = renderHook(() => useOnboardWizard(), { wrapper });
    act(() => {
      result.current.setIdentityField('name', 'o');
      result.current.setIdentityField('domain', 'd');
      result.current.setIdentityField('teamName', 't');
    });
    act(() => { result.current.onNext(); });           // create → interface
    await waitFor(() => expect(result.current.serviceId).toBe('svc-9'));
    act(() => { result.current.onSkip(); });            // skip interface → contract
    act(() => { result.current.onSkip(); });            // skip contract → review
    expect(result.current.activeStep).toBe('review');
    act(() => { result.current.onNext(); });            // finish
    expect(navigate).toHaveBeenCalledWith('/services/svc-9');
  });
});
```

- [ ] **Step 2: Run test to verify it fails**

Run: `npm run test -- useOnboardWizard`
Expected: FAIL (module not found).

- [ ] **Step 3: Write `OnboardReviewStep`**

```tsx
// src/frontend/src/features/catalog/onboard/OnboardReviewStep.tsx
import { useTranslation } from 'react-i18next';
import type { ServiceIdentityValues, ServiceInterfaceValues } from './onboardValidation';

interface OnboardReviewStepProps {
  identity: ServiceIdentityValues;
  interfaceValues: ServiceInterfaceValues | null;
  contractSummary: { title: string; type: string | null } | null;
}

/** Resumo honest-null do que foi/será criado. Passos saltados mostram "Skipped". */
export function OnboardReviewStep({ identity, interfaceValues, contractSummary }: OnboardReviewStepProps) {
  const { t } = useTranslation();
  const skipped = t('onboard.review.skipped', 'Skipped');

  return (
    <div className="space-y-4">
      <h2 className="text-base font-semibold text-heading">{t('onboard.review.heading', 'Review & create')}</h2>

      <Section title={t('onboard.review.service', 'Service')}>
        <Row label={t('onboard.identity.name')} value={identity.name || '—'} />
        <Row label={t('onboard.identity.domain')} value={identity.domain || '—'} />
        <Row label={t('onboard.identity.team')} value={identity.teamName || '—'} />
        <Row label={t('onboard.identity.serviceType')} value={identity.serviceType || '—'} />
      </Section>

      <Section title={t('onboard.review.interface', 'Interface')}>
        {interfaceValues
          ? <Row label={t('onboard.identity.name')} value={interfaceValues.name || '—'} />
          : <p className="text-sm text-muted py-1">{skipped}</p>}
      </Section>

      <Section title={t('onboard.review.contract', 'Contract')}>
        {contractSummary
          ? <Row label={t('onboard.identity.name')} value={contractSummary.title || contractSummary.type || '—'} />
          : <p className="text-sm text-muted py-1">{skipped}</p>}
      </Section>
    </div>
  );
}

function Section({ title, children }: { title: string; children: React.ReactNode }) {
  return (
    <div className="rounded-xl border border-edge bg-deep p-4">
      <p className="text-xs font-semibold uppercase tracking-wider text-muted mb-2">{title}</p>
      <dl className="divide-y divide-edge/60">{children}</dl>
    </div>
  );
}

function Row({ label, value }: { label: string; value: string }) {
  return (
    <div className="flex items-center justify-between py-1.5 text-sm">
      <dt className="text-muted">{label}</dt>
      <dd className="text-heading font-medium truncate ml-2">{value}</dd>
    </div>
  );
}
```

- [ ] **Step 4: Write `useOnboardWizard`**

```tsx
// src/frontend/src/features/catalog/onboard/useOnboardWizard.ts
import { useMemo, useState, useCallback } from 'react';
import { useNavigate } from 'react-router-dom';
import { useMutation } from '@tanstack/react-query';
import { useTranslation } from 'react-i18next';
import { serviceCatalogApi } from '../api';
import { useContractDraftForm } from '../../contracts/create/useContractDraftForm';
import type { ServiceType } from '../../../types';
import type { OnboardStep, OnboardStepMeta } from './OnboardWizardShell';
import {
  EMPTY_IDENTITY, EMPTY_INTERFACE,
  validateIdentity, validateInterface,
  type ServiceIdentityValues, type ServiceInterfaceValues,
} from './onboardValidation';

const STEP_ORDER: OnboardStep[] = ['identity', 'interface', 'contract', 'review'];

export function useOnboardWizard() {
  const { t } = useTranslation();
  const navigate = useNavigate();

  const [activeStep, setActiveStep] = useState<OnboardStep>('identity');
  const [identity, setIdentity] = useState<ServiceIdentityValues>(EMPTY_IDENTITY);
  const [iface, setIface] = useState<ServiceInterfaceValues>(EMPTY_INTERFACE);
  const [interfaceTouched, setInterfaceTouched] = useState(false);
  const [serviceId, setServiceId] = useState('');
  const [createdDraftId, setCreatedDraftId] = useState('');
  const [savedInterface, setSavedInterface] = useState<ServiceInterfaceValues | null>(null);
  const [error, setError] = useState<string | null>(null);

  const contractForm = useContractDraftForm({
    onCreated: (draftId) => { setCreatedDraftId(draftId); goToStep('review'); },
  });

  const setIdentityField = useCallback(
    <K extends keyof ServiceIdentityValues>(k: K, v: ServiceIdentityValues[K]) =>
      setIdentity((prev) => ({ ...prev, [k]: v })), []);

  const setInterfaceField = useCallback(
    <K extends keyof ServiceInterfaceValues>(k: K, v: ServiceInterfaceValues[K]) => {
      setInterfaceTouched(true);
      setIface((prev) => ({ ...prev, [k]: v }));
    }, []);

  const identityErrors = useMemo(() => validateIdentity(identity), [identity]);
  const interfaceErrors = useMemo(() => validateInterface(iface), [iface]);

  const registerMutation = useMutation({
    mutationFn: () => serviceCatalogApi.registerService({
      name: identity.name, domain: identity.domain, team: identity.teamName,
      description: identity.description || undefined, serviceType: identity.serviceType,
      criticality: identity.criticality, exposureType: identity.exposureType,
      technicalOwner: identity.technicalOwner || undefined,
      businessOwner: identity.businessOwner || undefined,
      documentationUrl: identity.documentationUrl || undefined,
      repositoryUrl: identity.repositoryUrl || undefined,
    }),
    onSuccess: (res) => {
      const id = res?.id ?? '';
      setServiceId(id);
      contractForm.setLinkedServiceId(id);
      contractForm.setSelectedServiceType(identity.serviceType as ServiceType);
      setError(null);
      setActiveStep('interface');
    },
    onError: () => setError(t('onboard.error.create', 'Could not create the service.')),
  });

  const interfaceMutation = useMutation({
    mutationFn: () => serviceCatalogApi.createServiceInterface({
      serviceAssetId: serviceId,
      name: iface.name, interfaceType: iface.interfaceType as never,
      description: iface.description || undefined,
      exposureScope: iface.exposureScope as never,
      basePath: iface.basePath || undefined, topicName: iface.topicName || undefined,
      wsdlNamespace: iface.wsdlNamespace || undefined,
      grpcServiceName: iface.grpcServiceName || undefined,
      scheduleCron: iface.scheduleCron || undefined,
      documentationUrl: iface.documentationUrl || undefined,
      requiresContract: iface.requiresContract,
    }),
    onSuccess: () => { setSavedInterface(iface); setError(null); setActiveStep('contract'); },
    onError: () => setError(t('onboard.error.interface', 'Could not create the interface.')),
  });

  const goToStep = useCallback((s: OnboardStep) => setActiveStep(s), []);
  const stepIndex = STEP_ORDER.indexOf(activeStep);
  const isFirstStep = stepIndex === 0;
  const isLastStep = activeStep === 'review';

  const pending =
    registerMutation.isPending || interfaceMutation.isPending || contractForm.createMutation.isPending;

  const canSkip = activeStep === 'interface' || activeStep === 'contract';

  const canGoNext = (() => {
    if (activeStep === 'identity') return Object.keys(identityErrors).length === 0;
    if (activeStep === 'interface') return !interfaceTouched || Object.keys(interfaceErrors).length === 0;
    if (activeStep === 'contract') return contractForm.canCreate || (!contractForm.selectedType && !contractForm.selectedMode);
    return true; // review
  })();

  const onNext = useCallback(() => {
    setError(null);
    if (activeStep === 'identity') { registerMutation.mutate(); return; }
    if (activeStep === 'interface') {
      if (interfaceTouched && iface.name.trim()) { interfaceMutation.mutate(); return; }
      setActiveStep('contract'); return;
    }
    if (activeStep === 'contract') {
      if (contractForm.selectedType && contractForm.selectedMode) { contractForm.createMutation.mutate(); return; }
      setActiveStep('review'); return;
    }
    navigate(`/services/${serviceId}`); // review → finish
  }, [activeStep, interfaceTouched, iface.name, contractForm, registerMutation, interfaceMutation, navigate, serviceId]);

  const onBack = useCallback(() => {
    const prev = STEP_ORDER[Math.max(stepIndex - 1, 0)];
    if (prev) setActiveStep(prev);
  }, [stepIndex]);

  const onSkip = useCallback(() => {
    if (activeStep === 'interface') { setSavedInterface(null); setActiveStep('contract'); }
    else if (activeStep === 'contract') { setActiveStep('review'); }
  }, [activeStep]);

  const onCancel = useCallback(() => {
    // Passo 1 ainda não persistiu → descarta; depois do passo 1 o serviço existe.
    if (serviceId) navigate(`/services/${serviceId}`);
    else navigate('/services');
  }, [serviceId, navigate]);

  const steps: OnboardStepMeta[] = [
    { id: 'identity', label: t('onboard.steps.identity', 'Identity & ownership') },
    { id: 'interface', label: t('onboard.steps.interface', 'Interface'), optional: true },
    { id: 'contract', label: t('onboard.steps.contract', 'Contract'), optional: true },
    { id: 'review', label: t('onboard.steps.review', 'Review') },
  ];

  return {
    activeStep, steps, isFirstStep, isLastStep, canSkip, canGoNext, pending,
    identity, identityErrors, setIdentityField,
    interface: iface, interfaceErrors, setInterfaceField, interfaceTouched,
    savedInterface, contractForm, serviceId, createdDraftId, error,
    onNext, onBack, onSkip, onCancel,
  };
}
```

- [ ] **Step 5: Run test to verify it passes**

Run: `npm run test -- useOnboardWizard OnboardReviewStep`
Expected: PASS (3 tests + review renders).
Run: `npx tsc --noEmit`
Expected: no errors.

- [ ] **Step 6: Commit**

```bash
git add src/frontend/src/features/catalog/onboard/useOnboardWizard.ts src/frontend/src/features/catalog/onboard/OnboardReviewStep.tsx src/frontend/src/__tests__/catalog/useOnboardWizard.test.tsx
git commit -m "feat(catalog): useOnboardWizard orchestration + review step"
```

---

## Task 8: `OnboardWizardPage` + routes + wire entry CTAs

**Files:**
- Create: `src/frontend/src/features/catalog/onboard/OnboardWizardPage.tsx`
- Modify: `src/frontend/src/routes/catalogRoutes.tsx`
- Modify: `src/frontend/src/features/catalog/pages/ServiceCatalogPage.tsx`
- Modify: `src/frontend/src/features/catalog/pages/ServiceCatalogListPage.tsx`
- Modify: `src/frontend/src/features/catalog/browse/ServiceBrowseSurface.tsx`
- Test: `src/frontend/src/__tests__/catalog/OnboardWizardPage.test.tsx`

**Interfaces:**
- Consumes: `useOnboardWizard`, `OnboardWizardShell`, `OnboardIdentityPreview`, `ServiceIdentityForm`, `ServiceInterfaceForm`, `OnboardReviewStep`, `TypeModeTab`, `DetailsTab`.
- Produces: `OnboardWizardPage` (default-exportable named export) at route `/services/onboard`.

- [ ] **Step 1: Write the failing test**

```tsx
// src/frontend/src/__tests__/catalog/OnboardWizardPage.test.tsx
import { describe, it, expect, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';
import { OnboardWizardPage } from '../../features/catalog/onboard/OnboardWizardPage';

vi.mock('react-i18next', () => ({ useTranslation: () => ({ t: (_k: string, d?: string) => d ?? _k }) }));
vi.mock('../../features/catalog/api/serviceCatalog', () => ({
  serviceCatalogApi: { listServices: vi.fn(() => Promise.resolve({ items: [] })) },
}));
vi.mock('../../features/catalog/api', () => ({
  serviceCatalogApi: {
    registerService: vi.fn(() => Promise.resolve({ id: 'svc-1' })),
    createServiceInterface: vi.fn(() => Promise.resolve({})),
    listServices: vi.fn(() => Promise.resolve({ items: [] })),
  },
}));

describe('OnboardWizardPage', () => {
  it('renders step 1 (identity) with the service name field', () => {
    const qc = new QueryClient({ defaultOptions: { queries: { retry: false } } });
    render(
      <QueryClientProvider client={qc}>
        <MemoryRouter><OnboardWizardPage /></MemoryRouter>
      </QueryClientProvider>,
    );
    expect(screen.getByLabelText(/service name/i)).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /next/i })).toBeDisabled();
  });
});
```

- [ ] **Step 2: Run test to verify it fails**

Run: `npm run test -- OnboardWizardPage`
Expected: FAIL (module not found).

- [ ] **Step 3: Write `OnboardWizardPage`**

```tsx
// src/frontend/src/features/catalog/onboard/OnboardWizardPage.tsx
import { useTranslation } from 'react-i18next';
import { useOnboardWizard } from './useOnboardWizard';
import { OnboardWizardShell } from './OnboardWizardShell';
import { OnboardIdentityPreview } from './OnboardIdentityPreview';
import { ServiceIdentityForm } from './ServiceIdentityForm';
import { ServiceInterfaceForm } from './ServiceInterfaceForm';
import { OnboardReviewStep } from './OnboardReviewStep';
import { TypeModeTab } from '../../contracts/create/tabs/TypeModeTab';
import { DetailsTab } from '../../contracts/create/tabs/DetailsTab';

/** Página do wizard de onboarding de serviço (rota /services/onboard). */
export function OnboardWizardPage() {
  const { t } = useTranslation();
  const w = useOnboardWizard();

  return (
    <OnboardWizardShell
      title={t('onboard.title', 'Onboard a service')}
      steps={w.steps}
      activeStep={w.activeStep}
      preview={<OnboardIdentityPreview values={w.identity} />}
      canGoNext={w.canGoNext}
      isFirstStep={w.isFirstStep}
      isLastStep={w.isLastStep}
      canSkip={w.canSkip}
      pending={w.pending}
      onBack={w.onBack}
      onNext={w.onNext}
      onSkip={w.onSkip}
      onCancel={w.onCancel}
    >
      {w.error && (
        <div className="mb-4 rounded-lg border border-critical/30 bg-critical/10 px-4 py-3 text-sm text-critical">
          {w.error}
        </div>
      )}

      {w.activeStep === 'identity' && (
        <>
          <h2 className="text-base font-semibold text-heading mb-4">{t('onboard.identity.heading', 'Service identity & ownership')}</h2>
          <ServiceIdentityForm values={w.identity} errors={w.identityErrors} onChange={w.setIdentityField} />
        </>
      )}

      {w.activeStep === 'interface' && (
        <>
          <h2 className="text-base font-semibold text-heading mb-1">{t('onboard.interface.heading', 'Expose an interface (optional)')}</h2>
          <p className="text-xs text-muted mb-4">{t('onboard.interface.skipHint')}</p>
          <ServiceInterfaceForm values={w.interface} errors={w.interfaceErrors} onChange={w.setInterfaceField} />
        </>
      )}

      {w.activeStep === 'contract' && (
        <>
          <h2 className="text-base font-semibold text-heading mb-1">{t('onboard.contract.heading', 'Define a contract (optional)')}</h2>
          <p className="text-xs text-muted mb-4">{t('onboard.contract.skipHint')}</p>
          <TypeModeTab
            filteredContractTypes={w.contractForm.filteredContractTypes}
            selectedType={w.contractForm.selectedType}
            onSelectType={w.contractForm.selectType}
            selectedMode={w.contractForm.selectedMode}
            onSelectMode={w.contractForm.setSelectedMode}
          />
          {w.contractForm.selectedType && w.contractForm.selectedMode && (
            <div className="mt-5 pt-5 border-t border-edge">
              <DetailsTab form={w.contractForm} />
            </div>
          )}
        </>
      )}

      {w.activeStep === 'review' && (
        <OnboardReviewStep
          identity={w.identity}
          interfaceValues={w.savedInterface}
          contractSummary={
            w.contractForm.selectedType
              ? { title: w.contractForm.title, type: w.contractForm.selectedType }
              : null
          }
        />
      )}
    </OnboardWizardShell>
  );
}
```

- [ ] **Step 4: Register route + redirect `/services/new`**

In `catalogRoutes.tsx`:
- Add the lazy import after the `ServiceDetailPage` import:
  ```tsx
  const OnboardWizardPage = lazy(() => import('../features/catalog/onboard/OnboardWizardPage').then(m => ({ default: m.OnboardWizardPage })));
  ```
- Add the route (place directly before the `/services/new` route):
  ```tsx
  <Route
    path="/services/onboard"
    element={
      <ProtectedRoute permission="catalog:assets:write" redirectTo="/unauthorized">
        <OnboardWizardPage />
      </ProtectedRoute>
    }
  />
  ```
- Replace the `/services/new` route element with a redirect:
  ```tsx
  <Route path="/services/new" element={<Navigate to="/services/onboard" replace />} />
  ```
  (`Navigate` is already imported in this file.)

- [ ] **Step 5: Wire the entry CTAs to `/services/onboard`**

In `ServiceCatalogListPage.tsx` line ~169: change `navigate('/services/new')` → `navigate('/services/onboard')`.

In `ServiceCatalogPage.tsx`, the header CTA (lines ~148-155) has no onClick. Add it:
```tsx
<Button
  variant="ghost"
  size="sm"
  icon={<PlusCircle size={15} />}
  onClick={() => navigate('/services/onboard')}
>
  {t('serviceCatalog.registerService')}
</Button>
```
(`navigate` is already in scope.)

In `ServiceBrowseSurface.tsx`: thread an optional `onRegisterService?: () => void` prop and pass it as the onboarding empty-state action.
- Add to the props interface: `onRegisterService?: () => void;`
- Import `EmptyState` already present; the onboarding empty-state (line ~119) gets an action:
  ```tsx
  <EmptyState
    title={t('serviceCatalog.browse.empty.title')}
    description={t('serviceCatalog.browse.empty.desc')}
    variant="onboarding"
    action={
      onRegisterService ? (
        <Button variant="primary" size="sm" onClick={onRegisterService}>
          {t('serviceCatalog.registerService')}
        </Button>
      ) : undefined
    }
  />
  ```
- In `ServiceCatalogPage.tsx`, pass `onRegisterService={() => navigate('/services/onboard')}` to `<ServiceBrowseSurface … />`.

- [ ] **Step 6: Run tests + typecheck**

Run: `npm run test -- OnboardWizardPage ServiceCatalogPage ServiceBrowseSurface`
Expected: PASS.
Run: `npx tsc --noEmit`
Expected: no errors.

- [ ] **Step 7: Commit**

```bash
git add src/frontend/src/features/catalog/onboard/OnboardWizardPage.tsx src/frontend/src/routes/catalogRoutes.tsx src/frontend/src/features/catalog/pages/ServiceCatalogPage.tsx src/frontend/src/features/catalog/pages/ServiceCatalogListPage.tsx src/frontend/src/features/catalog/browse/ServiceBrowseSurface.tsx src/frontend/src/__tests__/catalog/OnboardWizardPage.test.tsx
git commit -m "feat(catalog): OnboardWizardPage at /services/onboard, wire entry CTAs, redirect /services/new"
```

---

## Task 9: Cleanup — remove dead studio routes + orphan overlays

**Files:**
- Modify: `src/frontend/src/routes/contractsRoutes.tsx`
- Delete: `src/frontend/src/features/catalog/components/{ServiceRegistrationOverlay,ContractImportOverlay,ServiceInterfaceOverlay,WizardOverlay}.tsx`
- Delete: `src/frontend/src/__tests__/catalog/{ServiceRegistrationOverlay,ContractImportOverlay,ServiceInterfaceOverlay,WizardOverlay}.test.tsx`

**Interfaces:** none new.

- [ ] **Step 1: Confirm no non-test references remain**

Run (from `src/frontend`):
```bash
grep -rn "ServiceRegistrationOverlay\|ContractImportOverlay\|ServiceInterfaceOverlay\|WizardOverlay" src --include=*.ts --include=*.tsx | grep -v "__tests__" | grep -v "features/catalog/components/"
```
Expected: no output (only the component files + their tests reference these names).

Run:
```bash
grep -rn "studio/rest\|studio/async\|studio/soap\|studio/graphql\|studio/protobuf" src --include=*.ts --include=*.tsx | grep -v "routes/contractsRoutes.tsx"
```
Expected: no output (nothing navigates to these dead type routes). If any output appears, STOP and report — a live consumer exists.

- [ ] **Step 2: Remove the dead studio type routes**

In `contractsRoutes.tsx`, delete the five `<Route>` blocks for `/contracts/studio/rest`, `/contracts/studio/async`, `/contracts/studio/soap`, `/contracts/studio/graphql`, `/contracts/studio/protobuf` (lines ~68-107) and the now-unused lazy imports `RestOpenApiBuilderPage`, `AsyncApiBuilderPage`, `SoapWsdlBuilderPage`, `GraphQLBuilderPage`, `ProtobufBuilderPage` (lines ~26-30). Keep `/contracts/studio/new` (ContractStudioPage) and `/contracts/studio/:draftId` (DraftStudioPage). Keep `ContractStudioPage` import.

> The builder page components (`RestOpenApiBuilderPage`, etc.) are NOT deleted — they may be reachable from within `ContractStudioPage`. Only their dead top-level routes and unused route-file imports are removed.

- [ ] **Step 3: Delete the orphan overlay components + tests**

```bash
git rm src/frontend/src/features/catalog/components/ServiceRegistrationOverlay.tsx \
       src/frontend/src/features/catalog/components/ContractImportOverlay.tsx \
       src/frontend/src/features/catalog/components/ServiceInterfaceOverlay.tsx \
       src/frontend/src/features/catalog/components/WizardOverlay.tsx \
       src/frontend/src/__tests__/catalog/ServiceRegistrationOverlay.test.tsx \
       src/frontend/src/__tests__/catalog/ContractImportOverlay.test.tsx \
       src/frontend/src/__tests__/catalog/ServiceInterfaceOverlay.test.tsx \
       src/frontend/src/__tests__/catalog/WizardOverlay.test.tsx
```

- [ ] **Step 4: Verify nothing broke**

Run: `npx tsc --noEmit`
Expected: no errors (no dangling imports).
Run: `npm run test`
Expected: full suite green (deleted specs no longer collected).

- [ ] **Step 5: Commit**

```bash
git add -A
git commit -m "chore(catalog): remove orphan overlays + dead studio type routes"
```

---

## Task 10: e2e journey + final gates

**Files:**
- Create: `src/frontend/e2e/service-onboarding.spec.ts`

**Interfaces:** none.

- [ ] **Step 1: Write the e2e spec**

```ts
// src/frontend/e2e/service-onboarding.spec.ts
import { test, expect } from '@playwright/test';
import { mockAuthSession } from './helpers/auth';

/**
 * E2E — jornada de onboarding de serviço (/services/onboard).
 * Cobre: criação do serviço no passo 1, saltar interface e contrato, concluir.
 */
test.describe('Service onboarding journey', () => {
  test.beforeEach(async ({ page }) => {
    await mockAuthSession(page);
    await page.route('**/api/v1/catalog/services', (route) => {
      if (route.request().method() === 'POST') {
        return route.fulfill({ status: 201, contentType: 'application/json', body: JSON.stringify({ id: 'svc-e2e-1' }) });
      }
      return route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify({ items: [] }) });
    });
    await page.route('**/api/v1/catalog/services/**', (route) =>
      route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify({ id: 'svc-e2e-1', name: 'orders-api', displayName: 'orders-api', domain: 'Commerce', serviceType: 'RestApi', lifecycleStatus: 'Planning', apis: [] }) }),
    );
  });

  test('creates a service then skips interface and contract to finish', async ({ page }) => {
    await page.goto('/services/onboard');

    // Passo 1: identidade
    await page.getByLabel(/service name/i).fill('orders-api');
    await page.getByLabel(/domain/i).fill('Commerce');
    await page.getByLabel(/team/i).fill('Orders');
    await page.getByRole('button', { name: /next/i }).click();

    // Passo 2: interface (saltar)
    await expect(page.getByText(/expose an interface/i)).toBeVisible({ timeout: 5_000 });
    await page.getByRole('button', { name: /skip/i }).click();

    // Passo 3: contrato (saltar)
    await expect(page.getByText(/define a contract/i)).toBeVisible({ timeout: 5_000 });
    await page.getByRole('button', { name: /skip/i }).click();

    // Passo 4: revisão → concluir
    await expect(page.getByText(/review & create/i)).toBeVisible({ timeout: 5_000 });
    await page.getByRole('button', { name: /finish/i }).click();

    await expect(page).toHaveURL(/\/services\/svc-e2e-1/, { timeout: 5_000 });
  });

  test('dead-CTA repair: /services/new redirects to onboard', async ({ page }) => {
    await page.goto('/services/new');
    await expect(page).toHaveURL(/\/services\/onboard/, { timeout: 5_000 });
    await expect(page.getByLabel(/service name/i)).toBeVisible({ timeout: 5_000 });
  });
});
```

- [ ] **Step 2: Run the e2e spec**

Run (PowerShell):
```
Set-Location "C:\Users\dlima\Documents\GitHub\NexTraceOne\src\frontend"; $env:CI=""; npx playwright test --project=chromium e2e/service-onboarding.spec.ts
```
Expected: 2 passed. (playwright.config auto-builds + serves with reuseExistingServer.)

- [ ] **Step 3: Final full gates**

Run (from `src/frontend`):
```bash
npm run test        # full unit suite — expect all green
npm run lint        # ESLint — expect clean on changed files
npm run validate:i18n
npm run build       # production build — expect exit 0
```
Expected: all pass. Fix any failure before committing.

- [ ] **Step 4: Commit**

```bash
git add src/frontend/e2e/service-onboarding.spec.ts
git commit -m "test(catalog): e2e onboarding journey + /services/new redirect"
```

---

## Self-Review

**1. Spec coverage:**
- §4.1 journey shape / shell / early-service persistence → Tasks 6 (shell), 7 (hook: early register on step-1 Next), 8 (page/route). ✓
- §4.2 step contents + reuse map → `ServiceIdentityForm` (T3), `ServiceInterfaceForm` (T4), `TypeModeTab`/`DetailsTab` reuse (T8), review honest-null (T7). ✓
- §4.3 unification/cleanup → CTA wiring + `/services/new` redirect (T8); dead studio routes + orphan overlays removed (T9); studio `:draftId`/`new` kept; AI scaffold untouched. ✓
- §4.4 states → per-step Zod validation (T2/T7), skip (T7 `onSkip`, T6 shell), exit-after-step-1 no orphan (T7 `onCancel`), pending (T6/T7), additive persistence no rollback (T7 sequential mutations). ✓
- §4.5 testing → unit per component (T2-T7), e2e journey + skip + redirect (T10), i18n 4 locales (T1), regression grep-guard (T9), gates (T10). ✓
- Fase 2 correctly NOT built. ✓

**2. Placeholder scan:** No TBD/TODO; every code step has complete code; every command has expected output. ✓

**3. Type consistency:** `ServiceIdentityValues`/`ServiceInterfaceValues` defined in T2, consumed unchanged in T3/T4/T7/T8. `OnboardStep`/`OnboardStepMeta` defined in T6, imported by T7. `onCreated` added in T5, consumed in T7. `useOnboardWizard` return shape (T7) matches `OnboardWizardPage` usage (T8) and the hook test (T7). `registerService` arg shape matches the real API signature (`team`, not `teamName`). `createServiceInterface` uses `serviceAssetId`. ✓

**Note on a pragmatic deviation from the spec:** the spec said "reusa ServiceIdentityCard". That card is a non-exported helper inside the 1578-line `ServiceDetailPage`; reusing it would force exporting a heavyweight, tightly-coupled component. Per YAGNI + surgical-changes, the plan builds a focused `OnboardIdentityPreview` instead. Same visual language, no coupling to the large file. This is the one intentional divergence.
