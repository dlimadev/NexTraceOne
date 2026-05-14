# Configuração de Banco de Dados - Módulo AIKnowledge

## Visão Geral

O módulo AIKnowledge suporta **três configurações de banco de dados** para analytics e search. O usuário deve escolher **APENAS UMA** opção durante a instalação.

---

## Opções de Configuração

### **Opção A: PostgreSQL Only (Padrão)**
**Use quando:** Deployment pequeno/médio, sem necessidade de analytics avançado ou search full-text complexo.

**Configuração (`appsettings.json`):**
```json
{
  "ConnectionStrings": {
    "AiGovernance": "Host=localhost;Database=nextrace_aiknowledge;Username=postgres;Password=secret"
    // NÃO configure AiAnalytics ou AiSearch
  }
}
```

**Comportamento:**
- ✅ Todas as entidades no PostgreSQL (68 tabelas)
- ⚠️ Analytics retorna coleções vazias (NullAiAnalyticsRepository)
- ⚠️ Search retorna resultados vazios (NullAiSearchRepository)
- ✅ Funcionalidade básica completa (agentes, modelos, avaliações)
- ❌ Sem métricas de uso de tokens em tempo real
- ❌ Sem busca full-text avançada

**Vantagens:**
- Simplicidade operacional (apenas 1 database)
- Menor custo de infraestrutura
- Fácil backup/restore

**Desvantagens:**
- Queries analíticas lentas em grandes volumes
- Sem search com relevância/fuzzy matching

---

### **Opção B: PostgreSQL + ClickHouse (Recomendado para Analytics)**
**Use quando:** Necessita de métricas em tempo real, time-series analysis, dashboards de custo/performance.

**Configuração (`appsettings.json`):**
```json
{
  "ConnectionStrings": {
    "AiGovernance": "Host=localhost;Database=nextrace_aiknowledge;Username=postgres;Password=secret",
    "AiAnalytics": "Host=localhost;Port=9000;Database=ai_analytics;Username=default;Password="
    // NÃO configure AiSearch
  }
}
```

**Comportamento:**
- ✅ Entidades transacionais no PostgreSQL
- ✅ Métricas de uso de tokens no ClickHouse (alta performance)
- ✅ Execução de agentes com latência P95/P99
- ✅ Dashboards de custo em tempo real
- ⚠️ Search ainda usa NullAiSearchRepository (sem full-text)

**Tabelas ClickHouse Criadas:**
```sql
-- Token usage metrics
CREATE TABLE ai_token_usage (
    Id UUID,
    TenantId UUID,
    ModelId UUID,
    AgentId Nullable(UUID),
    PromptTokens UInt32,
    CompletionTokens UInt32,
    TotalTokens UInt32,
    CostUSD Decimal(18,4),
    Timestamp DateTime,
    OperationType String,
    UserId String
) ENGINE = MergeTree()
ORDER BY (TenantId, Timestamp);

-- Agent execution logs
CREATE TABLE ai_agent_executions (
    Id UUID,
    TenantId UUID,
    AgentId UUID,
    AgentName String,
    Status Enum8('Success' = 1, 'Failed' = 2),
    DurationMs Float64,
    CostUSD Decimal(18,4),
    Timestamp DateTime
) ENGINE = MergeTree()
ORDER BY (TenantId, Timestamp);

-- Model performance metrics
CREATE TABLE ai_model_performance (
    Id UUID,
    TenantId UUID,
    ModelId UUID,
    ModelName String,
    LatencyMs Float64,
    IsError UInt8,
    Timestamp DateTime
) ENGINE = MergeTree()
ORDER BY (TenantId, Timestamp);
```

**Vantagens:**
- 🚀 100x-1000x mais rápido que PostgreSQL para analytics
- 📊 Agregações em tempo real (SUM, AVG, quantiles)
- 💰 Custo total de tokens calculado instantaneamente
- 📈 Time-series nativo (perfect para métricas)

