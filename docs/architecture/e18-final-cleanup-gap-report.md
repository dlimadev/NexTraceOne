# E18 — Gap Report de Limpeza Final

> **Status:** CONCLUÍDO  
> **Data:** 2026-03-25  
> **Fase:** E18 — Limpeza Final e Fechamento Técnico da Trilha E  

---

## 1. O Que Foi Resolvido no E18

| Item | Resolução |
|------|-----------|
| `RulesetScorePlaceholder` — classe vazia órfã | ✅ Arquivo removido |
| `InMemoryIncidentStore` — falsa impressão de blocker de produção | ✅ Marcado DEPRECATED; EfIncidentStore confirmado como produtivo |
| `GetAutomationAuditTrail` — dados simulados sem indicação clara | ✅ Anotado como LIMITATION com instrução de substituição |
| `ConfigureRetention` — handler vazio sem indicação clara | ✅ Anotado como LIMITATION com referência ao gap real |
| `MfaPolicy.cs` — referência a "vendor ops" com conotação de licensing | ✅ Texto reescrito para "operações de integração externa" |
| Startup blocker `IMemoryCache` (E17) | ✅ Resolvido no E17 |
| `appsettings.json` com passwords reais (E17) | ✅ Resolvido no E17 |
| `ConfigurationDatabase` ausente no dev config (E17) | ✅ Resolvido no E17 |
| 5 testes stale de infrastructure (E17) | ✅ Resolvidos no E17 |
| 17+ licensing permissions em RolePermissionCatalog (E13) | ✅ Resolvidos no E13 |
| Referências de licensing em navegação e i18n (E13) | ✅ Resolvidos no E13 |

---

## 2. O Que Ainda Ficou Pendente

### 2.1 Stubs Aceitáveis Temporariamente

| Item | Razão |
|------|-------|
| `GenerateStubResponse` em `SendAssistantMessage.cs` — resposta LLM simulada | Depende de LLM provider real (OpenAI, Azure, Ollama). Funcionalidade de AI core. |
| `DatabaseRetrievalService` PoC — pesquisa apenas AIModels por keyword | RAG real requer vector store e corpus. Alta complexidade. |
| `AiSourceRegistryService` health check stub | Depende de conectores reais por tipo de fonte. |
| `GenerateSimulatedEntries` em GetAutomationAuditTrail | Integração com AutomationDbContext não concluída. |
| `ConfigureRetention` handler sem persistência | Integração com RetentionPolicy no AuditDbContext não concluída. |

### 2.2 Módulos PARTIAL — Extracções Pendentes

| Módulo | Pendência | Impacto |
|--------|-----------|---------|
| Integrations | OI-02: extracção do GovernanceDbContext | Módulo sem autonomia, sem webhook, sem retry |
| Product Analytics | OI-03: extracção do GovernanceDbContext | Pipeline ClickHouse não activo |
| Environment Management | OI-04: extracção do IdentityDbContext | env_ tables misturadas com iam_ |

### 2.3 ClickHouse — Pipeline Não Activo

| Item | Estado |
|------|--------|
| Schema `nextraceone_analytics` criado e válido | ✅ |
| `IAnalyticsWriter` / `ClickHouseAnalyticsWriter` implementados | ✅ |
| `Analytics:Enabled=false` por defeito | ✅ Intencional |
| Handlers invocando `IAnalyticsWriter` | ❌ Nenhum handler implementado ainda |
| Fluxo Product Analytics → ClickHouse | ❌ BROKEN |

---

## 3. Blockers Reais

| # | Blocker | Severidade | Fase |
|---|---------|-----------|------|
| **B1** | LLM real — `GenerateStubResponse` em produção daria resposta falsa | ALTA | Próxima onda |
| **B2** | Product Analytics → ClickHouse não activo | MÉDIA | Próxima onda |
| **B3** | OI-02/03/04 extracções — módulos sem autonomia | MÉDIA | Próxima onda |
| **B4** | E2E tests — falham por ausência de PostgreSQL no CI | ALTA | Infra |
| **B5** | `ConfigureRetention` handler não persiste | BAIXA | Próxima onda |

