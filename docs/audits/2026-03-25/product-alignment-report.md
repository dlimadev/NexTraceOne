# Relatório de Alinhamento de Produto — NexTraceOne

**Data:** 25 de março de 2026
**Objetivo:** Comparar o estado real do repositório com a visão oficial do produto conforme definida em `.github/copilot-instructions.md` e `PRODUCT-SCOPE.md`

---

## 1. Objetivo da Área no Contexto do Produto

Este relatório avalia se o NexTraceOne, como implementado, cumpre a sua missão declarada:

> "Fonte de verdade para serviços, contratos, mudanças, operação e conhecimento operacional."
> "Plataforma enterprise unificada para governança de serviços e contratos, change intelligence, confiança em mudanças de produção, confiabilidade operacional orientada por equipas, inteligência operacional assistida por IA, conhecimento operacional governado e otimização contextual de operação e custo."

---

## 2. Pilares Oficiais — Avaliação por Pilar

### Pilar 1: Service Governance

**Estado: PARTIAL**

**O que existe:**
- `CatalogGraphDbContext` com entidades `ApiAsset`, `ServiceAsset`, `ConsumerRelationship`, `ConsumerAsset`, `DiscoverySource`, `GraphSnapshot`
- Endpoints `ServiceCatalogEndpointModule` e `SourceOfTruthEndpointModule` no Catalog
- Frontend com `features/catalog/` com pages de Service Catalog, Source of Truth Explorer, Service Detail
- API calls reais: `serviceCatalogApi.getGraph()`, `getImpactPropagation()`, `getNodeHealth()`
- Governance module com `Team`, `GovernanceDomain` para ownership

**O que falta:**
- Service Lifecycle completo como entidade explícita (criação, depreciação, sunset)
- Service Metadata & Classification: campos de classificação (criticidade, domínio tecnológico) não verificados como first-class
- Team-to-service ownership como fluxo real integrado (existe no schema mas workflow UI completo não verificado)

**Evidência:** `src/modules/catalog/NexTraceOne.Catalog.Infrastructure/Graph/Persistence/CatalogGraphDbContext.cs`, `src/frontend/src/features/catalog/`

---

### Pilar 2: Contract Governance

**Estado: PARTIAL**

**O que existe:**
- `ContractsDbContext` com 11 entidades: `ContractVersion`, `ContractDraft`, `ContractReview`, `ContractDiff`, `ContractRuleViolation`, `ContractArtifact`, `ContractScorecard`, `ContractEvidencePack`, `ContractExample`, `CanonicalEntity`, `SpectralRuleset`
- Contract Studio completo no frontend com workflow Draft→InReview→Approved→Locked→Deprecated
- Check constraint: `protocol IN ('OpenApi', 'Swagger', 'Wsdl', 'AsyncApi', 'Protobuf', 'GraphQL')`
- Spectral ruleset validation, versionamento, diff, scorecard

**O que falta:**
- **SOAP Contracts / WSDL** — suporte declarado no check constraint (`Wsdl`) mas sem entidades ou fluxos específicos para SOAP/WSDL
- **Event Contracts / AsyncAPI** — declarado no constraint (`AsyncApi`) mas sem schema específico para event contracts (Kafka topics, bindings, schemas Avro)
- **Background Service Contracts** — não encontrado no schema
- **Contract Publication Center** — entidade existe? Fluxo de publicação para portal externo não verificado como funcional

**Evidência:** `src/modules/catalog/NexTraceOne.Catalog.Infrastructure/Contracts/Persistence/ContractsDbContext.cs:HasCheckConstraint`

---

### Pilar 3: Change Intelligence & Production Change Confidence

**Estado: PARTIAL**

