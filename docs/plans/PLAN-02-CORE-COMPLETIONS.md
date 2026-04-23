# Plano 02 — Core Product Completions

> **Prioridade:** 🔴 Alta  
> **Esforço total:** 4–6 semanas  
> **Contexto:** Itens parcialmente implementados ou pendentes identificados no `FUTURE-ROADMAP.md` waves A.1, A.2, A.4 e `HONEST-GAPS.md`

---

## CC-01 — SAML DEG-11: Promover de Nível A′ para Nível A

**Estado atual:** `ISamlService` / `ISamlConfigProvider` existe mas não aparece no `/admin/system-health` dashboard.  
**Referência:** `HONEST-GAPS.md` DEG-11, secção CFG-02.

**Implementação:**
1. Adicionar `bool IsConfigured { get; }` a `ISamlConfigProvider`
2. Implementar em `ConfigurationSamlProvider`: retorna `true` quando `identity.saml.idp_sso_url` está configurada e não vazia
3. Registar em `OptionalProviderNames.Saml` (seguindo padrão de `OptionalProviderNames.Canary`, `.Backup`, `.Kafka`, `.CloudBilling`)
4. Adicionar ao `GetOptionalProviders` query handler
5. `SystemHealthPage` frontend: mostrar SAML como 5º provider opcional com badge configured/not-configured
6. i18n: `samlProvider.*` em 4 locales
7. Testes: 8 unitários (IsConfigured true/false, registro no health dashboard)

**Ficheiros:**
- `src/modules/identityaccess/.../Services/ISamlConfigProvider.cs` (modificar)
- `src/modules/identityaccess/.../Services/ConfigurationSamlProvider.cs` (modificar)
- `src/building-blocks/.../OptionalProviders/OptionalProviderNames.cs` (adicionar Saml)
- `src/platform/NexTraceOne.ApiHost/Platform/GetOptionalProviders.cs` (modificar)

**Esforço:** 1 dia

---

## CC-02 — Promotion Readiness Delta: Completar Implementação

**Estado atual:** Backend slice existe (`GetPromotionReadinessDelta` query, endpoint, 8 testes), mas retorna `SimulatedNote` porque falta a bridge real com OI.  
**Referência:** `FUTURE-ROADMAP.md` Wave A.1 — "Pendências explícitas (a)...(d)".

**Implementação:**
1. **(a) Bridge real:** Registar `IRuntimeComparisonReader` na composition root (`ApiHost/Program.cs`) apontando para implementação real em `OperationalIntelligence.Infrastructure` que usa `CompareEnvironments` (já existe em OI)
2. **(b) Promotion Gate:** Adicionar `PromotionReadinessDeltaGate` nos gate evaluators de `PromotionModule` — gate não-bloqueante por defeito, controlado por config `promotion.readiness_delta.block_on_review`
3. **(c) UI no ReleaseTrain:** Adicionar cards de delta (error rate diff, latency diff, incident diff) + badge de readiness (Ready/Review/Blocked/Unknown) + sinalização de `SimulatedNote` quando sem dados reais
4. **(d) i18n:** `promotionReadinessDelta.*` em 4 locales

**Ficheiros:**
- `src/platform/NexTraceOne.ApiHost/Program.cs` (registar bridge)
- `src/modules/operationalintelligence/.../Services/RuntimeComparisonReader.cs` (criar implementação real)
- `src/modules/changegovernance/.../Gates/PromotionReadinessDeltaGate.cs` (novo)
- `src/frontend/src/features/change-governance/pages/ReleaseTrainPage.tsx` (adicionar delta UI)

**Esforço:** 4–5 dias

---

## CC-03 — Data Contracts: ContractType.DataContract

**Estado atual:** Não implementado — roadmapped em Wave A.2.  
**Referência:** `FUTURE-ROADMAP.md` Wave A.2, `ADR-007`.

**Implementação:**
1. Adicionar `DataContract` ao enum `ContractProtocol` (ou criar `ContractType.DataContract` separado)
2. Entidade `DataContractSchema`: `Owner`, `SlaFreshness`, `SchemaJson`, `PiiClassification` (None/Low/Medium/High/Critical), `SourceSystem`
3. `DataContractSpecParser`: parsing de schema de dados (colunas, tipos, nullable, PII tags)
4. Visual builder `VisualDataContractBuilder.tsx` no Contract Studio
5. Migration: tabela `ctr_data_contract_schemas`
6. Config keys: `contracts.data.pii_classification.enabled`, `contracts.data.sla_freshness.default_hours`
7. i18n: `dataContract.*` em 4 locales
8. Testes: 15+ unitários

**Esforço:** 1–1.5 semanas

---

## CC-04 — Contract-to-Consumer Tracking via OTel

**Estado atual:** Não implementado — roadmapped em Wave A.2.  
**Referência:** `FUTURE-ROADMAP.md` Wave A.2.

**Implementação:**
1. Entidade `ContractConsumerInventory`: (`ContractId`, `ConsumerService`, `ConsumerEnvironment`, `CalledAt`, `Version`, `FrequencyPerDay`)
2. `ContractConsumerIngestionJob` (Quartz, a cada 15min): lê traces do Elasticsearch/ClickHouse com `http.target` matching contratos publicados, persiste inventário via upsert
3. `GetContractConsumerInventory` query: lista consumidores reais por contrato com frequência
4. Endpoint: `GET /api/v1/catalog/contracts/{id}/consumers/real`
5. Migration: tabela `ctr_contract_consumer_inventory`
6. Config keys: `contracts.consumer_tracking.enabled`, `contracts.consumer_tracking.lookback_hours`
7. Testes: 12+ unitários

**Esforço:** 1 semana

