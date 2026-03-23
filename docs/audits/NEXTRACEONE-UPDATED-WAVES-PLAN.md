# PLANO OFICIAL POR ONDAS — NexTraceOne

> **Data:** 2026-03-23
> **Referência:** Onda 0 — Realinhamento de Baseline
> **Base:** Reclassificação de gaps (NEXTRACEONE-UPDATED-GAP-CLASSIFICATION.md)
> **Contexto:** Plano atualizado sem Grafana como premissa; alinhado com a arquitetura real do produto

---

## VISÃO GERAL

| Onda | Objetivo | Gaps | Esforço Estimado | Tipo |
|------|---------|------|-----------------|------|
| **Onda 0** | Realinhamento de baseline | — | ✅ Concluída | Auditoria |
| **Onda 1** | Desbloqueio de produção | GAP-001, GAP-002 | 1-2 dias | Infra/Ops |
| **Onda 2** | Eliminar demo/stub do core | GAP-003 a GAP-008, GAP-010 | 2-3 semanas | Funcional |
| **Onda 3** | Segurança e integração operacional | GAP-015, GAP-022 | 1 semana | Segurança/Ops |
| **Onda 4** | Hardening e maturidade operacional | GAP-009, GAP-011, GAP-012-R, GAP-013, GAP-014, GAP-016, GAP-020, GAP-021 | 1-2 semanas | Hardening |
| **Onda 5** | Qualidade e polish (pós-go-live) | GAP-017, GAP-018, GAP-019, GAP-023, GAP-024 | 2-3 semanas | Quality/Polish |

**Esforço total estimado: 6-9 semanas** (ondas 1-4 críticas: 4-6 semanas)

---

## ONDA 0 — REALINHAMENTO DE BASELINE ✅ CONCLUÍDA

### Objetivo
Corrigir a linha de base do programa de conclusão do NexTraceOne, eliminando premissas desatualizadas (Grafana) e criando um backlog oficial e confiável.

### Entregas
- ✅ Baseline arquitetural realinhado
- ✅ Grafana removido como premissa do backlog
- ✅ GAP-012 substituído por GAP-012-R
- ✅ Todos os 24 gaps revisados e reclassificados
- ✅ Plano oficial por ondas atualizado
- ✅ Documentação oficial da Onda 0

### Critério de aceite
- ✅ Arquitetura operacional real confirmada (ClickHouse + OTel Collector)
- ✅ Grafana não é mais dependência de nenhuma onda
- ✅ Backlog limpo de premissas erradas
- ✅ Próximas ondas podem começar sem ambiguidade

---

## ONDA 1 — DESBLOQUEIO DE PRODUÇÃO

### Objetivo
Resolver os únicos dois gaps que impedem a aplicação de ir para produção.

### Gaps incluídos

| Gap | Título | Esforço | Responsável sugerido |
|-----|--------|---------|---------------------|
| GAP-001 | Configurar secrets no GitHub Environment `production` | Pequeno | DevOps |
| GAP-002 | Configurar cron/scheduling para backup automatizado | Pequeno | DevOps |

### Detalhes de execução

**GAP-001 — Secrets de produção:**
- Configurar no GitHub Environment `production`:
  - `JWT_SECRET` (≥ 32 caracteres)
  - Connection strings para os 4 bancos de dados
  - Quaisquer outros secrets exigidos por `StartupValidation.cs`
- Verificar que a aplicação inicia sem erros de validação

**GAP-002 — Backup automatizado:**
- Configurar cron job ou scheduler para executar `scripts/db/backup.sh`
- Cobrir os 4 bancos de dados
- Configurar retenção de 30 dias
- Verificar que `scripts/db/restore.sh` funciona com os backups gerados

### Dependências
- Nenhuma dependência técnica; apenas acesso administrativo ao GitHub e infraestrutura.

### Critério de aceite
- [ ] Aplicação inicia em ambiente `production` sem erros de `StartupValidation`
- [ ] Backup automatizado executa diariamente para os 4 bancos
- [ ] Restore verificado pelo menos uma vez
- [ ] Retenção de 30 dias configurada

### O que fica fora
- Implementação funcional de features
- Correção de handlers demo
- Qualquer mudança de código

