# EXECUTION BASELINE — PR1 até PR16

## Objetivo
Este documento estabelece a baseline de execução do NexTraceOne para tudo o que foi implementado do **PR-1 ao PR-16**. O foco é criar uma visão honesta do estado atual do produto, identificar o que já entrega valor real, o que está incompleto e o que precisa ser corrigido antes de avançar para novas ondas de evolução.

## Princípios
- Não considerar um PR como “concluído” apenas porque existe código.
- Validar sempre por **fluxo funcional ponta a ponta**.
- Priorizar valor central do NexTraceOne:
  - Change Confidence
  - Source of Truth de serviços e contratos
  - Incident Correlation & Mitigation
  - AI grounded, útil e auditável
- Tratar arquitetura, frontend, i18n, observabilidade e segurança como parte da entrega.

## Escopo desta baseline
A baseline cobre:
- código backend
- frontend
- endpoints e contratos de API
- i18n
- jobs e processamento assíncrono
- documentação relevante
- testes mínimos
- capacidade real de uso por persona

## Classificação obrigatória
Cada item avaliado deve receber uma destas classificações:
- **KEEP** — está bom e deve ser preservado
- **REFACTOR** — está útil, mas precisa de correção estrutural
- **COMPLETE** — existe parcialmente e precisa ser concluído
- **REMOVE** — não entrega valor ou atrapalha o foco
- **CREATE** — ainda não existe e é necessário

## Avaliação por cluster

### Cluster A — Fundação e arquitetura
Cobrir:
- modularidade e boundaries
- convenções de código
- i18n
- segurança base
- observabilidade base
- readiness / health
- configuração por ambiente

#### Checklist
- [ ] Bounded contexts coerentes
- [ ] Separação Domain / Application / Infrastructure / API respeitada
- [ ] i18n aplicado nas telas críticas
- [ ] Logs em inglês e sem vazamento de dados sensíveis
- [ ] Exceptions técnicas em inglês
- [ ] Health/readiness existentes e úteis
- [ ] Configuração externa por ambiente revisada

#### Estado atual
| Item | Estado | Classificação | Observações |
|---|---|---|---|
| Arquitetura modular | A preencher |  |  |
| Convenções de código | A preencher |  |  |
| i18n base | A preencher |  |  |
| Segurança base | A preencher |  |  |
| Observabilidade base | A preencher |  |  |
| Health/Readiness | A preencher |  |  |

---

### Cluster B — Source of Truth e Contracts
Cobrir:
- catálogo de serviços
- ownership
- contratos REST / SOAP / Kafka / background services
- versionamento
- diff semântico
- compatibilidade
- busca e navegação
- detalhe de serviço e contrato

#### Checklist
- [ ] Serviço pode ser cadastrado/importado
- [ ] Contrato pode ser cadastrado/importado
- [ ] Histórico de versões está acessível
- [ ] Diff é utilizável
- [ ] Compatibilidade é compreensível
- [ ] Ownership por equipa/domínio está visível
- [ ] Busca e filtros são úteis
- [ ] Frontend suporta consulta real

#### Estado atual
| Item | Estado | Classificação | Observações |
|---|---|---|---|
| Catálogo de serviços | A preencher |  |  |
| Detalhe de serviço | A preencher |  |  |
| Catálogo de contratos | A preencher |  |  |
| Detalhe de contrato | A preencher |  |  |
| Versionamento | A preencher |  |  |
| Diff semântico | A preencher |  |  |
| Compatibilidade | A preencher |  |  |
| Ownership | A preencher |  |  |
| Busca | A preencher |  |  |

---

### Cluster C — Change Confidence
Cobrir:
- submissão de change
- evidence pack
- blast radius
- impacto em contratos/dependências
- advisory
- approval / reject / conditional approval
- rollout readiness
- trilha da decisão

