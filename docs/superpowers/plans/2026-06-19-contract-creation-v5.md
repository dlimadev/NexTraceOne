# Contract Creation v5 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking. UI craft tasks should also invoke `frontend-design:frontend-design`.

**Goal:** Mirror the Service Workspace v5 flagship for contracts — redesign the Contract Studio hub and rewrite the create flow as a 2-column workspace with a live contract identity card.

**Architecture:** Pure presentation/structure change. The monolithic `CreateContractPage` (~1000 lines) is split into a thin orchestrator + a `useContractDraftForm` hook (preserves all existing draft-creation logic verbatim) + a sticky `ContractIdentityCard` + 4 focused tab components. The hub repoints type cards to `/contracts/new?type=…`. No backend, API, or editor changes.

**Tech Stack:** React 19, TypeScript 5.9, TanStack Query 5, react-router-dom 7, Tailwind 4, design-system primitives from `src/frontend/src/shared/ui`, Vitest + Testing Library, i18next (pt/en/es/fr).

---

## Conventions for this plan

- All paths are relative to repo root. Frontend root: `src/frontend/`.
- Run tests from `src/frontend/`: `npm run test -- <file>` (Vitest).
- Lint: `npm run lint`. Build: `npm run build`.
- DS imports come from `../../../shared/ui` (or correct depth): `Button, TextField, TextArea, Select, SearchInput, Badge`.
- Commit after each task with the message shown.
- **Do not touch** `studio/`, `workspace/`, or `pages/*BuilderPage.tsx`.

## File structure (created / modified)

| File | Responsibility |
|---|---|
| `features/contracts/create/contractCreateConstants.ts` | **Create.** Hub→ContractType map, BEST_FOR copy keys, CREATION_MODES, FORM_TABS, type icon map. |
| `features/contracts/create/useContractDraftForm.ts` | **Create.** Form state + derived `summary` + create mutation (preserved logic). |
| `features/contracts/create/ContractIdentityCard.tsx` | **Create.** Sticky left card; live preview of the draft. |
| `features/contracts/create/tabs/ServiceTab.tsx` | **Create.** Service search + select. |
| `features/contracts/create/tabs/TypeModeTab.tsx` | **Create.** Type gallery (policy-filtered) + creation mode. |
| `features/contracts/create/tabs/DetailsTab.tsx` | **Create.** Title/description/protocol + type-specific metadata. |
| `features/contracts/create/tabs/ConfirmTab.tsx` | **Create.** Read-only recap + create CTA. |
| `features/contracts/create/CreateContractPage.tsx` | **Rewrite.** 2-column orchestrator wiring the above. |
| `features/contracts/pages/ContractStudioPage.tsx` | **Modify.** "Best for" line + deep links + repoint cards. |
| `__tests__/pages/CreateContractPage.test.tsx` | **Modify.** Tabs + live card + pre-seed coverage. |
| `__tests__/contracts/ContractStudioPage.test.tsx` | **Modify.** Best-for + new card target. |
| `locales/{pt,en,es,fr}/translation.json` | **Modify.** New i18n keys. |

---

## Task 1: Create-flow constants + hub→type mapping

**Files:**
- Create: `src/frontend/src/features/contracts/create/contractCreateConstants.ts`
- Test: `src/frontend/src/__tests__/contracts/contractCreateConstants.test.ts`

- [ ] **Step 1: Write the failing test**

```ts
// src/frontend/src/__tests__/contracts/contractCreateConstants.test.ts
import { describe, it, expect } from 'vitest';
import { HUB_KEY_TO_CONTRACT_TYPE, BEST_FOR_KEY, FORM_TABS, CREATION_MODES } from '../../features/contracts/create/contractCreateConstants';

describe('contractCreateConstants', () => {
  it('maps every hub card key to a ContractType value', () => {
    expect(HUB_KEY_TO_CONTRACT_TYPE['rest-openapi']).toBe('RestApi');
    expect(HUB_KEY_TO_CONTRACT_TYPE['asyncapi']).toBe('Event');
    expect(HUB_KEY_TO_CONTRACT_TYPE['soap-wsdl']).toBe('Soap');
    expect(HUB_KEY_TO_CONTRACT_TYPE['shared-schema']).toBe('SharedSchema');
  });

  it('has a best-for key for each contract type', () => {
    expect(BEST_FOR_KEY('RestApi')).toBe('contracts.create.bestFor.RestApi');
  });

  it('defines the four ordered form tabs', () => {
    expect(FORM_TABS).toEqual(['service', 'typeMode', 'details', 'confirm']);
  });

  it('defines three creation modes', () => {
    expect(CREATION_MODES.map((m) => m.id)).toEqual(['visual', 'import', 'ai']);
  });
});
```

- [ ] **Step 2: Run test to verify it fails**

Run: `cd src/frontend && npm run test -- src/__tests__/contracts/contractCreateConstants.test.ts`
Expected: FAIL — module not found.

- [ ] **Step 3: Write the implementation**

