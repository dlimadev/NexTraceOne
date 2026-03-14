# Seeds de Contratos — Módulo 5 (Contracts & Interoperability)

## Objetivo

Scripts SQL de seed para popular dados fictícios e realistas do módulo de Contratos,
destinados exclusivamente a ambientes de **desenvolvimento e debug**.

**⚠️ NÃO executar em produção.**

## Scripts

| Script | Descrição | Registos |
|--------|-----------|----------|
| `00-reset.sql` | Limpa todos os dados de seed (diffs, violações, artefatos, versões) | cleanup |
| `01-rest-contracts.sql` | Contratos REST — OpenAPI 3.1 e Swagger 2.0 | 4 versões |
| `02-soap-wsdl-contracts.sql` | Contratos SOAP — WSDL 1.1 (PaymentService) | 3 versões |
| `03-asyncapi-kafka-contracts.sql` | Contratos AsyncAPI/Kafka (User Events, Order Events) | 4 versões |
| `04-diffs-and-scenarios.sql` | Diffs semânticos pré-computados para cenários breaking/non-breaking | 6 diffs |

## Ordem de Execução

```bash
# 1. Limpar dados existentes
psql -f database/seeds/contracts/00-reset.sql

# 2. Inserir contratos REST (OpenAPI + Swagger)
psql -f database/seeds/contracts/01-rest-contracts.sql

# 3. Inserir contratos SOAP/WSDL
psql -f database/seeds/contracts/02-soap-wsdl-contracts.sql

# 4. Inserir contratos AsyncAPI/Kafka
psql -f database/seeds/contracts/03-asyncapi-kafka-contracts.sql

# 5. Inserir diffs e cenários breaking/non-breaking
psql -f database/seeds/contracts/04-diffs-and-scenarios.sql
```

Ou, executar tudo de uma vez:

```bash
for f in database/seeds/contracts/*.sql; do psql -f "$f"; done
```

## Cenários de Teste Viabilizados

### REST (OpenAPI / Swagger)
- **Users API (OpenAPI 3.1)** — 3 versões: 1.0.0 (Draft) → 1.1.0 (Approved, aditiva) → 2.0.0 (InReview, breaking)
- **Orders API (Swagger 2.0)** — 1 versão: 1.0.0 (Locked)
- Diff aditivo: novos paths adicionados entre v1.0.0 e v1.1.0
- Diff breaking: parâmetro obrigatório + path removido entre v1.1.0 e v2.0.0

### SOAP / WSDL
- **PaymentService (WSDL 1.1)** — 3 versões: 1.0.0 (Draft) → 1.1.0 (Approved, aditiva) → 2.0.0 (InReview, breaking)
- Diff aditivo: nova operação CheckPaymentStatus adicionada
- Diff breaking: operação RefundPayment removida

### AsyncAPI / Kafka
- **User Events (AsyncAPI 2.6)** — 3 versões: 1.0.0 (Draft) → 1.1.0 (Approved, aditiva) → 2.0.0 (InReview, breaking)
- **Order Events (AsyncAPI 2.6)** — 1 versão: 1.0.0 (Locked)
- Diff aditivo: novo canal user/deleted adicionado
- Diff breaking: campos obrigatórios adicionados ao canal user/signedup

### Lifecycle States Cobertos
- `Draft` — rascunho inicial
- `InReview` — aguardando revisão
- `Approved` — aprovado
- `Locked` — bloqueado para produção

### Protocolos Cobertos
- OpenAPI 3.1 (REST)
- Swagger 2.0 (REST legado)
- WSDL 1.1 (SOAP)
- AsyncAPI 2.6 (Kafka / mensageria)

## Convenções de UUIDs

| Prefixo | Uso |
|---------|-----|
| `c5000001-*` | API Assets (referência externa ao Engineering Graph) |
| `c5010001-*` | Versões de contratos REST |
| `c5020001-*` | Versões de contratos WSDL |
| `c5030001-*` | Versões de contratos AsyncAPI |
| `c5040001-*` | Diffs semânticos |

## Integração com Outros Módulos

Os dados de seed referenciam `api_asset_id` que devem existir no módulo Engineering Graph.
Para testes isolados do módulo de contratos, os IDs fictícios funcionam sem dependência externa.

Para integração completa com o Engineering Graph, execute também:
```bash
psql -f database/seeds/engineering-graph/01-services.sql
psql -f database/seeds/engineering-graph/02-apis.sql
```
