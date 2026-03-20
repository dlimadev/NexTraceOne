# NexTraceOne — Release Gate Final

**Data da validação:** 2026-03-20  
**Base documental obrigatória:**

- `docs/reviews/NexTraceOne_Production_Readiness_Review.md`
- `docs/reviews/NexTraceOne_Full_Production_Convergence_Report.md`
- `docs/release/NexTraceOne_Final_Production_Scope.md`
- `docs/acceptance/NexTraceOne_Baseline_Estavel.md`
- `docs/planos/NexTraceOne_Plano_Operacional_Finalizacao.md`
- `docs/planos/NexTraceOne_Plano_Evolucao_Fase_10.md`

---

## 1. Resumo executivo

### Veredicto objetivo

**NO-GO**

### Rationale executivo

O estado atual do repositório **não sustenta promoção para produção**.

Os bloqueadores objetivos revalidados nesta execução foram:

1. **A solution não está verde**.
   - `run_build` falhou com erros reais no repositório.
   - Há quebra de compilação em testes de `Catalog.Contracts`.
   - Há quebra de compilação TypeScript em áreas do frontend que deveriam estar prontas.

2. **O frontend não gera build de produção**.
   - `npm run build` em `src/frontend` falhou com **194 erros**.
   - O erro não está restrito a áreas fora do release: há falhas em superfícies incluídas, como `ServiceDetailPage` e `identity.ts`.
   - Código fora do escopo final continua a poluir o grafo de compilação, especialmente páginas de `Governance`.

3. **O `ApiHost` não tem startup viável no estado atual**.
   - A reexecução de testes críticos de integração/E2E falhou antes do primeiro cenário funcional.
   - O bloqueio atual é **`IdentityDbContext` com pending model changes**, disparado durante `ApplyDatabaseMigrationsAsync()`.
   - Isso invalida a prontidão operacional do host principal.

4. **A evidência de testes reais atuais é insuficiente para release**.
   - Foram reexecutados 6 testes críticos de integração/E2E.
   - **Resultado: 0 aprovados, 6 falhas**.
   - As falhas atuais não são cosméticas; travam startup do produto para cenários reais.

5. **O isolamento do escopo final ainda não está tecnicamente fechado**.
   - A navegação e os gates existem.
   - Porém o frontend ainda compila código excluído do release e esse código hoje quebra o build.
   - Portanto o escopo final está documentado, mas **a implementação ainda não está limpa o suficiente para release**.

### Conclusão curta

O NexTraceOne evoluiu em segurança de ingestão, aplicação de CORS, health checks reais e honestidade da navegação final.  
Isso **não compensa** os bloqueadores atuais de build, startup e testes críticos.

---

## 2. Escopo avaliado

### Escopo final declarado para release

Conforme `docs/release/NexTraceOne_Final_Production_Scope.md`, o release final deveria incluir:

- Login / Auth / Tenant selection
- Shell / Navigation
- Dashboard
- Service Catalog
- Source of Truth
- Change Governance core
- Identity Admin core
- Audit

### Escopo explicitamente fora do release

- Developer Portal
- Contracts
- Operations
- AI Hub
- Governance
- Integrations
- Product Analytics
- Platform Operations

### Revalidação do estado real

O repositório atual confirma que:

- existe filtragem de escopo em `src/frontend/src/releaseScope.ts`
- a sidebar usa `isRouteAvailableInFinalProductionScope(...)`
- a command palette usa o mesmo filtro
- rotas fora do release foram mantidas com `ReleaseScopeGate`

Mas a revalidação também confirmou que:

- áreas fora do release **ainda contaminam o build do frontend**
- páginas de `Governance` continuam a ser compiladas e hoje geram erros TypeScript
- portanto o escopo final está **semanticamente mais honesto**, mas **tecnicamente ainda não isolado o suficiente**

---

## 3. Resultado por frente

### FRENTE 1 — Build e executabilidade

**Resultado:** `NO-GO`

#### Backend

**Evidência executada**

