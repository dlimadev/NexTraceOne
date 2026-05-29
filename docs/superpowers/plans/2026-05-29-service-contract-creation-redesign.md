# Service & Contract Creation Redesign — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace flat inline forms with full-screen wizard overlays for service registration, interface creation, and contract import — consistent Grafana-style UX with SVG icons per category.

**Architecture:** Shared `WizardOverlay` base + 3 specialised overlays (`ServiceRegistrationOverlay`, `ServiceInterfaceOverlay`, `ContractImportOverlay`). Parent pages control a boolean state; overlays are conditionally rendered and call `onSuccess`/`onClose`. No backend changes.

**Tech Stack:** React 19, TypeScript 5.9, Tailwind CSS 4, Lucide React, i18next, TanStack Query v5, Vitest + Testing Library

**Spec:** `docs/superpowers/specs/2026-05-29-service-contract-creation-redesign.md`

---

## Task 1: i18n Keys

**Files:**
- Modify: `src/frontend/src/locales/en.json`
- Modify: `src/frontend/src/locales/pt-BR.json`
- Modify: `src/frontend/src/locales/es.json`
- Modify: `src/frontend/src/locales/pt-PT.json`

- [ ] **Step 1: Add wizard keys to `en.json`**

Open `src/frontend/src/locales/en.json`. Find the top-level `"catalog"` key (around line 1265). Inside it, add a `"wizard"` sub-object and step sub-objects. The existing catalog object already has `"actions"`, `"badges"`, `"columns"`, etc. — append the new keys after the last existing key inside `"catalog"`:

```json
"wizard": {
  "close": "Close",
  "back": "Back",
  "next": "Next",
  "submit": "Submit",
  "submitting": "Saving…",
  "stepOf": "Step {{current}} of {{total}}"
},
"registration": {
  "step": {
    "identity": "Identity",
    "classification": "Classification",
    "ownership": "Ownership",
    "references": "References",
    "review": "Review"
  },
  "title": "Register Service",
  "subtitle": "Add a new service to the catalog"
},
"interface": {
  "step": {
    "type": "Type",
    "details": "Details",
    "review": "Review"
  },
  "title": "New Interface",
  "subtitle": "Add an interface to {{serviceName}}"
},
"contract": {
  "step": {
    "interface": "Interface",
    "spec": "Specification",
    "version": "Version"
  },
  "title": "Import Contract",
  "subtitle": "Import a specification for an interface",
  "protocolDetected": "{{protocol}} detected",
  "protocolUnknown": "Select manually",
  "dropZone": {
    "hint": "Drop openapi.yaml or click to browse",
    "formats": "YAML · JSON · WSDL · Proto"
  },
  "tab": {
    "upload": "Upload",
    "url": "URL",
    "editor": "Editor"
  }
}
```

- [ ] **Step 2: Add wizard keys to `pt-BR.json`**

Same location inside `"catalog"` key:

```json
"wizard": {
  "close": "Fechar",
  "back": "Voltar",
  "next": "Avançar",
  "submit": "Salvar",
  "submitting": "Salvando…",
  "stepOf": "Passo {{current}} de {{total}}"
},
"registration": {
  "step": {
    "identity": "Identidade",
    "classification": "Classificação",
    "ownership": "Responsáveis",
    "references": "Referências",
    "review": "Revisão"
  },
  "title": "Registrar Serviço",
  "subtitle": "Adicione um novo serviço ao catálogo"
},
"interface": {
  "step": {
    "type": "Tipo",
    "details": "Detalhes",
    "review": "Revisão"
  },
  "title": "Nova Interface",
  "subtitle": "Adicione uma interface a {{serviceName}}"
},
"contract": {
  "step": {
    "interface": "Interface",
    "spec": "Especificação",
    "version": "Versão"
  },
  "title": "Importar Contrato",
  "subtitle": "Importe uma especificação para uma interface",
  "protocolDetected": "{{protocol}} detectado",
  "protocolUnknown": "Selecione manualmente",
  "dropZone": {
    "hint": "Arraste openapi.yaml ou clique para navegar",
    "formats": "YAML · JSON · WSDL · Proto"
  },
  "tab": {
    "upload": "Upload",
    "url": "URL",
    "editor": "Editor"
  }
}
```

- [ ] **Step 3: Add wizard keys to `es.json`**

Same location inside `"catalog"` key:

```json
"wizard": {
  "close": "Cerrar",
  "back": "Volver",
  "next": "Siguiente",
  "submit": "Guardar",
  "submitting": "Guardando…",
  "stepOf": "Paso {{current}} de {{total}}"
},
"registration": {
  "step": {
    "identity": "Identidad",
    "classification": "Clasificación",
    "ownership": "Responsables",
    "references": "Referencias",
    "review": "Revisión"
  },
  "title": "Registrar Servicio",
  "subtitle": "Agregue un nuevo servicio al catálogo"
},
"interface": {
  "step": {
    "type": "Tipo",
    "details": "Detalles",
    "review": "Revisión"
  },
  "title": "Nueva Interfaz",
  "subtitle": "Agregar una interfaz a {{serviceName}}"
},
"contract": {
  "step": {
    "interface": "Interfaz",
    "spec": "Especificación",
    "version": "Versión"
  },
  "title": "Importar Contrato",
  "subtitle": "Importe una especificación para una interfaz",
  "protocolDetected": "{{protocol}} detectado",
  "protocolUnknown": "Seleccionar manualmente",
  "dropZone": {
    "hint": "Suelte openapi.yaml o haga clic para navegar",
    "formats": "YAML · JSON · WSDL · Proto"
  },
  "tab": {
    "upload": "Subir",
    "url": "URL",
    "editor": "Editor"
  }
}
```

- [ ] **Step 4: Add wizard keys to `pt-PT.json`**

Same location inside `"catalog"` key:

```json
"wizard": {
  "close": "Fechar",
  "back": "Voltar",
  "next": "Avançar",
  "submit": "Guardar",
  "submitting": "A guardar…",
  "stepOf": "Passo {{current}} de {{total}}"
},
"registration": {
  "step": {
    "identity": "Identidade",
    "classification": "Classificação",
    "ownership": "Responsáveis",
    "references": "Referências",
    "review": "Revisão"
  },
  "title": "Registar Serviço",
  "subtitle": "Adicione um novo serviço ao catálogo"
},
"interface": {
  "step": {
    "type": "Tipo",
    "details": "Detalhes",
    "review": "Revisão"
  },
  "title": "Nova Interface",
  "subtitle": "Adicione uma interface a {{serviceName}}"
},
"contract": {
  "step": {
    "interface": "Interface",
    "spec": "Especificação",
    "version": "Versão"
  },
  "title": "Importar Contrato",
  "subtitle": "Importe uma especificação para uma interface",
  "protocolDetected": "{{protocol}} detectado",
  "protocolUnknown": "Selecionar manualmente",
  "dropZone": {
    "hint": "Arraste openapi.yaml ou clique para navegar",
    "formats": "YAML · JSON · WSDL · Proto"
  },
  "tab": {
    "upload": "Upload",
    "url": "URL",
    "editor": "Editor"
  }
}
```

- [ ] **Step 5: Verify frontend compiles**

```bash
cd src/frontend && npm run build 2>&1 | tail -5
```

Expected: build succeeds (exit 0). i18n keys are just JSON — no type errors possible.

- [ ] **Step 6: Commit**

```bash
git add src/frontend/src/locales/
git commit -m "feat(i18n): add catalog wizard keys for overlay redesign"
```

---

## Task 2: WizardOverlay Base Component

**Files:**
- Create: `src/frontend/src/features/catalog/components/WizardOverlay.tsx`
- Create: `src/frontend/src/__tests__/catalog/WizardOverlay.test.tsx`

- [ ] **Step 1: Write the failing test**

Create `src/frontend/src/__tests__/catalog/WizardOverlay.test.tsx`:

```tsx
import { describe, it, expect, vi } from 'vitest';
import { render, screen, fireEvent } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { WizardOverlay } from '../../features/catalog/components/WizardOverlay';
import { Fingerprint, LayoutGrid } from 'lucide-react';

const STEPS = [
  { id: 'step1', labelKey: 'catalog.registration.step.identity', icon: Fingerprint },
  { id: 'step2', labelKey: 'catalog.registration.step.classification', icon: LayoutGrid },
];

function renderOverlay(overrides = {}) {
  const props = {
    title: 'Test Wizard',
    headerIcon: <Fingerprint size={20} />,
    steps: STEPS,
    currentStep: 1,
    onClose: vi.fn(),
    onBack: vi.fn(),
    onNext: vi.fn(),
    onSubmit: vi.fn(),
    isLastStep: false,
    children: <div>Step content</div>,
    ...overrides,
  };
  return { ...render(<WizardOverlay {...props} />), props };
}

describe('WizardOverlay', () => {
  it('renders title and children', () => {
    renderOverlay();
    expect(screen.getByText('Test Wizard')).toBeInTheDocument();
    expect(screen.getByText('Step content')).toBeInTheDocument();
  });

  it('Back button is disabled on step 1', () => {
    renderOverlay({ currentStep: 1 });
    const backBtn = screen.getByRole('button', { name: /back|voltar/i });
    expect(backBtn).toBeDisabled();
  });

  it('Back button is enabled on step 2', () => {
    renderOverlay({ currentStep: 2 });
    const backBtn = screen.getByRole('button', { name: /back|voltar/i });
    expect(backBtn).not.toBeDisabled();
    fireEvent.click(backBtn);
    // onBack called via props
  });

  it('clicking Next calls onNext', async () => {
    const user = userEvent.setup();
    const { props } = renderOverlay({ isLastStep: false });
    await user.click(screen.getByRole('button', { name: /next|avançar/i }));
    expect(props.onNext).toHaveBeenCalledOnce();
  });

  it('shows Submit on last step instead of Next', () => {
    renderOverlay({ isLastStep: true, currentStep: 2 });
    expect(screen.queryByRole('button', { name: /^next$|^avançar$/i })).not.toBeInTheDocument();
    expect(screen.getByRole('button', { name: /submit|salvar|guardar/i })).toBeInTheDocument();
  });

  it('Next button is disabled when isNextDisabled=true', () => {
    renderOverlay({ isNextDisabled: true });
    const nextBtn = screen.getByRole('button', { name: /next|avançar/i });
    expect(nextBtn).toBeDisabled();
  });

  it('pressing Escape calls onClose', () => {
    const { props } = renderOverlay();
    fireEvent.keyDown(document, { key: 'Escape' });
    expect(props.onClose).toHaveBeenCalledOnce();
  });

  it('clicking the X button calls onClose', async () => {
    const user = userEvent.setup();
    const { props } = renderOverlay();
    await user.click(screen.getByRole('button', { name: /close|fechar/i }));
    expect(props.onClose).toHaveBeenCalledOnce();
  });
});
```

- [ ] **Step 2: Run test — verify it fails**

```bash
cd src/frontend && npm run test -- --run src/__tests__/catalog/WizardOverlay.test.tsx 2>&1 | tail -20
```

Expected: FAIL — `Cannot find module '../../features/catalog/components/WizardOverlay'`

- [ ] **Step 3: Implement WizardOverlay**

Create `src/frontend/src/features/catalog/components/WizardOverlay.tsx`:

