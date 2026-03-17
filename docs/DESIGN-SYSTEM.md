# DESIGN-SYSTEM.md — NexTraceOne

## 1. Objetivo

Este documento descreve o **design system de implementação** do NexTraceOne.  
Ele transforma a direção visual definida em `GUIDELINE.md` e a visão de produto descrita em `DESIGN.md` em tokens, regras e padrões utilizáveis por design e frontend.

A referência principal é a tela de login aprovada, complementada pelo dashboard.

---

## 2. Foundations

## 2.1 Color tokens

### Brand / Core
| Token | Valor | Uso |
|---|---:|---|
| `--nto-bg-canvas` | `#081120` | fundo principal da aplicação |
| `--nto-bg-deep` | `#0A1730` | áreas profundas, auth shell, hero backdrop |
| `--nto-bg-elevated` | `#0F1E38` | cards, painéis, containers |
| `--nto-bg-elevated-2` | `#132543` | hover, superfícies destacadas |
| `--nto-bg-input` | `#091729` | inputs e campos de formulário |
| `--nto-bg-overlay` | `rgba(3, 10, 22, 0.72)` | modais, drawers, overlays |

### Border
| Token | Valor | Uso |
|---|---:|---|
| `--nto-border-soft` | `rgba(129, 170, 214, 0.14)` | borda padrão |
| `--nto-border-medium` | `rgba(129, 170, 214, 0.22)` | borda hover |
| `--nto-border-strong` | `rgba(55, 190, 233, 0.42)` | foco, destaque |
| `--nto-divider` | `rgba(255,255,255,0.06)` | divisores |

### Text
| Token | Valor | Uso |
|---|---:|---|
| `--nto-text-primary` | `#F2F7FF` | títulos, labels críticos |
| `--nto-text-secondary` | `#B5C4D8` | corpo principal |
| `--nto-text-tertiary` | `#8EA0B7` | apoio, hints |
| `--nto-text-muted` | `#6D7E96` | metadados, desabilitado |
| `--nto-text-on-accent` | `#04111E` | texto sobre CTA claro |

### Accent
| Token | Valor | Uso |
|---|---:|---|
| `--nto-accent-cyan-500` | `#18CFF2` | foco, linha ativa, CTA secundário de destaque |
| `--nto-accent-cyan-400` | `#47DBFF` | hover/acento luminoso |
| `--nto-accent-mint-500` | `#1EF2C1` | success, healthy, destaque positivo |
| `--nto-accent-mint-400` | `#79FCE0` | brilho/halo positivo |
| `--nto-accent-blue-500` | `#2BB7E3` | CTA principal institucional |
| `--nto-accent-blue-400` | `#6ED7F5` | hover do CTA principal |

### Semantic
| Token | Valor | Uso |
|---|---:|---|
| `--nto-success` | `#1EF2C1` | healthy, OK, compliant |
| `--nto-info` | `#18CFF2` | estado ativo, contexto, links |
| `--nto-warning` | `#F5C062` | atenção, risco moderado |
| `--nto-danger` | `#FF7A86` | erro, incidente crítico, violação |
| `--nto-critical` | `#FF6A78` | criticidade máxima |
| `--nto-neutral` | `#8EA0B7` | neutro |

### Data Viz
| Token | Valor |
|---|---:|
| `--nto-data-1` | `#18CFF2` |
| `--nto-data-2` | `#1EF2C1` |
| `--nto-data-3` | `#8EA0B7` |
| `--nto-data-4` | `#F5C062` |
| `--nto-data-5` | `#FF7A86` |
| `--nto-data-6` | `#5F8DFF` |

---

## 2.2 Gradients

### Base gradients
```css
--nto-gradient-page: linear-gradient(180deg, #0A1730 0%, #081120 100%);
--nto-gradient-surface: linear-gradient(180deg, rgba(255,255,255,0.02) 0%, rgba(255,255,255,0.01) 100%);
--nto-gradient-accent: linear-gradient(135deg, rgba(24,207,242,0.18) 0%, rgba(30,242,193,0.08) 100%);
--nto-gradient-cta: linear-gradient(135deg, #29C0E5 0%, #2BB7E3 100%);
```

### Halo / glow
```css
--nto-glow-cyan: 0 0 0 1px rgba(24,207,242,0.22), 0 0 24px rgba(24,207,242,0.16);
--nto-glow-mint: 0 0 0 1px rgba(30,242,193,0.20), 0 0 24px rgba(30,242,193,0.14);
--nto-glow-danger: 0 0 0 1px rgba(255,122,134,0.24), 0 0 20px rgba(255,122,134,0.14);
```

