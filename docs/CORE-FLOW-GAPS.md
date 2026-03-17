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
| Sem testes de integração com DbContext real (CatalogGraphDbContext, ContractsDbContext) | Alta | Criar testes com Testcontainers PostgreSQL |
| ContractDraft→ContractVersion publish flow não testado E2E | Média | Adicionar integration test |

### Gaps de API
| Gap | Prioridade | Ação |
|---|---|---|
| Developer Portal backend incompleto — endpoints existem mas handlers parciais | Média | Completar handlers restantes |
| Global search não testa cross-entity results | Baixa | Adicionar teste específico |

### Gaps de frontend
| Gap | Prioridade | Ação |
|---|---|---|
| Contract Studio UX precisa de polish | Média | Melhorar wizard flow |
| ServiceCatalogPage é a única página com loading states reais | Alta | Padronizar pattern em todas as pages |

### Gaps de i18n
| Gap | Prioridade | Ação |
|---|---|---|
| Nenhum gap crítico — 115+ keys completas em en.json e pt-PT.json | — | — |

### Gaps de testes
| Gap | Prioridade | Ação |
|---|---|---|
| 53 unit tests existem mas 0 integration tests com DB real | Alta | Adicionar Testcontainers para PostgreSQL |
| E2E placeholder apenas — sem teste real | Alta | Implementar E2E para fluxo import→version→diff |

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
| Nenhum gap de domínio — 11 entidades, 4 DbContexts, 21+ handlers reais | — | — |

### Gaps de API
| Gap | Prioridade | Ação |
|---|---|---|
| Nenhum gap crítico — 9 change endpoints + 5 workflow endpoints mapeados | — | — |

### Gaps de frontend
| Gap | Prioridade | Ação |
|---|---|---|
| Nenhum gap crítico — ChangeCatalogPage e ChangeDetailPage totalmente wired | — | — |

### Gaps de i18n
| Gap | Prioridade | Ação |
|---|---|---|
| Nenhum gap — i18n completo | — | — |

### Gaps de testes
| Gap | Prioridade | Ação |
|---|---|---|
| 195 unit tests passam; falta integration tests com DB real | Alta | Adicionar quando Testcontainers disponível |

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
| Correlação incident↔change baseada em seed data JSON, não em lógica dinâmica | Alta | Implementar engine de correlação baseada em timestamps/services |
| Sem handler para criar incidents via API (só seed) | Média | Adicionar CreateIncident handler |

### Gaps de API
| Gap | Prioridade | Ação |
|---|---|---|
| 17 endpoints funcionais; falta endpoint de criação de incident | Média | Adicionar POST /incidents |
| Sem endpoint para atualizar correlação dinamicamente | Alta | Adicionar PATCH /incidents/{id}/correlation |

### Gaps de frontend
| Gap | Prioridade | Ação |
|---|---|---|
| UI de criação de mitigation workflow não existe (API existe) | Média | Criar formulário de mitigation workflow |
| Sem formulário de criação de incident no frontend | Média | Adicionar CreateIncidentForm |

### Gaps de i18n
| Gap | Prioridade | Ação |
|---|---|---|
| Nenhum gap — i18n completo para incidents | — | — |

### Gaps de testes
| Gap | Prioridade | Ação |
|---|---|---|
| 266 backend tests passam; coverage boa | — | — |
| Falta testes E2E para fluxo incident→mitigation→validation | Média | Implementar E2E test |

### Estado desejado
- IA útil em contratos, changes, incidents e mitigation
- grounding claro
- fontes/contexto usados visíveis
- governança e restrições respeitadas
- respostas realmente úteis

### Gaps de domínio
| Gap | Prioridade | Ação |
|---|---|---|
| SendAssistantMessage retorna respostas hardcoded — sem integração com LLM real | Alta | Integrar provider externo (OpenAI/Azure) via IExternalAIRoutingPort |
| ExternalAI module tem 8 features com TODO — nenhum handler implementado | Alta | Implementar handlers prioritários |
| Token counts hardcoded a 0 — sem contagem real | Média | Implementar quando LLM integrado |

### Gaps de API / retrieval
| Gap | Prioridade | Ação |
|---|---|---|
| Context enrichment (POST /ai/context/enrich) existe mas sem retrieval real de dados | Alta | Conectar enrichment a queries reais dos módulos |
| Model selection retorna modelos fictícios (NexTrace-Internal-v1) | Alta | Conectar a model registry real |

### Gaps de frontend
| Gap | Prioridade | Ação |
|---|---|---|
| AiAssistantPage usa mockConversations hardcoded (100% mock) | Alta | Conectar a conversationsApi real |
| AssistantPanel tem fallback mock quando backend falha | Baixa | Aceitar — é graceful degradation |
| Model Registry, Token Budget, AI Policies — todos com dados mock | Média | Conectar a backend quando handlers existirem |

### Gaps de i18n
| Gap | Prioridade | Ação |
|---|---|---|
| 115+ keys de aiHub existem em en.json e pt-PT.json | — | Nenhum gap |

### Gaps de testes
| Gap | Prioridade | Ação |
|---|---|---|
| 101 tests de AIKnowledge passam; coverage no governance features | — | — |
| Sem testes para ExternalAI (handlers não implementados) | Alta | Adicionar quando handlers existirem |

---

## Gaps transversais
| Gap transversal | Impacto | Prioridade | Ação |
|---|---|---|---|
| Governance module 100% hardcoded — sem persistência real | Alto | Alta | Adicionar EF DbContext e migrar handlers de mock para real |
| AiAssistantPage 100% mock conversations | Alto | Alta | Conectar a conversationsApi do backend |
| Falta de testes E2E dos fluxos centrais | Alto | Alta | Implementar com Playwright para fluxos B, C, D |
| Correlação incident↔change é seed data, não dinâmica | Alto | Alta | Criar engine de correlação baseada em eventos |
| Integration Hub / Ingestion API é stub sem processamento | Médio | Média | Implementar processamento real de pelo menos 1 conector |
| Product Analytics 100% mock | Médio | Média | Criar event tracking real |
| 83% das páginas sem EmptyState pattern | Médio | Média | Aplicar StateDisplay component |
| 96% das páginas sem error states | Médio | Média | Adicionar error boundary por secção |
| Docs template (EXECUTION-BASELINE, CORE-FLOW-GAPS) estavam vazios | Médio | Alta | Preenchidos nesta validação |
| i18n completo nas áreas críticas | Baixo | Baixa | Nenhuma ação — já coberto |

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
