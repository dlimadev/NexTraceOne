# NexTraceOne — Relatório de Auditoria: Backend
**Data:** 2026-04-10  
**Escopo:** Módulos .NET 10 — 12 bounded contexts, building blocks, platform  
**Ficheiros analisados:** ~3.112 ficheiros C#

---

## CRÍTICO

### [C-05] Endpoint de Export — Stub sem Implementação Real
**Ficheiro:** `src/modules/configuration/NexTraceOne.Configuration.API/Endpoints/ExportEndpointModule.cs` (linhas 37–47)  
**Módulo:** configuration  

**Problema:**  
O endpoint `POST /api/v1/export` retorna sempre o status "queued" com um `jobId` fictício. O comentário interno refere "Quartz job roadmap item" que não existe. Não há job de exportação implementado.

```csharp
// ACTUAL STATE - hardcoded response
return Results.Ok(new { status = "queued", jobId = Guid.NewGuid() });
```

**Impacto:** Qualquer feature de export (CSV, JSON, PDF) está completamente não funcional.

**Correcção:**
1. Criar `ExportJobHandler` com Quartz.NET que gere ficheiros reais
2. Armazenar jobs na tabela `cfg_export_jobs` com status e URL de download
3. Ou remover o endpoint até implementação real e comunicar ao frontend

---

### [C-06] Heurísticas Pseudo-Aleatórias no OnCall Intelligence
**Ficheiro:** `src/modules/operationalintelligence/NexTraceOne.OperationalIntelligence.Application/Incidents/Features/GetOnCallIntelligence/GetOnCallIntelligence.cs` (linhas 42–56)  
**Módulo:** operationalintelligence  

**Problema:**  
Indicadores de fadiga da equipa (night calls, response time) são calculados com seed hash do userId:

```csharp
var seed = Math.Abs(query.UserId.GetHashCode() % 100);
var nightCalls = Math.Min(20m + (seed % 30), 60m);
var responseMinutes = 15m + (seed % 45);
```

Os valores são determinísticos por userId mas completamente fictícios. O mesmo utilizador sempre verá os mesmos números, que não reflectem realidade.

**Impacto:** Dados de fadiga operacional apresentados como reais são fabricados. Decisões baseadas nestes dados são inválidas.

**Correcção:**
1. Criar tabela `opi_oncall_metrics` para armazenar métricas reais por utilizador/período
2. Ingerir dados de sistemas de on-call (PagerDuty, OpsGenie) via integration events
3. Calcular métricas a partir de dados de incidentes reais no `IncidentDbContext`
4. Até haver dados reais, mostrar "dados insuficientes" em vez de heurísticas

---

### [C-11] Contract Pipeline — Múltiplos Endpoints em Preview não Sinalizados
**Ficheiro:** `src/modules/catalog/NexTraceOne.Catalog.API/Portal/ContractPipeline/ContractPipelineEndpointModule.cs` (linhas 28–96)  
**Módulo:** catalog  

**Problema:**  
O endpoint `/server` retorna código com stubs e TODOs marcado explicitamente como "PREVIEW: not ready for production without review." Outros geradores (client SDK, tests, Postman) têm o mesmo problema. Não há feature flag ou header a indicar ao frontend que a resposta é preview.

**Impacto:** Developers recebem código scaffolded com `// TODO: implement` que não funciona, sem aviso na UI.

**Correcção:**
1. Adicionar campo `isPreview: bool` à resposta dos geradores
2. Frontend deve exibir banner/badge "Preview — requer revisão manual"
3. Documentar quais geradores estão production-ready vs preview

---

## ALTO

### [A-04] Silent Exception Handling em Handlers Críticos
**Ficheiros:**
- `catalog/.../GenerateMockConfiguration.cs` (linhas 82–86)
- `catalog/.../ExportContractMultiFormat.cs` (linha 73)
- `catalog/.../GenerateMockServer.cs` (linha 78)

**Problema:**  
```csharp
try {
    canonical = CanonicalModelBuilder.Build(...);
} catch {
    // Fallback com canonical nulo — segue em frente silenciosamente
}
```

Excepções de parsing são silenciadas. O handler continua com dados nulos/incompletos sem informar o utilizador do erro real.

**Correcção:**
```csharp
try {
    canonical = CanonicalModelBuilder.Build(...);
} catch (Exception ex) {
    logger.LogError(ex, "Failed to build canonical model for contract {ContractId}", contractId);
    return Result<T>.Failure("contract.canonical.parse_failed");
}
```

---

### [A-05] Null Result sem Validação em IncidentCorrelationService
**Ficheiro:** `src/modules/operationalintelligence/.../IncidentCorrelationService.cs` (linhas 26, 76–80)  

**Problema:**  
O método retorna `null` quando o contexto de correlação não é encontrado, mas acede a `blast.Value` sem verificar se `blast.IsSuccess && blast.Value != null`.

**Correcção:**
```csharp
// Substituir Task<T?> por Result<T>
public async Task<Result<CorrelationContext>> FindCorrelationAsync(...)
{
    // ...
    if (!blast.IsSuccess || blast.Value is null)
        return Result<CorrelationContext>.Failure("correlation.blast_radius.not_found");
}
```