#### Checklist
- [ ] Change pode ser criada
- [ ] Change pode ser listada e consultada
- [ ] Vínculo com serviço/contrato funciona
- [ ] Evidence readiness é visível
- [ ] Blast radius é útil
- [ ] Advisory é clara
- [ ] Aprovação é auditável
- [ ] Frontend suporta decisão real

#### Estado atual
| Item | Estado | Classificação | Observações |
|---|---|---|---|
| Create/List/Detail de change | A preencher |  |  |
| Evidence pack | A preencher |  |  |
| Blast radius | A preencher |  |  |
| Advisory | A preencher |  |  |
| Approval flow | A preencher |  |  |
| Rollout readiness | A preencher |  |  |
| Decision history | A preencher |  |  |

---

### Cluster D — Incident Correlation & Mitigation
Cobrir:
- lista e detalhe de incidentes
- correlação com changes
- correlação com serviços e dependências
- runbooks
- mitigação guiada
- validação pós-ação
- histórico do outcome

#### Checklist
- [ ] Incident list/detail utilizáveis
- [ ] Correlação com changes funciona
- [ ] Correlação com serviços/dependências funciona
- [ ] Runbooks estão acessíveis
- [ ] Mitigação guiada funciona
- [ ] Validação pós-ação existe
- [ ] Outcome fica registrado

#### Estado atual
| Item | Estado | Classificação | Observações |
|---|---|---|---|
| Incident list/detail | A preencher |  |  |
| Correlação com changes | A preencher |  |  |
| Correlação com serviços | A preencher |  |  |
| Runbooks | A preencher |  |  |
| Mitigação guiada | A preencher |  |  |
| Pós-validação | A preencher |  |  |
| Histórico | A preencher |  |  |

---

### Cluster E — Integrações, escopo e governança
Cobrir:
- Integration Hub
- conectores prioritários
- freshness e health
- multi-team / multi-domain governance
- governance packs do PR-16

#### Checklist
- [ ] Conectores principais estão estáveis
- [ ] Freshness é visível
- [ ] Health é visível
- [ ] Escopo por equipa/domínio funciona
- [ ] Governance packs influenciam comportamento real
- [ ] Admin views são úteis

#### Estado atual
| Item | Estado | Classificação | Observações |
|---|---|---|---|
| Integration Hub | A preencher |  |  |
| Connector health | A preencher |  |  |
| Freshness | A preencher |  |  |
| Scope por equipa/domínio | A preencher |  |  |
| Governance packs | A preencher |  |  |

---

### Cluster F — Analytics e hardening
Cobrir:
- product analytics do PR-14
- performance crítica
- jobs/background processing
- deployment/readiness
- logs e erros

#### Checklist
- [ ] Eventos analytics úteis
- [ ] Métricas de adoção relevantes
- [ ] Páginas críticas com performance aceitável
- [ ] Jobs estáveis
- [ ] Health/readiness válidos
- [ ] Logs úteis para operação

#### Estado atual
| Item | Estado | Classificação | Observações |
|---|---|---|---|
| Product analytics | A preencher |  |  |
| Performance crítica | A preencher |  |  |
| Jobs | A preencher |  |  |
| Deployment/readiness | A preencher |  |  |
| Logs/diagnóstico | A preencher |  |  |

## Riscos principais identificados
| Risco | Impacto | Probabilidade | Mitigação |
|---|---|---|---|
| Arquitetura rica, mas fluxo incompleto | Alto | Alta | Fechar fluxos ponta a ponta |
| UI com muito conceito e pouco uso real | Alto | Média | Validar por tarefa real |
| PR executado sem valor mensurável | Alto | Alta | Exigir evidência funcional |
| IA pouco grounded | Alto | Média | Validar fontes e utilidade real |

## Saídas obrigatórias desta baseline
- lista consolidada de gaps
- lista de remoções/simplificações necessárias
- ordem de correção por valor de produto
- critérios de aceite da Onda 1
- recomendação formal de Go / No-Go para evolução pós-PR-16