---

## 2.3 Typography

### Font stack
```css
--nto-font-sans: Inter, ui-sans-serif, system-ui, -apple-system, BlinkMacSystemFont, "Segoe UI", sans-serif;
--nto-font-mono: "JetBrains Mono", "IBM Plex Mono", ui-monospace, SFMono-Regular, monospace;
```

### Type scale
| Token | Size | Weight | Line-height | Uso |
|---|---:|---:|---:|---|
| `display-01` | 64px | 700 | 1.05 | hero/login |
| `display-02` | 48px | 700 | 1.1 | telas institucionais |
| `heading-01` | 32px | 700 | 1.15 | título de página |
| `heading-02` | 24px | 600 | 1.2 | subtítulo de página |
| `title-01` | 20px | 600 | 1.3 | títulos de seção |
| `title-02` | 18px | 600 | 1.3 | títulos de card |
| `body-lg` | 18px | 400 | 1.6 | descrição principal |
| `body-md` | 16px | 400 | 1.5 | texto comum |
| `body-sm` | 14px | 400 | 1.45 | labels, apoio |
| `caption` | 12px | 500 | 1.35 | metadados |
| `mono-sm` | 12px | 500 | 1.35 | ids, eventos, versões |

### Regras
- usar tracking levemente negativo apenas em headings grandes
- evitar texto em all caps exceto badges/overlines
- números de KPI podem usar peso 600 ou 700
- labels de formulário: 14px / 500

---

## 2.4 Spacing

Base 8pt.

| Token | Valor |
|---|---:|
| `space-1` | 4px |
| `space-2` | 8px |
| `space-3` | 12px |
| `space-4` | 16px |
| `space-5` | 20px |
| `space-6` | 24px |
| `space-8` | 32px |
| `space-10` | 40px |
| `space-12` | 48px |
| `space-16` | 64px |

### Regras
- padding interno de cards: 20px a 24px
- gap padrão entre componentes na mesma seção: 16px ou 24px
- gap entre seções maiores: 32px ou 40px

---

## 2.5 Radius

| Token | Valor | Uso |
|---|---:|---|
| `radius-sm` | 10px | chips, badges pequenos |
| `radius-md` | 14px | inputs, botões |
| `radius-lg` | 18px | cards |
| `radius-xl` | 24px | painéis maiores |
| `radius-pill` | 999px | pills, indicadores |

A linguagem visual do NexTraceOne é suavemente arredondada, nunca quadrada dura.

---

## 2.6 Shadows

```css
--nto-shadow-surface: 0 10px 30px rgba(0, 0, 0, 0.28);
--nto-shadow-elevated: 0 18px 40px rgba(0, 0, 0, 0.34);
--nto-shadow-floating: 0 24px 60px rgba(0, 0, 0, 0.42);
```

Regras:
- preferir sombra escura e difusa
- evitar sombra preta dura
- não usar sombra como único mecanismo de hierarquia

---

## 2.7 Motion

| Token | Valor |
|---|---:|
| `motion-fast` | `120ms` |
| `motion-base` | `180ms` |
| `motion-medium` | `240ms` |
| `motion-slow` | `320ms` |

### Curvas
```css
--nto-ease-standard: cubic-bezier(0.2, 0, 0, 1);
--nto-ease-emphasis: cubic-bezier(0.2, 0.8, 0.2, 1);
```

---

## 3. Layout system

## 3.1 Breakpoints
```css
--bp-sm: 640px;
--bp-md: 768px;
--bp-lg: 1024px;
--bp-xl: 1280px;
--bp-2xl: 1440px;
--bp-3xl: 1600px;
```

## 3.2 Containers
- páginas analíticas: max-width entre 1440px e 1680px
- páginas de autenticação: composição dividida 55/45 ou 60/40
- modais: small 480px, medium 640px, large 840px, xlarge 1120px

## 3.3 Z-index
| Camada | Valor |
|---|---:|
| base | 0 |
| sticky | 20 |
| header/sidebar | 40 |
| dropdown/popover | 60 |
| modal | 80 |
| toast | 100 |

---

## 4. Componentes