```tsx
import { useEffect } from 'react';
import { X, Check } from 'lucide-react';
import { useTranslation } from 'react-i18next';
import type { LucideIcon } from 'lucide-react';
import { Button } from '../../../components/Button';

export interface WizardStep {
  id: string;
  /** i18n key */
  labelKey: string;
  icon: LucideIcon;
}

export interface WizardOverlayProps {
  title: string;
  headerIcon: React.ReactNode;
  steps: WizardStep[];
  currentStep: number; // 1-based
  onClose: () => void;
  onBack: () => void;
  onNext: () => void;
  onSubmit: () => void;
  isSubmitting?: boolean;
  isNextDisabled?: boolean;
  isLastStep: boolean;
  children: React.ReactNode;
}

/**
 * Overlay full-screen reutilizável para wizards multi-step.
 * Layout: backdrop + painel centrado com stepper lateral 220px.
 */
export function WizardOverlay({
  title,
  headerIcon,
  steps,
  currentStep,
  onClose,
  onBack,
  onNext,
  onSubmit,
  isSubmitting = false,
  isNextDisabled = false,
  isLastStep,
  children,
}: WizardOverlayProps) {
  const { t } = useTranslation();

  // Fecha com Escape
  useEffect(() => {
    function handleKeyDown(e: KeyboardEvent) {
      if (e.key === 'Escape') onClose();
    }
    document.addEventListener('keydown', handleKeyDown);
    return () => document.removeEventListener('keydown', handleKeyDown);
  }, [onClose]);

  const stepLabel = steps[currentStep - 1]
    ? t(steps[currentStep - 1].labelKey, steps[currentStep - 1].id)
    : '';

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center">
      {/* Backdrop */}
      <div
        className="absolute inset-0 bg-black/60"
        onClick={onClose}
        aria-hidden="true"
      />

      {/* Panel */}
      <div className="relative z-10 w-full max-w-3xl mx-4 bg-panel border border-edge rounded-xl shadow-2xl flex flex-col max-h-[90vh] animate-fade-in">

        {/* Header */}
        <div className="flex items-center gap-3 px-6 py-4 border-b border-edge shrink-0">
          <div className="text-accent">{headerIcon}</div>
          <div className="flex-1 min-w-0">
            <h2 className="text-base font-semibold text-heading truncate">{title}</h2>
            <p className="text-xs text-muted">
              {t('catalog.wizard.stepOf', { current: currentStep, total: steps.length })} — {stepLabel}
            </p>
          </div>
          <button
            onClick={onClose}
            aria-label={t('catalog.wizard.close')}
            className="text-muted hover:text-heading transition-colors p-1 rounded"
          >
            <X size={18} />
          </button>
        </div>

        {/* Body */}
        <div className="flex flex-1 min-h-0">
          {/* Stepper sidebar */}
          <div className="w-[200px] shrink-0 border-r border-edge bg-canvas/50 px-4 py-5 flex flex-col gap-1">
            {steps.map((step, idx) => {
              const stepNum = idx + 1;
              const isDone = stepNum < currentStep;
              const isActive = stepNum === currentStep;
              const StepIcon = step.icon;

              return (
                <div
                  key={step.id}
                  className={`flex items-center gap-2.5 px-3 py-2 rounded-md text-xs transition-colors ${
                    isActive
                      ? 'bg-accent/15 text-accent font-semibold'
                      : isDone
                        ? 'text-success'
                        : 'text-muted'
                  }`}
                >
                  <div
                    className={`w-5 h-5 rounded-full flex items-center justify-center shrink-0 text-[10px] border transition-colors ${
                      isActive
                        ? 'bg-accent border-accent text-white'
                        : isDone
                          ? 'bg-success/20 border-success text-success'
                          : 'border-edge text-muted'
                    }`}
                  >
                    {isDone ? <Check size={10} /> : <StepIcon size={10} />}
                  </div>
                  <span className="truncate">{t(step.labelKey, step.id)}</span>
                </div>
              );
            })}
          </div>

          {/* Content area */}
          <div className="flex-1 overflow-y-auto px-6 py-5 min-w-0">
            {children}
          </div>
        </div>

        {/* Footer */}
        <div className="flex items-center justify-between px-6 py-4 border-t border-edge shrink-0">
          <Button
            variant="secondary"
            onClick={onBack}
            disabled={currentStep === 1}
          >
            {t('catalog.wizard.back')}
          </Button>

          {isLastStep ? (
            <Button onClick={onSubmit} loading={isSubmitting} disabled={isSubmitting}>
              {isSubmitting ? t('catalog.wizard.submitting') : t('catalog.wizard.submit')}
            </Button>
          ) : (
            <Button onClick={onNext} disabled={isNextDisabled}>
              {t('catalog.wizard.next')}
            </Button>
          )}
        </div>
      </div>
    </div>
  );
}
```

- [ ] **Step 4: Run test — verify it passes**

```bash
cd src/frontend && npm run test -- --run src/__tests__/catalog/WizardOverlay.test.tsx 2>&1 | tail -20
```

Expected: 8 tests pass.

- [ ] **Step 5: Commit**

```bash
git add src/frontend/src/features/catalog/components/WizardOverlay.tsx \
        src/frontend/src/__tests__/catalog/WizardOverlay.test.tsx
git commit -m "feat(catalog): WizardOverlay base component with stepper sidebar"
```

---

## Task 3: ServiceTypeIconPicker

**Files:**
- Create: `src/frontend/src/features/catalog/components/ServiceTypeIconPicker.tsx`
- Create: `src/frontend/src/__tests__/catalog/ServiceTypeIconPicker.test.tsx`

- [ ] **Step 1: Write the failing test**

Create `src/frontend/src/__tests__/catalog/ServiceTypeIconPicker.test.tsx`:

```tsx
import { describe, it, expect, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { ServiceTypeIconPicker } from '../../features/catalog/components/ServiceTypeIconPicker';

describe('ServiceTypeIconPicker — mode=service', () => {
  it('renders at least 10 type cards in service mode', () => {
    render(<ServiceTypeIconPicker value="RestApi" onChange={vi.fn()} mode="service" />);
    // Each card has role="option"
    const cards = screen.getAllByRole('option');
    expect(cards.length).toBeGreaterThanOrEqual(10);
  });

  it('selected type card has aria-selected=true', () => {
    render(<ServiceTypeIconPicker value="RestApi" onChange={vi.fn()} mode="service" />);
    const selected = screen.getAllByRole('option').find(
      (el) => el.getAttribute('aria-selected') === 'true'
    );
    expect(selected).toBeTruthy();
  });

  it('clicking a card calls onChange with that type', async () => {
    const user = userEvent.setup();
    const onChange = vi.fn();
    render(<ServiceTypeIconPicker value="RestApi" onChange={onChange} mode="service" />);
    const cards = screen.getAllByRole('option');
    // Click the second card
    await user.click(cards[1]);
    expect(onChange).toHaveBeenCalledOnce();
    expect(typeof onChange.mock.calls[0][0]).toBe('string');
  });
});

describe('ServiceTypeIconPicker — mode=interface', () => {
  it('renders fewer cards in interface mode than in service mode', () => {
    const { getAllByRole: getService } = render(
      <ServiceTypeIconPicker value="RestApi" onChange={vi.fn()} mode="service" />
    );
    const serviceCount = getService('option').length;

    const { getAllByRole: getIface } = render(
      <ServiceTypeIconPicker value="RestApi" onChange={vi.fn()} mode="interface" />
    );
    const ifaceCount = getIface('option').length;

    expect(ifaceCount).toBeLessThan(serviceCount);
  });
});
```

- [ ] **Step 2: Run test — verify it fails**

```bash
cd src/frontend && npm run test -- --run src/__tests__/catalog/ServiceTypeIconPicker.test.tsx 2>&1 | tail -10
```

Expected: FAIL — module not found.

- [ ] **Step 3: Implement ServiceTypeIconPicker**

Create `src/frontend/src/features/catalog/components/ServiceTypeIconPicker.tsx`:

