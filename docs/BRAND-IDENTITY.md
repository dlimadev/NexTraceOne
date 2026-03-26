# NexTraceOne — Plano de Identidade Visual Corporativa

> **Versão:** 1.0
> **Status:** Proposta Inicial
> **Contexto:** Plataforma de Change Intelligence & Engineering Governance — dark-first, enterprise-grade

---

## 1. Visão e Posicionamento

### 1.1 O que é o NexTraceOne

NexTraceOne é uma plataforma soberana de governança de engenharia para organizações enterprise. Ela unifica controle de mudanças, catálogo de serviços, correlação de incidentes e inteligência operacional em uma única superfície — dando às equipes de engenharia **confiança operacional em tempo real**.

### 1.2 Essência da Marca

| Pilar | Descrição |
|---|---|
| **Confiança** | Decisões de mudança e governança baseadas em dados precisos |
| **Clareza** | Superfície de informação densa, mas sempre legível e orientada |
| **Soberania** | Controle interno, sem dependência de terceiros |
| **Precisão** | Cada detalhe importa — do código ao contrato |

### 1.3 Personalidade Visual

- **Corporativa sem ser fria** — Sofisticada, mas acessível a engenheiros
- **Técnica sem ser ruidosa** — Alta densidade informacional com hierarquia clara
- **Moderna sem ser modista** — Longevidade > tendência
- **Dark-first** — Ambiente natural de trabalho de engenharia

---

## 2. Sistema de Logo

### 2.1 Construção do Logo

O logo combina dois elementos:

**Ícone (marca-símbolo):** A letra **N** em gradiente azul-ciano inscrita dentro de um **globo geodésico**. O globo representa conectividade global e visão de rede. Os nós de rede (dots) ao redor do globo representam os serviços monitorados. A letra N representa **NexTraceOne** como eixo central da plataforma.

**Logotipo (wordmark):** `NexTraceOne` com codificação de cor:
- `Nex` → Azul ciano `#2BB7E3` — a plataforma (o "próximo nível")
- `Trace` → Branco `#F2F7FF` — o núcleo funcional (rastreamento)
- `One` → Mint `#18E8B8` — unidade, convergência

### 2.2 Variantes do Logo

| Variante | Uso | Arquivo |
|---|---|---|
| **Horizontal completo** | Header principal, documentos, apresentações | `logo.svg` |
| **Ícone (solo)** | Favicon, avatar, app icon, loading screen | `logo-icon.svg` |
| **Horizontal c/ tagline** | Landing page, materiais de marketing, pitch | `logo-tagline.svg` |
| **Negativo (fundo claro)** | Documentos impressos, PDFs, relatórios | `logo-light.svg` |
| **Monocromático** | Bordados, serigrafia, gravações | `logo-mono.svg` |

### 2.3 Área de Proteção

O logo deve sempre ter uma área de proteção mínima ao redor igual a **1× a altura do ícone** em cada lado. Nenhum elemento externo deve invadir esta área.

### 2.4 Tamanhos Mínimos

| Uso | Tamanho mínimo |
|---|---|
| Ícone digital | 16×16px |
| Ícone impresso | 8mm × 8mm |
| Logo horizontal digital | 120px de largura |
| Logo horizontal impresso | 40mm de largura |

### 2.5 Usos Incorretos

- ❌ Não alterar as cores do logo
- ❌ Não esticar ou distorcer proporções
- ❌ Não aplicar sombra, emboss ou efeitos externos
- ❌ Não usar em fundos que causem baixo contraste
- ❌ Não recriar o logo com outros fonts ou formas
- ❌ Não inverter o gradiente (azul deve estar à esquerda/baixo, mint à direita/cima)

---

## 3. Paleta de Cores

### 3.1 Cores de Marca (Brand Core)

| Token | Hex | Uso Principal |
|---|---|---|
| `brand-blue` | `#2BB7E3` | CTA principal, cor institucional |
| `brand-cyan` | `#18CFF2` | Foco, estados ativos, links |
| `brand-mint` | `#1EF2C1` | Sucesso, saúde, destaque positivo |
| `brand-blue-dark` | `#1A6FA3` | Hover de CTA, variações escuras |

### 3.2 Paleta de Canvas (Dark Theme)

