# E16 — Gap Report da Estrutura ClickHouse

> **Status:** CONCLUÍDO — Gaps documentados  
> **Data:** 2026-03-25  
> **Fase:** E16 — Implementação da Camada Analítica ClickHouse  
> **Precedido por:** E15 — Geração de Baseline PostgreSQL por Módulo  
> **Sucedido por:** E17 — Validação Ponta a Ponta do Produto  

---

## 1. O Que Foi Resolvido no E16

| Item | Estado |
|------|--------|
| Schema SQL `nextraceone_analytics` com 12 objectos activos | ✅ Resolvido |
| Tabelas para Product Analytics (5 objectos) | ✅ Resolvido |
| Tabelas para Operational Intelligence (3 tabelas) | ✅ Resolvido |
| Tabelas para Integrations (2 tabelas) | ✅ Resolvido |
| Tabelas para Governance Analytics (2 tabelas) | ✅ Resolvido |
| Schema AI & Knowledge definido e comentado (PREPARE_ONLY) | ✅ Resolvido |
| Convenções de naming, particionamento, TTL e engines | ✅ Resolvido |
| `IAnalyticsWriter` com 9 métodos de escrita | ✅ Resolvido |
| `AnalyticsOptions` com configuração DI | ✅ Resolvido |
| 8 record types de eventos analíticos | ✅ Resolvido |
| `NullAnalyticsWriter` para graceful degradation | ✅ Resolvido |
| `ClickHouseAnalyticsWriter` via HTTP interface | ✅ Resolvido |
| `AddBuildingBlocksAnalytics` registado no DI | ✅ Resolvido |
| Docker Compose actualizado com 2 SQL files numerados | ✅ Resolvido |
| Separação clara PostgreSQL vs ClickHouse validada | ✅ Resolvido |
| Chaves de correlação definidas e documentadas | ✅ Resolvido |
| Build sem erros (290 + 410 testes passam) | ✅ Resolvido |

---

## 2. O Que Ficou Pendente e Depende do E17

### 2.1 Ingestão Real — Handlers de Domínio

**Descrição:** Os métodos `IAnalyticsWriter.WriteXxxAsync(...)` existem mas não são chamados por nenhum handler de domínio ainda.

| Ponto de Integração | Módulo | Tipo de Chamada |
|--------------------|--------|----------------|
| `AnalyticsEventProjectionHandler` | Product Analytics | `WriteProductEventAsync` após flush do buffer `pan_analytics_events` |
| `RuntimeSnapshotProjectionHandler` | Operational Intelligence | `WriteRuntimeMetricAsync` ao registar snapshot |
| `CostSnapshotProjectionHandler` | Operational Intelligence | `WriteCostEntryAsync` ao registar custo |
| `IncidentLifecycleProjectionHandler` | Operational Intelligence | `WriteIncidentTrendEventAsync` ao mudar estado |
| `IngestionExecutionCompletedHandler` | Integrations | `WriteIntegrationExecutionAsync` ao completar execução |
| `ConnectorHealthChangedHandler` | Integrations | `WriteConnectorHealthEventAsync` ao mudar health |
| `ComplianceSnapshotProjectionHandler` | Governance | `WriteComplianceTrendAsync` ao calcular score |
| `FinOpsPeriodProjectionHandler` | Governance | `WriteFinOpsAggregateAsync` ao agregar custos |

**Fase de resolução:** E17

### 2.2 Activação `Analytics:Enabled = true`

**Descrição:** Por defeito `Analytics:Enabled = false`. Nenhum ambiente tem ClickHouse Analytics activado.

| Acção | Ficheiro |
|-------|---------|
| Definir `Analytics:Enabled: true` | `appsettings.Development.json` ou override |
| Validar `Analytics:ConnectionString` | Apontar para `http://clickhouse:8123/?database=nextraceone_analytics` |

**Fase de resolução:** E17 (activar primeiro em ambiente de desenvolvimento local)

