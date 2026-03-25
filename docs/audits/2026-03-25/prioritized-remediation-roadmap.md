# Roadmap de Correcção Priorizado — NexTraceOne

**Data:** 25 de março de 2026
**Veredito Global:** STRATEGIC_BUT_INCOMPLETE

---

## Princípio de Priorização

As acções estão organizadas por urgência e impacto. Problemas de segurança têm prioridade absoluta sobre funcionalidade. Capacidades estratégicas incompletas são priorizadas acima de melhorias de UX.

---

## Bloco 0 — URGENTE (Antes de Qualquer Deploy ou Demo)

**Estes problemas invalidam segurança e devem ser resolvidos imediatamente.**

| # | Acção | Área | Ficheiro | Impacto |
|---|-------|------|---------|---------|
| 0.1 | Remover senha "ouro18" de todos os connection strings | Segurança | `appsettings.json`, `appsettings.Development.json` | CRÍTICO — credenciais expostas |
| 0.2 | Remover `"Secret": ""` do config JWT | Segurança | `appsettings.json:34` | CRÍTICO — JWT inseguro |
| 0.3 | Remover fallback JWT key hardcoded | Segurança | `JwtTokenService.cs:48` | CRÍTICO — forja de tokens possível |
| 0.4 | Tornar JWT_SECRET obrigatório no startup | Segurança | `BuildingBlocks.Security/DependencyInjection.cs` | CRÍTICO |
| 0.5 | Tornar NEXTRACE_ENCRYPTION_KEY obrigatório fora de Development | Segurança | `AesGcmEncryptor.cs` | HIGH |
| 0.6 | Remover mock response generator do AssistantPanel | Frontend/IA | `features/ai-hub/components/AssistantPanel.tsx` | HIGH — AI mock em produção |

---

## Bloco 1 — Correcções Críticas de Segurança e Arquitectura

**Sprint 1-2 (semanas 1-4)**

### 1.1 Segurança

| # | Acção | Ficheiro |
|---|-------|---------|
| 1.1 | Mover origens CORS localhost para appsettings.Development.json | `appsettings.json:81` |
| 1.2 | Remover AES fallback key hardcoded (já feito parcialmente em 0.5) | `AesGcmEncryptor.cs:113` |
| 1.3 | Corrigir uso de NEXTRACE_SKIP_INTEGRITY no pipeline CI | `.github/workflows/security.yml` |
| 1.4 | Adicionar validação de startup para RequireSecureCookies | `appsettings.Development.json:32` |
| 1.5 | Adicionar requisito de complexidade de password (>= 8 chars + complexidade) | `IdentityAccess.Domain/HashedPassword.cs` |

### 1.2 Configuração

| # | Acção | Ficheiro |
|---|-------|---------|
| 1.6 | Externalizar rate limits para appsettings.json (não hardcoded em Program.cs) | `Program.cs:97-209` |
| 1.7 | Documentar explicitamente no README que appsettings não deve conter segredos | `README.md` |

### 1.3 Frontend

| # | Acção | Ficheiro |
|---|-------|---------|
| 1.8 | Integrar AssistantPanel com endpoint real de chat | `features/ai-hub/components/AssistantPanel.tsx` |

---

## Bloco 2 — Correcções Estruturais e Bounded Contexts

**Sprint 3-5 (semanas 5-12)**

### 2.1 Bounded Contexts e Schema

| # | Acção | Ficheiro | Razão |
|---|-------|---------|-------|
| 2.1 | Criar módulo `integrations` com DbContext próprio | `src/modules/integrations/` | GovernanceDbContext com entidades do módulo errado |
| 2.2 | Migrar IntegrationConnector, IngestionSource, IngestionExecution para módulo Integrations | `GovernanceDbContext` | Separação de bounded contexts |
| 2.3 | Criar módulo `productanalytics` ou incluir AnalyticsEvent no Integrations | `GovernanceDbContext.AnalyticsEvent` | Bounded context correcto |
| 2.4 | Completar ExternalAiDbContext com DbSets e configurações EF Core | `AIKnowledge.Infrastructure/ExternalAI/` | DbContext inútil sem entidades |
| 2.5 | Adicionar repositórios para ExternalAI domain | `AIKnowledge.Infrastructure/ExternalAI/` | Pipeline ExternalAI bloqueado |
| 2.6 | Expandir ReliabilityDbContext com SLO, SLA, error budget, burn rate | `OperationalIntelligence.Infrastructure/Reliability/` | 1 entidade insuficiente |

### 2.2 Schema e Contratos

