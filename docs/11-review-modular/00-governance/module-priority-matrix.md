# Matriz de Prioridade de Módulos — NexTraceOne

## Objetivo

Esta matriz define a **prioridade de revisão e correção** de cada módulo do NexTraceOne, baseada em critérios objetivos alinhados com a visão oficial do produto.

---

## Critérios de Avaliação

Cada módulo é avaliado nos seguintes critérios, com uma escala de 1 a 5:

| Critério                   | Descrição                                                                                  | Escala                                           |
|----------------------------|--------------------------------------------------------------------------------------------|--------------------------------------------------|
| **Criticidade**            | Quão essencial é este módulo para o funcionamento core do produto                          | 1 = Opcional, 5 = Indispensável                  |
| **Impacto Transversal**    | Quantos outros módulos dependem deste ou são afetados por ele                              | 1 = Isolado, 5 = Afeta todos os módulos          |
| **Visibilidade no Produto**| Quão visível e frequente é o uso deste módulo pelos utilizadores                           | 1 = Raramente usado, 5 = Usado constantemente    |
| **Risco Técnico**          | Probabilidade de problemas técnicos, dívida técnica ou inconsistências                     | 1 = Baixo risco, 5 = Alto risco                  |
| **Dependências**           | Quantas dependências externas ou bloqueantes existem para este módulo                      | 1 = Autónomo, 5 = Muito dependente               |

### Cálculo da Prioridade Final

A prioridade final é uma classificação qualitativa (`CRITICAL`, `HIGH`, `MEDIUM`, `LOW`) baseada na combinação dos critérios acima e na posição do módulo na estratégia de evolução do produto.

---

## Matriz de Prioridade

| Módulo | Criticidade | Impacto Transversal | Visibilidade no Produto | Risco Técnico | Dependências | Prioridade Final | Justificação |
|--------|-------------|---------------------|-------------------------|---------------|--------------|------------------|--------------|
| **01-contracts** | 5 | 5 | 5 | _A avaliar_ | _A avaliar_ | 🔴 `CRITICAL` | Contratos são first-class citizens. O produto é fonte de verdade para contratos. Impacto direto em catálogo, change governance e IA. |
| **02-identity-access** | 5 | 5 | 4 | _A avaliar_ | _A avaliar_ | 🔴 `CRITICAL` | Fundação do produto. Sem identidade e autorização, nenhum módulo funciona corretamente. Base para persona-awareness. |
| **03-catalog** | 5 | 5 | 5 | _A avaliar_ | _A avaliar_ | 🔴 `CRITICAL` | Service Catalog é o pilar central. Ownership, dependências e topologia afetam todos os outros módulos. |
| **04-change-governance** | 4 | 4 | 4 | _A avaliar_ | _A avaliar_ | 🟠 `HIGH` | Change Confidence é diferenciador do produto. Depende de catálogo e contratos para funcionar plenamente. |
| **05-operational-intelligence** | 4 | 3 | 4 | _A avaliar_ | _A avaliar_ | 🟠 `HIGH` | Incidentes, runbooks e AIOps são essenciais para confiabilidade operacional. Depende de catálogo e identidade. |
| **06-governance** | 3 | 3 | 3 | _A avaliar_ | _A avaliar_ | 🟡 `MEDIUM` | Reports, compliance e risk center são importantes mas dependem de dados gerados por outros módulos. |
| **07-configuration** | 3 | 4 | 2 | _A avaliar_ | _A avaliar_ | 🟡 `MEDIUM` | Configuração de ambientes e integrações é transversal mas raramente visível ao utilizador final. |
| **08-ai-knowledge** | 4 | 4 | 4 | _A avaliar_ | _A avaliar_ | 🟠 `HIGH` | IA governada é pilar do produto. Model registry, políticas e knowledge hub impactam toda a experiência. |

---

## Módulos Adicionais

| Módulo | Criticidade | Impacto Transversal | Visibilidade no Produto | Risco Técnico | Dependências | Prioridade Final | Justificação |
|--------|-------------|---------------------|-------------------------|---------------|--------------|------------------|--------------|
| **09-audit-compliance** | 3 | 3 | 2 | _A avaliar_ | _A avaliar_ | 🟡 `MEDIUM` | Auditoria é transversal mas o módulo de consulta é secundário. Compliance depende de dados de outros módulos. |
| **10-notifications** | 2 | 2 | 3 | _A avaliar_ | _A avaliar_ | 🟢 `LOW` | Notificações são importantes para UX mas não são core. Podem ser implementadas incrementalmente. |
| **11-integrations** | 3 | 3 | 2 | _A avaliar_ | _A avaliar_ | 🟡 `MEDIUM` | Integrações externas são necessárias mas podem ser priorizadas conforme necessidade de clientes. |
| **12-product-analytics** | 2 | 1 | 2 | _A avaliar_ | _A avaliar_ | 🟢 `LOW` | Analytics de produto são úteis para decisões internas mas não impactam a experiência core. |

---

## Visualização por Prioridade

### 🔴 CRITICAL — Revisão e correção imediata

| Módulo | Justificação resumida |
|--------|-----------------------|
| 01-contracts | Contratos são pilar central do produto |
| 02-identity-access | Fundação de segurança e persona |
| 03-catalog | Catálogo de serviços é core |

### 🟠 HIGH — Revisão prioritária após os módulos critical

| Módulo | Justificação resumida |
|--------|-----------------------|
| 04-change-governance | Change Confidence é diferenciador |
| 05-operational-intelligence | Operações e AIOps são essenciais |
| 08-ai-knowledge | IA governada é pilar transversal |

### 🟡 MEDIUM — Revisão após estabilização dos módulos high

| Módulo | Justificação resumida |
|--------|-----------------------|
| 06-governance | Reports e compliance dependem de dados de outros módulos |
| 07-configuration | Configuração é transversal mas pouco visível |
| 09-audit-compliance | Consulta de auditoria é secundária |
| 11-integrations | Integrações externas são conforme necessidade |

### 🟢 LOW — Revisão quando os módulos medium estiverem estáveis

| Módulo | Justificação resumida |
|--------|-----------------------|
| 10-notifications | Notificações podem ser incrementais |
| 12-product-analytics | Analytics é interno e não impacta UX core |

---

## Dependências Entre Módulos

```
02-identity-access ──→ (todos os módulos dependem de identidade e autorização)
        │
        ▼
03-catalog ──→ 01-contracts ──→ 04-change-governance
        │            │
        ▼            ▼
05-operational-intelligence ──→ 08-ai-knowledge
        │                            │
        ▼                            ▼
06-governance ◄── 09-audit-compliance
        │
        ▼
07-configuration ──→ 11-integrations
        │
        ▼
10-notifications ──→ 12-product-analytics
```

---

## Instruções de Atualização

1. Após a análise de cada módulo, atualizar os valores de "Risco Técnico" e "Dependências"
2. Reavaliar a prioridade final se os valores mudarem significativamente
3. Documentar a justificação para qualquer mudança de prioridade
4. Manter a secção "Visualização por Prioridade" atualizada
5. Atualizar o diagrama de dependências conforme novos módulos ou relações forem descobertos

---

## Notas

- A prioridade é orientada pela **visão oficial do produto**, não apenas pela complexidade técnica
- Módulos com prioridade `CRITICAL` não devem ser adiados — o produto depende deles
- A prioridade pode mudar conforme a revisão avança e novos gaps são descobertos
- Esta matriz deve ser revisada mensalmente ou após cada ciclo de revisão
