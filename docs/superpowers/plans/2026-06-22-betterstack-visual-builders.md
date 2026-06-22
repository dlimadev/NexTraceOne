# Sub-projeto D — Visual Builders v5 polish — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Eliminar a duplicação presentacional comprovada nos 8 visual builders extraindo dois primitives partilhados (`AddButton`, `RemoveIconButton`) e substituindo as call sites, sem alterar comportamento, densidade ou os afordâncias color-coded.

**Architecture:** Adicionar `AddButton` (pílula accent canónica) e `RemoveIconButton` (Trash2 muted→danger com `className` passthrough) a `builders/shared/BuilderFormPrimitives.tsx`, ao lado dos `Field*` existentes. Substituir nos 8 builders apenas as pílulas "Add" **accent idênticas** e os botões de remover linha. Tudo o resto (seletores color-coded dos editores partilhados, taxonomia de cores de tipos, pílula purple "Add Schema", títulos `<h3>`, Monaco) fica intocado.

**Tech Stack:** React 19, TypeScript 5.9, Tailwind v4 (tokens `@theme`), lucide-react, Vitest + Testing Library.

## Global Constraints

- `npm run lint` + `npm run build` = 0 erros; suíte total verde.
- **Comportamento idêntico:** cada `<button>` convertido mantém texto+ícone e o handler `onClick` exato (incluindo `e.stopPropagation()` onde existe) e o reveal `group-hover`/`opacity`. Densidade e layout inalterados.
- `AddButton` (accent) substitui **apenas** botões cuja className é a pílula accent
  `inline-flex items-center gap-1 px-2 py-1 text-[10px] font-medium rounded-md bg-accent/10 text-accent hover:bg-accent/20 transition-colors`
  (inclui a variante com o typo `bg-accent\10` em LegacyContractBuilder, que assim fica corrigida).
- **NÃO tocar:** pílula purple "Add Schema" (`VisualRestBuilder`, `bg-purple-500/10 text-purple-400`); botão muted "Add common error responses" (`text-muted ... border-edge/50`, `Plus size={9}`) e os quick-add de response codes; add-buttons color-coded dos editores partilhados (`SchemaPropertyEditor`, `SchemaCompositionEditor`); taxonomia `TYPE_COLORS` (purple/pink/orange); títulos `<h3>`; setas mover ↑↓; `X` de remover tag; Monaco/`builderSync`/`builderValidation`/`ContractSection`.
- Mudança cirúrgica (lição do ciclo 9): no commit, `git add` **apenas** dos paths explícitos. Verificar `git diff --name-only main...HEAD` no fim.
- **Nota de cobertura:** não existe teste que renderize os `Visual*Builder` e clique nos botões. A rede de segurança destas tarefas é: os testes unitários novos dos primitives (Task 1) + `tsc`/`build`/`lint` + smoke visual manual (Task 4). Os `*BuilderValidation.test.ts` testam funções puras, não os botões.

---

### Task 1: Criar `AddButton` + `RemoveIconButton` em `BuilderFormPrimitives.tsx`

**Files:**
- Modify: `src/frontend/src/features/contracts/workspace/builders/shared/BuilderFormPrimitives.tsx`
- Test: `src/frontend/src/__tests__/contracts/BuilderFormPrimitives.test.tsx` (criar)

**Interfaces:**
- Produces:
  - `AddButton(props: { label: string; onClick: () => void; disabled?: boolean; iconSize?: number; className?: string })` — renderiza `<button type="button">` com `<Plus size={iconSize ?? 10}/> {label}` e a className-pílula accent canónica; concatena `className`.
  - `RemoveIconButton(props: { onClick: (e: React.MouseEvent<HTMLButtonElement>) => void; title?: string; disabled?: boolean; iconSize?: number; className?: string })` — renderiza `<button type="button">` com `<Trash2 size={iconSize ?? 12}/>`, base `text-muted hover:text-danger`, concatena `className`.

- [ ] **Step 1: Escrever os testes (falham)**

Criar `src/frontend/src/__tests__/contracts/BuilderFormPrimitives.test.tsx`:

