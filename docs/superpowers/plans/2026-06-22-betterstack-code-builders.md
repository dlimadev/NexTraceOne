# Sub-projeto C — Code Builders (Monaco) v5 — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Alinhar os 5 editores de contrato baseados em Monaco ao idioma visual Betterstack v5, replicando a moldura do `DraftStudioPage`, sem tocar na lógica do editor.

**Architecture:** Reescrever a shell `ContractBuilderLayout` para o padrão `PageContainer` + back-link + `<PageHeader>` + split-pane do editor num caixote limitado (Abordagem B). O componente `BuilderHeader` é absorvido pelo `PageHeader` e removido. As 5 páginas wrapper não mudam. Corrigem-se dois defeitos de polish (chave i18n literal e token `destructive` inexistente).

**Tech Stack:** React 19, TypeScript 5.9, react-i18next, Tailwind v4 (tokens `@theme`), Vitest + Testing Library, Monaco (intocado).

## Global Constraints

- `TreatWarningsAsErrors` equivalente no frontend: `npm run build` e `npm run lint` têm de ficar **0 erros**.
- UI sem strings hardcoded — usar chaves i18n. As chaves necessárias (`contractBuilder.*`) **já existem** nos 4 locales (`en`, `es`, `pt-BR`, `pt-PT`, ficheiros FLAT em `src/frontend/src/locales/<l>.json`). **Não** adicionar locales neste plano.
- Preservar os `data-testid`: `contract-builder-layout`, `parse-error-banner`, `btn-format`, `btn-save`, `btn-publish`.
- Token v5 para erro é `critical` (`--color-critical`). **Não existe** `--color-destructive` no `@theme`.
- Variante 4a — **só visual**: nenhuma lógica de persistência nova; `onSave`/`onPublish` permanecem props condicionais (não são ligados pelas páginas).
- Mudança cirúrgica (lição do ciclo 9): no commit, `git add` **apenas** dos paths explícitos abaixo. Verificar `git diff --name-only main...HEAD` no fim.

---

### Task 1: Reescrever `ContractBuilderLayout` para a moldura v5 e remover `BuilderHeader`

**Files:**
- Modify: `src/frontend/src/features/contracts/studio/ContractBuilderLayout.tsx` (reescrita completa)
- Delete: `src/frontend/src/features/contracts/studio/components/BuilderHeader.tsx`
- Test: `src/frontend/src/__tests__/contracts/ContractBuilderLayout.test.tsx` (adicionar 1 caso)

**Interfaces:**
- Consumes:
  - `PageContainer` de `../../../components/shell` — `({ children, className?, fluid?, compact? })`.
  - `PageHeader` de `../../../components/PageHeader` — `({ title, subtitle?, badge?, actions?, ... })`.
  - `Badge` de `../../../components/Badge` — `variant` aceita `'success' | 'danger'` (canónicas).
  - `Button` de `../../../components/Button` — `variant?: 'primary'|'secondary'|'ghost'|...`, `size?: 'xs'|'sm'|'md'|'lg'`; encaminha `data-testid` via `...rest`.
  - `MonacoEditorWrapper`, `SimpleSplitPane` — inalterados.
- Produces: `ContractBuilderLayout` mantém **a mesma API pública** (`ContractBuilderLayoutProps`: `contractName`, `protocol`, `language`, `initialContent`, `renderPreview`, `onSave?`, `onPublish?`). Os tipos `BuilderLanguage` e `BuilderValidationStatus` passam a ser exportados deste ficheiro (antes `BuilderValidationStatus` vinha do `BuilderHeader`). As 5 páginas wrapper não precisam de alterações.

- [ ] **Step 1: Adicionar o caso de teste do enquadramento v5 (falha)**

Adicionar este `it(...)` ao bloco `describe('ContractBuilderLayout', ...)` em `src/frontend/src/__tests__/contracts/ContractBuilderLayout.test.tsx` (a seguir ao caso "renders without crashing"):

```tsx
  it('renders v5 framing with a back-link to studio and the contract name as heading', () => {
    const { container } = wrap(
      <ContractBuilderLayout
        contractName="Payments API"
        protocol="OpenAPI 3.1"
        language="yaml"
        initialContent="openapi: 3.1.0"
        renderPreview={() => null}
      />,
    );
    expect(container.querySelector('a[href="/contracts/studio"]')).toBeInTheDocument();
    expect(screen.getByRole('heading', { name: 'Payments API' })).toBeInTheDocument();
  });
```

- [ ] **Step 2: Correr o teste e confirmar que falha**

Run: `cd src/frontend && npx vitest run src/__tests__/contracts/ContractBuilderLayout.test.tsx -t "v5 framing"`
Expected: FAIL — não existe `a[href="/contracts/studio"]` nem `<h1>Payments API</h1>` (o layout atual usa breadcrumb com texto "Studio" e nome via `BuilderHeader`, sem `<h1>`).

- [ ] **Step 3: Reescrever `ContractBuilderLayout.tsx` por completo**