```tsx
import { useTranslation } from 'react-i18next';

/** Todos os tipos disponíveis no modo 'service'. */
export const ALL_SERVICE_TYPES = [
  'RestApi', 'GraphqlApi', 'SoapService', 'ZosConnectApi',
  'KafkaProducer', 'KafkaConsumer', 'WebhookProducer', 'WebhookConsumer', 'MqQueue',
  'GrpcService',
  'BackgroundService', 'ScheduledProcess', 'ScheduledJob', 'BackgroundWorker',
  'Gateway',
  'IntegrationComponent', 'SharedPlatformService', 'Framework', 'ThirdParty', 'IntegrationBridge',
  'LegacySystem', 'CobolProgram', 'CicsTransaction', 'ImsTransaction', 'BatchJob', 'MainframeSystem', 'MqQueueManager',
] as const;

export type ServiceType = typeof ALL_SERVICE_TYPES[number];

/** Tipos disponíveis no modo 'interface' (subconjunto de ALL_SERVICE_TYPES). */
const INTERFACE_TYPES: ServiceType[] = [
  'RestApi', 'GraphqlApi', 'SoapService', 'ZosConnectApi',
  'KafkaProducer', 'KafkaConsumer', 'MqQueue',
  'GrpcService',
  'BackgroundWorker', 'ScheduledJob',
  'WebhookProducer', 'WebhookConsumer',
  'Gateway',
];

/** Cor semântica por tipo. */
const TYPE_STYLE: Record<ServiceType, { border: string; bg: string; text: string; stroke: string }> = {
  // API HTTP — índigo
  RestApi:           { border: 'border-indigo-500',  bg: 'bg-indigo-500/10',  text: 'text-indigo-400',  stroke: '#6366f1' },
  GraphqlApi:        { border: 'border-violet-500',  bg: 'bg-violet-500/10',  text: 'text-violet-400',  stroke: '#8b5cf6' },
  SoapService:       { border: 'border-indigo-400',  bg: 'bg-indigo-400/10',  text: 'text-indigo-300',  stroke: '#818cf8' },
  ZosConnectApi:     { border: 'border-indigo-300',  bg: 'bg-indigo-300/10',  text: 'text-indigo-300',  stroke: '#a5b4fc' },
  // Streaming / Eventos — âmbar
  KafkaProducer:     { border: 'border-amber-500',   bg: 'bg-amber-500/10',   text: 'text-amber-400',   stroke: '#f59e0b' },
  KafkaConsumer:     { border: 'border-amber-400',   bg: 'bg-amber-400/10',   text: 'text-amber-300',   stroke: '#fbbf24' },
  WebhookProducer:   { border: 'border-amber-500',   bg: 'bg-amber-500/10',   text: 'text-amber-400',   stroke: '#f59e0b' },
  WebhookConsumer:   { border: 'border-amber-400',   bg: 'bg-amber-400/10',   text: 'text-amber-300',   stroke: '#fbbf24' },
  MqQueue:           { border: 'border-amber-300',   bg: 'bg-amber-300/10',   text: 'text-amber-300',   stroke: '#fcd34d' },
  // RPC — ciano
  GrpcService:       { border: 'border-cyan-500',    bg: 'bg-cyan-500/10',    text: 'text-cyan-400',    stroke: '#06b6d4' },
  // Background — esmeralda
  BackgroundService: { border: 'border-emerald-500', bg: 'bg-emerald-500/10', text: 'text-emerald-400', stroke: '#10b981' },
  ScheduledProcess:  { border: 'border-emerald-400', bg: 'bg-emerald-400/10', text: 'text-emerald-300', stroke: '#34d399' },
  ScheduledJob:      { border: 'border-emerald-400', bg: 'bg-emerald-400/10', text: 'text-emerald-300', stroke: '#34d399' },
  BackgroundWorker:  { border: 'border-emerald-300', bg: 'bg-emerald-300/10', text: 'text-emerald-300', stroke: '#6ee7b7' },
  // Gateway — vermelho
  Gateway:           { border: 'border-red-500',     bg: 'bg-red-500/10',     text: 'text-red-400',     stroke: '#ef4444' },
  // Platform — slate azul
  IntegrationComponent:   { border: 'border-slate-400', bg: 'bg-slate-400/10', text: 'text-slate-300', stroke: '#94a3b8' },
  SharedPlatformService:  { border: 'border-slate-400', bg: 'bg-slate-400/10', text: 'text-slate-300', stroke: '#94a3b8' },
  Framework:              { border: 'border-slate-400', bg: 'bg-slate-400/10', text: 'text-slate-300', stroke: '#94a3b8' },
  ThirdParty:             { border: 'border-slate-400', bg: 'bg-slate-400/10', text: 'text-slate-300', stroke: '#94a3b8' },
  IntegrationBridge:      { border: 'border-slate-400', bg: 'bg-slate-400/10', text: 'text-slate-300', stroke: '#94a3b8' },
  // Legacy / Mainframe — cinza slate
  LegacySystem:      { border: 'border-slate-500', bg: 'bg-slate-500/10', text: 'text-slate-400', stroke: '#64748b' },
  CobolProgram:      { border: 'border-slate-500', bg: 'bg-slate-500/10', text: 'text-slate-400', stroke: '#64748b' },
  CicsTransaction:   { border: 'border-slate-500', bg: 'bg-slate-500/10', text: 'text-slate-400', stroke: '#64748b' },
  ImsTransaction:    { border: 'border-slate-500', bg: 'bg-slate-500/10', text: 'text-slate-400', stroke: '#64748b' },
  BatchJob:          { border: 'border-slate-500', bg: 'bg-slate-500/10', text: 'text-slate-400', stroke: '#64748b' },
  MainframeSystem:   { border: 'border-slate-500', bg: 'bg-slate-500/10', text: 'text-slate-400', stroke: '#64748b' },
  MqQueueManager:    { border: 'border-slate-500', bg: 'bg-slate-500/10', text: 'text-slate-400', stroke: '#64748b' },
};

/** Ícone SVG inline 22×22 para cada tipo de serviço. */
function ServiceTypeSvg({ type, stroke }: { type: ServiceType; stroke: string }) {
  switch (type) {
    case 'RestApi':
      return (
        <svg width="22" height="22" viewBox="0 0 22 22" fill="none">
          <rect x="2" y="6" width="18" height="10" rx="2" stroke={stroke} strokeWidth="1.5"/>
          <path d="M6 11h4M14 9l2 2-2 2" stroke={stroke} strokeWidth="1.5" strokeLinecap="round"/>
        </svg>
      );
    case 'GraphqlApi':
      return (
        <svg width="22" height="22" viewBox="0 0 22 22" fill="none">
          <circle cx="11" cy="11" r="2" stroke={stroke} strokeWidth="1.5"/>
          <circle cx="4" cy="7" r="1.5" stroke={stroke} strokeWidth="1.2"/>
          <circle cx="18" cy="7" r="1.5" stroke={stroke} strokeWidth="1.2"/>
          <circle cx="4" cy="15" r="1.5" stroke={stroke} strokeWidth="1.2"/>
          <circle cx="18" cy="15" r="1.5" stroke={stroke} strokeWidth="1.2"/>
          <path d="M5.3 7.8L9.5 9.5M12.5 9.5l4.2-1.7M5.3 14.2l4.2-1.7M12.5 12.5l4.2 1.7" stroke={stroke} strokeWidth="1.2"/>
        </svg>
      );
    case 'GrpcService':
      return (
        <svg width="22" height="22" viewBox="0 0 22 22" fill="none">
          <rect x="3" y="8" width="6" height="6" rx="1" stroke={stroke} strokeWidth="1.5"/>
          <rect x="13" y="8" width="6" height="6" rx="1" stroke={stroke} strokeWidth="1.5"/>
          <path d="M9 10l4 0M9 12l4 0" stroke={stroke} strokeWidth="1.5" strokeLinecap="round"/>
        </svg>
      );
    case 'KafkaProducer':
    case 'KafkaConsumer':
      return (
        <svg width="22" height="22" viewBox="0 0 22 22" fill="none">
          <circle cx="11" cy="11" r="3" stroke={stroke} strokeWidth="1.5"/>
          <path d="M4 8v-1a2 2 0 012-2h2M18 8v-1a2 2 0 00-2-2h-2M4 14v1a2 2 0 002 2h2M18 14v1a2 2 0 01-2 2h-2" stroke={stroke} strokeWidth="1.2" strokeLinecap="round"/>
        </svg>
      );
    case 'WebhookProducer':
    case 'WebhookConsumer':
      return (
        <svg width="22" height="22" viewBox="0 0 22 22" fill="none">
          <path d="M8 11a3 3 0 106 0" stroke={stroke} strokeWidth="1.5"/>
          <path d="M11 8V5M8 14H5a2 2 0 01-2-2V9M14 14h3a2 2 0 002-2V9" stroke={stroke} strokeWidth="1.5" strokeLinecap="round"/>
        </svg>
      );
    case 'Gateway':
      return (
        <svg width="22" height="22" viewBox="0 0 22 22" fill="none">
          <path d="M3 11h16M11 5v12" stroke={stroke} strokeWidth="1.5" strokeLinecap="round"/>
          <circle cx="11" cy="11" r="4" stroke={stroke} strokeWidth="1.5"/>
        </svg>
      );
    case 'BackgroundService':
    case 'BackgroundWorker':
      return (
        <svg width="22" height="22" viewBox="0 0 22 22" fill="none">
          <rect x="3" y="3" width="16" height="16" rx="3" stroke={stroke} strokeWidth="1.5"/>
          <path d="M7 11l3 3 5-5" stroke={stroke} strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round"/>
        </svg>
      );
    case 'ScheduledProcess':
    case 'ScheduledJob':
      return (
        <svg width="22" height="22" viewBox="0 0 22 22" fill="none">
          <circle cx="11" cy="11" r="7" stroke={stroke} strokeWidth="1.5"/>
          <path d="M11 7v4l2.5 2.5" stroke={stroke} strokeWidth="1.5" strokeLinecap="round"/>
        </svg>
      );
    case 'SoapService':
    case 'ZosConnectApi':
      return (
        <svg width="22" height="22" viewBox="0 0 22 22" fill="none">
          <rect x="2" y="5" width="18" height="12" rx="2" stroke={stroke} strokeWidth="1.5"/>
          <path d="M7 9h4M7 13h8" stroke={stroke} strokeWidth="1.5" strokeLinecap="round"/>
        </svg>
      );
    case 'MqQueue':
    case 'MqQueueManager':
      return (
        <svg width="22" height="22" viewBox="0 0 22 22" fill="none">
          <rect x="3" y="5" width="16" height="4" rx="1" stroke={stroke} strokeWidth="1.3"/>
          <rect x="3" y="9" width="16" height="4" rx="1" stroke={stroke} strokeWidth="1.3"/>
          <rect x="3" y="13" width="16" height="4" rx="1" stroke={stroke} strokeWidth="1.3"/>
        </svg>
      );
    case 'LegacySystem':
    case 'CobolProgram':
    case 'CicsTransaction':
    case 'ImsTransaction':
    case 'BatchJob':
    case 'MainframeSystem':
      return (
        <svg width="22" height="22" viewBox="0 0 22 22" fill="none">
          <rect x="3" y="3" width="16" height="16" rx="1" stroke={stroke} strokeWidth="1.5"/>
          <path d="M7 7h8M7 11h8M7 15h4" stroke={stroke} strokeWidth="1.3" strokeLinecap="round"/>
        </svg>
      );
    default:
      return (
        <svg width="22" height="22" viewBox="0 0 22 22" fill="none">
          <rect x="3" y="3" width="16" height="16" rx="3" stroke={stroke} strokeWidth="1.5"/>
          <path d="M11 7v8M7 11h8" stroke={stroke} strokeWidth="1.5" strokeLinecap="round"/>
        </svg>
      );
  }
}

/** Rótulo i18n para um tipo de serviço (fallback para o nome bruto). */
const TYPE_LABEL_KEYS: Record<ServiceType, string> = {
  RestApi: 'serviceCatalog.typeRestApi',
  GraphqlApi: 'serviceCatalog.typeGraphqlApi',
  SoapService: 'serviceCatalog.typeSoapService',
  ZosConnectApi: 'serviceCatalog.typeZosConnectApi',
  KafkaProducer: 'serviceCatalog.typeKafkaProducer',
  KafkaConsumer: 'serviceCatalog.typeKafkaConsumer',
  WebhookProducer: 'serviceCatalog.typeWebhookProducer',
  WebhookConsumer: 'serviceCatalog.typeWebhookConsumer',
  MqQueue: 'serviceCatalog.typeMqQueue',
  GrpcService: 'serviceCatalog.typeGrpcService',
  BackgroundService: 'serviceCatalog.typeBackgroundService',
  ScheduledProcess: 'serviceCatalog.typeScheduledProcess',
  ScheduledJob: 'serviceCatalog.typeScheduledJob',
  BackgroundWorker: 'serviceCatalog.typeBackgroundWorker',
  Gateway: 'serviceCatalog.typeGateway',
  IntegrationComponent: 'serviceCatalog.typeIntegrationComponent',
  SharedPlatformService: 'serviceCatalog.typeSharedPlatformService',
  Framework: 'serviceCatalog.typeFramework',
  ThirdParty: 'serviceCatalog.typeThirdParty',
  IntegrationBridge: 'serviceCatalog.typeIntegrationBridge',
  LegacySystem: 'serviceCatalog.typeLegacySystem',
  CobolProgram: 'serviceCatalog.typeCobolProgram',
  CicsTransaction: 'serviceCatalog.typeCicsTransaction',
  ImsTransaction: 'serviceCatalog.typeImsTransaction',
  BatchJob: 'serviceCatalog.typeBatchJob',
  MainframeSystem: 'serviceCatalog.typeMainframeSystem',
  MqQueueManager: 'serviceCatalog.typeMqQueueManager',
};

interface ServiceTypeIconPickerProps {
  value: string;
  onChange: (type: string) => void;
  /** 'service' mostra todos os tipos; 'interface' mostra apenas tipos de interface */
  mode: 'service' | 'interface';
}

/** Grid de cards selecionáveis com ícone SVG e cor semântica por categoria. */
export function ServiceTypeIconPicker({ value, onChange, mode }: ServiceTypeIconPickerProps) {
  const { t } = useTranslation();
  const types = mode === 'interface' ? INTERFACE_TYPES : [...ALL_SERVICE_TYPES];

  return (
    <div
      role="listbox"
      aria-label={t('serviceCatalog.serviceType', 'Service Type')}
      className="grid gap-2"
      style={{ gridTemplateColumns: 'repeat(auto-fill, minmax(90px, 1fr))' }}
    >
      {types.map((type) => {
        const style = TYPE_STYLE[type];
        const isSelected = value === type;
        return (
          <button
            key={type}
            type="button"
            role="option"
            aria-selected={isSelected}
            onClick={() => onChange(type)}
            className={`flex flex-col items-center gap-1.5 px-2 py-3 rounded-lg border transition-all text-center ${
              isSelected
                ? `${style.border} ${style.bg} ring-1 ring-inset ${style.border}`
                : 'border-edge hover:border-muted bg-canvas/50 hover:bg-canvas'
            }`}
          >
            <ServiceTypeSvg type={type} stroke={isSelected ? style.stroke : '#64748b'} />
            <span
              className={`text-[10px] leading-tight font-medium truncate w-full text-center ${
                isSelected ? style.text : 'text-muted'
              }`}
            >
              {t(TYPE_LABEL_KEYS[type], type)}
            </span>
          </button>
        );
      })}
    </div>
  );
}
```

