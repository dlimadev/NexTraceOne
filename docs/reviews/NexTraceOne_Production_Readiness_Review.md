# NexTraceOne Production Readiness Review

## 1. Resumo Executivo

### Estado real
- **Solução inteira:** `BLOQUEADO`
- **Backend:** `PARCIAL`
- **Frontend:** `BLOQUEADO`

O repositório mostra uma base tecnicamente séria em arquitetura modular, separação por camadas, uso consistente de `MediatR`, `Result`, `EF Core`, permissões por endpoint e múltiplos `DbContexts` com migrações próprias. O backend compila integralmente.

Isso não é suficiente para classificar o sistema como pronto para produção.

Os principais bloqueadores encontrados no código real são:
1. **Frontend não gera build de produção** por erros de sintaxe/JSX em páginas reais.
2. **`Ingestion.Api` expõe endpoints públicos sem autenticação nem autorização**, apesar do comentário afirmar API key.
3. **CORS é configurado, mas não é aplicado no pipeline** do `ApiHost`.
4. **Health checks de readiness/liveness são sintéticos e sempre saudáveis**, sem validar base de dados, filas ou workers reais.
5. **Ainda existem handlers e páginas com dados mockados/simulados** em fluxos que parecem produtivos.
6. **E2E real em .NET inexiste** (`PlaceholderTests`) e os Playwright usam forte interceptação/mock de APIs.
7. **O frontend ainda usa `sessionStorage` para access token**, e o fluxo mais seguro por cookie `httpOnly` existe mas está desativado e não adotado pelo frontend.

### Pontos fortes reais
- Arquitetura de monólito modular bem organizada por bounded context.
- `ApiHost` com composição clara dos módulos.
- Forte presença de `StronglyTypedIds`, `ValueObjects` e agregados em `IdentityAccess`, `Catalog.Contracts` e partes de `AIKnowledge`.
- Permissões por endpoint via `RequirePermission(...)` em boa parte das APIs.
- Migrações existentes para todos os `DbContexts` identificados.
- `IdentityAccess.Tests` e `Catalog.Tests` com bom volume e execução bem-sucedida.
- `IncidentRecord` usa `jsonb` corretamente no mapeamento EF para coleções complexas.
- `AI Runtime` possui fallback explícito e controlado quando provider externo falha.

### Conclusão curta
**O NexTraceOne ainda não está pronto para produção séria.**
O backend tem partes maduras, mas a solução como produto enterprise está travada por build quebrado do frontend, gaps de segurança reais, observabilidade enganosa e presença de fluxos mockados em áreas relevantes.

---

## 2. Veredicto Geral

| Escopo | Classificação | Veredicto |
|---|---|---|
| Solução inteira | `BLOQUEADO` | Não apta para produção. Há blockers técnicos objetivos no código. |
| Backend | `PARCIAL` | Estrutura forte, mas ainda com mocks, segurança inconsistente e operação incompleta. |
| Frontend | `BLOQUEADO` | Build de produção falha. Há páginas quebradas e módulos preview/mock. |
| Plataforma operacional | `FRÁGIL` | Health checks sintéticos, E2E insuficiente, ingestão sem proteção e workers limitados. |

### Go/No-Go atual
- **Decisão:** `NO-GO`
- **Motivos de bloqueio:** build frontend quebrado, ingestão pública sem auth, CORS não aplicado, readiness falso, fluxos mockados em módulos relevantes.

---

## 3. Avaliação por Frente

### FRENTE 1 — Visão Geral da Solução
**Classificação:** `PARCIAL`

#### O que está bom
- Estrutura modular clara por contexto:
  - `IdentityAccess`
  - `Catalog`
  - `ChangeGovernance`
  - `OperationalIntelligence`
  - `AuditCompliance`
  - `AIKnowledge`
  - `Governance`
- Separação consistente em `Domain`, `Application`, `Infrastructure`, `API`.
- `ApiHost` centraliza composição e mapeamento automático de endpoint modules via reflection.
- Há projetos separados para `ApiHost`, `BackgroundWorkers`, `Ingestion.Api` e `CLI`.

#### O que está fraco
- Existe heterogeneidade clara de maturidade entre módulos.
- Alguns módulos parecem maduros na arquitetura, mas ainda contêm fluxos simulados:
  - `Governance.Application.Features.SimulateGovernancePack`
  - `Catalog.Application.Portal.Features.ExecutePlayground`
  - `OperationalIntelligence.Application.Automation.Features.ListAutomationWorkflows`
- Há ficheiros “casca”/legado vazios, como `GovernanceEndpointModule.cs` e `CatalogGraphEndpointModule.cs`, o que não quebra o host, mas sugere evolução incompleta.

