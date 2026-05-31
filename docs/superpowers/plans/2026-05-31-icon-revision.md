# Icon Revision — NexTraceOne Frontend Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Substituir todos os ícones semanticamente errados ou duplicados na aplicação, seguindo o estilo Strato puro (monocromático, acento só no ativo), em 3 vagas por prioridade visual.

**Architecture:** Todas as alterações são em ficheiros TSX do frontend React. A biblioteca permanece Lucide React — apenas se trocam quais os ícones usados. Wave 1 toca um único ficheiro (`AppSidebar.tsx`). Waves 2 e 3 distribuem-se pelos 19 módulos de features.

**Tech Stack:** React 19, TypeScript 5.9, Lucide React, Tailwind CSS 4, Vite 7

**Spec:** `docs/superpowers/specs/2026-05-31-icon-revision-design.md`

---

## Wave 1 — Sidebar

### Task 1: Corrigir imports e duplicados no AppSidebar

**Files:**
- Modify: `src/frontend/src/components/shell/AppSidebar.tsx`

#### Contexto
`AppSidebar.tsx` tem ~80 itens de navegação. Os seguintes ícones estão duplicados entre contextos distintos:

| Item | Ícone actual | Ícone novo |
|---|---|---|
| `modelGovernance` (AI) | `Database` | `BrainCircuit` (já importado) |
| `canonicalEntities` (Contracts) | `Database` | `Boxes` |
| `aiAgents` | `Network` | `Sparkles` |
| `domains` (Organization) | `Network` | `Globe` |
| `customDashboards` | `LayoutDashboard` | `LayoutGrid` |
| `accessReview` | `ClipboardCheck` | `UserCheck` (já importado) |

- [ ] **Step 1: Adicionar novos imports ao bloco lucide-react**

No ficheiro `src/frontend/src/components/shell/AppSidebar.tsx`, localiza o bloco de imports do lucide-react (começa na linha 12 aproximadamente). Adiciona `Boxes, Sparkles, Globe, LayoutGrid` ao bloco e remove `Database, Network` (serão substituídos).

O bloco de imports da primeira linha deve ficar assim (mantém todos os outros, apenas altera Database/Network):

```tsx
import {
  LayoutDashboard, FileText, Zap, Users, Users2, CheckSquare, ArrowUpCircle,
  AlertTriangle, Clock, UserCheck, ClipboardCheck, Monitor, Bot,
  FileCode, Share2, Server,
  Settings, SlidersHorizontal,
  PanelLeftClose, PanelLeftOpen,
  Cable, TrendingUp, BookOpen, Briefcase,
  Workflow, StickyNote, BookMarked, Radar,   // Network removido — todos os usos substituídos nesta task
  CalendarDays, Award, BrainCircuit, Palette, Cpu,
  Archive, HardDrive, Gauge, Bell, RotateCcw, Leaf, Lock,
  Train, MapPin, GitCommit, Target, Download, Sliders, MessageSquare, Eye, BookText,
  PackageCheck, GitMerge, History, Building2,
  Bug, Radio, Flame, TrendingDown, Store, FileSearch, Lightbulb,
  HeartPulse, ArrowRightLeft, FlaskConical, Star, MonitorDot,
  Waypoints, LineChart, Stethoscope, PieChart, Diff,
  ListChecks, FileLock2, KeyRound, ShieldAlert, BookOpenCheck,
  Layers, ScanEye, Scale, DoorOpen, Tag, PhoneCall, Send,
  // Novos — Wave 1
  Boxes, Sparkles, Globe, LayoutGrid,
} from 'lucide-react';
```

- [ ] **Step 2: Substituir ícone de `modelGovernance`**

Localiza no array `navItems` o item com `to: '/ai/models'`:

```tsx
// ANTES:
{ labelKey: 'sidebar.modelGovernance', to: '/ai/models', icon: <Database size={18} />, permission: 'ai:governance:read', section: 'aiHub' },

// DEPOIS:
{ labelKey: 'sidebar.modelGovernance', to: '/ai/models', icon: <BrainCircuit size={18} />, permission: 'ai:governance:read', section: 'aiHub' },
```

- [ ] **Step 3: Substituir ícone de `canonicalEntities`**

Localiza o item com `to: '/contracts/canonical'`:

```tsx
// ANTES:
{ labelKey: 'sidebar.canonicalEntities', to: '/contracts/canonical', icon: <Database size={18} />, permission: 'contracts:read', section: 'contracts' },

// DEPOIS:
{ labelKey: 'sidebar.canonicalEntities', to: '/contracts/canonical', icon: <Boxes size={18} />, permission: 'contracts:read', section: 'contracts' },
```

- [ ] **Step 4: Substituir ícone de `aiAgents`**

Localiza o item com `to: '/ai/agents'`:

```tsx
// ANTES:
{ labelKey: 'sidebar.aiAgents', to: '/ai/agents', icon: <Network size={18} />, permission: 'ai:assistant:read', section: 'aiHub' },

// DEPOIS:
{ labelKey: 'sidebar.aiAgents', to: '/ai/agents', icon: <Sparkles size={18} />, permission: 'ai:assistant:read', section: 'aiHub' },
```

- [ ] **Step 5: Substituir ícone de `domains`**

Localiza o item com `to: '/governance/domains'`:

```tsx
// ANTES:
{ labelKey: 'sidebar.domains', to: '/governance/domains', icon: <Network size={18} />, permission: 'governance:domains:read', section: 'organization' },

// DEPOIS:
{ labelKey: 'sidebar.domains', to: '/governance/domains', icon: <Globe size={18} />, permission: 'governance:domains:read', section: 'organization' },
```

- [ ] **Step 6: Substituir ícone de `customDashboards`**

Localiza o item com `to: '/governance/custom-dashboards'`:

```tsx
// ANTES:
{ labelKey: 'sidebar.customDashboards', to: '/governance/custom-dashboards', icon: <LayoutDashboard size={18} />, permission: 'governance:reports:read', section: 'governance' },

// DEPOIS:
{ labelKey: 'sidebar.customDashboards', to: '/governance/custom-dashboards', icon: <LayoutGrid size={18} />, permission: 'governance:reports:read', section: 'governance' },
```

- [ ] **Step 7: Substituir ícone de `accessReview`**

Localiza o item com `to: '/access-reviews'`:

```tsx
// ANTES:
{ labelKey: 'sidebar.accessReview', to: '/access-reviews', icon: <ClipboardCheck size={18} />, permission: 'identity:users:read', section: 'admin' },

// DEPOIS:
{ labelKey: 'sidebar.accessReview', to: '/access-reviews', icon: <UserCheck size={18} />, permission: 'identity:users:read', section: 'admin' },
```

- [ ] **Step 8: Verificar build TypeScript**

```bash
cd src/frontend && npx tsc --noEmit 2>&1 | head -30
```

Expected: zero erros relacionados com ícones. Se aparecer erro de import, verifica que `Boxes`, `Sparkles`, `Globe`, `LayoutGrid` estão no bloco de imports.

- [ ] **Step 9: Commit**

```bash
git add src/frontend/src/components/shell/AppSidebar.tsx
git commit -m "feat(ui): wave1 — fix 6 duplicate sidebar icons"
```

---

### Task 2: Corrigir erros semânticos no AppSidebar

**Files:**
- Modify: `src/frontend/src/components/shell/AppSidebar.tsx`

#### Contexto

| Item | Ícone actual | Ícone novo | Motivo |
|---|---|---|---|
| `runbooks` | `FileCode` | `ScrollText` | Runbooks são documentos, não código |
| `syntheticMonitoring` | `Radio` | `Gauge` | Radio = broadcast; Gauge = medição/sonda |
| `contractPipeline` | `Zap` | `GitBranch` | Zap genérico; GitBranch = pipeline CI/CD |

- [ ] **Step 1: Adicionar `ScrollText, GitBranch` aos imports; remover `FileCode, Radio`**

Edita o bloco lucide-react — remove `FileCode` e `Radio` da lista, e adiciona `ScrollText, GitBranch` à secção "Novos — Wave 1":

```tsx
  // Novos — Wave 1
  Boxes, Sparkles, Globe, LayoutGrid,
  ScrollText, GitBranch,
```

Remove `FileCode` e `Radio` das linhas onde estão. Verifica que `Gauge` já está no bloco (linha `Archive, HardDrive, Gauge, ...`).

- [ ] **Step 2: Substituir ícone de `runbooks`**

Localiza o item com `to: '/operations/runbooks'`:

```tsx
// ANTES:
{ labelKey: 'sidebar.runbooks', to: '/operations/runbooks', icon: <FileCode size={18} />, permission: 'operations:runbooks:read', section: 'operations' },

// DEPOIS:
{ labelKey: 'sidebar.runbooks', to: '/operations/runbooks', icon: <ScrollText size={18} />, permission: 'operations:runbooks:read', section: 'operations' },
```

