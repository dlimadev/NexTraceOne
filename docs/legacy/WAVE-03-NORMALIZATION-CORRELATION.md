# Onda 3 — Normalização e Correlação

> **Duração estimada:** 3-4 semanas
> **Dependências:** Ondas 1 e 2
> **Risco:** Médio — correlação depende de naming conventions consistentes
> **Referência:** [LEGACY-MAINFRAME-WAVES.md](../LEGACY-MAINFRAME-WAVES.md)

---

## Objetivo

Correlacionar telemetria legacy com ativos do catálogo, mudanças e incidentes. Após esta onda, eventos de batch, MQ e mainframe estarão ligados aos ativos registados e poderão gerar incidentes automaticamente.

---

## Entregáveis

- [ ] Correlação batch event → `BatchJobDefinition` (por job name)
- [ ] Correlação MQ event → `QueueDefinition` + `QueueManager` (por queue/manager name)
- [ ] Correlação mainframe event → `MainframeSystem` + `CicsTransaction`/`ImsTransaction`
- [ ] Timeline unificada de eventos (moderno + legacy)
- [ ] Cross-correlation: batch failure → criação automática de incident
- [ ] Cross-correlation: MQ anomaly → criação automática de incident
- [ ] Extensão do `IncidentChangeCorrelation` para suportar legacy assets

---

## Impacto Backend

### Correlation Engine

#### `LegacyEventCorrelator` Service

Responsabilidade: receber eventos normalizados e correlacionar com ativos do catálogo.

```csharp
public interface ILegacyEventCorrelator
{
    Task<CorrelationResult> CorrelateAsync(
        NormalizedLegacyEvent @event, 
        CancellationToken ct);
}
```

**Estratégia de matching:**

| Campo do Evento | Match com Catálogo | Método |
|---|---|---|
| `JobName` | `BatchJobDefinition.JobName` | Exact match |
| `QueueManagerName` | `QueueManagerDefinition.Name` | Exact match |
| `QueueName` | `QueueDefinition.QueueName` | Exact match |
| `TransactionId` | `CicsTransaction.TransactionId` | Exact match |
| `ProgramName` | `CobolProgram.ProgramName` | Exact match |
| `SystemName` | `MainframeSystem.Name` | Exact match |
| `ServiceName` | `ServiceAsset.Name` | Fuzzy match (alias support) |

**Fallback hierarchy:**
1. Exact match por nome
2. Alias match (tabela de aliases configurável)
3. Fuzzy match (Levenshtein distance ≤ 2)
4. Unmatched — marcado para resolução manual

#### Handlers de Correlação

| Handler | Trigger | Acção |
|---|---|---|
| `CorrelateBatchEventHandler` | `BatchEventIngested` domain event | Encontra job no catálogo, liga evento |
| `CorrelateMqEventHandler` | `MqEventIngested` domain event | Encontra queue/manager, liga evento |
| `CorrelateMainframeEventHandler` | `MainframeEventIngested` domain event | Encontra ativo mainframe, liga evento |
| `AutoCreateIncidentFromBatchFailureHandler` | Batch event com status `failed`/`abended` | Cria `IncidentRecord` automático |
| `AutoCreateIncidentFromMqAnomalyHandler` | MQ event tipo `dlq_message` ou `depth_threshold` critical | Cria `IncidentRecord` automático |

### Extensão do Modelo de Incidentes

Extensão de `IncidentChangeCorrelation` para incluir:
```csharp
// Novos campos na correlação
public string? LegacyAssetType { get; private set; }    // "BatchJob", "MqQueue", "CicsTransaction"
public string? LegacyAssetName { get; private set; }    // Nome do ativo correlacionado
public string? LegacyAssetId { get; private set; }      // ID no catálogo
```

### Timeline Unificada

Query que combina:
- Events de releases (ChangeEvent)
- Events de batch (ClickHouse)
- Events de MQ (ClickHouse)
- Events de mainframe (ClickHouse)
- Incidentes (PostgreSQL)

Ordenados por timestamp, filtráveis por serviço/ativo/ambiente.

---

## Impacto Frontend

### Timeline de Eventos

Extensão das páginas de detalhe de ativos legacy para incluir:
- **Timeline unificada** — mostra todos os eventos relacionados ao ativo
- **Indicadores de correlação** — badges mostrando eventos correlacionados
- **Link para incidentes** — se evento gerou incidente, link direto

### Extensão da Lista de Incidentes

- Coluna/filtro "Source" que inclui "Batch", "MQ", "Mainframe"
- Badge na lista indicando se incidente foi criado automaticamente
- Detalhe do incidente com contexto legacy (job name, queue, return code)

---

## Impacto Base de Dados

Sem novas tabelas. Extensões:

| Tabela | Alteração |
|---|---|
| `ops_incident_change_correlations` | Novos campos: `LegacyAssetType`, `LegacyAssetName`, `LegacyAssetId` |
| Possível: `cat_legacy_event_aliases` | Tabela de aliases para matching (configurable por admin) |

---

## Testes

### Testes Unitários (~30)
- Correlator: matching exact, alias, fuzzy, unmatched
- Handlers: criação de incidente automático
- Timeline: query composition

### Testes de Integração (~15)
- Evento de batch → correlação com job no catálogo
- Batch failure → incidente criado automaticamente
- MQ anomaly → incidente criado
- Timeline mostra eventos de múltiplas fontes

---

## Critérios de Aceite

1. ✅ Evento de batch correlaciona com job definition no catálogo
2. ✅ Falha de batch gera incidente automaticamente
3. ✅ Anomalia MQ gera incidente automaticamente
4. ✅ Timeline mostra eventos legacy e modernos juntos
5. ✅ Incidentes gerados automaticamente são visíveis na lista de incidentes
6. ✅ Correlação funciona com exact match e alias match

---

## Riscos

| Risco | Severidade | Mitigação |
|---|---|---|
| Naming inconsistencies entre ambientes | Alta | Alias table configurável. Fuzzy matching. Manual override |
| Volume de eventos pode gerar muitos incidentes | Média | Throttling/dedup. Regra de criação configurável |
| Correlação cross-module requer outbox funcional | Alta | Garantir outbox ativo como pré-requisito (Onda 0) |

---

## Stories

| ID | Story | Prioridade |
|---|---|---|
| W3-S01 | Implementar `ILegacyEventCorrelator` service | P0 |
| W3-S02 | Implementar `CorrelateBatchEventHandler` | P1 |
| W3-S03 | Implementar `CorrelateMqEventHandler` | P1 |
| W3-S04 | Implementar `CorrelateMainframeEventHandler` | P1 |
| W3-S05 | Implementar auto-creation de incidente para batch failure | P1 |
| W3-S06 | Implementar auto-creation de incidente para MQ anomaly | P1 |
| W3-S07 | Estender `IncidentChangeCorrelation` com campos legacy | P1 |
| W3-S08 | Implementar unified timeline query | P1 |
| W3-S09 | Criar tabela de aliases para matching | P2 |
| W3-S10 | Extensão do frontend com timeline e indicadores | P2 |
| W3-S11 | Extensão da lista de incidentes com fonte legacy | P2 |
| W3-S12 | Criar testes unitários e integração (~45) | P1 |