#### Riscos reais
- `Ingestion.Api` viola a arquitetura padrão ao operar diretamente com repositórios e entidades de `Governance`, sem passar por `Application`/handlers.
- `CLI` é apenas stub com `TODO`s e sem comandos reais.
- `BackgroundWorkers` cobrem essencialmente `Identity`/outbox, mas a narrativa dos comentários é mais ampla do que a implementação real.

#### Veredicto
A solução está **bem estruturada**, mas **não uniformemente madura**. O risco aqui não é caos arquitetural; é **falsa sensação de completude**.

---

### FRENTE 2 — Domain Model e Entidades
**Classificação:** `PARCIAL`

#### Pontos fortes
- `IdentityAccess.User` protege invariantes básicas de autenticação, lockout e ativação.
- `IdentityAccess.Session` protege rotação e revogação de refresh token.
- `Catalog.Contracts.ContractVersion` tem máquina de estados explícita e operações de negócio coerentes.
- `AuditCompliance.AuditEvent` é imutável após criação, o que é adequado ao domínio.
- `AIKnowledge.AIModel` mantém regras mínimas de registro e estado.

#### Pontos fracos
- Em vários módulos, os agregados são muito mais “data holders” do que agregados ricos:
  - `Catalog.Graph.ServiceAsset`
  - `ChangeGovernance.ChangeIntelligence.Release`
  - `Governance.GovernancePack`
- `OperationalIntelligence.IncidentRecord` concentra grande volume de campos `Json`/`Jsonb`, aproximando-se de um “document aggregate” pouco coeso.
- Alguns domínios ainda usam `string` para conceitos que deveriam ser modelados de forma mais forte (`ServiceId`, `Environment`, `Domain`, ownership, etc.).
- Há referências cross-context por IDs primitivos (`Guid ApiAssetId`, `string ServiceId`) sem contratos de domínio explícitos.

#### Risco de anemic domain model
- **Baixo a médio** em `IdentityAccess` e `Catalog.Contracts`.
- **Médio a alto** em `Governance`, `Catalog.Graph`, `OperationalIntelligence` e partes de `ChangeGovernance`.

#### Entidades/modelos críticos auditados

##### `IdentityAccess.User`
- **Bom:** encapsulamento razoável, lockout, `ValueObjects` (`Email`, `FullName`, `HashedPassword`).
- **Fraco:** invariantes de perfil e identidade federada ainda simples.
- **Risco de produção:** baixo.

##### `IdentityAccess.Session`
- **Bom:** refresh token persistido como hash, rotação e revogação explícitas.
- **Fraco:** sem amarração adicional a fingerprint/dispositivo além de IP/user-agent textual.
- **Risco de produção:** médio.

##### `Catalog.Graph.ServiceAsset`
- **Bom:** identidade e ownership centrais.
- **Fraco:** modelo muito string-based e com pouca regra de domínio.
- **Risco:** médio; domínio pode degradar para catálogo anêmico.

##### `Catalog.Contracts.ContractVersion`
- **Bom:** melhor agregado do repositório auditado; lifecycle, lock, sign, deprecate.
- **Fraco:** `SemVer` fica como string pública; há espaço para internalizar mais regras em `ValueObject` persistido.
- **Risco:** baixo a médio.

##### `ChangeGovernance.Release`
- **Bom:** transições de status e rollback explícitos.
- **Fraco:** modelo ainda muito próximo de registro operacional com pouca inteligência interna.
- **Risco:** médio.

##### `OperationalIntelligence.IncidentRecord`
- **Bom:** modela o agregado operacional completo e usa `jsonb` para estrutura variável.
- **Fraco:** excesso de campos textuais/JSON; fronteira agregada larga demais.
- **Risco:** médio a alto para evolução, consistência e manutenção.

##### `Governance.GovernancePack`
- **Bom:** nome técnico imutável e lifecycle simples.
- **Fraco:** quase não há regras de negócio profundas; usa `DateTimeOffset.UtcNow` direto.
- **Risco:** médio.

##### `AuditCompliance.AuditEvent`
- **Bom:** imutabilidade e cadeia opcional de hash.
- **Fraco:** payload livre em string, sem política de schema/mascaramento forte no próprio agregado.
- **Risco:** médio.

---

### FRENTE 3 — Application Layer
**Classificação:** `PARCIAL`

#### O que está bom
- Estrutura VSA recorrente (`Command/Query + Validator + Handler + Response`) consistente.
- Uso frequente de `FluentValidation` e `Result<T>`.
- `TransactionBehavior` realiza `CommitAsync` automático para comandos bem-sucedidos.
- `TenantIsolationBehavior` impede execução sem tenant em requests não públicos.