**Desvantagens:**
- Infraestrutura adicional (ClickHouse server)
- Curva de aprendizado SQL ClickHouse
- Sem search full-text avançado

---

### **Opção C: PostgreSQL + ElasticSearch (Recomendado para Search)**
**Use quando:** Necessita de busca full-text complexa, relevância, stemming, synonyms.

**Configuração (`appsettings.json`):**
```json
{
  "ConnectionStrings": {
    "AiGovernance": "Host=localhost;Database=nextrace_aiknowledge;Username=postgres;Password=secret",
    "AiSearch": "http://localhost:9200"
    // NÃO configure AiAnalytics
  }
}
```

**Comportamento:**
- ✅ Entidades transacionais no PostgreSQL
- ✅ Busca full-text em prompts, conversas, conhecimento
- ✅ Relevância com boosting (name: 2.0, description: 1.5, content: 1.0)
- ✅ Filtros por tenant, categorias, tags, datas
- ⚠️ Analytics continua no PostgreSQL (mais lento)

**Índices ElasticSearch Criados:**
```json
// prompt-templates
{
  "mappings": {
    "properties": {
      "name": { "type": "text", "analyzer": "standard", "boost": 2.0 },
      "description": { "type": "text", "analyzer": "standard", "boost": 1.5 },
      "content": { "type": "text", "analyzer": "standard" },
      "tags": { "type": "keyword" },
      "category": { "type": "keyword" },
      "tenantId": { "type": "keyword" }
    }
  }
}

// conversations
{
  "mappings": {
    "properties": {
      "userQuery": { "type": "text", "analyzer": "standard", "boost": 2.0 },
      "aiResponse": { "type": "text", "analyzer": "standard", "boost": 1.5 },
      "messages": { "type": "text", "analyzer": "standard" },
      "agentName": { "type": "keyword" },
      "tenantId": { "type": "keyword" },
      "createdAt": { "type": "date" }
    }
  }
}

// knowledge-documents
{
  "mappings": {
    "properties": {
      "title": { "type": "text", "analyzer": "standard", "boost": 2.0 },
      "content": { "type": "text", "analyzer": "standard" },
      "tags": { "type": "keyword" },
      "sourceType": { "type": "keyword" },
      "tenantId": { "type": "keyword" }
    }
  }
}
```

**Vantagens:**
- 🔍 Search com relevância sofisticada
- 🌐 Full-text com stemming, synonyms, fuzzy matching
- 🏷️ Filtros faceted (categorias, tags, datas)
- 📝 Paginação eficiente em milhões de documentos

**Desvantagens:**
- Infraestrutura adicional (ElasticSearch cluster)
- Maior consumo de memória RAM
- Analytics lento (usa PostgreSQL)

---

## Por Que Não Usar ClickHouse + ElasticSearch Juntos?

### ❌ **Problemas Arquiteturais**

1. **Duplicação de Dados**
   - Mesmo dado indexado em dois sistemas
   - Risco de inconsistência entre ClickHouse e Elastic
   - Complexidade de sincronização (CDC, Kafka Connect, etc.)

2. **Custo Operacional Duplicado**
   - Dois clusters para manter
   - Double monitoring, alerting, backup
   - Maior superfície de falha

3. **Manutenção Complexa**
   - Upgrades coordenados
   - Migrations em dois sistemas
   - Troubleshooting difícil (qual sistema está com problema?)

4. **Overkill para Maioria dos Casos**
   - ClickHouse já faz search básico (full-text indexes)
   - ElasticSearch já faz aggregations simples
   - 95% dos casos são atendidos por uma única tecnologia

### ✅ **Quando Considerar Ambos (Raro)**

Apenas se:
- Volume > 100M eventos/dia
- Necessita de search full-text E analytics complexos simultaneamente
- Equipe SRE dedicada para operar ambos
- Orçamento para infra significativa

**Neste caso,** use:
- ClickHouse como fonte primária (analytics)
- ElasticSearch como índice secundário (search apenas)
- Pipeline de sincronização via Kafka Connect ou Materialized Views