```ts
// src/frontend/src/features/contracts/create/contractCreateConstants.ts
import { Globe, Server, Zap, Cog, Database, FileCode, MessageSquare, AlignJustify, Terminal, Webhook, Columns, Upload, Sparkles } from 'lucide-react';
import type { ContractTypeValue } from '../shared/constants';

/** Mapeia as chaves de card do hub (ContractStudioPage) para o enum ContractType do wizard. */
export const HUB_KEY_TO_CONTRACT_TYPE: Record<string, ContractTypeValue> = {
  'rest-openapi': 'RestApi',
  'asyncapi': 'Event',
  'soap-wsdl': 'Soap',
  'graphql': 'RestApi',          // GraphQL ainda mapeado p/ RestApi (protocolo GraphQl reservado)
  'protobuf': 'RestApi',         // Protobuf reservado — entra como RestApi até builder dedicado
  'shared-schema': 'SharedSchema',
};

/** Chave i18n da linha "Best for" por tipo de contrato. */
export const BEST_FOR_KEY = (type: ContractTypeValue | string): string => `contracts.create.bestFor.${type}`;

/** Ícone por ContractType (para galeria do TypeModeTab e cartão de identidade). */
export const TYPE_ICONS: Record<string, React.ComponentType<{ size?: number; className?: string }>> = {
  RestApi: Globe,
  Soap: Server,
  Event: Zap,
  BackgroundService: Cog,
  SharedSchema: Database,
  Copybook: FileCode,
  MqMessage: MessageSquare,
  FixedLayout: AlignJustify,
  CicsCommarea: Terminal,
  Webhook: Webhook,
};

export type CreationMode = 'visual' | 'import' | 'ai';

export const CREATION_MODES: { id: CreationMode; labelKey: string; descriptionKey: string; Icon: React.ComponentType<{ size?: number; className?: string }> }[] = [
  { id: 'visual', labelKey: 'contracts.create.modeVisual', descriptionKey: 'contracts.create.modeVisualDesc', Icon: Columns },
  { id: 'import', labelKey: 'contracts.create.modeImport', descriptionKey: 'contracts.create.modeImportDesc', Icon: Upload },
  { id: 'ai', labelKey: 'contracts.create.modeAi', descriptionKey: 'contracts.create.modeAiDesc', Icon: Sparkles },
];

export type FormTab = 'service' | 'typeMode' | 'details' | 'confirm';

export const FORM_TABS: FormTab[] = ['service', 'typeMode', 'details', 'confirm'];

export const FORM_TAB_LABEL_KEY: Record<FormTab, string> = {
  service: 'contracts.create.tabService',
  typeMode: 'contracts.create.tabTypeMode',
  details: 'contracts.create.tabDetails',
  confirm: 'contracts.create.tabConfirm',
};
```

- [ ] **Step 4: Run test to verify it passes**

Run: `cd src/frontend && npm run test -- src/__tests__/contracts/contractCreateConstants.test.ts`
Expected: PASS (4 tests).

- [ ] **Step 5: Commit**

```bash
git add src/frontend/src/features/contracts/create/contractCreateConstants.ts src/frontend/src/__tests__/contracts/contractCreateConstants.test.ts
git commit -m "feat(contracts): constantes do create workspace v5 (hub→type map, tabs, modos)"
```

---

## Task 2: `useContractDraftForm` hook (preserved create logic + live summary)

This hook extracts the form state, the derived `summary` (for the live card), and the create mutation **verbatim** from the current `CreateContractPage.tsx` (lines 105-294). Behavior must be identical: same `contractStudioApi` calls, same `navigate('/contracts/studio/{draftId}')`.

**Files:**
- Create: `src/frontend/src/features/contracts/create/useContractDraftForm.ts`
- Test: `src/frontend/src/__tests__/contracts/useContractDraftForm.test.tsx`

- [ ] **Step 1: Write the failing test**

```tsx
// src/frontend/src/__tests__/contracts/useContractDraftForm.test.tsx
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { renderHook, act } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';
import * as React from 'react';

vi.mock('../../features/contracts/api/contractStudio', () => ({
  contractStudioApi: { createDraft: vi.fn().mockResolvedValue({ draftId: 'd-1' }) },
}));
vi.mock('../../features/catalog/api/serviceCatalog', () => ({
  serviceCatalogApi: { listServices: vi.fn().mockResolvedValue({ items: [] }) },
}));
vi.mock('../../contexts/AuthContext', () => ({
  useAuth: () => ({ user: { email: 'me@x.io' } }),
}));

import { useContractDraftForm } from '../../features/contracts/create/useContractDraftForm';

function wrapper({ children }: { children: React.ReactNode }) {
  const qc = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return (
    <QueryClientProvider client={qc}>
      <MemoryRouter>{children}</MemoryRouter>
    </QueryClientProvider>
  );
}

describe('useContractDraftForm', () => {
  beforeEach(() => vi.clearAllMocks());

  it('summary reflects live form fields', () => {
    const { result } = renderHook(() => useContractDraftForm({}), { wrapper });
    act(() => result.current.setField('title')({ target: { value: 'Orders API' } } as never));
    expect(result.current.summary.title).toBe('Orders API');
  });

  it('pre-seeds type and mode from initial args', () => {
    const { result } = renderHook(() => useContractDraftForm({ initialType: 'RestApi', initialMode: 'import' }), { wrapper });
    expect(result.current.selectedType).toBe('RestApi');
    expect(result.current.selectedMode).toBe('import');
  });
});
```