| Token | Hex | Uso |
|---|---|---|
| `canvas` | `#081120` | Fundo base da aplicação |
| `deep` | `#0A1730` | Áreas de auth, hero, sidebar |
| `panel` | `#0F1E38` | Cards, painéis, containers |
| `elevated` | `#132543` | Hover, superfícies elevadas |
| `input` | `#091729` | Campos de formulário |
| `overlay` | `rgba(3,10,22,0.72)` | Modais, drawers |

### 3.3 Tipografia — Hierarquia de Texto

| Token | Hex | Alpha | Uso |
|---|---|---|---|
| `text-primary` | `#F2F7FF` | 100% | Títulos, labels críticos |
| `text-secondary` | `#B5C4D8` | — | Corpo principal |
| `text-tertiary` | `#8EA0B7` | — | Suporte, hints, subtítulos |
| `text-muted` | `#6D7E96` | — | Metadados, estados desabilitados |
| `text-on-accent` | `#04111E` | — | Texto sobre botões coloridos |

### 3.4 Estados Semânticos

| Estado | Cor | Hex | Aplicação |
|---|---|---|---|
| **Success / Healthy** | Mint | `#1EF2C1` | Serviço saudável, compliant, OK |
| **Info / Active** | Cyan | `#18CFF2` | Estado ativo, informativo, link |
| **Warning / Risk** | Âmbar | `#F5C062` | Atenção, risco moderado, pendente |
| **Danger / Incident** | Vermelho Rosa | `#FF7A86` | Erro, incidente crítico, falha |
| **Critical** | Vermelho | `#FF6A78` | Criticidade máxima, violação |
| **Neutral** | Cinza Azulado | `#8EA0B7` | Estado neutro, inativo |

### 3.5 Gradientes Principais

```
Gradient Logo / Ícone:
  from: #2563EB (azul) at bottom-left
  to:   #18E8B8 (mint) at top-right
  angle: 135°

Gradient CTA Button:
  from: #29C0E5
  to:   #2BB7E3
  angle: 135°

Gradient Page Background:
  from: #0A1730 (top)
  to:   #081120 (bottom)
  angle: 180°

Gradient Accent Surface:
  from: rgba(24,207,242,0.18) (top-left)
  to:   rgba(30,242,193,0.08) (bottom-right)
  angle: 135°
```

### 3.6 Data Visualization

| Slot | Cor | Hex | Semântica |
|---|---|---|---|
| `data-1` | Cyan | `#18CFF2` | Primário |
| `data-2` | Mint | `#1EF2C1` | Secundário |
| `data-3` | Cinza | `#8EA0B7` | Neutro |
| `data-4` | Âmbar | `#F5C062` | Atenção |
| `data-5` | Rosa | `#FF7A86` | Perigo |
| `data-6` | Azul índigo | `#5F8DFF` | Informação |

---

## 4. Tipografia

### 4.1 Família de Fontes

| Família | Uso | Stack |
|---|---|---|
| **Inter** | Interface principal, todos os textos UI | `Inter, ui-sans-serif, system-ui, -apple-system, sans-serif` |
| **JetBrains Mono** | Código, IDs, hashes, valores técnicos | `"JetBrains Mono", "IBM Plex Mono", ui-monospace, monospace` |

### 4.2 Escala Tipográfica

| Papel | Tamanho | Peso | Line-height | Uso |
|---|---|---|---|---|
| **Display XL** | 64px | 800 | 1.1 | Hero pages, onboarding |
| **Display** | 48px | 700 | 1.15 | Títulos de seção primários |
| **Heading 01** | 32px | 700 | 1.2 | Títulos principais de página |
| **Heading 02** | 24px | 600 | 1.25 | Subtítulos de seção |
| **Title 01** | 20px | 600 | 1.3 | Títulos de card, painel |
| **Title 02** | 18px | 600 | 1.35 | Subtítulos de card |
| **Body Large** | 18px | 400 | 1.6 | Corpo de texto destacado |
| **Body Medium** | 16px | 400 | 1.6 | Corpo padrão |
| **Body Small** | 14px | 400 | 1.55 | Corpo compacto |
| **Label** | 14px | 500 | 1.2 | Labels de formulário, badges |
| **Caption** | 12px | 400 | 1.4 | Metadados, timestamps |
| **Mono Small** | 12px | 400 | 1.5 | Código inline, IDs |
| **Overline** | 11px | 600 | 1.2 | Rótulos em uppercase |

