# Revisão do Design System — NexTraceOne Frontend

> **Data:** 2026-03-26
> **Foco:** Tokens · Paleta · Tipografia · Espaçamento · Componentes base · Consistência · Sustentabilidade

---

## 1. Objetivo

Avaliar se o design system do NexTraceOne está estruturado, documentado, consistentemente aplicado e pronto para escalar com o produto sem gerar dívida visual.

---

## 2. Estrutura do Design System

### Ficheiros Principais

| Ficheiro | Papel | Estado |
|---------|-------|--------|
| `src/index.css` | Fonte de verdade dos tokens CSS via `@theme` | Excelente |
| `src/shared/design-system/tokens.ts` | Espelho TypeScript dos tokens para uso programático | Bom |
| `src/shared/design-system/foundations.ts` | Classes Tailwind compostas por padrão visual | Bom |
| `src/lib/cn.ts` | Utilitário de merge de classes (clsx + tailwind-merge) | Correto |
| `src/shared/tokens/color-migration-guide.ts` | Guia de migração de cores | Presente (não auditado) |

### Organização Geral: **Sólida**

A arquitetura do design system está bem pensada: os tokens CSS são a fonte primária de verdade, espelhados em TypeScript apenas quando necessário para cálculos programáticos. A separação entre tokens (`index.css`), padrões de classe (`foundations.ts`) e valores TS (`tokens.ts`) é correta e sustentável.

---

## 3. Paleta de Cores

### 3.1 Sistema de Layering (Canvas → Elevated)

```
--color-canvas:   #081120  ← Fundo base do app
--color-deep:     #0A1730  ← Sidebar, modais scuros
--color-panel:    #0F1E38  ← Painéis, cards principais
--color-card:     #0F1E38  ← (idêntico a panel — ver abaixo)
--color-elevated: #132543  ← Cards elevados, dropdowns
--color-hover:    #132543  ← (idêntico a elevated — ver abaixo)
--color-active:   #1A2E50  ← Estado ativo/pressionado
--color-selected: #162A48  ← Estado selecionado
--color-input:    #091729  ← Fundo de campos de formulário
```

