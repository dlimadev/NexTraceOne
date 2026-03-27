# P5.1 — Post-Change Gap Report: Deploy Event → Release Correlation

**Data:** 2026-03-26  
**Fase:** P5.1 — Change Governance: Correlação Automática Deploy Event → Release

---

## 1. O que foi resolvido nesta fase

| Gap | Estado |
|-----|--------|
| Pipeline automático deploy event → Release | ✅ Implementado |
| Release deixou de depender apenas de criação manual | ✅ Implementado |
| Rastreabilidade via `ChangeEvent` ao receber deploy event | ✅ Implementado |
| Rastreabilidade via `ExternalMarker` ao receber deploy event | ✅ Implementado |
| Deduplicação por `ServiceName + Version + Environment` | ✅ Implementado |
| Ingestion API ligada ao módulo Change Governance via MediatR | ✅ Implementado |
| Resposta da Ingestion API inclui `releaseId` e `isNewRelease` | ✅ Implementado |
| Código compila sem erros | ✅ Validado |
| Testes unitários passam (200 ChangeGovernance, 2628+ total) | ✅ Validado |

---

## 2. O que ainda fica pendente após P5.1

### P5.2 — Trace Correlation (explicitamente fora do escopo de P5.1)

| Item pendente | Descrição |
|---------------|-----------|
| Tabela `trace_release_mapping` no ClickHouse | Ligação entre trace IDs (OTEL) e Release IDs |
| Correlação automática trace → release | Pipeline: trace chegando ao ClickHouse → correlacionar com Release |
| Query de traces contextualizados por release | API de observabilidade com contexto de release |
| Inserção em `ClickHouseAnalyticsWriter` | Registar evento de release no analytics schema |

### Correlação de catálogo

| Item pendente | Descrição |
|---------------|-----------|
| `ApiAssetId` permanece `Guid.Empty` para eventos externos | A ligação automática ao `ServiceId` do Catalog (`ApiAssetId`) ainda não é feita no pipeline de ingestão |
| Lookup de `ApiAssetId` por `ServiceName` no Catalog | Enriquecimento automático do `ApiAssetId` a partir do serviço registado no catálogo |

### Blast radius automático

| Item pendente | Descrição |
|---------------|-----------|
| Cálculo automático de blast radius ao criar release | Atualmente o blast radius requer chamada manual ao handler `CalculateBlastRadius` |

### Post-change verification automatizada

| Item pendente | Descrição |
|---------------|-----------|
| `ObservationWindow` criado automaticamente ao receber deploy | Actualmente requer criação manual |
| Alertas por degradação pós-release | Requer integração com telemetria e threshold configurable |
| `PostReleaseReview` iniciado automaticamente | Actualmente requer trigger manual |

### Rollback intelligence

| Item pendente | Descrição |
|---------------|-----------|
| Rollback event → correlação automática com release original | Quando um evento de rollback chega, ligar automaticamente à release que está a ser revertida |
| `RollbackAssessment` automático | Actualmente requer invocação manual |

### Integração com incidentes

| Item pendente | Descrição |
|---------------|-----------|
| Correlação automática release → incident | Quando um incidente ocorre após um deploy, ligar automaticamente à release mais recente do serviço |
| `ConfidenceStatus` actualizado por telemetria | Actualmente requer chamada manual a `UpdateConfidenceStatus` |

---

## 3. Limitações residuais após P5.1

1. **Persistência de dados da Integrations**: Os repositórios de `IntegrationConnector`,
   `IngestionSource` e `IngestionExecution` na Ingestion API utilizam `IntegrationsDbContext`
   directamente, mas o `IUnitOfWork` resolvido pelo DI após a adição de `AddChangeIntelligenceModule`
   aponta para `ChangeIntelligenceDbContext`. Os dados de integração (connector, source, execution)
   podem não ser persistidos no endpoint. Este é um bug pré-existente não relacionado com P5.1,
   que deve ser abordado numa fase de estabilização dedicada.

2. **`ApiAssetId = Guid.Empty`**: Releases criadas via Ingestion API não têm `ApiAssetId` preenchido.
   O índice `IX_chg_releases_ApiAssetId` continuará a conter `Guid.Empty` para estas releases.
   A correlação com o Catalog é uma tarefa futura.

3. **Validação FluentValidation na Ingestion API**: O `NotifyDeployment.Command` passa pelo pipeline
   de validação do MediatR. Se o deploy event tiver campos inválidos (ex: `CommitSha` > 100 chars),
   a correlação falha silenciosamente com log de aviso. O evento de integração é sempre aceite.

---

## 4. O que fica explicitamente para P5.2

- **Tabela `trace_release_mapping` no ClickHouse**: correlacionar `traceId` → `releaseId` automaticamente
- **Pipeline trace → release**: ao ingerir traces OTEL, identificar a release em contexto
- **Enriquecimento automático de `ApiAssetId`**: ao criar release via ingestão, resolver `ServiceName`
  → `ApiAssetId` via Catalog lookup
- **Blast radius automático**: disparar `CalculateBlastRadius` ao criar nova release
- **`ObservationWindow` automático**: criar janela de observação após deploy detectado

---

## 5. Próximos passos sugeridos

1. **Estabilização da Ingestion API**: resolver o conflito de `IUnitOfWork` entre Governance/Integrations
   e ChangeIntelligence, garantindo que dados de integração são persistidos correctamente.
2. **P5.2 — Trace Correlation**: implementar a tabela `trace_release_mapping` e o pipeline de correlação
   trace → release no ClickHouse.
3. **Enriquecimento de `ApiAssetId`**: adicionar ao `NotifyDeployment` handler um lookup ao Catalog
   para preencher `ApiAssetId` automaticamente quando o serviço está registado.
4. **Blast radius automático**: publicar um evento de domínio `ReleaseCreated` e consumir no handler
   de blast radius.