**O que existe:**
- `ChangeIntelligenceDbContext` com 10 entidades reais: `Release`, `BlastRadiusReport`, `ChangeIntelligenceScore`, `ChangeEvent`, `ExternalMarker`, `FreezeWindow`, `ReleaseBaseline`, `ObservationWindow`, `PostReleaseReview`, `RollbackAssessment`
- Frontend com pages: Releases, Change Catalog, Change Detail, Workflow, Promotion
- API calls reais: `changeIntelligenceApi.listReleases()`, `getIntelligenceSummary()`, `checkFreezeConflict()`
- `PromotionDbContext` com: `DeploymentEnvironment`, `PromotionRequest`, `PromotionGate`, `GateEvaluation`
- `WorkflowDbContext` com: `WorkflowTemplate`, `WorkflowInstance`, `WorkflowStage`, `EvidencePack`, `SlaPolicy`, `ApprovalDecision`

**O que falta:**
- Correlação real entre telemetria observabilidade e releases (pipeline end-to-end não verificado)
- Release Calendar visual (FreezeWindow existe; calendário UI não verificado)
- Rollback Intelligence acionável (RollbackAssessment existe; fluxo de rollback guiado não verificado)
- Evidence Pack completo com evidências de pipeline CI/CD integradas automaticamente

**Evidência:** `src/modules/changegovernance/NexTraceOne.ChangeGovernance.Infrastructure/ChangeIntelligence/Persistence/ChangeIntelligenceDbContext.cs`

---

### Pilar 4: Operational Reliability

**Estado: PARTIAL**

**O que existe:**
- `IncidentDbContext` com 5 entidades: `IncidentRecord`, `MitigationWorkflowRecord`, `MitigationWorkflowActionLog`, `MitigationValidationLog`, `RunbookRecord`
- Frontend Incidents com pages reais e correlação `correlatedChanges`
- `ReliabilityDbContext` com `ReliabilitySnapshot`
- Runbooks management no frontend

**O que falta:**
- SLO tracking como entidade explícita (apenas ReliabilitySnapshot — não um SLO real)
- Post-change verification automatizada
- Service reliability scores como entidade persistida com histórico

**Evidência:** `src/modules/operationalintelligence/NexTraceOne.OperationalIntelligence.Infrastructure/Incidents/Persistence/IncidentDbContext.cs`

---

### Pilar 5: Operational Consistency

**Estado: PARTIAL**

**O que existe:**
- `AutomationDbContext` com 3 entidades: `AutomationWorkflowRecord`, `AutomationValidationRecord`, `AutomationAuditRecord`
- `RuntimeIntelligenceDbContext` com 4 entidades: `RuntimeSnapshot`, `RuntimeBaseline`, `DriftFinding`, `ObservabilityProfile`
- Frontend: Automation Workflows, Environment Comparison pages

**O que falta:**
- Runtime comparison entre ambientes como fluxo guiado completo
- Drift detection automatizada com alertas
- Operational consistency scoring por serviço/equipa

**Evidência:** `src/modules/operationalintelligence/NexTraceOne.OperationalIntelligence.Infrastructure/`

---

### Pilar 6: AI-assisted Operations & Engineering

**Estado: PARTIAL**

**O que existe:**
- AI Governance real: model registry, access policies, budgets, audit trail
- Runtime providers reais: OllamaProvider e OpenAiProvider
- 10 agentes especializados: ServiceHealthAnalyzer, ChangeImpactEvaluator, APIContractDraftGenerator, etc.
- Chat real com `ExecuteAiChat` handler chamando providers
- Frontend AI Hub com 12 pages reais

**O que falta:**
- Context grounding real (toggles existem mas injeção real de contexto não verificada)
- Tools execution (AiAgent.AllowedTools existe mas executor não wired)
- Streaming de respostas (Stream=false hardcoded)
- AssistantPanel em páginas de detalhe usa mock response generator

**Evidência:** `src/modules/aiknowledge/NexTraceOne.AIKnowledge.Application/Governance/`, `src/frontend/src/features/ai-hub/components/AssistantPanel.tsx`

---

### Pilar 7: Source of Truth & Operational Knowledge

**Estado: INCOMPLETE**

**O que existe:**
- `SourceOfTruthEndpointModule` no Catalog
- Frontend Source of Truth Explorer, Service Source of Truth, Contract Source of Truth
- CatalogGraph como modelo de relações