| # | Acção | Ficheiro | Razão |
|---|-------|---------|-------|
| 2.7 | Adicionar entidades específicas para SOAP/WSDL contracts | `Catalog.Domain/Contracts/` | Protocolo declarado mas sem modelo |
| 2.8 | Adicionar entidades específicas para Event Contracts / AsyncAPI | `Catalog.Domain/Contracts/` | Protocolo declarado mas sem modelo |
| 2.9 | Expandir NotificationsDbContext com template, canal, SMTP config | `Notifications.Infrastructure/` | 3 entidades insuficientes |
| 2.10 | Expandir ConfigurationDbContext com hierarquia e feature flags | `Configuration.Infrastructure/` | 3 entidades insuficientes |

### 2.3 Persistência de Observabilidade

| # | Acção | Ficheiro | Razão |
|---|-------|---------|-------|
| 2.11 | Adicionar tabela `trace_release_mapping` no ClickHouse | `build/clickhouse/init-schema.sql` | Correlação telemetria-release |
| 2.12 | Implementar pipeline automático: deploy event → release correlation | `ChangeGovernance.Application/` | Correlação manual não escala |

---

## Bloco 3 — Fechamento de Lacunas Estratégicas

**Sprint 6-10 (semanas 13-24)**

### 3.1 Source of Truth e Knowledge

| # | Acção | Razão |
|---|-------|-------|
| 3.1 | Criar módulo `knowledge` no backend com: KnowledgeDocument, OperationalNote, KnowledgeRelation | Knowledge Hub ausente — pilar 7 do produto |
| 3.2 | Implementar Search cross-módulo (PostgreSQL FTS inicialmente) | CommandPalette sem backend search |
| 3.3 | Ligar Knowledge Hub a serviços, contratos, mudanças e incidentes | Knowledge Hub isolado não serve a visão |

### 3.2 Contract Governance

| # | Acção | Razão |
|---|-------|-------|
| 3.4 | Implementar handlers específicos para SOAP/WSDL import e gestão | Contrato SOAP precisa de fluxo dedicado |
| 3.5 | Implementar handlers específicos para Event Contracts / AsyncAPI | Kafka, bindings, schemas Avro |
| 3.6 | Verificar e completar fluxo de Publication Center | Portal pode não estar end-to-end |

### 3.3 Change Intelligence

| # | Acção | Razão |
|---|-------|-------|
| 3.7 | Implementar correlação automática deploy event → ChangeIntelligenceScore | Score manual não serve produção |
| 3.8 | Completar fluxo de Evidence Pack end-to-end com CI/CD integration | EvidencePack entidade não tem pipeline |
| 3.9 | Implementar post-change verification automatizada com baseline | Regra mandatória do produto |

### 3.4 AI Governance

| # | Acção | Razão |
|---|-------|-------|
| 3.10 | Completar 7 features TODO do ExternalAI domain | ExternalAI é subdomain estratégico |
| 3.11 | Implementar streaming nas AI providers (Ollama e OpenAI) | Stream=false impede UX adequada |
| 3.12 | Wiring de tool execution no AiAgentRuntimeService | Agentes com tools declaradas mas não executam |
| 3.13 | Completar context grounding real nos AssistantPanel toggles | Contexto de serviço/contrato/mudança injectado mock |
| 3.14 | Iniciar AI Orchestration: GenerateTestScenarios, SummarizeReleaseForApproval | 8 features estratégicas a iniciar |

### 3.5 FinOps

| # | Acção | Razão |
|---|-------|-------|
| 3.15 | Implementar pipeline de importação de dados de custo | CostIntelligenceDbContext sem dados reais |
| 3.16 | Ligar CostRecord a Service, Team, Environment, Change | FinOps sem contexto não serve o produto |

### 3.6 Licensing

| # | Acção | Razão |
|---|-------|-------|
| 3.17 | Criar módulo Licensing: License, Entitlement, ActivationToken, LicenseCheck | Módulo completamente ausente |
| 3.18 | Implementar feature gating backend ligado a entitlements | ReleaseScopeGate.tsx não tem backend |
| 3.19 | Implementar heartbeat/activation (online e offline) | Self-hosted enterprise requer licensing |

---

## Bloco 4 — Limpeza e Consolidação

**Sprint 11-12 (semanas 25-28)**

### 4.1 Seeds e Documentação