- `run_build` → **falhou**
- `dotnet build src/platform/NexTraceOne.ApiHost/NexTraceOne.ApiHost.csproj -nologo` → **sucesso com 12 warnings**
- `dotnet build src/platform/NexTraceOne.BackgroundWorkers/NexTraceOne.BackgroundWorkers.csproj -nologo` → **sucesso com 12 warnings**
- `dotnet build src/platform/NexTraceOne.Ingestion.Api/NexTraceOne.Ingestion.Api.csproj -nologo` → **sucesso com 9 warnings**

**Bloqueadores reais confirmados**

- `tests/modules/catalog/NexTraceOne.Catalog.Tests/Contracts/Application/Features/ContractsNewFeaturesTests.cs`
  - `CS7036`: construtor de `GetContractVersionDetail.Handler` exige `apiAssetRepository`
- `tests/modules/catalog/NexTraceOne.Catalog.Tests/Contracts/Application/Features/ProtocolAutoDetectionTests.cs`
  - `CS1503`: `IUnitOfWork` não converte para `IContractsUnitOfWork`

#### Startup do host principal

**Evidência executada**

- Reexecução de testes críticos de integração/E2E

**Resultado atual**

- `ApiHost` **não está com startup viável** no estado atual
- Falha em `Program.cs` ao executar `ApplyDatabaseMigrationsAsync()`
- Motivo: `IdentityDbContext` com `PendingModelChangesWarning`

#### Frontend

**Evidência executada**

- `npm run build` em `src/frontend` → **falhou**

**Bloqueadores reais confirmados**

- `src/frontend/src/features/catalog/pages/ServiceDetailPage.tsx`
  - `TS2305`: `ServiceContractItem` não existe em `../../../types`
  - `TS7006`: parâmetro `c` implicitamente `any`
- `src/frontend/src/features/identity-access/api/identity.ts`
  - `TS2305`: `PagedList` não exportado por `../../../types`
- `src/frontend/src/types/index.ts`
  - `TS2304`: `ContractProvenance` não encontrado
- múltiplas páginas de `Governance` fora do release continuam a quebrar o build

**Conclusão da frente 1**

Mesmo com os projetos de runtime principais a compilarem isoladamente, o produto **não passa o gate de build** porque a solution e o frontend produtivo continuam quebrados.

---

### FRENTE 2 — Escopo final do release

**Resultado:** `PARCIAL`

#### Evidência positiva

- `src/frontend/src/releaseScope.ts` contém prefixos incluídos e excluídos alinhados ao documento final
- `AppSidebar.tsx` filtra itens por `isRouteAvailableInFinalProductionScope(...)`
- `CommandPalette.tsx` filtra itens e resultados pelo mesmo critério
- `App.tsx` mantém rotas excluídas sob `ReleaseScopeGate`

#### Gap real remanescente

- o escopo final **não está limpo no pipeline de build**
- rotas excluídas deixam de parecer produtivas na navegação, mas o código dessas áreas ainda entra na compilação e hoje quebra o release

**Conclusão da frente 2**

O escopo está mais honesto na UX, mas **a separação técnica ainda é insuficiente** para um gate final de produção.

---

### FRENTE 3 — Convergência frontend ↔ backend

**Resultado:** `BLOQUEADO`

#### Evidência revalidada

Há endpoints, handlers e persistência reais nas superfícies centrais do release:

- `Identity`:
  - `/identity/auth/me` → `GetCurrentUserFeature.Query()`
  - `/identity/users`, `/identity/tenants/{tenantId}/users` → handlers reais e permissões
- `Catalog`:
  - `/api/v1/catalog/services/{serviceId}` → `GetServiceDetail.Handler`
- `Source of Truth`:
  - `/api/v1/source-of-truth/services/{serviceId}` → `GetServiceSourceOfTruth.Handler`
  - `/api/v1/source-of-truth/contracts/{contractVersionId}` → handler real
- `Change Governance`:
  - `/api/v1/releases` e `/api/v1/releases/{releaseId}` → handlers reais
- `Audit`:
  - `/api/v1/audit/*` → handlers reais com `RequirePermission(...)`

#### Bloqueio objetivo atual

Apesar da convergência arquitetural existir, **não há convergência liberável** porque:

- o frontend não builda
- o `ApiHost` não sobe de forma viável no estado atual
- os testes críticos que deveriam comprovar o round-trip real falham no bootstrap

**Conclusão da frente 3**

A convergência existe em estrutura, mas está **operacionalmente bloqueada**.