- [ ] **Step 2: Run test to verify it fails**

Run: `cd src/frontend && npm run test -- src/__tests__/contracts/useContractDraftForm.test.tsx`
Expected: FAIL — module not found.

- [ ] **Step 3: Write the implementation**

Create `useContractDraftForm.ts`. Move the following from the current `CreateContractPage.tsx` **unchanged in behavior**:
- All `useState` for form fields (current lines 108-135).
- `servicesQuery`, `prefilledServiceQuery`, derived `effectiveServiceType`, `filteredContractTypes`, `availableServices`, `filteredServices`, `protocols`, `canProceed*`/`canCreate` (current lines 143-294).
- The `createMutation` block (current lines 186-278) **verbatim**, including the SOAP/Event/BackgroundService/AI branches and `onSuccess: navigate('/contracts/studio/{draftId}')`.

Expose this shape:

```ts
import { useState, useMemo } from 'react';
import { useNavigate } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { useMutation, useQuery } from '@tanstack/react-query';
import { PROTOCOL_BY_TYPE, type ContractTypeValue } from '../shared/constants';
import { supportsContracts, allowedContractTypes } from '../shared/serviceContractPolicy';
import { contractStudioApi } from '../api/contractStudio';
import { serviceCatalogApi } from '../../catalog/api/serviceCatalog';
import { useAuth } from '../../../contexts/AuthContext';
import type { ContractProtocol, ContractType } from '../types';
import type { ServiceType } from '../../../types';
import type { CreationMode } from './contractCreateConstants';

interface UseContractDraftFormArgs {
  prefilledServiceId?: string;
  initialType?: ContractTypeValue | null;
  initialMode?: CreationMode | null;
}

export function useContractDraftForm(args: UseContractDraftFormArgs) {
  // ... all moved state + queries + mutation (behavior identical to current page) ...

  /** Resumo derivado para o cartão de identidade (atualiza ao vivo). */
  const summary = useMemo(() => ({
    title,                         // string
    serviceName: selectedServiceDisplay?.displayName ?? '',
    type: selectedType,            // ContractTypeValue | null
    protocol: selectedProtocol,    // ContractProtocol | ''
    mode: selectedMode,            // CreationMode | null
    proposedVersion: '1.0.0',
    author: currentActor,
  }), [title, selectedServiceDisplay, selectedType, selectedProtocol, selectedMode, currentActor]);

  return {
    // state + setters
    setField, title, description, /* ...all fields... */
    selectedType, setSelectedType, selectedMode, setSelectedMode,
    selectedProtocol, setSelectedProtocol, linkedServiceId, setLinkedServiceId,
    selectedServiceType, setSelectedServiceType, serviceSearch, setServiceSearch,
    // derived
    effectiveServiceType, filteredContractTypes, availableServices, filteredServices,
    selectedServiceDisplay, serviceSupportsContracts, protocols,
    canProceedFromService, canProceedFromConfigure, canCreate, summary,
    // queries + mutation
    servicesQuery, createMutation,
  };
}
```

Initialize `selectedType`/`selectedMode` from `args.initialType`/`args.initialMode` (instead of `null`), and `linkedServiceId`/`step` from `args.prefilledServiceId` as today.

- [ ] **Step 4: Run test to verify it passes**

Run: `cd src/frontend && npm run test -- src/__tests__/contracts/useContractDraftForm.test.tsx`
Expected: PASS (2 tests).

- [ ] **Step 5: Commit**

```bash
git add src/frontend/src/features/contracts/create/useContractDraftForm.ts src/frontend/src/__tests__/contracts/useContractDraftForm.test.tsx
git commit -m "feat(contracts): hook useContractDraftForm (logica de criacao preservada + summary vivo)"
```

---

## Task 3: `ContractIdentityCard` (sticky live preview)

Mirrors `ServiceIdentityCard` from `ServiceDetailPage.tsx:679-775`. Pure presentational, driven by `summary`.

**Files:**
- Create: `src/frontend/src/features/contracts/create/ContractIdentityCard.tsx`
- Test: `src/frontend/src/__tests__/contracts/ContractIdentityCard.test.tsx`

- [ ] **Step 1: Write the failing test**

```tsx
// src/frontend/src/__tests__/contracts/ContractIdentityCard.test.tsx
import { describe, it, expect, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import * as React from 'react';
vi.mock('react-i18next', () => ({ useTranslation: () => ({ t: (k: string, d?: string) => d ?? k }) }));
import { ContractIdentityCard } from '../../features/contracts/create/ContractIdentityCard';

const base = { title: '', serviceName: '', type: null, protocol: '', mode: null, proposedVersion: '1.0.0', author: 'me@x.io' };

describe('ContractIdentityCard', () => {
  it('shows placeholder name when empty', () => {
    render(<ContractIdentityCard summary={base} />);
    expect(screen.getByText('novo-contrato')).toBeInTheDocument();
  });
  it('reflects live title and service', () => {
    render(<ContractIdentityCard summary={{ ...base, title: 'Orders API', serviceName: 'Payments' }} />);
    expect(screen.getByText('Orders API')).toBeInTheDocument();
    expect(screen.getByText(/Payments/)).toBeInTheDocument();
  });
  it('always renders a Draft badge in create mode', () => {
    render(<ContractIdentityCard summary={base} />);
    expect(screen.getByText('Draft')).toBeInTheDocument();
  });
});
```

