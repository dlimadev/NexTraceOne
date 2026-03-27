# Phase 4 — Contextual AI Agents Integration

## Visão Geral

A Fase 4 integra os Agents de IA ao contexto funcional do NexTraceOne. Os agents deixam de existir apenas como uma tela isolada e passam a ser utilizados diretamente nos módulos corretos da plataforma, gerando artefactos governados nos fluxos reais.

---

## Agents por Contexto de Módulo

### Módulo REST API (context: `rest-api`)
| Agent | Categoria | Artefacto Gerado |
|-------|-----------|-----------------|
| API Contract Author | ApiDesign | OpenApiDraft (YAML) |
| API Test Scenario Generator | TestGeneration | TestScenarios (JSON) |
| Contract Governance | ContractGovernance | OpenApiDraft (YAML) |

### Módulo SOAP (context: `soap`)
| Agent | Categoria | Artefacto Gerado |
|-------|-----------|-----------------|
| SOAP Contract Author | SoapDesign | SoapContractDraft (XML) |
| API Test Scenario Generator | TestGeneration | TestScenarios (JSON) |

### Módulo Kafka / Event Contracts (context: `kafka`)
| Agent | Categoria | Artefacto Gerado |
|-------|-----------|-----------------|
| Kafka Schema Contract Designer | EventDesign | KafkaSchema (JSON/Avro) |
| API Test Scenario Generator | TestGeneration | TestScenarios (JSON) |
| Contract Governance | ContractGovernance | OpenApiDraft (YAML) |

### Módulo de Testes (context: `testing`)
| Agent | Categoria | Artefacto Gerado |
|-------|-----------|-----------------|
| API Test Scenario Generator | TestGeneration | TestScenarios (JSON) |
| API Contract Author | ApiDesign | OpenApiDraft (YAML) |
| Kafka Schema Contract Designer | EventDesign | KafkaSchema (JSON) |

---

## Novo Agent: SoapContractAuthorAgent

**ID de seed:** `a1000000-0000-0000-0000-000000000010`  
**Categoria:** `SoapDesign`  
**Artefacto gerado:** `SoapContractDraft` (formato: XML)

### Input esperado
- `serviceName`: Nome do serviço SOAP
- `operations`: Lista de operações
- `businessRules`: Regras de negócio
- `payloads`: Payloads esperados
- `existingContracts`: Contratos existentes para referência

### Output esperado
1. Draft WSDL/SOAP em XML
2. Sumário JSON com:
   - `serviceName`
   - `namespace`
   - `operations[]` com input/output/fault types
   - `pendencies[]`
   - `validationChecklist[]`
3. Status inicial: **Draft** (pendente de revisão)

---

## Integração no Frontend — Workspace de Contratos

A secção **AI Agents** foi adicionada ao workspace de contratos (sidebar → grupo "AI").

### Derivação do contexto
O contexto de módulo é derivado automaticamente do protocolo e tipo de serviço do contrato:

| Protocolo / Tipo | Contexto |
|-----------------|---------|
| `Wsdl` / `Soap` | `soap` |
| `AsyncApi` / `KafkaProducer` / `KafkaConsumer` | `kafka` |
| `OpenApi` / `Swagger` / `RestApi` | `rest-api` |

### Fluxo de uso
1. Utilizador abre um contrato no workspace
2. Navega para a secção "AI Agents" no sidebar
3. A secção carrega automaticamente os agents recomendados para o contexto do contrato
4. Utilizador seleciona um agent e escreve a descrição do que precisa
5. O contexto do contrato (nome, protocolo, versão, descrição) é incluído automaticamente no input
6. Agent executa e retorna o output + artefactos gerados
7. Artefactos ficam com status `Pending` aguardando revisão governada

---

## Ciclo de Vida dos Artefactos

```
Draft → InReview → Approved
                → Rejected
         → Superseded (por versão mais recente)
```

Estados suportados em `ArtifactReviewStatus`:
- `Pending` — criado, aguarda revisão
- `Approved` — aprovado pelo revisor
- `Rejected` — rejeitado com notas
- `Superseded` — substituído por versão mais recente

Campos auditáveis:
- `reviewedBy` (userId)
- `reviewedAt` (DateTimeOffset)
- `reviewNotes` (texto livre)

---

## Endpoint API

### GET `/api/v1/ai/agents/by-context`

Retorna agents recomendados para um contexto de módulo específico.

**Parâmetros:**
- `context` (string, obrigatório): `rest-api`, `soap`, `kafka`, `testing`

**Permissão:** `ai:assistant:read`

**Resposta:**
```json
{
  "items": [
    {
      "agentId": "uuid",
      "name": "api-contract-author",
      "displayName": "API Contract Author",
      "category": "ApiDesign",
      "isOfficial": true,
      "isActive": true,
      "capabilities": "generation",
      "targetPersona": "Architect",
      "icon": "📐"
    }
  ],
  "moduleContext": "rest-api",
  "totalCount": 3
}
```

---

## Chat da IA — Agents Cadastrados

No chat da IA (`/ai/assistant`), os agents cadastrados são visíveis no painel lateral "Available Agents":

- Botão **Agents** no header com contador de agents disponíveis
- Painel lateral listando cada agent com:
  - Ícone, nome, badge "Official" (se aplicável)
  - Categoria
  - Descrição resumida
  - Persona alvo
  - Capacidades
- Quando usando um agent especializado, o chat exibe indicação clara de que está no modo "agent"

---

## Testes

### Backend (NexTraceOne.AIKnowledge.Tests)
- `ListAgentsByContextTests` — 12 testes cobrindo:
  - Mapeamento de contextos (rest-api, soap, kafka, testing)
  - Sensibilidade a maiúsculas/minúsculas
  - Contexto desconhecido retorna vazio sem chamar repositório
  - Mapeamento correto de campos do agent
  - Variantes de alias (rest/openapi/asyncapi/wsdl/event/test)

### Frontend (vitest)
- `AiAgentsSectionTests` — 11 testes cobrindo:
  - Estado de loading
  - Estado de erro
  - Estado vazio
  - Renderização de agents
  - Derivação de contexto por protocolo (OpenApi → rest-api, Wsdl → soap, AsyncApi → kafka)
  - Painel de execução ao selecionar agent
  - Botão desabilitado com input vazio
  - Renderização de múltiplos agents
  - Execução e exibição de resultado com artefactos

---

## Próximos Passos

1. **Artifact viewer** — UI dedicada para visualizar, comparar e exportar artefactos gerados
2. **Review workflow** — Fluxo de aprovação de artefactos integrado ao módulo de contratos
3. **Agent executions history** — Histórico de execuções por contrato no workspace
4. **ConsumerProducerCompatibilityAgent** — Agent para análise de compatibilidade Kafka
5. **BreakingChangeReviewAgent** — Agent para revisão de breaking changes em contratos
