# NexTraceOne — Plano MVP1 Expandido

## Estado da Base (Pós-Auditoria — Março 2026)

### Scaffolding Completo (14 módulos × 5 camadas)
Todos os 14 módulos têm estrutura de projetos criada, entities de domínio definidas,
features VSA esquelatadas e configurações EF Core prontas. A base é sólida.

### Implementação Real Funcional
| Módulo | Estado | Observações |
|--------|--------|-------------|
| **Building Blocks** (6 BBs) | ✅ 100% | Domain, Application, Infrastructure, EventBus, Observability, Security |
| **Identity** | ✅ 100% | Login, Federation, RefreshToken, CreateUser, AssignRole, Sessions |
| **Licensing** | ✅ 90% | ActivateLicense, VerifyOnStartup, CheckCapability, TrackUsage — DbContext/Repos stub |
| Todos os outros | 🟡 20% | Estrutura, Entities, Errors — Features são stubs com TODO |

---

## Análise de Valor vs. Esforço por Módulo

### Critérios de avaliação
- **Dependências técnicas**: quais módulos devem existir antes
- **Valor de negócio**: impacto para bancos, seguradoras, governo
- **Esforço**: complexidade de implementação (1=baixo, 5=alto)
- **Risco**: probabilidade de bloqueios externos

| # | Módulo | Deps | Valor | Esforço | Risco | Decisão |
|---|--------|------|-------|---------|-------|---------|
| 1 | Identity | — | ★★★★★ | Feito | — | ✅ IN |
| 2 | Licensing | — | ★★★★★ | Feito | Baixo | ✅ IN |
| 3 | EngineeringGraph | Identity | ★★★★★ | 3 | Baixo | ✅ IN |
| 4 | Contracts | EngineeringGraph | ★★★★★ | 3 | Médio |  ✅ IN |
| 5 | ChangeIntelligence | EngineeringGraph, Contracts | ★★★★★ | 4 | Baixo | ✅ IN |
| 6 | RulesetGovernance | ChangeIntelligence | ★★★★ | 3 | Baixo | ✅ IN |
| 7 | Workflow | ChangeIntelligence, RulesetGovernance | ★★★★★ | 4 | Baixo | ✅ IN |
| 8 | Promotion | Workflow | ★★★★ | 3 | Baixo | ✅ IN |
| 9 | Audit | Identity, ChangeIntelligence | ★★★★★ | 3 | Baixo | ✅ IN |
| 10 | DeveloperPortal | EngineeringGraph, Contracts | ★★★ | 2 | Baixo | ✅ IN |
| 11 | RuntimeIntelligence | EngineeringGraph | ★★★ | 4 | Alto | ⚠️ PARCIAL |
| 12 | CostIntelligence | ChangeIntelligence | ★★★ | 4 | Alto | ⚠️ PARCIAL |
| 13 | AiOrchestration | ChangeIntelligence, Contracts | ★★★ | 5 | Alto | ❌ MVP2 |
| 14 | ExternalAi | AiOrchestration | ★★ | 5 | Alto | ❌ MVP2 |

---

## Decisões de Inclusão/Exclusão

### ✅ Incluídos no MVP1 Expandido (10 módulos)

**EngineeringGraph** — CRÍTICO. Sem o grafo de dependências, o BlastRadius é impossível.
É o backbone técnico do sistema. As features de discovery (OTel, Kong, Backstage) são
adaptadores plugáveis. A implementação mínima (RegisterAsset + MapRelationship) desbloqueia
todos os módulos dependentes.

**Contracts** — CRÍTICO. Gestão de contratos OpenAPI com diff semântico e classificação
de breaking changes é o diferencial central do produto para bancos. Depende de EngineeringGraph
para vincular contratos a assets.

**ChangeIntelligence** — CORE DO PRODUTO. É o módulo central da plataforma.
Notificação de deploy, classificação de mudança, cálculo de blast radius e score
de risco são o coração comercial do NexTraceOne. Sem isso, não há produto.

**RulesetGovernance** — COMPLEMENTAR IMEDIATO. Linting de regras organizacionais
é exigido por regulação bancária (BACEN, FEBRABAN) e compliance de governo.
Complexidade baixa, valor alto. Depende de ChangeIntelligence.

**Workflow** — ESSENCIAL PARA APROVAÇÃO. Sem fluxo de aprovação multi-stage,
bancos e governo não adotarão. É o ponto de controle humano no processo.
O EvidencePack (PDF) já está esquelatado e é o entregável mais pedido.

**Promotion** — ENCADEIA COM WORKFLOW. PromotionGates avalia critérios antes
de promover entre ambientes (dev → staging → prod). Depende de Workflow.
Complexidade baixa, já modela o fluxo correto.