---

### FRENTE 4 — Persistência, migrations e dados

**Resultado:** `BLOQUEADO`

#### Evidência positiva

Foram localizadas migrations reais para os contextos relevantes, incluindo:

- `IdentityDbContext`
- `CatalogGraphDbContext`
- `ContractsDbContext`
- `ChangeIntelligenceDbContext`
- `WorkflowDbContext`
- `PromotionDbContext`
- `AuditDbContext`
- `IncidentDbContext`
- `AiGovernanceDbContext`
- `ExternalAiDbContext`
- `AiOrchestrationDbContext`
- `GovernanceDbContext`

`ApplyDatabaseMigrationsAsync()` também cobre explicitamente múltiplos `DbContexts` do produto.

#### Bloqueio objetivo atual

A revalidação runtime mostrou que:

- `IdentityDbContext` está com **model drift** relativamente às migrations
- o host falha no bootstrap ao aplicar migrations
- isso bloqueia integração, E2E e startup confiável em ambiente limpo

**Conclusão da frente 4**

A existência de migrations já não basta. O estado atual mostra **desalinhamento entre modelo e histórico de migration** em `IdentityDbContext`, o que é impeditivo para release.

---

### FRENTE 5 — Testes reais

**Resultado:** `BLOQUEADO`

#### Evidência executada nesta validação

Foram executados 6 testes críticos:

- `CoreApiHostIntegrationTests.IdentityAccess_Should_Login_ListTenants_And_SelectTenant_And_Use_Real_CookieSession`
- `CoreApiHostIntegrationTests.Catalog_And_SourceOfTruth_Should_List_Services_Get_Detail_And_Expose_Real_Contract_Catalog_Summary`
- `CoreApiHostIntegrationTests.Contracts_Should_Create_Update_And_Submit_Draft_With_Real_Backend`
- `CoreApiHostIntegrationTests.AI_Should_Create_Open_Send_Persist_Relist_And_Reopen_Conversation_With_Real_Backend`
- `RealBusinessApiFlowTests.Incidents_Should_List_Seeded_Detail_And_Create_New_Incident`
- `RealBusinessApiFlowTests.AI_Should_Create_Conversation_Send_Message_And_List_Persisted_Messages`

**Resultado:** `0 Passed / 6 Failed`

#### Motivo dominante das falhas atuais

- todas as reexecuções falharam no bootstrap do host por `PendingModelChangesWarning` em `IdentityDbContext`

#### Avaliação por fluxo

| Fluxo | Estado | Evidência atual |
|---|---|---|
| Login/Auth | `SEM EVIDÊNCIA SUFICIENTE` | teste crítico reexecutado falhou no startup do host |
| Services / Source of Truth | `SEM EVIDÊNCIA SUFICIENTE` | teste crítico reexecutado falhou no startup do host |
| Contracts | `COBERTURA PARCIAL` | havia histórico positivo, mas o estado atual tem testes quebrados/compile break e host bloqueado |
| Changes | `SEM EVIDÊNCIA SUFICIENTE` | sem prova válida atual após reexecução bloqueada |
| Incidents | `SEM EVIDÊNCIA SUFICIENTE` | E2E reexecutado falhou no bootstrap |
| Governance core | `N/A fora do release` | módulo fora do release final |
| AI Assistant | `SEM EVIDÊNCIA SUFICIENTE` | teste reexecutado falhou no bootstrap |
| Audit | `SEM EVIDÊNCIA SUFICIENTE` | sem prova atual válida nesta execução |
| Integrations / Analytics | `N/A fora do release` | fora do release final |

**Conclusão da frente 5**

No estado atual, a evidência de testes reais **não suporta nem `GO` nem `GO COM RESSALVAS`**.

---

### FRENTE 6 — Segurança

**Resultado:** `ACEITÁVEL EM PARTES, MAS NÃO SUFICIENTE PARA REVERTER O NO-GO`

#### Blockers críticos anteriores efetivamente fechados

1. **`Ingestion.Api` protegida**
   - agora exige policy `IngestionApiKeyWrite`
   - requer API key válida, `auth_method=api_key` e permissão `integrations:write`

2. **CORS aplicado no `ApiHost`**
   - `AddCorsConfiguration()` existe
   - `app.UseCors()` está presente no pipeline

