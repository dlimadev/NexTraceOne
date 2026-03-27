# NexTraceOne — ZR-1 Convergência Técnica Base

**Data:** 2026-03-20  
**Escopo executado:** `Fase ZR-1 — Convergência técnica base`

## 1. Resumo executivo

A ZR-1 foi executada com foco exclusivo na estabilização estrutural do repositório.

O estado final revalidado nesta execução foi:

- `run_build` → **sucesso**
- `dotnet build src/platform/NexTraceOne.ApiHost/NexTraceOne.ApiHost.csproj -nologo` → **sucesso**
- `dotnet build src/platform/NexTraceOne.BackgroundWorkers/NexTraceOne.BackgroundWorkers.csproj -nologo` → **sucesso**
- `dotnet build src/platform/NexTraceOne.Ingestion.Api/NexTraceOne.Ingestion.Api.csproj -nologo` → **sucesso**
- `npm run build` em `src/frontend` → **sucesso**
- startup real do `ApiHost` → **viável e revalidado por testes reais**
- amostra de testes quebrados por drift técnico → **corrigida e verde**

A base técnica saiu do estado de quebra estrutural e ficou pronta para as fases seguintes de fechamento funcional.

---

## 2. Inventário inicial dos bloqueadores técnicos

| Blocker | Categoria | Severidade | Ficheiro/Projeto afetado | Causa provável |
|---|---|---:|---|---|
| Build honesto do frontend explodia após remover exclusões artificiais | Frontend / compilação | Crítico | `src/frontend/tsconfig.app.json`, `src/frontend/src/types/index.ts`, múltiplas páginas | `include: ["src"]` puxava áreas fora do grafo real; barrel de tipos estava incompleto e desalinhado com páginas/API |
| Shared types quebrados em Identity, Governance, Change Governance, Catalog e Source of Truth | Frontend / tipos | Crítico | `src/frontend/src/types/index.ts` e consumidores | drift entre DTOs reais, aliases antigos e páginas já evoluídas |
| Fixture E2E real quebrava antes do fluxo funcional | Testes / infraestrutura | Crítico | `tests/platform/NexTraceOne.E2E.Tests/Infrastructure/ApiE2EFixture.cs` | SQL raw string continha escape inválido de identificadores (`\"`) |
| Suite crítica de integração falhava por mismatch de contrato HTTP | Testes / integração | Alto | `tests/platform/NexTraceOne.IntegrationTests/CriticalFlows/CoreApiHostIntegrationTests.cs` | teste ainda esperava `200 OK`; endpoint real devolve `201 Created` |
| Artefactos diagnósticos residuais de migration | Persistência / higiene | Médio | `src/modules/identityaccess/.../Migrations/__Probe/*` | resíduos de verificação técnica transitória |
| Warnings operacionais não bloqueantes no startup real | Startup / runtime | Médio | `ApiHost` durante testes reais | avisos EF de value comparer/sentinel e `https port` não resolvido em ambiente de teste |
| Warning de chunk grande no frontend | Frontend / empacotamento | Médio | bundle principal `dist/assets/*.js` | aplicação compila, mas ainda com chunk > 500 kB |

---

## 3. Build issues encontrados

### Backend

- solution já compilava via `run_build`, mas havia drift técnico em testes reais:
  - `CoreApiHostIntegrationTests.ChangeGovernance_And_Incidents_Should_Expose_Real_Read_Write_And_Correlation_Flows`
  - status esperado desatualizado (`200`) para endpoint que retorna `201`
- havia artefactos residuais em `IdentityAccess.Infrastructure/Persistence/Migrations/__Probe`
- `ApiHost`, `BackgroundWorkers` e `Ingestion.Api` compilavam, mas com warnings de analyzer e warnings não bloqueantes de runtime/EF

### Testes

- `ApiE2EFixture.SeedMinimalTestDataAsync()` falhava antes de executar o fluxo de auth
- a falha era puramente técnica, não funcional

---

## 4. Issues de frontend build encontrados

Após limpar o `tsconfig` para compilar o grafo real da app, o frontend passou a expor o drift real existente:

### Configuração
- `tsconfig.app.json` dependia de exclusões artificiais de árvores inteiras
- isso mascarava erro estrutural do grafo principal