- [ ] **Step 4: Run test — verify it passes**

```bash
cd src/frontend && npm run test -- --run src/__tests__/catalog/ServiceTypeIconPicker.test.tsx 2>&1 | tail -10
```

Expected: 4 tests pass.

- [ ] **Step 5: Commit**

```bash
git add src/frontend/src/features/catalog/components/ServiceTypeIconPicker.tsx \
        src/frontend/src/__tests__/catalog/ServiceTypeIconPicker.test.tsx
git commit -m "feat(catalog): ServiceTypeIconPicker with SVG icons and semantic colors"
```

---

## Task 4: ServiceRegistrationOverlay

**Files:**
- Create: `src/frontend/src/features/catalog/components/ServiceRegistrationOverlay.tsx`
- Create: `src/frontend/src/__tests__/catalog/ServiceRegistrationOverlay.test.tsx`

- [ ] **Step 1: Write the failing test**

Create `src/frontend/src/__tests__/catalog/ServiceRegistrationOverlay.test.tsx`:

```tsx
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { ServiceRegistrationOverlay } from '../../features/catalog/components/ServiceRegistrationOverlay';

vi.mock('../../features/catalog/api', () => ({
  serviceCatalogApi: {
    registerService: vi.fn().mockResolvedValue({ id: 'new-service-id' }),
  },
}));

import { serviceCatalogApi } from '../../features/catalog/api';

function renderOverlay(onClose = vi.fn(), onSuccess = vi.fn()) {
  const qc = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={qc}>
      <ServiceRegistrationOverlay onClose={onClose} onSuccess={onSuccess} />
    </QueryClientProvider>
  );
}

describe('ServiceRegistrationOverlay', () => {
  beforeEach(() => {
    vi.mocked(serviceCatalogApi.registerService).mockResolvedValue({ id: 'new-service-id' });
  });

  it('renders step 1 identity fields on mount', () => {
    renderOverlay();
    expect(screen.getByPlaceholderText(/payment-service/i)).toBeInTheDocument();
  });

  it('blocks advance when name is empty', async () => {
    const user = userEvent.setup();
    renderOverlay();
    await user.click(screen.getByRole('button', { name: /next|avançar/i }));
    expect(screen.getByText(/name.*required|nome.*obrigatório/i)).toBeInTheDocument();
  });

  it('blocks advance when domain is empty', async () => {
    const user = userEvent.setup();
    renderOverlay();
    await user.type(screen.getByPlaceholderText(/payment-service/i), 'my-svc');
    await user.click(screen.getByRole('button', { name: /next|avançar/i }));
    expect(screen.getByText(/domain.*required|domínio.*obrigatório/i)).toBeInTheDocument();
  });

  it('advances to step 2 after valid step 1', async () => {
    const user = userEvent.setup();
    renderOverlay();
    await user.type(screen.getByPlaceholderText(/payment-service/i), 'svc');
    // domain placeholder
    await user.type(screen.getByPlaceholderText(/payments.*identity|pagamentos/i), 'finance');
    await user.click(screen.getByRole('button', { name: /next|avançar/i }));
    // Step 2 shows service type picker
    await waitFor(() => {
      expect(screen.getAllByRole('option').length).toBeGreaterThan(5);
    });
  });

  it('calls registerService and onSuccess when submitted', async () => {
    const user = userEvent.setup();
    const onSuccess = vi.fn();
    renderOverlay(vi.fn(), onSuccess);

    // Step 1
    await user.type(screen.getByPlaceholderText(/payment-service/i), 'my-svc');
    await user.type(screen.getByPlaceholderText(/payments.*identity|pagamentos/i), 'finance');
    await user.click(screen.getByRole('button', { name: /next|avançar/i }));
    // Step 2 — advance
    await user.click(screen.getByRole('button', { name: /next|avançar/i }));
    // Step 3 — fill team
    await user.type(screen.getByPlaceholderText(/platform-team/i), 'finance-team');
    await user.click(screen.getByRole('button', { name: /next|avançar/i }));
    // Step 4 — advance
    await user.click(screen.getByRole('button', { name: /next|avançar/i }));
    // Step 5 — submit
    await user.click(screen.getByRole('button', { name: /submit|salvar|guardar/i }));

    await waitFor(() => {
      expect(serviceCatalogApi.registerService).toHaveBeenCalledWith(
        expect.objectContaining({ name: 'my-svc', domain: 'finance', team: 'finance-team' })
      );
      expect(onSuccess).toHaveBeenCalledWith('new-service-id');
    });
  });
});
```

- [ ] **Step 2: Run test — verify it fails**

```bash
cd src/frontend && npm run test -- --run src/__tests__/catalog/ServiceRegistrationOverlay.test.tsx 2>&1 | tail -10
```

Expected: FAIL — module not found.

- [ ] **Step 3: Implement ServiceRegistrationOverlay**

Create `src/frontend/src/features/catalog/components/ServiceRegistrationOverlay.tsx`:

```tsx
import { useState } from 'react';
import { useMutation } from '@tanstack/react-query';
import { useTranslation } from 'react-i18next';
import { toast } from 'sonner';
import {
  Fingerprint, LayoutGrid, Users, Link as LinkIcon, ClipboardCheck,
} from 'lucide-react';
import { WizardOverlay } from './WizardOverlay';
import { ServiceTypeIconPicker } from './ServiceTypeIconPicker';
import { serviceCatalogApi } from '../api';

const inputClass =
  'w-full rounded-md bg-canvas border border-edge px-3 py-2 text-sm text-heading placeholder:text-muted focus:outline-none focus:ring-2 focus:ring-accent focus:border-accent transition-colors';
const selectClass = inputClass;
const labelClass = 'block text-sm font-medium text-body mb-1';

interface ServiceRegistrationOverlayProps {
  onClose: () => void;
  onSuccess: (serviceId: string) => void;
}

interface ServiceFormData {
  name: string;
  domain: string;
  subDomain: string;
  capability: string;
  serviceType: string;
  criticality: string;
  exposureType: string;
  dataClassification: string;
  regulatoryScope: string;
  infrastructureProvider: string;
  runtimeLanguage: string;
  team: string;
  technicalOwner: string;
  businessOwner: string;
  productOwner: string;
  contactChannel: string;
  description: string;
  documentationUrl: string;
  repositoryUrl: string;
}

const INITIAL_FORM: ServiceFormData = {
  name: '', domain: '', subDomain: '', capability: '',
  serviceType: 'RestApi', criticality: 'Medium', exposureType: 'Internal',
  dataClassification: '', regulatoryScope: '', infrastructureProvider: '', runtimeLanguage: '',
  team: '', technicalOwner: '', businessOwner: '', productOwner: '', contactChannel: '',
  description: '', documentationUrl: '', repositoryUrl: '',
};

const STEPS = [
  { id: 'identity',       labelKey: 'catalog.registration.step.identity',       icon: Fingerprint },
  { id: 'classification', labelKey: 'catalog.registration.step.classification', icon: LayoutGrid },
  { id: 'ownership',      labelKey: 'catalog.registration.step.ownership',      icon: Users },
  { id: 'references',     labelKey: 'catalog.registration.step.references',     icon: LinkIcon },
  { id: 'review',         labelKey: 'catalog.registration.step.review',         icon: ClipboardCheck },
];

/** Overlay de 5 passos para registrar um novo serviço no catálogo. */
export function ServiceRegistrationOverlay({ onClose, onSuccess }: ServiceRegistrationOverlayProps) {
  const { t } = useTranslation();
  const [step, setStep] = useState(1);
  const [form, setForm] = useState<ServiceFormData>(INITIAL_FORM);
  const [errors, setErrors] = useState<Partial<Record<keyof ServiceFormData, string>>>({});

  const set = (key: keyof ServiceFormData, value: string) =>
    setForm((f) => ({ ...f, [key]: value }));
  const clearError = (key: keyof ServiceFormData) =>
    setErrors((e) => { const n = { ...e }; delete n[key]; return n; });

  const mutation = useMutation({
    mutationFn: () => serviceCatalogApi.registerService({
      name: form.name, domain: form.domain, team: form.team,
      description: form.description || undefined,
      serviceType: form.serviceType || undefined,
      criticality: form.criticality || undefined,
      exposureType: form.exposureType || undefined,
      technicalOwner: form.technicalOwner || undefined,
      businessOwner: form.businessOwner || undefined,
      documentationUrl: form.documentationUrl || undefined,
      repositoryUrl: form.repositoryUrl || undefined,
    }),
    onSuccess: (data) => {
      onSuccess(data.id);
    },
    onError: () => {
      toast.error(t('common.errorSaving'));
    },
  });

  function validate(): boolean {
    const errs: typeof errors = {};
    if (step === 1) {
      if (!form.name.trim()) errs.name = t('serviceCatalog.name') + ' ' + t('common.isRequired', 'is required');
      if (!form.domain.trim()) errs.domain = t('serviceCatalog.domain', 'Domain') + ' ' + t('common.isRequired', 'is required');
    }
    if (step === 3) {
      if (!form.team.trim()) errs.team = t('serviceCatalog.team') + ' ' + t('common.isRequired', 'is required');
    }
    setErrors(errs);
    return Object.keys(errs).length === 0;
  }

  function handleNext() {
    if (!validate()) return;
    setStep((s) => s + 1);
  }

  function handleBack() {
    setStep((s) => Math.max(1, s - 1));
    setErrors({});
  }

  function handleSubmit() {
    mutation.mutate();
  }

  const isNextDisabled = step === 1
    ? !form.name.trim() || !form.domain.trim()
    : step === 3
      ? !form.team.trim()
      : false;

  function renderStep() {
    switch (step) {
      case 1:
        return (
          <div className="space-y-4">
            <div>
              <label className={labelClass}>{t('serviceCatalog.name')} <span className="text-danger">*</span></label>
              <input
                type="text"
                className={inputClass}
                value={form.name}
                onChange={(e) => { set('name', e.target.value); clearError('name'); }}
                placeholder="e.g., payment-service"
              />
              {errors.name && <p className="mt-1 text-xs text-danger">{errors.name}</p>}
            </div>
            <div>
              <label className={labelClass}>{t('serviceCatalog.domain', 'Domain')} <span className="text-danger">*</span></label>
              <input
                type="text"
                className={inputClass}
                value={form.domain}
                onChange={(e) => { set('domain', e.target.value); clearError('domain'); }}
                placeholder="e.g., payments, identity, orders"
              />
              {errors.domain && <p className="mt-1 text-xs text-danger">{errors.domain}</p>}
            </div>
            <div>
              <label className={labelClass}>{t('serviceCatalog.subDomain', 'Sub-Domain')}</label>
              <input type="text" className={inputClass} value={form.subDomain}
                onChange={(e) => set('subDomain', e.target.value)} placeholder="e.g., billing" />
            </div>
            <div>
              <label className={labelClass}>{t('serviceCatalog.capability', 'Capability')}</label>
              <input type="text" className={inputClass} value={form.capability}
                onChange={(e) => set('capability', e.target.value)} placeholder="e.g., payment-processing" />
            </div>
          </div>
        );

      case 2:
        return (
          <div className="space-y-4">
            <div>
              <label className={labelClass}>{t('serviceCatalog.serviceType', 'Service Type')}</label>
              <ServiceTypeIconPicker value={form.serviceType} onChange={(v) => set('serviceType', v)} mode="service" />
            </div>
            <div className="grid grid-cols-2 gap-4 mt-4">
              <div>
                <label className={labelClass}>{t('serviceCatalog.criticality', 'Criticality')}</label>
                <select className={selectClass} value={form.criticality} onChange={(e) => set('criticality', e.target.value)}>
                  <option value="Critical">{t('catalog.badges.criticality.Critical')}</option>
                  <option value="High">{t('catalog.badges.criticality.High')}</option>
                  <option value="Medium">{t('catalog.badges.criticality.Medium')}</option>
                  <option value="Low">{t('catalog.badges.criticality.Low')}</option>
                </select>
              </div>
              <div>
                <label className={labelClass}>{t('serviceCatalog.exposureType', 'Exposure')}</label>
                <select className={selectClass} value={form.exposureType} onChange={(e) => set('exposureType', e.target.value)}>
                  <option value="Internal">{t('catalog.badges.exposure.Internal')}</option>
                  <option value="External">{t('catalog.badges.exposure.External')}</option>
                  <option value="Partner">{t('catalog.badges.exposure.Partner')}</option>
                </select>
              </div>
              <div>
                <label className={labelClass}>{t('serviceCatalog.dataClassification', 'Data Classification')}</label>
                <input type="text" className={inputClass} value={form.dataClassification}
                  onChange={(e) => set('dataClassification', e.target.value)} />
              </div>
              <div>
                <label className={labelClass}>{t('serviceCatalog.infrastructureProvider', 'Infrastructure')}</label>
                <input type="text" className={inputClass} value={form.infrastructureProvider}
                  onChange={(e) => set('infrastructureProvider', e.target.value)} />
              </div>
            </div>
          </div>
        );

      case 3:
        return (
          <div className="space-y-4">
            <div>
              <label className={labelClass}>{t('serviceCatalog.team')} <span className="text-danger">*</span></label>
              <input
                type="text"
                className={inputClass}
                value={form.team}
                onChange={(e) => { set('team', e.target.value); clearError('team'); }}
                placeholder="e.g., platform-team"
              />
              {errors.team && <p className="mt-1 text-xs text-danger">{errors.team}</p>}
            </div>
            <div className="grid grid-cols-2 gap-4">
              <div>
                <label className={labelClass}>{t('serviceCatalog.technicalOwner', 'Technical Owner')}</label>
                <input type="text" className={inputClass} value={form.technicalOwner}
                  onChange={(e) => set('technicalOwner', e.target.value)} />
              </div>
              <div>
                <label className={labelClass}>{t('serviceCatalog.businessOwner', 'Business Owner')}</label>
                <input type="text" className={inputClass} value={form.businessOwner}
                  onChange={(e) => set('businessOwner', e.target.value)} />
              </div>
              <div>
                <label className={labelClass}>{t('serviceCatalog.productOwner', 'Product Owner')}</label>
                <input type="text" className={inputClass} value={form.productOwner}
                  onChange={(e) => set('productOwner', e.target.value)} />
              </div>
              <div>
                <label className={labelClass}>{t('serviceCatalog.contactChannel', 'Contact Channel')}</label>
                <input type="text" className={inputClass} value={form.contactChannel}
                  onChange={(e) => set('contactChannel', e.target.value)} placeholder="#slack-channel" />
              </div>
            </div>
          </div>
        );

      case 4:
        return (
          <div className="space-y-4">
            <div>
              <label className={labelClass}>{t('serviceCatalog.description', 'Description')}</label>
              <textarea
                className={`${inputClass} resize-none`}
                rows={3}
                value={form.description}
                onChange={(e) => set('description', e.target.value)}
              />
            </div>
            <div>
              <label className={labelClass}>{t('serviceCatalog.documentationUrl', 'Documentation URL')}</label>
              <input type="url" className={inputClass} value={form.documentationUrl}
                onChange={(e) => set('documentationUrl', e.target.value)} placeholder="https://docs.example.com" />
            </div>
            <div>
              <label className={labelClass}>{t('serviceCatalog.repositoryUrl', 'Repository URL')}</label>
              <input type="url" className={inputClass} value={form.repositoryUrl}
                onChange={(e) => set('repositoryUrl', e.target.value)} placeholder="https://github.com/org/repo" />
            </div>
          </div>
        );

      case 5:
        return (
          <div className="space-y-3">
            <h3 className="text-sm font-semibold text-heading mb-3">{t('catalog.registration.step.review')}</h3>
            {([
              ['serviceCatalog.name', form.name],
              ['serviceCatalog.domain', form.domain],
              ['serviceCatalog.team', form.team],
              ['serviceCatalog.serviceType', form.serviceType],
              ['serviceCatalog.criticality', form.criticality],
              ['serviceCatalog.exposureType', form.exposureType],
              ['serviceCatalog.technicalOwner', form.technicalOwner],
            ] as [string, string][]).map(([key, value]) =>
              value ? (
                <div key={key} className="flex justify-between text-sm">
                  <span className="text-muted">{t(key, key)}</span>
                  <span className="font-medium text-heading">{value}</span>
                </div>
              ) : null
            )}
          </div>
        );

      default:
        return null;
    }
  }

  return (
    <WizardOverlay
      title={t('catalog.registration.title')}
      headerIcon={<Fingerprint size={20} />}
      steps={STEPS}
      currentStep={step}
      onClose={onClose}
      onBack={handleBack}
      onNext={handleNext}
      onSubmit={handleSubmit}
      isSubmitting={mutation.isPending}
      isNextDisabled={isNextDisabled}
      isLastStep={step === STEPS.length}
    >
      {renderStep()}
    </WizardOverlay>
  );
}
```

