# NexTraceOne Selenium UI Navigation Tests

Projeto de testes automatizados com **Selenium WebDriver** para validação rápida de todas as páginas do frontend NexTraceOne.

## Objetivo

Detetar rapidamente bugs de UI em todas as páginas:
- Páginas que não carregam (crash, error boundary)
- Redirects inesperados para `/unauthorized` ou `/login`
- Erros graves de JavaScript na consola do browser
- Módulos inacessíveis ou com lazy-load partido

## Estrutura

```
NexTraceOne.Selenium.Tests/
├── Infrastructure/
│   ├── BrowserFixture.cs          # Gestão do ciclo de vida do ChromeDriver
│   ├── SeleniumCollection.cs      # xUnit collection para partilhar browser
│   ├── SeleniumSettings.cs        # Configuração via variáveis de ambiente
│   └── SeleniumTestBase.cs        # Classe base com helpers comuns
├── Modules/
│   ├── AuthNavigationTests.cs     # Login, forgot-password, MFA, activation
│   ├── DashboardNavigationTests.cs # Home / Dashboard
│   ├── CatalogNavigationTests.cs  # Service Catalog, Source of Truth, Search
│   ├── ContractsNavigationTests.cs # Contracts, Studio, Governance, Publication
│   ├── KnowledgeNavigationTests.cs # Knowledge Hub, Documents, Notes
│   ├── ChangesNavigationTests.cs  # Changes, Releases, Workflow, Promotion
│   ├── OperationsNavigationTests.cs # Incidents, Runbooks, Reliability, Automation
│   ├── AiHubNavigationTests.cs    # AI Assistant, Models, Policies, Agents
│   ├── GovernanceNavigationTests.cs # Executive, Risk, FinOps, Compliance, Teams
│   ├── AdminNavigationTests.cs    # Users, Audit, Notifications, Config, Integrations
│   └── FullSmokeNavigationTests.cs # Smoke test parameterizado com TODAS as rotas
└── README.md
```

## Pré-requisitos

1. **Google Chrome** instalado
2. **Frontend em execução** (por padrão em `http://localhost:4173`)
3. **.NET 10 SDK**

## Como executar

### Iniciar o frontend (preview build)
```bash
cd src/frontend
npm run build && npm run preview
```

### Executar todos os testes
```bash
cd tests/platform/NexTraceOne.Selenium.Tests
dotnet test
```

### Executar apenas o smoke test completo
```bash
dotnet test --filter "FullyQualifiedName~FullSmokeNavigationTests"
```

### Executar testes de um módulo específico
```bash
dotnet test --filter "FullyQualifiedName~ContractsNavigationTests"
dotnet test --filter "FullyQualifiedName~OperationsNavigationTests"
dotnet test --filter "FullyQualifiedName~GovernanceNavigationTests"
```

## Configuração via variáveis de ambiente

| Variável                     | Padrão                    | Descrição                         |
|------------------------------|---------------------------|-----------------------------------|
| `NXT_SELENIUM_BASE_URL`      | `http://localhost:4173`   | URL base do frontend              |
| `NXT_SELENIUM_TIMEOUT`       | `15`                      | Timeout de espera (segundos)      |
| `NXT_SELENIUM_HEADLESS`      | `true`                    | `false` para ver o browser abrir  |
| `NXT_SELENIUM_SCREENSHOT_DIR`| `<output>/screenshots`    | Directoria para screenshots       |

### Exemplo: executar com browser visível
```powershell
$env:NXT_SELENIUM_HEADLESS = "false"
dotnet test --filter "FullyQualifiedName~DashboardNavigationTests"
```

## O que cada teste verifica

1. **Page Load** — a página carrega sem timeout
2. **No Error Boundary** — não aparece ecrã de erro React
3. **No Unauthorized Redirect** — com sessão Admin, não redireciona para `/unauthorized`
4. **No JS Errors** — a consola do browser não tem erros `SEVERE`
5. **Screenshots** — captura automática em caso de falha para diagnóstico rápido

## Cobertura de rotas

O projeto cobre **100+ rotas** distribuídas por todos os módulos:

- **Auth**: 7 rotas (login, forgot-password, reset-password, activate, mfa, invitation, select-tenant)
- **Dashboard**: 1 rota
- **Catalog**: 9 rotas (search, source-of-truth, services, graph, legacy, discovery, maturity, portal)
- **Contracts**: 9 rotas (catalog, new, governance, spectral, canonical, publication, workspace, portal, studio)
- **Knowledge**: 3 rotas (hub, documents, notes)
- **Changes**: 6 rotas (catalog, detail, releases, workflow, promotion, calendar)
- **Operations**: 12 rotas (incidents, timeline, detail, runbooks, reliability, SLOs, automation, admin, comparison, platform)
- **AI Hub**: 11 rotas (assistant, models, policies, routing, IDE, budgets, audit, agents, detail, analysis, config)
- **Governance**: 21 rotas (executive, drilldown, finops, reports, compliance, risk, heatmap, policies, controls, evidence, maturity, benchmarking, teams, domains, packs, waivers, delegated-admin, configuration)
- **Admin**: 25 rotas (users, environments, break-glass, JIT, delegations, access-reviews, sessions, audit, notifications, configuration, integrations, analytics)