### 2.3 Testes de Integração ClickHouse

**Descrição:** Não existem testes que validem escrita real no ClickHouse.

| Teste | Descrição |
|-------|-----------|
| `ClickHouseAnalyticsWriterIntegrationTests` | Validar INSERT real nas tabelas `pan_events`, `ops_runtime_metrics`, etc. |
| `AnalyticsWriterNullBehaviorTests` | Validar que NullAnalyticsWriter nunca lança excepção |
| `AnalyticsDIRegistrationTests` | Validar registo correcto por configuração |

**Fase de resolução:** E17

### 2.4 Outbox Processor → ClickHouse Consumer

**Descrição:** A arquitectura final prevê fluxo via Outbox pattern para eventos de domínio. Este consumer não existe ainda.

```
Outbox (PostgreSQL) → Outbox Processor (BackgroundWorkers) → ClickHouse Consumer → ClickHouse
```

**Alternativa no MVP:** escrita directa via `IAnalyticsWriter` nos handlers (mais simples, menos resiliente).

**Fase de resolução:** E17 ou fase posterior conforme necessidade de resiliência

---

## 3. O Que Depende de Evolução Posterior (Além do E17)

### 3.1 AI & Knowledge Analytics

**Estado:** PREPARE_ONLY — schema definido em `analytics-schema.sql` (comentado)

**Tabelas preparadas:**
- `aik_token_usage` — token consumption por model/user/agent
- `aik_model_performance` — latência e success rate por modelo

**Condição de activação:** volume > 10K eventos/dia/tenant de token usage

**Esforço estimado:** 1–2 dias de trabalho após condição atingida

### 3.2 Change Governance Analytics

**Estado:** NOT_IN_SCOPE_FOR_E16

**Tabelas planeadas (futuro):**
- `chg_change_frequency` — frequência e velocidade de deploys por serviço

**Condição de activação:** volume de change events justificar analytics dedicado

### 3.3 Audit & Compliance Long-Term Archive

**Estado:** NOT_IN_SCOPE_FOR_E16

**Tabelas planeadas (futuro):**
- `aud_long_term_events` — arquivo de audit events após 1 ano

**Condição de activação:** volume de audit events ou necessidade de retenção > 1 ano

### 3.4 Service Catalog Health Trends

**Estado:** NOT_IN_SCOPE_FOR_E16

**Tabelas planeadas (futuro):**
- `cat_health_trends` — health score evolution por serviço ao longo do tempo

**Condição de activação:** integração com monitoring pipeline e volume de snapshots

---

## 4. O Que Ainda Exige Tuning ou Expansão

### 4.1 TTL Policies — Validação em Produção Real

| Tabela | TTL Actual | Observação |
|--------|-----------|------------|
| `pan_events` | 2 anos | Validar com stakeholders de produto se 2 anos é suficiente para cohort analysis |
| `ops_runtime_metrics` | 90 dias | Verificar se é suficiente para comparações históricas longas |
| `int_execution_logs` | 1 ano | Rever com base em requisitos de compliance de integrações |
| `gov_finops_aggregates` | 2 anos | Validar com requisitos FinOps reais |

### 4.2 Índices Adicionais

As tabelas actuais usam o `ORDER BY` como índice primário. Com volume real podem ser necessários:
- Skip indexes para queries frequentes por coluna não incluída no ORDER BY
- Bloom filter indexes para colunas de baixa cardinalidade muito filtradas

### 4.3 Batch Writing Performance

O `ClickHouseAnalyticsWriter` suporta `WriteProductEventsBatchAsync` para escritas em lote. Os outros métodos são single-row. Para alto volume em produção, considerar:
- Batch writing para todos os tipos de eventos (não só `pan_events`)
- Buffer local com flush periódico (implementação de batching layer)

### 4.4 Credenciais e Segurança ClickHouse

