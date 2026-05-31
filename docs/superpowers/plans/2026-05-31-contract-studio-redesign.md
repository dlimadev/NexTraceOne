# Contract Studio Redesign — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Redesign `ContractStudioPage` into an APIM-style Hub with stats + in-progress drafts + categorised type picker, and rewrite all builder pages to use a shared Code + Visual split layout (Monaco left, protocol-aware preview right).

**Architecture:** `ContractBuilderLayout` wraps the existing `SimpleSplitPane` and `MonacoEditorWrapper` with a debounced parse → preview pipeline. Each builder page becomes a thin wrapper that provides `language`, `initialContent`, and a `renderPreview` function. The Hub reads from `useContractsSummary()` and `useContractList()` which already exist.

**Tech Stack:** React 19, TypeScript 5.9, Vitest + Testing Library, js-yaml 4.1.1, TanStack Query 5, SimpleSplitPane (existing), MonacoEditorWrapper (existing)

---

## File Map

### New files
```
src/frontend/src/features/contracts/studio/
├── ContractBuilderLayout.tsx
└── components/
    ├── BuilderHeader.tsx
    └── previews/
        ├── RestOperationsPreview.tsx
        ├── AsyncApiChannelsPreview.tsx
        ├── GraphQlTypesPreview.tsx
        ├── ProtobufServicesPreview.tsx
        └── SoapOperationsPreview.tsx

src/frontend/src/__tests__/contracts/
├── ContractBuilderLayout.test.tsx
├── RestOperationsPreview.test.tsx
└── AsyncApiChannelsPreview.test.tsx
```

### Modified files
```
src/frontend/src/features/contracts/pages/
├── ContractStudioPage.tsx          ← Hub APIM-style
├── RestOpenApiBuilderPage.tsx       ← uses ContractBuilderLayout
├── AsyncApiBuilderPage.tsx          ← uses ContractBuilderLayout
├── GraphQLBuilderPage.tsx           ← uses ContractBuilderLayout
├── ProtobufBuilderPage.tsx          ← uses ContractBuilderLayout
└── SoapWsdlBuilderPage.tsx          ← uses ContractBuilderLayout

src/frontend/src/locales/
├── en.json
├── pt-BR.json
├── es.json
└── pt-PT.json
```

### NOT modified
- `SimpleSplitPane.tsx` — used as-is
- `MonacoEditorWrapper.tsx` — used as-is
- `useContractList.ts` / `useContractsSummary` — used as-is
- `ContractWorkspacePage.tsx` and all `workspace/` files

---

## Task 1: BuilderHeader component

**Files:**
- Create: `src/frontend/src/features/contracts/studio/components/BuilderHeader.tsx`

- [ ] **Step 1: Create the file**

```tsx
// src/frontend/src/features/contracts/studio/components/BuilderHeader.tsx
import { Link } from 'react-router-dom';
import { ArrowLeft } from 'lucide-react';
import { useTranslation } from 'react-i18next';
import { Badge } from '../../../../components/Badge';
import { Button } from '../../../../components/Button';

export type BuilderValidationStatus = 'idle' | 'valid' | 'errors';

export interface BuilderHeaderProps {
  contractName: string;
  protocol: string;
  validationStatus: BuilderValidationStatus;
  errorLine?: number;
  onFormat?: () => void;
  /** If provided, shows the Save Draft button */
  onSave?: (content: string) => void;
  /** If provided, shows the Publish button */
  onPublish?: (content: string) => void;
  /** Returns current editor content at call time */
  getContent: () => string;
}

export function BuilderHeader({
  contractName,
  protocol,
  validationStatus,
  errorLine,
  onFormat,
  onSave,
  onPublish,
  getContent,
}: BuilderHeaderProps) {
  const { t } = useTranslation();

  const chipLabel =
    validationStatus === 'valid'
      ? t('contractBuilder.validation.valid')
      : validationStatus === 'errors'
        ? t('contractBuilder.validation.errors', { line: errorLine ?? '' })
        : null;

  const chipVariant: 'success' | 'destructive' =
    validationStatus === 'valid' ? 'success' : 'destructive';

  return (
    <div
      className="flex items-center justify-between h-11 px-4 border-b border-edge bg-card flex-shrink-0"
      data-testid="builder-header"
    >
      {/* Breadcrumb */}
      <div className="flex items-center gap-2 min-w-0">
        <Link
          to="/contracts/studio"
          className="flex items-center gap-1 text-xs text-muted hover:text-heading transition-colors"
        >
          <ArrowLeft size={13} />
          Studio
        </Link>
        <span className="text-xs text-faded">/</span>
        <span className="text-xs font-medium text-heading truncate max-w-48">{contractName}</span>
        <Badge variant="neutral" className="text-[10px] font-mono ml-1">{protocol}</Badge>
        {chipLabel && (
          <Badge variant={chipVariant} className="text-[10px]">{chipLabel}</Badge>
        )}
      </div>

      {/* Actions */}
      <div className="flex items-center gap-2">
        {onFormat && (
          <Button size="sm" variant="ghost" onClick={onFormat} data-testid="btn-format">
            {t('contractBuilder.header.format')}
          </Button>
        )}
        {onSave && (
          <Button size="sm" variant="secondary" onClick={() => onSave(getContent())} data-testid="btn-save">
            {t('contractBuilder.header.saveDraft')}
          </Button>
        )}
        {onPublish && (
          <Button size="sm" onClick={() => onPublish(getContent())} data-testid="btn-publish">
            {t('contractBuilder.header.publish')}
          </Button>
        )}
      </div>
    </div>
  );
}
```

- [ ] **Step 2: Commit**

```bash
git add src/frontend/src/features/contracts/studio/components/BuilderHeader.tsx
git commit -m "feat(studio): add BuilderHeader component"
```

---

## Task 2: ContractBuilderLayout + tests

**Files:**
- Create: `src/frontend/src/features/contracts/studio/ContractBuilderLayout.tsx`
- Create: `src/frontend/src/__tests__/contracts/ContractBuilderLayout.test.tsx`

- [ ] **Step 1: Write the failing tests**

```tsx
// src/frontend/src/__tests__/contracts/ContractBuilderLayout.test.tsx
import * as React from 'react';
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { render, screen, act } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';

vi.mock('react-i18next', () => ({
  useTranslation: () => ({ t: (key: string) => key }),
}));
vi.mock('monaco-editor', () => ({ default: {} }));

let capturedOnChange: ((val: string) => void) | undefined;
vi.mock('../../../features/contracts/workspace/editor/MonacoEditorWrapper', () => ({
  MonacoEditorWrapper: vi.fn(({ onChange }: { onChange?: (v: string) => void }) => {
    capturedOnChange = onChange;
    return null;
  }),
}));

import { ContractBuilderLayout } from '../../../features/contracts/studio/ContractBuilderLayout';

function wrap(node: React.ReactNode) {
  return render(<MemoryRouter>{node}</MemoryRouter>);
}

describe('ContractBuilderLayout', () => {
  beforeEach(() => {
    vi.useFakeTimers();
    capturedOnChange = undefined;
  });
  afterEach(() => {
    vi.useRealTimers();
  });

  it('renders without crashing', () => {
    wrap(
      <ContractBuilderLayout
        contractName="Payments API"
        protocol="OpenAPI 3.1"
        language="yaml"
        initialContent="openapi: 3.1.0"
        renderPreview={() => <div data-testid="preview">Preview</div>}
      />,
    );
    expect(screen.getByTestId('contract-builder-layout')).toBeInTheDocument();
    expect(screen.getByTestId('preview')).toBeInTheDocument();
  });

  it('shows parse-error banner when YAML is invalid, after debounce', () => {
    wrap(
      <ContractBuilderLayout
        contractName="Test"
        protocol="OpenAPI 3.1"
        language="yaml"
        initialContent="openapi: 3.1.0"
        renderPreview={() => <div>Preview</div>}
      />,
    );
    act(() => { capturedOnChange?.('invalid: {{{'); });
    act(() => { vi.advanceTimersByTime(500); });
    expect(screen.getByTestId('parse-error-banner')).toBeInTheDocument();
  });

  it('calls onSave with current content when save button is clicked', () => {
    const onSave = vi.fn();
    wrap(
      <ContractBuilderLayout
        contractName="Test"
        protocol="OpenAPI 3.1"
        language="yaml"
        initialContent="openapi: 3.1.0"
        renderPreview={() => null}
        onSave={onSave}
      />,
    );
    screen.getByTestId('btn-save').click();
    expect(onSave).toHaveBeenCalledWith('openapi: 3.1.0');
  });

  it('does not render Save button when onSave is not provided', () => {
    wrap(
      <ContractBuilderLayout
        contractName="Test"
        protocol="OpenAPI 3.1"
        language="yaml"
        initialContent=""
        renderPreview={() => null}
      />,
    );
    expect(screen.queryByTestId('btn-save')).not.toBeInTheDocument();
  });
});
```

