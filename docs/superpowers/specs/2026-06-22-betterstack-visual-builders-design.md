# Betterstack — Sub-projeto D: Visual Builders — Design

**Data:** 2026-06-22
**Branch:** `redesign/betterstack-visual-builders`
**Memória de contexto:** `project_betterstack_redesign` (ciclos 9–12), `project_contract_studio_redesign`
**Precede:** sub-projetos A (Contract Workspace, ciclo 10), B (Draft Studio, ciclo 11), C (Code Builders, ciclo 12)

## Contexto

Último sub-projeto da decomposição dos editores de contrato. Abrange os 8 *visual builders*
(`src/frontend/src/features/contracts/workspace/builders/Visual*Builder.tsx`, ~5k linhas) e os
seus editores/primitives partilhados (`builders/shared/`). São montados via barrel
`builders/index.ts` e embebidos no `ContractSection`, que por sua vez vive dentro do Contract
Workspace já redesenhado (ciclo A) e do Draft Studio (ciclo B).

### Achado de auditoria (reformula o que D precisa de ser)

Ao contrário dos módulos antigos, os visual builders **já estão v5-token-clean**:
- Zero cores cruas / zero tokens legados em todo o diretório `builders/` (construídos em 2026-05).
- Já usam DS `Card`/`CardHeader` e tokens semânticos (`bg-elevated`, `border-edge`, `ring-accent`,
  `text-muted`, `text-danger`, `text-heading`).
- São embebidos → **não há shell/moldura para adicionar** (isso é trabalho dos ciclos A/B).
- Os "controlos crus" (`<button>`/`<input>`) são, na maioria, afordâncias inline intencionais e
  densas (ícones de remover em hover, pílulas "Add", inputs minúsculos). Uma migração cega para
  componentes DS (`Button`/`TextField`) **pioraria** a densidade e arriscaria partir os editores —
  é a lição recorrente da memória ("trocas cegas quebrariam os editores").

Decisão do utilizador (via brainstorming): **polish leve e cirúrgico, alcance C refinado** — não
há sweep de tokens nem framing; o trabalho é remover duplicação presentacional comprovada e
normalizar duas divergências reais, sem mexer no que já está consistente.

## Duplicação/inconsistência comprovada (a base do trabalho)

1. **Pílula "Add X" duplicada verbatim ×14** nos 8 builders — className idêntica:
   `inline-flex items-center gap-1 px-2 py-1 text-[10px] font-medium rounded-md bg-accent/10 text-accent hover:bg-accent/20 transition-colors`.
2. **Ícone de remover (`Trash2`) duplicado** nos 8 builders — `text-muted hover:text-danger transition-colors`
   (algumas instâncias com `e.stopPropagation()` no onClick e/ou `opacity-0 group-hover:opacity-100`).
3. **Add-buttons divergentes nos 2 editores partilhados:** `SchemaPropertyEditor` usa um botão
   *outline* (`text-[9px] text-accent border border-accent/30`, sem fundo); `SchemaCompositionEditor`
   usa `Plus size={9}`. Visualmente diferentes da pílula cheia dos builders.

### O que NÃO é inconsistência (e por isso fica intocado)
- Os ~30 títulos de secção `<h3 className="text-xs font-semibold text-heading">` já estão **uniformes**
  em todos os builders. Harmonizá-los seria uma mudança visual sem ganho de consistência → fora de escopo.
- O primitive `BuilderSubSection` (em `BuilderFormPrimitives.tsx`) **não é usado por nenhum builder**
  (aparente código morto). Apenas registado como observação; **não removido** neste sub-projeto
  (decisão separada).

## Mudanças de design

### 1. Dois primitives presentacionais novos em `builders/shared/BuilderFormPrimitives.tsx`

- **`AddButton`** — a pílula canónica "Add X".
  - Props: `label: string`, `onClick: () => void`, `disabled?: boolean`, `iconSize?: number`
    (default `10`), `className?: string` (passthrough para casos específicos).
  - Renderiza `<button type="button">` com `<Plus size={iconSize} />` + `label`, usando a className
    canónica da pílula cheia (item 1 acima). Mantém `disabled` → `opacity`/`cursor` coerentes com os
    outros primitives.
- **`RemoveIconButton`** — a afordância de remover linha.
  - Props: `onClick: (e?) => void`, `title?: string`, `disabled?: boolean`, `iconSize?: number`
    (default `12`), `className?: string` (passthrough — **obrigatório** para preservar variações
    existentes como `opacity-0 group-hover:opacity-100`).
  - Renderiza `<button type="button">` com `<Trash2 size={iconSize} />` e base
    `text-muted hover:text-danger transition-colors`; a className passada é concatenada.
  - O `onClick` recebe o evento para que os call sites que fazem `e.stopPropagation()` continuem a
    funcionar inalterados.

### 2. Substituir call sites

- **8 builders** (`VisualRestBuilder`, `VisualSoapBuilder`, `VisualEventBuilder`,
  `VisualWorkserviceBuilder`, `VisualSharedSchemaBuilder`, `VisualWebhookBuilder`,
  `VisualLegacyContractBuilder`, `VisualDataContractBuilder`): a pílula "Add" → `AddButton`; os
  botões Trash2 de remover linha → `RemoveIconButton` (com `className` passthrough onde havia
  `group-hover`/`opacity` e `e.stopPropagation()` preservado).
- **2 editores partilhados** (`SchemaPropertyEditor`, `SchemaCompositionEditor`): os add-buttons
  divergentes → `AddButton`. **Normalização visual deliberada**: o botão outline e o `size=9`
  passam a usar a pílula cheia canónica.

### 3. Fora de escopo (explícito)
- `Field*` (input/area/select/checkbox/tag) primitives — ficam como estão (densos, propositados,
  já token-clean).
- Títulos `<h3>` de secção (já consistentes); `BuilderSubSection` órfão (só observação).
- Botões de mover linha (setas ↑↓) em `SchemaPropertyEditor`, toggles/chevrons de expansão, o `X`
  de remover tag dentro de `FieldTagInput` — afordâncias distintas, não duplicadas no mesmo padrão.
- Monaco, `builderSync`/`builderValidation`/`builderParse`, `ContractSection`, lógica de estado.

## Critérios de sucesso

1. Zero ocorrências da className-pílula "Add" duplicada fora de `AddButton`; um único sítio para o
   estilo add/remove.
2. Comportamento idêntico: cada `<button>` renderizado mantém texto+ícone e os handlers (incl.
   `stopPropagation` e reveal por `group-hover`) → testes existentes por role/texto continuam verdes.
3. As 2 divergências dos editores partilhados ficam visualmente alinhadas à pílula canónica.
4. `npm run lint` + `npm run build` 0 erros; suíte total verde.
5. `git diff --name-only` contém apenas ficheiros sob `workspace/builders/` (+ o spec/plano de docs).
   Nada não relacionado (lição do ciclo 9: `git add` só de paths explícitos).

## Riscos

- **`RemoveIconButton` e variações por call site:** alguns removes têm `group-hover`/`opacity` e
  `stopPropagation`. Mitigação: `className` passthrough + `onClick(e)` com evento. Verificar cada
  call site convertido individualmente.
- **Normalização visual dos editores partilhados (item 3):** muda ligeiramente a aparência de 2
  botões (outline→pílula cheia). É intencional e aprovado, mas confirmar no smoke visual.
- **Volume:** ~10 ficheiros tocados; mudanças mecânicas mas numerosas → executar por ficheiro/lote
  com verificação, não num só commit gigante.