---

## Guia de Escolha

| Critério | PostgreSQL Only | PostgreSQL + ClickHouse | PostgreSQL + ElasticSearch |
|----------|----------------|------------------------|---------------------------|
| **Volume de Dados** | < 1M registros | 1M - 1B+ registros | 1M - 100M documentos |
| **Queries Analíticas** | Básicas (< 1s) | Complexas (< 100ms) | Básicas (< 1s) |
| **Search Full-Text** | LIKE básico | Básico | Avançado (relevância) |
| **Time-Series** | ❌ Lento | ✅ Nativo | ⚠️ Limitado |
| **Custo Infra** | $ (baixo) | $$ (médio) | $$$ (alto - RAM) |
| **Complexidade Ops** | Baixa | Média | Alta |
| **Backup/Restore** | Simples | Moderado | Complexo |
| **Casos de Uso** | MVP, startups | Analytics, FinOps | Knowledge base, suporte |

---

## Implementação Técnica

### Detecção Automática

O sistema detecta automaticamente qual configuração está ativa:

```csharp
// Em DependencyInjection.cs
var clickHouseConnectionString = configuration.GetConnectionString("AiAnalytics");
var elasticSearchConnectionString = configuration.GetConnectionString("AiSearch");

if (!string.IsNullOrEmpty(clickHouseConnectionString))
{
    // Opção B: ClickHouse
    services.AddSingleton<IAiAnalyticsRepository>(sp => 
        new ClickHouseAiAnalyticsRepository(clickHouseConnectionString));
    services.AddSingleton<IAiSearchRepository, NullAiSearchRepository>();
}
else if (!string.IsNullOrEmpty(elasticSearchConnectionString))
{
    // Opção C: ElasticSearch
    services.AddSingleton<IAiAnalyticsRepository, NullAiAnalyticsRepository>();
    services.AddSingleton<IAiSearchRepository>(sp => 
        new ElasticSearchAiRepository(elasticSearchConnectionString));
}
else
{
    // Opção A: PostgreSQL only
    services.AddSingleton<IAiAnalyticsRepository, NullAiAnalyticsRepository>();
    services.AddSingleton<IAiSearchRepository, NullAiSearchRepository>();
}
```

### Interfaces Abstratas

Todas as features usam interfaces, não implementações concretas:

```csharp
// Application layer
public interface IAiAnalyticsRepository
{
    Task<List<TokenUsageMetrics>> GetTokenUsageMetricsAsync(DateTime from, DateTime to, Guid? modelId = null);
    Task<decimal> GetTotalTokenCostAsync(DateTime from, DateTime to);
    // ...
}

public interface IAiSearchRepository
{
    Task<SearchResult<PromptTemplateDocument>> SearchPromptsAsync(string query, int page = 1, int pageSize = 20);
    // ...
}
```

Isso permite:
- ✅ Troca transparente de implementação
- ✅ Testes unitários com mocks
- ✅ Zero código condicional nas features

---

## Próximos Passos

1. **Implementar ClickHouse Real** (atualmente stub)
   - Instalar pacote ClickHouse.Client
   - Implementar queries SQL reais
   - Criar migrations para tabelas

2. **Implementar ElasticSearch Real** (atualmente stub)
   - Instalar pacote NEST
   - Criar índices com mappings
   - Implementar indexação automática

3. **Adicionar Health Checks**
   - Verificar conectividade ClickHouse/Elastic
   - Alertar se configuração inválida

4. **Documentar Endpoints**
   - Swagger/OpenAPI com exemplos
   - Guias de migração entre opções

---

## Referências

- [ClickHouse Documentation](https://clickhouse.com/docs)
- [ElasticSearch Documentation](https://www.elastic.co/guide)
- [PostgreSQL Performance Tuning](https://wiki.postgresql.org/wiki/Tuning_Your_PostgreSQL_Server)
