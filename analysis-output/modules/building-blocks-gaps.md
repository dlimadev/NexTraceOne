> **⚠️ ARCHIVED — April 2026**: Este documento foi gerado como análise pontual de gaps. Muitos dos gaps aqui listados já foram resolvidos. Para o estado atual, consultar `docs/CONSOLIDATED-GAP-ANALYSIS-AND-ACTION-PLAN.md` e `docs/IMPLEMENTATION-STATUS.md`.

# Building Blocks — Gaps, Erros e Pendências

## 1. Estado resumido do módulo
5 projectos (Core, Application, Infrastructure, Observability, Security). 39 test files. Produção-ready. Gaps mínimos.

## 2. Gaps críticos
Nenhum.

## 3. Gaps altos
Nenhum.

## 4. Gaps médios

### 4.1 Observability aponta para localhost:4317 como default
- **Severidade:** MEDIUM
- **Classificação:** CONFIG_RISK
- **Descrição:** OpenTelemetry configurado para `localhost:4317` como default gRPC endpoint. Em produção, isto requer override obrigatório. Se não configurado, telemetria é enviada para localhost (silently fails).
- **Impacto:** Deploy em produção sem configuração de OTEL endpoint resulta em perda de telemetria sem erro visível.
- **Evidência:** `src/building-blocks/NexTraceOne.BuildingBlocks.Observability/DependencyInjection.cs` — configuração canónica `Telemetry:Collector:OtlpGrpcEndpoint`

## 5. Itens mock / stub / placeholder
Nenhum.

## 6. Erros de desenho / implementação incorreta
Nenhum.

## 7-12. Gaps de frontend / backend / banco / configuração / documentação / seed
N/A.

## 13. Ações corretivas obrigatórias
1. Documentar obrigatoriedade de configuração do OTEL endpoint em produção
2. Considerar log warning no arranque quando OTEL endpoint = localhost e ambiente != Development