- [ ] **Step 3: Substituir ícone de `syntheticMonitoring`**

Localiza o item com `to: '/operations/synthetic-monitoring'`:

```tsx
// ANTES:
{ labelKey: 'sidebar.syntheticMonitoring', to: '/operations/synthetic-monitoring', icon: <Radio size={18} />, permission: 'operations:reliability:read', section: 'operations' },

// DEPOIS:
{ labelKey: 'sidebar.syntheticMonitoring', to: '/operations/synthetic-monitoring', icon: <Gauge size={18} />, permission: 'operations:reliability:read', section: 'operations' },
```

- [ ] **Step 4: Substituir ícone de `contractPipeline`**

Localiza o item com `to: '/catalog/contracts/pipeline'`:

```tsx
// ANTES:
{ labelKey: 'sidebar.contractPipeline', to: '/catalog/contracts/pipeline', icon: <Zap size={18} />, permission: 'catalog:contracts:pipeline:read', section: 'contracts' },

// DEPOIS:
{ labelKey: 'sidebar.contractPipeline', to: '/catalog/contracts/pipeline', icon: <GitBranch size={18} />, permission: 'catalog:contracts:pipeline:read', section: 'contracts' },
```

`Zap` permanece nos imports porque é usado no `sectionIcons` da secção `changes`.

- [ ] **Step 5: Verificar build**

```bash
cd src/frontend && npx tsc --noEmit 2>&1 | head -30
```

Expected: zero erros.

- [ ] **Step 6: Commit**

```bash
git add src/frontend/src/components/shell/AppSidebar.tsx
git commit -m "feat(ui): wave1 — fix 3 semantic sidebar icons (runbooks, synthetic, pipeline)"
```

---

### Task 3: Corrigir ícone de secção Operations no rail

**Files:**
- Modify: `src/frontend/src/components/shell/AppSidebar.tsx`

#### Contexto
O objecto `sectionIcons` define os ícones do rail vertical (ícone por secção). A secção `operations` usa `AlertTriangle` (perigo/erro), quando devia usar `Activity` (pulso operacional).

- [ ] **Step 1: Adicionar `Activity` aos imports**

Na secção "Novos — Wave 1" do bloco de imports:

```tsx
  // Novos — Wave 1
  Boxes, Sparkles, Globe, LayoutGrid,
  ScrollText, GitBranch, Activity,
```

- [ ] **Step 2: Alterar sectionIcons**

Localiza o objecto `sectionIcons` (à volta da linha 147 aproximadamente):

```tsx
// ANTES:
const sectionIcons: Partial<Record<NavSection, React.ReactNode>> = {
  home: <LayoutDashboard size={22} />,
  services: <Server size={22} />,
  contracts: <FileText size={22} />,
  changes: <Zap size={22} />,
  operations: <AlertTriangle size={22} />,   // ← alterar esta linha
  aiHub: <Bot size={22} />,
  governance: <Briefcase size={22} />,
  organization: <Users size={22} />,
  integrations: <Cable size={22} />,
  admin: <Settings size={22} />,
};

// DEPOIS (só muda a linha operations):
  operations: <Activity size={22} />,
```

`AlertTriangle` permanece nos imports porque é usado no item `incidents` do navItems.

- [ ] **Step 3: Verificar build**

```bash
cd src/frontend && npx tsc --noEmit 2>&1 | head -30
```

Expected: zero erros.

- [ ] **Step 4: Commit**

```bash
git add src/frontend/src/components/shell/AppSidebar.tsx
git commit -m "feat(ui): wave1 — operations rail icon AlertTriangle → Activity"
```

---

## Wave 2 — PageHeaders dos módulos

### Task 4: Auditar e corrigir PageHeader icons nos 19 módulos

**Files:**
- Modify: ficheiros em `src/frontend/src/features/*/pages/*.tsx`

#### Critério de decisão
Para cada ficheiro de página auditado, verifica:
1. O ícone do `<PageHeader icon={...}>` representa correctamente o conceito da página?
2. O mesmo ícone é usado noutro módulo visível no mesmo contexto (sidebar + main content)?

Se a resposta a (1) for não, ou a (2) for sim → substituir.

#### Módulos prioritários e problemas conhecidos

Percorre os ficheiros abaixo e aplica as substituições indicadas. Para módulos não listados, lê o `PageHeader` e verifica pelo critério acima.