- [ ] **Step 4: Run test — verify it passes**

```bash
cd src/frontend && npm run test -- --run src/__tests__/catalog/ServiceRegistrationOverlay.test.tsx 2>&1 | tail -15
```

Expected: 5 tests pass.

- [ ] **Step 5: Commit**

```bash
git add src/frontend/src/features/catalog/components/ServiceRegistrationOverlay.tsx \
        src/frontend/src/__tests__/catalog/ServiceRegistrationOverlay.test.tsx
git commit -m "feat(catalog): ServiceRegistrationOverlay 5-step wizard"
```

---

## Task 5: ServiceInterfaceOverlay

**Files:**
- Create: `src/frontend/src/features/catalog/components/ServiceInterfaceOverlay.tsx`
- Create: `src/frontend/src/__tests__/catalog/ServiceInterfaceOverlay.test.tsx`

- [ ] **Step 1: Write the failing test**

Create `src/frontend/src/__tests__/catalog/ServiceInterfaceOverlay.test.tsx`:

```tsx
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { ServiceInterfaceOverlay } from '../../features/catalog/components/ServiceInterfaceOverlay';

vi.mock('../../features/catalog/api', () => ({
  serviceCatalogApi: {
    createServiceInterface: vi.fn().mockResolvedValue({ id: 'iface-1', name: 'test' }),
  },
}));

import { serviceCatalogApi } from '../../features/catalog/api';

function renderOverlay(onClose = vi.fn(), onSuccess = vi.fn()) {
  const qc = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={qc}>
      <ServiceInterfaceOverlay
        serviceId="svc-123"
        serviceName="Payment Service"
        onClose={onClose}
        onSuccess={onSuccess}
      />
    </QueryClientProvider>
  );
}

describe('ServiceInterfaceOverlay', () => {
  beforeEach(() => {
    vi.mocked(serviceCatalogApi.createServiceInterface).mockResolvedValue({ id: 'iface-1', name: 'test' } as never);
  });

  it('renders step 1 with type picker on mount', () => {
    renderOverlay();
    expect(screen.getAllByRole('option').length).toBeGreaterThan(3);
  });

  it('blocks advance when name is empty', async () => {
    const user = userEvent.setup();
    renderOverlay();
    await user.click(screen.getByRole('button', { name: /next|avançar/i }));
    expect(screen.getByText(/name.*required|nome.*obrigatório/i)).toBeInTheDocument();
  });

  it('selecting KafkaProducer on step 2 shows topic field', async () => {
    const user = userEvent.setup();
    renderOverlay();
    // Click KafkaProducer card
    const kafkaCard = screen.getAllByRole('option').find(
      (el) => el.textContent?.toLowerCase().includes('kafka')
    );
    expect(kafkaCard).toBeTruthy();
    await user.click(kafkaCard!);
    // Fill name
    const nameInput = screen.getByPlaceholderText(/interface name|nome da interface/i);
    await user.type(nameInput, 'my-topic');
    await user.click(screen.getByRole('button', { name: /next|avançar/i }));
    // Step 2 shows topic field
    expect(await screen.findByPlaceholderText(/topic|tópico/i)).toBeInTheDocument();
  });

  it('calls createServiceInterface and onSuccess on submit', async () => {
    const user = userEvent.setup();
    const onSuccess = vi.fn();
    renderOverlay(vi.fn(), onSuccess);
    // Fill name
    const nameInput = screen.getByPlaceholderText(/interface name|nome da interface/i);
    await user.type(nameInput, 'my-api');
    await user.click(screen.getByRole('button', { name: /next|avançar/i }));
    // Step 2 — advance
    await user.click(screen.getByRole('button', { name: /next|avançar/i }));
    // Step 3 — submit
    await user.click(screen.getByRole('button', { name: /submit|salvar|guardar/i }));
    await waitFor(() => {
      expect(serviceCatalogApi.createServiceInterface).toHaveBeenCalledWith(
        expect.objectContaining({ serviceAssetId: 'svc-123', name: 'my-api' })
      );
      expect(onSuccess).toHaveBeenCalledOnce();
    });
  });
});
```

- [ ] **Step 2: Run test — verify it fails**

```bash
cd src/frontend && npm run test -- --run src/__tests__/catalog/ServiceInterfaceOverlay.test.tsx 2>&1 | tail -10
```

Expected: FAIL — module not found.

- [ ] **Step 3: Implement ServiceInterfaceOverlay**

Create `src/frontend/src/features/catalog/components/ServiceInterfaceOverlay.tsx`:

```tsx
import { useState } from 'react';
import { useMutation } from '@tanstack/react-query';
import { useTranslation } from 'react-i18next';
import { toast } from 'sonner';
import { Plug, Settings2, ClipboardCheck } from 'lucide-react';
import { WizardOverlay } from './WizardOverlay';
import { ServiceTypeIconPicker } from './ServiceTypeIconPicker';
import { serviceCatalogApi } from '../api';
import type { InterfaceType } from '../../../types';

const inputClass =
  'w-full rounded-md bg-canvas border border-edge px-3 py-2 text-sm text-heading placeholder:text-muted focus:outline-none focus:ring-2 focus:ring-accent focus:border-accent transition-colors';
const selectClass = inputClass;
const labelClass = 'block text-sm font-medium text-body mb-1';

interface ServiceInterfaceOverlayProps {
  serviceId: string;
  serviceName: string;
  onClose: () => void;
  onSuccess: () => void;
}

interface InterfaceFormData {
  name: string;
  interfaceType: string;
  exposureScope: string;
  basePath: string;
  topicName: string;
  wsdlNamespace: string;
  grpcServiceName: string;
  scheduleCron: string;
  documentationUrl: string;
  requiresContract: boolean;
}

const INITIAL_FORM: InterfaceFormData = {
  name: '', interfaceType: 'RestApi', exposureScope: 'Internal',
  basePath: '', topicName: '', wsdlNamespace: '',
  grpcServiceName: '', scheduleCron: '', documentationUrl: '', requiresContract: false,
};

const STEPS = [
  { id: 'type',    labelKey: 'catalog.interface.step.type',    icon: Plug },
  { id: 'details', labelKey: 'catalog.interface.step.details', icon: Settings2 },
  { id: 'review',  labelKey: 'catalog.interface.step.review',  icon: ClipboardCheck },
];

const TYPES_WITH_BASE_PATH: string[] = ['RestApi', 'SoapService', 'ZosConnectApi', 'GraphqlApi'];
const TYPES_WITH_WSDL:      string[] = ['SoapService'];
const TYPES_WITH_TOPIC:     string[] = ['KafkaProducer', 'KafkaConsumer', 'MqQueue'];
const TYPES_WITH_GRPC:      string[] = ['GrpcService'];
const TYPES_WITH_CRON:      string[] = ['BackgroundWorker', 'ScheduledJob'];

/** Overlay de 3 passos para criar uma interface de serviço. */
export function ServiceInterfaceOverlay({ serviceId, serviceName, onClose, onSuccess }: ServiceInterfaceOverlayProps) {
  const { t } = useTranslation();
  const [step, setStep] = useState(1);
  const [form, setForm] = useState<InterfaceFormData>(INITIAL_FORM);
  const [errors, setErrors] = useState<Partial<Record<keyof InterfaceFormData, string>>>({});

  const set = (key: keyof InterfaceFormData, value: string | boolean) =>
    setForm((f) => ({ ...f, [key]: value }));

  const mutation = useMutation({
    mutationFn: () => serviceCatalogApi.createServiceInterface({
      serviceAssetId: serviceId,
      name: form.name,
      interfaceType: form.interfaceType as InterfaceType,
      exposureScope: form.exposureScope || undefined,
      basePath: form.basePath || undefined,
      topicName: form.topicName || undefined,
      wsdlNamespace: form.wsdlNamespace || undefined,
      grpcServiceName: form.grpcServiceName || undefined,
      scheduleCron: form.scheduleCron || undefined,
      documentationUrl: form.documentationUrl || undefined,
    }),
    onSuccess: () => onSuccess(),
    onError: () => toast.error(t('common.errorSaving')),
  });

  function validate(): boolean {
    const errs: typeof errors = {};
    if (step === 1 && !form.name.trim()) {
      errs.name = t('serviceCatalog.name') + ' ' + t('common.isRequired', 'is required');
    }
    setErrors(errs);
    return Object.keys(errs).length === 0;
  }

  function handleNext() {
    if (!validate()) return;
    setStep((s) => s + 1);
  }

  function renderStep() {
    switch (step) {
      case 1:
        return (
          <div className="space-y-4">
            <div>
              <label className={labelClass}>{t('catalog.interface.step.type')}</label>
              <ServiceTypeIconPicker value={form.interfaceType} onChange={(v) => set('interfaceType', v)} mode="interface" />
            </div>
            <div className="mt-4">
              <label className={labelClass}>{t('serviceCatalog.name')} <span className="text-danger">*</span></label>
              <input
                type="text"
                className={inputClass}
                value={form.name}
                onChange={(e) => { set('name', e.target.value); setErrors({}); }}
                placeholder={t('catalog.interface.namePlaceholder', 'Interface name, e.g., POST /payments')}
              />
              {errors.name && <p className="mt-1 text-xs text-danger">{errors.name}</p>}
            </div>
            <div>
              <label className={labelClass}>{t('catalog.interface.exposureScope', 'Exposure Scope')}</label>
              <select className={selectClass} value={form.exposureScope} onChange={(e) => set('exposureScope', e.target.value)}>
                <option value="Internal">{t('catalog.badges.exposure.Internal')}</option>
                <option value="External">{t('catalog.badges.exposure.External')}</option>
                <option value="Partner">{t('catalog.badges.exposure.Partner')}</option>
              </select>
            </div>
          </div>
        );

      case 2: {
        const ifType = form.interfaceType;
        return (
          <div className="space-y-4">
            {TYPES_WITH_BASE_PATH.includes(ifType) && (
              <div>
                <label className={labelClass}>{t('catalog.interface.basePath', 'Base Path')}</label>
                <input type="text" className={`${inputClass} font-mono`} value={form.basePath}
                  onChange={(e) => set('basePath', e.target.value)} placeholder="/api/v1" />
              </div>
            )}
            {TYPES_WITH_WSDL.includes(ifType) && (
              <div>
                <label className={labelClass}>{t('catalog.interface.wsdlNamespace', 'WSDL Namespace')}</label>
                <input type="text" className={`${inputClass} font-mono`} value={form.wsdlNamespace}
                  onChange={(e) => set('wsdlNamespace', e.target.value)} placeholder="http://example.com/service" />
              </div>
            )}
            {TYPES_WITH_TOPIC.includes(ifType) && (
              <div>
                <label className={labelClass}>{t('catalog.interface.topicName', 'Topic Name')}</label>
                <input type="text" className={`${inputClass} font-mono`} value={form.topicName}
                  onChange={(e) => set('topicName', e.target.value)} placeholder="payments.events.v1" />
              </div>
            )}
            {TYPES_WITH_GRPC.includes(ifType) && (
              <div>
                <label className={labelClass}>{t('catalog.interface.grpcServiceName', 'gRPC Service Name')}</label>
                <input type="text" className={`${inputClass} font-mono`} value={form.grpcServiceName}
                  onChange={(e) => set('grpcServiceName', e.target.value)} placeholder="PaymentService" />
              </div>
            )}
            {TYPES_WITH_CRON.includes(ifType) && (
              <div>
                <label className={labelClass}>{t('catalog.interface.scheduleCron', 'Schedule (cron)')}</label>
                <input type="text" className={`${inputClass} font-mono`} value={form.scheduleCron}
                  onChange={(e) => set('scheduleCron', e.target.value)} placeholder="0 */6 * * *" />
              </div>
            )}
            <div>
              <label className={labelClass}>{t('serviceCatalog.documentationUrl', 'Documentation URL')}</label>
              <input type="url" className={inputClass} value={form.documentationUrl}
                onChange={(e) => set('documentationUrl', e.target.value)} />
            </div>
            <div className="flex items-center gap-2">
              <input type="checkbox" id="requiresContract" checked={form.requiresContract}
                onChange={(e) => set('requiresContract', e.target.checked)}
                className="rounded border-edge text-accent" />
              <label htmlFor="requiresContract" className="text-sm text-body">
                {t('catalog.interface.requiresContract', 'Requires contract')}
              </label>
            </div>
          </div>
        );
      }

      case 3:
        return (
          <div className="space-y-3">
            <h3 className="text-sm font-semibold text-heading mb-3">{t('catalog.interface.step.review')}</h3>
            {([
              [t('serviceCatalog.name'), form.name],
              [t('catalog.interface.step.type'), form.interfaceType],
              [t('catalog.interface.exposureScope', 'Exposure Scope'), form.exposureScope],
              ...(form.basePath ? [[t('catalog.interface.basePath', 'Base Path'), form.basePath]] : []),
              ...(form.topicName ? [[t('catalog.interface.topicName', 'Topic'), form.topicName]] : []),
              ...(form.grpcServiceName ? [[t('catalog.interface.grpcServiceName', 'gRPC'), form.grpcServiceName]] : []),
            ] as [string, string][]).map(([label, value]) => (
              <div key={label} className="flex justify-between text-sm">
                <span className="text-muted">{label}</span>
                <span className="font-medium text-heading font-mono">{value}</span>
              </div>
            ))}
            <div className="text-xs text-muted mt-2">
              {t('catalog.interface.service', 'Service')}: <span className="text-heading">{serviceName}</span>
            </div>
          </div>
        );

      default:
        return null;
    }
  }

  return (
    <WizardOverlay
      title={t('catalog.interface.title')}
      headerIcon={<Plug size={20} />}
      steps={STEPS}
      currentStep={step}
      onClose={onClose}
      onBack={() => { setStep((s) => Math.max(1, s - 1)); setErrors({}); }}
      onNext={handleNext}
      onSubmit={() => mutation.mutate()}
      isSubmitting={mutation.isPending}
      isNextDisabled={step === 1 && !form.name.trim()}
      isLastStep={step === STEPS.length}
    >
      {renderStep()}
    </WizardOverlay>
  );
}
```

- [ ] **Step 4: Run test — verify it passes**

```bash
cd src/frontend && npm run test -- --run src/__tests__/catalog/ServiceInterfaceOverlay.test.tsx 2>&1 | tail -15
```

Expected: 4 tests pass.

- [ ] **Step 5: Commit**

```bash
git add src/frontend/src/features/catalog/components/ServiceInterfaceOverlay.tsx \
        src/frontend/src/__tests__/catalog/ServiceInterfaceOverlay.test.tsx
git commit -m "feat(catalog): ServiceInterfaceOverlay 3-step wizard with adaptive fields"
```

---

## Task 6: ContractImportOverlay

**Files:**
- Create: `src/frontend/src/features/catalog/components/ContractImportOverlay.tsx`
- Create: `src/frontend/src/__tests__/catalog/ContractImportOverlay.test.tsx`

- [ ] **Step 1: Write the failing test**

Create `src/frontend/src/__tests__/catalog/ContractImportOverlay.test.tsx`:

```tsx
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { ContractImportOverlay } from '../../features/catalog/components/ContractImportOverlay';

vi.mock('../../features/contracts/api/contracts', () => ({
  contractsApi: {
    importContract: vi.fn().mockResolvedValue({ id: 'contract-1' }),
  },
}));

import { contractsApi } from '../../features/contracts/api/contracts';

function renderOverlay(props = {}) {
  const qc = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={qc}>
      <ContractImportOverlay
        onClose={vi.fn()}
        onSuccess={vi.fn()}
        {...props}
      />
    </QueryClientProvider>
  );
}

describe('ContractImportOverlay', () => {
  beforeEach(() => {
    vi.mocked(contractsApi.importContract).mockResolvedValue({ id: 'contract-1' } as never);
  });

  it('renders step 1 with apiAssetId input when no preselectedApiAssetId', () => {
    renderOverlay();
    expect(screen.getByPlaceholderText(/asset id|uuid/i)).toBeInTheDocument();
  });

  it('when preselectedApiAssetId is given step 1 shows the pre-filled name', () => {
    renderOverlay({ preselectedApiAssetId: 'api-123', preselectedApiAssetName: 'Payment API' });
    expect(screen.getByText(/Payment API/i)).toBeInTheDocument();
  });

  it('detecting OpenAPI content sets protocol badge', async () => {
    const user = userEvent.setup();
    renderOverlay({ preselectedApiAssetId: 'api-123', preselectedApiAssetName: 'Payment API' });
    // advance step 1 (preselected)
    await user.click(screen.getByRole('button', { name: /next|avançar/i }));
    // switch to Editor tab
    await user.click(screen.getByRole('button', { name: /editor/i }));
    const textarea = screen.getByRole('textbox');
    await user.type(textarea, 'openapi: "3.0.0"');
    // badge should appear
    expect(await screen.findByText(/openapi/i)).toBeInTheDocument();
  });

  it('calls importContract and onSuccess on final submit', async () => {
    const user = userEvent.setup();
    const onSuccess = vi.fn();
    renderOverlay({ preselectedApiAssetId: 'api-123', preselectedApiAssetName: 'Payment API', onSuccess });
    // Step 1 — advance
    await user.click(screen.getByRole('button', { name: /next|avançar/i }));
    // Step 2 — switch to editor, type content
    await user.click(screen.getByRole('button', { name: /editor/i }));
    await user.type(screen.getByRole('textbox'), 'openapi: "3.0.0"');
    await user.click(screen.getByRole('button', { name: /next|avançar/i }));
    // Step 3 — fill version
    await user.type(screen.getByPlaceholderText(/1\.0\.0/), '1.0.0');
    await user.click(screen.getByRole('button', { name: /submit|salvar|guardar/i }));
    await waitFor(() => {
      expect(contractsApi.importContract).toHaveBeenCalledWith(
        expect.objectContaining({ apiAssetId: 'api-123', version: '1.0.0' })
      );
      expect(onSuccess).toHaveBeenCalledOnce();
    });
  });
});
```

