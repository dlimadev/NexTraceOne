# Gaps — Backend
> Análise detalhada dos gaps encontrados no backend .NET do NexTraceOne.

---

## 1. Interfaces PLANNED sem Implementação (Crítico)

### 1.1 IIntegrationContextResolver
**Ficheiro:** `src/building-blocks/NexTraceOne.BuildingBlocks.Application/Integrations/IIntegrationContextResolver.cs`

**Status declarado no código:**
```
// IMPLEMENTATION STATUS: Planned — no implementation exists.
// This interface defines multi-tenant integration binding resolution.
// Do NOT register in DI or reference in handlers until an implementation exists.
```

**Impacto:** Toda integração externa (GitLab, Jenkins, GitHub, Azure DevOps) que precise resolver o binding correto por tenant + ambiente não tem base de execução. Integrações actuais provavelmente usam configuração estática ou fallback inseguro.

**Risco:** Contaminação de dados entre tenants se um binding errado for resolvido.

---

### 1.2 IDistributedSignalCorrelationService
**Ficheiro:** `src/building-blocks/NexTraceOne.BuildingBlocks.Application/Correlation/IDistributedSignalCorrelationService.cs`

**Status:** PLANNED — sem implementação.

**Impacto:** A correlação entre mudanças e incidentes (core de Change Intelligence) depende desta interface. Sem ela, a correlação é manual ou inferida apenas por janela temporal simples — o que não é adequado para enterprise.

---

### 1.3 IPromotionRiskSignalProvider
**Ficheiro:** `src/building-blocks/NexTraceOne.BuildingBlocks.Application/Correlation/IPromotionRiskSignalProvider.cs`

**Status:** PLANNED — sem implementação.

**Impacto:** O scoring de risco de promoção entre ambientes não tem sinais reais de runtime. O score retornado é provavelmente estático ou baseado apenas em regras simples.

---

## 2. TODOs e Gaps em Handlers

### 2.1 Alert → Incident sem Outbox
**Ficheiro:** `src/modules/operationalintelligence/NexTraceOne.OperationalIntelligence.Infrastructure/Incidents/IncidentAlertHandler.cs`
**Linha:** ~61

```csharp
// TODO: Migrate to outbox pattern or Quartz retry job for at-least-once delivery.
```

**Problema:** Se a criação do incidente falhar (timeout de BD, exceção transiente), o alerta é perdido. Não há retry, não há dead-letter queue, não há compensação.

**Impacto em produção:** Alertas críticos podem desaparecer silenciosamente sem rastreio.

**Solução:** Implementar OutboxEventBus (já existe foundation em BuildingBlocks) ou criar Quartz job de retry com exponential backoff.

---

### 2.2 Contract Code Generation Pipeline com TODOs
**Ficheiros:**
- `src/modules/catalog/NexTraceOne.Catalog.Application/Portal/ContractPipeline/Features/GenerateServerFromContract/GenerateServerFromContract.cs`
- `src/modules/catalog/.../GenerateMockServer/GenerateMockServer.cs`

**TODOs encontrados no código gerado:**
```
// TODO: Inject application services and implement endpoint handlers (.NET)
// TODO: Inject services and implement handlers (Java)
// TODO: Implement handlers for {{serviceName}} (Node.js)
// TODO: decode body and persist (Go)
```

**Contexto:** O endpoint está marcado como "PREVIEW" e avisa que o código gerado não está pronto para produção. Mas o risco é que utilizadores usem o output sem ler o aviso.

**Linguagens suportadas (todas com stubs):** .NET, Java, Node.js, Go, Python.

---

## 3. Módulo Licensing — Enforcement Ausente

**Estado:** Framework de licensing existe (entidades, DTOs, policies).

**O que falta:**
- Machine fingerprinting real
- Assembly integrity verification
- Heartbeat de licença para validação recorrente
- Revogação remota
- Enforcement no pipeline de requests (middleware que bloqueia se licença inválida)
- Licença offline (air-gapped)

**Impacto:** Um cliente pode usar o produto indefinidamente sem licença válida — o produto não se defende.

---

## 4. Módulo Integrations — Funcionalidade Mínima

**Estado:** Estrutura existe (DbContext, entidades, endpoints).

**O que falta:**
- IIntegrationContextResolver (ver secção 1.1)
- Testes de binding por tenant
- Lógica de retry para falhas de integração externa
- Webhook inbound com validação de assinatura
- Auditoria de chamadas a sistemas externos

**Módulos afectados:** GitLab, Jenkins, GitHub Actions, Azure DevOps — todos dependem de bindings resolvidos correctamente.

---

## 5. Módulo ProductAnalytics — Implementação Rasa

**Estado:** DbContext existe, endpoints mapeados, mas muito pouco testado e pouco implementado.

**Evidência:** Apenas 4 ficheiros de teste para todo o módulo.

**O que provavelmente falta:**
- Colecta real de eventos de uso do produto
- Segmentação por persona
- Correlação de features usadas com outcomes
- Funis de adopção
- Relatórios de activação e retenção

---

## 6. OperationalIntelligence — Cost Intelligence Parcial

**Estado:** Estrutura de cost intelligence existe (CostIntelligenceDbContext, entidades).

**O que falta:**
- Ingestão real de dados de custo (cloud billing APIs)
- Correlação custo → mudança → ambiente
- Alertas de anomalia de custo
- Relatório de desperdício por serviço/equipa
- FinOps por release (custo incremental de deploy)

---

## 7. AIKnowledge — IDE Integration Foundation Apenas

**Estado:** Entidades e endpoints para IDE integrations existem.

**O que falta:**
- Protocolo real de extensão VS Code (Language Server Protocol ou similar)
- Protocolo real de extensão Visual Studio
- Autenticação da extensão com o backend
- Auditoria de acções feitas via IDE
- Contexto de contrato passado para o LLM a partir do IDE

---

## 8. Resumo de Prioridades Backend

| Gap | Severidade | Esforço Estimado |
|-----|-----------|-----------------|
| IIntegrationContextResolver | P0 — Crítico | Alto |
| Alert→Incident sem outbox | P0 — Crítico | Médio |
| IDistributedSignalCorrelationService | P0 — Crítico | Alto |
| IPromotionRiskSignalProvider | P0 — Crítico | Alto |
| Licensing enforcement | P1 — Alto | Muito alto |
| Integrations functional gaps | P1 — Alto | Alto |
| ProductAnalytics implementation | P1 — Alto | Médio |
| Cost intelligence ingestão real | P2 — Médio | Alto |
| IDE integration real protocol | P2 — Médio | Muito alto |
| Code generation pipeline (stubs) | P2 — Médio | Médio |