Substituir todo o conteúdo de `src/frontend/src/features/contracts/studio/ContractBuilderLayout.tsx` por:

```tsx
import { useState, useEffect, useCallback } from 'react';
import { Link } from 'react-router-dom';
import { ChevronLeft } from 'lucide-react';
import { useTranslation } from 'react-i18next';
import * as yaml from 'js-yaml';
import SimpleSplitPane from '../../../components/SimpleSplitPane';
import { MonacoEditorWrapper } from '../workspace/editor/MonacoEditorWrapper';
import { PageContainer } from '../../../components/shell';
import { PageHeader } from '../../../components/PageHeader';
import { Badge } from '../../../components/Badge';
import { Button } from '../../../components/Button';

export type BuilderLanguage = 'yaml' | 'json' | 'graphql' | 'proto' | 'xml';
export type BuilderValidationStatus = 'idle' | 'valid' | 'errors';

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
  const { t } = useTranslation();
  const [content, setContent] = useState(initialContent);
  const [debouncedContent, setDebouncedContent] = useState(initialContent);
  const [lastValidContent, setLastValidContent] = useState(initialContent);
  const [validationStatus, setValidationStatus] = useState<BuilderValidationStatus>('idle');
  const [errorLine, setErrorLine] = useState<number | undefined>(undefined);

  useEffect(() => {
    const timer = setTimeout(() => setDebouncedContent(content), 400);
    return () => clearTimeout(timer);
  }, [content]);

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

  const chipLabel =
    validationStatus === 'valid'
      ? t('contractBuilder.validation.valid')
      : validationStatus === 'errors'
        ? t('contractBuilder.validation.errors', { line: errorLine ?? '' })
        : null;
  const chipVariant: 'success' | 'danger' = validationStatus === 'valid' ? 'success' : 'danger';

  return (
    <PageContainer className="animate-fade-in">
      <div data-testid="contract-builder-layout">
        <div className="mb-4">
          <Link
            to="/contracts/studio"
            className="inline-flex items-center gap-1.5 text-sm text-muted hover:text-heading transition-colors"
          >
            <ChevronLeft size={14} /> {t('contracts.studio.backToStudio', 'Studio')}
          </Link>
        </div>

        <PageHeader
          title={contractName}
          subtitle={protocol}
          badge={
            chipLabel ? (
              <Badge variant={chipVariant} className="text-[10px]">{chipLabel}</Badge>
            ) : undefined
          }
          actions={
            <>
              <Button size="sm" variant="ghost" onClick={handleFormat} data-testid="btn-format">
                {t('contractBuilder.header.format')}
              </Button>
              {onSave && (
                <Button
                  size="sm"
                  variant="secondary"
                  onClick={() => onSave(content)}
                  data-testid="btn-save"
                >
                  {t('contractBuilder.header.saveDraft')}
                </Button>
              )}
              {onPublish && (
                <Button size="sm" onClick={() => onPublish(content)} data-testid="btn-publish">
                  {t('contractBuilder.header.publish')}
                </Button>
              )}
            </>
          }
        />

        <div className="h-[60vh] min-h-[420px] border border-edge rounded-lg overflow-hidden">
          <SimpleSplitPane
            className="h-full"
            initialLeftPercent={45}
            minLeftPercent={25}
            minRightPercent={25}
            left={
              <MonacoEditorWrapper value={content} language={monacoLanguage} onChange={setContent} />
            }
            right={
              <div className="h-full overflow-auto p-4 bg-elevated">
                {validationStatus === 'errors' && (
                  <div
                    className="mb-3 px-3 py-2 rounded text-xs text-critical bg-critical/10 border border-critical/25"
                    data-testid="parse-error-banner"
                  >
                    {t('contractBuilder.preview.parseError')}
                    {errorLine ? ` (line ${errorLine})` : ''}
                  </div>
                )}
                {renderPreview(lastValidContent)}
              </div>
            }
          />
        </div>
      </div>
    </PageContainer>
  );
}
```

Notas de mudança (face ao ficheiro atual):
- Removido o import e uso de `BuilderHeader`/`getContent`; o tipo `BuilderValidationStatus` passa a ser declarado e exportado aqui.
- Moldura nova: `PageContainer` + back-link `ChevronLeft` → `/contracts/studio` + `<PageHeader>` (título=`contractName`, subtítulo=`protocol`, `badge`=chip de validação, `actions`=Format/Save/Publish com os mesmos `data-testid`).
- Editor agora num caixote `h-[60vh] min-h-[420px] border border-edge rounded-lg overflow-hidden` (antes `flex-1` full-height).
- **Fix i18n:** banner usa `t('contractBuilder.preview.parseError')` (antes a string literal `contractBuilder.preview.parseError`).
- **Fix token:** banner usa `text-critical bg-critical/10 border-critical/25` (antes `text-destructive bg-destructive/10 border-destructive/20`, sem token correspondente no `@theme`).

- [ ] **Step 4: Remover o `BuilderHeader.tsx` órfão**