- [ ] **Step 2: Run tests — expect FAIL**

```bash
cd src/frontend && npm run test -- ContractBuilderLayout
```

Expected: `Cannot find module '../../../features/contracts/studio/ContractBuilderLayout'`

- [ ] **Step 3: Create ContractBuilderLayout**

```tsx
// src/frontend/src/features/contracts/studio/ContractBuilderLayout.tsx
import { useState, useEffect, useRef, useCallback } from 'react';
import * as yaml from 'js-yaml';
import SimpleSplitPane from '../../../components/SimpleSplitPane';
import { MonacoEditorWrapper } from '../workspace/editor/MonacoEditorWrapper';
import { BuilderHeader } from './components/BuilderHeader';
import type { BuilderValidationStatus } from './components/BuilderHeader';

export type BuilderLanguage = 'yaml' | 'json' | 'graphql' | 'proto' | 'xml';

export interface ContractBuilderLayoutProps {
  contractName: string;
  protocol: string;
  language: BuilderLanguage;
  initialContent: string;
  renderPreview: (content: string) => React.ReactNode;
  onSave?: (content: string) => void;
  onPublish?: (content: string) => void;
}

function tryParse(language: BuilderLanguage, content: string): { ok: boolean; line?: number } {
  if (!content.trim()) return { ok: true };
  try {
    if (language === 'yaml') {
      yaml.load(content);
      return { ok: true };
    }
    if (language === 'json') {
      JSON.parse(content);
      return { ok: true };
    }
    return { ok: true };
  } catch (e: unknown) {
    const line = (e as { mark?: { line?: number } })?.mark?.line;
    return { ok: false, line: line !== undefined ? line + 1 : undefined };
  }
}

export function ContractBuilderLayout({
  contractName,
  protocol,
  language,
  initialContent,
  renderPreview,
  onSave,
  onPublish,
}: ContractBuilderLayoutProps) {
  const [content, setContent] = useState(initialContent);
  const [debouncedContent, setDebouncedContent] = useState(initialContent);
  const [lastValidContent, setLastValidContent] = useState(initialContent);
  const [validationStatus, setValidationStatus] = useState<BuilderValidationStatus>('idle');
  const [errorLine, setErrorLine] = useState<number | undefined>(undefined);
  const getContent = useCallback(() => content, [content]);

  // Debounce: wait 400 ms after last keystroke before parsing
  useEffect(() => {
    const t = setTimeout(() => setDebouncedContent(content), 400);
    return () => clearTimeout(t);
  }, [content]);

  // Re-parse whenever debounced content changes
  useEffect(() => {
    const result = tryParse(language, debouncedContent);
    if (result.ok) {
      setLastValidContent(debouncedContent);
      setErrorLine(undefined);
      setValidationStatus(debouncedContent.trim() ? 'valid' : 'idle');
    } else {
      setErrorLine(result.line);
      setValidationStatus('errors');
    }
  }, [debouncedContent, language]);

  const handleFormat = useCallback(() => {
    if (language === 'yaml') {
      try {
        setContent(yaml.dump(yaml.load(content), { indent: 2 }));
      } catch { /* ignore: invalid YAML */ }
    } else if (language === 'json') {
      try {
        setContent(JSON.stringify(JSON.parse(content), null, 2));
      } catch { /* ignore: invalid JSON */ }
    }
  }, [content, language]);

  const monacoLanguage = language === 'proto' ? 'plaintext' : language;

  return (
    <div className="flex flex-col h-full" data-testid="contract-builder-layout">
      <BuilderHeader
        contractName={contractName}
        protocol={protocol}
        validationStatus={validationStatus}
        errorLine={errorLine}
        onFormat={handleFormat}
        onSave={onSave}
        onPublish={onPublish}
        getContent={getContent}
      />

      <div className="flex-1 min-h-0">
        <SimpleSplitPane
          className="h-full"
          initialLeftPercent={45}
          minLeftPercent={25}
          minRightPercent={25}
          left={
            <MonacoEditorWrapper
              value={content}
              language={monacoLanguage}
              onChange={setContent}
            />
          }
          right={
            <div className="h-full overflow-auto p-4 bg-elevated">
              {validationStatus === 'errors' && (
                <div
                  className="mb-3 px-3 py-2 rounded text-xs text-destructive bg-destructive/10 border border-destructive/20"
                  data-testid="parse-error-banner"
                >
                  contractBuilder.preview.parseError
                  {errorLine ? ` (line ${errorLine})` : ''}
                </div>
              )}
              {renderPreview(lastValidContent)}
            </div>
          }
        />
      </div>
    </div>
  );
}
```

- [ ] **Step 4: Run tests — expect PASS**

```bash
cd src/frontend && npm run test -- ContractBuilderLayout
```

Expected: 4 tests pass.

- [ ] **Step 5: Commit**

```bash
git add src/frontend/src/features/contracts/studio/ContractBuilderLayout.tsx \
        src/frontend/src/__tests__/contracts/ContractBuilderLayout.test.tsx
git commit -m "feat(studio): add ContractBuilderLayout with Monaco + debounced preview"
```

---

## Task 3: RestOperationsPreview + test

**Files:**
- Create: `src/frontend/src/features/contracts/studio/components/previews/RestOperationsPreview.tsx`
- Create: `src/frontend/src/__tests__/contracts/RestOperationsPreview.test.tsx`

- [ ] **Step 1: Write the failing tests**

```tsx
// src/frontend/src/__tests__/contracts/RestOperationsPreview.test.tsx
import * as React from 'react';
import { describe, it, expect, vi } from 'vitest';
import { render, screen } from '@testing-library/react';

vi.mock('react-i18next', () => ({
  useTranslation: () => ({ t: (key: string) => key }),
}));

import { RestOperationsPreview } from '../../../features/contracts/studio/components/previews/RestOperationsPreview';

const VALID_YAML = `
openapi: 3.1.0
info:
  title: Test API
  version: 1.0.0
paths:
  /users:
    get:
      summary: List users
    post:
      summary: Create user
  /users/{id}:
    get:
      summary: Get user by ID
    delete:
      summary: Delete user
`;

describe('RestOperationsPreview', () => {
  it('renders paths and methods from valid OpenAPI YAML', () => {
    render(<RestOperationsPreview content={VALID_YAML} />);
    expect(screen.getByText('/users')).toBeInTheDocument();
    expect(screen.getByText('/users/{id}')).toBeInTheDocument();
    expect(screen.getAllByText('GET').length).toBeGreaterThan(0);
    expect(screen.getByText('List users')).toBeInTheDocument();
  });

  it('shows operation count in footer', () => {
    render(<RestOperationsPreview content={VALID_YAML} />);
    expect(screen.getByText(/operations/i)).toBeInTheDocument();
  });

  it('renders empty state for invalid YAML without throwing', () => {
    render(<RestOperationsPreview content="invalid: {{{" />);
    expect(screen.getByTestId('preview-empty')).toBeInTheDocument();
  });

  it('renders empty state for YAML with no paths', () => {
    render(<RestOperationsPreview content="openapi: 3.1.0\ninfo:\n  title: Empty" />);
    expect(screen.getByTestId('preview-empty')).toBeInTheDocument();
  });
});
```