#### O que está fraco
- Há handlers produtivos convivendo com handlers claramente simulados.
- Parte de `Governance` devolve dados reais do próprio módulo, mas métricas cross-module voltam neutras/zeradas.
- Alguns handlers paginam de forma incorreta:
  - `ListIncidents` devolve `TotalCount = items.Count` já paginado.
  - `ListAutomationWorkflows` devolve `TotalCount = items.Count` da lista após paginação.

#### Casos concretos
- `Governance.Application.Features.SimulateGovernancePack` → **mockado** com lista fixa.
- `Catalog.Application.Portal.Features.ExecutePlayground` → **mockado** com body e duração artificiais.
- `OperationalIntelligence.Application.Automation.Features.ListAutomationWorkflows` → **mockado** com dados estáticos.
- `Governance.Application.Features.GetExecutiveOverview` → **parcial**, com métricas cross-module explicitamente neutras (`0`).

#### Veredicto
A camada Application é **consistente como padrão**, mas **não confiável como indicador de completude funcional**. Há vários casos de uso ainda mockados ou parciais em fluxos de negócio relevantes.

---

### FRENTE 4 — Infrastructure Layer
**Classificação:** `PARCIAL`

#### Pontos fortes
- Todos os `DbContexts` identificados têm pasta de migração.
- Uso consistente de `AuditInterceptor` e `TenantRlsInterceptor` nos módulos auditados.
- `jsonb` usado corretamente em `OperationalIntelligence.Incidents`.
- `EfIncidentStore` substitui o store in-memory em runtime real do módulo de incidentes.

#### Pontos fracos
- Muitos `DependencyInjection.cs` usam fallback para connection string local com `postgres/postgres` hardcoded.
- Health/readiness não validam infraestrutura real.
- Seeds existem só para parte dos contextos e com foco forte em ambiente de desenvolvimento.
- `DevelopmentSeedDataExtensions` captura erro e continua, ocultando ambientes parcialmente preparados.

#### Contextos “meio integrados”
- `AI Runtime` não possui `DbContext` próprio e depende de runtime services; isso é aceitável, mas reduz rastreabilidade estrutural do módulo como “subdomínio persistido”.
- `Governance`, `Automation`, `Playground`, `Simulation` e algumas telas front podem aparentar integração completa, mais parte do comportamento ainda é local/mock.

#### Risco de performance
- Não foram encontrados sinais graves de N+1 nos ficheiros auditados mais críticos.
- Há risco de payload grande e consultas pesadas por causa de agregados JSON extensos (`IncidentRecord`).

---

### FRENTE 5 — API Layer e Contratos
**Classificação:** `PARCIAL`

#### O que está bom
- Endpoints Minimal API com organização por módulo.
- Bom uso de `RequirePermission(...)` em `Identity`, `Catalog`, `Contracts`, `AI`, `Operational Runtime`.
- Contratos tendem a ser explícitos e finos na API, delegando a handlers.

#### Gaps e riscos
- **`Ingestion.Api` está sem autenticação/autorização** e sem `AddBuildingBlocksSecurity`. Os endpoints de ingestão ficam públicos.
- `Ingestion.Api` também não usa a camada `Application`; orquestra domínio e persistência diretamente no `Program.cs`.
- `ApiHost` configura CORS, mas **não chama `app.UseCors()`**; o contrato cross-origin com o frontend não está efetivamente aplicado.
- Existem endpoints reais para muitos fluxos, mas o frontend ainda não consome todos de forma produtiva; algumas páginas usam mock local.

#### Fluxos auditados
- **Login/Auth/Session:** implementado no backend; frontend ainda usa bearer em `sessionStorage`; cookie session existe mas está desligado.
- **Catalog/Services/Source of Truth:** backend bem mapeado.
- **Contracts/Service Studio:** backend bom; frontend ainda usa enriquecimento mock em `studioMock.ts`.
- **Change Governance:** backend com boa cobertura de endpoints, mas frontend contém páginas preview/mock e testes frágeis.
- **Incidents:** backend com `EfIncidentStore`; automação ainda tem dados simulados.
- **AI Assistant:** backend existe e é mais maduro do que o frontend sugere.
- **Governance core / analytics / integrations:** existe backend, mas parte da experiência e das métricas está parcial.

---

### FRENTE 6 — Database, Migrations e Prontidão de Dados
**Classificação:** `QUASE PRONTO` para schema, `PARCIAL` para validação operacional

#### Achado principal
Todos os `DbContexts` descobertos possuem migrações. Isso é positivo.

#### Limitação importante
Nesta revisão, **não foi possível validar a aplicação real de todas as migrações em PostgreSQL**, porque os testes de integração dependem de Docker/Testcontainers e falharam no ambiente atual. Além disso, a própria fixture de integração cobre apenas 5 contextos, não todos os 16.

