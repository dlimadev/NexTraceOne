# Frontend UI/UX Redesign — Design Spec
**Data:** 2026-05-26  
**Abordagem:** Shell First → Componentes → Dashboard → Auth → Feature Pages  
**Direcção:** Redesign Expressivo — produto com personalidade, ainda enterprise-professional  
**Sidebar:** Sempre dark (independente do tema do utilizador)

---

## 1. Decisões de Arquitectura

| Decisão | Escolha | Racional |
|---|---|---|
| Sidebar theme | Sempre dark (`#0F1E38 → #081120`) | Ancora navegação, identidade forte — padrão Linear/Vercel/Supabase |
| Topbar height | 80px → **56px** | Liberta 24px de espaço vertical por página |
| KPI columns | 5 → **4** | "APIs registadas" move para Catálogo; 4 colunas respiram melhor em todos os viewports |
| Alert style | `<Link>` simples → **Alert estruturado** | icon-box + título + descrição + chevron — muito mais legível |
| Nav active state | `bg-blue text-white` → **gradient lateral + border-left 2px** | Menos agressivo, mais elegante, melhor contraste em modo escuro |

---

## 2. AppShell (Secção 1)

### Icon Rail
- Background: `linear-gradient(180deg, #0F1E38, #081120)` — **sempre**, independente do tema
- Logo: gradiente de marca `#1B7FE8 → #12C4E8 → #18E8B8` com `box-shadow: 0 0 16px rgba(18,196,232,.25)`
- Active state: `background: rgba(27,127,232,.25)` + `box-shadow: 0 0 12px rgba(27,127,232,.18), inset 0 0 0 1px rgba(27,127,232,.4)` + ícone `#3D96F2`
- Inactive: `color: rgba(129,170,214,.5)`, hover `bg: rgba(255,255,255,.04)` + cor `.8`
- Admin icon: `margin-top: auto` — separado no fundo do rail
- Counter de incidentes: `background: #FF7A86`, `font-size: 8px`, posição `top: -2px; right: -2px`

### Content Panel
- Background: `linear-gradient(180deg, #0D1C35, #081120)` — **sempre dark**
- Border-right: `1px solid rgba(129,170,214,.08)`
- Nav item active: `background: linear-gradient(90deg, rgba(27,127,232,.22), rgba(18,196,232,.08))` + `border-left: 2px solid #1B7FE8` + `color: #EAF2FF font-weight: 500`
- Nav item inactive: `color: rgba(181,196,216,.6)`, hover fundo `rgba(255,255,255,.04)`

### AppSidebarFooter — **RESTAURAR** (estava completamente comentado)
```tsx
// Ficheiro: src/frontend/src/components/shell/AppSidebarFooter.tsx
// Descomentar e conectar em AppSidebar.tsx (remover os comentários dos dois blocos)
// User card: avatar gradiente de marca + nome + role + chevron
// Clicável → abre mini-menu com Perfil / Acesso / Logout
```
- Avatar: `linear-gradient(135deg, #1B7FE8, #12C4E8, #18E8B8)`, `border-radius: 8px`, `30×30px`
- Nome: `font-size: 11px font-weight: 600 color: #F2F7FF`
- Role: `font-size: 9px color: rgba(142,160,183,.6)`
- Hover: `background: rgba(255,255,255,.04)`

### AppTopbar
- Height: `h-20` (80px) → `h-14` (56px)
- Search bar: `height: 32px` com kbd hint `⌘K`
- Action buttons: `30×30px border-radius: 7px`
- User avatar: `26×26px border-radius: 7px`
- Divider: `width: 1px height: 20px` (era `h-6`)

---

## 3. Componentes Partilhados (Secção 2)

### StatCard
- **Novo**: `border-top: 3px solid {cor semântica}` no topo do card
- Ícone: `width: 34px height: 34px border-radius: 9px` com `background: rgba({cor},.08)` (fundo tonal)
- Valor: cor semântica (ex: `color: #1B7FE8` para serviços) em vez de `text-heading` genérico
- Sparkline: 7 barras com gradiente de opacidade crescente até ao valor actual
- TrendBadge: `bg-success/8 text-success` (up) ou `bg-critical/8 text-critical` (down)