- [ ] **Step 2: Run test to verify it fails**

Run: `cd src/frontend && npm run test -- src/__tests__/contracts/ContractIdentityCard.test.tsx`
Expected: FAIL — module not found.

- [ ] **Step 3: Write the implementation**

```tsx
// src/frontend/src/features/contracts/create/ContractIdentityCard.tsx
import { useTranslation } from 'react-i18next';
import { Badge } from '../../../shared/ui';
import { cn } from '../../../lib/cn';
import { PROTOCOL_COLORS } from '../shared/constants';
import { TYPE_ICONS } from './contractCreateConstants';
import type { ContractTypeValue } from '../shared/constants';
import type { CreationMode } from './contractCreateConstants';

export interface ContractSummary {
  title: string;
  serviceName: string;
  type: ContractTypeValue | null;
  protocol: string;
  mode: CreationMode | null;
  proposedVersion: string;
  author: string;
}

/** Cartão de identidade do contrato — preview vivo à esquerda do create workspace (padrão v5). */
export function ContractIdentityCard({ summary }: { summary: ContractSummary }) {
  const { t } = useTranslation();
  const Icon = summary.type ? (TYPE_ICONS[summary.type] ?? TYPE_ICONS.RestApi) : TYPE_ICONS.RestApi;
  const name = summary.title.trim() || t('contracts.create.draftNamePlaceholder', 'novo-contrato');
  const hasTitle = summary.title.trim().length > 0;

  return (
    <div className="rounded-2xl border border-edge bg-card overflow-hidden shadow-sm">
      <div className="bg-gradient-to-b from-accent/10 to-transparent p-4">
        <div className="flex items-center gap-3">
          <div className={cn('flex items-center justify-center w-11 h-11 rounded-xl shrink-0', summary.type ? 'bg-accent text-white' : 'bg-accent/20 text-accent')}>
            <Icon size={20} />
          </div>
          <div className="min-w-0 flex-1">
            <p className={cn('font-mono text-sm font-semibold truncate', hasTitle ? 'text-heading' : 'text-muted')}>{name}</p>
            <p className="text-xs text-muted truncate mt-0.5">{summary.serviceName ? `↳ ${summary.serviceName}` : '—'}</p>
          </div>
          <Badge variant="warning" size="sm" className="shrink-0 ml-auto">{t('contracts.draftStatus.Editing', 'Draft')}</Badge>
        </div>
        <div className="flex flex-wrap gap-1.5 mt-3">
          {summary.type && <Badge variant="primary" size="sm">{t(`contracts.contractTypes.${summary.type}`, summary.type)}</Badge>}
          {summary.protocol && <span className={cn('text-[10px] px-1.5 py-0.5 rounded font-medium', PROTOCOL_COLORS[summary.protocol] ?? 'bg-muted/15 text-muted border border-muted/25')}>{summary.protocol}</span>}
          {summary.mode && <Badge variant="default" size="sm">{t(`contracts.create.mode${summary.mode.charAt(0).toUpperCase() + summary.mode.slice(1)}`, summary.mode)}</Badge>}
        </div>
      </div>

      <div className="grid grid-cols-3 gap-px bg-edge border-t border-b border-edge">
        <MiniStat value={summary.proposedVersion} label={t('contracts.create.cardVersion', 'Version')} mono />
        <MiniStat value="0" label={t('contracts.create.cardOperations', 'Operations')} />
        <MiniStat value="—" label={t('contracts.create.cardValidation', 'Validation')} muted />
      </div>

      <div className="px-4 py-2 divide-y divide-edge/60">
        <MetaRow label={t('contracts.create.cardService', 'Service')} value={summary.serviceName || '—'} />
        <MetaRow label={t('contracts.create.cardProtocol', 'Protocol')} value={summary.protocol || '—'} />
        <MetaRow label={t('contracts.create.cardAuthor', 'Author')} value={summary.author || '—'} />
      </div>

      <p className="text-[11px] text-muted text-center py-2 px-4 border-t border-edge">
        {t('contracts.create.livePreviewHint', 'Resumo atualiza ao vivo')}
      </p>
    </div>
  );
}

function MiniStat({ value, label, mono, muted }: { value: string; label: string; mono?: boolean; muted?: boolean }) {
  return (
    <div className="bg-deep text-center py-3">
      <p className={cn('text-sm font-bold', muted ? 'text-muted' : 'text-heading', mono && 'font-mono')}>{value}</p>
      <p className="text-[10px] text-muted mt-0.5">{label}</p>
    </div>
  );
}

function MetaRow({ label, value }: { label: string; value: string }) {
  return (
    <div className="flex items-center justify-between py-2 text-xs">
      <span className="text-muted">{label}</span>
      <span className="text-heading font-medium truncate ml-2">{value}</span>
    </div>
  );
}
```