#### Observações
- O `ApiHost` tenta migrar 16 contextos no startup quando `Development` ou `NEXTRACE_AUTO_MIGRATE=true`.
- Seeds existem apenas para 7 bases.
- Isso significa: **schema coverage está boa; readiness de dados completa ainda não está comprovada end-to-end**.

---

### FRENTE 7 — Frontend
**Classificação:** `BLOQUEADO`

#### Bloqueadores objetivos
O frontend **não gera build de produção**:
- `src/frontend/src/features/ai-hub/pages/IdeIntegrationsPage.tsx:398`
- `src/frontend/src/features/governance/pages/ExecutiveDrillDownPage.tsx:138`
- `src/frontend/src/features/governance/pages/TeamDetailPage.tsx:443`

Isso, por si só, já bloqueia produção.

#### O que está bom
- Estrutura por features bem organizada.
- Uso disseminado de `React Query`, `AuthContext`, `PersonaContext`, shell modular e lazy loading.
- Há testes de frontend em volume razoável.
- Há `PreviewGate` e `PreviewBanner`, o que ao menos sinaliza parte do escopo não homologado.

#### O que está fraco
- Ainda existem mocks relevantes no frontend:
  - `PackSimulationPage.tsx` usa `mockSimulation` local.
  - `studioMock.ts` enriquece contratos com dados artificiais.
- Há páginas preview/experimental em rotas reais.
- Os testes Vitest não protegem todas as páginas; o build falhou em páginas que passaram fora do radar.
- O fluxo de autenticação web continua dependente de `sessionStorage` para access token.

#### Estado de loading/error/empty
- Em páginas amostradas, há padrões de loading/error/empty bem encaminhados.
- Isso não compensa os blockers de build nem a presença de páginas mockadas.

---

### FRENTE 8 — Segurança
**Classificação:** `FRÁGIL`

#### Achados

##### 1. Ingestion API sem autenticação — `CRÍTICO`
- O comentário promete API key e isolamento.
- O código real não configura `AddBuildingBlocksSecurity`, `UseAuthentication` nem `RequireAuthorization`.
- Resultado: endpoints de ingestão ficam públicos.

##### 2. CORS configurado mas não aplicado — `ALTO`
- `AddCorsConfiguration()` é chamado.
- `app.UseCors()` não é chamado no pipeline do `ApiHost`.
- Em deployment separado frontend/backend, isto quebra integração e invalida parte do modelo de sessão/cookies planejado.

##### 3. Access token em `sessionStorage` — `ALTO`
- O próprio código reconhece que o modelo seguro ideal é cookie `httpOnly`.
- O frontend continua com bearer em JS-accessible storage.
- O fluxo por cookie existe, mas está behind feature flag e não adotado pelo SPA.

##### 4. Health checks de segurança/infra enganosos — `ALTO`
- Readiness informa “database healthy” e “background jobs operational” sem verificar nada real.
- Isso compromete operação e resposta a incidente.

##### 5. Connection string fallback com credenciais padrão — `MÉDIO`
- Vários módulos caem para `Host=localhost;Database=nextraceone;Username=postgres;Password=postgres`.
- É risco de configuração insegura e de bootstrap incorreto.

##### 6. Proxy awareness ausente — `MÉDIO`
- Rate limiting usa IP remoto, mas não há `UseForwardedHeaders()`.
- Atrás de proxy/reverse proxy, a limitação por IP e logging podem ficar incorretos.

##### 7. Cookie session segura existe, mas incompleta na adoção — `MÉDIO`
- O backend tem modelo de cookie + CSRF.
- O frontend produtivo não usa esse fluxo.
- Resultado: a mitigação mais forte existe no código, mas não protege o produto atual.

#### O que está bom
- JWT validation bem configurada no `BuildingBlocks.Security`.
- Policies dinâmicas por permissão.
- `TenantResolutionMiddleware` e `TenantIsolationBehavior` existem e cooperam.
- Refresh token é persistido como hash no domínio `IdentityAccess`.

---

### FRENTE 9 — Testes e Qualidade
**Classificação:** `PARCIAL`

#### Evidência executada
- `run_build` → sucesso.
- `IdentityAccess.Tests` → **186/186** OK.
- `Catalog.Tests` → **466/466** OK.
- `Governance.Tests` → **23/23** OK.
- Frontend `npm test` → **falhou** com 4 testes em 2 ficheiros.
- `IntegrationTests` → falharam por indisponibilidade de Docker/Testcontainers no ambiente.

