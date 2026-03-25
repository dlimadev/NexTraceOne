# Relatório de Estado de Integrações — NexTraceOne

**Data:** 25 de março de 2026

---

## 1. Objectivo

Auditar o estado das integrações com sistemas externos: CI/CD, gestão de identidade, fontes de telemetria, documentação e providers de IA.

---

## 2. Modelo de Integração no Backend

### 2.1 Entidades Temporárias no GovernanceDbContext

**Nota:** As entidades de integração actualmente residem no `GovernanceDbContext` de forma temporária:
- `IntegrationConnector` — definição de conector
- `IngestionSource` — fonte de ingestão
- `IngestionExecution` — execução de ingestão
- `AnalyticsEvent` — eventos analíticos

Estas entidades estão previstas para extracção para um módulo dedicado de Integrações/Analytics.

### 2.2 Frontend de Integrações

**Path:** `src/frontend/src/features/integrations/`

**Páginas:**
- Integration Hub — catálogo de conectores
- Connector Detail — configuração de conector
- Ingestion Executions — histórico de execuções
- Ingestion Freshness — frescura dos dados ingeridos

**Estado:** READY — UI existe com integração real

---

## 3. Integrações CI/CD

### 3.1 GitLab

**Estado:** PARTIAL

**ExternalMarker entidade** em `ChangeIntelligenceDbContext` sugere suporte a markers externos (deploys, tags de CI/CD).

**Evidência:** Sem adapter específico de GitLab verificado no código.

**Lacuna:** Sem webhook handler ou adapter explícito para GitLab.

### 3.2 GitHub

**Estado:** PARTIAL — GitHub Actions workflows para CI/CD do próprio produto, sem adapter de integração com projetos externos.

### 3.3 Jenkins

**Estado:** MISSING — não encontrado

### 3.4 Azure DevOps

**Estado:** MISSING — não encontrado

### 3.5 Padrão de Integração Esperado

O `IngestionSource` como entidade sugere que o modelo de integração é via pull/webhook de fontes externas para eventos de deploy/change, não integração directa com os sistemas CI/CD.

---

## 4. Integrações de Identidade

### 4.1 OIDC

**Ficheiro:** `src/platform/NexTraceOne.ApiHost/appsettings.json`

```json
"Oidc": {
  "Authority": "",
  "ClientId": "",
  "ClientSecret": ""
}
```

**Entidades:** `ExternalIdentity` e `SsoGroupMapping` no `IdentityDbContext`

**Estado:** PARTIAL — schema existe; provider OIDC configurável mas vazio por defeito

### 4.2 SAML

**Estado:** INCOMPLETE — entidades de mapeamento SSO existem (`SsoGroupMapping`) mas SAML específico não verificado

---

## 5. Integrações de Observabilidade

### 5.1 OpenTelemetry

**Ficheiro:** `build/otel-collector/otel-collector.yaml`

**Estado:** READY — pipeline completo com OTLP receivers e ClickHouse exporters

### 5.2 Serilog

**Estado:** READY — Serilog configurado com sinks: Console, File, PostgreSQL, Grafana Loki

### 5.3 Grafana Loki

**Estado:** PARTIAL — Serilog sink configurado; sem Loki no docker-compose principal

### 5.4 Prometheus

**Estado:** PARTIAL — exporter configurado no OTel Collector; sem Prometheus no docker-compose principal

---

## 6. Integrações de IA

### 6.1 Ollama (Local)

**Ficheiro:** `src/modules/aiknowledge/NexTraceOne.AIKnowledge.Infrastructure/Runtime/OllamaProvider.cs`

**Estado:** READY — cliente HTTP real com health check, listagem de modelos e chat completion

**Configuração:** `OLLAMA_ENDPOINT` via env var

**Problema:** Sem container Ollama no `docker-compose.yml` — requer instalação manual

### 6.2 OpenAI

**Ficheiro:** `src/modules/aiknowledge/NexTraceOne.AIKnowledge.Infrastructure/Runtime/OpenAiProvider.cs`

**Estado:** READY — cliente HTTP real

**Configuração:** `OPENAI_API_KEY` via env var

### 6.3 Azure OpenAI / Gemini

**Estado:** `AiProvider` entidade tem registo para Azure e Gemini como providers mas sem implementação de provider verificada

---

## 7. Integrações de Conhecimento

### 7.1 Documentação Interna

**Estado:** PARTIAL — `AIKnowledgeSource` como entidade; sem connectors verificados

### 7.2 Git Repos como Knowledge Source

**Estado:** MISSING — não encontrado

---

## 8. Padrão de Integração — Avaliação

| Requisito | Estado | Evidência |
|-----------|--------|-----------|
| Contratos claros | PARCIAL | IngestionSource como entidade |
| Sem acoplamento hardcoded de vendor | CUMPRIDO | Providers via DI; sem SDK vendor |
| Auditabilidade | CUMPRIDO | IngestionExecution com histórico |
| Config por tenant/ambiente | PARCIAL | Entidades existem; não verificado |
| Modelo canónico interno | CUMPRIDO | ChangeEvent como modelo canónico |

---

## 9. Resumo por Integração

| Integração | Estado | Lacunas |
|-----------|--------|---------|
| GitLab CI/CD | PARTIAL | Sem adapter específico |
| GitHub CI/CD | PARTIAL | Apenas para produto, não para projetos |
| Jenkins | MISSING | Não encontrado |
| Azure DevOps | MISSING | Não encontrado |
| OIDC/SSO | PARTIAL | Configurável mas não pré-configurado |
| SAML | INCOMPLETE | Schema existe; implementação não verificada |
| OpenTelemetry | READY | Pipeline completo |
| Serilog | READY | Configurado |
| Grafana Loki | PARTIAL | Sink configurado; sem container |
| Prometheus | PARTIAL | Exporter configurado; sem container |
| Ollama | READY | Provider real; sem Docker container |
| OpenAI | READY | Provider real |
| Azure OpenAI | INCOMPLETE | Entidade existe; provider não verificado |

---

## 10. Recomendações

| Prioridade | Acção |
|-----------|-------|
| P1 | Criar módulo de Integrações dedicado (extrair de GovernanceDbContext) |
| P1 | Adicionar container Ollama ao docker-compose.yml |
| P2 | Implementar adapter GitLab para eventos de deploy |
| P2 | Implementar adapter GitHub Actions para eventos de deploy |
| P2 | Completar configuração OIDC com exemplo prático |
| P2 | Adicionar Grafana + Loki ao docker-compose.yml |
| P3 | Implementar adapter Jenkins |
| P3 | Implementar adapter Azure DevOps |
| P3 | Implementar Azure OpenAI e Gemini providers |
