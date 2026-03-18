# NexTraceOne — Plano de Teste Funcional (Fase 8)

> **Documento de referência:** `docs/acceptance/NexTraceOne_Escopo_Homologavel.md`
> **Plano operacional:** `docs/planos/NexTraceOne_Plano_Operacional_Finalizacao.md`
> **Ordem de execução:** conforme Fase 8 do plano operacional

---

## 1. Objectivo

Validar com confiança o escopo homologável do NexTraceOne, seguindo a ordem recomendada da Fase 8. Cada secção contém casos de teste funcionais com resultado esperado. Bugs encontrados devem ser classificados (P0/P1/P2) e reportados.

---

## 2. Pré-requisitos

- Backend a correr (`dotnet run` no ApiHost)
- Frontend a correr (`npm run dev`)
- PostgreSQL com migrations aplicadas (auto-aplicadas ao arrancar o ApiHost)
- Seed data carregado (auto-carregado em Development)
- Browser moderno (Chrome/Edge/Firefox)

---

## 3. Ordem de teste

### 3.1 Login / Tenant / Auth

| # | Caso de teste | Passos | Resultado esperado |
|---|--------------|--------|-------------------|
| A01 | Login válido | Aceder `/login`, inserir `admin@nextraceone.dev` / `Admin@123`, submeter | Redirecionado para `/select-tenant` |
| A02 | Selecção de tenant | Na página de tenants, selecionar "NexTrace Corp" | Redirecionado para `/` (Dashboard) |
| A03 | Login inválido | Inserir credenciais erradas | Mensagem de erro visível, sem crash |
| A04 | Rota protegida sem sessão | Aceder `/services` sem login | Redirecionado para `/login` |
| A05 | Rota não autorizada | Aceder rota sem permissão | Redirecionado para `/unauthorized` |
| A06 | Logout | Clicar logout na sidebar/topbar | Sessão terminada, redirecionado para `/login` |
| A07 | Múltiplos utilizadores | Login como `techlead@nextraceone.dev`, `dev@nextraceone.dev`, `auditor@nextraceone.dev` | Todos entram sem erro |
| A08 | Forgot password | Aceder `/forgot-password` | Página carrega sem erro |
| A09 | Activation page | Aceder `/activate` | Página carrega sem erro |
| A10 | MFA page | Aceder `/mfa` | Página carrega sem erro |

### 3.2 Shell / Navegação

| # | Caso de teste | Passos | Resultado esperado |
|---|--------------|--------|-------------------|
| B01 | Sidebar visível | Após login, sidebar aparece | Grupos de menu organizados por secção |
| B02 | Sidebar collapse | Clicar botão de colapsar sidebar | Sidebar colapsa para ícones, expande de volta |
| B03 | Navegação entre módulos | Clicar em Services, Contracts, Changes, Incidents, Audit | Cada módulo carrega sem crash |
| B04 | Preview badge | Itens Preview na sidebar | Mostram badge amber "Preview" |
| B05 | Preview gate | Aceder rota Preview (ex: `/governance/reports`) | Banner de Preview visível no topo da página |
| B06 | Lazy loading | Navegar para qualquer página protegida | Spinner de loading aparece brevemente antes da página |
| B07 | Deep link | Aceder directamente `/services` via URL | Página carrega correctamente (com auth) |
| B08 | Breadcrumbs | Navegar para sub-páginas | Breadcrumbs reflectem localização |
| B09 | Catch-all route | Aceder rota inexistente (`/xyz`) | Redirecionado para `/` |

### 3.3 Dashboard

| # | Caso de teste | Passos | Resultado esperado |
|---|--------------|--------|-------------------|
| C01 | Dashboard carrega | Aceder `/` após login | Dashboard renderiza sem erro |
| C02 | Widgets com dados | Verificar cards/widgets | Dados carregados do backend (ou estados vazios coerentes) |
| C03 | Quick actions | Verificar acções rápidas, se existirem | Links funcionam e levam ao destino correcto |

### 3.4 Service Catalog & Source of Truth

| # | Caso de teste | Passos | Resultado esperado |
|---|--------------|--------|-------------------|
| D01 | Listagem de serviços | Aceder `/services` | Lista de serviços carregada do backend |
| D02 | Filtros/pesquisa | Usar campo de pesquisa ou filtros | Lista filtra correctamente |
| D03 | Detalhe de serviço | Clicar num serviço | Página de detalhe carrega com informação do serviço |
| D04 | Link Source of Truth | No detalhe do serviço, verificar link para SoT | Link leva a `/source-of-truth/services/:id` |
| D05 | Grafo de dependências | Aceder `/services/graph` | Grafo renderiza com nós e arestas |
| D06 | Source of Truth Explorer | Aceder `/source-of-truth` | Explorador carrega com serviços e contratos |
| D07 | SoT Serviço | Aceder `/source-of-truth/services/:serviceId` | Página de verdade do serviço com dados |
| D08 | SoT Contrato | Aceder `/source-of-truth/contracts/:contractVersionId` | Página de verdade do contrato com dados |
| D09 | Global Search | Aceder `/search`, pesquisar termo | Resultados relevantes aparecem |
| D10 | Loading state | Verificar loading em carga lenta | Componente `PageLoadingState` aparece |
| D11 | Error state | Simular falha de API (backend parado) | Componente `PageErrorState` aparece com mensagem |

### 3.5 Contracts / Draft Studio / Workspace