#### Achados
- As suites unitárias de `IdentityAccess` e `Catalog` são fortes.
- `Governance.Tests` é pequena para o tamanho funcional declarado do módulo.
- `tests/platform/NexTraceOne.E2E.Tests` contém apenas `PlaceholderTests`.
- Os Playwright existem, mas interceptam APIs e simulam sessão; isso é **UI-flow testado com mocks**, não E2E real de sistema.
- A fixture de integração cobre apenas:
  - `CatalogGraphDbContext`
  - `ContractsDbContext`
  - `ChangeIntelligenceDbContext`
  - `IdentityDbContext`
  - `IncidentDbContext`

#### Conclusão
A testabilidade do repositório é melhor do que a média, mas **não protege produção de ponta a ponta**.

---

### FRENTE 10 — Observabilidade e Operação
**Classificação:** `FRÁGIL`

#### O que está bom
- `Serilog` configurado no host principal.
- `OpenTelemetry` presente para tracing e metrics.
- Health endpoints `/health`, `/ready`, `/live` existem.
- Logs de lifecycle do host existem.

#### O que está fraco
- Health checks não validam dependências reais.
- OTLP exporter está configurável, mas a revisão não encontrou validação operacional do collector/export path.
- `BackgroundWorkers` não têm health próprio nem métricas explícitas por job.
- `GlobalExceptionHandler` devolve erro genérico sem `correlationId`.
- `Ingestion.Api` não recebe o mesmo hardening operacional do `ApiHost`.

#### Veredicto
Há instrumentação básica, mas **observabilidade enterprise real ainda não está pronta**.

---

### FRENTE 11 — i18n e Consistência de UX
**Classificação:** `PARCIAL`

#### Pontos positivos
- `i18next` configurado com `en`, `pt-BR`, `pt-PT` e `es`.
- A maior parte das páginas amostradas usa `useTranslation()`.

#### Gaps
- Existem fallbacks embutidos em chamadas `t(..., 'default text')`, inclusive em componentes shared.
- Existem textos e conceitos misturados entre português e inglês nas traduções, por exemplo termos como `Self-hosted`, `Break Glass`, `Workflow`, `FinOps`, `JIT Access` preservados em vários idiomas.
- Páginas mockadas trazem nomes/status hardcoded que escapam da camada de i18n real.

#### Conclusão
i18n existe e está difundido, mas **a consistência enterprise multilíngua ainda não está fechada**.

---

### FRENTE 12 — Prontidão Final para Produção
**Classificação:** `BLOQUEADO`

#### Resultado final
- **Solução inteira:** `BLOQUEADO`
- **Backend:** `PARCIAL`
- **Frontend:** `BLOQUEADO`

**Sem correção dos blockers objetivos, a ida para produção não é defensável.**

---

## 4. Avaliação por Módulo

| Módulo | Classificação | Observação principal |
|---|---|---|
| `IdentityAccess` | `QUASE PRONTO` | Backend forte; frontend/auth web ainda depende de bearer em `sessionStorage`. |
| `Catalog.Graph` | `QUASE PRONTO` | API e handlers consistentes; domínio ainda um pouco anêmico. |
| `Catalog.Contracts` | `QUASE PRONTO` | Melhor submódulo do repositório auditado; frontend ainda usa enrich/mock em pontos críticos. |
| `Catalog.Portal` | `PARCIAL` | Playground é mock; não é produção real. |
| `ChangeGovernance.ChangeIntelligence` | `PARCIAL` | Backend estruturado, mas cobertura e frontend ainda não sustentam confiança plena. |
| `ChangeGovernance.Workflow` | `PARCIAL` | Rotas existem; E2E real não prova o fluxo. |
| `ChangeGovernance.Promotion` | `PARCIAL` | Frontend com testes falhando e fragilidade visual/funcional. |
| `ChangeGovernance.RulesetGovernance` | `PARCIAL` | Presença estrutural boa, mas sem evidência suficiente de maturidade operacional. |
| `OperationalIntelligence.Incidents` | `QUASE PRONTO` | Persistência real via EF; ainda há bugs de paginação e pouca prova end-to-end. |
| `OperationalIntelligence.Automation` | `MOCKADO` | `ListAutomationWorkflows` devolve dados simulados. |
| `OperationalIntelligence.Runtime` | `PARCIAL` | Endpoints presentes; prontidão operacional e testes ainda insuficientes. |
| `OperationalIntelligence.Cost` | `PARCIAL` | Estrutura existe, mas evidência auditada é limitada. |
| `AuditCompliance` | `QUASE PRONTO` | Modelo simples e coeso; cobertura funcional ainda moderada. |
| `AIKnowledge.Governance` | `PARCIAL` | Assistente e registry existem; precisa endurecimento operacional e de segurança. |
| `AIKnowledge.Runtime` | `PARCIAL` | Bom desenho de provider/fallback; sem prova operacional suficiente. |
| `AIKnowledge.ExternalAI` | `PARCIAL` | Integração existe, mas governança completa ainda precisa validação real em produção. |
| `AIKnowledge.Orchestration` | `PARCIAL` | Estrutura boa, mas sem prova suficiente de robustez ponta a ponta. |
| `Governance` | `PARCIAL` | Muito volume funcional, mas com métricas neutras, simulação mock e frontend quebrado. |
| `ApiHost` | `PARCIAL` | Composição boa; falta `UseCors()`, health checks reais e forward headers. |
| `BackgroundWorkers` | `PARCIAL` | Implementação correta como `BackgroundService`, mas cobertura funcional limitada. |
| `Ingestion.Api` | `BLOQUEADO` | Sem autenticação/autorizações; bypass da camada Application. |
| `CLI` | `MOCKADO` | Stub sem comandos reais. |
| `Frontend` | `BLOQUEADO` | Build de produção quebrado. |