- [ ] **Step 2: Run — expect FAIL**

```bash
cd src/frontend && npm run test -- RestOperationsPreview
```

- [ ] **Step 3: Create RestOperationsPreview**

```tsx
// src/frontend/src/features/contracts/studio/components/previews/RestOperationsPreview.tsx
import * as yaml from 'js-yaml';
import { useTranslation } from 'react-i18next';
import { Badge } from '../../../../../components/Badge';

interface ParsedOperation {
  method: string;
  summary: string;
}

type PathsMap = Record<string, ParsedOperation[]>;

const HTTP_METHODS = ['get', 'post', 'put', 'patch', 'delete', 'head', 'options'];

const METHOD_VARIANT: Record<string, 'success' | 'default' | 'warning' | 'info' | 'destructive'> = {
  GET: 'success',
  POST: 'default',
  PUT: 'warning',
  PATCH: 'info',
  DELETE: 'destructive',
  HEAD: 'neutral',
  OPTIONS: 'neutral',
};

function parsePaths(content: string): PathsMap | null {
  try {
    const doc = yaml.load(content) as {
      paths?: Record<string, Record<string, { summary?: string }>>;
    };
    if (!doc?.paths || typeof doc.paths !== 'object') return null;
    const result: PathsMap = {};
    for (const [path, methods] of Object.entries(doc.paths)) {
      if (!methods || typeof methods !== 'object') continue;
      const ops = Object.entries(methods)
        .filter(([m]) => HTTP_METHODS.includes(m.toLowerCase()))
        .map(([m, op]) => ({
          method: m.toUpperCase(),
          summary: (op as { summary?: string })?.summary ?? '',
        }));
      if (ops.length > 0) result[path] = ops;
    }
    return Object.keys(result).length > 0 ? result : null;
  } catch {
    return null;
  }
}

interface RestOperationsPreviewProps {
  content: string;
}

export function RestOperationsPreview({ content }: RestOperationsPreviewProps) {
  const { t } = useTranslation();
  const paths = parsePaths(content);

  if (!paths) {
    return (
      <div className="flex flex-col items-center justify-center h-32 text-muted" data-testid="preview-empty">
        <span className="text-xs">{t('contractBuilder.preview.empty')}</span>
      </div>
    );
  }

  const totalOps = Object.values(paths).reduce((sum, ops) => sum + ops.length, 0);

  return (
    <div className="space-y-3" data-testid="rest-operations-preview">
      {Object.entries(paths).map(([path, ops]) => (
        <div key={path}>
          <div className="text-xs font-mono font-semibold text-heading mb-1.5">{path}</div>
          <div className="space-y-1 pl-3">
            {ops.map((op) => (
              <div key={op.method} className="flex items-center gap-2">
                <Badge
                  variant={METHOD_VARIANT[op.method] ?? 'default'}
                  className="text-[10px] font-mono w-16 justify-center flex-shrink-0"
                >
                  {op.method}
                </Badge>
                <span className="text-xs text-muted truncate">{op.summary}</span>
              </div>
            ))}
          </div>
        </div>
      ))}

      <div className="pt-3 border-t border-edge text-xs text-faded">
        {Object.keys(paths).length} paths · {totalOps} operations
      </div>
    </div>
  );
}
```

- [ ] **Step 4: Run — expect PASS**

```bash
cd src/frontend && npm run test -- RestOperationsPreview
```

- [ ] **Step 5: Commit**

```bash
git add src/frontend/src/features/contracts/studio/components/previews/RestOperationsPreview.tsx \
        src/frontend/src/__tests__/contracts/RestOperationsPreview.test.tsx
git commit -m "feat(studio): add RestOperationsPreview with OpenAPI path parsing"
```

---

## Task 4: AsyncApiChannelsPreview + test

**Files:**
- Create: `src/frontend/src/features/contracts/studio/components/previews/AsyncApiChannelsPreview.tsx`
- Create: `src/frontend/src/__tests__/contracts/AsyncApiChannelsPreview.test.tsx`

- [ ] **Step 1: Write the failing tests**

```tsx
// src/frontend/src/__tests__/contracts/AsyncApiChannelsPreview.test.tsx
import * as React from 'react';
import { describe, it, expect, vi } from 'vitest';
import { render, screen } from '@testing-library/react';

vi.mock('react-i18next', () => ({
  useTranslation: () => ({ t: (key: string) => key }),
}));

import { AsyncApiChannelsPreview } from '../../../features/contracts/studio/components/previews/AsyncApiChannelsPreview';

const VALID_YAML = `
asyncapi: 3.0.0
info:
  title: Order Events
  version: 1.0.0
channels:
  userRegistered:
    address: user.registered
    bindings:
      kafka: {}
  orderCreated:
    address: order.created
    bindings:
      amqp: {}
`;

describe('AsyncApiChannelsPreview', () => {
  it('renders channel addresses from valid AsyncAPI YAML', () => {
    render(<AsyncApiChannelsPreview content={VALID_YAML} />);
    expect(screen.getByText('user.registered')).toBeInTheDocument();
    expect(screen.getByText('order.created')).toBeInTheDocument();
  });

  it('renders protocol badges', () => {
    render(<AsyncApiChannelsPreview content={VALID_YAML} />);
    expect(screen.getByText('kafka')).toBeInTheDocument();
    expect(screen.getByText('amqp')).toBeInTheDocument();
  });

  it('renders empty state for invalid YAML without throwing', () => {
    render(<AsyncApiChannelsPreview content="not: valid: {{{" />);
    expect(screen.getByTestId('preview-empty')).toBeInTheDocument();
  });
});
```

- [ ] **Step 2: Run — expect FAIL**

```bash
cd src/frontend && npm run test -- AsyncApiChannelsPreview
```

- [ ] **Step 3: Create AsyncApiChannelsPreview**

```tsx
// src/frontend/src/features/contracts/studio/components/previews/AsyncApiChannelsPreview.tsx
import * as yaml from 'js-yaml';
import { useTranslation } from 'react-i18next';
import { Badge } from '../../../../../components/Badge';

interface ParsedChannel {
  id: string;
  address: string;
  protocol: string;
}

function parseChannels(content: string): ParsedChannel[] | null {
  try {
    const doc = yaml.load(content) as {
      channels?: Record<string, { address?: string; bindings?: Record<string, unknown> }>;
    };
    if (!doc?.channels || typeof doc.channels !== 'object') return null;
    const channels = Object.entries(doc.channels).map(([id, ch]) => ({
      id,
      address: ch.address ?? id,
      protocol: ch.bindings ? Object.keys(ch.bindings)[0] ?? 'unknown' : 'unknown',
    }));
    return channels.length > 0 ? channels : null;
  } catch {
    return null;
  }
}

interface AsyncApiChannelsPreviewProps {
  content: string;
}

export function AsyncApiChannelsPreview({ content }: AsyncApiChannelsPreviewProps) {
  const { t } = useTranslation();
  const channels = parseChannels(content);

  if (!channels) {
    return (
      <div className="flex flex-col items-center justify-center h-32 text-muted" data-testid="preview-empty">
        <span className="text-xs">{t('contractBuilder.preview.empty')}</span>
      </div>
    );
  }

  return (
    <div className="space-y-2" data-testid="async-channels-preview">
      {channels.map((ch) => (
        <div key={ch.id} className="flex items-center gap-2 py-1">
          <span className="w-2 h-2 rounded-full bg-success flex-shrink-0" />
          <span className="text-xs font-mono text-heading flex-1 truncate">{ch.address}</span>
          <Badge variant="neutral" className="text-[10px] font-mono">{ch.protocol}</Badge>
        </div>
      ))}
      <div className="pt-2 border-t border-edge text-xs text-faded">
        {channels.length} {channels.length === 1 ? 'channel' : 'channels'}
      </div>
    </div>
  );
}
```

- [ ] **Step 4: Run — expect PASS**

```bash
cd src/frontend && npm run test -- AsyncApiChannelsPreview
```