### Badge
- Novo: `dot indicator` — `width: 5px height: 5px border-radius: 50% background: currentColor`
- Novo: `pill variant` — `border-radius: 999px` para criticidade e feature flags
- Dot pulsante: `animation: pulse 1.5s ease-in-out infinite` em badges de incidente/crítico
- 6 estados: `success · warning · danger · info · neutral · accent`

### Button
- Variante `institutional`: `background: linear-gradient(135deg, #1B7FE8, #1468CC)` + `box-shadow: 0 2px 8px rgba(27,127,232,.3)` — para SSO
- Loading state: spinner SVG inline `(animation: spin .8s linear infinite)` + texto "A guardar…"
- Icon buttons: `32×32px border-radius: 7px`

### Card (CardHeader)
- `card-title-dot`: `width: 7px height: 7px border-radius: 50%` colorido por domínio semântico
- Dot pulsante em cards de incidentes/mudanças críticas
- Footer: `background: #fafbfc border-top: 1px solid #f0f1f5`

### Empty State
- Ícone container: `border: 1.5px dashed rgba(27,127,232,.25) border-radius: 14px background: rgba(27,127,232,.06)`
- CTA inline no empty state (botão primário pequeno)

### Alert (novo padrão — substituir `<Link>` simples no dashboard)
```
icon-box (28×28 com fundo tonal) + título (font-weight:600) + descrição (.7 opacity) + chevron
```
- Critical: `bg-critical/6 border-critical/18 color-critical`
- Warning: `bg-warning/6 border-warning/18 color-warning`
- Info: `bg-info/6 border-info/18 color-info`

---

## 4. Dashboard (Secção 3)

### Layout
1. **Context bar**: título + persona badge + subtítulo (lado esquerdo) + environment switcher (lado direito)
2. **Persona switcher decorativo**: chips horizontais (Engineer · Tech Lead · Architect · Executive · Platform Admin)
3. **KPI row — 4 colunas** (era 5): Serviços · Contratos · Mudanças · Incidentes
4. **Alerts strip**: alertas estruturados (alert component) — apenas quando há dados
5. **Operational grid 2×2**: Serviços recentes | Saúde contratos | Mudanças | Incidentes

### KPI Cards
- `grid-template-columns: repeat(4, 1fr)` com `gap: 12px`
- Cada card: `border-top: 3px solid {cor}` + ícone tonal + valor colorido + sparkline 7 barras
- "APIs registadas" removida do dashboard — move para `/services` como stat local

### Operational Cards
- Header: dot colorido (pulsante em critical) em vez de ícone Lucide
- Serviços: lista com avatar (inicial) + nome + team/stack + badge criticidade
- Contratos: grid 3×1 (draft/revisão/aprovados) + stacked progress bar com legenda
- Mudanças/Incidentes: grid 3×1 com mini-metrics coloridas

---

## 5. Auth Pages (Secção 4)

### AuthShell — painel esquerdo
- Dois orbs animados com `animation: pulse 4s/5s ease-in-out infinite`:
  ```css
  /* já existe no AuthShell mas sem animação — adicionar */
  .orb1 { animation: pulse 4s ease-in-out infinite; }
  .orb2 { animation: pulse 5s ease-in-out infinite 1s; }
  ```
- Logo com `box-shadow: 0 0 24px rgba(18,196,232,.25)`
- Headline: mais curta e impactante (ex: "Engineering Governance, unified.")

### AuthCard — painel direito
- `box-shadow: 0 4px 24px rgba(0,0,0,.06)` (era sem shadow)
- `border-radius: 14px` (era `rounded-2xl` = 16px — manter)
- Heading: "Bem-vindo de volta" em vez de "Welcome to NexTraceOne" (via i18n `auth.welcomeTitle`)

### TenantSelectionPage
- Sem mudanças estruturais — aplicar os novos estilos de card/badge automaticamente via componentes

---