Run: `git rm src/frontend/src/features/contracts/studio/components/BuilderHeader.tsx`
(Confirmado previamente: só o `ContractBuilderLayout` importava `BuilderHeader`/`BuilderValidationStatus`.)

- [ ] **Step 5: Correr o teste do layout e confirmar que passa (incl. os 4 casos pré-existentes)**

Run: `cd src/frontend && npx vitest run src/__tests__/contracts/ContractBuilderLayout.test.tsx`
Expected: PASS — 5 casos verdes (render, **v5 framing** novo, parse-error banner após debounce, onSave com conteúdo atual, ausência de Save sem handler).

- [ ] **Step 6: Verificar lint + build + suíte total**

Run: `cd src/frontend && npm run lint && npm run build && npm run test`
Expected: lint 0 erros; build OK; suíte total verde (não deve haver regressões — nenhuma das 5 páginas builder mudou de API).

Se algum teste de outra página falhar por referência a `BuilderHeader`/textid antigo: confirmar que era pré-existente e não introduzido (não deve existir — grep do plano confirmou que nada mais importava `BuilderHeader`).

- [ ] **Step 7: Verificar higiene do diff (lição do ciclo 9)**

Run: `git status --porcelain && git diff --name-only main...HEAD`
Expected: apenas estes paths modificados/criados/removidos —
`src/frontend/src/features/contracts/studio/ContractBuilderLayout.tsx` (M),
`src/frontend/src/features/contracts/studio/components/BuilderHeader.tsx` (D),
`src/frontend/src/__tests__/contracts/ContractBuilderLayout.test.tsx` (M),
e o spec/plano de docs já commitados. **Nenhum** ficheiro não relacionado.

- [ ] **Step 8: Commit (paths explícitos)**

```bash
git add src/frontend/src/features/contracts/studio/ContractBuilderLayout.tsx \
        src/frontend/src/features/contracts/studio/components/BuilderHeader.tsx \
        src/frontend/src/__tests__/contracts/ContractBuilderLayout.test.tsx
git commit -m "feat(contracts): Code Builders v5 (moldura DraftStudio + fix i18n/token)

Reescreve ContractBuilderLayout para PageContainer + PageHeader + editor
em caixote (Abordagem B); absorve e remove BuilderHeader; corrige chave
i18n parseError literal e token destructive inexistente -> critical.

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

### Task 2: Smoke visual dos 5 builders (checkpoint manual)

**Files:** nenhum (verificação). Só editar se o smoke revelar uma cor crua/token legado numa das 5 páginas (à partida estão limpas).

- [ ] **Step 1: Arrancar o dev server**

Run: `cd src/frontend && npm run dev`

- [ ] **Step 2: Visitar cada rota e confirmar o idioma v5**

Abrir e inspecionar visualmente:
`/contracts/studio/rest`, `/contracts/studio/async`, `/contracts/studio/graphql`, `/contracts/studio/protobuf`, `/contracts/studio/soap`.

Critérios por página:
- Back-link "Studio" com chevron → topo esquerdo.
- `<PageHeader>`: título (nome do contrato) + subtítulo (protocolo) + chip de validação ao lado do título (muda para "Válido"/erro ao editar).
- Botão "Formatar" no header (YAML/JSON formatam; GraphQL/Protobuf/SOAP/XML não — comportamento inalterado).
- Editor Monaco + preview dentro do caixote com borda arredondada, altura ~60vh.
- Introduzir YAML inválido (numa página YAML) → banner de parse-error **traduzido** (não a chave literal) com cor `critical`.

- [ ] **Step 3: Registar resultado**

Sem código se tudo OK. Se algo falhar visualmente, abrir uma correção cirúrgica e repetir Task 1 Steps 6–8 para o ficheiro afetado.

---

## Self-Review

**Spec coverage:**
- Moldura Abordagem B (PageContainer+PageHeader+caixote) → Task 1 Step 3. ✅
- Variante 4a (Save/Publish condicionais, não ligados) → Task 1 Step 3 (props mantidas condicionais; páginas não passam handlers). ✅
- Remoção do `BuilderHeader` + relocação de `BuilderValidationStatus` → Task 1 Steps 3–4. ✅
- Fix i18n literal `parseError` → Task 1 Step 3 (e Global Constraints: chaves já existem). ✅
- Fix token `destructive`→`critical` → Task 1 Step 3. ✅
- 5 páginas wrapper inalteradas + sweep → Task 2. ✅
- Preservar `data-testid` + suíte verde → Task 1 Steps 5–6. ✅
- Higiene de diff (ciclo 9) → Task 1 Step 7. ✅
- Fora de escopo (Monaco/preview/templates/persistência/sub-projeto D) → respeitado (não tocados). ✅

**Placeholder scan:** sem TBD/TODO; todo o código presente verbatim. ✅

**Type consistency:** `ContractBuilderLayoutProps` inalterado; `BuilderValidationStatus`/`BuilderLanguage` exportados do mesmo ficheiro; `chipVariant` usa `'success' | 'danger'` (variantes válidas do `Badge`); `Button` `variant`/`size`/`data-testid` conferem com `Button.tsx`. ✅