## 4.1 App Shell
### Sidebar
- largura desktop: 264px a 280px
- fundo: `--nto-bg-elevated`
- borda direita suave
- grupos separados por heading discreto
- item ativo com contraste e realce lateral
- ícone + label sempre alinhados

### Topbar
- altura: 64px a 72px
- busca global central ou central-esquerda
- seletor de workspace/ambiente
- ações utilitárias à direita
- fundo levemente translúcido ou superfície sólida premium

---

## 4.2 Auth Shell

### Estrutura recomendada
- área esquerda: narrativa de valor e branding
- área direita: card de autenticação
- fundo com halos radiais, grid suave e profundidade

### Hero
- grande headline em 3 a 5 linhas
- uma palavra de ênfase em mint/cyan
- descrição curta em corpo grande
- chips institucionais opcionais
- mini widget/preview operacional como prova visual

### Auth Card
- largura ideal: 420px a 460px
- padding interno: 40px
- título forte
- campos com espaço generoso
- CTA primário dominante
- SSO como ação secundária de mesmo nível estratégico

---

## 4.3 Buttons

### Primary button
**Uso:** confirmar, entrar, criar, salvar, avançar.

```css
height: 56px;
padding-inline: 24px;
border-radius: 18px;
background: var(--nto-gradient-cta);
color: var(--nto-text-on-accent);
font-size: 16px;
font-weight: 700;
```

Estados:
- hover: elevação + leve aumento de brilho
- focus: ring ciano
- active: redução mínima de escala ou luminosidade
- disabled: opacidade + contraste reduzido

### Secondary button
- fundo escuro elevado
- borda suave
- texto primário
- hover com border-medium e superfície mais clara

### Ghost / Toolbar button
- usado em toolbars, grids, filtros e ícones
- deve manter hit area mínima de 40px

---

## 4.4 Inputs

### Field container
- display vertical
- label acima
- helper text abaixo quando necessário
- mensagem de erro diretamente associada ao campo

### Input padrão
```css
height: 56px;
border-radius: 18px;
background: var(--nto-bg-input);
border: 1px solid var(--nto-border-soft);
padding-inline: 18px;
color: var(--nto-text-primary);
```

Estados:
- default: borda soft
- hover: border-medium
- focus: border-strong + glow ciano
- error: border danger + mensagem textual
- success: border mint

### Placeholder
- usar `--nto-text-muted`
- nunca competir com valor digitado

### Password input
- botão mostrar/ocultar com ícone
- nunca comprometer a percepção de segurança
- logs/network/frontend não devem expor credenciais em texto puro

---

## 4.5 Select, Combobox e Search
- mesma base visual do input
- caret discreto
- menu flutuante com superfície elevada
- opção ativa com fundo destacado
- busca global com ícone à esquerda e ação rápida

---

## 4.6 Checkbox / Toggle
### Checkbox
- 20px
- cantos arredondados suaves
- check claro e consistente
- foco com ring

### Toggle
- 44x24 ou 48x28
- thumb animado suavemente
- estado ligado com cyan ou mint conforme semântica

---

## 4.7 Cards

### KPI card
Estrutura:
- título
- métrica principal
- contexto auxiliar
- status ou CTA opcional

Padrões:
- padding 24px
- título em `title-02`
- número em 36px a 48px
- ação de drill-down opcional no rodapé

### Analytical card
- título + ações no header
- área de conteúdo com padding consistente
- footer opcional para CTA/contexto

### Feature card / preview
- usado em login, onboarding, páginas institucionais
- pode conter ilustração de gráfico/linha/topologia

---

## 4.8 Table
### Cabeçalho
- 44px a 48px de altura
- texto secundário com peso 600
- divisores discretos

### Linha
- 52px a 64px
- hover suave
- seleção clara
- ações em trailing area

### Conteúdo
- primeira coluna tende a ter título principal
- status com badge
- números alinhados à direita quando fizer sentido
- metadados em texto terciário

---

## 4.9 Badge / Chip / Status pill
### Badge semântico
- altura: 24px a 28px
- radius pill
- usar versão sólida suave ou ghost tonal

Exemplos:
- `Healthy`
- `High risk`
- `Critical`
- `SSO disponível`
- `SLA 99.95%`

### Chips de módulo/capacidade
- `API Catalog`
- `AIOps`
- `Observability`

Usar fundo elevado, borda suave e ponto colorido opcional.

---

