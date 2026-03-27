# Auditoria — Catálogo e Gestão de Agentes de IA do NexTraceOne

> **Data:** 2025-07-15
> **Classificação:** FUNCIONAL_APARENTE — 10 agentes oficiais, criação pelo utilizador suportada, gestão completa.

---

## 1. Resumo

O NexTraceOne possui um catálogo robusto de agentes de IA com **10 agentes oficiais semeados** cobrindo 6 categorias de domínio. A entidade `AiAgent` é rica (20+ propriedades), suporta propriedade multi-nível (System/Tenant/User), visibilidade configurável e ciclo de publicação completo. O utilizador pode criar, editar e executar agentes via UI e API.

---

## 2. Agentes oficiais semeados

Todos os 10 agentes são **System/Tenant/Published** — criados pelo sistema, disponíveis ao tenant, publicados e activos.

### 2.1 Agentes de resposta a incidentes (IncidentResponse)

| # | Nome | Propósito |
|---|---|---|
| 1 | Service Health Analyzer | Analisa saúde de serviços e identifica degradações |
| 4 | Incident Root Cause Investigator | Investiga causa raiz de incidentes |

### 2.2 Agentes de análise de serviços (ServiceAnalysis)

| # | Nome | Propósito |
|---|---|---|
| 2 | SLA Compliance Checker | Verifica conformidade com SLAs |

### 2.3 Agentes de inteligência de mudanças (ChangeIntelligence)

| # | Nome | Propósito |
|---|---|---|
| 3 | Change Impact Evaluator | Avalia impacto de mudanças em produção |
| 6 | Release Risk Evaluator | Avalia risco de releases |

### 2.4 Agentes de auditoria de segurança (SecurityAudit)

| # | Nome | Propósito |
|---|---|---|
| 5 | Security Posture Assessor | Avalia postura de segurança |

### 2.5 Agentes de design de contratos (ApiDesign / EventDesign / SoapDesign)

| # | Nome | Categoria | Saída |
|---|---|---|---|
| 7 | API Contract Draft Generator | ApiDesign | OpenAPI 3.1 YAML |
| 9 | Kafka Schema Contract Designer | EventDesign | Avro / JSON schemas |
| 10 | SOAP Contract Author | SoapDesign | WSDL / SOAP |

### 2.6 Agentes de geração de testes (TestGeneration)

| # | Nome | Propósito |
|---|---|---|
| 8 | API Test Scenario Generator | Gera cenários de teste para APIs |

---

## 3. Cobertura por pilar do produto

| Pilar NexTraceOne | Agentes | Cobertura |
|---|---|---|
| Service Governance | Service Health Analyzer, SLA Compliance | ✅ |
| Contract Governance | API Contract Draft, Kafka Schema, SOAP Contract | ✅ Forte |
| Change Confidence | Change Impact Evaluator, Release Risk Evaluator | ✅ |
| Operational Reliability | Incident Root Cause Investigator | ✅ |
| AI-assisted Operations | Todos | ✅ |
| Security | Security Posture Assessor | ✅ |

---

## 4. Entidade AiAgent — estrutura completa

### Identificação

| Propriedade | Tipo | Propósito |
|---|---|---|
| Name | string | Nome interno |
| DisplayName | string | Nome de exibição |
| Slug | string | Identificador URL-safe |
| Description | string | Descrição do agente |
| Category | string | Categoria funcional |
| Icon | string | Ícone na UI |

### Configuração de IA

| Propriedade | Tipo | Propósito |
|---|---|---|
| SystemPrompt | string (até 10K) | Prompt de sistema detalhado |
| PreferredModelId | ref | Modelo preferido |
| AllowedModelIds | string[] | Modelos permitidos |
| AllowModelOverride | bool | Utilizador pode trocar modelo |
| AllowedTools | string[] | Ferramentas declaradas |
| InputSchema | JSON | Schema de entrada |
| OutputSchema | JSON | Schema de saída |
| Capabilities | string[] | Capacidades declaradas |

### Propriedade e visibilidade

| Propriedade | Valores | Propósito |
|---|---|---|
| OwnershipType | System / Tenant / User | Quem criou |
| Visibility | Private / Team / Tenant | Quem pode ver |
| TargetPersona | string | Persona-alvo |

### Ciclo de vida

| Propriedade | Valores | Propósito |
|---|---|---|
| PublicationStatus | Draft / PendingReview / Active / Published / Archived / Blocked | Estado de publicação |
| Version | int | Versão do agente |
| ExecutionCount | int | Contagem de execuções |

---

## 5. Prompts de sistema dos agentes oficiais

Os agentes oficiais possuem **prompts detalhados de 800+ caracteres** cada, com:

- Regras específicas de comportamento
- Formato de saída esperado
- Domínio e contexto do NexTraceOne
- Restrições de resposta
- Estrutura da análise

**Exemplo conceptual (API Contract Draft Generator):**
- Gera contratos OpenAPI 3.1 YAML
- Segue convenções REST
- Inclui schemas, exemplos, códigos de erro
- Respeita padrões de versionamento
- Gera documentação inline

---

## 6. Criação de agentes pelo utilizador

| Aspecto | Estado | Evidência |
|---|---|---|
| API de criação | ✅ Funcional | `createAgent` endpoint |
| UI de criação | ✅ Funcional | `AiAgentsPage.tsx` — formulário de criação |
| OwnershipType | ✅ User | Agentes criados têm ownership do utilizador |
| Visibilidade | ✅ Configurável | Private / Team / Tenant |
| Publicação | ✅ Ciclo completo | Draft → PendingReview → Active → Published |

---

## 7. Frontend — páginas de agentes

### AiAgentsPage.tsx (711 linhas)

| Funcionalidade | Estado |
|---|---|
| Catálogo de agentes | ✅ |
| Filtragem por categoria | ✅ |
| Criação de agente | ✅ |
| Execução de agente | ✅ |
| Revisão de artefactos | ✅ |

### AgentDetailPage.tsx (563 linhas)

| Funcionalidade | Estado |
|---|---|
| Visualização de detalhes | ✅ |
| Edição de agente | ✅ |
| Execução directa | ✅ |
| Histórico de execuções | ✅ |
| Artefactos gerados | ✅ |

---

## 8. Lacunas identificadas

| # | Lacuna | Severidade | Detalhe |
|---|---|---|---|
| 1 | AllowedTools não executados | Alta | Campo declarado mas tools não invocados em runtime |
| 2 | Sem marketplace de agentes | Média | Não existe partilha entre tenants |
| 3 | Sem versionamento semântico | Baixa | Campo Version é inteiro, sem major.minor.patch |
| 4 | Sem templates de agente | Baixa | Criação é livre sem templates pré-definidos |
| 5 | Sem importação/exportação | Baixa | Não é possível exportar/importar definições de agentes |

---

## 9. Recomendações

1. **Implementar execução de tools** — ligar `AllowedTools` à execução real para expandir capacidades dos agentes
2. **Adicionar templates de agente** — facilitar criação de agentes pelo utilizador com templates por categoria
3. **Versionamento semântico** — migrar Version de int para string semver
4. **Importação/exportação** — permitir exportar definições de agentes como JSON/YAML
5. **Adicionar mais agentes oficiais** — cobrir domínios como FinOps, compliance, performance

---

> **Veredicto:** O catálogo de agentes é **funcional e bem estruturado**, com boa cobertura dos pilares do produto, criação pelo utilizador suportada e gestão completa do ciclo de vida. A lacuna principal é a execução de tools não estar ligada.