- [ ] **Step 5: Commit**

```bash
git add src/frontend/src/features/contracts/studio/components/previews/AsyncApiChannelsPreview.tsx \
        src/frontend/src/__tests__/contracts/AsyncApiChannelsPreview.test.tsx
git commit -m "feat(studio): add AsyncApiChannelsPreview with channel parsing"
```

---

## Task 5: GraphQL, Protobuf and SOAP previews

**Files:**
- Create: `src/frontend/src/features/contracts/studio/components/previews/GraphQlTypesPreview.tsx`
- Create: `src/frontend/src/features/contracts/studio/components/previews/ProtobufServicesPreview.tsx`
- Create: `src/frontend/src/features/contracts/studio/components/previews/SoapOperationsPreview.tsx`

- [ ] **Step 1: Create GraphQlTypesPreview**

```tsx
// src/frontend/src/features/contracts/studio/components/previews/GraphQlTypesPreview.tsx
import { useTranslation } from 'react-i18next';
import { Badge } from '../../../../../components/Badge';

interface ParsedType {
  kind: string;
  name: string;
  fieldCount: number;
}

function parseTypes(sdl: string): ParsedType[] | null {
  try {
    const regex = /(?:^|\n)(type|input|enum|interface|union)\s+(\w+)[^{]*\{([^}]*)\}/gm;
    const types: ParsedType[] = [];
    let match;
    while ((match = regex.exec(sdl)) !== null) {
      const [, kind, name, body] = match;
      const fields = body
        .split('\n')
        .map((l) => l.trim())
        .filter((l) => l && !l.startsWith('#') && !l.startsWith('"""'));
      if (name !== 'Query' && name !== 'Mutation' && name !== 'Subscription') {
        types.push({ kind, name, fieldCount: fields.length });
      } else {
        types.push({ kind: 'operation', name, fieldCount: fields.length });
      }
    }
    return types.length > 0 ? types : null;
  } catch {
    return null;
  }
}

const KIND_VARIANT: Record<string, 'default' | 'info' | 'warning' | 'success' | 'neutral'> = {
  type: 'default',
  input: 'info',
  enum: 'warning',
  interface: 'neutral',
  union: 'neutral',
  operation: 'success',
};

interface GraphQlTypesPreviewProps {
  content: string;
}

export function GraphQlTypesPreview({ content }: GraphQlTypesPreviewProps) {
  const { t } = useTranslation();
  const types = parseTypes(content);

  if (!types) {
    return (
      <div className="flex flex-col items-center justify-center h-32 text-muted" data-testid="preview-empty">
        <span className="text-xs">{t('contractBuilder.preview.empty')}</span>
      </div>
    );
  }

  return (
    <div className="space-y-1.5" data-testid="graphql-types-preview">
      {types.map((tp) => (
        <div key={tp.name} className="flex items-center gap-2">
          <Badge variant={KIND_VARIANT[tp.kind] ?? 'neutral'} className="text-[10px] w-16 justify-center flex-shrink-0">
            {tp.kind}
          </Badge>
          <span className="text-xs font-mono text-heading flex-1">{tp.name}</span>
          <span className="text-xs text-faded">{tp.fieldCount} fields</span>
        </div>
      ))}
      <div className="pt-2 border-t border-edge text-xs text-faded">
        {types.length} {types.length === 1 ? 'type' : 'types'}
      </div>
    </div>
  );
}
```

- [ ] **Step 2: Create ProtobufServicesPreview**

```tsx
// src/frontend/src/features/contracts/studio/components/previews/ProtobufServicesPreview.tsx
import { useTranslation } from 'react-i18next';
import { Badge } from '../../../../../components/Badge';

interface ParsedService {
  name: string;
  rpcs: string[];
}

function parseProto(content: string): { services: ParsedService[]; messages: string[] } | null {
  try {
    const services: ParsedService[] = [];
    const messages: string[] = [];

    const serviceRegex = /service\s+(\w+)\s*\{([^}]*)\}/gm;
    let m;
    while ((m = serviceRegex.exec(content)) !== null) {
      const rpcs = [...m[2].matchAll(/rpc\s+(\w+)/g)].map((r) => r[1]);
      services.push({ name: m[1], rpcs });
    }

    const msgRegex = /message\s+(\w+)/g;
    while ((m = msgRegex.exec(content)) !== null) {
      messages.push(m[1]);
    }

    if (services.length === 0 && messages.length === 0) return null;
    return { services, messages };
  } catch {
    return null;
  }
}

interface ProtobufServicesPreviewProps {
  content: string;
}

export function ProtobufServicesPreview({ content }: ProtobufServicesPreviewProps) {
  const { t } = useTranslation();
  const parsed = parseProto(content);

  if (!parsed) {
    return (
      <div className="flex flex-col items-center justify-center h-32 text-muted" data-testid="preview-empty">
        <span className="text-xs">{t('contractBuilder.preview.empty')}</span>
      </div>
    );
  }

  return (
    <div className="space-y-3" data-testid="protobuf-services-preview">
      {parsed.services.length > 0 && (
        <div>
          <div className="text-xs font-semibold text-muted uppercase tracking-wider mb-2">
            Services ({parsed.services.length})
          </div>
          {parsed.services.map((svc) => (
            <div key={svc.name} className="mb-2">
              <div className="text-xs font-mono font-semibold text-heading">{svc.name}</div>
              <div className="pl-3 space-y-0.5 mt-1">
                {svc.rpcs.map((rpc) => (
                  <div key={rpc} className="flex items-center gap-1.5">
                    <Badge variant="neutral" className="text-[10px]">rpc</Badge>
                    <span className="text-xs font-mono text-muted">{rpc}</span>
                  </div>
                ))}
              </div>
            </div>
          ))}
        </div>
      )}
      {parsed.messages.length > 0 && (
        <div>
          <div className="text-xs font-semibold text-muted uppercase tracking-wider mb-2">
            Messages ({parsed.messages.length})
          </div>
          <div className="flex flex-wrap gap-1.5">
            {parsed.messages.map((msg) => (
              <Badge key={msg} variant="neutral" className="text-[10px] font-mono">{msg}</Badge>
            ))}
          </div>
        </div>
      )}
    </div>
  );
}
```

- [ ] **Step 3: Create SoapOperationsPreview**

```tsx
// src/frontend/src/features/contracts/studio/components/previews/SoapOperationsPreview.tsx
import { useTranslation } from 'react-i18next';
import { Badge } from '../../../../../components/Badge';

function parseOperations(xml: string): string[] | null {
  try {
    const ops: string[] = [];
    const regex = /<(?:wsdl:)?operation\s+name="([^"]+)"/g;
    let m;
    while ((m = regex.exec(xml)) !== null) {
      ops.push(m[1]);
    }
    const unique = [...new Set(ops)];
    return unique.length > 0 ? unique : null;
  } catch {
    return null;
  }
}

interface SoapOperationsPreviewProps {
  content: string;
}

export function SoapOperationsPreview({ content }: SoapOperationsPreviewProps) {
  const { t } = useTranslation();
  const operations = parseOperations(content);

  if (!operations) {
    return (
      <div className="flex flex-col items-center justify-center h-32 text-muted" data-testid="preview-empty">
        <span className="text-xs">{t('contractBuilder.preview.empty')}</span>
      </div>
    );
  }

  return (
    <div className="space-y-1.5" data-testid="soap-operations-preview">
      <div className="text-xs font-semibold text-muted uppercase tracking-wider mb-2">
        Operations ({operations.length})
      </div>
      {operations.map((op) => (
        <div key={op} className="flex items-center gap-2">
          <Badge variant="warning" className="text-[10px] flex-shrink-0">op</Badge>
          <span className="text-xs font-mono text-heading">{op}</span>
        </div>
      ))}
    </div>
  );
}
```

- [ ] **Step 4: Commit**

```bash
git add src/frontend/src/features/contracts/studio/components/previews/
git commit -m "feat(studio): add GraphQL, Protobuf and SOAP preview components"
```

---