- [ ] **Step 1: Auditar `features/ai-hub`**

```bash
grep -rn "PageHeader" src/frontend/src/features/ai-hub/pages/ --include="*.tsx" | grep "icon="
```

Consistência esperada dentro do módulo:
- AI Assistant: `Bot` ✓
- AI Agents: deve agora usar `Sparkles` (consistente com sidebar)
- Model Governance: deve usar `BrainCircuit` (consistente com sidebar)
- AI Audit: `ScanEye` ✓

Se algum PageHeader usar um ícone diferente do sidebar para o mesmo item, actualiza-o para coincidir.

- [ ] **Step 2: Auditar `features/operations`**

```bash
grep -rn "PageHeader" src/frontend/src/features/operations/pages/ --include="*.tsx" | grep "icon="
```

Ícones esperados (consistentes com sidebar):
- Incidents: `AlertTriangle` ✓
- Runbooks: deve ser `ScrollText` após Wave 1
- Reliability/SLOs: `HeartPulse` ✓
- Request Explorer: `ArrowRightLeft` ✓
- Trace Explorer: `Activity` ou `Waypoints` ✓
- Error Tracking: `Bug` ✓
- Synthetic Monitoring: deve ser `Gauge` após Wave 1

Actualiza qualquer PageHeader que não coincida com o ícone do sidebar correspondente.

- [ ] **Step 3: Auditar `features/governance`**

```bash
grep -rn "PageHeader" src/frontend/src/features/governance/pages/ --include="*.tsx" | grep "icon="
```

Ícones esperados:
- Executive Overview: `Briefcase` ✓
- Reports: `PieChart` ✓
- Custom Dashboards: deve ser `LayoutGrid` após Wave 1
- FinOps: `TrendingUp` ✓
- GreenOps: `Leaf` ✓
- Compliance: `ClipboardCheck` ✓
- Risk Center: `ShieldAlert` ✓
- Policies: `Scale` ✓
- Technical Debt: `TrendingDown` ✓
- Audit Trail: `History` ✓

- [ ] **Step 4: Auditar `features/change-governance`**

```bash
grep -rn "PageHeader" src/frontend/src/features/change-governance/pages/ --include="*.tsx" | grep "icon="
```

Ícones esperados:
- Changes: `Diff` ✓
- Releases: `Tag` ✓
- Release Calendar: `CalendarDays` ✓
- DORA Metrics: `LineChart` ✓
- Promotion: `ArrowUpCircle` ✓
- Workflow: `CheckSquare` ✓
- Approval Gateway: `DoorOpen` ✓
- Approval Policies: `FileLock2` ✓
- Release Gates: `GitMerge` ✓
- Rollback: `RotateCcw` ✓

- [ ] **Step 5: Auditar `features/catalog` e `features/contracts`**

```bash
grep -rn "PageHeader" src/frontend/src/features/catalog/pages/ src/frontend/src/features/contracts/pages/ --include="*.tsx" | grep "icon="
```

Ícones esperados em catalog:
- Service Catalog: `Server` ✓
- Dependency Graph: `Share2` ✓
- Service Discovery: `Radar` ✓
- Score/Maturity: `Award` ✓
- Developer Portal: `BookMarked` ✓
- Feature Flags: `Sliders` ✓
- Legacy Assets: `Archive` ✓

Ícones esperados em contracts:
- Contract Catalog: `FileText` ✓
- Contract Pipeline: deve ser `GitBranch` após Wave 1
- Contracts Health: `Stethoscope` ✓
- CDCT: `FlaskConical` ✓
- Spectral Rulesets: `ListChecks` ✓
- Canonical Entities: deve ser `Boxes` após Wave 1
- Publication Center: `Send` ✓

- [ ] **Step 6: Auditar restantes módulos**

```bash
grep -rn "PageHeader" \
  src/frontend/src/features/audit-compliance/pages/ \
  src/frontend/src/features/identity-access/pages/ \
  src/frontend/src/features/integrations/pages/ \
  src/frontend/src/features/knowledge/pages/ \
  src/frontend/src/features/platform-admin/pages/ \
  --include="*.tsx" | grep "icon="
```

Para cada resultado, verifica o critério: o ícone é coerente com o conceito da página e não duplicado?

- [ ] **Step 7: Verificar build de todos os módulos**

```bash
cd src/frontend && npx tsc --noEmit 2>&1 | head -50
```

Expected: zero erros.

- [ ] **Step 8: Commit**