```tsx
import { describe, it, expect, vi } from 'vitest';
import { render, screen, fireEvent } from '@testing-library/react';

vi.mock('react-i18next', () => ({
  useTranslation: () => ({ t: (k: string) => k }),
}));

import {
  AddButton,
  RemoveIconButton,
} from '../../features/contracts/workspace/builders/shared/BuilderFormPrimitives';

describe('AddButton', () => {
  it('renders the label and fires onClick', () => {
    const onClick = vi.fn();
    render(<AddButton label="Add Endpoint" onClick={onClick} />);
    fireEvent.click(screen.getByRole('button', { name: /Add Endpoint/ }));
    expect(onClick).toHaveBeenCalledTimes(1);
  });

  it('does not fire onClick when disabled', () => {
    const onClick = vi.fn();
    render(<AddButton label="Add" onClick={onClick} disabled />);
    fireEvent.click(screen.getByRole('button'));
    expect(onClick).not.toHaveBeenCalled();
  });
});

describe('RemoveIconButton', () => {
  it('fires onClick with the event (so call sites can stopPropagation)', () => {
    const onClick = vi.fn();
    render(<RemoveIconButton onClick={onClick} title="Remove" />);
    fireEvent.click(screen.getByRole('button', { name: 'Remove' }));
    expect(onClick).toHaveBeenCalledTimes(1);
    expect(onClick.mock.calls[0][0]).toBeTruthy();
  });

  it('keeps the muted base and applies passthrough className', () => {
    render(<RemoveIconButton onClick={() => {}} className="opacity-0 group-hover:opacity-100" />);
    const btn = screen.getByRole('button');
    expect(btn.className).toContain('text-muted');
    expect(btn.className).toContain('group-hover:opacity-100');
  });
});
```

- [ ] **Step 2: Correr e confirmar falha**

Run: `cd src/frontend && npx vitest run src/__tests__/contracts/BuilderFormPrimitives.test.tsx`
Expected: FAIL — `AddButton`/`RemoveIconButton` não são exportados de `BuilderFormPrimitives`.

- [ ] **Step 3: Implementar os primitives**

Em `src/frontend/src/features/contracts/workspace/builders/shared/BuilderFormPrimitives.tsx`:

(a) Atualizar o import de ícones (linha 8, atualmente `import { X } from 'lucide-react';`) para incluir `Plus` e `Trash2`:

```tsx
import { X, Plus, Trash2 } from 'lucide-react';
```

(b) Acrescentar, no fim do ficheiro (a seguir a `BuilderSubSection`):

```tsx
const ADD_BUTTON_CLASS =
  'inline-flex items-center gap-1 px-2 py-1 text-[10px] font-medium rounded-md bg-accent/10 text-accent hover:bg-accent/20 transition-colors disabled:opacity-50 disabled:cursor-not-allowed';

/** Pílula "Add X" canónica partilhada pelos visual builders. */
export function AddButton({
  label,
  onClick,
  disabled,
  iconSize = 10,
  className,
}: {
  label: string;
  onClick: () => void;
  disabled?: boolean;
  iconSize?: number;
  className?: string;
}) {
  return (
    <button
      type="button"
      onClick={onClick}
      disabled={disabled}
      className={`${ADD_BUTTON_CLASS} ${className ?? ''}`}
    >
      <Plus size={iconSize} /> {label}
    </button>
  );
}

/** Afordância de remover linha (Trash2). `className` passthrough preserva reveal/spacing por call site. */
export function RemoveIconButton({
  onClick,
  title,
  disabled,
  iconSize = 12,
  className,
}: {
  onClick: (e: React.MouseEvent<HTMLButtonElement>) => void;
  title?: string;
  disabled?: boolean;
  iconSize?: number;
  className?: string;
}) {
  return (
    <button
      type="button"
      onClick={onClick}
      disabled={disabled}
      title={title}
      className={`text-muted hover:text-danger ${className ?? ''}`}
    >
      <Trash2 size={iconSize} />
    </button>
  );
}
```

- [ ] **Step 4: Correr e confirmar passagem**

Run: `cd src/frontend && npx vitest run src/__tests__/contracts/BuilderFormPrimitives.test.tsx`
Expected: PASS — 4 casos verdes.

- [ ] **Step 5: Lint + commit**

Run: `cd src/frontend && npm run lint`
Expected: 0 erros.

