# NexTraceOne — Arquitetura Visual & Design System

> **Sovereign Change Intelligence Platform**
> Identidade visual dark-first, enterprise-grade, inspirada em plataformas de observabilidade.

---

## 1. Filosofia de Design

O frontend da NexTraceOne segue princípios de plataformas enterprise de análise e observabilidade:

- **Dark-first** — superfícies escuras em camadas para uso prolongado e alta densidade informacional
- **Hierarquia visual forte** — cada nível de superfície, texto e borda é semanticamente distinto
- **Densidade controlada** — muita informação acessível sem sobrecarregar visualmente
- **Status operacional imediato** — cores semânticas + ícones + texto para comunicar estado
- **Acessibilidade AA** — contraste mínimo 4.5:1, foco visível, navegação completa por teclado
- **Identidade da marca** — azul elétrico (#15AFF6) → índigo (#4379EE) → roxo (#9039E8) como gradiente de destaque

---

## 2. Design Tokens

Todos os tokens estão em `src/frontend/src/index.css` usando a diretiva `@theme {}` do Tailwind CSS v4.
Cada `--color-nome` gera automaticamente utilitários `bg-nome`, `text-nome`, `border-nome`.

### 2.1 Superfícies (layering system)

| Token        | Hex       | Uso                                              |
|-------------|-----------|--------------------------------------------------|
| `canvas`    | `#0A0C18` | Fundo base da aplicação (`<html>`, fundo geral)  |
| `panel`     | `#0E1220` | Sidebar, header, painéis fixos                   |
| `card`      | `#12182A` | Cards, formulários, seções elevadas              |
| `elevated`  | `#1A2236` | Elementos sobre cards (dropdowns, tabs inativas) |
| `hover`     | `#1E2840` | Estado hover de linhas, botões ghost             |
| `active`    | `#243050` | Estado ativo/pressionado                         |

### 2.2 Texto

| Token     | Hex       | Uso                                  |
|----------|-----------|--------------------------------------|
| `heading`| `#FDFDFE` | Títulos, valores de destaque, texto primário |
| `body`   | `#D9DEE8` | Texto de corpo, parágrafos, labels   |
| `muted`  | `#92939C` | Texto secundário, captions, hints    |
| `faded`  | `#5C6478` | Labels de seção, texto ultra-sutil   |

### 2.3 Bordas

| Token         | Hex       | Uso                          |
|--------------|-----------|------------------------------|
| `edge`       | `#2A3348` | Divisores padrão, bordas leves |
| `edge-strong`| `#3A4762` | Bordas de foco, separadores fortes |

### 2.4 Marca

| Token          | Hex       | Uso                            |
|---------------|-----------|--------------------------------|
| `brand`       | `#4379EE` | Cor primária da marca          |
| `brand-blue`  | `#15AFF6` | Azul elétrico (informação, início do gradiente) |
| `brand-purple`| `#9039E8` | Roxo (fim do gradiente, destaques) |
| `accent`      | `#4379EE` | Elementos interativos (botões, links, estados ativos) |
| `accent-hover`| `#5A8FFF` | Hover sobre elementos accent   |

### 2.5 Status Semânticos

| Token      | Hex       | Uso                            |
|-----------|-----------|--------------------------------|
| `success` | `#24B47E` | Healthy, aprovado, ok          |
| `warning` | `#E6A23C` | Degraded, atenção, alerta      |
| `critical`| `#E15241` | Critical, erro, falha          |
| `info`    | `#15AFF6` | Informativo, em progresso      |

### 2.6 Gradiente da Marca

```css
linear-gradient(90deg, #15AFF6 0%, #4379EE 45%, #9039E8 100%)
```

Aplicar apenas em pontos de foco: brand stripe no topo da sidebar, barras de progresso, CTAs premium.
Nunca em fundos grandes ou preenchimentos dominantes.

### 2.7 Tipografia

- **Sans-serif**: Inter (400–700) — toda a interface
- **Monospace**: JetBrains Mono (400–500) — código, hashes, UUIDs, shortcuts

Escala tipográfica:
- Display: 32–40px
- Page title: 24–28px
- Section title: 18–20px
- Card title: 14–16px
- Body: 14–15px
- Caption: 12–13px

### 2.8 Sombras

Sombras escuras para superfícies dark:
- `shadow-xs` a `shadow-xl` — profundidades crescentes
- `shadow-glow` — halo sutil azul para estados hover premium (ex.: cards de tenant)

---

## 3. Arquitetura do Shell

```
┌──────────────────────────────────────────────────────┐
│  Sidebar (w-64, bg-panel, fixed left)                │
│  ┌────────────────────────────────────────────┐      │
│  │  Brand gradient stripe (h-1)               │      │
│  │  Logo + "Change Intelligence"              │      │
│  │  ─────────────────────────────             │      │
│  │  PLATFORM                                  │      │
│  │    Dashboard                               │      │
│  │    Releases                                │      │
│  │    Engineering Graph                       │      │
│  │    Contracts                               │      │
│  │    Workflow                                │      │
│  │    Promotion                               │      │
│  │  ADMINISTRATION                            │      │
│  │    Users                                   │      │
│  │    Audit                                   │      │
│  │  ─────────────────────────────             │      │
│  │  User avatar + role + logout               │      │
│  └────────────────────────────────────────────┘      │
│                                                      │
│  Main Column (ml-64, flex-col)                       │
│  ┌────────────────────────────────────────────┐      │
│  │  AppHeader (h-14, bg-panel)                │      │
│  │    [🔍 Search pages, actions… ⌘K]  🌐 🔔 ⚙ [U]│  │
│  ├────────────────────────────────────────────┤      │
│  │  Content (flex-1, overflow-y-auto)         │      │
│  │    <Outlet /> — páginas da aplicação       │      │
│  └────────────────────────────────────────────┘      │
└──────────────────────────────────────────────────────┘
```

### Componentes do Shell

| Componente       | Arquivo                | Responsabilidade                       |
|-----------------|------------------------|----------------------------------------|
| `AppLayout`     | `AppLayout.tsx`        | Shell autenticado com Cmd+K global     |
| `Sidebar`       | `Sidebar.tsx`          | Navegação principal, seções, logout    |
| `AppHeader`     | `AppHeader.tsx`        | Busca, idioma, notificações, avatar    |
| `CommandPalette`| `CommandPalette.tsx`   | Modal Cmd+K com busca e teclado        |

---

## 4. Inventário de Componentes

### 4.1 Layout & Shell
- `AppLayout` — shell autenticado
- `Sidebar` — navegação principal
- `AppHeader` — header global
- `CommandPalette` — busca/navegação rápida

### 4.2 Feedback & Status
- `StatusPill` — indicador com dot colorido + label (7 status kinds)
- `Badge` — badge semântico translúcido
- `EmptyState` — estado vazio com ícone, título, descrição e ação
- `Skeleton` / `SkeletonLine` / `SkeletonCard` / `SkeletonTable` — loading states

### 4.3 Data Display
- `Card` / `CardHeader` / `CardContent` — container de conteúdo
- `StatCard` — métrica com título, valor, ícone e tendência
- `Button` — primário, secundário, danger, ghost

---

## 5. Padrões de Implementação

### 5.1 Formulários (inputs)

```
bg-canvas border border-edge text-heading placeholder:text-muted
focus:ring-2 focus:ring-accent focus:border-accent rounded-md
```

### 5.2 Tabelas

```
thead: bg-panel border-b border-edge
th: text-muted text-xs font-medium uppercase tracking-wider
tbody: divide-y divide-edge
tr: hover:bg-hover transition-colors
td: text-body
```

### 5.3 Badges Semânticos (translúcidos)

```
bg-success/15 text-success   → healthy, aprovado
bg-warning/15 text-warning   → degraded, alerta
bg-critical/15 text-critical → crítico, erro
bg-info/15 text-info         → informativo, em progresso
bg-brand-purple/15 text-brand-purple → mudança recente
```

### 5.4 Botões

| Variante    | Classes                                          |
|------------|--------------------------------------------------|
| Primary    | `bg-accent text-white hover:bg-accent-hover`     |
| Secondary  | `bg-card border border-edge text-body hover:bg-hover` |
| Danger     | `bg-critical text-white hover:bg-critical/90`    |
| Ghost      | `text-muted hover:bg-hover hover:text-body`      |

### 5.5 Focus Ring

Todos os elementos interativos usam `focus-visible:ring-2 focus-visible:ring-accent` para acessibilidade.

---

## 6. Acessibilidade

- **Contraste**: mínimo AA (4.5:1 para texto, 3:1 para UI)
- **Foco visível**: `outline: 2px solid accent` com `outline-offset: 2px`
- **Cor nunca é o único indicador**: `StatusPill` usa dot + texto; badges usam label textual
- **Navegação por teclado**: `CommandPalette` com ↑↓ Enter Escape
- **Reduced motion**: animações desabilitadas via `prefers-reduced-motion: reduce`
- **ARIA**: `role="status"` em StatusPill, `role="dialog" aria-modal` em CommandPalette

---

## 7. Internacionalização (i18n)

- Dois idiomas: `en.json` e `pt-BR.json`
- Toda string de UI vem de i18n — zero texto hardcoded
- Alternância de idioma no AppHeader
- Namespaces: `common.*`, `auth.*`, `sidebar.*`, `header.*`, `commandPalette.*`, `dashboard.*`, `users.*`, etc.
- Detecção automática do idioma do navegador via `i18next-browser-languageDetector`

---

## 8. Gradiente da Marca — Regras de Uso

| ✅ Usar em                        | ❌ Não usar em                    |
|----------------------------------|----------------------------------|
| Brand stripe (sidebar topo)      | Fundo de páginas inteiras        |
| Barra de progresso premium       | Background de cards comuns       |
| Logo/identidade contextual       | Textos longos                    |
| CTA principal (quando destaque)  | Bordas generalizadas             |
| Chip/tag selecionado             | Ícones repetitivos               |

---

## 9. Stack Técnica

| Camada       | Tecnologia                     |
|-------------|--------------------------------|
| Framework   | React 19 + TypeScript 5.9      |
| Build       | Vite 7                         |
| Styling     | Tailwind CSS v4 (`@theme {}`)  |
| Routing     | react-router-dom v7            |
| Data        | TanStack Query v5              |
| i18n        | react-i18next v16              |
| Ícones      | lucide-react v0.577            |
| Fontes      | Inter + JetBrains Mono (Google Fonts) |