---

## 5. Inventário de Entidades/Modelos Críticos

| Entidade/Modelo | Módulo | Estado | Avaliação |
|---|---|---|---|
| `User` | IdentityAccess | `QUASE PRONTO` | Bom uso de VO e invariantes de autenticação. |
| `Session` | IdentityAccess | `QUASE PRONTO` | Rotação/revogação corretas; sem endurecimento de device binding. |
| `ServiceAsset` | Catalog.Graph | `PARCIAL` | Central no produto, mas com baixo teor comportamental. |
| `ContractVersion` | Catalog.Contracts | `QUASE PRONTO` | Agregado mais maduro do repositório auditado. |
| `Release` | ChangeGovernance | `PARCIAL` | Funcional, porém ainda mais registry do que agregado rico. |
| `IncidentRecord` | OperationalIntelligence | `PARCIAL` | Persistência forte, mas agregado excessivamente textual/JSON. |
| `AuditEvent` | AuditCompliance | `QUASE PRONTO` | Modelo simples e coerente para trilha imutável. |
| `AIModel` | AIKnowledge | `PARCIAL` | Registry coerente, porém ainda simples. |
| `GovernancePack` | Governance | `PARCIAL` | Lifecycle básico; pouca profundidade de regra. |
| `IntegrationConnector` / `IngestionExecution` | Governance | `PARCIAL` | Essenciais para integrations, mas `Ingestion.Api` expõe fragilidade de uso. |

---

## 6. Inventário de DbContexts/Migrations/Seeds

### DbContexts identificados

| DbContext | Migrations | Seed dev | Status |
|---|---|---|---|
| `IdentityDbContext` | `20260313210303_InitialIdentitySchema` | `seed-identity.sql` | `QUASE PRONTO` |
| `AuditDbContext` | `20260313210322_InitialAuditSchema` | `seed-audit.sql` | `QUASE PRONTO` |
| `CatalogGraphDbContext` | `20260315201522_InitialCatalogGraphSchema` | `seed-catalog.sql` | `QUASE PRONTO` |
| `ContractsDbContext` | `20260315201534_InitialContractsSchema` | não | `PARCIAL` |
| `DeveloperPortalDbContext` | `20260315201551_InitialDeveloperPortalSchema` | não | `PARCIAL` |
| `ChangeIntelligenceDbContext` | `20260315201603_InitialChangeIntelligenceSchema` | `seed-changegovernance.sql` | `QUASE PRONTO` |
| `RulesetGovernanceDbContext` | `20260315201615_InitialRulesetGovernanceSchema` | não | `PARCIAL` |
| `WorkflowDbContext` | `20260315201627_InitialWorkflowSchema` | não | `PARCIAL` |
| `PromotionDbContext` | `20260315201640_InitialPromotionSchema` | não | `PARCIAL` |
| `IncidentDbContext` | `20260317161138_InitialIncidentsSchema` | `seed-incidents.sql` | `QUASE PRONTO` |
| `AiGovernanceDbContext` | `20260318084918_InitialAiGovernanceSchema` | `seed-aiknowledge.sql` | `QUASE PRONTO` |
| `GovernanceDbContext` | `20260318120427_InitialGovernanceSchema`, `20260318142113_AddAnalyticsEvents` | `seed-governance.sql` | `QUASE PRONTO` |
| `RuntimeIntelligenceDbContext` | `20260318161556_InitialRuntimeIntelligenceSchema` | não | `PARCIAL` |
| `CostIntelligenceDbContext` | `20260318161604_InitialCostIntelligenceSchema` | não | `PARCIAL` |
| `ExternalAiDbContext` | `20260318183620_InitialExternalAiSchema` | não | `PARCIAL` |
| `AiOrchestrationDbContext` | `20260318183635_InitialAiOrchestrationSchema` | não | `PARCIAL` |