## Task 6: Rewrite RestOpenApiBuilderPage

**Files:**
- Modify: `src/frontend/src/features/contracts/pages/RestOpenApiBuilderPage.tsx`

- [ ] **Step 1: Rewrite the file**

```tsx
// src/frontend/src/features/contracts/pages/RestOpenApiBuilderPage.tsx
import { useTranslation } from 'react-i18next';
import { ContractBuilderLayout } from '../studio/ContractBuilderLayout';
import { RestOperationsPreview } from '../studio/components/previews/RestOperationsPreview';

const REST_TEMPLATE = `openapi: 3.1.0
info:
  title: My REST API
  version: 1.0.0
  description: API description
servers:
  - url: https://api.example.com/v1
paths:
  /resources:
    get:
      summary: List resources
      operationId: listResources
      responses:
        '200':
          description: Success
          content:
            application/json:
              schema:
                type: array
                items:
                  $ref: '#/components/schemas/Resource'
    post:
      summary: Create resource
      operationId: createResource
      requestBody:
        required: true
        content:
          application/json:
            schema:
              $ref: '#/components/schemas/ResourceInput'
      responses:
        '201':
          description: Created
components:
  schemas:
    Resource:
      type: object
      properties:
        id:
          type: string
        name:
          type: string
    ResourceInput:
      type: object
      required: [name]
      properties:
        name:
          type: string
`;

export function RestOpenApiBuilderPage() {
  const { t } = useTranslation();

  return (
    <ContractBuilderLayout
      contractName={t('restBuilder.title')}
      protocol="OpenAPI 3.1"
      language="yaml"
      initialContent={REST_TEMPLATE}
      renderPreview={(content) => <RestOperationsPreview content={content} />}
    />
  );
}
```

- [ ] **Step 2: Commit**

```bash
git add src/frontend/src/features/contracts/pages/RestOpenApiBuilderPage.tsx
git commit -m "feat(studio): rewrite RestOpenApiBuilderPage with Code+Visual split"
```

---

## Task 7: Rewrite AsyncApiBuilderPage

**Files:**
- Modify: `src/frontend/src/features/contracts/pages/AsyncApiBuilderPage.tsx`

- [ ] **Step 1: Rewrite the file**

```tsx
// src/frontend/src/features/contracts/pages/AsyncApiBuilderPage.tsx
import { useTranslation } from 'react-i18next';
import { ContractBuilderLayout } from '../studio/ContractBuilderLayout';
import { AsyncApiChannelsPreview } from '../studio/components/previews/AsyncApiChannelsPreview';

const ASYNCAPI_TEMPLATE = `asyncapi: 3.0.0
info:
  title: My Event-Driven API
  version: 1.0.0
channels:
  userRegistered:
    address: user.registered
    bindings:
      kafka: {}
    messages:
      userRegisteredMessage:
        payload:
          type: object
          properties:
            userId:
              type: string
            email:
              type: string
  orderCreated:
    address: order.created
    bindings:
      kafka: {}
    messages:
      orderCreatedMessage:
        payload:
          type: object
          properties:
            orderId:
              type: string
            total:
              type: number
operations:
  onUserRegistered:
    action: receive
    channel:
      $ref: '#/channels/userRegistered'
  onOrderCreated:
    action: receive
    channel:
      $ref: '#/channels/orderCreated'
`;

export function AsyncApiBuilderPage() {
  const { t } = useTranslation();

  return (
    <ContractBuilderLayout
      contractName={t('asyncApiBuilder.title')}
      protocol="AsyncAPI 3.x"
      language="yaml"
      initialContent={ASYNCAPI_TEMPLATE}
      renderPreview={(content) => <AsyncApiChannelsPreview content={content} />}
    />
  );
}
```

- [ ] **Step 2: Commit**

```bash
git add src/frontend/src/features/contracts/pages/AsyncApiBuilderPage.tsx
git commit -m "feat(studio): rewrite AsyncApiBuilderPage with Code+Visual split"
```

---

## Task 8: Rewrite GraphQL, Protobuf and SOAP builders

**Files:**
- Modify: `src/frontend/src/features/contracts/pages/GraphQLBuilderPage.tsx`
- Modify: `src/frontend/src/features/contracts/pages/ProtobufBuilderPage.tsx`
- Modify: `src/frontend/src/features/contracts/pages/SoapWsdlBuilderPage.tsx`

- [ ] **Step 1: Rewrite GraphQLBuilderPage**

```tsx
// src/frontend/src/features/contracts/pages/GraphQLBuilderPage.tsx
import { useTranslation } from 'react-i18next';
import { ContractBuilderLayout } from '../studio/ContractBuilderLayout';
import { GraphQlTypesPreview } from '../studio/components/previews/GraphQlTypesPreview';

const GRAPHQL_TEMPLATE = `type Query {
  user(id: ID!): User
  users(page: Int, pageSize: Int): UserConnection!
}

type Mutation {
  createUser(input: CreateUserInput!): User!
  updateUser(id: ID!, input: UpdateUserInput!): User!
  deleteUser(id: ID!): Boolean!
}

type User {
  id: ID!
  name: String!
  email: String!
  createdAt: String!
}

type UserConnection {
  items: [User!]!
  totalCount: Int!
}

input CreateUserInput {
  name: String!
  email: String!
}

input UpdateUserInput {
  name: String
  email: String
}
`;

export function GraphQLBuilderPage() {
  const { t } = useTranslation();

  return (
    <ContractBuilderLayout
      contractName={t('graphqlBuilder.title')}
      protocol="GraphQL SDL"
      language="graphql"
      initialContent={GRAPHQL_TEMPLATE}
      renderPreview={(content) => <GraphQlTypesPreview content={content} />}
    />
  );
}
```

- [ ] **Step 2: Rewrite ProtobufBuilderPage**

```tsx
// src/frontend/src/features/contracts/pages/ProtobufBuilderPage.tsx
import { useTranslation } from 'react-i18next';
import { ContractBuilderLayout } from '../studio/ContractBuilderLayout';
import { ProtobufServicesPreview } from '../studio/components/previews/ProtobufServicesPreview';

const PROTO_TEMPLATE = `syntax = "proto3";

package myservice.v1;

option go_package = "github.com/example/myservice/v1";

// UserService manages user accounts.
service UserService {
  rpc GetUser (GetUserRequest) returns (User);
  rpc ListUsers (ListUsersRequest) returns (ListUsersResponse);
  rpc CreateUser (CreateUserRequest) returns (User);
  rpc DeleteUser (DeleteUserRequest) returns (DeleteUserResponse);
}

message User {
  string id = 1;
  string name = 2;
  string email = 3;
  string created_at = 4;
}

message GetUserRequest {
  string id = 1;
}

message ListUsersRequest {
  int32 page = 1;
  int32 page_size = 2;
}

message ListUsersResponse {
  repeated User users = 1;
  int32 total_count = 2;
}

message CreateUserRequest {
  string name = 1;
  string email = 2;
}

message DeleteUserRequest {
  string id = 1;
}

message DeleteUserResponse {
  bool success = 1;
}
`;

export function ProtobufBuilderPage() {
  const { t } = useTranslation();

  return (
    <ContractBuilderLayout
      contractName={t('protobufBuilder.title')}
      protocol=".proto / gRPC"
      language="proto"
      initialContent={PROTO_TEMPLATE}
      renderPreview={(content) => <ProtobufServicesPreview content={content} />}
    />
  );
}
```

- [ ] **Step 3: Rewrite SoapWsdlBuilderPage**

