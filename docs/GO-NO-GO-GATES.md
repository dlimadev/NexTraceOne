# Go / No-Go Gates

## Objetivo
Definir critérios objetivos para decidir quando o NexTraceOne pode avançar para a próxima fase de evolução.

## Regra principal
Nenhuma nova fase deve começar apenas porque um prompt foi executado ou porque existe código compilando. Só avançar mediante **evidência funcional, consistência do produto e alinhamento com o núcleo do NexTraceOne**.

---

## Gate G1 — Saída da Onda 1
### Objetivo
Garantir que os fluxos centrais do produto estão realmente utilizáveis.

### Critérios obrigatórios
- [ ] Source of Truth / Contract Governance funciona ponta a ponta
- [ ] Change Confidence funciona ponta a ponta
- [ ] Incident Correlation & Mitigation funciona ponta a ponta
- [ ] AI Assistant grounded é útil em tarefas reais
- [ ] i18n nas telas críticas está funcional
- [ ] navegação entre entidades está coerente
- [ ] testes mínimos dos fluxos centrais existem
- [ ] docs principais refletem o estado real do código

### Decisão
- **GO** se todos os critérios críticos estiverem atendidos
- **NO-GO** se qualquer fluxo central ainda estiver parcial ou inconsistente

---

## Gate G2 — Entrada no Horizonte 1 pós-PR-16
### Objetivo
Permitir evolução pragmática de PR-17 reinterpretado, PR-18 reinterpretado, PR-19 e PR-20.

### Critérios obrigatórios
- [ ] O núcleo do produto já entrega valor claro
- [ ] connectors principais possuem health/freshness minimamente confiáveis
- [ ] scoping por equipa/domínio está utilizável
- [ ] governance packs do PR-16 não estão bloqueando o fluxo real do produto
- [ ] assistant grounded já tem utilidade prática demonstrada

### Decisão
- **GO** para PR-17 reinterpretado, PR-18 reinterpretado, PR-19 e PR-20
- **NO-GO** se a evolução estiver tentando compensar um núcleo ainda fraco

---

## Gate G3 — Entrada no Horizonte 2
### Objetivo
Permitir capacidades avançadas ligadas a autonomia controlada e simulação mais forte.

### Critérios obrigatórios
- [ ] dados históricos suficientes
- [ ] outcomes rastreáveis de incidentes, changes e mitigação
- [ ] qualidade de signals confiável
- [ ] packs, policies e guardrails funcionando de forma real
- [ ] assistant grounded e operational memory suficientemente maduros
- [ ] valor claro para autonomy/safety/governance adicional

### Decisão
- **GO** para PR-21 a PR-24 reinterpretados
- **NO-GO** se os dados ainda forem fracos ou o uso real insuficiente

---

## Gate G4 — Entrada no Horizonte 3
### Objetivo
Permitir capacidades avançadas como twin mais rico, predição e command center sofisticado.

### Critérios obrigatórios
- [ ] múltiplos fluxos centrais estáveis em produção/piloto real
- [ ] histórico suficiente para projeção e warning úteis
- [ ] command/advisory/governance já geram valor comprovado
- [ ] necessidade clara de síntese mais avançada

### Decisão
- **GO** para PR-25 a PR-28
- **NO-GO** se isso ainda for mais arquitetura do que valor real

---

## Gate G5 — Entrada no Horizonte 4
### Objetivo
Permitir camadas institucionais avançadas como control tower, strategy engine, decision memory graph, reasoning orchestrator e decision fabric.

### Critérios obrigatórios
- [ ] uso enterprise real por múltiplas equipas/domínios
- [ ] trilhas fortes de decisão, outcome e learning
- [ ] necessidade comprovada de governança institucional avançada
- [ ] maturidade suficiente para sustentar abstrações superiores

### Decisão
- **GO** para PR-29 a PR-34
- **NO-GO** se ainda houver gaps relevantes no núcleo ou pouca maturidade institucional

---

## Gate de qualidade transversal
Esses critérios valem para qualquer avanço.

### Qualidade de produto
- [ ] a feature melhora uma tarefa real
- [ ] a feature não aumenta abstração sem valor
- [ ] a UX está utilizável
- [ ] a feature é compreensível por pelo menos uma persona-alvo real

### Qualidade técnica
- [ ] boundaries respeitados
- [ ] API coerente
- [ ] frontend e backend alinhados
- [ ] i18n aplicada
- [ ] logs e exceptions corretos
- [ ] testes mínimos existentes

### Qualidade de alinhamento estratégico
- [ ] fortalece Change Confidence
- [ ] ou fortalece Source of Truth
- [ ] ou fortalece Incident/Mitigation
- [ ] ou fortalece AI grounded e útil

Se não fortalecer nenhum desses eixos, a prioridade cai.

---

## Template de decisão Go / No-Go

### Fase avaliada
`[Preencher]`

### Critérios atendidos
- [ ] Critério 1
- [ ] Critério 2
- [ ] Critério 3

### Bloqueios encontrados
- [ ] Bloqueio 1
- [ ] Bloqueio 2

### Riscos de avançar agora
- `[Preencher]`

### Decisão
- [ ] GO
- [ ] NO-GO

### Justificativa
`[Preencher]`

### Ações obrigatórias antes de reavaliar
1. `[Preencher]`
2. `[Preencher]`
3. `[Preencher]`