### Observações
- **Todos os contextos identificados possuem migração.**
- **Nem todos possuem seed dev.**
- **A suite de integração com PostgreSQL cobre apenas 5 contextos**: `CatalogGraph`, `Contracts`, `ChangeIntelligence`, `Identity`, `Incidents`.

---

## 7. Inventário de Endpoints e Gaps

### Endpoints críticos mapeados
- `Identity`: `/api/v1/identity/auth/*`, `/users`, `/tenants/mine`, `/auth/select-tenant`, etc.
- `Catalog`: `/api/v1/catalog/*`
- `Contracts`: `/api/v1/contracts/*`
- `Source of Truth`: `/api/v1/source-of-truth/*`
- `Developer Portal`: `/api/v1/developerportal/*`
- `Change Governance`: `/api/v1/releases/*`, workflow/promotion/rulesets em módulos próprios
- `Operational Runtime`: `/api/v1/runtime/*`
- `AI`: `/api/v1/ai/*`
- `Governance`: múltiplos endpoint modules por tema

### Gaps relevantes
| Área | Gap | Severidade |
|---|---|---|
| Ingestion | Endpoints públicos, sem auth/API key real | `CRÍTICO` |
| ApiHost | CORS não aplicado em runtime | `ALTO` |
| Developer Portal | Playground responde mock, não sandbox real | `ALTO` |
| Governance | Simulação de pack é mock | `ALTO` |
| Operational Automation | Listagem de workflows é mock | `ALTO` |
| Governance analytics | Parte das métricas retorna neutro/zero por falta de integração cross-module | `MÉDIO` |

---

## 8. Inventário de Páginas Frontend e Gaps

| Área | Estado | Gap principal |
|---|---|---|
| Login / Auth | `PARCIAL` | sessão via bearer em `sessionStorage`; cookie session não adotada |
| Users / Identity | `QUASE PRONTO` | páginas funcionais, mas dependem do modelo de token atual |
| Service Catalog | `QUASE PRONTO` | boa estrutura; precisa prova E2E real |
| Contract Catalog | `QUASE PRONTO` | backend bom; frontend de studio ainda usa enrich mock |
| Contract Studio | `PARCIAL` | `studioMock.ts` mascara falta de dados do backend |
| Releases / Workflow / Promotion | `PARCIAL` | cobertura frontend instável; testes falham em Promotion |
| Incidents / Operations | `PARCIAL` | parte do backend real, pouca prova ponta a ponta |
| AI Hub | `BLOQUEADO` | página `IdeIntegrationsPage.tsx` quebra build |
| Governance drill-down | `BLOQUEADO` | `ExecutiveDrillDownPage.tsx` quebra build |
| Governance team detail | `BLOQUEADO` | `TeamDetailPage.tsx` quebra build |
| Pack Simulation | `MOCKADO` | usa `mockSimulation` local |

---

## 9. Achados de Segurança

### `CRÍTICO`
1. `Ingestion.Api` sem autenticação/autorização real.

### `ALTO`
2. CORS configurado mas não aplicado no pipeline.
3. `sessionStorage` para access token ainda é o modelo ativo do frontend.
4. Health/readiness checks retornam verde sem validar dependências reais.

### `MÉDIO`
5. Connection strings default hardcoded em vários módulos.
6. Ausência de `UseForwardedHeaders()` compromete rate limit e IP real atrás de proxy.
7. Fluxo seguro de cookie session existe, mas não protege o produto atual porque está desativado/não adotado.
8. `GlobalExceptionHandler` sem `correlationId` e sem contexto operacional de suporte.

### `BAIXO`
9. Alguns modelos ainda usam `DateTimeOffset.UtcNow` direto, reduzindo previsibilidade e testabilidade.

---

## 10. Achados de Testes

### Pontos fortes
- `IdentityAccess.Tests`: 186 testes, todos OK.
- `Catalog.Tests`: 466 testes, todos OK.
- `Governance.Tests`: 23 testes, todos OK.

### Pontos fracos
- `NexTraceOne.E2E.Tests` em .NET é apenas placeholder.
- Playwright usa forte mock/interceptação; não valida backend real.
- Testes de frontend falharam.
- Integração PostgreSQL depende de Docker/Testcontainers; no ambiente atual falhou.
- A fixture de integração cobre apenas parte dos contextos.

### Fluxos críticos sem proteção suficiente
- Ingestion system-to-system com auth real.
- Login com cookie session + CSRF end-to-end.
- Promotion/change governance ponta a ponta.
- Governance packs / waivers / drilldown executivo ponta a ponta.
- AI Assistant com providers reais, budgets e políticas reais.
- Readiness/health operacional com falha de dependências reais.

---

## 11. Achados de Observabilidade/Operação