### 4.3 Regras Tipográficas

- Usar **letter-spacing: -0.02em** em Display e Heading para peso óptico
- Evitar itálico na UI (reservar para tooltips e citações)
- Mono apenas para valores técnicos — nunca para textos de navegação
- Máximo de 2 pesos tipográficos em uma mesma superfície

---

## 5. Iconografia

### 5.1 Biblioteca Principal

**Lucide React** — conjunto coerente de ícones lineares 24×24 com stroke-width=1.5.

### 5.2 Regras de Uso

| Tamanho | Contexto |
|---|---|
| 12px | Badges, inline com texto compacto |
| 16px | Itens de lista, tabelas, metadados |
| 20px | Botões, labels de formulário |
| 24px | Ações principais, navigation items |
| 32px | Estados vazios (EmptyState) |
| 48px+ | Ilustrações de onboarding |

### 5.3 Ícones Semânticos do NexTraceOne

| Conceito | Ícone Sugerido | Cor |
|---|---|---|
| Serviço | `Box` / `Layers` | `text-secondary` |
| Contrato | `FileText` / `GitBranch` | `text-secondary` |
| Mudança | `GitCommit` / `ArrowUpRight` | `brand-cyan` |
| Incidente | `AlertTriangle` / `Zap` | `danger` |
| Governança | `Shield` / `Lock` | `text-secondary` |
| AI/Knowledge | `Sparkles` / `Brain` | `brand-mint` |
| Aprovação | `CheckCircle` | `success` |
| Risco | `AlertOctagon` | `warning` |
| FinOps / Custo | `DollarSign` / `TrendingUp` | `data-4` |
| Audit | `ClipboardList` | `text-tertiary` |

---

## 6. Layout e Grade

### 6.1 Base Grid

- **Unidade base:** 8px (grids multíplices de 4, 8, 12, 16, 24, 32, 48, 64)
- **Colunas:** Sistema de 12 colunas para layouts complexos; 4-col para mobile
- **Gutters:** 16px (mobile), 24px (tablet), 32px (desktop)
- **Margem lateral:** 24px (mobile), 40px (tablet), 64px (desktop)

### 6.2 Layout Principal da Aplicação

```
┌─────────────────────────────────────────────────────────┐
│  TOPBAR (64px height, sticky)                           │
│  [Logo] [Global Search]    [Notifications] [Avatar]     │
├──────────┬──────────────────────────────────────────────┤
│          │                                              │
│ SIDEBAR  │  MAIN CONTENT AREA                          │
│ (240px   │                                              │
│  collapsed│  ┌─────────────────────────────────────┐   │
│  56px)   │  │  PAGE HEADER                        │   │
│          │  │  [Breadcrumb] [Title] [Actions]      │   │
│ [Module  │  └─────────────────────────────────────┘   │
│  Items]  │                                              │
│          │  ┌──────────┐ ┌──────────┐ ┌──────────┐   │
│          │  │  CARD    │ │  CARD    │ │  CARD    │   │
│          │  └──────────┘ └──────────┘ └──────────┘   │
│          │                                              │
│          │  ┌─────────────────────────────────────┐   │
│          │  │  TABLE / LIST / GRAPH               │   │
│          │  └─────────────────────────────────────┘   │
└──────────┴──────────────────────────────────────────────┘
```

### 6.3 Regiões do Layout

| Região | Altura/Largura | Token |
|---|---|---|
| Topbar | 64px | `--nto-header-h: 64px` |
| Sidebar expandida | 240px | `--nto-sidebar-w: 240px` |
| Sidebar colapsada | 56px | `--nto-sidebar-collapsed: 56px` |
| Page header | 72px | interno |
| Content padding | 32px | `--nto-content-p: 32px` |

### 6.4 Raios de Borda (Border Radius)

| Token | Valor | Uso |
|---|---|---|
| `rounded-xs` | 6px | Tags, badges pequenos |
| `rounded-sm` | 10px | Inputs, botões secundários |
| `rounded-md` | 14px | Cards, painéis, dropdowns |
| `rounded-lg` | 18px | Modais, drawers |
| `rounded-xl` | 24px | Painéis hero, banners |
| `rounded-pill` | 999px | Badges de status, chips |

---

## 7. Componentes Core — Guia Visual

### 7.1 Botões

