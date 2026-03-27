# Archive: AI Audits (Histórico)

> Arquivado em: 2026-03-27 (P12.3)

Auditorias históricas do módulo AI do NexTraceOne que foram substituídas por documentação mais recente.

## Ficheiros

| Ficheiro | Data | Contradição Principal |
|---------|------|----------------------|
| `AI-LOCAL-IMPLEMENTATION-AUDIT-2026-03-17.md` | 2026-03-17 | Afirma "0% maturidade de migrations de BD" e "ZERO SDK de IA" — ambos desactualizados. DbContexts de IA estão registados no pipeline de migrations (P8+). Providers customizados existem (OllamaHttpClient, OpenAiHttpClient). |

**Não usar como referência do estado actual do módulo AI.**  
Para o estado actual, consultar:
- `docs/architecture/p9-5-ai-grounding-context-assembly-report.md`
- `docs/11-review-modular/00-governance/ai-and-agents-structural-audit.md`
- `docs/11-review-modular/07-ai-knowledge/`