```bash
git add src/frontend/src/features/contracts/workspace/builders/shared/BuilderFormPrimitives.tsx \
        src/frontend/src/__tests__/contracts/BuilderFormPrimitives.test.tsx
git commit -m "feat(contracts): AddButton + RemoveIconButton primitives partilhados

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

### Task 2: Migrar os 6 builders simples para os primitives

**Files (Modify):**
- `src/frontend/src/features/contracts/workspace/builders/VisualSoapBuilder.tsx`
- `.../VisualEventBuilder.tsx`
- `.../VisualWebhookBuilder.tsx`
- `.../VisualSharedSchemaBuilder.tsx`
- `.../VisualLegacyContractBuilder.tsx`
- `.../VisualDataContractBuilder.tsx`

**Interfaces:**
- Consumes: `AddButton`, `RemoveIconButton` de `./shared/BuilderFormPrimitives` (Task 1).

**Recipe de transformação** (aplicar em cada ficheiro):

**(R1) Import:** adicionar `import { AddButton, RemoveIconButton } from './shared/BuilderFormPrimitives';` junto aos imports existentes.

**(R2) Pílula "Add":** substituir o botão accent. Exemplo (VisualSoapBuilder, `addOperation`):

```tsx
// ANTES
<button type="button" onClick={addOperation} className="inline-flex items-center gap-1 px-2 py-1 text-[10px] font-medium rounded-md bg-accent/10 text-accent hover:bg-accent/20 transition-colors">
  <Plus size={10} /> {t('contracts.builder.soap.addOperation', 'Add Operation')}
</button>

// DEPOIS
<AddButton label={t('contracts.builder.soap.addOperation', 'Add Operation')} onClick={addOperation} />
```

**(R3) Remover linha (variante reveal):** estes 6 builders usam todos a mesma variante reveal, size 12, com `stopPropagation`. Exemplo (VisualSoapBuilder, `removeOp`):

```tsx
// ANTES
<button type="button" onClick={(e) => { e.stopPropagation(); removeOp(op.id); }} className="opacity-0 group-hover:opacity-100 text-muted hover:text-danger transition-all">
  <Trash2 size={12} />
</button>

// DEPOIS
<RemoveIconButton onClick={(e) => { e.stopPropagation(); removeOp(op.id); }} className="opacity-0 group-hover:opacity-100 transition-all" />
```

(Nota: `text-muted hover:text-danger` saem da className do call site porque já estão na base do primitive; tudo o resto — `opacity-0 group-hover:opacity-100 transition-all` — fica no `className` passthrough. `size={12}` é o default, por isso `iconSize` é omitido.)

**(R4) Limpeza de imports:** após converter, se `Plus` e/ou `Trash2` deixarem de ser usados no ficheiro, removê-los do import de `lucide-react`. **Verificar com grep no ficheiro antes de remover** (alguns ficheiros podem usar `Plus`/`Trash2` noutro sítio).

**Checklist por ficheiro** (handler → label da Add; handler do Remove):

| Ficheiro | Add (handler / label key) | Remove (handler) |
|---|---|---|
| VisualSoapBuilder | `addOperation` / `contracts.builder.soap.addOperation` | `removeOp` |
| VisualEventBuilder | `addChannel` / `contracts.builder.event.addChannel` | `removeChannel` |
| VisualWebhookBuilder | `addHeader` / `contracts.builder.webhook.addHeader` | `removeHeader` |
| VisualSharedSchemaBuilder | `addProperty` / `contracts.builder.sharedSchema.addProperty` | `removeProp` |
| VisualLegacyContractBuilder | `addField` / `contracts.builder.legacy.addField` (⚠️ className tem typo `bg-accent\10` — a conversão corrige-o) | `removeField` |
| VisualDataContractBuilder | `addColumn` / `contracts.builder.dataContract.addColumn` | `removeColumn` |

Todos os Remove destes 6 = variante reveal `opacity-0 group-hover:opacity-100 transition-all`, size 12, `e.stopPropagation()`. Manter o guard `{!isReadOnly && (...)}` à volta de cada botão.

- [ ] **Step 1: Aplicar R1–R4 nos 6 ficheiros**

Seguir a checklist. Preservar `onClick` exato e o guard `isReadOnly`.

- [ ] **Step 2: Verificar tipos/build + lint**

Run: `cd src/frontend && npm run lint && npm run build`
Expected: 0 erros (sem imports não usados; sem JSX inválido).

- [ ] **Step 3: Correr a suíte de contratos**

Run: `cd src/frontend && npx vitest run src/__tests__/contracts`
Expected: PASS — incluindo os `*BuilderValidation` e o novo `BuilderFormPrimitives.test.tsx`.

- [ ] **Step 4: Higiene do diff + commit**

Run: `git status --porcelain && git diff --name-only main...HEAD`
Expected: apenas os 6 ficheiros desta task + os de Task 1 + docs. Nada não relacionado.

```bash
git add src/frontend/src/features/contracts/workspace/builders/VisualSoapBuilder.tsx \
        src/frontend/src/features/contracts/workspace/builders/VisualEventBuilder.tsx \
        src/frontend/src/features/contracts/workspace/builders/VisualWebhookBuilder.tsx \
        src/frontend/src/features/contracts/workspace/builders/VisualSharedSchemaBuilder.tsx \
        src/frontend/src/features/contracts/workspace/builders/VisualLegacyContractBuilder.tsx \
        src/frontend/src/features/contracts/workspace/builders/VisualDataContractBuilder.tsx
