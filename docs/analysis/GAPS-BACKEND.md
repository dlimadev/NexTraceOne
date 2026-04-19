# NexTraceOne — Gaps do Backend

> **Parte da série de análise realista.** Ver [ESTADO-ATUAL-PRODUTO.md](./ESTADO-ATUAL-PRODUTO.md) para contexto.

---

## 1. Building Blocks — Observabilidade

### 1.1 ClickHouseObservabilityProvider — 6 Null Silenciosos

**Ficheiro:** `src/building-blocks/NexTraceOne.BuildingBlocks.Observability/Observability/Providers/ClickHouse/ClickHouseObservabilityProvider.cs`

**Problema:** Seis métodos da interface `IObservabilityProvider` retornam `null` sem logging, sem exceção, sem fallback explícito:

```csharp
// Linha 222, 236 — QueryMetricsAsync paths
return null;

// Linha 475 — GetServiceHealthSummaryAsync
return null;

// Linha 517, 549, 559 — Outros métodos de agregação
return null;
```

**Impacto:** Se um cliente configurar `OBSERVABILITY_PROVIDER=ClickHouse`, as funcionalidades de health summary, métricas e telemetria avançada retornam silenciosamente `null`. A UI mostrará dados vazios ou ausentes sem qualquer erro, tornando impossível distinguir "sem dados" de "provider quebrado".

**Não é um pattern de fallback intencional documentado** — o ficheiro não tem comentários explicando que estes métodos são intencionalmente não implementados.

**Solução necessária:** Ou implementar os métodos, ou adicionar `throw new NotSupportedException("Method X not yet implemented for ClickHouse provider")` com logging explícito.

---

### 1.2 ElasticObservabilityProvider — 4 Null Silenciosos

**Ficheiro:** `src/building-blocks/NexTraceOne.BuildingBlocks.Observability/Observability/Providers/Elastic/ElasticObservabilityProvider.cs`

**Problema:** O provider **primário** (Elasticsearch é o provider padrão) também tem null returns:

```csharp
// Linhas 227, 240 — QueryMetricsAsync edge cases
return null;

// Linhas 423, 430 — GetTraceDetailsAsync falha em certas condições
return null;

// Linhas 468, 483 — Agregações de span
return null;
```

**Impacto:** Mesmo no cenário padrão (Elasticsearch), certas queries de métricas e traces retornam null. A UI de observabilidade (LogExplorerPage, TraceExplorerPage) pode apresentar dados ausentes em condições específicas de query.

**Diferença em relação ao ClickHouse:** No Elastic, estes parecem ser edge cases de parsing (e.g., resposta mal formatada do ES), não métodos fundamentalmente não implementados. Mas a ausência de logging nos paths de `return null` dificulta o diagnóstico.

---

### 1.3 TelemetryContextEnricher — Null em Context Resolution

**Ficheiro:** `src/building-blocks/NexTraceOne.BuildingBlocks.Observability/Telemetry/TelemetryContextEnricher.cs:97`

```csharp
return null; // quando tenant ou environment não resolúvel
```

**Impacto:** Traces e logs podem ser emitidos sem contexto de tenant/environment, tornando impossível filtrar por tenant na telemetria.

---

## 2. Building Blocks — Segurança

### 2.1 ApiKeyAuthenticationHandler — Null em Validação

**Ficheiro:** `src/building-blocks/NexTraceOne.BuildingBlocks.Security/Authentication/ApiKeyAuthenticationHandler.cs:110`

```csharp
return null; // quando API key não encontrada no header
```

**Impacto:** Se o handler retornar null em vez de `AuthenticateResult.NoResult()`, o pipeline de autenticação pode ter comportamento indefinido. Deve retornar `Task<AuthenticateResult>` apropriado.

### 2.2 JwtTokenService — Null em Refresh Token

**Ficheiro:** `src/building-blocks/NexTraceOne.BuildingBlocks.Security/Authentication/JwtTokenService.cs:91,112`

**Problema:** Dois caminhos de validação de token retornam null — se o refresh token expirou de forma não padrão, o utilizador pode ficar preso sem mensagem de erro clara.

### 2.3 PermissionCodeMapper — Null para Permissões Desconhecidas

**Ficheiro:** `src/building-blocks/NexTraceOne.BuildingBlocks.Security/Authorization/PermissionCodeMapper.cs:62`

```csharp
return null; // quando código de permissão não mapeado
```

**Impacto:** Uma permissão não registada retorna null em vez de um erro explícito. Em cenários de RBAC, isto pode resultar em acesso negado silencioso ou, dependendo do consumer, num NullReferenceException não tratado.

---

## 3. Módulo AI Knowledge — Gaps Críticos

### 3.1 AiGovernanceModuleService — Cross-Module Interface Incompleta

**Ficheiro:** `src/modules/aiknowledge/NexTraceOne.AIKnowledge.Infrastructure/Governance/Services/AiGovernanceModuleService.cs`

```csharp
public Task<int?> GetConversationCount(...) => Task.FromResult<int?>(null); // linha 21
public Task<int?> GetActiveAgentCount(...) => Task.FromResult<int?>(null);  // linha 30
```

