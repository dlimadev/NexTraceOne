# Core Flow Gaps

## Objetivo
Consolidar os gaps dos fluxos centrais do NexTraceOne, separando claramente o que falta no **núcleo do produto** do que pode ser adiado para ondas futuras.

## Regra principal
Só entra neste documento o que impacta diretamente pelo menos um destes resultados:
- reduzir risco de changes em produção
- melhorar source of truth de serviços e contratos
- reduzir tempo de diagnóstico e mitigação
- tornar a IA mais útil, grounded e auditável
- aumentar consistência operacional por equipa/domínio

---

## Fluxo 1 — Source of Truth / Contract Governance

### Estado desejado
- catálogo de serviços utilizável
- contratos REST/SOAP/Kafka/background services consultáveis
- versionamento e diff reais
- ownership claro
- dependências visíveis
- busca confiável

### Gaps de domínio
| Gap | Prioridade | Ação |
|---|---|---|
| A preencher | Alta |  |

### Gaps de API
| Gap | Prioridade | Ação |
|---|---|---|
| A preencher | Alta |  |

### Gaps de frontend
| Gap | Prioridade | Ação |
|---|---|---|
| A preencher | Alta |  |

### Gaps de i18n
| Gap | Prioridade | Ação |
|---|---|---|
| A preencher | Média |  |

### Gaps de testes
| Gap | Prioridade | Ação |
|---|---|---|
| A preencher | Alta |  |

---

## Fluxo 2 — Change Confidence

### Estado desejado
- create/list/detail de changes
- evidence pack útil
- blast radius útil
- advisory clara
- approval/reject/conditional approval
- decision trail

### Gaps de domínio
| Gap | Prioridade | Ação |
|---|---|---|
| A preencher | Alta |  |

### Gaps de API
| Gap | Prioridade | Ação |
|---|---|---|
| A preencher | Alta |  |

### Gaps de frontend
| Gap | Prioridade | Ação |
|---|---|---|
| A preencher | Alta |  |

### Gaps de i18n
| Gap | Prioridade | Ação |
|---|---|---|
| A preencher | Média |  |

### Gaps de testes
| Gap | Prioridade | Ação |
|---|---|---|
| A preencher | Alta |  |

---

## Fluxo 3 — Incident Correlation & Mitigation

### Estado desejado
- incident detail útil
- correlação com changes/serviços/dependências
- runbooks
- mitigação guiada
- validação pós-ação
- outcome registrado

### Gaps de domínio
| Gap | Prioridade | Ação |
|---|---|---|
| A preencher | Alta |  |

### Gaps de API
| Gap | Prioridade | Ação |
|---|---|---|
| A preencher | Alta |  |

### Gaps de frontend
| Gap | Prioridade | Ação |
|---|---|---|
| A preencher | Alta |  |

### Gaps de i18n
| Gap | Prioridade | Ação |
|---|---|---|
| A preencher | Média |  |

### Gaps de testes
| Gap | Prioridade | Ação |
|---|---|---|
| A preencher | Alta |  |

---

## Fluxo 4 — AI Assistant grounded

### Estado desejado
- IA útil em contratos, changes, incidents e mitigation
- grounding claro
- fontes/contexto usados visíveis
- governança e restrições respeitadas
- respostas realmente úteis

### Gaps de domínio
| Gap | Prioridade | Ação |
|---|---|---|
| A preencher | Alta |  |

### Gaps de API / retrieval
| Gap | Prioridade | Ação |
|---|---|---|
| A preencher | Alta |  |

### Gaps de frontend
| Gap | Prioridade | Ação |
|---|---|---|
| A preencher | Alta |  |

### Gaps de i18n
| Gap | Prioridade | Ação |
|---|---|---|
| A preencher | Média |  |

### Gaps de testes
| Gap | Prioridade | Ação |
|---|---|---|
| A preencher | Alta |  |

---

## Gaps transversais
| Gap transversal | Impacto | Prioridade | Ação |
|---|---|---|---|
| Navegação inconsistente entre entidades | Alto | Alta |  |
| UI rica demais e pouco objetiva | Alto | Alta |  |
| Duplicação de modelos/DTOs | Médio | Média |  |
| Falta de testes E2E dos fluxos centrais | Alto | Alta |  |
| Docs desatualizadas em relação ao código | Médio | Alta |  |
| i18n incompleto | Médio | Média |  |

## Itens explicitamente fora do foco imediato
Estes itens não devem consumir prioridade alta enquanto houver gaps no núcleo:
- abstrações institucionais muito avançadas
- camadas meta de reasoning/fabric além do necessário ao núcleo
- projeção/foresight sem base operacional confiável
- visualizações sofisticadas sem ganho claro de fluxo

## Prioridades operacionais
### Alta prioridade
- tudo que bloqueia uso real do núcleo

### Média prioridade
- tudo que melhora governança, integração e adoção do núcleo

### Baixa prioridade
- tudo que amplia abstração sem valor diário imediato
