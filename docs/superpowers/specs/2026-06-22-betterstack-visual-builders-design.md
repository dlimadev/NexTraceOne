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

Ao contrário dos módulos antigos, os visual builders estão **largamente v5-token-clean**:
- Já usam DS `Card`/`CardHeader` e tokens semânticos (`bg-elevated`, `border-edge`, `ring-accent`,
  `text-muted`, `text-danger`, `text-heading`).
- Exceção intencional: existe uma **taxonomia de cores cruas para tipos de schema** (`object`=purple,
  `$ref`=pink, `oneOf/anyOf/allOf`=orange — mapa `TYPE_COLORS` em `SchemaPropertyEditor`), usada para
  color-coding de tipos JSON-schema. É deliberada e **fica fora de escopo** (converter a tokens seria
  outra discussão, não polish).
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
2. **Ícone de remover (`Trash2`) duplicado** nos 8 builders — base `text-muted hover:text-danger`
   (variantes por call site: `transition-colors`/`transition-all`, `opacity-0 group-hover:opacity-100`,
   `pb-1`, size 11/12, com/sem `e.stopPropagation()` no onClick).

### O que NÃO é inconsistência (e por isso fica intocado)
- **Add-buttons dos editores partilhados são seletores color-coded, NÃO uma pílula única.** Inspeção
  detalhada (decisão do utilizador, scope corrigido) mostrou que `SchemaPropertyEditor` tem vários
  botões add-property color-coded por tipo (add string=`text-accent`, add object=`text-purple-400`,
  add $ref=`text-pink-400`) e `SchemaCompositionEditor` tem "Add Variant" (botão de texto). Normalizá-los
  para a pílula accent única **destruiria** o color-coding → **fora de escopo, intocados**.
- **Pílula "Add Schema" purple** em `VisualRestBuilder:324` (`bg-purple-500/10 text-purple-400`) — é
  deliberadamente colorida para a taxonomia de schemas; **não** é uma pílula accent → não é substituída
  pelo `AddButton` (que é accent).
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
  `VisualLegacyContractBuilder`, `VisualDataContractBuilder`): **apenas** as pílulas "Add" **accent
  idênticas** (`bg-accent/10 text-accent hover:bg-accent/20`) → `AddButton`; os botões Trash2 de
  remover linha → `RemoveIconButton` (com `className` passthrough onde havia `group-hover`/`opacity`/
  `pb-1`/`transition-*` e `e.stopPropagation()` preservado via `onClick(e)`).
- A pílula "Add Schema" **purple** (`VisualRestBuilder:324`) **não** é substituída (não é accent).

### 3. Fora de escopo (explícito)
- **Editores partilhados** (`SchemaPropertyEditor`, `SchemaCompositionEditor`): add-buttons
  color-coded e a taxonomia de cores de tipos (purple/pink/orange) — intencionais, intocados.
- `Field*` (input/area/select/checkbox/tag) primitives — ficam como estão (densos, propositados).
- Títulos `<h3>` de secção (já consistentes); `BuilderSubSection` órfão (só observação).
- Botões de mover linha (setas ↑↓) em `SchemaPropertyEditor`, toggles/chevrons de expansão, o `X`
  de remover tag dentro de `FieldTagInput` — afordâncias distintas, não duplicadas no mesmo padrão.
- Monaco, `builderSync`/`builderValidation`/`builderParse`, `ContractSection`, lógica de estado.

## Critérios de sucesso

1. Zero ocorrências da className-pílula "Add" **accent** duplicada fora de `AddButton`; um único sítio
   para o estilo add/remove dos 8 builders.
2. Comportamento idêntico: cada `<button>` renderizado mantém texto+ícone e os handlers (incl.
   `stopPropagation` e reveal por `group-hover`) → testes existentes continuam verdes.
3. A pílula purple "Add Schema" e os seletores color-coded dos editores partilhados ficam **inalterados**.
4. `npm run lint` + `npm run build` 0 erros; suíte total verde.
5. `git diff --name-only` contém apenas ficheiros sob `workspace/builders/` (+ o spec/plano de docs).
   Nada não relacionado (lição do ciclo 9: `git add` só de paths explícitos).

## Riscos

- **`RemoveIconButton` e variações por call site:** os removes têm 4 variantes de className
  (`group-hover`/`opacity`, `pb-1`, `transition-colors`/`transition-all`, size 11/12) e alguns
  `stopPropagation`. Mitigação: base mínima (`text-muted hover:text-danger`) + `className` passthrough
  verbatim por site + `onClick(e)` com evento. Verificar cada call site convertido individualmente.
- **Não normalizar por engano os color-coded:** o `AddButton` (accent) só substitui pílulas accent
  idênticas. Confirmar que a pílula purple "Add Schema" e os seletores dos editores partilhados ficam
  intocados.
- **Volume:** ~8 ficheiros de builder tocados; mudanças mecânicas mas numerosas → executar por
  ficheiro/lote com verificação, não num só commit gigante.