## 4.10 Tabs
- sublinhado luminoso ou pill discreta
- troca rápida
- sem animação excessiva
- uso comum em detalhes de serviço, APIs, políticas, evidências, changelog

---

## 4.11 Modal / Drawer
### Modal
- foco total em decisão ou edição curta
- fundo overlay escuro
- close claro
- CTA principal no footer

### Drawer
- ideal para filtros densos, detalhes rápidos e side workflows
- largura 420px a 560px

---

## 4.12 Toast / Inline alert
### Toast
- canto superior direito
- curta duração
- sem ocupar centro da tela
- ícone + título + mensagem opcional

### Inline alert
- usado dentro do layout
- semântica por cor + ícone + texto
- não depender só da cor

---

## 4.13 Empty state
Todo empty state deve ter:
- título
- explicação curta
- ação recomendada
- visual leve opcional

Evitar empty state genérico.

---

## 4.14 Skeleton / Loading
- usar blocos com shimmer suave
- manter forma real do conteúdo
- não usar spinner como padrão principal para páginas densas

---

## 4.15 Graph / Topology node
Para topologias e mapas de dependência:

### Node
- círculo ou card pequeno
- glow apenas quando ativo/selecionado
- ícone do tipo do ativo
- label legível

### Edge
- linha discreta
- destaque por seleção/risco/impacto
- setas ou intensidade quando necessário

---

## 5. Iconografia

### Estilo
- linear, técnico, limpo
- strokes consistentes
- preenchimento mínimo
- bom contraste em dark theme

### Regras
- ícones pequenos: 16px
- ícones padrão: 20px
- ícones de card/topbar: 20px a 24px
- ícones hero/kpi: 24px a 32px

---

## 6. Estados

## 6.1 Interaction states
Todo componente interativo deve prever:
- default
- hover
- focus-visible
- active
- disabled
- loading

## 6.2 Semantic states
Todo componente semântico deve prever:
- neutral
- info
- success
- warning
- danger

---

## 7. Acessibilidade

### Requisitos mínimos
- contraste adequado em dark UI
- foco visível e inequívoco
- navegação por teclado
- hit area mínima de 40x40
- labels reais em formulários
- erro com texto explicativo
- ícone nunca substitui texto em fluxo crítico

---

## 8. Naming e estrutura recomendada no código

### Tokens
```text
color.bg.canvas
color.bg.elevated
color.text.primary
color.accent.cyan.500
space.4
radius.lg
shadow.surface
motion.base
```

### Componentes
```text
AppShell
SidebarNav
Topbar
PageHeader
KpiCard
StatusBadge
PrimaryButton
TextField
PasswordField
SearchField
SelectField
DataTable
TopologyGraph
InsightList
ComplianceCard
AuthHero
AuthCard
```

---

## 9. Exemplo de tokens em CSS

```css
:root {
  --nto-bg-canvas: #081120;
  --nto-bg-deep: #0A1730;
  --nto-bg-elevated: #0F1E38;
  --nto-bg-elevated-2: #132543;
  --nto-bg-input: #091729;

  --nto-text-primary: #F2F7FF;
  --nto-text-secondary: #B5C4D8;
  --nto-text-tertiary: #8EA0B7;
  --nto-text-muted: #6D7E96;

  --nto-border-soft: rgba(129, 170, 214, 0.14);
  --nto-border-medium: rgba(129, 170, 214, 0.22);
  --nto-border-strong: rgba(55, 190, 233, 0.42);

  --nto-accent-cyan-500: #18CFF2;
  --nto-accent-mint-500: #1EF2C1;
  --nto-accent-blue-500: #2BB7E3;

  --nto-success: #1EF2C1;
  --nto-info: #18CFF2;
  --nto-warning: #F5C062;
  --nto-danger: #FF7A86;

  --nto-radius-md: 14px;
  --nto-radius-lg: 18px;
  --nto-space-4: 16px;
  --nto-space-6: 24px;
  --nto-motion-base: 180ms;
}
```

---

## 10. Regras finais de implementação

1. Nenhum componente novo deve nascer fora deste sistema.
2. Toda cor deve vir de token.
3. Toda variação de spacing deve respeitar a escala.
4. Todo estado interativo precisa ter `hover`, `focus-visible` e `disabled`.
5. Toda nova tela precisa reutilizar a linguagem da autenticação e do dashboard.
6. Segurança percebida é parte do design: campos sensíveis, autenticação e ações críticas devem transmitir confiança visual e comportamental.