| Variante | Fundo | Texto | Uso |
|---|---|---|---|
| **Primary** | `gradient-cta` | `text-on-accent` | Ação principal única |
| **Secondary** | `panel` + border | `text-primary` | Ação alternativa |
| **Ghost** | Transparente | `text-secondary` | Ações terciárias |
| **Danger** | `danger` (α15%) | `danger` | Ações destrutivas |
| **Link** | Nenhum | `brand-cyan` | Links inline |

### 7.2 Cards

Estrutura padrão de card:
```
┌─────────────────────────────────┐
│ HEADER: [Icon] [Title] [Badge]  │ 48px
├─────────────────────────────────┤
│                                 │
│ CONTENT                         │
│                                 │
├─────────────────────────────────┤
│ FOOTER: [Meta] [Actions]        │ 40px
└─────────────────────────────────┘
```
- Background: `panel` (`#0F1E38`)
- Border: `border-soft` (`rgba(129,170,214,0.14)`)
- Border-radius: `rounded-md` (14px)
- Padding: 20px

### 7.3 Tabelas

- Header: `deep` background, text `text-muted` uppercase overline
- Row hover: `elevated` background
- Row selected: `selected` background + left border `brand-cyan`
- Stripe alternado: sutil (evitar se possível, preferir hover)

### 7.4 Badges de Status

| Status | Background | Texto | Dot |
|---|---|---|---|
| Healthy | `mint α15%` | `mint` | `mint` |
| Active | `cyan α15%` | `cyan` | `cyan` |
| Warning | `amber α15%` | `amber` | `amber` |
| Critical | `danger α15%` | `danger` | `danger` |
| Inactive | `neutral α12%` | `neutral` | `neutral` |

---

## 8. Páginas-Chave — Direção Visual

### 8.1 Login / Autenticação

- **Fundo:** Split layout — lado esquerdo com `deep` + gradiente radial ciano (glow sutil), lado direito com `canvas`
- **Logotipo:** Centralizado no lado esquerdo, versão ícone + wordmark horizontal
- **Card de login:** `panel` elevado, bordas `border-medium`, sombra `shadow-xl`
- **Campo de senha:** ícone de eye + indicador de força
- **CTA:** Botão Primary full-width
- **Tagline:** "Operational Confidence" abaixo do logo, em `text-muted`

### 8.2 Dashboard Principal

- **Topbar:** Logo collapsed + search global + notificações + avatar com tenant selector
- **KPI Cards:** Grid 4-col, métricas com ícones coloridos e sparkline inline
- **Activity Feed:** Lista de eventos recentes com ícones semânticos e timestamps relativos
- **Risk Score Widget:** Gauge visual com gradiente vermelho→amber→mint
- **Service Health Map:** Grid de serviços com dots de status coloridos

### 8.3 Catálogo de Serviços

- **Header:** Título + filtros (Environment, Owner, Status) + botão "Register Service"
- **Toggle view:** Grid Cards / Lista / Graph (topology)
- **Card de serviço:** Avatar (inicial), nome, tipo (API/Worker/DB), owner, health badge, SLO%
- **Topology Graph:** Canvas escuro com nós conectados, nós coloridos por saúde

### 8.4 Change Governance

- **Lista de mudanças:** Tabela com colunas: ID, Título, Risco (badge), Ambiente, Status (workflow), Autor, Data
- **Risk Score:** Barra de progresso colorida cyan→amber→danger
- **Detail Panel:** Drawer lateral com tabs (Overview, Blast Radius, Approvals, History)
- **Blast Radius:** Grafo de serviços impactados com animação de propagação

### 8.5 Incidents

- **Timeline:** Feed vertical de eventos com indicadores de severidade
- **Correlation View:** Cards relacionados com linhas de correlação
- **Runbook Panel:** Acordeão com steps sequenciais e checklist

### 8.6 AI Hub

- **Chat Interface:** Layout tipo terminal — fundo `canvas`, mensagens com glass-morphism sutil
- **Context Sources:** Chips coloridos mostrando quais contratos/serviços estão no contexto
- **Knowledge Cards:** Grid de artefatos com tipo e confiança visual

---

## 9. Efeitos e Animações

### 9.1 Durações

| Token | Valor | Uso |
|---|---|---|
| `motion-fast` | 120ms | Hover, focus rings |
| `motion-base` | 180ms | Transições de cor, borders |
| `motion-medium` | 240ms | Expansão de painéis, dropdowns |
| `motion-slow` | 320ms | Modais, drawers, page transitions |