- [ ] **Step 4: Run test to verify it passes**

Run: `cd src/frontend && npm run test -- src/__tests__/contracts/ContractIdentityCard.test.tsx`
Expected: PASS (3 tests).

- [ ] **Step 5: Commit**

```bash
git add src/frontend/src/features/contracts/create/ContractIdentityCard.tsx src/frontend/src/__tests__/contracts/ContractIdentityCard.test.tsx
git commit -m "feat(contracts): ContractIdentityCard (preview vivo, padrao v5)"
```

---

## Task 4: Tab components (Service, TypeMode, Details, Confirm)

Each tab is a focused presentational component receiving the relevant slice of the hook. Relocate existing JSX from `CreateContractPage.tsx` into these files, swapping raw `<input>/<select>/<textarea>/<button>` for DS `TextField/TextArea/Select/SearchInput/Button` where straightforward.

**Files:**
- Create: `src/frontend/src/features/contracts/create/tabs/ServiceTab.tsx`
- Create: `src/frontend/src/features/contracts/create/tabs/TypeModeTab.tsx`
- Create: `src/frontend/src/features/contracts/create/tabs/DetailsTab.tsx`
- Create: `src/frontend/src/features/contracts/create/tabs/ConfirmTab.tsx`
- Test: `src/frontend/src/__tests__/contracts/contractCreateTabs.test.tsx`

- [ ] **Step 1: Write the failing test**

```tsx
// src/frontend/src/__tests__/contracts/contractCreateTabs.test.tsx
import { describe, it, expect, vi } from 'vitest';
import { render, screen, fireEvent } from '@testing-library/react';
import * as React from 'react';
vi.mock('react-i18next', () => ({ useTranslation: () => ({ t: (k: string, d?: string) => d ?? k }) }));
import { TypeModeTab } from '../../features/contracts/create/tabs/TypeModeTab';

const TYPES = [{ value: 'RestApi', labelKey: 'contracts.contractTypes.RestApi' }];

describe('TypeModeTab', () => {
  it('renders a best-for line per type card and reports selection', () => {
    const onType = vi.fn();
    render(
      <TypeModeTab
        filteredContractTypes={TYPES as never}
        selectedType="RestApi"
        onSelectType={onType}
        selectedMode="visual"
        onSelectMode={vi.fn()}
      />,
    );
    expect(screen.getByText(/Best for HTTP/i)).toBeInTheDocument();
    fireEvent.click(screen.getByText('REST / OpenAPI'));
    expect(onType).toHaveBeenCalled();
  });
});
```

- [ ] **Step 2: Run test to verify it fails**

Run: `cd src/frontend && npm run test -- src/__tests__/contracts/contractCreateTabs.test.tsx`
Expected: FAIL — module not found.

- [ ] **Step 3: Write the implementations**

`TypeModeTab.tsx` — type gallery (filtered) + mode gallery. Each type card shows icon, label, the **`BEST_FOR_KEY(ct.value)`** line, and protocol chips (`PROTOCOL_BY_TYPE`). Reuse the selected/idle card styling from current `CreateContractPage.tsx:558-651`. Props:

```tsx
import { useTranslation } from 'react-i18next';
import { PROTOCOL_BY_TYPE, PROTOCOL_COLORS, type ContractTypeValue } from '../../shared/constants';
import { TYPE_ICONS, CREATION_MODES, BEST_FOR_KEY, type CreationMode } from '../contractCreateConstants';

interface TypeModeTabProps {
  filteredContractTypes: ReadonlyArray<{ value: string; labelKey: string }>;
  selectedType: ContractTypeValue | null;
  onSelectType: (t: ContractTypeValue) => void;
  selectedMode: CreationMode | null;
  onSelectMode: (m: CreationMode) => void;
}
export function TypeModeTab(props: TypeModeTabProps) { /* gallery markup incl. <p>{t(BEST_FOR_KEY(ct.value))}</p> */ }
```

`ServiceTab.tsx` — relocate `CreateContractPage.tsx:373-501` body (the search + service cards grid). Swap the raw search `<input>` for `SearchInput`. Props: `{ filteredServices, linkedServiceId, onSelectService, serviceSearch, onSearchChange, isLoading }`.

`DetailsTab.tsx` — relocate `CreateContractPage.tsx:707-1044` (the "Main form card" through the BackgroundService metadata block). Swap raw `<input>/<textarea>/<select>` for `TextField/TextArea/Select`. Keep all conditionals (`isSoapType`, `isEventType`, `isBackgroundServiceType`, `selectedMode === 'import'|'ai'`, `protocols.length > 1`). Props: the relevant fields + setters from the hook.

`ConfirmTab.tsx` — new read-only recap: service, type, mode, protocol, title/description, and the create error block (relocate `CreateContractPage.tsx:1047-1052`). Props: `{ summary, description, canCreate, isCreating, onCreate, isError }` with a primary `Button` calling `onCreate`.

- [ ] **Step 4: Run test to verify it passes**