```bash
git add src/frontend/src/features/
git commit -m "feat(ui): wave2 — PageHeader icons aligned with sidebar across 19 modules"
```

---

## Wave 3 — Status indicators e ícones de acção

### Task 5: Consolidar status icons

**Files:**
- Modify: `src/frontend/src/features/**/*.tsx`, `src/frontend/src/components/*.tsx`

#### Regras de consolidação

| Contexto | Antes | Depois |
|---|---|---|
| Sucesso / OK | `CheckCircle` ou `CheckCircle2` | Sempre `CheckCircle2` |
| Falha hard / erro definitivo | `XCircle` ou `X` em badges | Sempre `XCircle` |
| Warning / atenção operacional | `AlertTriangle` | Manter |
| Info / aviso informativo (não perigo) | `AlertTriangle` usado em contexto não-critico | Substituir por `Info` |

- [ ] **Step 1: Encontrar todos os usos mistos de CheckCircle vs CheckCircle2**

```bash
grep -rn "CheckCircle[^2]" src/frontend/src/features/ --include="*.tsx" -l
```

Para cada ficheiro listado, abre-o e substitui `CheckCircle ` (com espaço — para não apanhar CheckCircle2) por `CheckCircle2 ` nas ocorrências dentro de JSX (`<CheckCircle` → `<CheckCircle2`). Actualiza também o import se necessário (adiciona `CheckCircle2`, remove `CheckCircle` se ficar sem usos).

- [ ] **Step 2: Encontrar usos de `AlertTriangle` em contextos não-críticos**

```bash
grep -rn "AlertTriangle" src/frontend/src/features/ --include="*.tsx" -B2 -A2 | grep -v "incidents\|error\|danger\|critical\|alert\|warning" | head -40
```

Para cada ocorrência que apareça em contextos puramente informativos (ex: empty states, help text, soft notices), substitui por `Info` da Lucide. Verifica o contexto antes de substituir — se a mensagem comunica perigo real, mantém `AlertTriangle`.

- [ ] **Step 3: Verificar build**

```bash
cd src/frontend && npx tsc --noEmit 2>&1 | head -50
```

Expected: zero erros.

- [ ] **Step 4: Commit**

```bash
git add src/frontend/src/features/ src/frontend/src/components/
git commit -m "feat(ui): wave3 — consolidate status icons (CheckCircle2, XCircle, AlertTriangle/Info)"
```

---

### Task 6: Audit de ícones de acção em tabelas e botões

**Files:**
- Modify: ficheiros relevantes em `src/frontend/src/features/**/*.tsx`

- [ ] **Step 1: Verificar consistência de botões de edição**

```bash
grep -rn "Pencil\|Edit2\|Edit3\|FilePen" src/frontend/src/features/ --include="*.tsx" -l
```

Escolhe um único ícone de edição: `Pencil` (mais simples, Lucide puro). Para cada ficheiro listado, unifica para `Pencil`. Actualiza imports.

- [ ] **Step 2: Verificar consistência de empty states**

```bash
grep -rn "InboxIcon\|PackageOpen\|FolderOpen\|SearchX" src/frontend/src/features/ --include="*.tsx" | head -20
```

Se houver mistura, unifica o ícone de "sem resultados" para `SearchX` (pesquisa sem resultados) ou `PackageOpen` (lista vazia). Usa `SearchX` quando o empty state resulta de um filtro/pesquisa; usa `PackageOpen` quando é simplesmente uma lista vazia.

- [ ] **Step 3: Build final e verificação**

```bash
cd src/frontend && npx tsc --noEmit 2>&1
```

Expected: zero erros de tipagem.

```bash
cd src/frontend && npm run lint 2>&1 | tail -20
```

Expected: zero erros de lint novos.

- [ ] **Step 4: Commit final**

```bash
git add src/frontend/src/features/ src/frontend/src/components/
git commit -m "feat(ui): wave3 — unify edit/empty-state action icons"
```

---

## Verificação final

- [ ] Abre a aplicação no browser e navega pelo sidebar — verifica que todos os ícones são distintos e coerentes
- [ ] Verifica a secção Operations no rail (deve mostrar `Activity`, não `AlertTriangle`)
- [ ] Verifica que "Custom Dashboards" tem ícone diferente do "Dashboard" home
- [ ] Verifica que "Model Governance" e "Canonical Entities" têm ícones distintos
- [ ] Verifica que "AI Agents" e "Domains" têm ícones distintos