| # | Acção | Prioridade |
|---|-------|-----------|
| 4.1 | Arquivar `docs/architecture/legacy-seeds/` → `docs/archive/legacy-seeds/` | P1 |
| 4.2 | Consolidar documentação de estado de IA num único ficheiro actualizado | P2 |
| 4.3 | Arquivar auditorias AI de julho 2025 que contradizem estado actual | P2 |
| 4.4 | Reorganizar `docs/11-review-modular/00-governance/` de 110+ para ~15 ficheiros | P3 |
| 4.5 | Arquivar docs de fases antigas (phase-0 a phase-3) | P3 |
| 4.6 | Criar índice central de documentação | P3 |

### 4.2 Código

| # | Acção | Ficheiro |
|---|-------|---------|
| 4.7 | Remover `ListKnowledgeSourceWeights` hardcoded | `AIKnowledge.Application/Governance/` |
| 4.8 | Remover `ListSuggestedPrompts` hardcoded → mover para ConfigurationEntry | `AIKnowledge.Application/Governance/` |

---

## Bloco 5 — Experiência e Produto

**Sprint 13-16 (semanas 29-36)**

### 5.1 Charts e Visualizações

| # | Acção | Impacto |
|---|-------|---------|
| 5.1 | Integrar Apache ECharts para dashboards executive e FinOps | Dashboards actuais sem gráficos |
| 5.2 | Adicionar gráficos de tendência temporal para change intelligence | Risk scoring visual |
| 5.3 | Adicionar gráficos de reliability e SLO | Operational dashboards |

### 5.2 i18n

| # | Acção | Impacto |
|---|-------|---------|
| 5.4 | Completar tradução pt-BR (3.096 → 4.814 chaves) | 36% das chaves em falta |
| 5.5 | Verificar e completar tradução es (3.812 → 4.814 chaves) | 21% das chaves em falta |

### 5.3 Personas e UX

| # | Acção | Impacto |
|---|-------|---------|
| 5.6 | Implementar persona-specific AI UX (system prompt por persona) | AI igual para todos |
| 5.7 | Adicionar Release Calendar visual (FreezeWindow + UI) | Gestão de janelas de mudança |
| 5.8 | Completar Rollback Intelligence como fluxo guiado | RollbackAssessment sem UI completa |

### 5.4 Self-Hosted

| # | Acção | Impacto |
|---|-------|---------|
| 5.9 | Adicionar container Ollama ao docker-compose.yml | IA local sem instalação manual |
| 5.10 | Adicionar Grafana + Loki ao docker-compose.yml | Observabilidade completa out-of-the-box |
| 5.11 | Criar guia de instalação IIS completo | Self-hosted Windows enterprise |
| 5.12 | Criar guia de hardening para ambientes self-hosted | Security baseline para produção |

### 5.5 Routing e Stack

| # | Acção | Prioridade |
|---|-------|-----------|
| 5.13 | Avaliar migração React Router DOM → TanStack Router | P3 |
| 5.14 | Avaliar adopção Radix UI para acessibilidade | P3 |
| 5.15 | Criar extensão IDE real (VS Code prioritário) | P2 |

---

## Resumo por Bloco

| Bloco | Acções | Semanas | Foco |
|-------|--------|---------|------|
| Bloco 0 | 6 | Imediato | CRÍTICO — segurança e AI mock |
| Bloco 1 | 8 | 1-4 | Segurança e configuração |
| Bloco 2 | 12 | 5-12 | Arquitectura e bounded contexts |
| Bloco 3 | 19 | 13-24 | Capacidades estratégicas |
| Bloco 4 | 8 | 25-28 | Limpeza e consolidação |
| Bloco 5 | 15 | 29-36 | UX e produto |
| **Total** | **68** | **~36 semanas** | |

---

## Critérios de Sucesso por Fase

### Após Bloco 0
- Sem credenciais hardcoded em nenhum ficheiro de configuração
- AssistantPanel usa endpoint real de chat

### Após Bloco 1
- Segurança básica completa (JWT, AES, CORS, rate limits)
- CI pipeline com integrity check real

### Após Bloco 2
- Bounded contexts correctos (integrations módulo separado)
- ExternalAI DbContext funcional
- SLO tracking mínimo implementado

### Após Bloco 3
- 10 pilares do produto com capacidades funcionais
- Knowledge Hub básico implementado
- Licensing básico implementado
- AI streaming e tools funcionais
- Correlação telemetria-release automática

### Após Bloco 4
- Documentação sem contradições
- Repositório sem resíduos activos

### Após Bloco 5
- Dashboards com gráficos ricos
- i18n 4 idiomas completos
- Self-hosted documentation completa
- UX persona-aware completa