---

### [A-08] CancellationToken com Default — Inconsistência nos Módulos
**Ficheiro:** `src/modules/aiknowledge/.../AiModelCatalogService.cs` (linhas 20–32)  

**Problema:**  
Vários métodos async usam `CancellationToken cancellationToken = default` como parâmetro. Isto mascara o facto de que muitos callers omitem o token, tornando operações lentas impossíveis de cancelar.

**Correcção:**
- Remover `= default` de assinaturas de métodos de serviço
- Passar `CancellationToken.None` explicitamente nos callers que não têm contexto de cancelamento
- Garantir que todos os controllers passam `HttpContext.RequestAborted`

---

## MÉDIO

### [M-02] Thresholds de Correlation Score Hardcoded
**Ficheiro:** `src/modules/operationalintelligence/.../IncidentCorrelationService.cs` (linhas 142–150, 201–204)  

```csharp
// Hardcoded temporal thresholds
if (hoursDiff <= 1) return 50;
if (hoursDiff <= 4) return 35;
if (hoursDiff <= 12) return 20;
if (hoursDiff <= 24) return 10;

// Hardcoded confidence thresholds
if (score >= 80) return CorrelationConfidence.High;
if (score >= 45) return CorrelationConfidence.Medium;
if (score >= 20) return CorrelationConfidence.Low;
```

**Correcção:** Mover thresholds para `ConfigurationDbContext` ou `appsettings.json` sob chave `IncidentCorrelation:Thresholds`.

---

### [M-03] Moeda EUR Hardcoded no Benchmarking
**Ficheiro:** `src/modules/governance/.../GetBenchmarking.cs` (linha 70)  

```csharp
var currency = g.Select(r => r.Currency).FirstOrDefault() ?? "EUR";
```

**Correcção:** Obter moeda padrão do contexto do tenant ou de `ConfigurationService.GetTenantCurrencyAsync()`.

---

### [M-06] Ausência de Integration Events no Módulo Configuration
**Módulo:** configuration  

Quando um valor de configuração é alterado (`SetConfigurationValue`, `SetFeatureFlagOverride`), não são publicados integration events. Outros módulos que dependem de configuração não são notificados e podem usar valores em cache.

**Correcção:**
1. Criar `ConfigurationValueChangedIntegrationEvent`
2. Publicar via outbox em todos os handlers de escrita do módulo configuration

---

### [M-07] Validação Incompleta no AddBookmark
**Ficheiro:** `src/modules/configuration/.../AddBookmark.cs` (linhas 16–23)  

```csharp
// Missing:
RuleFor(x => x.EntityType).IsInEnum();
```

---

### [M-08] Generated Code com TODO — Stubs não Funcionais
**Ficheiro:** `src/modules/catalog/.../GenerateServerFromContract.cs` (linha 75)  

O código gerado contém:
```csharp
// TODO: Inject application services and implement endpoint handlers
```

Developers recebem scaffolding não funcional. Deve ser claramente marcado como "template inicial" na resposta.

---

### [M-09] Audit Trail Ausente em Operações de Configuração
**Ficheiro:** `src/modules/configuration/.../CreateAlertRule.cs` (linhas 36–46)  

Criação de regras de alerta salva em DB mas não emite evento de auditoria explícito. Em módulos de configuração, toda escrita deve gerar trilha de auditoria.

**Correcção:** Despachar `AuditEvent` via `IAuditService` em todos os handlers de escrita do módulo configuration.

---

## BAIXO

### [L-01] Fire-and-Forget sem Retry no IncidentAlertHandler
**Ficheiro:** `src/modules/operationalintelligence/.../IncidentAlertHandler.cs` (linhas 21–68)  

Erros na criação de incidentes a partir de alertas são apenas logados, sem retry ou dead-letter.

**Correcção:** Usar outbox pattern ou Quartz job para garantir at-least-once delivery.

---

### [L-02] Truncamento de Snippets sem Flag
**Ficheiro:** `src/modules/aiknowledge/.../DatabaseRetrievalService.cs` (linhas 22, 68, 106, 140)  

Snippets truncados a 200 chars com "..." mas sem campo `isTruncated: bool` no resultado.

---

### [L-03] Empty Spec — Stub em vez de Erro de Validação
**Ficheiro:** `src/modules/catalog/.../GenerateMockConfiguration.cs` (linhas 56–62)  

Quando spec está vazia, retorna rota mock hardcoded em vez de erro de validação claro.

---

### [L-04] LanguageOverride não Validado vs Idiomas Suportados
**Ficheiro:** `src/modules/aiknowledge/.../GenerateAiScaffold.cs` (linhas 57, 120)  

`LanguageOverride` é aceite sem validar contra a lista de idiomas suportados pelo template.

---

## Resumo por Módulo

| Módulo | C | A | M | L |
|--------|---|---|---|---|
| operationalintelligence | 1 | 2 | 2 | 1 |
| catalog | 2 | 1 | 2 | 1 |
| configuration | 1 | — | 2 | — |
| aiknowledge | — | 1 | — | 2 |
| governance | — | — | 1 | — |