---

## ONDA 2 — ELIMINAR DEMO/STUB DO CORE

### Objetivo
Substituir todos os handlers que retornam dados demo ou stub por implementações reais, eliminando a dívida funcional que impede o produto de ser considerado enterprise-ready.

### Gaps incluídos

| Gap | Título | Esforço | Módulo |
|-----|--------|---------|--------|
| GAP-003 | GetEfficiencyIndicators — implementar query real | Médio | Governance |
| GAP-004 | GetWasteSignals — implementar detecção real | Médio | Governance |
| GAP-005 | GetFrictionIndicators — implementar detecção real | Médio | Governance |
| GAP-006 | RunComplianceChecks — implementar motor real | Grande | Governance |
| GAP-007 | GenerateDraftFromAi — integrar IA real | Grande | Catalog |
| GAP-008 | DocumentRetrievalService — implementar RAG | Grande | AIKnowledge |
| GAP-010 | EncryptionInterceptor — implementar interceptor EF Core | Grande | Security |

### Subondas sugeridas

**Onda 2a — Governance real (GAP-003, 004, 005, 006):**
- Implementar queries reais para os 3 indicadores de FinOps
- Implementar motor de compliance com regras configuráveis
- Prioridade: Alta (afeta credibilidade do módulo de Governance inteiro)

**Onda 2b — IA real (GAP-007, 008):**
- Integrar provider real de IA para geração de contratos
- Implementar RAG com embeddings para document retrieval
- Dependência: Definir qual provider de IA será utilizado (OpenAI/Azure/LLM local)

**Onda 2c — Segurança de dados (GAP-010):**
- Implementar EF Core interceptor com AES-256-GCM
- `AesGcmEncryptor` já existe como serviço standalone — reutilizar
- Prioridade: Alta (campo-level encryption é requisito enterprise)

### Dependências
- **GAP-007 e GAP-008** dependem da decisão sobre qual provider de IA usar
- **GAP-003-005** dependem de haver dados reais de custo/operação no sistema
- **GAP-010** depende de definir quais campos são sensíveis (PII, secrets)

### Critério de aceite
- [ ] Nenhum handler do Governance retorna `IsSimulated: true` ou `DataSource: "demo"`
- [ ] `RunComplianceChecks` executa verificações reais (não hardcoded)
- [ ] `GenerateDraftFromAi` gera contratos via provider de IA real
- [ ] `DocumentRetrievalService` retorna resultados reais de busca semântica
- [ ] `EncryptionInterceptor` encripta campos sensíveis em repouso
- [ ] Testes unitários e de integração cobrem os novos comportamentos

### O que fica fora
- Telemetry retrieval (Onda 4)
- UI polish (Onda 4)
- Load testing (Onda 5)

---

## ONDA 3 — SEGURANÇA E INTEGRAÇÃO OPERACIONAL

### Objetivo
Fechar os gaps de segurança e integração operacional que impedem credibilidade enterprise mas não bloqueiam produção.

### Gaps incluídos

| Gap | Título | Esforço | Módulo |
|-----|--------|---------|--------|
| GAP-015 | Expandir rate limiting para endpoints de dados | Médio | Security |
| GAP-022 | Integrar AlertGateway com criação de incidentes | Médio | Operations |

### Detalhes de execução

**GAP-015 — Rate limiting expandido:**
- Adicionar políticas de rate limiting para:
  - Endpoints de busca/listagem (catalog, contracts, services)
  - Endpoints de relatórios e exportação
  - Endpoints de IA (token-aware)
- Manter políticas existentes (`"auth"`, `"auth-sensitive"`)

**GAP-022 — Alerting → Incidents:**
- Wiring do `AlertGateway` existente com `IncidentDbContext`
- Alertas que excedam threshold devem criar incidentes automaticamente
- Suportar escalação (alert → incident → notification)

### Dependências
- GAP-022 depende de incident persistence estar funcional (já está: `EfIncidentStore`)

### Critério de aceite
- [ ] Rate limiting aplicado a pelo menos 5 categorias de endpoints além de auth
- [ ] Alertas de severidade alta geram incidentes automaticamente
- [ ] Escalação funcional (alert → incident)
- [ ] Testes de integração para ambos os fluxos