- [ ] **Step 2: Run test — verify it fails**

```bash
cd src/frontend && npm run test -- --run src/__tests__/catalog/ContractImportOverlay.test.tsx 2>&1 | tail -10
```

Expected: FAIL — module not found.

- [ ] **Step 3: Implement ContractImportOverlay**

Create `src/frontend/src/features/catalog/components/ContractImportOverlay.tsx`:

```tsx
import { useState, useCallback } from 'react';
import { useMutation } from '@tanstack/react-query';
import { useTranslation } from 'react-i18next';
import { toast } from 'sonner';
import { Layers, FileCode, Tag } from 'lucide-react';
import { WizardOverlay } from './WizardOverlay';
import { contractsApi } from '../../contracts/api/contracts';
import type { ContractProtocol } from '../../../types';

const inputClass =
  'w-full rounded-md bg-canvas border border-edge px-3 py-2 text-sm text-heading placeholder:text-muted focus:outline-none focus:ring-2 focus:ring-accent focus:border-accent transition-colors';
const labelClass = 'block text-sm font-medium text-body mb-1';

interface ContractImportOverlayProps {
  preselectedApiAssetId?: string;
  preselectedApiAssetName?: string;
  onClose: () => void;
  onSuccess: () => void;
}

const STEPS = [
  { id: 'interface', labelKey: 'catalog.contract.step.interface', icon: Layers },
  { id: 'spec',      labelKey: 'catalog.contract.step.spec',      icon: FileCode },
  { id: 'version',   labelKey: 'catalog.contract.step.version',   icon: Tag },
];

type SpecTab = 'upload' | 'url' | 'editor';
type DetectedProtocol = ContractProtocol | null;

/**
 * Detecta o protocolo de uma spec a partir dos primeiros 2KB do conteúdo.
 */
function detectProtocol(content: string): DetectedProtocol {
  const sample = content.slice(0, 2048);
  if (/openapi[:\s"']/i.test(sample)) return 'OpenApi';
  if (/asyncapi[:\s"']/i.test(sample)) return 'AsyncApi';
  if (/syntax\s*=\s*["']proto/i.test(sample)) return 'Protobuf';
  if (/<definitions|<wsdl:/i.test(sample)) return 'Wsdl';
  if (/"swagger"\s*:/i.test(sample)) return 'Swagger';
  if (/type\s+Query\s*\{|schema\s*\{/i.test(sample)) return 'GraphQl';
  return null;
}

/** Overlay de 3 passos para importar uma versão de contrato. */
export function ContractImportOverlay({
  preselectedApiAssetId,
  preselectedApiAssetName,
  onClose,
  onSuccess,
}: ContractImportOverlayProps) {
  const { t } = useTranslation();
  const [step, setStep] = useState(1);
  const [apiAssetId, setApiAssetId] = useState(preselectedApiAssetId ?? '');
  const [specContent, setSpecContent] = useState('');
  const [specTab, setSpecTab] = useState<SpecTab>('upload');
  const [specUrl, setSpecUrl] = useState('');
  const [detectedProtocol, setDetectedProtocol] = useState<DetectedProtocol>(null);
  const [version, setVersion] = useState('');
  const [protocol, setProtocol] = useState<ContractProtocol>('OpenApi');
  const [errors, setErrors] = useState<Record<string, string>>({});

  const mutation = useMutation({
    mutationFn: () => contractsApi.importContract({
      apiAssetId,
      content: specContent,
      version,
      protocol: detectedProtocol ?? protocol,
    }),
    onSuccess: () => onSuccess(),
    onError: () => toast.error(t('common.errorSaving')),
  });

  const handleContentChange = useCallback((content: string) => {
    setSpecContent(content);
    const detected = detectProtocol(content);
    setDetectedProtocol(detected);
    if (detected) setProtocol(detected);
  }, []);

  function validate(): boolean {
    const errs: Record<string, string> = {};
    if (step === 1 && !apiAssetId.trim()) {
      errs.apiAssetId = t('contracts.apiAssetId') + ' ' + t('common.isRequired', 'is required');
    }
    if (step === 2 && !specContent.trim()) {
      errs.spec = t('catalog.contract.step.spec') + ' ' + t('common.isRequired', 'is required');
    }
    if (step === 3 && !version.trim()) {
      errs.version = t('contracts.version') + ' ' + t('common.isRequired', 'is required');
    }
    setErrors(errs);
    return Object.keys(errs).length === 0;
  }

  function handleNext() {
    if (!validate()) return;
    setStep((s) => s + 1);
  }

  async function handleFileDrop(file: File) {
    const text = await file.text();
    handleContentChange(text);
  }

  function renderStep() {
    switch (step) {
      case 1:
        if (preselectedApiAssetId) {
          return (
            <div className="space-y-3">
              <p className="text-sm text-muted">{t('contracts.apiAssetId')}</p>
              <div className="px-3 py-2 rounded-md bg-canvas border border-edge text-sm text-heading font-mono">
                {preselectedApiAssetName ?? preselectedApiAssetId}
              </div>
              <p className="text-xs text-muted font-mono">{preselectedApiAssetId}</p>
            </div>
          );
        }
        return (
          <div className="space-y-4">
            <div>
              <label className={labelClass}>{t('contracts.apiAssetId')} <span className="text-danger">*</span></label>
              <input
                type="text"
                className={`${inputClass} font-mono`}
                value={apiAssetId}
                onChange={(e) => { setApiAssetId(e.target.value); setErrors({}); }}
                placeholder="uuid — Asset ID da interface"
              />
              {errors.apiAssetId && <p className="mt-1 text-xs text-danger">{errors.apiAssetId}</p>}
            </div>
          </div>
        );

      case 2:
        return (
          <div className="space-y-4">
            {/* Protocol badge */}
            {detectedProtocol ? (
              <span className="inline-flex items-center gap-1.5 px-2.5 py-1 rounded-full text-xs font-medium bg-success/15 text-success border border-success/30">
                {t('catalog.contract.protocolDetected', { protocol: detectedProtocol })}
              </span>
            ) : specContent ? (
              <span className="inline-flex items-center gap-1.5 px-2.5 py-1 rounded-full text-xs font-medium bg-warning/15 text-warning border border-warning/30">
                {t('catalog.contract.protocolUnknown')}
              </span>
            ) : null}

            {/* Tabs */}
            <div className="flex gap-0.5 bg-canvas rounded-md p-0.5 w-fit border border-edge">
              {(['upload', 'url', 'editor'] as SpecTab[]).map((tab) => (
                <button
                  key={tab}
                  type="button"
                  onClick={() => setSpecTab(tab)}
                  className={`px-3 py-1.5 rounded text-xs font-medium transition-colors ${
                    specTab === tab
                      ? 'bg-accent/20 text-accent'
                      : 'text-muted hover:text-heading'
                  }`}
                >
                  {t(`catalog.contract.tab.${tab}`)}
                </button>
              ))}
            </div>

            {specTab === 'upload' && (
              <div
                onDragOver={(e) => e.preventDefault()}
                onDrop={(e) => {
                  e.preventDefault();
                  const file = e.dataTransfer.files[0];
                  if (file) handleFileDrop(file);
                }}
                className="border-2 border-dashed border-accent/30 rounded-lg p-8 text-center bg-accent/5 hover:bg-accent/10 transition-colors cursor-pointer"
                onClick={() => {
                  const input = document.createElement('input');
                  input.type = 'file';
                  input.accept = '.yaml,.yml,.json,.xml,.proto,.wsdl';
                  input.onchange = (e) => {
                    const file = (e.target as HTMLInputElement).files?.[0];
                    if (file) handleFileDrop(file);
                  };
                  input.click();
                }}
              >
                <p className="text-sm text-muted">{t('catalog.contract.dropZone.hint')}</p>
                <p className="text-xs text-muted/60 mt-1">{t('catalog.contract.dropZone.formats')}</p>
                {specContent && (
                  <p className="text-xs text-success mt-2">✓ {specContent.length} {t('common.characters', 'chars')}</p>
                )}
              </div>
            )}

            {specTab === 'url' && (
              <div>
                <label className={labelClass}>{t('contracts.specUrl', 'Specification URL')}</label>
                <input type="url" className={inputClass} value={specUrl}
                  onChange={(e) => setSpecUrl(e.target.value)}
                  placeholder="https://api.example.com/openapi.yaml" />
              </div>
            )}

            {specTab === 'editor' && (
              <textarea
                className={`${inputClass} font-mono resize-none`}
                rows={12}
                value={specContent}
                onChange={(e) => handleContentChange(e.target.value)}
                placeholder={'openapi: "3.0.0"\ninfo:\n  title: My API\n  version: 1.0.0'}
              />
            )}

            {errors.spec && <p className="mt-1 text-xs text-danger">{errors.spec}</p>}
          </div>
        );

      case 3:
        return (
          <div className="space-y-4">
            <div>
              <label className={labelClass}>{t('contracts.version')} <span className="text-danger">*</span></label>
              <input
                type="text"
                className={`${inputClass} font-mono`}
                value={version}
                onChange={(e) => { setVersion(e.target.value); setErrors({}); }}
                placeholder="1.0.0"
              />
              {errors.version && <p className="mt-1 text-xs text-danger">{errors.version}</p>}
            </div>
            <div>
              <label className={labelClass}>{t('contracts.protocol')}</label>
              <select
                className={inputClass}
                value={detectedProtocol ?? protocol}
                onChange={(e) => setProtocol(e.target.value as ContractProtocol)}
              >
                {(['OpenApi', 'Swagger', 'AsyncApi', 'Wsdl', 'Protobuf', 'GraphQl'] as ContractProtocol[]).map((p) => (
                  <option key={p} value={p}>{t(`contracts.protocols.${p}`, p)}</option>
                ))}
              </select>
            </div>
          </div>
        );

      default:
        return null;
    }
  }

  const isNextDisabled =
    (step === 1 && !apiAssetId.trim()) ||
    (step === 2 && !specContent.trim()) ||
    (step === 3 && !version.trim());

  return (
    <WizardOverlay
      title={t('catalog.contract.title')}
      headerIcon={<FileCode size={20} />}
      steps={STEPS}
      currentStep={step}
      onClose={onClose}
      onBack={() => { setStep((s) => Math.max(1, s - 1)); setErrors({}); }}
      onNext={handleNext}
      onSubmit={() => mutation.mutate()}
      isSubmitting={mutation.isPending}
      isNextDisabled={isNextDisabled}
      isLastStep={step === STEPS.length}
    >
      {renderStep()}
    </WizardOverlay>
  );
}
```

- [ ] **Step 4: Run test — verify it passes**

```bash
cd src/frontend && npm run test -- --run src/__tests__/catalog/ContractImportOverlay.test.tsx 2>&1 | tail -15
```

