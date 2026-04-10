# Contract Conformance — Análise de Gaps do Módulo de Contratos

> Parte do plano: [01-OVERVIEW.md](01-OVERVIEW.md)

---

## 1. Metodologia

Esta análise categoriza os gaps por área funcional e classifica cada um por:
- **Impacto:** Alto / Médio / Baixo
- **Esforço:** Alto / Médio / Baixo
- **Tipo:** Gap novo | Parcialmente implementado | Ausente

---

## 2. Gaps no Fluxo de Conformance (tema principal)

| Gap | Impacto | Esforço | Estado |
|-----|---------|---------|--------|
| Sem endpoint `POST /contracts/validate-implementation` | Alto | Médio | Ausente |
| `IActiveContractResolver` não existe | Alto | Baixo | Ausente |
| `ContractConformanceCheck` entity não existe | Alto | Médio | Ausente |
| Sem CI Token com binding a serviço | Alto | Médio | Ausente |
| Sem `.nextraceone.yaml` convention | Médio | Baixo | Ausente |
| Sem GitHub Action `nextraceone/contract-gate` | Alto | Médio | Ausente |
| `EvaluateContractComplianceGate` não verifica conformance check | Médio | Baixo | Parcial |
| Sem política de conformance persistida por serviço/ambiente | Médio | Médio | Ausente |
| Sem seeder de `ConfigurationDefinition` para conformance | Médio | Baixo | Ausente |

---

## 3. Gaps no Changelog

| Gap | Impacto | Esforço | Estado |
|-----|---------|---------|--------|
| `ContractChangelogEntry` entity não existe | Alto | Médio | Ausente |
| Sem domain event handlers que gerem changelog | Alto | Médio | Ausente |
| `ConformanceCheckCompletedDomainEvent` não declarado | Alto | Baixo | Ausente |
| Sem query `GetContractChangelog` | Alto | Baixo | Ausente |
| Sem feed global `GetContractChangelogFeed` | Médio | Baixo | Ausente |
| Sem job de retenção do changelog | Baixo | Baixo | Ausente |
| Sem tab "Changelog" no `ContractWorkspacePage` (frontend) | Médio | Médio | Ausente |
| Sem widget de changelog no dashboard de equipa | Baixo | Baixo | Ausente |

---

## 4. Gaps no Fluxo de Notificação a Consumers

| Gap | Impacto | Esforço | Estado |
|-----|---------|---------|--------|
| Webhook delivery não implementado (infra existe) | Alto | Médio | Parcial |
| Sem notificação automática a consumers em breaking change | Alto | Médio | Ausente |
| Sem tracking de "consumer acknowledgement" de breaking change | Médio | Médio | Ausente |
| Sem notificação proactiva antes do sunset (N dias) | Médio | Baixo | Ausente |
| `ConsumerExpectation` tem infra mas sem lógica de validação real | Médio | Alto | Parcial |

**Detalhe sobre webhook:** A entidade `WebhookSubscription` e o repositório estão implementados (migração `20260408120000_AddWebhookSubscriptions.cs`), mas a lógica de entrega (HTTP POST para o endpoint do subscriber) não está implementada. Os integration events chegam ao outbox mas não são entregues externamente.

---

## 5. Gaps no Diff Semântico

| Gap | Impacto | Esforço | Estado |
|-----|---------|---------|--------|
| Sem análise de impacto ao nível do consumer (blast radius por operação) | Médio | Alto | Ausente |
| Sem sugestão automática de "deprecation path" para breaking changes | Médio | Médio | Ausente |
| Event schema evolution compatibility matrix (Kafka/AsyncAPI) | Médio | Alto | Ausente |
| Diff viewer visual lado-a-lado no frontend | Médio | Médio | Ausente |
| Sem comparação de múltiplas versões simultaneamente (3+) | Baixo | Alto | Ausente |

---

## 6. Gaps no Contract Studio (Editor)

| Gap | Impacto | Esforço | Estado |
|-----|---------|---------|--------|
| Sem validação em tempo real enquanto o utilizador edita (as-you-type) | Médio | Alto | Ausente |
| Sem editor visual para Spectral rulesets (UI existe só como lista) | Baixo | Médio | Ausente |
| Sem edição colaborativa simultânea (multi-user) | Baixo | Alto | Ausente |
| Import wizard UI não implementado (API existe) | Médio | Médio | Ausente |
| Sem preview de Swagger UI/ReDoc a partir da spec no Studio | Baixo | Baixo | Ausente |

---

## 7. Gaps de Integração com OpenAPI/AsyncAPI