3. **Read endpoints inspecionados usam autorização/permissão coerente**
   - `AuthEndpoints`, `UserEndpoints`, `SourceOfTruthEndpointModule`, `AuditEndpointModule` e rotas de catálogo inspecionadas usam `RequireAuthorization()` ou `RequirePermission(...)`

#### Achados remanescentes

| Achado | Severidade | Observação |
|---|---|---|
| fluxo cookie `httpOnly` existe mas está **desabilitado por default** | `MÉDIO` | `CookieSessionOptions.Enabled = false`; o frontend faz fallback para bearer se o endpoint seguro não existir |
| ausência de evidência de `UseForwardedHeaders` / proxy awareness no host inspecionado | `MÉDIO` | relevante para produção atrás de reverse proxy |
| rotas fora do release continuam registradas, ainda que honestamente gated | `BAIXO` | não parecem produtivas, mas continuam acessíveis via gate |

#### Reclassificação de risco anterior

- não há evidência atual de `accessToken` persistido em `sessionStorage`
- pela implementação atual, `accessToken`, `refreshToken` e `csrfToken` ficam em memória; `sessionStorage` guarda apenas `tenantId` e `userId`
- isso reduz o risco anteriormente apontado, mas não elimina a necessidade de endurecer o modelo cookie em produção

**Conclusão da frente 6**

Segurança **melhorou materialmente** face ao review anterior.  
Não encontrei nesta revalidação um gap crítico de segurança equivalente ao cenário anterior de `Ingestion.Api` pública.  
Ainda assim, o release continua `NO-GO` por build, startup e testes.

---

### FRENTE 7 — Operação, health e observabilidade

**Resultado:** `PARCIAL`

#### Evidência positiva

- `ApiHost` expõe `/health`, `/ready` e `/live`
- `ApiHostHealthChecks` usa checks reais de conectividade para múltiplos `DbContexts`
- `/live` usa self-check com versão e uptime real
- `BackgroundWorkers` expõe health de `identity-db`, `outbox-processor-job` e `identity-expiration-job`
- `Ingestion.Api` expõe `/health`, `/ready`, `/live` e `governance-db` readiness
- `ApiHost` usa logging estruturado (`Serilog`) e `Ingestion.Api` aplica `X-Correlation-Id`

#### Bloqueio operacional atual

- o `ApiHost` não passou na validação de startup real por causa do drift de migrations/model
- portanto a prontidão operacional é **boa em desenho**, mas **não viável na execução atual**

**Conclusão da frente 7**

Health e observabilidade deixaram de ser sintéticos, o que é positivo.  
Mas o principal host continua sem startup confiável neste estado.

---

### FRENTE 8 — UX técnica do produto final

**Resultado:** `BLOQUEADO`

#### Evidência positiva

- existe `ReleaseScopeGate` com mensagem honesta para módulos fora do release
- a navegação principal foi filtrada por escopo final

#### Bloqueadores reais

- o frontend não compila
- áreas excluídas do release continuam a quebrar a build
- superfícies incluídas também têm erros reais de tipos/imports

**Conclusão da frente 8**

A UX final está mais honesta, mas o produto **não está tecnicamente coerente o suficiente para produção** porque a aplicação frontend não fecha build.

---

### FRENTE 9 — Módulos core do release

**Resultado:** ver tabela por módulo na seção 4

---

### FRENTE 10 — Veredicto final

**Resultado:** `NO-GO`

Motivos suficientes e independentes entre si:

- blocker de build
- blocker de startup real do `ApiHost`
- falha atual de testes críticos de integração/E2E
- escopo final ainda contaminado por código excluído no frontend

---

## 4. Resultado por módulo