### Shared types / barrel
- faltavam exports para:
  - `PagedList`
  - `ContractProvenance`
  - DTOs de Governance
  - DTOs de Organization/Governance structure
  - DTOs de Contracts / Contract Studio
  - DTOs de Change Governance / Workflow / Promotion / Advisory
  - DTOs de Source of Truth
  - DTOs de Audit e Developer Portal

### Páginas afetadas pelo drift
- `identity-access/pages/UsersPage.tsx`
- `governance/api/organizationGovernance.ts`
- `change-governance/pages/*`
- `catalog/pages/ServiceDetailPage.tsx`
- `catalog/pages/SourceOfTruthExplorerPage.tsx`
- `catalog/pages/ServiceSourceOfTruthPage.tsx`
- `catalog/pages/ContractSourceOfTruthPage.tsx`
- `audit-compliance/pages/AuditPage.tsx`

### Resultado objetivo
- o problema central não era um único módulo funcional
- era o desacoplamento incompleto entre barrel de tipos, páginas antigas e áreas fora do release que ainda entravam no type graph

---

## 5. Issues de startup/migrations encontrados

### Startup real do `ApiHost`
Revalidado via teste real E2E/integration:

- validação de startup executada
- `ApplyDatabaseMigrationsAsync()` executada com sucesso
- contexts migrados com sucesso:
  - `IdentityDbContext`
  - `CatalogGraphDbContext`
  - `ContractsDbContext`
  - `ChangeIntelligenceDbContext`
  - `RulesetGovernanceDbContext`
  - `WorkflowDbContext`
  - `PromotionDbContext`
  - `AuditDbContext`
  - `DeveloperPortalDbContext`
  - `IncidentDbContext`
  - `RuntimeIntelligenceDbContext`
  - `CostIntelligenceDbContext`
  - `AiGovernanceDbContext`
  - `ExternalAiDbContext`
  - `AiOrchestrationDbContext`
  - `GovernanceDbContext`
- seed de desenvolvimento aplicada com sucesso
- host iniciou com sucesso em ambiente de teste

### Itens técnicos observados
- warnings EF de value comparer/sentinel continuam a aparecer em runtime
- `Failed to determine the https port for redirect` continua a aparecer no ambiente de teste
- estes pontos não bloquearam startup nem migrations

### Higiene de migrations
- removidos artefactos `__Probe` residuais do módulo `Identity`

---

## 6. Testes quebrados por drift técnico

### Corrigidos nesta fase

1. **E2E Auth**
   - teste: `NexTraceOne.E2E.Tests.Flows.AuthApiFlowTests.GetCurrentUser_Me_Endpoint_Should_Return_User_Info`
   - causa: SQL inválido na fixture
   - resultado final: **Passed**

2. **Critical Flow Change Governance + Incidents**
   - teste: `NexTraceOne.IntegrationTests.CriticalFlows.CoreApiHostIntegrationTests.ChangeGovernance_And_Incidents_Should_Expose_Real_Read_Write_And_Correlation_Flows`
   - causa: expectativa de `200 OK` desatualizada
   - resultado final: **Passed**

3. **Critical Flow Identity/Auth**
   - teste: `NexTraceOne.IntegrationTests.CriticalFlows.CoreApiHostIntegrationTests.IdentityAccess_Should_Login_ListTenants_And_SelectTenant_And_Use_Real_CookieSession`
   - resultado final: **Passed**

### Revalidação complementar
- `ContractsNewFeaturesTests` → **10 passed**
- `ProtocolAutoDetectionTests` → **6 passed**

---

## 7. Correções aplicadas

### Frontend
- `src/frontend/tsconfig.app.json`
  - removido `include: ["src"]`
  - configurado `files: ["src/main.tsx"]`
  - mantido foco no grafo real da app
  - removidas exclusões artificiais de módulos de produto como mecanismo principal
- `src/frontend/src/types/index.ts`
  - reconstruído e expandido o barrel central com DTOs/aliases necessários ao build completo
- páginas ajustadas para consumir contratos reais/opcionais com segurança:
  - `AuditPage`
  - `ContractSourceOfTruthPage`
  - `ServiceDetailPage`
  - `ServiceSourceOfTruthPage`
  - `SourceOfTruthExplorerPage`
  - `ReleasesPage`
  - `WorkflowPage`
