# EXECUTION BASELINE — PR1 até PR16

## Objetivo
Este documento estabelece a baseline de execução do NexTraceOne para tudo o que foi implementado do **PR-1 ao PR-16**. O foco é criar uma visão honesta do estado atual do produto, identificar o que já entrega valor real, o que está incompleto e o que precisa ser corrigido antes de avançar para novas ondas de evolução.

## Princípios
- Não considerar um PR como “concluído” apenas porque existe código.
- Validar sempre por **fluxo funcional ponta a ponta**.
- Priorizar valor central do NexTraceOne:
  - Change Confidence
  - Source of Truth de serviços e contratos
  - Incident Correlation & Mitigation
  - AI grounded, útil e auditável
- Tratar arquitetura, frontend, i18n, observabilidade e segurança como parte da entrega.

## Escopo desta baseline
A baseline cobre:
- código backend
- frontend
- endpoints e contratos de API
- i18n
- jobs e processamento assíncrono
- documentação relevante
- testes mínimos
- capacidade real de uso por persona

## Classificação obrigatória
Cada item avaliado deve receber uma destas classificações:
- **KEEP** — está bom e deve ser preservado
- **REFACTOR** — está útil, mas precisa de correção estrutural
- **COMPLETE** — existe parcialmente e precisa ser concluído
- **REMOVE** — não entrega valor ou atrapalha o foco
- **CREATE** — ainda não existe e é necessário

## Avaliação por cluster

### Cluster A — Fundação e arquitetura
Cobrir:
- modularidade e boundaries
- convenções de código
- i18n
- segurança base
- observabilidade base
- readiness / health
- configuração por ambiente

#### Checklist
- [x] Bounded contexts coerentes
- [x] Separação Domain / Application / Infrastructure / API respeitada
- [x] i18n aplicado nas telas críticas
- [x] Logs em inglês e sem vazamento de dados sensíveis
- [x] Exceptions técnicas em inglês
- [x] Health/readiness existentes e úteis
- [ ] Configuração externa por ambiente revisada

#### Estado atual
| Item | Estado | Classificação | Observações |
|---|---|---|---|
| Arquitetura modular | DONE | KEEP | 7 módulos isolados com DDD/CQRS, separação por .csproj, sem vazamentos de infraestrutura no Domain |
| Convenções de código | DONE | KEEP | Strongly-typed IDs, CancellationToken em async, Result pattern, guard clauses — verificado em todos os handlers |
| i18n base | DONE | KEEP | 4 locales (en, pt-PT, pt-BR, es), ~5,651 keys, 90+ componentes usando useTranslation |
| Segurança base | DONE | KEEP | JWT Bearer + permissions, rate limiting (100/min IP), CORS restritivo, assembly integrity check |
| Observabilidade base | DONE | KEEP | Serilog com console+file sinks, OpenTelemetry activity sources, métricas definidas |
| Health/Readiness | DONE | KEEP | /health, /ready, /live endpoints configurados com HealthCheckResponseWriter |

---

### Cluster B — Source of Truth e Contracts
Cobrir:
- catálogo de serviços
- ownership
- contratos REST / SOAP / Kafka / background services
- versionamento
- diff semântico
- compatibilidade
- busca e navegação
- detalhe de serviço e contrato

#### Checklist
- [x] Serviço pode ser cadastrado/importado
- [x] Contrato pode ser cadastrado/importado
- [x] Histórico de versões está acessível
- [x] Diff é utilizável
- [x] Compatibilidade é compreensível
- [x] Ownership por equipa/domínio está visível
- [x] Busca e filtros são úteis
- [x] Frontend suporta consulta real

#### Estado atual
| Item | Estado | Classificação | Observações |
|---|---|---|---|
| Catálogo de serviços | DONE | KEEP | ServiceAsset com EF persistence, CRUD completo, busca LIKE, filtros por team/domain/criticality |
| Detalhe de serviço | DONE | KEEP | ServiceDetailPage com API real, ownership, dependências, AssistantPanel com contexto |
| Catálogo de contratos | DONE | KEEP | ContractVersion com 7 lifecycle states, multi-protocol (OpenAPI/WSDL/AsyncAPI/Swagger) |
| Detalhe de contrato | DONE | KEEP | ContractDetailPage com spec viewer, diff view, lock/sign operations |
| Versionamento | DONE | KEEP | SemVer, import, create version, lock, deprecate, lifecycle transitions — tudo persisted |
| Diff semântico | DONE | KEEP | ContractDiffCalculator domain service, ComputeSemanticDiff handler, UI side-by-side |
| Compatibilidade | DONE | KEEP | ClassifyBreakingChange, GetCompatibilityAssessment, SuggestSemanticVersion — reais |
| Ownership | DONE | KEEP | TeamName, TechnicalOwner, BusinessOwner em ServiceAsset, UpdateServiceOwnership handler |
| Busca | DONE | KEEP | SearchServices, SearchContracts, SearchAssets, GlobalSearch — todos com LIKE patterns |

---

### Cluster C — Change Confidence
Cobrir:
- submissão de change
- evidence pack
- blast radius
- impacto em contratos/dependências
- advisory
- approval / reject / conditional approval
- rollout readiness
- trilha da decisão

#### Checklist
- [x] Change pode ser criada
- [x] Change pode ser listada e consultada
- [x] Vínculo com serviço/contrato funciona
- [x] Evidence readiness é visível
- [x] Blast radius é útil
- [x] Advisory é clara
- [x] Aprovação é auditável
- [x] Frontend suporta decisão real