```tsx
// src/frontend/src/features/contracts/pages/SoapWsdlBuilderPage.tsx
import { useTranslation } from 'react-i18next';
import { ContractBuilderLayout } from '../studio/ContractBuilderLayout';
import { SoapOperationsPreview } from '../studio/components/previews/SoapOperationsPreview';

const WSDL_TEMPLATE = `<?xml version="1.0" encoding="UTF-8"?>
<definitions xmlns="http://schemas.xmlsoap.org/wsdl/"
             xmlns:soap="http://schemas.xmlsoap.org/wsdl/soap/"
             xmlns:tns="http://example.com/userservice"
             xmlns:xsd="http://www.w3.org/2001/XMLSchema"
             targetNamespace="http://example.com/userservice"
             name="UserService">

  <types>
    <xsd:schema targetNamespace="http://example.com/userservice">
      <xsd:element name="GetUserRequest">
        <xsd:complexType>
          <xsd:sequence>
            <xsd:element name="userId" type="xsd:string"/>
          </xsd:sequence>
        </xsd:complexType>
      </xsd:element>
      <xsd:element name="GetUserResponse">
        <xsd:complexType>
          <xsd:sequence>
            <xsd:element name="id" type="xsd:string"/>
            <xsd:element name="name" type="xsd:string"/>
            <xsd:element name="email" type="xsd:string"/>
          </xsd:sequence>
        </xsd:complexType>
      </xsd:element>
    </xsd:schema>
  </types>

  <message name="GetUserRequest">
    <part name="parameters" element="tns:GetUserRequest"/>
  </message>
  <message name="GetUserResponse">
    <part name="parameters" element="tns:GetUserResponse"/>
  </message>

  <portType name="UserServicePortType">
    <operation name="GetUser">
      <input message="tns:GetUserRequest"/>
      <output message="tns:GetUserResponse"/>
    </operation>
    <operation name="ListUsers">
      <input message="tns:GetUserRequest"/>
      <output message="tns:GetUserResponse"/>
    </operation>
  </portType>

  <binding name="UserServiceBinding" type="tns:UserServicePortType">
    <soap:binding style="document" transport="http://schemas.xmlsoap.org/soap/http"/>
    <operation name="GetUser">
      <soap:operation soapAction="GetUser"/>
      <input><soap:body use="literal"/></input>
      <output><soap:body use="literal"/></output>
    </operation>
    <operation name="ListUsers">
      <soap:operation soapAction="ListUsers"/>
      <input><soap:body use="literal"/></input>
      <output><soap:body use="literal"/></output>
    </operation>
  </binding>

  <service name="UserService">
    <port name="UserServicePort" binding="tns:UserServiceBinding">
      <soap:address location="http://example.com/userservice"/>
    </port>
  </service>
</definitions>
`;

export function SoapWsdlBuilderPage() {
  const { t } = useTranslation();

  return (
    <ContractBuilderLayout
      contractName={t('soapBuilder.title')}
      protocol="WSDL 1.1"
      language="xml"
      initialContent={WSDL_TEMPLATE}
      renderPreview={(content) => <SoapOperationsPreview content={content} />}
    />
  );
}
```

- [ ] **Step 4: Commit**

```bash
git add src/frontend/src/features/contracts/pages/GraphQLBuilderPage.tsx \
        src/frontend/src/features/contracts/pages/ProtobufBuilderPage.tsx \
        src/frontend/src/features/contracts/pages/SoapWsdlBuilderPage.tsx
git commit -m "feat(studio): rewrite GraphQL, Protobuf and SOAP builders with Code+Visual split"
```

---

## Task 9: ContractStudioPage Hub redesign + test

**Files:**
- Modify: `src/frontend/src/features/contracts/pages/ContractStudioPage.tsx`
- Create: `src/frontend/src/__tests__/contracts/ContractStudioPage.test.tsx`

- [ ] **Step 1: Write the failing tests**

```tsx
// src/frontend/src/__tests__/contracts/ContractStudioPage.test.tsx
import * as React from 'react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';

vi.mock('react-i18next', () => ({
  useTranslation: () => ({ t: (key: string) => key }),
  Trans: ({ children }: { children: React.ReactNode }) => <>{children}</>,
}));

vi.mock('../../../features/contracts/hooks', () => ({
  useContractsSummary: vi.fn(),
  useContractList: vi.fn(),
}));

import { useContractsSummary, useContractList } from '../../../features/contracts/hooks';
import { ContractStudioPage } from '../../../features/contracts/pages/ContractStudioPage';

function wrap(node: React.ReactNode) {
  const qc = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClient>
      <QueryClientProvider client={qc}>
        <MemoryRouter>{node}</MemoryRouter>
      </QueryClientProvider>
    </QueryClient>,
  );
}

describe('ContractStudioPage', () => {
  beforeEach(() => {
    vi.mocked(useContractsSummary).mockReturnValue({
      data: { totalCount: 32, approvedCount: 18, draftCount: 5 },
      isLoading: false,
      isError: false,
    } as ReturnType<typeof useContractsSummary>);

    vi.mocked(useContractList).mockReturnValue({
      data: {
        items: [
          {
            contractVersionId: 'v-1',
            apiName: 'Payments API v2',
            protocol: 'OpenApi',
            lifecycleState: 'Draft',
            updatedAt: new Date().toISOString(),
          },
        ],
        totalCount: 1,
      },
      isLoading: false,
      isError: false,
    } as ReturnType<typeof useContractList>);
  });

  it('renders three stat cards', () => {
    wrap(<ContractStudioPage />);
    expect(screen.getByTestId('stat-total')).toBeInTheDocument();
    expect(screen.getByTestId('stat-published')).toBeInTheDocument();
    expect(screen.getByTestId('stat-draft')).toBeInTheDocument();
  });

  it('shows stat values from summary', () => {
    wrap(<ContractStudioPage />);
    expect(screen.getByText('32')).toBeInTheDocument();
    expect(screen.getByText('18')).toBeInTheDocument();
    expect(screen.getByText('5')).toBeInTheDocument();
  });

  it('shows in-progress draft card with name', () => {
    wrap(<ContractStudioPage />);
    expect(screen.getByText('Payments API v2')).toBeInTheDocument();
  });

  it('renders type picker with REST/OpenAPI card linking to /contracts/studio/rest', () => {
    wrap(<ContractStudioPage />);
    const link = screen.getByTestId('type-card-rest-openapi');
    expect(link).toBeInTheDocument();
  });
});
```

- [ ] **Step 2: Run — expect FAIL**

```bash
cd src/frontend && npm run test -- "ContractStudioPage.test"
```

- [ ] **Step 3: Rewrite ContractStudioPage**