**Impacto:** O módulo Governance consulta `IAiGovernanceModule` para métricas no executive overview. Com estas retornando null, as métricas de uso de AI no painel executivo **nunca terão dados reais**.

### 3.2 AiOrchestrationModule — Stub de Cross-Module

**Ficheiro:** `src/modules/aiknowledge/NexTraceOne.AIKnowledge.Infrastructure/Orchestration/Services/AiOrchestrationModule.cs:31`

```csharp
return null;
```

**Impacto:** Qualquer módulo que consulte `IAiOrchestrationModule` receberá null, comprometendo funcionalidades dependentes de orquestração cross-module.

### 3.3 ExternalAiModule — Três Null Returns

**Ficheiro:** `src/modules/aiknowledge/NexTraceOne.AIKnowledge.Infrastructure/ExternalAI/Services/ExternalAiModule.cs:76,84,119`

**Impacto:** External AI routing (OpenAI, outros provedores externos) pode falhar silenciosamente. O routing adapter `ExternalAiRoutingPortAdapter.cs:206,227` também tem null returns, criando uma cadeia de falhas silenciosas.

### 3.4 AIKnowledgeSource — Null em Entity Method

**Ficheiro:** `src/modules/aiknowledge/NexTraceOne.AIKnowledge.Domain/Governance/Entities/AIKnowledgeSource.cs:146`

**Problema:** Um método de entidade de domínio retorna null, o que viola o princípio de que entidades de domínio devem ser sempre válidas.

### 3.5 AiModelCatalogService — Null para Modelos

**Ficheiro:** `src/modules/aiknowledge/NexTraceOne.AIKnowledge.Infrastructure/Runtime/Services/AiModelCatalogService.cs:42,65`

**Impacto:** Se o modelo não for encontrado no catálogo, retorna null em vez de um Result.Failure apropriado. Consumidores que não verificam null podem lançar NullReferenceException.

### 3.6 LmStudio e Ollama Clients — Null em Response Parsing

**Ficheiros:**
- `LmStudioHttpClient.cs:119`
- `OllamaHttpClient.cs:154`

**Problema:** Quando a resposta do LLM não tem o formato esperado, os clients retornam null. Em vez de um erro explícito de "resposta inesperada do LLM", o utilizador verá um assistente que simplesmente não responde.

---

## 4. Módulo Catalog

### 4.1 GraphQL CatalogQuery — Null em Queries

**Ficheiro:** `src/modules/catalog/NexTraceOne.Catalog.API/GraphQL/CatalogQuery.cs:129`

**Problema:** Uma query GraphQL retorna null, o que viola o contrato GraphQL (deve retornar erro tipado, não null).

### 4.2 PushToRepository — Não Integra VCS Real

**Ficheiro:** `src/modules/catalog/NexTraceOne.Catalog.Application/Portal/Features/PushToRepository/PushToRepository.cs`

**Problema:** O feature "Exportar para Repositório" é promovido como funcionalidade do Developer Portal, mas o handler **apenas gera comandos git para copy-paste**. Não há integração real com GitHub API, GitLab API, ou Azure DevOps API.

O handler gera literalmente:
```
git clone https://github.com/... .
git checkout -b feature/my-service
# Copy generated files to the repository directory
# File: Controllers/MyController.cs (1234 bytes)
git add .
git commit -m "feat: scaffold 3 files via NexTraceOne"
git push origin feature/my-service
```

**Impacto:** A UI provavelmente apresenta isto como "push para repositório", mas o utilizador tem de copiar e executar manualmente. Se isto não estiver bem comunicado na UI, é uma funcionalidade enganosa.

**Nota:** O comentário no código diz "Em versão futura, esta feature pode invocar directamente os conectores de Integrations" — o que confirma que é intencionalmente incompleto, mas não está marcado como PREVIEW na UI.

### 4.3 Developer Portal SearchCatalog — Stub

**Ficheiro:** `src/modules/catalog/NexTraceOne.Catalog.Infrastructure/Contracts/Services/ContractsModuleService.cs:125`

```csharp
return null; // SearchCatalog cross-module — stub
```

**Impacto:** A busca no Developer Portal que deveria cruzar módulos (serviços, contratos, documentação) retorna null. O `GlobalSearchPage` tem acesso parcial, mas a integração cross-module está pendente.

### 4.4 GenerateServerFromContract — Stubs com TODO no Código Gerado

**Ficheiro:** `src/modules/catalog/NexTraceOne.Catalog.Application/Portal/ContractPipeline/Features/GenerateServerFromContract/GenerateServerFromContract.cs`

O endpoint é documentado como PREVIEW mas gera código com TODOs como parte do output:
```
// TODO: Inject application services and implement endpoint handlers
// TODO: Inject services and implement handlers
// TODO: Implement handlers for {{serviceName}}
// TODO: decode body and persist
```

**Impacto:** O código gerado não é production-ready. Isto pode estar OK como feature de scaffold, mas precisa estar explicitamente comunicado na UI que o código gerado requer implementação manual adicional.

### 4.5 GenerateMigrationPatch — TODOs no Output Gerado