Run: `cd src/frontend && npm run test -- src/__tests__/contracts/contractCreateTabs.test.tsx`
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add src/frontend/src/features/contracts/create/tabs src/frontend/src/__tests__/contracts/contractCreateTabs.test.tsx
git commit -m "feat(contracts): tabs do create workspace (Service/TypeMode/Details/Confirm) em DS"
```

---

## Task 5: Rewrite `CreateContractPage` as the 2-column orchestrator

**Files:**
- Modify (rewrite): `src/frontend/src/features/contracts/create/CreateContractPage.tsx`

- [ ] **Step 1: Update the page test first**

Replace `src/frontend/src/__tests__/pages/CreateContractPage.test.tsx` body with coverage of the new structure (keep the existing mocks block at the top; add `useContractList`-free assertions):

```tsx
it('renders the live identity card and form tabs', async () => {
  renderPage();
  expect(await screen.findByText('Resumo atualiza ao vivo')).toBeInTheDocument();
  expect(screen.getByText('Serviço')).toBeInTheDocument();
  expect(screen.getByText('Tipo & Modo')).toBeInTheDocument();
});

it('pre-seeds type from ?type= query param', async () => {
  // render within <MemoryRouter initialEntries={['/contracts/new?type=RestApi&serviceId=svc-1']}>
  //   <Routes><Route path="/contracts/new" element={<CreateContractPage/>}/></Routes>
  // assert the REST type card is selected (aria-pressed / selected class) in the Tipo & Modo tab
});
```

Note: the i18n mock returns the key; assert against keys or default strings accordingly (the page mock `t` should return the default — align with `vi.mock('react-i18next', () => ({ useTranslation: () => ({ t: (k,d)=>d??k }) }))`). Update the mock at the top of the file to this form.

- [ ] **Step 2: Run test to verify it fails**

Run: `cd src/frontend && npm run test -- src/__tests__/pages/CreateContractPage.test.tsx`
Expected: FAIL — "Resumo atualiza ao vivo" not found (old structure).

- [ ] **Step 3: Rewrite the page**

```tsx
// src/frontend/src/features/contracts/create/CreateContractPage.tsx
import { useState } from 'react';
import { useNavigate, useSearchParams } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { ChevronLeft, ArrowLeft, ArrowRight, Check, X } from 'lucide-react';
import { PageContainer } from '../../../components/shell';
import { Button } from '../../../shared/ui';
import { cn } from '../../../lib/cn';
import { useContractDraftForm } from './useContractDraftForm';
import { ContractIdentityCard } from './ContractIdentityCard';
import { ServiceTab } from './tabs/ServiceTab';
import { TypeModeTab } from './tabs/TypeModeTab';
import { DetailsTab } from './tabs/DetailsTab';
import { ConfirmTab } from './tabs/ConfirmTab';
import { FORM_TABS, FORM_TAB_LABEL_KEY, type FormTab, type CreationMode } from './contractCreateConstants';
import type { ContractTypeValue } from '../shared/constants';

