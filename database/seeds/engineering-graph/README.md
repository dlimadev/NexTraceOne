# Engineering Graph — Seeds de Teste

> **ATENÇÃO:** Estes scripts são destinados **APENAS** para ambientes de desenvolvimento e debug local. **Nunca** executar em ambientes de produção ou staging.

## Finalidade

Massa de dados fictícia para o módulo **Engineering Graph** (Bounded Context: Catalog). Permite validar funcionalidades do grafo de engenharia sem depender de importações reais de contratos ou telemetria OpenTelemetry.

## Ordem de Execução

Os scripts devem ser executados na ordem numérica para respeitar as dependências entre tabelas (foreign keys):

| #  | Ficheiro | Descrição | Registros |
|----|----------|-----------|-----------|
| 00 | `00-reset-engineering-graph-test-data.sql` | Limpa todos os dados de teste (ordem inversa de FK) | — |
| 01 | `01-seed-services.sql` | 8 serviços em 3 domínios (Payments, Identity, Orders) | 8 |
| 02 | `02-seed-apis.sql` | 12 APIs distribuídas pelos serviços | 12 |
| 03 | `03-seed-consumers.sql` | 6 consumidores com tipos diversos | 6 |
| 04 | `04-seed-consumer-relationships.sql` | 17 relações de consumo (Explicit/Inferred) | 17 |
| 05 | `05-seed-discovery-sources.sql` | 12 fontes de descoberta (Manual/OTel/Catalog) | 12 |
| 06 | `06-seed-snapshots.sql` | 3 snapshots temporais do grafo | 3 |
| 07 | `07-seed-node-health.sql` | 12 registros de saúde para overlay visual | 12 |
| 08 | `08-seed-saved-views.sql` | 2 vistas salvas com filtros persistidos | 2 |

## Dados Semeados

### Serviços (01)
- **Payments:** payment-gateway, payment-processor, payment-reconciliation
- **Identity:** auth-service, user-management
- **Orders:** order-orchestrator, catalog-service, notification-service

### APIs (02)
12 APIs com versões semânticas (v1.0.0 a v3.0.0), rotas RESTful e visibilidade Public/Internal.

### Consumidores (03)
6 consumidores com tipos diversos: Frontend (mobile-app, web-portal), Gateway (api-gateway), Job (batch-processor), Service (monitoring-agent), External (external-partner).

### Relações de Consumo (04)
17 relações mostrando dependências directas e inferidas:
- **Explicit** (declaradas manualmente ou por catálogo) — confiança 0.90–1.00
- **Inferred** (detectadas por telemetria) — confiança 0.60–0.85

### Fontes de Descoberta (05)
Proveniência dos dados no grafo:
- **Manual** — registo humano directo (confiança 1.00)
- **OpenTelemetry** — inferência por traces distribuídos (confiança 0.85–0.95)
- **CatalogImport** — importação automatizada (confiança 0.90)

### Snapshots (06)
3 capturas temporais para validar time-travel e diff:
- **Baseline Q1 2026** (30 dias atrás) — 8 nós, 20 arestas
- **Post-Release v2.1** (7 dias atrás) — 10 nós, 25 arestas
- **Current State** (agora) — 12 nós, 28 arestas

### Saúde de Nós (07)
Registros de saúde para overlay visual:
- **Healthy** (score > 0.90) — maioria dos serviços e APIs
- **Degraded** (score 0.65) — payment-reconciliation com erros intermitentes
- **Unknown** (score 0.00) — monitoring-agent sem dados de telemetria

### Vistas Salvas (08)
- **Payments Domain Overview** — visão partilhada do domínio Payments com overlay Health
- **Critical Dependencies** — dependências cross-domain privada com overlay Risk

## Cenários de Teste Habilitados

Com estes dados, é possível validar:

1. **Visualização do Grafo** — renderizar nós e arestas com layout hierárquico ou force-directed
2. **Overlay de Saúde** — colorir nós por status (verde/amarelo/vermelho/cinzento)
3. **Blast Radius** — calcular impacto de mudanças na Payments API (4 consumidores directos)
4. **Cross-Domain Dependencies** — identificar que Auth API é consumida por 4 consumidores de 3 domínios
5. **Time-Travel** — comparar snapshots para detectar alterações no grafo
6. **Filtragem por Domínio** — filtrar nós por domínio Payments/Identity/Orders
7. **Filtragem por Confiança** — filtrar relações por score mínimo de confiança
8. **Proveniência dos Dados** — rastrear como cada API foi descoberta/registada
9. **Vistas Salvas** — carregar configurações de visualização persistidas
10. **Detecção de Degradação** — identificar payment-reconciliation como ponto de atenção

## Como Executar

### Execução completa (reset + seed)

```bash
# Executar todos os scripts em ordem
for f in database/seeds/engineering-graph/*.sql; do
    echo "Executando: $f"
    psql -h localhost -U nextraceone -d nextraceone_dev -f "$f"
done
```

### Apenas reset (limpar dados de teste)

```bash
psql -h localhost -U nextraceone -d nextraceone_dev \
    -f database/seeds/engineering-graph/00-reset-engineering-graph-test-data.sql
```

### Script individual

```bash
psql -h localhost -U nextraceone -d nextraceone_dev \
    -f database/seeds/engineering-graph/01-seed-services.sql
```

## Esquema de IDs Determinísticos

Os IDs seguem um padrão previsível para facilitar referências cruzadas e debugging:

| Prefixo | Entidade | Exemplo |
|---------|----------|---------|
| `e1000000-...` | ServiceAsset | `e1000000-0000-0000-0000-000000000001` |
| `e2000000-...` | ApiAsset | `e2000000-0000-0000-0000-000000000001` |
| `e3000000-...` | ConsumerAsset | `e3000000-0000-0000-0000-000000000001` |
| `e4000000-...` | ConsumerRelationship | `e4000000-0000-0000-0000-000000000001` |
| `e5000000-...` | DiscoverySource | `e5000000-0000-0000-0000-000000000001` |
| `e6000000-...` | GraphSnapshot | `e6000000-0000-0000-0000-000000000001` |
| `e7000000-...` | NodeHealthRecord | `e7000000-0000-0000-0000-000000000001` |
| `e8000000-...` | SavedGraphView | `e8000000-0000-0000-0000-000000000001` |

## Tabelas EF Core Mapeadas

| Tabela | Entidade de Domínio |
|--------|-------------------|
| `eg_service_assets` | `ServiceAsset` |
| `eg_api_assets` | `ApiAsset` (Aggregate Root) |
| `eg_consumer_assets` | `ConsumerAsset` |
| `eg_consumer_relationships` | `ConsumerRelationship` |
| `eg_discovery_sources` | `DiscoverySource` |
| `graph_snapshots` | `GraphSnapshot` |
| `node_health_records` | `NodeHealthRecord` |
| `saved_graph_views` | `SavedGraphView` |