```tsx
// src/frontend/src/features/contracts/pages/ContractStudioPage.tsx
import { useRef } from 'react';
import { useTranslation } from 'react-i18next';
import { Link, useNavigate } from 'react-router-dom';
import {
  Code2, Zap, Globe, Hash, FileCode2, GitMerge, ArrowRight, ChevronRight,
} from 'lucide-react';
import { PageContainer, PageSection } from '../../../components/shell';
import { PageHeader } from '../../../components/PageHeader';
import { Badge } from '../../../components/Badge';
import { Button } from '../../../components/Button';
import { useContractsSummary, useContractList } from '../hooks';
import type { ContractListItem } from '../types';

// ── Protocol → builder route ──────────────────────────────────────────────────

const PROTOCOL_ROUTE: Record<string, string> = {
  OpenApi: '/contracts/studio/rest',
  Swagger: '/contracts/studio/rest',
  Wsdl: '/contracts/studio/soap',
  AsyncApi: '/contracts/studio/async',
  Protobuf: '/contracts/studio/protobuf',
  GraphQl: '/contracts/studio/graphql',
};

const PROTOCOL_LABEL: Record<string, string> = {
  OpenApi: 'OpenAPI',
  Swagger: 'Swagger',
  Wsdl: 'WSDL',
  AsyncApi: 'AsyncAPI',
  Protobuf: 'Protobuf',
  GraphQl: 'GraphQL',
  WorkerService: 'Worker',
};

// ── Type registry ─────────────────────────────────────────────────────────────

interface ContractType {
  key: string;
  label: string;
  protocol: string;
  icon: React.ReactNode;
  accentClass: string;
  route: string;
  bestForKey: string;
}

const CONTRACT_TYPES: ContractType[] = [
  {
    key: 'rest-openapi',
    label: 'REST / OpenAPI',
    protocol: 'OpenAPI 3.1',
    icon: <Globe size={16} />,
    accentClass: 'text-accent',
    route: '/contracts/studio/rest',
    bestForKey: 'contractStudio.type.bestFor.rest',
  },
  {
    key: 'soap-wsdl',
    label: 'SOAP / WSDL',
    protocol: 'WSDL 1.1 / 2.0',
    icon: <Code2 size={16} />,
    accentClass: 'text-warning',
    route: '/contracts/studio/soap',
    bestForKey: 'contractStudio.type.bestFor.soap',
  },
  {
    key: 'graphql',
    label: 'GraphQL',
    protocol: 'SDL',
    icon: <Hash size={16} />,
    accentClass: 'text-info',
    route: '/contracts/studio/graphql',
    bestForKey: 'contractStudio.type.bestFor.graphql',
  },
  {
    key: 'asyncapi',
    label: 'AsyncAPI 3.x',
    protocol: 'AsyncAPI 3.x',
    icon: <Zap size={16} />,
    accentClass: 'text-success',
    route: '/contracts/studio/async',
    bestForKey: 'contractStudio.type.bestFor.asyncapi',
  },
  {
    key: 'protobuf',
    label: 'Protobuf / gRPC',
    protocol: '.proto',
    icon: <FileCode2 size={16} />,
    accentClass: 'text-accent',
    route: '/contracts/studio/protobuf',
    bestForKey: 'contractStudio.type.bestFor.protobuf',
  },
  {
    key: 'shared-schema',
    label: 'Shared Schema',
    protocol: 'Multi-format',
    icon: <GitMerge size={16} />,
    accentClass: 'text-muted',
    route: '/contracts/studio/shared-schema',
    bestForKey: 'contractStudio.type.bestFor.sharedSchema',
  },
];

const CATEGORIES = [
  {
    labelKey: 'contractStudio.newContract.categories.rest',
    keys: ['rest-openapi', 'soap-wsdl', 'graphql'],
  },
  {
    labelKey: 'contractStudio.newContract.categories.events',
    keys: ['asyncapi', 'protobuf'],
  },
  {
    labelKey: 'contractStudio.newContract.categories.shared',
    keys: ['shared-schema'],
  },
];

// ── Helpers ───────────────────────────────────────────────────────────────────

function timeAgo(isoDate: string | undefined): string {
  if (!isoDate) return '';
  const diff = Date.now() - new Date(isoDate).getTime();
  const mins = Math.floor(diff / 60_000);
  if (mins < 60) return `${mins}m ago`;
  const hrs = Math.floor(mins / 60);
  if (hrs < 24) return `${hrs}h ago`;
  return `${Math.floor(hrs / 24)}d ago`;
}

// ── Sub-components ────────────────────────────────────────────────────────────

function StatCard({
  value,
  labelKey,
  testId,
}: {
  value: number | undefined;
  labelKey: string;
  testId: string;
}) {
  const { t } = useTranslation();
  return (
    <div
      className="flex flex-col gap-1 rounded-md border border-edge bg-card px-4 py-3"
      data-testid={testId}
    >
      <span className="text-2xl font-bold text-heading tabular-nums">
        {value !== undefined ? value : '—'}
      </span>
      <span className="text-xs text-muted">{t(labelKey)}</span>
    </div>
  );
}

function DraftCard({ item }: { item: ContractListItem }) {
  const id = item.contractVersionId ?? item.versionId ?? item.id;
  const name = item.apiName ?? item.name ?? '—';
  const protocol = PROTOCOL_LABEL[item.protocol] ?? item.protocol;
  const route = id ? `/contracts/workspace/${id}` : '#';

  return (
    <div className="flex-shrink-0 w-52 rounded-md border border-edge bg-card p-3 flex flex-col gap-2">
      <div className="flex items-center gap-2">
        <Badge variant="neutral" className="text-[10px] font-mono">{protocol}</Badge>
        <Badge variant="warning" className="text-[10px]">Draft</Badge>
      </div>
      <span className="text-sm font-medium text-heading truncate" title={name}>{name}</span>
      {item.updatedAt && (
        <span className="text-xs text-faded">{timeAgo(item.updatedAt)}</span>
      )}
      <Link
        to={route}
        className="text-xs font-medium text-accent hover:underline flex items-center gap-1 mt-auto"
      >
        Resume <ArrowRight size={11} />
      </Link>
    </div>
  );
}

function TypeCard({ type }: { type: ContractType }) {
  const { t } = useTranslation();
  const navigate = useNavigate();
  return (
    <button
      type="button"
      data-testid={`type-card-${type.key}`}
      onClick={() => navigate(type.route)}
      className="group w-full text-left rounded-md border border-edge bg-card hover:bg-elevated hover:border-edge-strong transition-all duration-150 p-3 focus:outline-none focus:ring-1 focus:ring-accent"
    >
      <div className="flex items-center gap-2 mb-2">
        <span className={type.accentClass}>{type.icon}</span>
        <span className="text-sm font-semibold text-heading">{type.label}</span>
        <Badge variant="neutral" className="text-[10px] font-mono ml-auto">{type.protocol}</Badge>
      </div>
      <p className="text-xs text-faded leading-relaxed">{t(type.bestForKey)}</p>
      <div className="flex items-center gap-1 text-xs font-medium text-accent opacity-0 group-hover:opacity-100 transition-opacity mt-2">
        Open builder <ChevronRight size={11} />
      </div>
    </button>
  );
}

// ── Page ──────────────────────────────────────────────────────────────────────

export function ContractStudioPage() {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const newContractRef = useRef<HTMLDivElement>(null);

  const summaryQuery = useContractsSummary();
  const draftsQuery = useContractList({ lifecycleState: 'Draft', pageSize: 10 });

  const summary = summaryQuery.data;
  const drafts = draftsQuery.data?.items ?? [];

  const scrollToNew = () =>
    newContractRef.current?.scrollIntoView({ behavior: 'smooth' });

  return (
    <PageContainer>
      <PageHeader
        title={t('contractStudio.title')}
        subtitle={t('contractStudio.subtitle')}
        actions={
          <Button size="sm" onClick={scrollToNew}>
            + {t('contractStudio.newContract.title')}
          </Button>
        }
      />

      <PageSection>
        {/* Stats */}
        <div className="grid grid-cols-3 gap-3 mb-6">
          <StatCard value={summary?.totalCount} labelKey="contractStudio.stats.total" testId="stat-total" />
          <StatCard value={summary?.approvedCount} labelKey="contractStudio.stats.published" testId="stat-published" />
          <StatCard value={summary?.draftCount} labelKey="contractStudio.stats.inDraft" testId="stat-draft" />
        </div>

        {/* In Progress */}
        <div className="mb-6">
          <div className="flex items-center justify-between mb-3">
            <h2 className="text-xs font-semibold text-muted uppercase tracking-wider">
              {t('contractStudio.inProgress.title')}
              {drafts.length > 0 && (
                <span className="ml-2 text-faded normal-case font-normal">({drafts.length})</span>
              )}
            </h2>
            <Link to="/contracts/catalog" className="text-xs text-accent hover:underline flex items-center gap-1">
              {t('contractStudio.inProgress.viewAll')} <ArrowRight size={11} />
            </Link>
          </div>

          {drafts.length === 0 ? (
            <p className="text-xs text-faded py-4">{t('contractStudio.inProgress.empty')}</p>
          ) : (
            <div className="flex gap-3 overflow-x-auto pb-2">
              {drafts.map((item) => (
                <DraftCard key={item.contractVersionId ?? item.id ?? item.apiAssetId} item={item} />
              ))}
            </div>
          )}
        </div>

        {/* New Contract */}
        <div ref={newContractRef}>
          <h2 className="text-xs font-semibold text-muted uppercase tracking-wider mb-4">
            {t('contractStudio.newContract.title')}
          </h2>

          {CATEGORIES.map((cat) => {
            const types = CONTRACT_TYPES.filter((t) => cat.keys.includes(t.key));
            return (
              <div key={cat.labelKey} className="mb-5">
                <h3 className="text-xs text-faded mb-2">{t(cat.labelKey)}</h3>
                <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-2">
                  {types.map((type) => (
                    <TypeCard key={type.key} type={type} />
                  ))}
                </div>
              </div>
            );
          })}
        </div>
      </PageSection>
    </PageContainer>
  );
}
```