git commit -m "refactor(contracts): 6 visual builders usam AddButton/RemoveIconButton

Corrige tambem o typo bg-accent\\10 no LegacyContractBuilder.

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

### Task 3: Migrar os 2 builders pesados (Rest + Workservice)

**Files (Modify):**
- `src/frontend/src/features/contracts/workspace/builders/VisualWorkserviceBuilder.tsx`
- `src/frontend/src/features/contracts/workspace/builders/VisualRestBuilder.tsx`

**Interfaces:**
- Consumes: `AddButton`, `RemoveIconButton` de `./shared/BuilderFormPrimitives` (Task 1).

**(A) VisualWorkserviceBuilder** — 5 Adds accent + 5 Removes.
- Adds (todos a pílula accent idêntica): `addDependency` (`contracts.builder.workservice.addDep`), `addConsumedTopic` (`...addTopic`), `addProducedTopic` (`...addTopic`), `addConsumedService` (`...addService`), `addProducedEvent` (`...addEvent`). → `<AddButton label={t(...)} onClick={HANDLER} />`.
- Removes (todos `text-muted hover:text-danger transition-colors pb-1`, `Trash2 size={11}`, **sem** stopPropagation): `removeDep`, `removeConsumedTopic`, `removeProducedTopic`, `removeConsumedSvc`, `removeProducedEvt`. Converter cada:

```tsx
// ANTES
<button type="button" onClick={() => removeDep(dep.id)} className="text-muted hover:text-danger transition-colors pb-1">
  <Trash2 size={11} />
</button>
// DEPOIS
<RemoveIconButton onClick={() => removeDep(dep.id)} iconSize={11} className="transition-colors pb-1" />
```

- Após converter, `Plus` e `Trash2` deixam de ser usados → removê-los do import `lucide-react` (verificar com grep no ficheiro).

**(B) VisualRestBuilder** — 1 Add accent + vários Removes. ⚠️ Tem afordâncias a EXCLUIR.
- **Converter (Add):** apenas `addEndpoint` (className = pílula accent idêntica) → `<AddButton label={t('contracts.builder.rest.addEndpoint', 'Add Endpoint')} onClick={addEndpoint} />`.
- **NÃO converter (Adds):** `addSchema` (pílula **purple** `bg-purple-500/10 text-purple-400`); "Add common error responses" (`text-muted ... border-edge/50`, `Plus size={9}`); quaisquer quick-add de response codes (botões muted/coloridos, não a pílula accent). Critério: só converter quando a className for **exatamente** a pílula accent.
- **Converter (Removes):** todos os `<button>` com `text-muted hover:text-danger` + `<Trash2 .../>`. Conhecidos:
  - schema delete: `onClick={(e) => { e.stopPropagation(); update({ schemas: ... }); }}`, className `text-muted hover:text-danger transition-colors opacity-0 group-hover:opacity-100`, size 12 → `<RemoveIconButton onClick={...} className="transition-colors opacity-0 group-hover:opacity-100" />`.
  - `removeEndpoint`: `onClick={(e) => { e.stopPropagation(); removeEndpoint(ep.id); }}`, className `text-muted hover:text-danger transition-colors`, size 12 → `<RemoveIconButton onClick={...} className="transition-colors" />`.
  - param remove: `onClick={() => updateEndpoint(ep.id, { parameters: ep.parameters.filter((_, j) => j !== pi) })}`, className `text-muted hover:text-danger transition-colors`, size 11 → `<RemoveIconButton onClick={...} iconSize={11} className="transition-colors" />`.
  - **Procurar exaustivamente** com `grep -n "Trash2" VisualRestBuilder.tsx` e converter cada botão Trash2 que use `text-muted hover:text-danger`, aplicando a mesma regra (residual className − `text-muted hover:text-danger`; `iconSize` = o size original se ≠ 12; `onClick` exato com stopPropagation se presente).