export function CreateContractPage() {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();
  const prefilledServiceId = searchParams.get('serviceId') ?? '';
  const initialType = (searchParams.get('type') as ContractTypeValue | null) ?? null;
  const initialMode = (searchParams.get('mode') as CreationMode | null) ?? null;

  const form = useContractDraftForm({ prefilledServiceId, initialType, initialMode });

  // Se serviço já vem pré-preenchido, começa na tab de tipo.
  const [activeTab, setActiveTab] = useState<FormTab>(prefilledServiceId ? 'typeMode' : 'service');
  const tabIndex = FORM_TABS.indexOf(activeTab);
  const goNext = () => { const n = FORM_TABS[Math.min(tabIndex + 1, FORM_TABS.length - 1)]; if (n) setActiveTab(n); };
  const goPrev = () => { const p = FORM_TABS[Math.max(tabIndex - 1, 0)]; if (p) setActiveTab(p); };

  return (
    <PageContainer className="animate-fade-in">
      <div className="flex items-center justify-between mb-4">
        <button onClick={() => navigate('/contracts/studio/new')} className="inline-flex items-center gap-1.5 text-sm text-muted hover:text-heading transition-colors">
          <ChevronLeft size={14} /> {t('contractStudio.title', 'Contract Studio')}
        </button>
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-[300px_minmax(0,1fr)] gap-6 items-start">
        <div className="lg:sticky lg:top-4">
          <ContractIdentityCard summary={form.summary} />
        </div>

        <div className="min-w-0">
          <div className="flex items-center justify-between mb-4">
            <h1 className="text-lg font-bold text-heading">{t('contracts.create.title', 'Novo contrato')}</h1>
            <div className="flex items-center gap-2">
              <Button variant="ghost" size="sm" icon={<X size={14} />} onClick={() => navigate('/contracts')}>
                {t('common.cancel', 'Cancelar')}
              </Button>
              <Button variant="primary" size="sm" icon={<Check size={14} />} loading={form.createMutation.isPending}
                disabled={!form.canCreate} onClick={() => form.createMutation.mutate()}>
                {t('contracts.create.createDraft', 'Criar draft')}
              </Button>
            </div>
          </div>

          {/* Stepper tabs */}
          <div className="flex gap-0.5 border-b border-edge overflow-x-auto">
            {FORM_TABS.map((tab, idx) => (
              <button key={tab} type="button" onClick={() => setActiveTab(tab)}
                className={cn('flex items-center gap-2 px-4 py-2.5 text-sm font-semibold whitespace-nowrap border-b-2 transition-colors',
                  activeTab === tab ? 'text-accent border-accent' : 'text-muted border-transparent hover:text-heading')}>
                <span className={cn('w-5 h-5 rounded-full text-[11px] flex items-center justify-center font-bold', activeTab === tab ? 'bg-accent text-white' : 'bg-elevated text-muted')}>{idx + 1}</span>
                {t(FORM_TAB_LABEL_KEY[tab], tab)}
              </button>
            ))}
          </div>

          <div className="bg-card border border-edge border-t-0 rounded-b-xl p-5">
            {activeTab === 'service' && <ServiceTab {...{ filteredServices: form.filteredServices, linkedServiceId: form.linkedServiceId, serviceSearch: form.serviceSearch, isLoading: form.servicesQuery.isLoading, onSearchChange: form.setServiceSearch, onSelectService: (svc) => { form.setLinkedServiceId(svc.serviceId); form.setSelectedServiceType(svc.serviceType); } }} />}
            {activeTab === 'typeMode' && <TypeModeTab filteredContractTypes={form.filteredContractTypes} selectedType={form.selectedType} onSelectType={form.setSelectedType} selectedMode={form.selectedMode} onSelectMode={form.setSelectedMode} />}
            {activeTab === 'details' && <DetailsTab form={form} />}
            {activeTab === 'confirm' && <ConfirmTab summary={form.summary} description={form.description} canCreate={form.canCreate} isCreating={form.createMutation.isPending} isError={form.createMutation.isError} onCreate={() => form.createMutation.mutate()} />}

            <div className="flex justify-between pt-4 mt-4 border-t border-edge">
              <Button variant="ghost" size="sm" icon={<ArrowLeft size={14} />} onClick={goPrev} disabled={tabIndex === 0}>
                {t('common.back', 'Anterior')}
              </Button>
              {tabIndex < FORM_TABS.length - 1 && (
                <Button variant="primary" size="sm" onClick={goNext}>
                  {t('common.next', 'Próximo')} <ArrowRight size={14} />
                </Button>
              )}
            </div>
          </div>
        </div>
      </div>
    </PageContainer>
  );
}
```

Adjust prop wiring to match the exact field/setter names exposed by `useContractDraftForm` (Task 2). `DetailsTab` receives the whole `form` for brevity; alternatively destructure explicit props.

- [ ] **Step 4: Run test to verify it passes**

Run: `cd src/frontend && npm run test -- src/__tests__/pages/CreateContractPage.test.tsx`
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add src/frontend/src/features/contracts/create/CreateContractPage.tsx src/frontend/src/__tests__/pages/CreateContractPage.test.tsx
git commit -m "feat(contracts): reescreve CreateContractPage como workspace 2 colunas v5"
```

---

## Task 6: Hub — "Best for" lines + deep links + repoint cards

**Files:**
- Modify: `src/frontend/src/features/contracts/pages/ContractStudioPage.tsx`
- Modify: `src/frontend/src/__tests__/contracts/ContractStudioPage.test.tsx`

- [ ] **Step 1: Add the failing test**

Append to `ContractStudioPage.test.tsx`:

```tsx
it('shows a best-for line on the REST card and targets the create workspace', () => {
  wrap(<ContractStudioPage />);
  const card = screen.getByTestId('type-card-rest-openapi');
  expect(card).toBeInTheDocument();
  // best-for copy is rendered (i18n mock returns the key)
  expect(screen.getByText('contracts.create.bestFor.RestApi')).toBeInTheDocument();
});
```

- [ ] **Step 2: Run test to verify it fails**

Run: `cd src/frontend && npm run test -- src/__tests__/contracts/ContractStudioPage.test.tsx`
Expected: FAIL — best-for text not found.

- [ ] **Step 3: Implement the hub edits**

In `ContractStudioPage.tsx`:
- Add a `bestForKey` to each `CONTRACT_TYPES` entry, e.g. REST → `'contracts.create.bestFor.RestApi'`, AsyncAPI → `'contracts.create.bestFor.Event'`, SOAP → `'contracts.create.bestFor.Soap'`, GraphQL → `'contracts.create.bestFor.RestApi'`, Protobuf → `'contracts.create.bestFor.RestApi'`, Shared Schema → `'contracts.create.bestFor.SharedSchema'`.
- In `ContractTypeCard`, render `<p className="text-xs text-muted ...">{t(type.bestForKey)}</p>` and add two footer buttons: `[Design]` → `onSelect('visual')`, `[Import]` → `onSelect('import')`.
- Replace each card `route` usage: import `HUB_KEY_TO_CONTRACT_TYPE` from `../create/contractCreateConstants`. `onSelect(mode)` navigates to `/contracts/new?type=${HUB_KEY_TO_CONTRACT_TYPE[type.key]}${mode ? `&mode=${mode}` : ''}`. Primary card click → no mode. Remove navigation to `/contracts/studio/${type.key}` (builders demoted).
- Keep the `data-testid={\`type-card-${type.key}\`}` attributes (tests depend on them).