### O que fica fora
- Load testing da configuração de rate limiting (Onda 5)
- Customização avançada de regras de alerting

---

## ONDA 4 — HARDENING E MATURIDADE OPERACIONAL

### Objetivo
Resolver gaps de maturidade operacional, UX e consistência que elevam o produto ao nível enterprise.

### Gaps incluídos

| Gap | Título | Esforço | Módulo |
|-----|--------|---------|--------|
| GAP-009 | TelemetryRetrievalService — integrar com ClickHouse | Médio | AIKnowledge |
| GAP-011 | Corrigir flag IsSimulated em GetExecutiveDrillDown | Pequeno | Governance |
| GAP-012-R | Validar e documentar superfície operacional sem Grafana | Pequeno-Médio | Observability |
| GAP-013 | Remover preview badge de EvidencePackages | Pequeno | Governance |
| GAP-014 | Remover preview badge de GovernancePackDetail | Pequeno | Governance |
| GAP-016 | Integrar GetPlatformHealth com health checks reais | Médio | Governance |
| GAP-020 | Remover mock fallback do AssistantPanel | Pequeno | AIKnowledge |
| GAP-021 | Configurar CORS por ambiente | Pequeno | Security |

### Detalhes de execução

**GAP-009 — Telemetry Retrieval:**
- Implementar `TelemetryRetrievalService` com queries reais ao ClickHouse
- Usar `IObservabilityProvider` (já implementado: `ClickHouseObservabilityProvider`)
- Retornar resultados de traces/logs para grounding de IA

**GAP-011 — Flag IsSimulated:**
- Corrigir `GetExecutiveDrillDown` linha 115: mudar `IsSimulated: true` para `false`
- Verificar que os dados retornados são de fato reais

**GAP-012-R — Superfície operacional:**
- Validar que as 6 páginas operacionais cobrem cenários críticos de troubleshooting
- Documentar queries ClickHouse comuns para deep dive
- Atualizar runbooks sem referência a Grafana
- Documentar a superfície oficial no Knowledge Hub

**GAP-013/014 — Preview badges:**
- Remover badges `<Badge variant="warning">` após validar que as features subjacentes estão completas

**GAP-016 — Platform Health real:**
- Substituir retornos hardcoded `Healthy` por health checks reais
- Integrar com health check endpoints de cada subsistema

**GAP-020 — AssistantPanel:**
- Remover 175+ linhas de mock response do frontend
- Garantir que o painel funciona com provider real ou mostra estado vazio apropriado

**GAP-021 — CORS:**
- Documentar configuração CORS por ambiente
- Configurar origins permitidas para staging e produção

### Dependências
- GAP-009 depende da Onda 2 (IA real) para ter contexto de uso
- GAP-020 depende da Onda 2 (IA real) para ter provider funcional
- GAP-012-R depende do estado da documentação de observabilidade atualizada (Onda 0 ✅)

### Critério de aceite
- [ ] `TelemetryRetrievalService` retorna dados reais do ClickHouse
- [ ] `IsSimulated` flag corrigido no `GetExecutiveDrillDown`
- [ ] Superfície operacional documentada e validada sem Grafana
- [ ] Preview badges removidos
- [ ] Health checks reais no `GetPlatformHealth`
- [ ] AssistantPanel sem mock fallback
- [ ] CORS documentado e configurado por ambiente

### O que fica fora
- Criação de dashboards custom (não faz parte do produto — NexTraceOne é a UI)
- Integração com ferramentas externas de visualização

---

## ONDA 5 — QUALIDADE E POLISH (PÓS-GO-LIVE)

### Objetivo
Completar melhorias de qualidade, testing e polish que não bloqueiam produção nem credibilidade enterprise, mas elevam a maturidade geral do produto.

### Gaps incluídos

| Gap | Título | Esforço | Módulo |
|-----|--------|---------|--------|
| GAP-017 | Implementar load testing formal | Médio | Testing |
| GAP-018 | Implementar Playwright E2E para frontend | Médio | Testing |
| GAP-019 | Implementar teste E2E de refresh token | Pequeno | Testing |
| GAP-023 | Avaliar e implementar ProductStore se justificado | Grande | Observability |
| GAP-024 | Corrigir ESLint warnings no frontend | Médio | Quality |