- [ ] **Step 4: Run tests — expect PASS**

```bash
cd src/frontend && npm run test -- "ContractStudioPage.test"
```

- [ ] **Step 5: Commit**

```bash
git add src/frontend/src/features/contracts/pages/ContractStudioPage.tsx \
        src/frontend/src/__tests__/contracts/ContractStudioPage.test.tsx
git commit -m "feat(studio): redesign ContractStudioPage as APIM-style Hub"
```

---

## Task 10: i18n locale keys

**Files:**
- Modify: `src/frontend/src/locales/en.json`
- Modify: `src/frontend/src/locales/pt-BR.json`
- Modify: `src/frontend/src/locales/es.json`
- Modify: `src/frontend/src/locales/pt-PT.json`

Each locale already has a `"contractStudio"` object. **Add** the new nested keys into it (do not remove existing keys). Also add a new top-level `"contractBuilder"` object.

- [ ] **Step 1: Add keys to en.json**

Find the `"contractStudio"` object in `en.json` and add the following keys inside it:

```json
"stats": {
  "total": "Contracts",
  "published": "Published",
  "inDraft": "In Draft"
},
"inProgress": {
  "title": "In Progress",
  "resume": "Resume",
  "viewAll": "View all",
  "empty": "No contracts in draft"
},
"newContract": {
  "title": "New Contract",
  "categories": {
    "rest": "REST & HTTP",
    "events": "Event-Driven",
    "shared": "Shared / Cross-cutting"
  }
},
"type": {
  "bestFor": {
    "rest": "Public APIs, microservices, webhooks",
    "soap": "Enterprise integrations, mainframe services",
    "graphql": "Flexible queries, API aggregation",
    "asyncapi": "Kafka, SNS, AMQP, WebSocket events",
    "protobuf": "gRPC, IoT, high-performance RPC",
    "sharedSchema": "Shared schemas, cross-contract references"
  }
}
```

Add a new top-level key `"contractBuilder"` (at the same level as `"contractStudio"`):

```json
"contractBuilder": {
  "header": {
    "format": "Format",
    "saveDraft": "Save Draft",
    "publish": "Publish"
  },
  "validation": {
    "valid": "Valid",
    "errors": "Parse error"
  },
  "preview": {
    "parseError": "Parse error",
    "empty": "No preview — start writing to see a live preview"
  }
}
```

- [ ] **Step 2: Add keys to pt-BR.json**

Same structure, Portuguese (Brazil) translations:

`contractStudio` additions:
```json
"stats": { "total": "Contratos", "published": "Publicados", "inDraft": "Em Rascunho" },
"inProgress": { "title": "Em Progresso", "resume": "Continuar", "viewAll": "Ver todos", "empty": "Nenhum contrato em rascunho" },
"newContract": { "title": "Novo Contrato", "categories": { "rest": "REST & HTTP", "events": "Event-Driven", "shared": "Compartilhado" } },
"type": { "bestFor": { "rest": "APIs públicas, microsserviços", "soap": "Integrações enterprise, mainframe", "graphql": "Queries flexíveis, agregação", "asyncapi": "Kafka, SNS, AMQP, WebSocket", "protobuf": "gRPC, IoT, RPC de alto desempenho", "sharedSchema": "Schemas compartilhados, refs cross-contract" } }
```

`contractBuilder` (new top-level):
```json
"contractBuilder": {
  "header": { "format": "Formatar", "saveDraft": "Salvar Rascunho", "publish": "Publicar" },
  "validation": { "valid": "Válido", "errors": "Erro de parse" },
  "preview": { "parseError": "Erro de parse", "empty": "Nenhum preview — escreva para ver o preview ao vivo" }
}
```

- [ ] **Step 3: Add keys to es.json**

Same structure, Spanish translations:

`contractStudio` additions:
```json
"stats": { "total": "Contratos", "published": "Publicados", "inDraft": "En Borrador" },
"inProgress": { "title": "En Progreso", "resume": "Continuar", "viewAll": "Ver todos", "empty": "No hay contratos en borrador" },
"newContract": { "title": "Nuevo Contrato", "categories": { "rest": "REST & HTTP", "events": "Orientado a Eventos", "shared": "Compartido" } },
"type": { "bestFor": { "rest": "APIs públicas, microservicios", "soap": "Integraciones enterprise, mainframe", "graphql": "Consultas flexibles, agregación", "asyncapi": "Kafka, SNS, AMQP, WebSocket", "protobuf": "gRPC, IoT, RPC de alto rendimiento", "sharedSchema": "Esquemas compartidos, refs cross-contract" } }
```

`contractBuilder` (new top-level):
```json
"contractBuilder": {
  "header": { "format": "Formatear", "saveDraft": "Guardar Borrador", "publish": "Publicar" },
  "validation": { "valid": "Válido", "errors": "Error de parse" },
  "preview": { "parseError": "Error de parse", "empty": "Sin preview — empiece a escribir para ver un preview en vivo" }
}
```

- [ ] **Step 4: Add keys to pt-PT.json**

Same structure, Portuguese (Portugal) translations:

`contractStudio` additions:
```json
"stats": { "total": "Contratos", "published": "Publicados", "inDraft": "Em Rascunho" },
"inProgress": { "title": "Em Progresso", "resume": "Continuar", "viewAll": "Ver todos", "empty": "Nenhum contrato em rascunho" },
"newContract": { "title": "Novo Contrato", "categories": { "rest": "REST & HTTP", "events": "Event-Driven", "shared": "Partilhado" } },
"type": { "bestFor": { "rest": "APIs públicas, microsserviços", "soap": "Integrações enterprise, mainframe", "graphql": "Queries flexíveis, agregação", "asyncapi": "Kafka, SNS, AMQP, WebSocket", "protobuf": "gRPC, IoT, RPC de alto desempenho", "sharedSchema": "Schemas partilhados, refs cross-contract" } }
```

`contractBuilder` (new top-level):
```json
"contractBuilder": {
  "header": { "format": "Formatar", "saveDraft": "Guardar Rascunho", "publish": "Publicar" },
  "validation": { "valid": "Válido", "errors": "Erro de parse" },
  "preview": { "parseError": "Erro de parse", "empty": "Sem preview — escreva para ver o preview em direto" }
}
```

- [ ] **Step 5: Verify TypeScript build passes**

```bash
cd src/frontend && npm run build 2>&1 | tail -20
```

Expected: build success, no TypeScript errors.

- [ ] **Step 6: Run full test suite**

```bash
cd src/frontend && npm run test 2>&1 | tail -30
```

Expected: all tests pass (or same pass count as before this feature).

- [ ] **Step 7: Commit**

```bash
git add src/frontend/src/locales/
git commit -m "feat(i18n): add contractStudio Hub and contractBuilder locale keys"
```

---

## Self-Review Checklist

- [x] **Spec coverage:** Hub (stats + in-progress + type picker) ✓ | ContractBuilderLayout ✓ | 5 preview components ✓ | 5 builder rewrites ✓ | i18n ✓
- [x] **No placeholders:** All steps contain complete code
- [x] **Type consistency:** `BuilderValidationStatus` defined in `BuilderHeader.tsx` and imported by `ContractBuilderLayout.tsx`; `ContractBuilderLayoutProps` used consistently across all builders; `PROTOCOL_ROUTE` map consistent with existing `ContractProtocol` values
- [x] **SimpleSplitPane reuse:** Plan uses existing `SimpleSplitPane` — no duplicate component created
- [x] **MonacoEditorWrapper reuse:** Plan uses existing wrapper — no direct `@monaco-editor/react` import
- [x] **Hook reuse:** `useContractsSummary` and `useContractList` used as-is — no new hooks created
- [x] **Draft Resume route:** `/contracts/workspace/${id}` — uses existing `ContractWorkspacePage`
