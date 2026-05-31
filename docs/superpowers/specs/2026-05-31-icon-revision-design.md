# Icon Revision — NexTraceOne Frontend

**Data:** 2026-05-31  
**Âmbito:** Aplicação completa (174 ícones únicos, 501 ficheiros TSX)  
**Biblioteca:** Lucide React (única — sem fragmentação)  
**Estilo alvo:** Strato puro — ícones monocromáticos subtis, barra de acento `#adb0ff` só no item ativo

---

## Objectivo

Tornar todos os ícones da aplicação coerentes com a sua funcionalidade, sem duplicados e alinhados com o sistema de design Strato. Não se altera a biblioteca (Lucide React permanece), nem o tratamento de cor (estilo C: monocromático com acento no ativo).

---

## Estratégia: 3 Vagas

### Vaga 1 — Sidebar (`AppSidebar.tsx`)

Foco nos ~80 itens de navegação e nos 10 ícones do rail de secções. Maior visibilidade — o utilizador vê estes ícones em cada página.

#### Duplicados a corrigir

| Item de nav | Ícone actual | Ícone proposto | Ficheiro |
|---|---|---|---|
| Model Governance (AI) | `Database` | `BrainCircuit` | `AppSidebar.tsx` |
| Canonical Entities (Contracts) | `Database` | `Boxes` | `AppSidebar.tsx` |
| AI Agents | `Network` | `Sparkles` | `AppSidebar.tsx` |
| Domains (Organization) | `Network` | `Globe` | `AppSidebar.tsx` |
| Custom Dashboards | `LayoutDashboard` | `LayoutGrid` | `AppSidebar.tsx` |
| Access Review | `ClipboardCheck` | `UserCheck` | `AppSidebar.tsx` |

#### Erros semânticos a corrigir

| Item de nav | Ícone actual | Ícone proposto | Motivo |
|---|---|---|---|
| Runbooks | `FileCode` | `ScrollText` | Runbooks são documentos operacionais, não código fonte |
| Synthetic Monitoring | `Radio` | `Gauge` | Radio = broadcasting; Gauge = medição/sonda |
| Contract Pipeline | `Zap` | `GitBranch` | Zap é velocidade genérica; GitBranch representa pipeline CI/CD |

#### Ícone de secção no rail

| Secção | Ícone actual | Ícone proposto | Motivo |
|---|---|---|---|
| Operations (rail) | `AlertTriangle` | `Activity` | AlertTriangle comunica perigo/erro; Activity comunica pulso operacional |

#### Ícones do rail que ficam inalterados

`home: LayoutDashboard` · `services: Server` · `contracts: FileText` · `changes: Zap` · `aiHub: Bot` · `governance: Briefcase` · `organization: Users` · `integrations: Cable` · `admin: Settings`

---

### Vaga 2 — PageHeaders dos 19 módulos

Auditoria dos ficheiros `features/*/pages/*.tsx`. Para cada módulo, verificar se o ícone no `<PageHeader icon={...}>` é coerente com o conceito do módulo.

**Módulos prioritários** (maior risco de ícone desalinhado):

| Módulo | Localização | Verificar |
|---|---|---|
| AI Hub | `features/ai-hub/pages/` | `Bot` vs `BrainCircuit` vs `Sparkles` — consistência interna |
| Governance | `features/governance/pages/` | Distinguir reports, compliance, risk, dashboards |
| Operations | `features/operations/pages/` | Distinguir incidents, SLOs, traces, runbooks |
| Change Governance | `features/change-governance/pages/` | Distinguir pipeline, gates, rollback |
| Audit & Compliance | `features/audit-compliance/pages/` | Distinguir audit trail, compliance frameworks |
| Catalog | `features/catalog/pages/` | Distinguir catalog, portal, graph, discovery |

**Critério de aprovação:** nenhum módulo usa o mesmo ícone que outro módulo vizível no mesmo contexto.

---

### Vaga 3 — Tabelas, badges, status indicators

#### Consolidação de status icons

| Contexto | Estado actual | Estado alvo |
|---|---|---|
| Sucesso | Mix de `CheckCircle` e `CheckCircle2` | Sempre `CheckCircle2` (filled, mais legível a tamanhos pequenos) |
| Falha hard | Mix de `X`, `XCircle`, `AlertTriangle` | Sempre `XCircle` para falha definitiva |
| Warning / atenção | `AlertTriangle` (usado 80×) | `AlertTriangle` apenas para alertas reais; `Info` para avisos informativos |
| Carregamento / refresh | `RefreshCw` (45×) | Manter — uso consistente confirmado |

#### Regras de acção

- Botões destrutivos (delete, remove): `Trash2` — já consistente, manter  
- Botões de adicionar: `Plus` — já consistente, manter  
- Botões de editar: `Pencil` ou `Edit2` — auditar e unificar  
- Botões de fechar/dismiss: `X` — manter  

---

## Ficheiros afectados

| Vaga | Ficheiros principais |
|---|---|
| 1 | `src/frontend/src/components/shell/AppSidebar.tsx` |
| 2 | `src/frontend/src/features/*/pages/*.tsx` (19 módulos) |
| 3 | `src/frontend/src/features/*/pages/*.tsx`, `src/frontend/src/features/*/components/*.tsx`, `src/frontend/src/components/*.tsx` |

---

## Critérios de sucesso

1. Zero ícones duplicados entre itens de navegação de contextos diferentes
2. Cada ícone de PageHeader é único por módulo (no mesmo ecrã do rail)
3. Status icons seguem a hierarquia: `CheckCircle2` → sucesso · `XCircle` → falha · `AlertTriangle` → warning · `Info` → informativo
4. Nenhuma regressão de TypeScript (todos os novos ícones existem em `lucide-react`)
5. Builds limpos sem warnings

---

## Fora de âmbito

- Alterar a biblioteca de ícones (Lucide permanece)
- Adicionar cor aos ícones (estilo Strato puro mantido — monocromático)
- Criar ícones SVG customizados (excepto os já existentes para service kinds no `TraceExplorerPage`)
- Alterar tamanhos de ícones (18px nav, 16px tabelas, 14px badges)
