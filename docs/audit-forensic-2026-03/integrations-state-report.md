# Relatório de Integrações — NexTraceOne
**Auditoria Forense | Março 2026**

---

## 1. Estado Geral das Integrações

**Status: STUB/INCOMPLETE**

O módulo de integrações existe estruturalmente mas não há integração real com nenhum sistema externo confirmada end-to-end.

---

## 2. Integrations Module

| Componente | Estado | Evidência |
|---|---|---|
| IntegrationsDbContext | Existe (sem migração confirmada) | `src/modules/integrations/NexTraceOne.Integrations.Infrastructure/Persistence/IntegrationsDbContext.cs` |
| ConnectorDetailPage (frontend) | Conectado ao backend | `src/frontend/src/features/integrations/` |
| IngestionExecutionsPage (frontend) | Conectado ao backend | Frontend conectado |
| Conectores CI/CD | Stubs | Sem implementação real |

---

## 3. Ingestion API — Estado

**Status: PARTIAL — METADATA ONLY**

`NexTraceOne.Ingestion.Api` tem 5 endpoints de ingestão:
- Dados chegam e são registados com `processingStatus: "metadata_recorded"`
- Payload **não processado** — sem transformação, correlação ou persistência semântica
- Integração com GitLab/Jenkins/GitHub Actions/Azure DevOps: não funcional

**Evidência:** `docs/IMPLEMENTATION-STATUS.md` §Ingestion

---

## 4. Identity Providers

**Status: JWT local implementado; OIDC/SAML não auditado em detalhe**

- JWT local: READY
- OIDC externo: documentado em strategy docs; implementação não confirmada no código
- SAML: documentado; implementação não confirmada

---

## 5. AI Providers

| Provider | Estado | Evidência |
|---|---|---|
| Ollama (local) | Configurado, enabled | `appsettings.json "AiRuntime.Ollama"` |
| OpenAI | Configurado, disabled por padrão | `appsettings.json "AiRuntime.OpenAI"` |
| Azure OpenAI | Não detectado | — |
| Anthropic | Não detectado | — |

**Gap:** `IExternalAIRoutingPort` existe como abstração. Handlers ExternalAI são 8 TODOs. O routing de provider não está funcionalmente conectado ao assistant.

---

## 6. Source Control / CI/CD

| Sistema | Estado | Evidência |
|---|---|---|
| GitLab | Stub | Conector previsto; sem implementação real |
| Jenkins | Stub | Conector previsto; sem implementação real |
| GitHub Actions | Stub | Conector previsto; sem implementação real |
| Azure DevOps | Stub | Conector previsto; sem implementação real |

**Impacto:** Change Intelligence depende parcialmente de eventos de deploy vindos de sistemas CI/CD. Sem integração real, deploys precisam ser registados manualmente via API.

---

## 7. Knowledge/Docs Sources

**Status: Não auditado em detalhe**

Integração com fontes externas de conhecimento (Confluence, Notion, etc.) — não detectada implementação.

---

## 8. Telemetria / Observabilidade

| Componente | Estado |
|---|---|
| OpenTelemetry OTLP | Configurado (endpoint localhost — requer config em prod) |
| OTEL Collector | Configurado em `build/otel-collector/` |
| ClickHouse (destino analítico) | Schema definido; pipeline não validado end-to-end |

---

## 9. Recomendações

| Ação | Prioridade | Impacto |
|---|---|---|
| Implementar pelo menos 1 conector CI/CD real (ex: GitHub Actions) | Alta | Change events automáticos |
| Processar payload real na Ingestion API | Alta | Pipeline de ingestão funcional |
| Gerar migração para IntegrationsDbContext | Média | Schema deployável |
| Validar OIDC/SAML se enterprise SSO for requisito | Alta | Adoção enterprise |
| Documentar modelo canônico de evento de deploy | Média | Contrato estável para integrações |
