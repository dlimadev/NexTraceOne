# Plano 07 — Legacy/Mainframe Support

> **Prioridade:** 🔵 Futuro  
> **Esforço total:** 24+ semanas  
> **Referência:** `FUTURE-ROADMAP.md` secção 12 (Legacy/Mainframe Waves 00–12)  
> **Contexto:** Expansão do NexTraceOne para core systems legados (IBM Z, COBOL, CICS, IMS, DB2, MQ). Mercado: bancos, seguradoras, governo, utilities.

---

## Por Que Legacy/Mainframe?

Muitas organizações enterprise têm 30–60% da sua lógica de negócio crítica em mainframe. O Dynatrace tem suporte limitado para Z/COBOL. O NexTraceOne pode ser o primeiro produto de governança operacional com visibilidade nativa de mainframe.

---

## Wave 00 — Strategy & Foundation (3–4 semanas)

**Objetivo:** Definir a arquitetura de domínio para integração com core systems.

**Entregas:**
1. **Domain model para legacy assets:** `LegacyServiceAsset` extends `ServiceAsset` com campos específicos: `PlatformType` (Z/OS | VSE | iSeries), `CobdolVersion`, `Region`, `LPAR`
2. **Contract types para legacy:**
   - `Copybook` contract (já existe em Contract Studio)
   - `MqMessage` contract (já existe)
   - `CicsCommarea` contract (já existe)
   - `FixedLayout` contract (já existe)
   - `ImsSegment` contract (novo)
   - `Db2StoredProcedure` contract (novo)
3. **Telemetry bridge:** como ingerir traces de COBOL (IBM Z APM → OTel Collector → NexTraceOne)
4. Decisão de arquitetura: IBMZ APM REST API vs. SMF records vs. CICS Transaction Gateway

---

## Wave 01 — Catalog Foundation (4–6 semanas)

**Objetivo:** Service Catalog para activos IBM Z / COBOL / CICS / IMS / DB2 / MQ.

**Entregas:**
1. **COBOL Program asset:** `CobolProgramAsset` com campos: `ProgramId`, `CompileDate`, `LinesOfCode`, `CalledPrograms[]`, `DataSets[]`
2. **CICS Transaction asset:** `CicsTransactionAsset` com `TransactionId`, `RelatedPrograms[]`, `AverageResponseMs`
3. **IMS Database asset:** `ImsDatabaseAsset` com `DbdName`, `Segments[]`, `AccessMethod` (HDAM/HIDAM/HSAM)
4. **DB2 Stored Procedure asset:** `Db2StoredProcedureAsset` com schema, inputs/outputs
5. **MQ Queue asset:** `MqQueueAsset` com `QueueManager`, `QueueName`, `MessageTypes[]`
6. Import via JCL/SYSPRINT parsing (batch job que analisa outputs de compilação COBOL)

---

## Wave 02 — Input Formats & Telemetry Ingestion (3–4 semanas)

**Objetivo:** Ingerir telemetria de mainframe no pipeline OTel do NexTraceOne.

**Entregas:**
1. **SMF Record Parser:** parsear SMF records (System Management Facilities) exportados do Z/OS
   - SMF Type 30 (Job step timing)
   - SMF Type 110 (CICS performance)
   - SMF Type 101 (DB2 accounting)
2. **CICS Gateway receiver:** receiver OTel para CICS Transaction Gateway (Java-based bridge)
3. **Batch job traces:** `JobExecutionRecord` com JOBNAME, STEPNAME, elapsed time, CC (return code)
4. **Legacy metrics:** CPU time, I/O operations, paging, queue depths para métricas de SLO

---

## Wave 03 — Normalization & Correlation (2–3 semanas)

**Objetivo:** Correlacionar traces de mainframe com microserviços modernos.

**Entregas:**
1. **Trace correlation:** propagar `traceparent` de serviços .NET/Java para CICS via CICS-to-OTLP bridge
2. **Cross-platform service map:** edges entre `ServiceAsset` (moderno) e `CicsTransactionAsset` (legacy) no grafo
3. **Normalization rules:** mapear campos SMF para atributos OTel canónicos (`service.name`, `http.status_code` equivalentes)

---

## Wave 04 — Contract Governance para Legacy (3–4 semanas)

**Objetivo:** Governança de contratos para interfaces legacy (copybooks, MQ messages, COMMAREA).

**Entregas:**
1. **Copybook Contract visual builder:** editor visual de layouts COBOL com campos, tipos PIC, offsets
2. **COMMAREA diff detection:** comparar dois copybooks e identificar breaking changes (field renamed, offset changed, type changed)
3. **MQ Message contract:** schema de mensagens MQ com versioning e consumer inventory
4. **Contract Drift para legacy:** comparar contrato publicado com copybook real em produção

---

## Wave 05–12 — (Resumo das Waves Avançadas)

| Wave | Objetivo | Esforço |
|------|----------|---------|
| **05** — Hybrid Graph | Grafo unificado moderno + mainframe (bidireccional, navigável) | 2–3 sem |
| **06** — Change Intelligence para Legacy | Blast radius quando se modifica programa COBOL | 2–3 sem |
| **07** — Batch Intelligence | Scheduler JCL tracking, job dependencies, failure prediction | 2–3 sem |
| **08** — Messaging Intelligence | MQ queue depth trends, message aging, dead letter analysis | 2–3 sem |
| **09** — AI Assistive para Legacy | Agente especializado em COBOL: analisa programs, sugere modernização | 3–4 sem |
| **10** — Workflow & Policies | Change governance para mainframe (emergency fix, scheduled batch change) | 2 sem |
| **11** — Frontend Enterprise | Views dedicadas para legacy assets no frontend existente | 2–3 sem |
| **12** — Security Readiness | Auditoria de mainframe: RACF/ACF2 integration, security events | 2 sem |

---

## Pré-requisitos

| Pré-requisito | Notas |
|--------------|-------|
| Acesso a ambiente Z/OS de desenvolvimento | IBM Z Development and Test Environment (ZD&T) — licença necessária |
| CICS Transaction Gateway | Middleware IBM para bridge Java↔CICS |
| IBM Z APM Connect | Exporta telemetria Z para OTel (IBM Open Beta) |
| Contrato com cliente enterprise que usa mainframe | Para validação real das funcionalidades |

## Decisão de Priorização

Este grupo é marcado como **Futuro** porque:
1. Requer parceria/acesso a hardware IBM Z para desenvolvimento
2. O mercado target (bancos grandes, seguradoras) tem ciclos de venda longos
3. O ROI justifica-se com 2–3 clientes enterprise grandes
4. Pode ser desenvolvido como módulo independente sem impacto no core

**Recomendação:** iniciar quando houver um cliente enterprise âncora disposto a co-desenvolver.