| # | Caso de teste | Passos | Resultado esperado |
|---|--------------|--------|-------------------|
| E01 | Catálogo de contratos | Aceder `/contracts` | Lista de contratos carregada |
| E02 | Filtros/pesquisa | Usar filtros no catálogo | Lista filtra correctamente |
| E03 | Criar contrato | Aceder `/contracts/new`, preencher formulário | Formulário de criação funciona, redireciona para studio |
| E04 | Draft Studio | Aceder `/contracts/studio/:draftId` | Editor de draft carrega com dados |
| E05 | Workspace | Aceder `/contracts/:contractVersionId` | Workspace do contrato carrega com tabs/secções |
| E06 | Contract Portal | Aceder `/contracts/:contractVersionId/portal` | Portal do contrato carrega |
| E07 | Contract Governance | Aceder `/contracts/governance` | Página de governança carrega com dados |
| E08 | Loading/Error states | Verificar estados em cada página | PageLoadingState e PageErrorState consistentes |

### 3.6 Change Governance

| # | Caso de teste | Passos | Resultado esperado |
|---|--------------|--------|-------------------|
| F01 | Catálogo de mudanças | Aceder `/changes` | Lista de mudanças carregada |
| F02 | Detalhe de mudança | Clicar numa mudança | Página de detalhe com timeline, correlação, blast radius |
| F03 | Releases | Aceder `/releases` | Lista de releases carregada |
| F04 | Workflow | Aceder `/workflow` | Página de workflow carrega |
| F05 | Promotion | Aceder `/promotion` | Página de promoção carrega |
| F06 | Filtros | Usar filtros nas listagens | Resultados filtram correctamente |
| F07 | Loading/Error states | Verificar estados | Consistentes com padrão |

### 3.7 Operations — Incidents

| # | Caso de teste | Passos | Resultado esperado |
|---|--------------|--------|-------------------|
| G01 | Listagem de incidentes | Aceder `/operations/incidents` | Lista de incidentes carregada |
| G02 | Filtros/stats | Verificar filtros e estatísticas | Dados coerentes |
| G03 | Detalhe de incidente | Clicar num incidente | Página com timeline, correlação, evidência, mitigação, runbooks, contratos |
| G04 | Badges de severidade | Verificar badges no detalhe | Severity, Status, Correlation Confidence, Mitigation Status visíveis |
| G05 | Serviços vinculados | Verificar secção de serviços impactados | Links para `/services/:id` funcionam |
| G06 | Contratos relacionados | Verificar secção de contratos | Links para `/contracts/:id` funcionam |
| G07 | Runbooks | Verificar secção de runbooks | Runbooks listados com links externos |
| G08 | AI Assistant Panel | Verificar painel de IA no detalhe | Painel carrega com contexto do incidente |
| G09 | Runbooks page | Aceder `/operations/runbooks` | Página de runbooks carrega |
| G10 | Loading/Error states | Verificar estados | Consistentes com padrão |

### 3.8 Audit

| # | Caso de teste | Passos | Resultado esperado |
|---|--------------|--------|-------------------|
| H01 | Listagem de auditoria | Aceder `/audit` | Lista de eventos de auditoria carregada |
| H02 | Filtros | Verificar filtros disponíveis | Filtram correctamente |
| H03 | Paginação | Navegar entre páginas, se disponível | Paginação funciona |
| H04 | Loading/Error states | Verificar estados | Consistentes com padrão |

### 3.9 Identity Admin

| # | Caso de teste | Passos | Resultado esperado |
|---|--------------|--------|-------------------|
| I01 | Users page | Aceder `/users` | Lista de utilizadores carregada |
| I02 | Break-glass | Aceder `/break-glass` | Página carrega, formulário funcional |
| I03 | JIT Access | Aceder `/jit-access` | Página carrega |
| I04 | Delegations | Aceder `/delegations` | Página carrega |
| I05 | Access Reviews | Aceder `/access-reviews` | Página carrega |
| I06 | My Sessions | Aceder `/my-sessions` | Página carrega |
| I07 | Loading/Error states | Verificar estados | Consistentes com padrão |

### 3.10 AI Hub (parcial)

| # | Caso de teste | Passos | Resultado esperado |
|---|--------------|--------|-------------------|
| J01 | AI Assistant | Aceder `/ai/assistant` | Página carrega, chat funcional |
| J02 | Contextual AI | Verificar Assistant Panel em detalhe de incidente | Painel com contexto preenchido |

### 3.11 Platform Operations

| # | Caso de teste | Passos | Resultado esperado |
|---|--------------|--------|-------------------|
| K01 | Platform Ops | Aceder `/platform/operations` | Página carrega com informações da plataforma |

---

## 4. Classificação de bugs

| Prioridade | Definição | Acção |
|-----------|-----------|-------|
| **P0** | Bloqueia fluxo principal (crash, loop infinito, dados corrompidos) | Corrigir antes de continuar |
| **P1** | Funcionalidade importante não funciona (filtro quebrado, link errado) | Corrigir na Fase 9 |
| **P2** | Issue menor de UX/visual (alinhamento, texto, cor) | Backlog pós-aceite |

---

## 5. Saídas esperadas (Fase 8)

- Bugs classificados por prioridade (P0/P1/P2)
- Gaps reais de produto identificados
- Ajustes de UX necessários documentados
- Backlog pós-aceite criado
- Decisão de pronto ou não pronto **por módulo**

---

## 6. Personas de teste recomendadas

| Utilizador | Persona | Objectivo do teste |
|-----------|---------|-------------------|
| admin@nextraceone.dev | PlatformAdmin | Acesso total, testar todas as rotas |
| techlead@nextraceone.dev | TechLead | Fluxos de serviços, contratos, mudanças |
| dev@nextraceone.dev | Developer | Fluxos de catálogo, criação de contratos |
| auditor@nextraceone.dev | Auditor | Fluxos de auditoria, compliance, leitura |
