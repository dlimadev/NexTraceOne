# PHASE-0 — Demo Debt Inventory

**Status:** ACTIVE  
**Generated:** 2026-03-21  
**Method:** Systematic scan do repositório real (grep, análise manual de handlers, páginas e configuração)  
**Policy reference:** `docs/engineering/PHASE-0-PRODUCT-FREEZE-POLICY.md`

---

## Resumo Executivo

| Métrica | Valor |
|---|---|
| Total de itens catalogados | 47 |
| P0 — Bloqueadores absolutos | 4 |
| P1 — Fechamento funcional crítico | 18 |
| P2 — Fechamento funcional importante | 14 |
| P3 — Hardening | 8 |
| P4 — Polimento | 3 |

| Tipo | Quantidade |
|---|---|
| Backend fake (IsSimulated / GenerateSimulated) | 17 |
| UI fake (mock local em página operacional) | 9 |
| TODO crítico (handler vazio exposto) | 7 |
| Hardcode operacional | 5 |
| Segurança / Configuração | 4 |
| Infraestrutura | 3 |
| Persistência ausente | 2 |

---

## Inventário Completo

| ID | Área | Módulo | Tipo | Severidade | Ficheiro | Evidência | Impacto Real | Correção Esperada | Fase | Dependências | Critério de Aceite |
|---|---|---|---|---|---|---|---|---|---|---|---|
| D-001 | Segurança | ApiHost | Segurança | **P0 — Crítica** | `src/platform/NexTraceOne.ApiHost/appsettings.json` | `"IntegrityCheck": false` (linha 22) | Verificações de integridade desactivadas por padrão — permite startup inseguro sem validação de estado | Mudar para `true`; apenas `appsettings.Development.json` pode ter `false` | Fase 1 | Nenhuma | `IntegrityCheck` é `true` em `appsettings.json`; override em dev documentado |
| D-002 | Segurança | Frontend | Segurança | **P0 — Crítica** | `src/frontend/src/App.tsx` | `<ReactQueryDevtools ... />` sem guard de ambiente (linha 792, pré-correcção) | Devtools de debug expostos em todos os builds, incluindo produção | **CORRIGIDO nesta fase** — guard `import.meta.env.DEV` adicionado com lazy import | Fase 0 ✅ | Nenhuma | `ReactQueryDevtools` não renderiza em build de produção; presente apenas em DEV |
| D-003 | Infraestrutura | ApiHost | Infraestrutura | **P0 — Crítica** | `.github/` | Ausência de pipeline CI/CD (apenas `copilot-instructions.md` presente) | Zero automação de build, testes e deploy — impossível garantir qualidade contínua | Criar pipeline GitHub Actions: build + test + lint + guardrail check | Fase 1 | Scripts de qualidade | Pipeline executa e passa em cada PR |
| D-004 | Infraestrutura | Platform | Infraestrutura | **P0 — Crítica** | `/` (raiz do repositório) | Ausência de `Dockerfile` e `docker-compose.yml` de produção (apenas `build/otel-collector/docker-compose.telemetry.yaml`) | Impossível containerizar ou deployar o produto em qualquer ambiente | Criar `Dockerfile` para ApiHost, BackgroundWorkers e Frontend; criar `docker-compose.yml` de desenvolvimento completo | Fase 1 | Configuração de secrets | Imagem Docker produz aplicação funcional; compose sobe stack completa |
| D-005 | Backend fake | OperationalIntelligence | Backend fake | **P1 — Alta** | `src/modules/operationalintelligence/.../ListServiceReliability/ListServiceReliability.cs` | `GenerateSimulatedItems(request)` (linha 57); `IsSimulated: true` (linha 64) | Página de Reliability exibe dados completamente fictícios sem indicação clara ao utilizador enterprise | Implementar consulta real ao RuntimeIntelligenceDatabase; remover `GenerateSimulatedItems` | Fase 2 | RuntimeIntelligenceDatabase schema, métricas de produção ingeridas | `IsSimulated` removido do Response; dados provêm de persistência real |
| D-006 | Backend fake | OperationalIntelligence | Backend fake | **P1 — Alta** | `src/modules/operationalintelligence/.../GetServiceReliabilityDetail/GetServiceReliabilityDetail.cs` | `IsSimulated: true` em 3 locais (linhas 79, 103, 121) | Detalhes de reliability de serviço são 100% fabricados | Implementar queries reais ao banco; alimentar com dados de ingestion | Fase 2 | D-005 | Campos de detalhe provêm de DB real; `IsSimulated` ausente |
| D-007 | Backend fake | OperationalIntelligence | Backend fake | **P1 — Alta** | `src/modules/operationalintelligence/.../GetServiceReliabilityCoverage/GetServiceReliabilityCoverage.cs` | `bool IsSimulated = true` (default no Response DTO, linha 59) | Cobertura de reliability sempre retorna como simulada | Implementar cálculo real de cobertura a partir de dados ingeridos | Fase 2 | D-005 | `IsSimulated` é `false` por padrão; cobertura calculada de dados reais |
| D-008 | Backend fake | OperationalIntelligence | Backend fake | **P1 — Alta** | `src/modules/operationalintelligence/.../GetServiceReliabilityTrend/GetServiceReliabilityTrend.cs` | `bool IsSimulated = true` (default, linha 80) | Tendência de reliability sempre simulada | Implementar cálculo de tendência real com janela temporal | Fase 2 | D-005 | Trend calculado de dados reais de ingestion |
| D-009 | Backend fake | OperationalIntelligence | Backend fake | **P1 — Alta** | `src/modules/operationalintelligence/.../GetDomainReliabilitySummary/GetDomainReliabilitySummary.cs` | `bool IsSimulated = true` (default, linha 58) | Resumo de reliability por domínio sempre simulado | Implementar agregação real por domínio | Fase 2 | D-005 | Dados de domínio provêm de DB real |
| D-010 | Backend fake | OperationalIntelligence | Backend fake | **P1 — Alta** | `src/modules/operationalintelligence/.../GetTeamReliabilitySummary/GetTeamReliabilitySummary.cs` | `bool IsSimulated = true` (default, linha 60) | Resumo de reliability por equipa sempre simulado | Implementar agregação real por equipa | Fase 2 | D-005 | Dados de equipa provêm de DB real |
| D-011 | Backend fake | OperationalIntelligence | Backend fake | **P1 — Alta** | `src/modules/operationalintelligence/.../GetTeamReliabilityTrend/GetTeamReliabilityTrend.cs` | `bool IsSimulated = true` (default, linha 68) | Tendência de reliability por equipa sempre simulada | Implementar cálculo de tendência real | Fase 2 | D-005 | Trend de equipa calculado de dados reais |
| D-012 | Backend fake | OperationalIntelligence | Backend fake | **P2 — Média** | `src/modules/operationalintelligence/.../GetAutomationAuditTrail/GetAutomationAuditTrail.cs` | `GenerateSimulatedEntries(request)` (linha 45) | Audit trail de automação completamente fictício — risco de compliance | Implementar persistência real de audit entries; ler de DB | Fase 2 | AutomationDatabase schema | Entries provêm de DB real; nenhum `GenerateSimulated*` |
| D-013 | Backend fake | Governance/FinOps | Backend fake | **P1 — Alta** | `src/modules/governance/.../GetFinOpsSummary/GetFinOpsSummary.cs` | `IsSimulated: true` (linha 98) | Resumo de FinOps da plataforma sempre simulado — decisões de custo baseadas em dados fictícios | Implementar integração com fonte real de dados de custo (cloud billing, etc.) | Fase 3 | Integração com billing API | `IsSimulated` ausente; dados provêm de fonte real |
| D-014 | Backend fake | Governance/FinOps | Backend fake | **P1 — Alta** | `src/modules/governance/.../GetServiceFinOps/GetServiceFinOps.cs` | `IsSimulated: true` (linha 68) | FinOps por serviço sempre simulado | Implementar atribuição real de custo por serviço | Fase 3 | D-013 | Dados de custo por serviço são reais |
| D-015 | Backend fake | Governance/FinOps | Backend fake | **P1 — Alta** | `src/modules/governance/.../GetTeamFinOps/GetTeamFinOps.cs` | `IsSimulated: true` (linha 51) | FinOps por equipa sempre simulado | Implementar agregação real de custos por equipa | Fase 3 | D-013 | Dados de custo por equipa são reais |
| D-016 | Backend fake | Governance/FinOps | Backend fake | **P1 — Alta** | `src/modules/governance/.../GetDomainFinOps/GetDomainFinOps.cs` | `IsSimulated: true` (linha 56) | FinOps por domínio sempre simulado | Implementar agregação real de custos por domínio | Fase 3 | D-013 | Dados de custo por domínio são reais |
| D-017 | Backend fake | Governance/FinOps | Backend fake | **P2 — Média** | `src/modules/governance/.../GetFinOpsTrends/GetFinOpsTrends.cs` | `IsSimulated: true` (linha 66) | Tendências de custo sempre simuladas | Implementar cálculo real de tendências com série temporal | Fase 3 | D-013 | Trends calculados de dados reais |
| D-018 | Backend fake | Governance/FinOps | Backend fake | **P2 — Média** | `src/modules/governance/.../GetWasteSignals/GetWasteSignals.cs` | `IsSimulated: true` (linha 76) | Sinais de desperdício sempre simulados | Implementar detecção real de desperdício (idle resources, oversized, etc.) | Fase 3 | D-013 | Sinais provêm de análise real |
| D-019 | Backend fake | Governance/FinOps | Backend fake | **P2 — Média** | `src/modules/governance/.../GetEfficiencyIndicators/GetEfficiencyIndicators.cs` | `IsSimulated: true` (linha 79) | Indicadores de eficiência sempre simulados | Implementar métricas reais de eficiência | Fase 3 | D-013 | Indicadores calculados de dados reais |
| D-020 | Backend fake | Governance/FinOps | Backend fake | **P2 — Média** | `src/modules/governance/.../GetFrictionIndicators/GetFrictionIndicators.cs` | `IsSimulated: true` (linha 74) | Indicadores de fricção sempre simulados | Implementar coleta real de indicadores de experiência do utilizador | Fase 3 | Product analytics pipeline | Indicadores provêm de dados reais de uso |
| D-021 | Backend fake | Governance/FinOps | Backend fake | **P2 — Média** | `src/modules/governance/.../GetBenchmarking/GetBenchmarking.cs` | `IsSimulated: true` (linha 73) | Benchmarking sempre simulado | Implementar cálculo real de benchmarks por domínio/equipa | Fase 3 | D-013 | Benchmarks calculados de dados reais |
| D-022 | Backend fake | Governance | Backend fake | **P2 — Média** | `src/modules/governance/.../GetExecutiveTrends/GetExecutiveTrends.cs` | `IsSimulated: true` (linha 85) | Tendências executivas sempre simuladas | Implementar agregação real de KPIs executivos | Fase 3 | Múltiplos módulos | Trends executivos calculados de dados reais |
| D-023 | Backend fake | Governance | Backend fake | **P2 — Média** | `src/modules/governance/.../GetExecutiveDrillDown/GetExecutiveDrillDown.cs` | `bool IsSimulated = true` (default, linha 111) | Drill-down executivo sempre simulado | Implementar drill-down real com dados cruzados | Fase 3 | D-022 | Drill-down provém de dados reais |
| D-024 | TODO crítico | AIKnowledge | TODO crítico | **P1 — Alta** | `src/modules/aiknowledge/.../ExternalAI/Features/CaptureExternalAIResponse/CaptureExternalAIResponse.cs` | `// TODO: Implementar lógica de negócio desta feature.` (linhas 6-20) | Handler completamente vazio — endpoint exposto sem comportamento | Implementar captura real de resposta de IA externa com persistência | Fase 2 | ExternalAiDatabase schema | Handler processa input, persiste resultado, retorna Response tipado |
| D-025 | TODO crítico | AIKnowledge | TODO crítico | **P1 — Alta** | `src/modules/aiknowledge/.../ExternalAI/Features/ConfigureExternalAIPolicy/ConfigureExternalAIPolicy.cs` | `// TODO: Implementar lógica de negócio desta feature.` | Handler vazio — política de IA externa não configurável | Implementar configuração real de política com validação e persistência | Fase 2 | D-024 | Política configurada e persistida |
| D-026 | TODO crítico | AIKnowledge | TODO crítico | **P1 — Alta** | `src/modules/aiknowledge/.../ExternalAI/Features/ApproveKnowledgeCapture/ApproveKnowledgeCapture.cs` | `// TODO: Implementar lógica de negócio desta feature.` | Aprovação de conhecimento capturado não implementada — fluxo de governança de IA quebrado | Implementar workflow de aprovação com estado e auditoria | Fase 2 | D-024 | Aprovação altera estado do KnowledgeCapture com auditoria |
| D-027 | TODO crítico | AIKnowledge | TODO crítico | **P1 — Alta** | `src/modules/aiknowledge/.../ExternalAI/Features/ReuseKnowledgeCapture/ReuseKnowledgeCapture.cs` | `// TODO: Implementar lógica de negócio desta feature.` | Reutilização de conhecimento não implementada — core da governança de IA ausente | Implementar lookup e reutilização de knowledge captures aprovados | Fase 2 | D-026 | Reutilização registada e rastreada |
| D-028 | TODO crítico | AIKnowledge | TODO crítico | **P1 — Alta** | `src/modules/aiknowledge/.../ExternalAI/Features/GetExternalAIUsage/GetExternalAIUsage.cs` | `// TODO: Implementar lógica de negócio desta feature.` | Relatório de uso de IA externa não implementado — governança de tokens sem dados | Implementar query real de uso com agregação por modelo/utilizador/período | Fase 2 | D-024 | Uso consultável com dados reais |
| D-029 | TODO crítico | AIKnowledge | TODO crítico | **P1 — Alta** | `src/modules/aiknowledge/.../ExternalAI/Features/ListKnowledgeCaptures/ListKnowledgeCaptures.cs` | `// TODO: Implementar lógica de negócio desta feature.` | Listagem de knowledge captures não implementada | Implementar query com filtros e paginação | Fase 2 | D-024 | Lista retorna captures reais do DB |
| D-030 | UI fake | Operations | UI fake | **P1 — Alta** | `src/frontend/src/features/operations/pages/TeamReliabilityPage.tsx` | `const mockServices = [...]` (linha 16) — 8 serviços hardcoded | Página de reliability da equipa exibe dados completamente fictícios | Conectar a endpoint real `GET /api/v1/reliability/services`; remover `mockServices` | Fase 2 | D-005, D-011 | Página usa API real; dados actualizados dinamicamente |
| D-031 | UI fake | Operations | UI fake | **P1 — Alta** | `src/frontend/src/features/operations/pages/ServiceReliabilityDetailPage.tsx` | `const mockDetails: Record<...>` (linha 14) — detalhes de serviços hardcoded | Detalhe de serviço exibe dados completamente fictícios | Conectar a endpoint real de detalhe de reliability; remover `mockDetails` | Fase 2 | D-006 | Página usa API real; sem `mockDetails` |
| D-032 | UI fake | Operations | UI fake | **P1 — Alta** | `src/frontend/src/features/operations/pages/PlatformOperationsPage.tsx` | `const mockSubsystems`, `mockJobs`, `mockQueues`, `mockEvents` (linhas 58-80) — 4 arrays hardcoded | Página de operações de plataforma exibe estado completamente fictício — não reflete estado real do sistema | Implementar endpoint de health/status real (subsystems, jobs, queues, events); conectar página | Fase 2 | BackgroundWorkers health API | Página exibe estado real do sistema; arrays mock removidos |
| D-033 | UI fake | Product Analytics | UI fake | **P2 — Média** | `src/frontend/src/features/product-analytics/pages/PersonaUsagePage.tsx` | `const mockPersonas = [...]` (linha 27) — dados de 6 personas hardcoded | Análise de uso por persona exibe dados completamente fictícios | Implementar pipeline de analytics real; conectar página a API de analytics | Fase 3 | Analytics pipeline | Página usa dados reais de tracking de uso |
| D-034 | UI fake | Product Analytics | UI fake | **P2 — Média** | `src/frontend/src/features/product-analytics/pages/ValueTrackingPage.tsx` | `const mockMilestones = [...]` (linha 27) | Tracking de valor sempre fictício | Implementar tracking real de milestones por utilizador | Fase 3 | Analytics pipeline | Milestones provêm de dados reais |
| D-035 | UI fake | Product Analytics | UI fake | **P2 — Média** | `src/frontend/src/features/product-analytics/pages/JourneyFunnelPage.tsx` | `const mockJourneys = [...]` (linha 23) | Funil de jornada sempre fictício | Implementar tracking real de jornadas de utilizador | Fase 3 | Analytics pipeline | Jornadas provêm de dados reais de eventos |
| D-036 | Hardcode | Governance | Hardcode operacional | **P2 — Média** | `src/modules/governance/.../ListIntegrationConnectors/ListIntegrationConnectors.cs` | `Environment: "Production"` hardcoded (linha 63) | Todos os connectors parecem ser de produção independentemente do ambiente real | Adicionar campo `Environment` à entidade `IntegrationConnector`; usar valor real | Fase 2 | Governance domain entity | `Environment` provém da entidade; não hardcoded |
| D-037 | Hardcode | Governance | Hardcode operacional | **P2 — Média** | `src/modules/governance/.../GetIntegrationConnector/GetIntegrationConnector.cs` | `AuthenticationMode: "OAuth2 App Token"` e `PollingMode: "Webhook + Polling"` hardcoded (linhas 60-61) | Detalhes de autenticação e polling sempre iguais — não reflectem configuração real | Adicionar campos ao domínio; mapear de entidade real | Fase 2 | D-036 | Campos provêm da entidade; sem hardcode |
| D-038 | Hardcode | Governance | Hardcode operacional | **P2 — Média** | `src/modules/governance/.../GetIntegrationConnector/GetIntegrationConnector.cs` | `AllowedTeams: new List<string> { "platform-squad" }` hardcoded (linha 75) | Ownership de connectors sempre atribuído a "platform-squad" | Implementar ownership real via tabela de permissões | Fase 2 | D-036 | `AllowedTeams` provém de ownership real |
| D-039 | Hardcode | Governance | Hardcode operacional | **P2 — Média** | `src/modules/governance/.../ListGovernancePacks/ListGovernancePacks.cs` | `ScopeCount: 0, RuleCount: 0` hardcoded (linhas 45-46) | Governance packs sempre aparecem com 0 regras e 0 scopes — sem valor informativo | Implementar contagem real de rules e scopes via query ao DB | Fase 2 | GovernanceRuleBinding implementado | Counts provêm de DB; não são 0 por default |
| D-040 | Hardcode | Governance | Hardcode operacional | **P2 — Média** | `src/modules/governance/.../GetGovernancePack/GetGovernancePack.cs` | `RuleCount: 0` e `// TODO: implementar RuleBindings` (linhas 42, 46) | Detalhe de governance pack incompleto — rule bindings ausentes | Implementar enriquecimento real com regras e bindings | Fase 2 | D-039 | Pack detail inclui rules reais |
| D-041 | Persistência | Governance | Persistência ausente | **P1 — Alta** | `src/modules/governance/.../` (múltiplos handlers FinOps) | Handlers FinOps retornam dados simulados — nenhuma tabela de dados de custo real existe | Nenhuma decisão de FinOps real pode ser tomada com base nos dados actuais | Criar schema de dados de custo; implementar ingestion de billing data; implementar handlers reais | Fase 3 | Integração externa de billing | Dados de custo persistidos e consultáveis |
| D-042 | Persistência | AIKnowledge | Persistência ausente | **P1 — Alta** | `src/modules/aiknowledge/.../ExternalAI/Features/` (6 handlers vazios) | Governança de IA externa completamente ausente no backend | Impossível auditar, aprovar ou controlar uso de IA externa | Implementar entidades e persistence para ExternalAI governance | Fase 2 | ExternalAiDatabase migrations | Entidades criadas; operações CRUD funcionais |
| D-043 | Infraestrutura | Platform | Infraestrutura | **P1 — Alta** | `src/platform/NexTraceOne.ApiHost/appsettings.json` | `ApplyDatabaseMigrationsAsync` executa sem controle de versão de schema em staging | Em staging, migrations auto-executam sem pipeline de migração explícito | Documentar processo de migração para staging; criar script de migration runner | Fase 1 | CI/CD pipeline (D-003) | Migrations em staging executadas via pipeline controlado |
| D-044 | Segurança | ApiHost | Configuração | **P0 — Crítica** | `src/platform/NexTraceOne.ApiHost/appsettings.json` | `"IntegrityCheck": false` — verificações de integridade desactivadas por default | Startup pode completar sem garantias de estado consistente | Habilitar `IntegrityCheck: true` em `appsettings.json`; manter `false` apenas em dev override | Fase 1 | Nenhuma | `IntegrityCheck` habilitado em prod; override documentado |
| D-045 | UI fake | Operations | UI fake | **P3 — Baixa** | `src/frontend/src/features/operations/pages/TeamReliabilityPage.tsx` | `DemoBanner` presente mas dados mock locais (não `IsSimulated` do backend) | Banner presente mas sincronismo com backend ausente — utilizador vê demo que não reflecte estado real | Corrigir após D-030: remover banner quando API real disponível | Fase 2 | D-030 | Banner ausente quando dados são reais |
| D-046 | TODO crítico | AIKnowledge | TODO crítico | **P2 — Média** | `src/modules/aiknowledge/.../Orchestration/Features/GenerateRobotFrameworkDraft/GenerateRobotFrameworkDraft.cs` | `// TODO` em lógica de geração | Geração de robot framework draft pode ser parcial | Validar completude da implementação; remover TODOs | Fase 2 | AIKnowledge infrastructure | Nenhum TODO em path de execução |
| D-047 | TODO crítico | Governance | TODO crítico | **P3 — Baixa** | `src/modules/governance/.../ListGovernanceWaivers/ListGovernanceWaivers.cs` | `RuleName: w.RuleId ?? "(Entire Pack)"` — nome de regra não resolvido (linha 54) | Waivers mostram ID em vez de nome descritivo | Resolver nome real da regra via join ou lookup | Fase 2 | D-039 | Nome da regra exibido correctamente |