Expected: 4 tests pass.

- [ ] **Step 5: Commit**

```bash
git add src/frontend/src/features/catalog/components/ContractImportOverlay.tsx \
        src/frontend/src/__tests__/catalog/ContractImportOverlay.test.tsx
git commit -m "feat(catalog): ContractImportOverlay with protocol auto-detection"
```

---

## Task 7: Wire ServiceCatalogListPage

Replace the inline flat form with `ServiceRegistrationOverlay`.

**Files:**
- Modify: `src/frontend/src/features/catalog/pages/ServiceCatalogListPage.tsx`

- [ ] **Step 1: Read the current file**

```bash
head -200 src/frontend/src/features/catalog/pages/ServiceCatalogListPage.tsx
```

Confirm: the file has `showServiceForm` state + an inline `<Card>` form starting around line 187.

- [ ] **Step 2: Apply the wiring changes**

In `ServiceCatalogListPage.tsx`, make these changes:

**a) Add import at top (after existing imports):**
```tsx
import { ServiceRegistrationOverlay } from '../components/ServiceRegistrationOverlay';
```

**b) Replace the state block** — find and remove:
```tsx
const [showServiceForm, setShowServiceForm] = useState(false);
const [serviceForm, setServiceForm] = useState({
  name: '',
  team: '',
  description: '',
  domain: '',
  serviceType: 'RestApi',
  criticality: 'Medium',
  exposureType: 'Internal',
  technicalOwner: '',
  businessOwner: '',
  documentationUrl: '',
  repositoryUrl: '',
});
```

Replace with:
```tsx
const [showRegistration, setShowRegistration] = useState(false);
```

**c) Remove the `registerService` mutation** — find and delete the entire `const registerService = useMutation({...})` block (including the `useMutation` import if it becomes unused — but `useMutation` is still used by other parts, so only remove the variable, not the import).

**d) Update the button** — find:
```tsx
<Button onClick={() => setShowServiceForm((v) => !v)}>
  <Plus size={16} /> {t('serviceCatalog.registerService')}
</Button>
```

Replace with:
```tsx
<Button onClick={() => setShowRegistration(true)}>
  <Plus size={16} /> {t('serviceCatalog.registerService')}
</Button>
```

**e) Remove the entire inline form block** — find and delete everything from:
```tsx
{/* ── Formulário de registro de serviço ── */}
{showServiceForm && (
  <Card className="mb-6">
```
...down to its closing `)}` (approximately lines 187–340+).

**f) Add the overlay render** — just after the `<div className="flex items-start justify-between mb-2">` closing `</div>`, add:
```tsx
{showRegistration && (
  <ServiceRegistrationOverlay
    onClose={() => setShowRegistration(false)}
    onSuccess={(id) => {
      setShowRegistration(false);
      queryClient.invalidateQueries({ queryKey: ['catalog-services'] });
      navigate(`/services/${id}`);
    }}
  />
)}
```

**g) Ensure `navigate` and `useQueryClient` are imported** — add if missing:
```tsx
import { useQueryClient } from '@tanstack/react-query';
import { useNavigate } from 'react-router-dom';
```

And at the top of the component function body:
```tsx
const queryClient = useQueryClient();
const navigate = useNavigate();
```

- [ ] **Step 3: Run the existing test to verify nothing broke**

```bash
cd src/frontend && npm run test -- --run src/__tests__/catalog/ServiceCatalogListPage.test.tsx 2>&1 | tail -20
```

Expected: existing tests pass (or gracefully skip the old form tests that tested the now-deleted inline form).

- [ ] **Step 4: Run full catalog test suite**

```bash
cd src/frontend && npm run test -- --run src/__tests__/catalog/ 2>&1 | tail -20
```

Expected: all pass.

- [ ] **Step 5: Commit**

```bash
git add src/frontend/src/features/catalog/pages/ServiceCatalogListPage.tsx
git commit -m "feat(catalog): replace inline registration form with ServiceRegistrationOverlay"
```

---

## Task 8: Wire ServiceDetailPage

Add overlay triggers for interface creation and contract import.

**Files:**
- Modify: `src/frontend/src/features/catalog/pages/ServiceDetailPage.tsx`

- [ ] **Step 1: Add imports**

At the top of `ServiceDetailPage.tsx`, add:
```tsx
import { useQueryClient } from '@tanstack/react-query';
import { ServiceInterfaceOverlay } from '../components/ServiceInterfaceOverlay';
import { ContractImportOverlay } from '../components/ContractImportOverlay';
```

- [ ] **Step 2: Add state**

Inside the `ServiceDetailPage` function, after existing `useState` calls, add:
```tsx
const queryClient = useQueryClient();
const [showInterfaceOverlay, setShowInterfaceOverlay] = useState(false);
const [showContractOverlay, setShowContractOverlay] = useState(false);
const [contractApiAssetId, setContractApiAssetId] = useState<string | undefined>(undefined);
const [contractApiAssetName, setContractApiAssetName] = useState<string | undefined>(undefined);
```

- [ ] **Step 3: Add overlay renders before the closing `</PageContainer>`**

Find the closing `</PageContainer>` tag and just before it, insert:
```tsx
{showInterfaceOverlay && (
  <ServiceInterfaceOverlay
    serviceId={serviceId!}
    serviceName={service.displayName || service.name}
    onClose={() => setShowInterfaceOverlay(false)}
    onSuccess={() => {
      setShowInterfaceOverlay(false);
      queryClient.invalidateQueries({ queryKey: ['catalog-service-detail', serviceId] });
    }}
  />
)}

{showContractOverlay && (
  <ContractImportOverlay
    preselectedApiAssetId={contractApiAssetId}
    preselectedApiAssetName={contractApiAssetName}
    onClose={() => setShowContractOverlay(false)}
    onSuccess={() => {
      setShowContractOverlay(false);
      queryClient.invalidateQueries({ queryKey: ['catalog-service-contracts', serviceId] });
    }}
  />
)}
```

- [ ] **Step 4: Add trigger buttons in the Interfaces tab**

In the `ServiceInterfacesTab` component invocation (in the `interfaces` tab case), the tab renders a `<ServiceInterfacesTab>` component. Open `src/frontend/src/features/catalog/components/ServiceInterfacesTab.tsx` and find where the tab's header is rendered. Add a button there — OR, simpler: find the `interfaces` tab content in `ServiceDetailPage.tsx` and, if the tab content is rendered inline, add a button above it.

Look for where the tab renders interfaces. In the `tabItems` section of ServiceDetailPage, the `interfaces` tab renders `<ServiceInterfacesTab>`. Find that render and wrap it:

```tsx
{activeTab === 'interfaces' && (
  <PageSection>
    <div className="flex justify-end mb-4">
      <Button onClick={() => setShowInterfaceOverlay(true)}>
        <Plus size={14} /> {t('catalog.interface.title')}
      </Button>
    </div>
    <ServiceInterfacesTab serviceId={serviceId!} />
  </PageSection>
)}
```

Note: check the existing tab rendering pattern in the file; if `ServiceInterfacesTab` is rendered with a different pattern, match it.

- [ ] **Step 5: Add trigger button for contract import in the APIs tab**

Find where `service.apis` or API list is rendered (the `apis` tab). Add a button to open the contract overlay for a specific interface. A minimal approach: add an "Import Contract" button to the APIs tab header that opens the overlay without pre-selection:

```tsx
<Button
  variant="secondary"
  size="sm"
  onClick={() => {
    setContractApiAssetId(undefined);
    setContractApiAssetName(undefined);
    setShowContractOverlay(true);
  }}
>
  <Plus size={14} /> {t('catalog.contract.title')}
</Button>
```

For rows that have a specific API asset, trigger with its ID:
```tsx
onClick={() => {
  setContractApiAssetId(api.apiAssetId);
  setContractApiAssetName(api.name);
  setShowContractOverlay(true);
}}
```

- [ ] **Step 6: Run build to verify no type errors**

```bash
cd src/frontend && npm run build 2>&1 | grep -E "error|Error" | head -20
```

Expected: no TypeScript errors.

- [ ] **Step 7: Commit**

```bash
git add src/frontend/src/features/catalog/pages/ServiceDetailPage.tsx
git commit -m "feat(catalog): wire ServiceInterfaceOverlay and ContractImportOverlay in ServiceDetailPage"
```

---

## Task 9: Wire ContractsPage

Replace the inline import form in ContractsPage with `ContractImportOverlay`.

**Files:**
- Modify: `src/frontend/src/features/catalog/pages/ContractsPage.tsx`

- [ ] **Step 1: Add import**

At the top of `ContractsPage.tsx`, add:
```tsx
import { ContractImportOverlay } from '../components/ContractImportOverlay';
```

- [ ] **Step 2: Replace import state**

Find:
```tsx
const [showImportForm, setShowImportForm] = useState(false);
const [importForm, setImportForm] = useState({
  apiAssetId: '',
  content: '',
  version: '',
  protocol: 'OpenApi' as ContractProtocol,
});
```

Replace with:
```tsx
const [showContractOverlay, setShowContractOverlay] = useState(false);
```

- [ ] **Step 3: Remove the import mutation**

Find and delete:
```tsx
const importMutation = useMutation({
  mutationFn: contractsApi.importContract,
  onSuccess: () => {
    queryClient.invalidateQueries({ queryKey: ['contracts'] });
    setShowImportForm(false);
    setImportForm({ apiAssetId: '', content: '', version: '', protocol: 'OpenApi' });
  },
});
```

- [ ] **Step 4: Update the Import Contract button**

Find:
```tsx
<Button onClick={() => setShowImportForm((v) => !v)}>
  <Plus size={16} /> {t('contracts.importContract')}
</Button>
```

Replace with:
```tsx
<Button onClick={() => setShowContractOverlay(true)}>
  <Plus size={16} /> {t('contracts.importContract')}
</Button>
```

- [ ] **Step 5: Remove the inline Import Form JSX block**

Find and delete everything from:
```tsx
{/* Import Form */}
{showImportForm && (
  <Card className="mb-6">
```
...through its closing `)}`.

- [ ] **Step 6: Add the overlay render**

After the `<PageHeader .../>` element (or the notification block), insert:
```tsx
{showContractOverlay && (
  <ContractImportOverlay
    onClose={() => setShowContractOverlay(false)}
    onSuccess={() => {
      setShowContractOverlay(false);
      queryClient.invalidateQueries({ queryKey: ['contracts'] });
    }}
  />
)}
```

- [ ] **Step 7: Run build to verify no type errors**

```bash
cd src/frontend && npm run build 2>&1 | grep -E "error|Error" | head -20
```

Expected: 0 errors.

- [ ] **Step 8: Run all catalog tests**

```bash
cd src/frontend && npm run test -- --run src/__tests__/catalog/ 2>&1 | tail -20
```

Expected: all pass.

- [ ] **Step 9: Commit**

```bash
git add src/frontend/src/features/catalog/pages/ContractsPage.tsx
git commit -m "feat(catalog): replace inline import form with ContractImportOverlay in ContractsPage"
```

---

## Final Verification

- [ ] **Run all frontend tests**

```bash
cd src/frontend && npm run test -- --run 2>&1 | tail -30
```

Expected: all tests pass, no failures.

- [ ] **Run lint**

```bash
cd src/frontend && npm run lint 2>&1 | tail -20
```

Expected: 0 errors.

- [ ] **Run build**

```bash
cd src/frontend && npm run build 2>&1 | tail -10
```

Expected: build completes with exit 0.