#### Estado atual
| Item | Estado | Classificação | Observações |
|---|---|---|---|
| Create/List/Detail de change | DONE | KEEP | 21+ handlers reais, 4 DbContexts, EF migrations, paginação, filtros |
| Evidence pack | DONE | KEEP | GetChangeAdvisory agrega score+blast+rollback+release com 4 factores ponderados |
| Blast radius | DONE | KEEP | CalculateBlastRadius real: direct+transitive consumers, persistido em DB |
| Advisory | DONE | KEEP | 4-factor weighted scoring (Evidence 25%, BlastRadius 25%, Score 25%, Rollback 25%), recomendação Approve/Reject/Conditional |
| Approval flow | DONE | KEEP | RecordChangeDecision persiste ChangeEvent com decidedBy, rationale, conditions |
| Rollout readiness | DONE | KEEP | ComputeChangeScore, AssessRollbackViability, PostReleaseReview — todos reais |
| Decision history | DONE | KEEP | GetChangeDecisionHistory via IChangeEventRepository, timeline completa |

---

### Cluster D — Incident Correlation & Mitigation
Cobrir:
- lista e detalhe de incidentes
- correlação com changes
- correlação com serviços e dependências
- runbooks
- mitigação guiada
- validação pós-ação
- histórico do outcome

#### Checklist
- [x] Incident list/detail utilizáveis
- [x] Correlação com changes funciona
- [x] Correlação com serviços/dependências funciona
- [x] Runbooks estão acessíveis
- [x] Mitigação guiada funciona
- [x] Validação pós-ação existe
- [x] Outcome fica registrado

#### Estado atual
| Item | Estado | Classificação | Observações |
|---|---|---|---|
| Incident list/detail | DONE | KEEP | EfIncidentStore (678 linhas), IncidentDbContext com 5 DbSets, 6 seed incidents, CRUD real |
| Correlação com changes | PARTIAL | COMPLETE | CorrelatedChangesJson em IncidentRecord — funcional mas baseado em seed data, não dinâmico |
| Correlação com serviços | PARTIAL | COMPLETE | CorrelatedServicesJson + LinkedServicesJson — mesma limitação: seed data |
| Runbooks | DONE | KEEP | RunbookRecord com steps/prerequisites/post-notes, 3 seed runbooks, ListRunbooks/GetRunbookDetail reais |
| Mitigação guiada | DONE | KEEP | MitigationWorkflowRecord com Draft→Approved→InProgress→Completed, CreateMitigationWorkflow persiste em DB |
| Pós-validação | DONE | KEEP | MitigationValidationLog com checks individuais (Passed/Failed/PartiallyPassed), persistido em DB |
| Histórico | DONE | KEEP | GetMitigationHistory, UpdateMitigationWorkflowAction — audit trail completo |

---

### Cluster E — Integrações, escopo e governança
Cobrir:
- Integration Hub
- conectores prioritários
- freshness e health
- multi-team / multi-domain governance
- governance packs do PR-16

#### Checklist
- [ ] Conectores principais estão estáveis
- [ ] Freshness é visível
- [ ] Health é visível
- [ ] Escopo por equipa/domínio funciona
- [ ] Governance packs influenciam comportamento real
- [ ] Admin views são úteis

#### Estado atual
| Item | Estado | Classificação | Observações |
|---|---|---|---|
| Integration Hub | PARTIAL | COMPLETE | Frontend com 4 páginas e i18n; Ingestion API é stub (aceita mas não persiste) |
| Connector health | NOT STARTED | CREATE | Endpoints aceitam dados mas não processam nem armazenam |
| Freshness | NOT STARTED | CREATE | Página existe no frontend mas sem backend real |
| Scope por equipa/domínio | PARTIAL | COMPLETE | DelegatedAdmin domain model existe; backend não implementa enforcement |
| Governance packs | PARTIAL | REFACTOR | 9 entidades de domínio definidas; 20+ handlers retornam dados hardcoded; Infrastructure vazia |

---

### Cluster F — Analytics e hardening
Cobrir:
- product analytics do PR-14
- performance crítica
- jobs/background processing
- deployment/readiness
- logs e erros

#### Checklist
- [ ] Eventos analytics úteis
- [ ] Métricas de adoção relevantes
- [x] Páginas críticas com performance aceitável
- [x] Jobs estáveis
- [x] Health/readiness válidos
- [x] Logs úteis para operação

#### Estado atual
| Item | Estado | Classificação | Observações |
|---|---|---|---|
| Product analytics | PARTIAL | COMPLETE | 5 páginas frontend com i18n e layout; todos dados mock (mockSummary hardcoded) |
| Performance crítica | DONE | KEEP | Vite build com code-splitting; lazy-loaded routes; chunk warnings (>500kB) mas funcional |
| Jobs | DONE | KEEP | OutboxProcessorJob + IdentityExpirationJob + 5 expiration handlers — reais |
| Deployment/readiness | DONE | KEEP | /health, /ready, /live endpoints; HealthCheckResponseWriter configurado |
| Logs/diagnóstico | DONE | KEEP | Serilog com console+file sinks, daily rolling; OpenTelemetry activity sources + meters |

## Riscos principais identificados
| Risco | Impacto | Probabilidade | Mitigação |
|---|---|---|---|
| Arquitetura rica, mas fluxo incompleto | Alto | Alta | Fechar fluxos ponta a ponta |
| UI com muito conceito e pouco uso real | Alto | Média | Validar por tarefa real |
| PR executado sem valor mensurável | Alto | Alta | Exigir evidência funcional |
| IA pouco grounded | Alto | Média | Validar fontes e utilidade real |

## Saídas obrigatórias desta baseline
- lista consolidada de gaps
- lista de remoções/simplificações necessárias
- ordem de correção por valor de produto
- critérios de aceite da Onda 1
- recomendação formal de Go / No-Go para evolução pós-PR-16