### 9.2 Easing

- `ease-out` → Elementos que **entram** na tela
- `ease-in` → Elementos que **saem** da tela
- `ease-in-out` → Expansão/colapso de painéis

### 9.3 Glows e Halos

Uso moderado de glows para chamar atenção em estados críticos ou de destaque:
```css
/* Glow ciano — foco, destaque ativo */
box-shadow: 0 0 0 1px rgba(24,207,242,0.22), 0 0 24px rgba(24,207,242,0.16);

/* Glow mint — sucesso, healthy */
box-shadow: 0 0 0 1px rgba(30,242,193,0.20), 0 0 24px rgba(30,242,193,0.14);

/* Glow danger — incidente crítico */
box-shadow: 0 0 0 1px rgba(255,122,134,0.24), 0 0 20px rgba(255,122,134,0.14);
```

---

## 10. Configuração no Figma

### 10.1 Estrutura do Arquivo Figma

```
📁 NexTraceOne — Design System
  ├── 📄 00. Cover & Index
  ├── 📄 01. Foundations
  │   ├── Colors
  │   ├── Typography
  │   ├── Spacing
  │   ├── Border Radius
  │   ├── Shadows & Glows
  │   └── Motion
  ├── 📄 02. Iconography
  ├── 📄 03. Components
  │   ├── Atoms (Button, Badge, Input, Toggle…)
  │   ├── Molecules (Card, Table Row, Nav Item…)
  │   └── Organisms (Topbar, Sidebar, Modal…)
  ├── 📄 04. Patterns
  │   ├── Forms
  │   ├── Data Tables
  │   ├── Empty States
  │   └── Loading States
  └── 📄 05. Screens
      ├── Auth (Login, MFA, Reset)
      ├── Dashboard
      ├── Service Catalog
      ├── Change Governance
      ├── Incidents
      ├── AI Hub
      └── Admin
```

### 10.2 Variáveis Figma

Usar **Figma Variables** (não apenas Styles) para:
- Color tokens (modes: dark / light)
- Spacing tokens
- Border radius tokens
- Typography tokens

Arquivo de tokens para import via plugin: `src/frontend/public/brand/figma-tokens.json`

### 10.3 Auto Layout

- Todos os componentes devem usar **Auto Layout** com padding definido via spacing tokens
- Min-width e max-width configurados nos componentes para evitar overflow

---

## 11. Implementação — Checklist

### 11.1 Frontend (React + Tailwind)

- [x] CSS custom properties definidas em `src/index.css`
- [x] Design tokens exportados em `shared/design-system/tokens.ts`
- [x] Componentes base em `shared/ui/`
- [ ] Logo SVG adicionado em `public/brand/`
- [ ] Favicon multi-resolução gerado
- [ ] Open Graph image gerada (1200×630px)
- [ ] Splash screen para loading inicial

### 11.2 Assets a Gerar

| Asset | Formato | Dimensão | Localização |
|---|---|---|---|
| Logo completo | SVG | Vetorial | `public/brand/logo.svg` |
| Ícone | SVG | 160×160 | `public/brand/logo-icon.svg` |
| Favicon | ICO + PNG | 16, 32, 48px | `public/favicon.ico` |
| Apple touch icon | PNG | 180×180px | `public/apple-touch-icon.png` |
| OG Image | PNG | 1200×630px | `public/og-image.png` |
| Logo light (fundo claro) | SVG | Vetorial | `public/brand/logo-light.svg` |

---

## 12. Próximos Passos

1. **Aprovação da identidade** — Revisar este documento com stakeholders
2. **Criação do arquivo Figma** — Montar as foundations e componentes-base
3. **Prototipagem das 5 telas core** — Login, Dashboard, Catalog, Change, Incident
4. **Validação com usuários** — Sessões de usability testing com Platform Engineers
5. **Handoff para desenvolvimento** — Export de tokens e specs via Figma Dev Mode
6. **Audit de implementação** — Verificar conformidade do frontend com o design system

---

*Documento gerado com base no projeto NexTraceOne — versão 1.0*
*Referências visuais: `docs/DESIGN-SYSTEM.md`, `docs/GUIDELINE.md`, `docs/DESIGN.md`*