**Audit** — COMPLIANCE OBRIGATÓRIO. Bancos e governo precisam de trilha de
auditoria com hash chain (prova de integridade). RecordAuditEvent e
VerifyChainIntegrity são requisitos legais, não features. Esforço baixo.

**DeveloperPortal** — PORTAL DE CONSUMIDORES. Developers precisam de visibilidade
sobre APIs disponíveis, health e impacto. É read-model puro (sem aggregate) —
consome dados de EngineeringGraph e Contracts. Esforço muito baixo.

### ⚠️ Incluídos Parcialmente no MVP1

**RuntimeIntelligence** — Apenas o receptor OTLP passivo (ingestão de snapshots
via webhook/push). A análise de drift e correlação runtime↔deploy fica para MVP2.
O valor para o BlastRadius (evidência de tráfego real) justifica a ingestão básica.

**CostIntelligence** — Apenas ingestão de snapshots de custo (IngestCostSnapshot)
e correlação simples com releases (GetCostByRelease). A análise de tendências
e anomalias fica para MVP2. Banks precisam demonstrar custo de deployment.

### ❌ Excluídos do MVP1 (→ MVP2)

**AiOrchestration** — Classificação de mudança com IA generativa adiciona valor,
mas requer integração com LLMs externos, gestão de prompts, custos variáveis
e latência impredizível. O risco de bloqueio é alto. MVP2 após validação da
proposta de valor core.

**ExternalAi** — Depende de AiOrchestration. MVP2.

---

## MVP1 Expandido — Escopo Final

### Módulos Entregues
1. Building Blocks (6) — ✅ completo
2. Identity — ✅ completo
3. Licensing — ✅ completar Repositories/DI
4. EngineeringGraph — implementar features core
5. Contracts — implementar diff semântico e classificação
6. ChangeIntelligence — implementar pipeline completo
7. RulesetGovernance — implementar upload e execução de ruleset
8. Workflow — implementar initiate/approve/reject + EvidencePack
9. Promotion — implementar PromotionRequest e gates
10. Audit — implementar hash chain completo
11. DeveloperPortal — implementar read-models (SearchCatalog, GetApiDetail)
12. RuntimeIntelligence (parcial) — ingestão de snapshots
13. CostIntelligence (parcial) — ingestão e correlação com releases

---

## Novo Roadmap — Fases e Semanas

### Fase 1 — Fundação ✅ CONCLUÍDA (Semanas 1–4)

| Semana | Módulo | Features | Status |
|--------|--------|----------|--------|
| 1–2 | Building Blocks | Todos os 6 BBs | ✅ Completo |
| 3–4 | Identity | Login, Session, User, Role, RBAC | ✅ Completo |

### Fase 2 — Licenciamento e Grafo (Semanas 5–7)

| Semana | Módulo | Features Prioritárias |
|--------|--------|----------------------|
| 5 | Licensing | Completar Infrastructure (Repositories, Migrations), VerifyLicenseOnStartup funcional |
| 6 | EngineeringGraph | RegisterApiAsset, RegisterServiceAsset, MapConsumerRelationship, GetAssetGraph |
| 7 | EngineeringGraph | InferDependencyFromOtel (receptor passivo), ValidateDiscoveredDependency |

**Entregável:** Grafo de dependências funcional com APIs e consumidores mapeados.

### Fase 3 — Contratos e Portal (Semanas 8–10)

| Semana | Módulo | Features Prioritárias |
|--------|--------|----------------------|
| 8 | Contracts | ImportContract, GetContractHistory, ValidateContractIntegrity |
| 9 | Contracts | ComputeSemanticDiff, ClassifyBreakingChange, SuggestSemanticVersion |
| 10 | DeveloperPortal | SearchCatalog, GetApiDetail, GetMyApis, GetApiConsumers |

**Entregável:** Catálogo de APIs com diff semântico e classificação de breaking changes.

### Fase 4 — Change Intelligence (Semanas 11–14) — CORE DO PRODUTO

| Semana | Módulo | Features Prioritárias |
|--------|--------|----------------------|
| 11 | ChangeIntelligence | NotifyDeployment, ClassifyChangeLevel, UpdateDeploymentState |
| 12 | ChangeIntelligence | CalculateBlastRadius (consome EngineeringGraph), ComputeChangeScore |
| 13 | ChangeIntelligence | GetRelease, ListReleases, GetBlastRadiusReport, RegisterRollback |
| 14 | RulesetGovernance | UploadRuleset, ExecuteLintForRelease, GetLintResult |

**Entregável:** Pipeline completo de classificação de mudança com blast radius e score.

### Fase 5 — Governança e Aprovação (Semanas 15–18)