## 6. Feature Pages — Melhorias Sistémicas (Secção 5)

Estas páginas herdam as melhorias das secções anteriores automaticamente. Melhorias específicas adicionais:

### Service Catalog (ServiceCatalogServicesTab)
- Tabela com avatar de serviço (inicial com `bg: rgba(27,127,232,.1)`) + nome + team/stack em segunda linha
- Badge de tipo de serviço (Backend · GraphQL · Worker · Frontend) à esquerda da criticidade
- Hover de linha: `hover:bg-hover transition-colors`
- Count badge no título da página: `bg-accent/10 text-cyan border border-cyan/15`

### AI Chat (AiAssistantPage / ChatSidebar)
- Bolhas user: `background: #1B7FE8 color: white border-radius: 12px 12px 4px 12px`
- Bolhas AI: `background: white border: 1px solid #e2e5ed border-radius: 12px 12px 12px 4px`
- Header da bolha AI: `font-size: 9px font-weight: 600 color: #0891B2` com nome do modelo
- Badge do modelo activo no header do chat: `bg-success/10 text-success border-success/20`

---

## 7. Ficheiros a Modificar

| Ficheiro | Tipo de mudança |
|---|---|
| `src/frontend/src/index.css` | Adicionar animação `pulse` com nome diferente de `pulse-soft`; ajustar `--nto-header-height: 56px` |
| `src/frontend/src/components/shell/AppSidebar.tsx` | Sidebar always-dark; descomentar AppSidebarFooter; ajustar active state |
| `src/frontend/src/components/shell/AppSidebarFooter.tsx` | Descomentar e implementar user card |
| `src/frontend/src/components/shell/AppTopbar.tsx` | `h-20` → `h-14`; search 32px; action btns 30px |
| `src/frontend/src/components/StatCard.tsx` | border-top 3px; ícone tonal; valor colorido; sparkline 7 barras |
| `src/frontend/src/components/Badge.tsx` | dot indicator; pill variant; 6 estados semânticos |
| `src/frontend/src/shared/ui/Button.tsx` | variante institutional; loading state inline |
| `src/frontend/src/components/Card.tsx` | title dot; footer background |
| `src/frontend/src/components/EmptyState.tsx` | ícone dashed-border; CTA inline |
| `src/frontend/src/components/ErrorState.tsx` | alinhar com padrão Alert |
| `src/frontend/src/features/shared/pages/DashboardPage.tsx` | 4 KPIs; alertas estruturados; persona switcher; dots nos card headers |
| `src/frontend/src/features/identity-access/components/AuthShell.tsx` | animação nos orbs; shadow na logo |
| `src/frontend/src/features/identity-access/pages/LoginPage.tsx` | heading i18n update |
| `src/frontend/src/features/catalog/components/ServiceCatalogServicesTab.tsx` | avatar + team + badge tipo |
| `src/frontend/src/features/ai-hub/pages/AiAssistantPage.tsx` | bolhas diferenciadas; badge modelo |
| `src/frontend/src/features/ai-hub/components/AssistantMessageBubble.tsx` | estilos de bolha |

---

## 8. O que NÃO muda

- Paleta de cores e tokens CSS (`index.css`) — já são sólidos
- Tipografia (Instrument Sans Variable + JetBrains Mono) — mantida
- Sistema de i18n — todas as strings via `t()`
- Estrutura de rotas e lazy loading
- TanStack Query e gestão de estado
- Testes existentes — actualizar snapshots se necessário
- Acessibilidade (WCAG 2.1 AA) — manter `focus-visible`, `aria-*`, skip-link

---

## 9. Critérios de Sucesso

- [ ] `AppSidebarFooter` visível e funcional (user info + logout)
- [ ] Sidebar mantém fundo dark em light mode
- [ ] Topbar com 56px de altura
- [ ] Dashboard com 4 KPIs e alertas estruturados
- [ ] StatCard com border-top semântico em todas as instâncias
- [ ] Empty states com CTA inline
- [ ] Auth orbs com animação pulse
- [ ] Todos os testes existentes a passar (pode requerer snapshot updates)