**O que falta:**
- **Knowledge Hub** — sem módulo dedicado de Knowledge Hub no backend
- Operational Notes como entidade persistida
- Search / Command Palette como capacidade cross-módulo (CommandPalette.tsx existe no frontend mas escopo não verificado)
- Knowledge Relations entre serviços, contratos, mudanças, incidentes

**Evidência:** Ausência de módulo `knowledge` em `src/modules/`

---

### Pilar 8: AI Governance & Developer Acceleration

**Estado: PARTIAL**

**O que existe:**
- Model registry completo (AIModel com 40+ propriedades)
- AIAccessPolicy com escopo (user, group, role, persona, team)
- AiTokenQuotaPolicy e AiTokenUsageLedger
- AIUsageEntry com 18 campos de auditoria
- AIIDEClientRegistration e AIIDECapabilityPolicy
- AiAgent com 10 especializados

**O que falta:**
- IDE Extensions reais (sem extensão VS Code ou Visual Studio no repositório)
- Policies configuráveis por ambiente (não verificado)
- Persona-specific AI UX (UI igual para todas as personas)

**Evidência:** `src/modules/aiknowledge/NexTraceOne.AIKnowledge.Infrastructure/Governance/Persistence/AiGovernanceDbContext.cs`

---

### Pilar 9: Operational Intelligence & Optimization

**Estado: PARTIAL**

**O que existe:**
- `RuntimeIntelligenceDbContext` com ObservabilityProfile, RuntimeSnapshot
- `CostIntelligenceDbContext` com 6 entidades: CostSnapshot, CostAttribution, CostTrend, ServiceCostProfile, CostImportBatch, CostRecord
- Frontend: Operational Intelligence pages, FinOps pages

**O que falta:**
- Pipeline analítico completo de ClickHouse → insights
- Anomaly detection real (configuração UI existe; lógica de detecção não verificada)
- ML-based optimization suggestions

**Evidência:** `src/modules/operationalintelligence/NexTraceOne.OperationalIntelligence.Infrastructure/Cost/Persistence/CostIntelligenceDbContext.cs`

---

### Pilar 10: FinOps Contextual

**Estado: PARTIAL**

**O que existe:**
- Entidades de custo: CostSnapshot, CostAttribution, CostTrend, ServiceCostProfile
- Frontend: Executive FinOps, Service FinOps, Team FinOps, Domain FinOps

**O que falta:**
- Pipeline real de importação/correlação de dados de custo com deployment/serviço/equipa
- Integração com provedores de billing cloud

**Evidência:** `src/frontend/src/features/governance/` (pages de FinOps), `CostIntelligenceDbContext`

---

## 3. Anti-Padrões Detectados

| Anti-Padrão | Encontrado? | Evidência |
|-------------|-------------|-----------|
| Produto virando observabilidade genérica | NÃO | Produto mantém foco em service/contract/change governance |
| Chat IA sem governança | NÃO | AIAccessPolicy, AiTokenQuotaPolicy, AIUsageEntry implementados |
| Telas bonitas mas vazias | PARCIALMENTE | AssistantPanel tem mock; demais telas têm integração real |
| Módulos escondidos porque incompletos | NÃO | releaseScope.ts não exclui nenhuma rota |
| Pedir GUID ao utilizador final | NÃO verificado | não encontrado em análise de UI |
| Misturar domínio com infra | PARCIALMENTE | GovernanceDbContext com 4 entidades de outros módulos |
| Quebrar isolamento entre módulos | NÃO | Comunicação via contracts/events |
| Endpoints "faz tudo" | NÃO verificado | Arquitetura CQRS sugere foco correcto |
| Duplicar fonte de verdade | PARCIALMENTE | Seeds legados com dados duplicados (arquivados) |
| Textos hardcoded fora de i18n | NÃO | 4.814 chaves i18n verificadas |
| Ignorar tenant, ambiente e persona | NÃO | TenantId, ICurrentEnvironment, Persona system implementados |
| Microserviços prematuros | NÃO | Modular monolith como definido |