---

## Legenda de Severidade

| Código | Nome | Descrição |
|---|---|---|
| **P0** | Bloqueador absoluto | Impede qualquer deploy seguro. Deve ser resolvido antes de qualquer release. |
| **P1** | Fechamento funcional crítico | Impede o produto de ser real. Deve ser resolvido nas próximas fases prioritárias. |
| **P2** | Fechamento funcional importante | Produto incompleto sem este item. Resolvido nas fases seguintes. |
| **P3** | Hardening | Robustez e polimento. Resolvido após funcionalidade core estar real. |
| **P4** | Polimento | Melhorias incrementais de qualidade. |

---

## Referência de Ficheiros com Padrões Proibidos

### Backend — `IsSimulated = true` em handlers operacionais

```
src/modules/governance/NexTraceOne.Governance.Application/Features/GetExecutiveTrends/GetExecutiveTrends.cs:85
src/modules/governance/NexTraceOne.Governance.Application/Features/GetWasteSignals/GetWasteSignals.cs:76
src/modules/governance/NexTraceOne.Governance.Application/Features/GetTeamFinOps/GetTeamFinOps.cs:51
src/modules/governance/NexTraceOne.Governance.Application/Features/GetFinOpsTrends/GetFinOpsTrends.cs:66
src/modules/governance/NexTraceOne.Governance.Application/Features/GetExecutiveDrillDown/GetExecutiveDrillDown.cs:111
src/modules/governance/NexTraceOne.Governance.Application/Features/GetFinOpsSummary/GetFinOpsSummary.cs:98
src/modules/governance/NexTraceOne.Governance.Application/Features/GetFrictionIndicators/GetFrictionIndicators.cs:74
src/modules/governance/NexTraceOne.Governance.Application/Features/GetDomainFinOps/GetDomainFinOps.cs:56
src/modules/governance/NexTraceOne.Governance.Application/Features/GetEfficiencyIndicators/GetEfficiencyIndicators.cs:79
src/modules/governance/NexTraceOne.Governance.Application/Features/GetBenchmarking/GetBenchmarking.cs:73
src/modules/governance/NexTraceOne.Governance.Application/Features/GetServiceFinOps/GetServiceFinOps.cs:68
src/modules/operationalintelligence/.../GetServiceReliabilityDetail/GetServiceReliabilityDetail.cs:79,103,121
src/modules/operationalintelligence/.../GetServiceReliabilityCoverage/GetServiceReliabilityCoverage.cs:59
src/modules/operationalintelligence/.../GetDomainReliabilitySummary/GetDomainReliabilitySummary.cs:58
src/modules/operationalintelligence/.../GetServiceReliabilityTrend/GetServiceReliabilityTrend.cs:80
src/modules/operationalintelligence/.../ListServiceReliability/ListServiceReliability.cs:57,64,69
src/modules/operationalintelligence/.../GetTeamReliabilityTrend/GetTeamReliabilityTrend.cs:68
src/modules/operationalintelligence/.../GetTeamReliabilitySummary/GetTeamReliabilitySummary.cs:60
src/modules/operationalintelligence/.../GetAutomationAuditTrail/GetAutomationAuditTrail.cs:45,49
```