---

## CC-05 — AI Evaluation Harness

**Estado atual:** ADR-009 aceite mas implementação pendente.  
**Referência:** `FUTURE-ROADMAP.md` Wave A.4, `ADR-009`.

**Implementação:**
1. Entidade `AiEvalDataset`: conjunto de pares (input, expected_output) por caso de uso de agente
2. Entidade `AiEvalRun`: execução de avaliação sobre um dataset com um modelo específico
3. Métricas por run: `ExactMatch`, `SemanticSimilarity` (cosine), `ToolCallAccuracy`, `Latency P50/P95`, `TokenCost`
4. `RunAiEvaluation` command: executa dataset contra modelo configurado, persiste resultados
5. `GetAiEvalReport` query: comparação de modelos em datasets comuns, tendências de qualidade
6. Endpoints: `POST /api/v1/ai/eval/datasets`, `POST /api/v1/ai/eval/runs`, `GET /api/v1/ai/eval/report`
7. Migration: tabelas `aik_eval_datasets`, `aik_eval_runs`, `aik_eval_results`
8. Config keys: `ai.eval.concurrent_requests`, `ai.eval.similarity_threshold`
9. i18n: `aiEval.*` em 4 locales
10. Testes: 20+ unitários

**Esforço:** 1–1.5 semanas

---

## CC-06 — Breaking Change Proposal Workflow

**Estado atual:** Não implementado — roadmapped em Wave A.2.  
**Referência:** `FUTURE-ROADMAP.md` Wave A.2.

**Implementação:**
1. Entidade `BreakingChangeProposal`: workflow formal antes de publicar versão com breaking changes
   - Estados: `Draft → ConsultationOpen → ConsumerResponded → ApprovalPending → Approved → Rejected`
   - Campos: `ContractId`, `ProposedBreakingChanges[]`, `MigrationWindowDays`, `DeprecationPlanId`
2. `ProposeBreakingChange` command: cria proposta com lista de breaking changes e notifica consumidores reais (via `INotificationModule`)
3. `RespondToBreakingChangeProposal` command: consumidor confirma ou rejeita impacto
4. `ApproveBreakingChangeProposal` command: aprova publicação após consulta
5. Integração com `ContractDriftDetection` (Wave A.2): bloqueio automático se consumidores ativos
6. Migration: tabelas `ctr_breaking_change_proposals`, `ctr_breaking_change_responses`
7. i18n: `breakingChangeProposal.*` em 4 locales
8. Testes: 18+ unitários

**Esforço:** 1 semana

---

## CC-07 — Predictive Blast Radius v2

**Estado atual:** Blast radius atual é heurístico (ownership + dependência). Falta correlação com traces OTel históricos e incidentes passados.  
**Referência:** `FUTURE-ROADMAP.md` Wave A.1.

**Implementação:**
1. Evoluir `BlastRadiusCalculator` para incluir:
   - Frequência histórica de chamadas OTel por par (service, contract) nos últimos 30 dias
   - Taxa de incidentes passados correlacionados ao serviço no par
   - Score final: `ProbabilityOfRegression` (0–100) por contrato consumido
2. Novo campo `ProbabilityOfRegression` no `BlastRadiusReport` response
3. Config keys: `blast_radius.v2.historical_lookback_days`, `blast_radius.v2.min_call_frequency`
4. Testes: 12+ unitários (casos com/sem histórico OTel)

**Esforço:** 4–5 dias

---

## CC-08 — Contract Linting Marketplace (Spectral Packages)

**Estado atual:** Spectral integration existe (`RulesetGovernance`); falta distribuição de pacotes via `ConfigurationDefinitionSeeder`.  
**Referência:** `FUTURE-ROADMAP.md` Wave A.2.

**Implementação:**
1. Seed 4 pacotes Spectral via `ConfigurationDefinitionSeeder`:
   - `spectral:enterprise` — regras de naming conventions, versioning obrigatório, operationId único
   - `spectral:security` — auth obrigatório, HTTPS only, rate limiting declarado
   - `spectral:accessibility` — descriptions obrigatórias, exemplos em todos os campos, deprecated corretamente marcado
   - `spectral:internal-platform` — regras internas do tenant, configuráveis
2. Endpoint `GET /api/v1/rulesets/marketplace` — lista pacotes disponíveis com versão e descrição
3. `ActivateSpectralPackage` command: ativa pacote para tenant com possibilidade de override de regras individuais
4. Config key: `spectral.marketplace.packages_enabled` (lista de pacotes ativos)
5. Testes: 10+ unitários

**Esforço:** 3 dias

---

## Ordem de Execução Recomendada

```
Semana 1:   CC-01 (SAML Level A — 1 dia) + CC-02 (Readiness Delta — 4 dias)
Semana 2:   CC-07 (Blast Radius v2) + CC-08 (Spectral Marketplace)
Semanas 3–4: CC-03 (Data Contracts)
Semana 5:   CC-04 (Consumer Tracking via OTel)
Semana 6:   CC-05 (AI Eval Harness) + CC-06 (Breaking Change Workflow)
```

## Critérios de Aceite

- [ ] SAML aparece como 5º provider em `/admin/system-health` com estado configured/not-configured
- [ ] `GetPromotionReadinessDelta` retorna dados reais (sem `SimulatedNote`) quando OI tem snapshots de runtime
- [ ] Contratos de dados (`DataContract`) criáveis via Contract Studio com classificação PII
- [ ] Consumer inventory atualizado automaticamente a cada 15min via job
- [ ] Avaliação de modelos IA executável via API com métricas de qualidade
- [ ] Breaking change em contrato com consumidores ativos dispara workflow de consulta automático