---

## 4. Regras do Produto — Cumprimento

### 4.1 O produto NÃO deve virar...

| Proibição | Avaliação |
|-----------|-----------|
| Dashboard genérico de observabilidade | CUMPRIDO — observabilidade serve contexto de mudança/serviço |
| Catálogo de APIs isolado | PARCIALMENTE CUMPRIDO — existe catalog mas falta integração com change/incident |
| Repositório documental sem semântica operacional | CUMPRIDO — documentação ligada a serviços e contratos |
| Ferramenta genérica de incidentes | CUMPRIDO — incidentes correlacionam com mudanças |
| Chat com LLM sem governança | CUMPRIDO — AIAccessPolicy, audit, budgets implementados |
| Clone superficial de Grafana/Backstage/ServiceNow | CUMPRIDO — produto tem narrativa operacional própria |

---

## 5. Personas — Reflexo no Produto

| Persona | UI Adequada? | Sidebar Personalizado? | Quick Actions? | AI Contextual? |
|---------|-------------|----------------------|----------------|----------------|
| Engineer | SIM | SIM | SIM | PARCIAL |
| Tech Lead | SIM | SIM | SIM | PARCIAL |
| Architect | SIM | SIM | SIM | PARCIAL |
| Product | SIM | SIM | SIM | PARCIAL |
| Executive | SIM | SIM | SIM | PARCIAL |
| Platform Admin | SIM | SIM | SIM | PARCIAL |
| Auditor | SIM | SIM | SIM | PARCIAL |

**Nota:** Todas as 7 personas têm config de sidebar, quick actions e AI prompts no `PersonaContext`. A IA não diferencia o contexto de resposta por persona.

**Evidência:** `src/frontend/src/auth/persona.ts`, `src/frontend/src/contexts/PersonaContext.tsx`

---

## 6. Capacidades Ausentes vs Visão Oficial

As seguintes capacidades estão definidas na visão oficial mas **não encontradas no código**:

| Capacidade | Módulo Esperado | Estado |
|------------|-----------------|--------|
| SOAP Contracts workflow completo | Catalog | INCOMPLETE |
| Event Contracts / AsyncAPI completo | Catalog | INCOMPLETE |
| Background Service Contracts | Catalog | MISSING |
| Contract Publication Center | Catalog | INCOMPLETE |
| Knowledge Hub dedicado | Knowledge (ausente) | MISSING |
| Operational Notes | Knowledge | MISSING |
| Release Calendar visual | ChangeGovernance | INCOMPLETE |
| Licensing & Entitlements | Licensing (ausente) | MISSING |
| IDE Extensions reais | AIKnowledge | MISSING |
| Assembly Integrity / Anti-tamper | Security | PARTIAL |

---

## 7. Desvios do Target Técnico

| Target | Actual | Impacto |
|--------|--------|---------|
| React 18 | React 19.2.0 | BAIXO — upgrade, não downgrade |
| TanStack Router | React Router DOM 7.13.1 | BAIXO — funcional; migração possível |
| Radix UI | Componentes customizados | MÉDIO — acessibilidade pode ser menor |
| Apache ECharts | Sem biblioteca de charts | MÉDIO — dashboards sem gráficos ricos |
| Zustand | React Context + TanStack Query | BAIXO — padrão adequado |

---

## 8. Recomendação

O NexTraceOne está **estruturalmente alinhado** com a visão oficial, com identity, catalog, contracts, change intelligence, operations, AI governance e frontend todos parcial ou totalmente implementados. Os desvios são de **completude, não de direcção**.

**Prioridades de alinhamento:**
1. Completar SOAP/Event Contract types no schema (alinha Contract Governance)
2. Criar módulo Knowledge Hub no backend (alinha Source of Truth)
3. Criar módulo Licensing (requisito enterprise)
4. Completar ExternalAI domain (alinha AI Governance)
5. Integrar ClickHouse analytics com entidades de negócio (alinha Operational Intelligence)
6. Adicionar biblioteca de gráficos (Apache ECharts) para dashboards ricos