- [ ] **Step 4: Run test to verify it passes**

Run: `cd src/frontend && npm run test -- src/__tests__/contracts/ContractStudioPage.test.tsx`
Expected: PASS (all, including the original 4).

- [ ] **Step 5: Commit**

```bash
git add src/frontend/src/features/contracts/pages/ContractStudioPage.tsx src/frontend/src/__tests__/contracts/ContractStudioPage.test.tsx
git commit -m "feat(contracts): hub com 'Best for' + deep links para o create workspace"
```

---

## Task 7: i18n keys in 4 locales

**Files:**
- Modify: `src/frontend/src/locales/pt/translation.json`
- Modify: `src/frontend/src/locales/en/translation.json`
- Modify: `src/frontend/src/locales/es/translation.json`
- Modify: `src/frontend/src/locales/fr/translation.json`

- [ ] **Step 1: Locate the `contracts.create` block**

Run: `cd src/frontend && npx grep -n "\"create\"" src/locales/pt/translation.json` (or open and find `"contracts": { ... "create": {`).

- [ ] **Step 2: Add the new keys (pt example; translate per locale)**

Under `contracts.create`, add:

```json
"tabService": "Serviço",
"tabTypeMode": "Tipo & Modo",
"tabDetails": "Detalhes",
"tabConfirm": "Confirmar",
"draftNamePlaceholder": "novo-contrato",
"livePreviewHint": "Resumo atualiza ao vivo",
"cardVersion": "Versão",
"cardOperations": "Operações",
"cardValidation": "Validação",
"cardService": "Serviço",
"cardProtocol": "Protocolo",
"cardAuthor": "Autor",
"bestFor": {
  "RestApi": "Ideal para APIs HTTP request/response entre microsserviços",
  "Event": "Ideal para serviços orientados a eventos (Kafka, AMQP, SNS, WebSocket)",
  "Soap": "Ideal para serviços SOAP legados e integrações enterprise",
  "SharedSchema": "Tipos reutilizáveis referenciados por vários contratos"
}
```

English `bestFor` values (used by tests asserting `/Best for HTTP/i`):
```json
"RestApi": "Best for HTTP request/response APIs between microservices",
"Event": "Best for Kafka, AMQP, SNS, WebSocket event-driven services",
"Soap": "Best for legacy SOAP services and enterprise integrations",
"SharedSchema": "Reusable types referenced across multiple contracts"
```

Provide es/fr translations of the same keys.

- [ ] **Step 3: Validate JSON**

Run: `cd src/frontend && node -e "['pt','en','es','fr'].forEach(l=>JSON.parse(require('fs').readFileSync('src/locales/'+l+'/translation.json')))" && echo OK`
Expected: `OK` (no parse errors).

- [ ] **Step 4: Commit**

```bash
git add src/frontend/src/locales
git commit -m "i18n(contracts): chaves do create workspace v5 (4 locales)"
```

---

## Task 8: Full verification

- [ ] **Step 1: Typecheck + lint**

Run: `cd src/frontend && npm run lint`
Expected: 0 errors.

- [ ] **Step 2: Run the contracts + create test suites**

Run: `cd src/frontend && npm run test -- src/__tests__/contracts src/__tests__/pages/CreateContractPage.test.tsx`
Expected: all PASS.

- [ ] **Step 3: Run the full unit suite**

Run: `cd src/frontend && npm run test`
Expected: green (baseline 2323+ passing; no regressions).

- [ ] **Step 4: Build**

Run: `cd src/frontend && npm run build`
Expected: success.

- [ ] **Step 5: Manual smoke (optional but recommended)**

Run `npm run dev`, visit `/contracts/studio/new` → click a type card → confirm it opens `/contracts/new?type=…` with the type pre-seeded, the left card updates live as you type the title/select service, and "Criar draft" navigates to `/contracts/studio/{draftId}`.

- [ ] **Step 6: Confirm out-of-scope files untouched**

Run: `git diff --name-only main... | grep -E 'studio/|workspace/|BuilderPage' || echo "clean — no editor files touched"`
Expected: `clean — no editor files touched`.

- [ ] **Step 7: Final commit (if any verification fixups)**

```bash
git add -A && git commit -m "chore(contracts): verificacao final do create workspace v5"
```

---

## Self-review notes (for the implementer)

- **Behavior parity:** Task 2 must preserve the create mutation exactly — same `contractStudioApi` branch per type and the same `navigate('/contracts/studio/{draftId}')`. If a field/setter name differs from this plan, follow the actual hook export and adjust wiring in Tasks 4-5.
- **Type vocabulary:** the hub uses keys like `rest-openapi`; the wizard uses `RestApi`. `HUB_KEY_TO_CONTRACT_TYPE` (Task 1) is the only bridge — keep it the single source.
- **Tests depend on `data-testid="type-card-<key>"`** in the hub — do not rename.
- **No editor changes:** Task 8 Step 6 is the guard.