- `src/frontend/src/features/catalog/api/developerPortal.ts`
  - DTOs locais declarados no próprio módulo para eliminar drift desnecessário com o barrel global

### Testes
- `tests/platform/NexTraceOne.E2E.Tests/Infrastructure/ApiE2EFixture.cs`
  - corrigido SQL raw string da seed mínima
- `tests/platform/NexTraceOne.IntegrationTests/CriticalFlows/CoreApiHostIntegrationTests.cs`
  - alinhado status esperado de criação de incidente para `201 Created`

### Persistência
- removidos ficheiros de probe em:
  - `src/modules/identityaccess/NexTraceOne.IdentityAccess.Infrastructure/Persistence/Migrations/__Probe/*`

---

## 8. Resultado final do backend build

- `run_build` → **sucesso**
- `dotnet build src/platform/NexTraceOne.ApiHost/NexTraceOne.ApiHost.csproj -nologo` → **sucesso**
- `dotnet build src/platform/NexTraceOne.BackgroundWorkers/NexTraceOne.BackgroundWorkers.csproj -nologo` → **sucesso**
- `dotnet build src/platform/NexTraceOne.Ingestion.Api/NexTraceOne.Ingestion.Api.csproj -nologo` → **sucesso**

Observação:
- permanecem warnings de analyzer/qualidade e alguns warnings EF não bloqueantes
- não há bloqueador estrutural de compilação no backend nesta fase

---

## 9. Resultado final do frontend build

- `npm run typecheck` → **sucesso**
- `npm run build` → **sucesso**

Observação:
- o build final ainda gera warning de chunk grande no bundle principal
- isso é um gap de otimização, não um blocker técnico da ZR-1

---

## 10. Resultado final do startup do `ApiHost`

Revalidado por testes reais:

- startup validation → **passou**
- migrations → **aplicadas com sucesso**
- seed development → **aplicada com sucesso**
- host start → **sucesso**
- shutdown gracioso → **sucesso**

Veredito de startup nesta fase: **startup viável**

---

## 11. Resultado final da base de testes

### Amostra reexecutada nesta execução
- `CoreApiHostIntegrationTests.ChangeGovernance_And_Incidents_Should_Expose_Real_Read_Write_And_Correlation_Flows` → **Passed**
- `AuthApiFlowTests.GetCurrentUser_Me_Endpoint_Should_Return_User_Info` → **Passed**
- `CoreApiHostIntegrationTests.IdentityAccess_Should_Login_ListTenants_And_SelectTenant_And_Use_Real_CookieSession` → **Passed**
- `ContractsNewFeaturesTests` → **10 Passed**
- `ProtocolAutoDetectionTests` → **6 Passed**

### Leitura técnica
A base de testes agora distingue melhor:
- o que falha por comportamento real
- do que falhava apenas por drift estrutural

---

## 12. Blockers técnicos fechados

- build verde da solution
- build verde do frontend
- fixture E2E quebrada por SQL inválido
- suite crítica quebrada por expectativa HTTP desatualizada
- grafo principal do frontend sem dependência de exclusões artificiais amplas como mecanismo principal
- barrel de tipos do frontend realinhado ao consumo atual das páginas e APIs
- startup real do `ApiHost` com migrations e seed viáveis
- artefactos `__Probe` removidos do módulo `Identity`

---

## 13. Gaps técnicos remanescentes

### Não bloqueantes para ZR-1
- warnings EF de value comparer/sentinel no startup real
- warning `https port` no ambiente de teste
- warnings de analyzer (`CA1848`, `CA1859`, `CA1873`) no backend
- warning de chunk grande no build do frontend (`> 500 kB`)

### Fora do escopo desta fase
- conclusão funcional de módulos preview/parciais
- fechamento total de evidência UX/E2E sem mocks para todos os módulos
- hardening adicional de bundle splitting/performance

---

## 14. Veredicto da ZR-1

**CONCLUÍDA**

### Justificativa objetiva
Os critérios de sucesso da fase foram atingidos:

- `run_build` verde
- `npm run build` verde
- `ApiHost` com startup viável
- migrations/snapshots principais revalidados em startup real
- testes quebrados por drift técnico corrigidos
- repositório retirado do estado de quebra estrutural de base
