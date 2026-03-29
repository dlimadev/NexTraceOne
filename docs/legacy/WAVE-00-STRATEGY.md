# Onda 0 — Estratégia e Baseline

> **Duração estimada:** 2 semanas
> **Dependências:** Nenhuma
> **Risco:** Baixo — apenas extensões aditivas
> **Referência:** [LEGACY-MAINFRAME-WAVES.md](../LEGACY-MAINFRAME-WAVES.md)

---

## Objetivo

Preparar o projeto para a evolução legacy sem quebrar funcionalidade existente. Esta onda cria a base técnica mínima que todas as ondas seguintes necessitam.

---

## Entregáveis

- [ ] Documentação de visão legacy integrada no `PRODUCT-VISION.md`
- [ ] Documentação de modos de maturidade (`docs/legacy/MATURITY-MODES.md`)
- [ ] Extensão de enums existentes para tipos legacy
- [ ] Extensão do DB check constraint para novos `ServiceType` values
- [ ] Feature flags para capabilities legacy (Configuration module)
- [ ] Chaves i18n base para termos legacy (~100 keys iniciais)
- [ ] Alinhamento do `ServiceType` enum C# com DB check constraint

---

## Impacto Backend

### Enums a Estender

**ServiceType** (adicionar ao enum C# — já existem no DB constraint):
```csharp
// Valores já no DB check constraint mas ausentes no enum C#:
GraphqlApi = 8,
GrpcService = 9,
LegacySystem = 10,
Gateway = 11,
ThirdParty = 12,

// Novos valores para mainframe:
CobolProgram = 13,
CicsTransaction = 14,
ImsTransaction = 15,
BatchJob = 16,
MainframeSystem = 17,
MqQueueManager = 18,
ZosConnectApi = 19
```

**ContractType** (adicionar):
```csharp
Copybook = 5,
MqMessage = 6,
FixedLayout = 7,
CicsCommarea = 8
```

**ContractProtocol** (adicionar):
```csharp
Copybook = 7,
MqMessageDescriptor = 8,
FixedRecordLayout = 9,
CicsCommarea = 10,
ImsSegment = 11
```

**NodeType** (adicionar):
```csharp
MainframeSystem = 7,
BatchJob = 8,
CicsTransaction = 9,
MqQueue = 10,
Copybook = 11
```

**EdgeType** (adicionar):
```csharp
Produces = 8,       // Service → MQ Queue
Consumes = 9,       // Service → MQ Queue
Triggers = 10,      // Batch Job → Batch Job
Schedules = 11,     // Scheduler → Batch Job
Uses = 12,          // COBOL Program → Copybook
BoundTo = 13        // z/OS Connect → CICS Transaction
```

**ChangeType** (adicionar):
```csharp
BatchJobChange = 7,
CopybookChange = 8,
CicsTransactionChange = 9,
MqConfigurationChange = 10,
MainframeInfraChange = 11,
ZosConnectBindingChange = 12
```

### Feature Flags

Registar no `ConfigurationModule`:

| Flag | Default | Descrição |
|------|---------|-----------|
| `legacy.enabled` | `false` | Gate principal para toda a capability legacy |
| `legacy.batch-intelligence.enabled` | `false` | Batch intelligence module |
| `legacy.messaging-intelligence.enabled` | `false` | MQ intelligence module |
| `legacy.copybook-parser.enabled` | `false` | Parser de copybooks COBOL |
| `legacy.telemetry-ingestion.enabled` | `false` | Ingestão de telemetria mainframe |

### Migração EF Core

Nova migração para:
1. Expandir check constraint `CK_cat_service_assets_service_type` com novos tipos
2. Sem novas tabelas nesta onda

---

## Impacto Frontend

### i18n — Chaves Base (~100)

Namespace `legacy` no `en.json`:
```json
{
  "legacy": {
    "title": "Legacy Systems",
    "subtitle": "Core system assets and governance",
    "mainframeSystem": "Mainframe System",
    "cobolProgram": "COBOL Program",
    "copybook": "Copybook",
    "cicsTransaction": "CICS Transaction",
    "imsTransaction": "IMS Transaction",
    "batchJob": "Batch Job",
    "db2Artifact": "DB2 Artifact",
    "mqQueueManager": "MQ Queue Manager",
    "mqQueue": "MQ Queue",
    "mqChannel": "MQ Channel",
    "zosConnect": "z/OS Connect Binding",
    "lpar": "LPAR",
    "region": "Region",
    "sysplex": "Sysplex",
    "returnCode": "Return Code",
    "abendCode": "Abend Code",
    "jobName": "Job Name",
    "stepName": "Step Name",
    "transactionId": "Transaction ID",
    "programName": "Program Name",
    "queueDepth": "Queue Depth",
    "channelStatus": "Channel Status",
    "operationalWindow": "Operational Window",
    "slaStatus": "SLA Status"
  }
}
```

Sem alterações visuais nesta onda.

---

## Impacto Base de Dados

| Operação | Tabela | Tipo |
|---|---|---|
| Atualizar check constraint | `cat_service_assets` | ALTER |
| Registar feature flags | `cfg_feature_flag_definitions` | INSERT (seed) |

---

## Testes

- [ ] Testes de regressão em todos os catálogos existentes (652+ catalog tests devem continuar a passar)
- [ ] Testes de regressão em change governance (195+ tests devem continuar a passar)
- [ ] Novos testes para enum serialization/deserialization com novos valores
- [ ] Teste de migração em base de dados limpa e com dados existentes

---

## Critérios de Aceite

1. ✅ Todos os testes existentes passam sem regressão
2. ✅ Novos valores de enum aceites pelo DB e pela API
3. ✅ Feature flags registados e consultáveis via Configuration module
4. ✅ i18n keys base disponíveis no `en.json`
5. ✅ Documentação de visão atualizada

---

## Riscos

| Risco | Mitigação |
|---|---|
| Serialização de novos enum values pode quebrar clientes existentes | Usar `JsonStringEnumConverter` (já em uso) — novos valores são aditivos |
| Check constraint update falha em dados existentes | Migração é expansão (ADD values) — compatível |

---

## Stories

| ID | Story | Prioridade |
|---|---|---|
| W0-S01 | Alinhar `ServiceType` enum C# com DB check constraint | P0 |
| W0-S02 | Adicionar novos `ServiceType` values para mainframe | P0 |
| W0-S03 | Adicionar novos `ContractType` + `ContractProtocol` values | P0 |
| W0-S04 | Adicionar novos `NodeType` + `EdgeType` values | P0 |
| W0-S05 | Adicionar novos `ChangeType` values | P0 |
| W0-S06 | Criar migração EF para check constraint update | P0 |
| W0-S07 | Registar feature flags para legacy capabilities | P1 |
| W0-S08 | Criar chaves i18n base (~100 keys) | P1 |
| W0-S09 | Atualizar `PRODUCT-VISION.md` com visão híbrida | P1 |
| W0-S10 | Criar `MATURITY-MODES.md` | P2 |