---

## 4. O Que Pode Seguir para a Próxima Onda de Engenharia

| Item | Prioridade |
|------|-----------|
| Activar `IAnalyticsWriter` nos handlers de OI e Integrations | P1 |
| Implementar LLM provider real (pelo menos Ollama local) | P1 |
| Configurar PostgreSQL no CI para E2E tests | P1 |
| OI-02: extrair IntegrationsDbContext do GovernanceDbContext | P2 |
| OI-03: extrair ProductAnalyticsDbContext do GovernanceDbContext | P2 |
| OI-04: extrair EnvironmentManagement do IdentityDbContext | P2 |
| EnvironmentId em AuditEvent | P2 |
| Retention enforcement worker (Audit) | P2 |
| SMTP real para Notifications | P2 |
| MFA enforcement background worker | P3 |
| API Key entity (Identity) | P3 |
| RAG real para AI Knowledge | P3 |
| Tool calling em AI agents | P3 |
| IDE extensions reais | P3 |

---

## 5. O Que É Puramente Evolutivo

| Item | Razão |
|------|-------|
| Streaming de respostas AI (STUB-B05) | UX improvement, não blocker funcional |
| `CatalogGraphModuleService` full implementation | Requer queries de grafo complexas |
| Blast radius com Catalog queries reais | Alta complexidade, requer ciclos de teste |
| Webhook receiver para Integrations | Requer parceiros de integração |
| Teams/Slack real para Notifications | Requer tokens e workspaces de teste |
| SAML/OIDC provider real | Requer IdP de teste |
| ClickHouse tuning avançado | Depende de volume real de dados |
| FinOps métricas derivadas (trends, waste) | Requer histórico real de dados de custo |
| Compliance export | Feature de relatório, não blocker core |

---

## 6. Risco Residual por Área

| Área | Risco | Mitigação |
|------|-------|-----------|
| **AI** | Resposta stub em produção daria conteúdo falso | `Analytics:Enabled=false`; LLM real deve ser activado antes de exposição a utilizadores |
| **ClickHouse** | Dados analíticos vazios sem pipeline activo | Schema pronto; activação é toggle de configuração |
| **OI Automation** | Audit trail de automação é simulado | Documentado como LIMITATION; endpoint existe mas dados são hardcoded |
| **Audit Retention** | Retention config não persiste | Handler existe mas é no-op; RetentionPolicy entity existe no DB |
| **E2E tests** | 51 testes dependem de PostgreSQL no CI | Testcontainers já configurados; falta setup no CI environment |
| **Security (flaky)** | 2 testes de security são flaky | Pré-existente; não regressão; environment detection inconsistente |

---

## 7. Artefactos Limpos na Trilha E (Resumo)

| Artefacto | E-Fase | Tipo |
|-----------|--------|------|
| `RulesetScorePlaceholder` removido | E18 | Classe órfã |
| `InMemoryIncidentStore` marcado DEPRECATED | E18 | Mock test-only |
| `GetAutomationAuditTrail` LIMITATION anotado | E18 | Stub comentário |
| `ConfigureRetention` LIMITATION anotado | E18 | Handler no-op |
| `MfaPolicy.cs` comentários de "vendor/licensing" reescritos | E18 | Licensing residue |
| `IMemoryCache` missing from Program.cs (startup blocker) | E17 | Bug |
| Passwords em `appsettings.json` | E17 | Segurança |
| `ConfigurationDatabase` ausente em dev config | E17 | Config |
| 5 testes stale infrastructure | E17 | Testes obsoletos |
| 17 licensing permissions em `RolePermissionCatalog` | E13 | Licensing residue |
| Licensing refs em navegação e i18n | E13 | Licensing residue |
| `licenseId` em locales (4 idiomas) | E13 | Licensing residue |
| 29 legacy migrations removidas | E14 | Migration debt |
| 4 databases → 1 nextraceone | E14 | Architectural debt |
| 7 legacy SQL seed files arquivados | E14 | Legacy residue |
| 154 tabelas com prefixos correctos | E15 | Schema |
| ClickHouse schema initial (12 objects) | E16 | Analytics |