| Gap | Impacto | Esforço | Estado |
|-----|---------|---------|--------|
| Sem conversão automática YAML ↔ JSON nos exporters | Baixo | Baixo | Ausente |
| Sem migração automática Swagger 2.0 → OpenAPI 3.0 | Baixo | Médio | Ausente |
| Sem import de spec a partir de URL (Swagger Hub, etc.) | Médio | Baixo | Ausente |
| Sem export bulk/batch para múltiplos contratos | Baixo | Baixo | Ausente |
| Sem geração automática de Swagger UI/ReDoc na publicação | Baixo | Baixo | Ausente |
| Sem sync com schema registry (Confluent, AWS Glue) | Médio | Alto | Ausente |

---

## 8. Gaps no Ciclo de Vida e Governance

| Gap | Impacto | Esforço | Estado |
|-----|---------|---------|--------|
| Sunset date não é enforced automaticamente | Médio | Baixo | Ausente |
| Sem operações bulk (deprecar múltiplos contratos de uma vez) | Baixo | Baixo | Ausente |
| Sem "consumer acceptance workflow" formal | Médio | Médio | Ausente |
| Sem tracking de SLA comprometido por contrato | Baixo | Médio | Ausente |
| Sem rollback intelligence específico para contratos | Médio | Médio | Ausente |
| Sem relatório de contratos sem conformance check recente | Médio | Baixo | Ausente |

---

## 9. Gaps de Visualização e UX

| Gap | Impacto | Esforço | Estado |
|-----|---------|---------|--------|
| Sem tab "Conformance" no `ContractWorkspacePage` | Alto | Médio | Ausente |
| Sem badge de status de conformance por ambiente na listagem | Médio | Baixo | Ausente |
| Sem visualização gráfica de dependências (lineage) de canonical entities | Baixo | Alto | Ausente |
| Sem mapa de topologia de producers/consumers de contratos | Médio | Alto | Ausente |
| Sem página de gestão de CI Tokens (frontend) | Alto | Médio | Ausente |
| Sem widget "Contract Health" no dashboard de serviço | Médio | Baixo | Ausente |
| Sem mock server gerado a partir da spec (para consumer testing) | Baixo | Alto | Ausente |

---

## 10. Gaps de Observabilidade do Módulo

| Gap | Impacto | Esforço | Estado |
|-----|---------|---------|--------|
| `DetectContractDrift` existe mas não gera alertas/notificações | Alto | Baixo | Parcial |
| Drift runtime não aparece no changelog automaticamente | Alto | Baixo | Ausente |
| Sem correlação directa drift runtime ↔ ConformanceCheck CI | Médio | Médio | Ausente |
| Sem anomaly detection na health score (quedas súbitas) | Baixo | Alto | Ausente |

---

## 11. Gaps de Testes

| Gap | Impacto | Esforço | Estado |
|-----|---------|---------|--------|
| Sem testes de integração para Spectral | Médio | Médio | Ausente |
| Sem testes do diff calculator multi-protocolo | Médio | Médio | Parcial |
| Sem testes de conflito em edição concorrente de drafts | Baixo | Baixo | Ausente |
| Sem testes de entrega de webhooks | Médio | Baixo | Ausente |
| Sem testes E2E para fluxo de conformance CI | Alto | Médio | Ausente |

---

## 12. Priorização consolidada de gaps

### Críticos (bloqueia proposta de valor principal)

1. Endpoint `POST /contracts/validate-implementation`
2. `IActiveContractResolver`
3. `ContractConformanceCheck` entity + persistência
4. CI Token com binding a serviço
5. Webhook delivery (notificação a consumers)
6. `ContractChangelogEntry` + handlers

### Importantes (melhora significativamente o produto)

7. Política de conformance persistida (via Configuration module)
8. Extensão do `EvaluateContractComplianceGate`
9. Tab "Changelog" no frontend
10. Tab "Conformance History" no frontend
11. Página de gestão de CI Tokens
12. Notificação proactiva antes de sunset
13. Detecção de drift runtime → changelog + alerta
14. `ConsumerExpectation` com validação real

### Nice-to-have (valor incremental)

15. Diff viewer visual lado-a-lado
16. Import wizard UI
17. Sunset date enforcement automático
18. Consumer acceptance workflow
19. Import de spec por URL
20. Bulk operations (deprecar múltiplos)

### Roadmap futuro

21. Schema registry integration (Confluent, AWS Glue)
22. Event schema evolution compatibility matrix
23. Mock server gerado a partir da spec
24. Mapa topológico de producers/consumers
25. Edição colaborativa no Studio
26. Anomaly detection no health score

---

## 13. Observação sobre dívida técnica existente

O módulo de Contratos está bem arquitectado e tem uma base sólida. Os gaps identificados não são problemas de qualidade — são **funcionalidades ainda não implementadas** que completam o ciclo de governança. A infra (entidades, parsers, diff calculators, lifecycle) está pronta para ser extendida sem rupturas.

O único ponto de dívida real é o **webhook delivery** — a infra existe mas a entrega real nunca foi implementada, o que bloqueia múltiplos fluxos que dependem de notificação externa.