| Semana | Módulo | Features Prioritárias |
|--------|--------|----------------------|
| 15 | Workflow | CreateWorkflowTemplate, InitiateWorkflow |
| 16 | Workflow | ApproveStage, RejectWorkflow, GenerateEvidencePack, ExportEvidencePackPdf |
| 17 | Promotion | CreatePromotionRequest, EvaluatePromotionGates |
| 18 | Promotion | GetPromotionStatus, ApprovePromotion, RollbackPromotion |

**Entregável:** Fluxo de aprovação completo com evidence pack em PDF.

### Fase 6 — Auditoria e Compliance (Semanas 19–20)

| Semana | Módulo | Features Prioritárias |
|--------|--------|----------------------|
| 19 | Audit | RecordAuditEvent, GetAuditTrail, SearchAuditLog |
| 20 | Audit | VerifyChainIntegrity, ExportAuditReport, GetComplianceReport |

**Entregável:** Hash chain auditável com relatórios de compliance.

### Fase 7 — Intelligence Parcial (Semanas 21–22)

| Semana | Módulo | Features Prioritárias |
|--------|--------|----------------------|
| 21 | RuntimeIntelligence | IngestRuntimeSnapshot, GetRuntimeDriftFindings (básico) |
| 22 | CostIntelligence | IngestCostSnapshot, GetCostByRelease, AttributeCostToService |

**Entregável:** Correlação de custo e runtime com releases.

### Fase 8 — Hardening e Entrega (Semanas 23–26)

| Semana | Objetivo |
|--------|----------|
| 23 | CLI `nex` completo (validate, release, notify, approval, impact) |
| 24 | Testes de integração E2E completos + performance testing |
| 25 | Docker Compose completo + documentação de deploy + secrets management |
| 26 | Onboarding Accelerator + documentação de API + treinamento |

---

## Riscos da Expansão e Mitigações

| Risco | Probabilidade | Impacto | Mitigação |
|-------|--------------|---------|-----------|
| ChangeIntelligence depende de EngineeringGraph completo | Alta | Alto | Implementar EngineeringGraph mock/stub funcional antes de CI |
| Diff semântico de OpenAPI é complexo | Média | Médio | Usar biblioteca Microsoft.OpenApi para parsing; focar em diff estrutural primeiro |
| Workflow com SLA e escalação requer Quartz.NET configurado | Média | Médio | Implementar worker de SLA como BackgroundService simples primeiro |
| EvidencePack em PDF requer biblioteca de rendering | Baixa | Baixo | QuestPDF ou markdown → PDF; feature é desejável, não bloqueante |
| Licensing sem Repositories pode bloquear inicialização | Alta | Alto | **PRIORIDADE IMEDIATA**: completar repositories do Licensing na Fase 2 |
| Performance de blast radius em grafos grandes | Média | Alto | Paginação por nível de profundidade; limite configurável no MVP1 |

---

## Pré-requisitos para Início da Fase 2

Antes de iniciar EngineeringGraph, completar:

1. **Licensing Infrastructure**: `LicensingDbContext` com IUnitOfWork, `LicenseRepository`, migrations
2. **Licensing DI**: registrar em `AddLicensingInfrastructure`
3. **Testes de Licensing**: `LicenseTests` e `LicensingApplicationTests` passando
4. **ApiHost**: adicionar todos os módulos conforme são implementados

---

## Estimativa de Esforço Total MVP1 Expandido

| Fase | Semanas | Módulos |
|------|---------|---------|
| 1 — Fundação | 4 sem. ✅ | BBs + Identity |
| 2 — Grafo | 3 sem. | Licensing + EngineeringGraph |
| 3 — Contratos | 3 sem. | Contracts + DeveloperPortal |
| 4 — Change Intelligence | 4 sem. | ChangeIntelligence + RulesetGovernance |
| 5 — Governança | 4 sem. | Workflow + Promotion |
| 6 — Auditoria | 2 sem. | Audit |
| 7 — Intelligence | 2 sem. | RuntimeIntelligence (parcial) + CostIntelligence (parcial) |
| 8 — Hardening | 4 sem. | CLI + E2E + DevOps |
| **Total MVP1** | **26 sem.** | **13 módulos** |

---

## Próximos Passos Imediatos

1. Completar `LicensingDbContext`, `LicenseRepository` e migrations (Fase 2, Semana 5)
2. Implementar `EngineeringGraph` — `RegisterApiAsset` + `MapConsumerRelationship` (Fase 2, Semana 6)
3. Implementar `Contracts` — `ImportContract` + `ComputeSemanticDiff` (Fase 3, Semana 8)
4. Implementar `ChangeIntelligence` — `NotifyDeployment` + `CalculateBlastRadius` (Fase 4, Semana 11)
5. Registro no ApiHost de todos os módulos conforme implementados