**O que funciona bem:**
- A escada de luminosidade navy (#081120 → #1A2E50) cria profundidade visual legível
- Os 7+ passos de superfície permitem layering complexo sem recorrer a sombras pesadas

**Problemas:**

| Token | Problema | Impacto |
|-------|---------|---------|
| `--color-card` e `--color-panel` | Valores idênticos (#0F1E38) — dois tokens para o mesmo valor visual | Confusão na escolha entre Card e Panel |
| `--color-elevated` e `--color-hover` | Valores idênticos (#132543) — hover não é distinguível de elevated em estado base | Hover visual potencialmente imperceptível em cards elevados |
| `--color-subtle` (#0D1B32) | Cor entre canvas e deep sem uso documentado explícito | Token órfão potencial |

### 3.2 Acentos

```
--color-accent:       #2BB7E3  (cyan médio — CTA, links, foco)
--color-cyan:         #18CFF2  (cyan mais brilhante — dados, acentos)
--color-mint:         #1EF2C1  (verde-turquesa — sucesso, saúde)
```

**Análise:**
- Dois tokens cyan (`accent` vs `cyan`) com valores diferentes cria ambiguidade: quando usar `text-accent` vs `text-cyan`?
- `--color-mint: #1EF2C1` é uma cor muito saturada (alto chroma). Em contextos densos de dados, pode competir visualmente com o conteúdo
- Os tokens de glow (`--shadow-glow-cyan`, `--shadow-glow-mint`) amplificam o risco de aparência "neon" se usados sem disciplina

**Recomendação:** Documentar claramente a semântica de `accent` vs `cyan` — ou consolidar em um único token primário de acento com variantes de intensidade.

### 3.3 Cores Semânticas

```
--color-success:  #1EF2C1  (mint)
--color-info:     #18CFF2  (cyan)
--color-warning:  #F5C062  (âmbar)
--color-critical: #FF7A86  (rosa-coral)
--color-danger:   #FF6A78  (variante de critical — ver abaixo)
```

**Problema:** `--color-critical` e `--color-danger` são dois tokens para "vermelho/risco" com valores muito próximos:
- `critical: #FF7A86`
- `danger: #FF6A78`

Isso cria inconsistência no código: alguns componentes usam `text-critical`, outros `text-danger`, sem semântica diferenciada. Em `foundations.ts:41`, `danger` mapeia para `text-critical`. Mas no código de páginas, tanto `text-critical` quanto `text-danger` aparecem.

**Recomendação:** Consolidar em um único token `--color-danger` (ou `--color-critical`) e deprecar o outro.

### 3.4 Bordas

```
--color-edge:        rgba(129, 170, 214, 0.14)  ← borda padrão
--color-edge-strong: rgba(129, 170, 214, 0.22)  ← hover/focus
--color-edge-focus:  rgba(55, 190, 233, 0.42)   ← focus ring de campos
--color-divider:     rgba(255, 255, 255, 0.06)  ← separadores internos
```

**Análise:** Sistema de bordas excelente — usa opacity sobre o mesmo tom de base, garantindo coerência visual independentemente de luminância de fundo. `--color-divider` muito sutil (0.06 opacity) — correto para separadores internos de listas.

---

## 4. Tipografia

### 4.1 Fontes

```
--font-sans: 'Inter'          ← UI text
--font-mono: 'JetBrains Mono' ← Código, IDs, dados técnicos
```

Ambas carregadas via `@fontsource`. Escolha excelente: Inter é referência em UIs enterprise; JetBrains Mono é premium para contextos técnicos.

### 4.2 Escala Tipográfica

Definida em `index.css` como classes compostas:

```css
.type-display-01   → 4rem / 700 / 1.05 / -0.02em
.type-display-02   → 3rem / 700 / 1.1
.type-heading-01   → 2rem / 700 / 1.15
.type-heading-02   → 1.5rem / 600 / 1.2
.type-title-01     → 1.25rem / 600 / 1.3
.type-title-02     → 1.125rem / 600 / 1.3
.type-body-lg      → 1.125rem / 400 / 1.6
.type-body-md      → 1rem / 400 / 1.5
.type-body-sm      → 0.875rem / 400 / 1.45
.type-caption      → 0.75rem / 500 / 1.35
.type-label        → 0.875rem / 500 / 1.35
.type-overline     → 0.6875rem / 600 / 1.3 / 0.05em / UPPERCASE
.type-mono-sm      → 0.75rem / 500 / JetBrains Mono
```

**Análise:** Escala bem definida com 13 passos. O `type-overline` com `letter-spacing: 0.05em` e `text-transform: uppercase` é excelente para rótulos de seções.

**Problema:** As classes `.type-*` definidas em CSS não estão sendo consistentemente usadas nos componentes. Em vez disso, os componentes usam classes Tailwind inline como `text-2xl font-bold`, `text-sm font-semibold`, `text-xs font-medium`. Isso cria drift entre a escala definida e a aplicada.

**Evidência:**
- `DashboardPage.tsx:188`: `text-2xl font-bold text-heading` (não usa `.type-heading-01`)
- `ExecutiveOverviewPage.tsx:106`: `text-sm font-semibold text-heading` (não usa `.type-label`)
- `ServiceDetailPage.tsx:127`: `text-2xl font-bold text-heading` (não usa `.type-heading-01`)

Resultado: a escala tipográfica é formalmente correta mas **praticamente ignorada** na maioria dos componentes. O `Typography.tsx` existe mas não é o padrão dominante de uso.

---

## 5. Espaçamento

### 5.1 Tokens de Spacing

```
--spacing-1: 4px
--spacing-2: 8px
...
--spacing-16: 64px
```

**Problema:** Estes tokens CSS de spacing **não estão integrados com Tailwind 4** de forma que `p-spacing-4` funcione como `padding: 16px`. O sistema usa os valores default do Tailwind (que também são base-4 por default), mas os tokens de spacing CSS (`--spacing-*`) ficam redundantes.

**Evidência:** Nenhum componente auditado usa `p-[var(--spacing-4)]` — todos usam utilidades Tailwind padrão (`p-4`, `p-5`, `p-6`). O sistema de tokens de spacing é declarativo mas não integrado ao workflow real.

### 5.2 Consistência de Padding

| Local | Padding observado |
|-------|------------------|
| `PageContainer.tsx` | `px-4 sm:px-5 lg:px-6 xl:px-8` / `py-5 lg:py-6` |
| `ServiceDetailPage.tsx` | `p-6 lg:p-8` (fora do PageContainer) |
| `Card CardBody` | `p-5` (padrão), `p-0` (sobreescrito) |
| `FilterBar.tsx` | `mb-5` (gap inferior) |
| `AppSidebar.tsx nav` | `py-3 px-3` ou `px-1.5` |

Existe variação aceitável mas o facto de `ServiceDetailPage` usar padding manual sem `PageContainer` é a causa de inconsistência visual mais visível.

---

## 6. Border Radius

```
--radius-xs:   6px
--radius-sm:   10px
--radius-md:   14px
--radius-lg:   18px
--radius-xl:   24px
--radius-2xl:  32px
--radius-pill: 999px
```

**Análise:** A escala começa em 6px (não-zero) e sobe rapidamente para 18px (lg). Os raios são generosos — `radius-lg: 18px` em cards dá um estilo moderno, mas pode tender a um aspeto "consumer app" em contextos muito densos de dados.

**Sugestão:** Para tabelas e componentes densos de dados, considerar `radius-xs` (6px) como padrão em vez de `radius-sm` ou `radius-md`.

**Consistência:**
- `Card.tsx` usa `rounded-lg` (Tailwind), que mapeia para o valor default do Tailwind (8px) — **não** para `--radius-lg` (18px). Isto é um problema de integração entre os tokens do design system e o Tailwind config.

---

## 7. Sombras

```
--shadow-xs:       0 1px 2px rgba(0,0,0,.28)
--shadow-sm:       0 2px 6px rgba(0,0,0,.30)
--shadow-surface:  0 10px 30px rgba(0,0,0,.28)  ← igual a shadow-md
--shadow-md:       0 10px 30px rgba(0,0,0,.28)
--shadow-elevated: 0 18px 40px rgba(0,0,0,.34)  ← igual a shadow-lg
--shadow-lg:       0 18px 40px rgba(0,0,0,.34)
--shadow-floating: 0 24px 60px rgba(0,0,0,.42)  ← igual a shadow-xl
--shadow-xl:       0 24px 60px rgba(0,0,0,.42)
```

**Problema:** `shadow-surface` = `shadow-md`, `shadow-elevated` = `shadow-lg`, `shadow-floating` = `shadow-xl`. Existem **6 nomes para 3 valores distintos**. Isso cria confusão no uso e na manutenção.

**Recomendação:** Consolidar para 3-4 sombras com nomes sem ambiguidade: `shadow-sm`, `shadow-md`, `shadow-lg`, `shadow-floating`.

---

## 8. Componentes Base — Qualidade do Design System

### Button (`src/components/Button.tsx`)

| Aspeto | Avaliação |
|--------|-----------|
| Variantes | Excelente — primary (CTA gradient), secondary, danger, ghost, subtle |
| Tamanhos | Bom — sm (h-9), md (h-11), lg (h-14) |
| Estados | Bom — loading com spinner, disabled com opacity |
| Focus | Bom — `focus-visible:ring-2 focus-visible:ring-accent` |
| Ícone loading | Fraco — SVG inline em vez de `<Loader />` do sistema |

### Badge (`src/components/Badge.tsx`)

| Aspeto | Avaliação |
|--------|-----------|
| Variantes semânticas | Bom — success, warning, danger, info |
| Variantes duplicadas | **Problema** — `default` e `neutral` mapeiam para o mesmo CSS |
| Tamanho único | Limitação — sem variante `sm`/`lg` |
| Suporte a ícone | Ausente — não aceita `icon` prop; páginas adicionam ícone via `className` (ex: `IncidentDetailPage.tsx:175`) |

### StatCard (`src/components/StatCard.tsx`)

| Aspeto | Avaliação |
|--------|-----------|
| Estrutura | Adequada para KPIs |
| Trend indicator | **Fraco** — usa caracteres ASCII `↑`/`↓` |
| Clicabilidade | Ausente — StatCard não suporta `onClick` ou `as Link` |
| Contexto | Limitado — sem subtítulo ou hint contextual |
| Acessibilidade | Fraco — números sem semântica para leitores de ecrã |

### Card (`src/components/Card.tsx`)

| Aspeto | Avaliação |
|--------|-----------|
| Estrutura | Bom — Card/CardHeader/CardBody/CardFooter |
| Border radius | Problema — usa `rounded-lg` (Tailwind 8px) em vez do token `--radius-lg` (18px) |
| Interatividade | Ausente — sem variante de Card clicável (usa `interactiveSurfaceClass` de foundations.ts mas não é exportado como prop do Card) |

---

## 9. Integração Tailwind — Problemas de Mapeamento

O Tailwind 4 com o `@theme` block do `index.css` deveria mapear automaticamente os tokens CSS para utilidades Tailwind. Ex: `--color-accent: #2BB7E3` deveria gerar `text-accent`, `bg-accent`, `border-accent`, etc.

**Achados:**
- `text-accent`, `bg-accent`, `border-edge` etc. são usados nos componentes — indica que o mapeamento `@theme` → Tailwind está funcional
- O problema é o uso paralelo de cores Tailwind hardcoded (`bg-red-900/40`, `text-emerald-500`) em vez dos tokens — isto indica que os contribuidores às vezes preferem usar o Tailwind "vanilla" em vez de respeitar os tokens definidos

**Conclusão:** O problema não é técnico — o `@theme` funciona. O problema é de **disciplina de uso** e ausência de linting que impeça o uso de cores Tailwind diretas quando existem tokens equivalentes.

---

## 10. Sustentabilidade do Design System

### Pontos Fortes
- `color-migration-guide.ts` sugere que a equipa antecipa migrações de cor — boa prática
- Documentação referenciada em `docs/DESIGN-SYSTEM.md` (evidenciado pelos comentários no código)
- `foundations.ts` com padrões reutilizáveis é uma boa forma de garantir consistência

### Riscos
- Sem linting (`eslint` rule) que impeça `text-red-500` quando `text-critical` existe
- Classes `.type-*` definidas mas não enforçadas — drift entre escala declarada e aplicada
- Tokens de spacing redundantes com Tailwind default sem integração formal
- Tokens duplicados (card=panel, elevated=hover, critical≈danger, surface=md, elevated=lg) criam ruído na manutenção

### Recomendação Principal

Adicionar uma regra ESLint custom (ou `eslint-plugin-tailwindcss` com allowlist) que:
1. Alerte quando classes de cor Tailwind "raw" são usadas onde existem tokens equivalentes
2. Force o uso de `text-heading`, `text-body`, `text-muted`, `text-faded`, `text-accent`, `text-success`, `text-warning`, `text-critical` em vez dos equivalentes Tailwind

---

## 11. Resumo de Problemas do Design System

| Problema | Ficheiro | Prioridade |
|----------|---------|------------|
| Variantes `default` e `neutral` idênticas no Badge | `Badge.tsx:16-17` | Baixa |
| `--color-card` = `--color-panel` (tokens redundantes) | `index.css:24-25` | Média |
| `--color-elevated` = `--color-hover` (tokens redundantes) | `index.css:27-28` | Média |
| `--color-critical` ≈ `--color-danger` (valores quase idênticos sem semântica diferenciada) | `index.css:63-65` | Alta |
| Sombras com nomes duplicados (surface=md, elevated=lg, floating=xl) | `index.css:91-97` | Média |
| Escala `.type-*` declarada mas não usada nos componentes | `index.css:258-340` | Alta |
| `rounded-lg` em Card não reflete `--radius-lg` do design system | `Card.tsx` | Média |
| Tokens de spacing CSS não integrados com Tailwind | `index.css:104-115` | Baixa |
| Sem ESLint rule para prevenir bypass de tokens | Configuração global | Alta |
| Dois tokens cyan (`accent` vs `cyan`) com semântica ambígua | `index.css:47-54` | Média |