### Detalhes de execução

**GAP-017 — Load testing:**
- Implementar testes de carga com k6, Artillery ou JMeter
- Cobrir endpoints críticos: auth, catalog search, contract creation, incident listing
- Definir baselines de performance

**GAP-018 — Playwright E2E:**
- Implementar smoke E2E para os 4 fluxos core:
  1. Source of Truth → buscar serviço → ver contratos
  2. Change Confidence → criar change → aprovar
  3. Incidents → listar → detalhe → correlação
  4. AI Assistant → perguntar → resposta contextualizada

**GAP-019 — Refresh token E2E:**
- Teste E2E que verifica o fluxo completo de refresh token
- Simular expiração e renovação automática

**GAP-023 — ProductStore:**
- Reavaliar necessidade com base no uso real de IA e correlações
- Se justificado, implementar schema `telemetry` em PostgreSQL com:
  - Agregados de métricas
  - Topologia observada
  - Contextos de investigação
- Se não justificado, documentar a decisão de não implementar

**GAP-024 — ESLint:**
- Corrigir os 108 erros de linting progressivamente
- Priorizar: unused vars (63), setState-in-effect (20), JSX parsing (1)
- Objetivo: zero warnings no CI

### Dependências
- GAP-017 requer ambiente de staging funcional
- GAP-018 requer frontend deployado
- GAP-023 requer avaliação de uso real (dados das Ondas 2-4)

### Critério de aceite
- [ ] Load tests executam e geram relatório de baseline
- [ ] Playwright E2E cobre 4 fluxos core com sucesso
- [ ] Refresh token E2E passa
- [ ] ProductStore implementado ou descartado com justificativa
- [ ] ESLint CI passa sem warnings

### O que fica fora
- Features novas
- Redesign de UI
- Novos módulos

---

## DEPENDÊNCIAS ENTRE ONDAS

```
Onda 0 ──────► Onda 1 ──────► Onda 2 ──────► Onda 3 ──────► Onda 4 ──────► Onda 5
(Baseline)     (Produção)     (Demo/Stub)    (Segurança)    (Hardening)    (Polish)
                                │                              ▲
                                └──────────────────────────────┘
                                (GAP-009 e GAP-020 dependem de IA real da Onda 2)
```

### Ondas paralelas possíveis
- **Onda 1 e Onda 2a** podem correr em paralelo (infra vs código)
- **Onda 3** pode começar após Onda 1 (rate limiting não depende de demo removal)
- **Onda 4** depende parcialmente da Onda 2 (GAP-009, GAP-020)
- **Onda 5** pode começar após go-live

---

## NOTAS IMPORTANTES

### Grafana não é dependência de nenhuma onda

A decisão arquitetural de remover Grafana foi confirmada na Onda 0. Nenhuma onda futura deve:
- Criar dashboards Grafana
- Configurar provisioning Grafana
- Instalar serviços Grafana, Tempo, Loki ou Prometheus
- Referenciar Grafana como componente esperado

### Observabilidade é interna ao produto

A superfície de observabilidade e troubleshooting é composta por:
1. Páginas operacionais do NexTraceOne (6 superfícies)
2. ClickHouse para dados brutos (queries SQL)
3. OTel Collector para pipeline de ingestão
4. `verify-pipeline.sh` para verificação do pipeline

### AI provider precisa ser decidido antes da Onda 2

Os gaps GAP-007 e GAP-008 dependem da escolha do provider de IA:
- OpenAI (API externa, requer governança de tokens)
- Azure OpenAI (enterprise, controlado)
- LLM local (sem dependência externa, menor qualidade)
- Modelo híbrido (IA interna + externa governada)

Esta decisão deve ser tomada antes do início da Onda 2.

---

> **Este documento é o plano oficial por ondas do NexTraceOne após a Onda 0 de realinhamento de baseline.**
> **Ver também:** `NEXTRACEONE-WAVE-0-BASELINE-REALIGNMENT.md` e `NEXTRACEONE-UPDATED-GAP-CLASSIFICATION.md`