1. `/health`, `/ready`, `/live` existem, mas readiness é sintética.
2. Não há evidência de health por worker/job real.
3. `BackgroundWorkers` não expõem estado detalhado.
4. OTLP está configurável, mas não há prova de operação pronta enterprise.
5. `Ingestion.Api` está operacionalmente sub-endurecida comparada ao `ApiHost`.

---

## 12. Achados de i18n

1. `i18n` existe e suporta `en`, `pt-BR`, `pt-PT`, `es`.
2. Há mistura de termos em inglês nas variantes PT (`Self-hosted`, `Break Glass`, `Workflow`, `FinOps`, `JIT Access`).
3. Há uso de fallback literal em algumas traduções/componentes.
4. Páginas mockadas carregam nomes/status fixos que não passam integralmente pela camada de i18n.

---

## 13. Dívidas Técnicas Prioritárias

1. Corrigir o build do frontend.
2. Proteger `Ingestion.Api` com auth real e autorização por API key/permissions.
3. Aplicar `UseCors()` no `ApiHost`.
4. Remover ou isolar flows mockados (`SimulateGovernancePack`, `ExecutePlayground`, `ListAutomationWorkflows`, `studioMock`).
5. Migrar o frontend para cookie session `httpOnly` + CSRF ou equivalente mais seguro.
6. Fortalecer E2E real e integração para todos os contextos persistidos relevantes.
7. Corrigir bugs de paginação e contagem total em handlers amostrados.

---

## 14. Riscos por Severidade

### `CRÍTICO`
- `Ingestion.Api` público sem autenticação.
- Frontend não gerar build de produção.

### `ALTO`
- CORS não aplicado.
- Health checks enganosos.
- Access token em `sessionStorage` como modelo atual.
- Fluxos de negócio ainda mockados em áreas visíveis do produto.

### `MÉDIO`
- Testes E2E insuficientes.
- Integração PostgreSQL parcial e dependente de Docker indisponível no ambiente.
- Executive/governance analytics parcialmente neutros.
- Defaults inseguros de configuração.
- Observabilidade incompleta para operação enterprise.

### `BAIXO`
- Ficheiros endpoint module vazios/legados.
- Uso pontual de tempo do sistema direto em domínio.
- Mistura terminológica no i18n.

---

## 15. Plano de Correção Priorizado

### Prioridade 0 — bloqueadores de release
1. Corrigir `IdeIntegrationsPage.tsx`, `ExecutiveDrillDownPage.tsx` e `TeamDetailPage.tsx` até o frontend voltar a buildar.
2. Fechar `Ingestion.Api` com `AddBuildingBlocksSecurity`, autenticação por API key e autorização mínima.
3. Inserir `app.UseCors()` no `ApiHost` na ordem correta do pipeline.
4. Reprovar qualquer rota/tela ainda mockada que esteja exposta como funcionalidade produtiva.

### Prioridade 1 — segurança e operação
5. Tornar health checks reais: banco, dependências, workers, providers externos.
6. Habilitar proxy awareness (`UseForwardedHeaders`) para produção.
7. Migrar auth web para cookie `httpOnly` + CSRF ou justificar formalmente o bearer atual com compensações fortes.

### Prioridade 2 — confiabilidade funcional
8. Remover `studioMock.ts` e fazer o backend entregar o shape necessário.
9. Substituir `SimulateGovernancePack` por leitura/cálculo real.
10. Substituir `ExecutePlayground` por sandbox real ou esconder completamente o endpoint.
11. Substituir `ListAutomationWorkflows` por persistência/consulta real.
12. Corrigir `TotalCount` em listagens com paginação incorreta.

### Prioridade 3 — qualidade e produção enterprise
13. Criar E2E real de sistema para login, contracts, changes, incidents, governance, AI.
14. Expandir integração PostgreSQL para mais contextos além dos 5 atuais.
15. Revisar modelos anêmicos e excesso de JSON bag nos agregados mais críticos.
16. Remover defaults inseguros de connection strings do código de produção.

---

## 16. Critério Go/No-Go para Produção

### `NO-GO` enquanto qualquer item abaixo permanecer aberto
- Frontend não gerar build de produção.
- `Ingestion.Api` continuar sem autenticação/autorização real.
- CORS continuar sem aplicação no pipeline.
- Health checks continuarem retornando sucesso sem validar dependências reais.
- Fluxos mockados continuarem expostos como features utilizáveis.

### Critério mínimo para `GO`
- Build backend e frontend verdes.
- Smoke tests reais de login, catalog, contracts, changes, incidents e AI assistant.
- Ingestion protegida por auth real.
- Readiness real de DB/dependências/workers.
- Redução explícita ou remoção dos módulos preview/mock do escopo produtivo.
- Evidência de deploy limpo com migrações aplicadas e sem seed oculta obrigatória.