| Módulo | Classificação | Entra no release final? | Fundamentação objetiva |
|---|---|---|---|
| Identity & Access | `BLOQUEADO` | Sim | endpoints reais e autorização presentes, mas `IdentityDbContext` com pending model changes bloqueia startup/testes; `identity.ts` tem erro de tipo (`PagedList`) |
| Shell / Navigation / Dashboard | `PARCIAL` | Sim | filtragem de escopo e gate existem, mas o frontend global não builda |
| Catalog / Source of Truth | `BLOQUEADO` | Sim | endpoints/handlers/persistência reais; `ServiceDetailPage` quebra build e runtime real está bloqueado pelo host |
| Change Governance core | `BLOQUEADO` | Sim | backend real existente, mas sem validação runtime atual devido falha de startup global |
| Audit | `BLOQUEADO` | Sim | backend real com permissões, porém sem host viável hoje |
| Contracts | `BLOQUEADO` | Não | fora do release; testes do módulo estão quebrados no build atual |
| Incidents / Operations | `BLOQUEADO` | Não | fora do release; E2E atual falhou no bootstrap do host |
| AI Assistant / AI Hub | `BLOQUEADO` | Não | fora do release; reexecução atual falhou no bootstrap do host |
| Governance | `BLOQUEADO` | Não | fora do release e continua preview/parcial; páginas de governance hoje quebram o frontend build |
| Integrations / Ingestion | `PARCIAL` | Não | `Ingestion.Api` compila e está protegida por API key, mas superfície do produto continua fora do release |
| Product Analytics | `BLOQUEADO` | Não | fora do release, sem validação produtiva nesta execução |
| Platform Operations / BackgroundWorkers | `PARCIAL` | Não | worker compila e health é real, mas não houve prova suficiente de startup/runtime produtivo nesta validação |

---

## 5. Matriz final frontend ↔ backend ↔ persistence

> Status final reflete o **estado atual do repositório**, não apenas a existência arquitetural do endpoint.

| Módulo | Página / rota | Endpoint | Handler / feature | Persistência | Status final |
|---|---|---|---|---|---|
| Identity | `/login`, `/select-tenant`, `/mfa`, `/my-sessions` | `/identity/auth/cookie-session` (fallback `/identity/auth/login`), `/identity/tenants/mine`, `/identity/auth/select-tenant`, `/identity/auth/me` | `LocalLogin`, `ListMyTenants`, `SelectTenant`, `GetCurrentUser` | `IdentityDbContext` | `BLOQUEADO` |
| Identity Admin | `/users` | `/identity/users`, `/identity/tenants/{tenantId}/users` | `CreateUser`, `ListTenantUsers`, `GetUserProfile`, `AssignRole` | `IdentityDbContext` | `BLOQUEADO` |
| Identity Admin | `/break-glass`, `/jit-access`, `/delegations`, `/access-reviews` | endpoints do módulo identity admin | handlers reais do módulo | `IdentityDbContext` | `BLOQUEADO` |
| Catalog | `/services` | `/api/v1/catalog/services`, `/api/v1/catalog/services/summary`, `/api/v1/catalog/services/search` | queries de catálogo | `CatalogGraphDbContext` | `BLOQUEADO` |
| Catalog | `/services/:serviceId` | `/api/v1/catalog/services/{serviceId}` | `GetServiceDetail` | `CatalogGraphDbContext` | `BLOQUEADO` |
| Source of Truth | `/search`, `/source-of-truth` | `/api/v1/source-of-truth/search`, `/api/v1/source-of-truth/global-search` | `SearchSourceOfTruth`, `GlobalSearch` | `CatalogGraphDbContext` + `ContractsDbContext` | `BLOQUEADO` |
| Source of Truth | `/source-of-truth/services/:serviceId` | `/api/v1/source-of-truth/services/{serviceId}` | `GetServiceSourceOfTruth` | `CatalogGraphDbContext` + `ContractsDbContext` | `BLOQUEADO` |
| Source of Truth | `/source-of-truth/contracts/:contractVersionId` | `/api/v1/source-of-truth/contracts/{contractVersionId}` | `GetContractSourceOfTruth` | `ContractsDbContext` + `CatalogGraphDbContext` | `BLOQUEADO` |
| Change Governance | `/changes`, `/changes/:changeId`, `/releases` | `/api/v1/releases*` e endpoints change intelligence relacionados | `ListReleases`, `GetRelease`, `GetReleaseHistory` e correlatos | `ChangeIntelligenceDbContext` | `BLOQUEADO` |
| Workflow | `/workflow` | endpoints workflow | handlers workflow | `WorkflowDbContext` | `BLOQUEADO` |
| Promotion | `/promotion` | endpoints promotion | handlers promotion | `PromotionDbContext` | `BLOQUEADO` |
| Audit | `/audit` | `/api/v1/audit/search`, `/trail`, `/verify-chain`, `/report`, `/compliance` | handlers audit reais | `AuditDbContext` | `BLOQUEADO` |
| Shell / Dashboard | `/` | composição de APIs do shell/dashboard | consultas agregadas do frontend | múltiplos stores | `PARCIAL` |