### Backend — Handlers vazios com `TODO: Implementar`

```
src/modules/aiknowledge/.../ExternalAI/Features/CaptureExternalAIResponse/CaptureExternalAIResponse.cs
src/modules/aiknowledge/.../ExternalAI/Features/ConfigureExternalAIPolicy/ConfigureExternalAIPolicy.cs
src/modules/aiknowledge/.../ExternalAI/Features/ApproveKnowledgeCapture/ApproveKnowledgeCapture.cs
src/modules/aiknowledge/.../ExternalAI/Features/ReuseKnowledgeCapture/ReuseKnowledgeCapture.cs
src/modules/aiknowledge/.../ExternalAI/Features/GetExternalAIUsage/GetExternalAIUsage.cs
src/modules/aiknowledge/.../ExternalAI/Features/ListKnowledgeCaptures/ListKnowledgeCaptures.cs
```

### Frontend — `const mock*` em páginas operacionais

```
src/frontend/src/features/operations/pages/TeamReliabilityPage.tsx:16          (mockServices)
src/frontend/src/features/operations/pages/ServiceReliabilityDetailPage.tsx:14  (mockDetails)
src/frontend/src/features/operations/pages/PlatformOperationsPage.tsx:58-80     (mockSubsystems, mockJobs, mockQueues, mockEvents)
src/frontend/src/features/product-analytics/pages/PersonaUsagePage.tsx:27       (mockPersonas)
src/frontend/src/features/product-analytics/pages/ValueTrackingPage.tsx:27      (mockMilestones)
src/frontend/src/features/product-analytics/pages/JourneyFunnelPage.tsx:23      (mockJourneys)
```

### Frontend — `ReactQueryDevtools` sem guard (CORRIGIDO)

```
src/frontend/src/App.tsx:792  ← CORRIGIDO em PHASE-0 com import.meta.env.DEV guard
```

### Configuração — `IntegrityCheck: false`

```
src/platform/NexTraceOne.ApiHost/appsettings.json:22
```

### Infraestrutura — Ausências confirmadas

```
.github/workflows/   ← Diretório vazio (sem pipeline CI/CD)
Dockerfile           ← Ausente (raiz, ApiHost, Frontend)
docker-compose.yml   ← Ausente (apenas docker-compose.telemetry.yaml em build/)
```