Actualmente ClickHouse usa `default` sem password em ambiente de desenvolvimento.  
Para produção:
- definir `CLICKHOUSE_PASSWORD` em `.env`
- configurar utilizador dedicado com permissões apenas no `nextraceone_analytics`
- usar TLS na conexão HTTP

### 4.5 Retenção do Buffer PostgreSQL (`pan_analytics_events`)

A tabela `pan_analytics_events` no PostgreSQL serve como buffer temporário (7 dias).  
O worker de purge deste buffer ainda não existe. Sem ele, a tabela cresce indefinidamente.

**Fase de resolução:** E17 ou fase posterior

---

## 5. Módulos ou Pontos Que Ainda Exigem Atenção

### 5.1 OI-01, OI-02, OI-03, OI-04 — Extracções Pendentes

As extracções dos sub-módulos de Operational Intelligence para DbContexts separados estão pendentes desde E15. Isto não bloqueia o E16 (ClickHouse não depende de DbContext), mas deve ser resolvido antes do E17.

| Extracção | Estado |
|-----------|--------|
| OI-01: Extrair IncidentDbContext | ❌ Pendente |
| OI-02: Extrair IntegrationsDbContext (do GovernanceDbContext) | ❌ Pendente |
| OI-03: Extrair ProductAnalyticsDbContext (do GovernanceDbContext) | ❌ Pendente |
| OI-04: Extrair CostIntelligenceDbContext | ❌ Pendente |

### 5.2 `pan_analytics_events` — Módulo Dono

A tabela `pan_analytics_events` ainda vive no `GovernanceDbContext`. Deve ser extraída para um `ProductAnalyticsDbContext` dedicado como parte da OI-03.

### 5.3 `int_connectors`, `int_ingestion_sources`, `int_ingestion_executions` — Módulo Dono

Estas tabelas ainda vivem no `GovernanceDbContext`. Devem ser extraídas para um `IntegrationsDbContext` dedicado como parte da OI-02.

### 5.4 Correlação Environment — `environment` vs `environment_id`

Em Operational Intelligence, o campo `Environment` é uma string (não UUID). No ClickHouse foi mantido como `LowCardinality(String)`. Para correlação correcta com `env_environments`, a Application Layer deve resolver o nome para o ID quando necessário.

---

## 6. Gaps Não Bloqueantes (Aceites para E17)

| Gap | Tipo | Impacto |
|-----|------|---------|
| Nenhum handler chama `IAnalyticsWriter` ainda | Funcional | Zero dados fluem para ClickHouse — esperado para E16 |
| `Analytics:Enabled` é false por defeito | Config | ClickHouseAnalyticsWriter não activo — esperado para E16 |
| Sem testes de integração ClickHouse | Qualidade | Escrita não testada contra servidor real — esperado para E16 |
| Buffer purge worker não existe | Operacional | `pan_analytics_events` acumula sem purge — médio prazo |

---

## 7. Estado Final da Camada ClickHouse Após E16

| Dimensão | Estado |
|----------|--------|
| **Schema SQL ClickHouse** | ✅ Pronto — 12 objectos activos em `nextraceone_analytics` |
| **Convenções e naming** | ✅ Estabelecidas e documentadas |
| **Separação PG vs CH** | ✅ Validada e documentada |
| **Chaves de correlação** | ✅ Definidas por tabela |
| **C# abstractions** | ✅ Interface + Records + Writers |
| **DI registration** | ✅ `AddBuildingBlocksAnalytics` disponível |
| **Docker Compose** | ✅ Inicializa ambas as DBs automaticamente |
| **Ingestão real activa** | ❌ Pendente E17 |
| **Testes de integração** | ❌ Pendente E17 |
| **Handlers invocando IAnalyticsWriter** | ❌ Pendente E17 |

**Conclusão:** O E16 entregou a estrutura física, os contratos C# e a configuração de infraestrutura. O E17 pode começar imediatamente com a activação da ingestão e validação ponta a ponta.