**Leitura correta da matriz:**  
Há backend real em boa parte da superfície incluída, mas **o produto não é liberável** porque os fluxos não podem ser validados como release-ready no estado atual.

---

## 6. Resultado de build

### Backend — solution completa

**Status:** `FALHOU`

**Evidência:** `run_build`

**Erros reais relevantes no repositório**

- `ContractsNewFeaturesTests.cs` → `CS7036`
- `ProtocolAutoDetectionTests.cs` → `CS1503`
- erros TypeScript reais em `ServiceDetailPage.tsx`, `identity.ts` e `types/index.ts`

> Nota: `run_build` também devolveu ruído em ficheiros temporários externos ao workspace.  
> O veredicto desta frente foi baseado apenas nos erros reais do repositório, que já são suficientes para `NO-GO`.

### ApiHost

**Status:** `Compila isoladamente, mas startup atual bloqueado`

- build isolado: **sucesso com 12 warnings**
- startup real: **falha** por `IdentityDbContext` com pending model changes

### BackgroundWorkers

**Status:** `Compila isoladamente`

- build isolado: **sucesso com 12 warnings**

### Ingestion.Api

**Status:** `Compila isoladamente`

- build isolado: **sucesso com 9 warnings**

### Frontend

**Status:** `FALHOU`

**Evidência:** `npm run build`

- falha com **194 erros**
- erros em áreas incluídas no release e em áreas excluídas que ainda entram no build

---

## 7. Resultado de testes

### Testes reexecutados nesta validação

| Teste | Tipo | Resultado |
|---|---|---|
| `CoreApiHostIntegrationTests.IdentityAccess_Should_Login_ListTenants_And_SelectTenant_And_Use_Real_CookieSession` | Integration | `FAILED` |
| `CoreApiHostIntegrationTests.Catalog_And_SourceOfTruth_Should_List_Services_Get_Detail_And_Expose_Real_Contract_Catalog_Summary` | Integration | `FAILED` |
| `CoreApiHostIntegrationTests.Contracts_Should_Create_Update_And_Submit_Draft_With_Real_Backend` | Integration | `FAILED` |
| `CoreApiHostIntegrationTests.AI_Should_Create_Open_Send_Persist_Relist_And_Reopen_Conversation_With_Real_Backend` | Integration | `FAILED` |
| `RealBusinessApiFlowTests.Incidents_Should_List_Seeded_Detail_And_Create_New_Incident` | E2E/API flow | `FAILED` |
| `RealBusinessApiFlowTests.AI_Should_Create_Conversation_Send_Message_And_List_Persisted_Messages` | E2E/API flow | `FAILED` |

### Causa dominante

- bootstrap do `ApiHost` falhou em todos por `IdentityDbContext` com pending model changes

### Conclusão

A evidência de testes reais **não suporta promoção a produção**.

---

## 8. Resultado de segurança

### Fechados

- `Ingestion.Api` não está pública; exige API key e permissão correta
- `ApiHost` aplica CORS no pipeline
- endpoints inspecionados do release usam autorização/permissão coerente
- o token de acesso já não está evidenciado em `sessionStorage`

### Remanescentes

| Achado | Severidade | Estado |
|---|---|---|
| cookie session seguro desabilitado por default | `MÉDIO` | aberto |
| ausência de evidência de forwarded headers/proxy awareness | `MÉDIO` | aberto |
| rotas excluídas continuam existentes sob gate | `BAIXO` | aceite apenas como estratégia de produto, não como blocker isolado |

### Conclusão

Não encontrei nesta revalidação um **blocker crítico de segurança equivalente ao cenário anterior**, mas isso **não altera o `NO-GO`** porque o produto continua bloqueado por build e startup.

---

## 9. Resultado de health / operation

### ApiHost