**Ficheiro:** `src/modules/catalog/NexTraceOne.Catalog.Application/Contracts/Features/GenerateMigrationPatch/GenerateMigrationPatch.cs`

Gera patches de migração com comentários como:
```
// TODO: Update the handler/controller for this route to match the new contract
// TODO: Review and update the controller/handler that implements this operation.
// TODO: Add handler for this new endpoint or field
```

**Notas:** Isto é intencional como feature de geração de código (o utilizador deve rever). Mas os TODOs gerados são indistinguíveis de TODOs do próprio código da plataforma se não for bem comunicado.

---

## 5. Módulo Operational Intelligence

### 5.1 Correlação Incident↔Change é Básica

**Documentação interna diz:** "Gap remanescente: Heurísticas de correlação incident↔change são básicas (timestamp+service matching)."

**O que isto significa na prática:**
- Um incidente que ocorre 2 horas depois de um deploy será correlacionado automaticamente
- Mas se o incidente ocorrer 3 dias depois (problema de latência progressiva), a correlação **não acontece**
- O sistema não usa ML, análise de patterns, ou correlação de métricas para inferir causalidade

**Impacto:** A funcionalidade "Change Intelligence" que é core do produto tem correlação automática limitada. Em produção real, a maioria das correlações terão de ser feitas manualmente.

---

## 6. Módulo Change Governance

### 6.1 WorkflowModuleService — Null Returns

**Ficheiro:** `src/modules/changegovernance/NexTraceOne.ChangeGovernance.Infrastructure/Workflow/Services/WorkflowModuleService.cs`

**Problema confirmado por grep:** Contém `return null` em caminhos de cross-module. Módulos que dependem de `IWorkflowModule` para verificar estados de aprovação podem receber null.

---

## 7. Módulo Configuration

### 7.1 ExportEndpointModule — Funcionalidade Incompleta

**Ficheiro:** `src/modules/configuration/NexTraceOne.Configuration.API/Endpoints/ExportEndpointModule.cs`

**Problema confirmado por grep:** Contém `return null`, sugerindo que a funcionalidade de exportação de configurações pode estar incompleta.

---

## 8. GraphQL — Cobertura de Federação Incompleta

**Estado prometido (ADR-006):** GraphQL federation para todos os módulos

**Estado real:**
- `CatalogQuery` — Implementado (services, contracts, npsSummary)
- `ChangeGovernanceQuery` — Implementado (changesSummary)
- Todos os outros 10 módulos — **SEM resolvers GraphQL**

Os endpoints GraphQL existentes funcionam, mas a "federation" prometida no ADR-006 cobre apenas 2 de 12 módulos. O ADR-006 é classificado como "Futuro" — o que está correto — mas a documentação interna lista GraphQL como "READY" sem esclarecer que é apenas parcialmente federado.

---

## 9. Outbox Pattern — Gaps Operacionais

### 9.1 Sem Dead Letter Queue

O Outbox tem 25 processors (um por DbContext), mas:
- Não há mecanismo de Dead Letter Queue para mensagens que falham repetidamente
- Não há UI para operators verem mensagens com falha
- Não há alerta quando uma mensagem excede o número máximo de tentativas
- A tabela de outbox pode crescer indefinidamente se um processador estiver quebrado

### 9.2 Sem Monitoring Dashboard

Em produção, não há forma de responder a:
- "Quantas mensagens de outbox estão pendentes agora?"
- "Alguma mensagem está bloqueada há mais de 1 hora?"
- "Qual processor está atrasado?"

---

## 10. Background Workers — Gaps

### 10.1 Sem Graceful Shutdown Documentado

Os background workers (Quartz jobs) não têm documentação sobre como fazer graceful shutdown durante deploys. Em Kubernetes, o `terminationGracePeriodSeconds` precisa ser calibrado com a duração máxima dos jobs.

### 10.2 Assembly Integrity Checker — Risco de Falso Positivo

**Ficheiro:** `src/platform/NexTraceOne.ApiHost/` (startup)

O `AssemblyIntegrityChecker` pode bloquear o startup se os assemblies forem recompilados com flags diferentes (e.g., otimizações diferentes em CI vs. local). Não há documentação sobre como proceder quando o checker falha legitimamente após um build válido.

---

## 11. Resumo de Prioridades

### Crítico — Corrigir Antes de Produção

1. Connection pool sizing (ver [GAPS-DATABASE-TESTES.md](./GAPS-DATABASE-TESTES.md))
2. Null silencioso no ClickHouseObservabilityProvider
3. Null silencioso nos AI service modules (cross-module)
4. Dead letter queue para Outbox

### Alto — Corrigir na Próxima Sprint

5. ElasticObservabilityProvider null paths com logging
6. ExternalAiModule e routing adapter null paths
7. PushToRepository — comunicar claramente que é "instrução de push", não push automático
8. Developer Portal SearchCatalog — implementar ou remover da UI

### Médio — Planear para Próximo Quarter

9. GraphQL federation para módulos restantes
10. Incident↔Change correlation com ML básico
11. PermissionCodeMapper null → erro explícito
12. Workflow e outros cross-module services com null returns
