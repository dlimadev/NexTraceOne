# WAVE 1 — Validation Tracker

## Objetivo
Acompanhar a execução e validação da **Onda 1**, garantindo que os 4 fluxos centrais do NexTraceOne fiquem realmente utilizáveis, integrados e com valor de produto.

## Fluxos da Onda 1
1. Source of Truth / Contract Governance
2. Change Confidence
3. Incident Correlation & Mitigation
4. AI Assistant grounded

## Status global da Onda 1
| Fluxo | Backend | Frontend | i18n | Testes | Docs | Evidência real | Status geral |
|---|---|---|---|---|---|---|---|
| Source of Truth / Contract Governance | A preencher | A preencher | A preencher | A preencher | A preencher | A preencher | A preencher |
| Change Confidence | A preencher | A preencher | A preencher | A preencher | A preencher | A preencher | A preencher |
| Incident Correlation & Mitigation | A preencher | A preencher | A preencher | A preencher | A preencher | A preencher | A preencher |
| AI Assistant grounded | A preencher | A preencher | A preencher | A preencher | A preencher | A preencher | A preencher |

## Regras de validação
- Não marcar como concluído sem demonstração ponta a ponta.
- Toda validação precisa ter evidência.
- Toda funcionalidade crítica precisa de teste mínimo.
- Todo texto visível na UI precisa usar i18n.
- Toda divergência entre docs e código deve ser registrada.

---

## Fluxo 1 — Source of Truth / Contract Governance

### Objetivo
Permitir que a plataforma funcione como fonte real de verdade para serviços e contratos.

### Critérios de aceite
- [ ] Serviço pode ser encontrado por busca e filtros
- [ ] Contrato pode ser importado/cadastrado
- [ ] Versões podem ser consultadas
- [ ] Diff entre versões funciona e é compreensível
- [ ] Ownership está visível
- [ ] Relações com equipa/domínio estão visíveis
- [ ] Navegação serviço -> contrato -> versão -> diff funciona
- [ ] Frontend é utilizável por dev e tech lead

### Evidências obrigatórias
- [ ] vídeo ou sequência de uso
- [ ] screenshots das telas finais
- [ ] lista de endpoints usados
- [ ] payloads principais validados
- [ ] teste mínimo executado

### Gaps encontrados
| Gap | Severidade | Owner | Ação |
|---|---|---|---|
| A preencher |  |  |  |

---

## Fluxo 2 — Change Confidence

### Objetivo
Permitir decisão com contexto antes de promover mudança.

### Critérios de aceite
- [ ] Change pode ser criada
- [ ] Change detail está funcional
- [ ] Evidências ficam visíveis
- [ ] Blast radius está disponível
- [ ] Advisory é clara
- [ ] Approval / Reject / Conditional Approval funcionam
- [ ] Histórico da decisão fica registrado
- [ ] Frontend dá contexto suficiente para decidir

### Evidências obrigatórias
- [ ] fluxo create -> review -> decision
- [ ] screenshots do detail e advisory
- [ ] endpoints validados
- [ ] teste mínimo do fluxo

### Gaps encontrados
| Gap | Severidade | Owner | Ação |
|---|---|---|---|
| A preencher |  |  |  |

---

## Fluxo 3 — Incident Correlation & Mitigation

### Objetivo
Ajudar troubleshooting e resposta operacional real.

### Critérios de aceite
- [ ] Incident list/detail funcionam
- [ ] Changes relacionadas aparecem
- [ ] Serviços/dependências relacionadas aparecem
- [ ] Runbooks estão acessíveis
- [ ] Mitigação guiada funciona
- [ ] Pós-validação existe
- [ ] Outcome é registrado

### Evidências obrigatórias
- [ ] demonstração do incident detail
- [ ] demonstração de correlação útil
- [ ] demonstração de mitigação e pós-validação
- [ ] endpoints validados
- [ ] teste mínimo do fluxo

### Gaps encontrados
| Gap | Severidade | Owner | Ação |
|---|---|---|---|
| A preencher |  |  |  |

---

## Fluxo 4 — AI Assistant grounded

### Objetivo
Fazer a IA ajudar em tarefas reais com grounding confiável.

### Critérios de aceite
- [ ] Assistant responde sobre contratos
- [ ] Assistant responde sobre changes
- [ ] Assistant responde sobre incidents
- [ ] Assistant responde sobre mitigação/runbooks
- [ ] Fontes/contexto usados ficam claros
- [ ] Resposta parece grounded, não genérica
- [ ] Restrições e governança da IA estão respeitadas

### Evidências obrigatórias
- [ ] exemplos de prompts reais
- [ ] exemplos de respostas com grounding
- [ ] validação em service/contract/change/incident detail
- [ ] teste mínimo do fluxo

### Gaps encontrados
| Gap | Severidade | Owner | Ação |
|---|---|---|---|
| A preencher |  |  |  |

---

## Ajustes transversais obrigatórios
| Item | Status | Observações |
|---|---|---|
| Loading states | A preencher |  |
| Empty states | A preencher |  |
| Error states | A preencher |  |
| Navegação entre entidades | A preencher |  |
| Consistência visual | A preencher |  |
| i18n nas telas críticas | A preencher |  |
| Testes E2E dos fluxos centrais | A preencher |  |
| Docs atualizadas | A preencher |  |

## Decisão de saída da Onda 1
| Critério | Status | Observações |
|---|---|---|
| 4 fluxos ponta a ponta funcionam | A preencher |  |
| Valor real demonstrado | A preencher |  |
| Sem gaps críticos bloqueadores | A preencher |  |
| Go para validação consolidada pós-PR-16 | A preencher |  |