- `/live` → self-check real com versão e uptime
- `/ready` → checks reais de DB para contextos centrais
- `/health` → cobertura ampliada de DBs + AI providers
- **estado atual:** startup bloqueado por migrations/model drift

### BackgroundWorkers

- `/live`, `/ready`, `/health` presentes
- readiness de DB e checks de jobs reais
- **estado atual:** build ok; startup não demonstrado nesta execução

### Ingestion.Api

- `/live`, `/ready`, `/health` presentes
- readiness com `GovernanceDbContext`
- correlação via `X-Correlation-Id`
- **estado atual:** build ok; startup não demonstrado nesta execução

### Conclusão

Operação e health são mais sérios do que no review inicial, mas **o deploy do host principal continua bloqueado no estado atual**.

---

## 10. Gaps remanescentes

1. `run_build` não está verde
2. `npm run build` do frontend falha com 194 erros
3. `IdentityDbContext` tem pending model changes e bloqueia startup do `ApiHost`
4. testes críticos reexecutados falharam 6/6
5. código fora do release continua a contaminar o build do frontend
6. testes de `Catalog.Contracts` estão desalinhados do código atual
7. superfícies incluídas no release ainda têm erros de tipos/imports no frontend

---

## 11. Riscos aceites

**Nenhum risco foi aceite para promoção a produção nesta validação.**  
O gate foi encerrado com `NO-GO`.

---

## 12. Blockers fechados

1. **Proteção da `Ingestion.Api`**
   - agora há política de API key com permissão `integrations:write`

2. **CORS aplicado no `ApiHost`**
   - o pipeline agora usa `app.UseCors()`

3. **Health checks deixaram de ser sintéticos**
   - `ApiHost`, `BackgroundWorkers` e `Ingestion.Api` usam checks reais de `DbContext` e self/live checks reais

4. **Escopo final ficou mais honesto na navegação**
   - sidebar, command palette e rotas excluídas estão filtradas/gated

5. **Persistência de token no browser foi endurecida face ao review anterior**
   - a implementação atual mantém tokens em memória, não em `sessionStorage`

---

## 13. Blockers abertos

1. **Solution build quebrada**
2. **Frontend build de produção quebrado**
3. **`ApiHost` sem startup viável devido a pending model changes em `IdentityDbContext`**
4. **Testes críticos de integração/E2E falhando no estado atual**
5. **Escopo final ainda contaminado por código excluído que entra no build**
6. **Testes de `Catalog.Contracts` quebrados por drift de assinatura/contrato interno**

---

## 14. Veredicto final

# NO-GO

### Justificativa direta

O release final **não está pronto para produção** porque hoje falha em critérios não negociáveis:

- build real
- startup real do host principal
- testes reais atuais
- isolamento técnico do escopo final

Não existe base técnica honesta para emitir `GO` ou `GO COM RESSALVAS` neste estado.

---

## 15. Condições para promoção a produção

A promoção só pode ser reconsiderada após, no mínimo:

1. `run_build` verde
2. `npm run build` verde
3. correção do drift de `IdentityDbContext` com migration compatível e bootstrap validado
4. reexecução dos testes críticos de integração/E2E com resultado verde
5. remoção do código fora do release do grafo de build ou correção completa dessas áreas
6. confirmação final de startup do `ApiHost` em ambiente limpo

---

## 16. Plano de correção imediata pós-gate

### Prioridade 0

1. **Corrigir `IdentityDbContext` pending model changes**
   - alinhar modelo e snapshot
   - gerar/aplicar migration necessária
   - validar `ApplyDatabaseMigrationsAsync()`

2. **Fechar o frontend build**
   - corrigir erros em `ServiceDetailPage.tsx`
   - corrigir `identity.ts` e `types/index.ts`
   - remover da compilação ou corrigir definitivamente páginas de `Governance` fora do release

3. **Corrigir build quebrado dos testes de Contracts**
   - atualizar testes para a assinatura atual dos handlers
   - repor compilação da solution

### Prioridade 1

4. **Reexecutar testes críticos do release incluído**
   - Identity/Auth
   - Catalog/Source of Truth
   - Change Governance core
   - Audit

5. **Revalidar startup do `ApiHost`**
   - ambiente limpo
   - migrations automáticas
   - readiness real

---

## Decisão final deste gate

**NO-GO**