- **Imports:** `Trash2` deixa de ser usado (todos convertidos) → remover; **`Plus` permanece** (usado nos botões purple/muted não convertidos) → manter. Verificar com grep antes de mexer no import.

- [ ] **Step 1: Migrar VisualWorkserviceBuilder (A)**

Aplicar (A): import dos primitives, 5 Adds, 5 Removes, limpeza de imports.

- [ ] **Step 2: Migrar VisualRestBuilder (B)**

Aplicar (B): import dos primitives, `addEndpoint`, todos os Removes Trash2 (grep exaustivo), **excluindo** purple/muted adds; manter `Plus`, remover `Trash2`.

- [ ] **Step 3: Verificar tipos/build + lint**

Run: `cd src/frontend && npm run lint && npm run build`
Expected: 0 erros.

- [ ] **Step 4: Confirmar que as exclusões ficaram intactas**

Run: `cd src/frontend && grep -n "bg-purple-500/10\|border-edge/50" src/features/contracts/workspace/builders/VisualRestBuilder.tsx`
Expected: a pílula purple "Add Schema" e o botão muted "Add common error responses" ainda presentes (não convertidos).

- [ ] **Step 5: Suíte de contratos**

Run: `cd src/frontend && npx vitest run src/__tests__/contracts`
Expected: PASS.

- [ ] **Step 6: Higiene do diff + commit**

Run: `git diff --name-only main...HEAD`
Expected: apenas estes 2 ficheiros + os de Tasks 1–2 + docs.

```bash
git add src/frontend/src/features/contracts/workspace/builders/VisualWorkserviceBuilder.tsx \
        src/frontend/src/features/contracts/workspace/builders/VisualRestBuilder.tsx
git commit -m "refactor(contracts): Rest + Workservice builders usam AddButton/RemoveIconButton

Exclui a pilula purple Add Schema e o botao muted Add common error responses.

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

### Task 4: Smoke visual + suíte total (checkpoint manual)

**Files:** nenhum (verificação). Só editar se o smoke revelar regressão.

- [ ] **Step 1: Suíte total**

Run: `cd src/frontend && npm run test`
Expected: suíte total verde.

- [ ] **Step 2: Smoke visual**

Run: `cd src/frontend && npm run dev`. Abrir um contrato no workspace/draft studio e, para cada tipo (REST, Soap, Event, Workservice, SharedSchema, Webhook, Legacy, DataContract), confirmar visualmente:
- As pílulas "Add X" mantêm o aspeto accent (fundo `bg-accent/10`, texto accent).
- Os ícones de remover aparecem em hover (reveal) onde antes apareciam; nos do Workservice aparecem sempre (sem reveal), alinhados.
- A pílula "Add Schema" (REST) continua **purple**; "Add common error responses" continua **muted**.
- Os botões color-coded de tipo no editor de propriedades (string=accent/object=purple/$ref=pink) **inalterados**.
- O "Add Field" do Legacy agora tem fundo accent (o typo `bg-accent\10` estava a deixá-lo sem fundo).

- [ ] **Step 3: Registar resultado**

Sem código se tudo OK. Se algo regredir, abrir correção cirúrgica no ficheiro afetado e repetir a verificação.

---

## Self-Review

**Spec coverage:**
- AddButton + RemoveIconButton primitives → Task 1. ✅
- Substituir pílulas accent + Trash2 nos 8 builders → Tasks 2–3. ✅
- Excluir purple "Add Schema", muted "Add common error responses", seletores color-coded, taxonomia de cores → Global Constraints + Task 3 (B) + Step 4. ✅
- Comportamento idêntico (onClick/stopPropagation/reveal) → recipe R3/(A)/(B) com passthrough. ✅
- Fix do typo `bg-accent\10` (Legacy) → Task 2 checklist. ✅
- Higiene de diff (ciclo 9) → Tasks 2–3 Step de diff. ✅
- Fora de escopo (Field*, h3, Monaco, BuilderSubSection órfão) → não tocados. ✅

**Placeholder scan:** sem TBD/TODO; código completo nos primitives; recipe + checklist exaustivos para a parte mecânica. ✅

**Type consistency:** `AddButton`/`RemoveIconButton` definidos na Task 1 com as assinaturas usadas nas Tasks 2–3; `RemoveIconButton.onClick` recebe `React.MouseEvent` (suporta `stopPropagation`); `iconSize` default 12 (Add 10). ✅
