# NexTraceOne — Análise de Modelos de IA: Capacidade, Requisitos e Recomendações

> **Data:** Abril 2026  
> **Estado:** Documento de decisão técnica — IA interna e externa  
> **Referência:** `DefaultModelCatalog.cs`, `DefaultAgentCatalog.cs`, `AI-ARCHITECTURE.md`

---

## 1. Sumário Executivo

Este documento analisa qual modelo de IA o NexTraceOne deve usar como **modelo interno principal** para suportar todas as funcionalidades de IA já desenvolvidas e futuras, incluindo:

- 14 agentes especializados
- Geração e análise de contratos REST, SOAP, eventos e AsyncAPI
- Change intelligence e blast radius
- Incident investigation e root cause analysis
- Geração de código e scaffolding de serviços
- Security review automatizado
- Documentation quality e architecture fitness
- Tool calling autónomo para consulta de catálogo, topology e telemetria

O foco principal é um **modelo gratuito com licença que permita comercialização**.

---

## 2. O Que a IA do NexTraceOne Precisa Fazer

Baseado nos **14 agentes especializados** e nos use cases de IA implementados:

| Capacidade | Criticidade | Exemplos de Uso no NexTraceOne |
|-----------|-------------|-------------------------------|
| **Chat/Conversação** | 🔴 Crítica | AI Assistant, todos os agentes |
| **Raciocínio/Análise** | 🔴 Crítica | Change Advisor, Incident Responder, Security Reviewer |
| **Geração de Código** | 🟠 Alta | Contract Designer, Test Generator, Service Scaffold Agent |
| **Geração de Texto** | 🟠 Alta | Documentation Assistant, Release Notes, Runbooks |
| **Tool Calling / Function Calling** | 🔴 Crítica | Agentes que consultam catálogo, contratos, incidentes, topology |
| **Structured Output (JSON)** | 🔴 Crítica | Architecture Fitness Agent, Doc Quality Agent, scoring |
| **Context Window Grande** | 🟠 Alta | Análise de contratos longos, diff semântico, evidence pack |
| **Embeddings** | 🟡 Média | Pesquisa semântica, knowledge grounding, document retrieval |

### 2.1 Agentes que Exigem Capacidades Avançadas

| Agente | Capacidades Exigidas | Notas |
|--------|---------------------|-------|
| `service-analyst` | chat, analysis | Precisa consultar catálogo via tool calling |
| `contract-designer` | chat, generation, analysis | Gera OpenAPI/WSDL/AsyncAPI completos |
| `change-advisor` | chat, analysis | Avalia risco, blast radius, promotion readiness |
| `incident-responder` | chat, analysis | Correlaciona incidentes com mudanças e telemetria |
| `test-generator` | chat, generation | Gera Robot Framework drafts e test scenarios |
| `docs-assistant` | chat, generation | Cria runbooks e knowledge articles |
| `security-reviewer` | chat, analysis | Review OWASP, auth gaps, data exposure |
| `event-designer` | chat, generation, analysis | Kafka topics, AsyncAPI, event schemas |
| `service-scaffold-agent` | generation | Gera projetos completos em JSON estruturado |
| `dependency-advisor` | chat, analysis | CVEs, SBOM, license compliance |
| `architecture-fitness-agent` | analysis | Output JSON estruturado de fitness evaluation |
| `documentation-quality-agent` | analysis | Scoring JSON com dimensões de qualidade |
| `contract-pipeline-agent` | chat, generation | Server stubs, mock servers, Postman collections |

---

## 3. Estado Atual: Modelos Configurados

O `DefaultModelCatalog.cs` contém 7 modelos seed:

### 3.1 Modelos Internos (Ollama)

| Modelo | Parâmetros | Tipo | Default For | Capabilities | Context | GPU | Licença |
|--------|-----------|------|-------------|-------------|---------|-----|---------|
| `deepseek-r1:1.5b` | 1.5B | Chat | Reasoning | chat, reasoning | 32K | Não | MIT |
| `llama3.2:3b` | 3B | Chat | Chat | chat, code, general | 131K | Não | Llama 3.2 Community |
| `nomic-embed-text` | ~137M | Embedding | Embeddings | embeddings | 8K | Não | Apache 2.0 |
| `codellama:7b` | 7B | CodeGen | — | code, completion | 16K | Sim | Llama 2 Community |

### 3.2 Modelos Externos

| Modelo | Provider | Capabilities | Context | Licença |
|--------|----------|-------------|---------|---------|
| `gpt-4o` | OpenAI | chat, code, reasoning, vision | 128K | Proprietária |
| `gpt-4o-mini` | OpenAI | chat, code | 128K | Proprietária |
| `claude-3-5-sonnet` | Anthropic | chat, code, reasoning, analysis | 200K | Proprietária |

### 3.3 Diagnóstico dos Modelos Locais Atuais

| Modelo Atual | Problema |
|-------------|----------|
| `deepseek-r1:1.5b` | ❌ Muito pequeno para raciocínio complexo; tool calling inexistente |
| `llama3.2:3b` | ❌ Muito pequeno para análise profunda; geração de código limitada |
| `codellama:7b` | ⚠️ Apenas código, sem chat, sem tool calling, sem structured output |
| `nomic-embed-text` | ✅ Adequado para embeddings |

**Conclusão:** Os modelos locais atuais (1.5B e 3B) são placeholders de desenvolvimento. Para produção enterprise, precisamos de modelos significativamente maiores.

---

## 4. Recomendação Principal: Modelo Gratuito para Comercialização

### 4.1 🥇 Escolha Primária — Qwen 2.5 Coder 32B Instruct

| Aspecto | Detalhe |
|---------|--------|
| **Nome técnico** | `qwen2.5-coder:32b-instruct-q4_K_M` |
| **Parâmetros** | 32.5 bilhões |
| **Licença** | ✅ **Apache 2.0** — uso comercial livre, sem restrições |
| **Quantização** | Q4_K_M (melhor equilíbrio qualidade/tamanho) |
| **Tamanho em disco** | ~20 GB |
| **Context Window** | 131,072 tokens |
| **Tool Calling** | ✅ Sim (nativo) |
| **Structured Output** | ✅ Sim (JSON mode) |
| **Code Generation** | ✅ Excelente (benchmark SWE-bench top tier) |
| **Raciocínio** | ✅ Forte (comparável a GPT-4o em tarefas de código) |
| **Chat** | ✅ Sim |
| **Streaming** | ✅ Sim |
| **Multilingual** | ✅ Sim (inglês + português) |
| **Disponível no Ollama** | ✅ `ollama pull qwen2.5-coder:32b-instruct-q4_K_M` |

**Porquê este modelo:**

- Apache 2.0 = comercialização sem restrições, sem royalties, sem obrigações
- 32B parâmetros = qualidade enterprise para todas as funcionalidades do NexTraceOne
- Excelente em código E análise/raciocínio simultaneamente
- Tool calling nativo = agentes autónomos funcionais
- Context window de 131K = contratos longos, diffs semânticos, evidence packs completos
- Disponível via Ollama = integração zero-change com o NexTraceOne existente

### 4.2 🥈 Modelo Complementar para Chat/Análise — Qwen 2.5 72B Instruct

| Aspecto | Detalhe |
|---------|--------|
| **Nome técnico** | `qwen2.5:72b-instruct-q4_K_M` |
| **Parâmetros** | 72.7 bilhões |
| **Licença** | ✅ **Apache 2.0** |
| **Tamanho em disco** | ~42 GB |
| **Context Window** | 131,072 tokens |
| **Para quê** | Raciocínio complexo avançado, análise de risco, incident investigation profunda |

### 4.3 🥉 Modelo Leve para CPU-only — Qwen 2.5 Coder 14B

| Aspecto | Detalhe |
|---------|--------|
| **Nome técnico** | `qwen2.5-coder:14b-instruct-q4_K_M` |
| **Parâmetros** | 14 bilhões |
| **Licença** | ✅ **Apache 2.0** |
| **Tamanho em disco** | ~9 GB |
| **Para quê** | Clientes sem GPU, cenários de fallback, implantações menores |

### 4.4 Embeddings — Manter nomic-embed-text

| Aspecto | Detalhe |
|---------|--------|
| **Nome** | `nomic-embed-text` |
| **Licença** | ✅ **Apache 2.0** |
| **Tamanho** | ~274 MB |
| **Para quê** | Pesquisa semântica, knowledge grounding |

---

## 5. Requisitos Mínimos de Servidor

### 5.1 Cenário A: Produção Mínima Viável (Qwen 2.5 Coder 32B)

| Componente | Requisito Mínimo |
|-----------|-----------------|
| **GPU** | 1x NVIDIA RTX 4090 (24 GB VRAM) ou 1x A6000 (48 GB VRAM) |
| **VRAM** | 24 GB mínimo (Q4_K_M do 32B cabe em 24 GB com offloading parcial) |
| **RAM** | 64 GB DDR5 |
| **CPU** | 16 cores / 32 threads (AMD EPYC 7313 ou Intel Xeon Gold 5315Y) |
| **Storage** | 500 GB NVMe SSD (modelos + OS + dados) |
| **OS** | Ubuntu 22.04 LTS ou Windows Server 2022 |
| **Custo estimado** | ~$2,500–4,000 (servidor próprio) ou ~$1.50–2.50/hora (cloud) |

### 5.2 Cenário B: Produção Recomendada (32B + 72B + Embeddings)

| Componente | Requisito |
|-----------|----------|
| **GPU** | 2x NVIDIA A6000 (48 GB VRAM cada) ou 1x A100 (80 GB VRAM) |
| **VRAM Total** | 80–96 GB |
| **RAM** | 128 GB DDR5 ECC |
| **CPU** | 32 cores / 64 threads |
| **Storage** | 1 TB NVMe SSD |
| **OS** | Ubuntu 22.04 LTS |
| **Custo estimado** | ~$8,000–15,000 (servidor próprio) ou ~$4–8/hora (cloud) |

### 5.3 Cenário C: Produção Sem GPU (CPU-only, clientes menores)

| Componente | Requisito |
|-----------|----------|
| **GPU** | ❌ Nenhuma |
| **Modelo** | `qwen2.5-coder:14b-instruct-q4_K_M` (~9 GB) |
| **RAM** | 64 GB DDR5 |
| **CPU** | 32+ cores (AMD EPYC 7543 ou Intel Xeon Gold 6338) |
| **Storage** | 500 GB NVMe SSD |
| **Performance** | ~5–15 tokens/segundo (aceitável para uso enterprise não intensivo) |
| **Custo estimado** | ~$3,000–5,000 (servidor) |

---

## 6. Tabela Comparativa de Modelos Gratuitos

| Modelo | Params | Licença | Tool Calling | Code | Análise | Context | VRAM Mín. |
|--------|--------|---------|-------------|------|---------|---------|-----------|
| **🥇 Qwen 2.5 Coder 32B** | 32B | Apache 2.0 ✅ | ✅ | ⭐⭐⭐⭐⭐ | ⭐⭐⭐⭐ | 131K | 24 GB |
| **🥈 Qwen 2.5 72B** | 72B | Apache 2.0 ✅ | ✅ | ⭐⭐⭐⭐ | ⭐⭐⭐⭐⭐ | 131K | 48 GB |
| **🥉 Qwen 2.5 Coder 14B** | 14B | Apache 2.0 ✅ | ✅ | ⭐⭐⭐⭐ | ⭐⭐⭐ | 131K | 12 GB |
| DeepSeek-V3 | 685B MoE | MIT ✅ | ✅ | ⭐⭐⭐⭐⭐ | ⭐⭐⭐⭐⭐ | 128K | 320+ GB ❌ |
| Mistral Small 24B | 24B | Apache 2.0 ✅ | ✅ | ⭐⭐⭐ | ⭐⭐⭐⭐ | 32K | 16 GB |
| Gemma 2 27B | 27B | Gemma ⚠️ | ❌ | ⭐⭐⭐ | ⭐⭐⭐⭐ | 8K | 18 GB |
| Llama 3.1 70B | 70B | Llama ⚠️ | ✅ | ⭐⭐⭐⭐ | ⭐⭐⭐⭐⭐ | 128K | 48 GB |
| Phi-3 Medium 14B | 14B | MIT ✅ | ⚠️ Parcial | ⭐⭐⭐ | ⭐⭐⭐ | 128K | 10 GB |
| StarCoder2 15B | 15B | BigCode ORL ✅ | ❌ | ⭐⭐⭐⭐ | ⭐⭐ | 16K | 12 GB |

---

## 7. Modelos a Evitar para Comercialização

| Modelo | Motivo |
|--------|--------|
| **Llama 3.x** | Licença "Community" com restrição de 700M MAU e cláusula "Meta" de atribuição |
| **Gemma** | Licença Google com restrições de uso e redistribuição |
| **DeepSeek R1 (versão full)** | MIT mas requer 320+ GB VRAM — impraticável para self-hosted |
| **Claude / GPT** | Proprietários — apenas via API paga |

---

## 8. Recomendação de Modelo PAGO (opcional)

Para clientes que desejem complementar com modelos pagos (já suportados pelo NexTraceOne):

| Provedor | Modelo | Custo (input/output por 1M tokens) | Melhor Para |
|----------|--------|-------------------------------------|-------------|
| **OpenAI** | GPT-4o | $2.50 / $10.00 | Análise complexa, vision, tool calling |
| **OpenAI** | GPT-4o-mini | $0.15 / $0.60 | Chat rápido, custo-benefício |
| **Anthropic** | Claude 3.5 Sonnet | $3.00 / $15.00 | Código, raciocínio profundo, 200K context |
| **Anthropic** | Claude 3.5 Haiku | $0.25 / $1.25 | Chat rápido, custo baixo |
| **Google** | Gemini 2.0 Flash | $0.10 / $0.40 | Custo ultra-baixo, latência mínima |

**Nota:** O NexTraceOne já suporta providers OpenAI e tem placeholder para Anthropic. O routing inteligente redireciona queries conforme política de IA.

---

## 9. Arquitetura de Deploy de IA Recomendada

```
┌─────────────────────────────────────────────────────────────┐
│                    NexTraceOne Server                        │
│  ┌─────────────┐  ┌─────────────┐  ┌──────────────────────┐ │
│  │ API Host    │  │ Workers     │  │ Frontend             │ │
│  │ (.NET 10)   │  │ (Quartz)    │  │ (React/Vite)         │ │
│  └──────┬──────┘  └──────┬──────┘  └──────────────────────┘ │
│         │                │                                   │
│         ▼                ▼                                   │
│  ┌────────────────────────────────┐                         │
│  │ AI Runtime (AIKnowledge Module)│                         │
│  │ - Model Router                │                         │
│  │ - Token Quota Engine          │                         │
│  │ - Audit Logger                │                         │
│  └──────────┬────────────────────┘                         │
│             │                                               │
│     ┌───────┴──────────┐                                    │
│     ▼                  ▼                                    │
│ ┌──────────┐    ┌────────────┐                             │
│ │ Ollama   │    │ External   │ (opcional, governado)       │
│ │ (Local)  │    │ API Proxy  │                             │
│ └────┬─────┘    └────────────┘                             │
│      │                                                      │
│      ▼                                                      │
│ ┌──────────────────────────────────┐                       │
│ │ GPU Server / CPU Server          │                       │
│ │ ┌──────────────────────────────┐ │                       │
│ │ │ qwen2.5-coder:32b (Primary) │ │  ← Chat+Code+Tool    │
│ │ ├──────────────────────────────┤ │                       │
│ │ │ nomic-embed-text (Embeddings)│ │  ← Pesquisa semântica│
│ │ └──────────────────────────────┘ │                       │
│ └──────────────────────────────────┘                       │
└─────────────────────────────────────────────────────────────┘
```

---

## 10. Impacto no DefaultModelCatalog

Quando avançar com a decisão, será necessário atualizar `DefaultModelCatalog.cs`:

| Atual | Ação | Motivo |
|-------|------|--------|
| `deepseek-r1:1.5b` | Substituir por `qwen2.5-coder:32b` | Modelo principal de produção |
| `llama3.2:3b` | Manter como fallback leve CPU-only | Backup para clientes sem GPU |
| `codellama:7b` | Remover (Qwen 32B cobre code) | Consolidação |
| `nomic-embed-text` | ✅ Manter | Já é o melhor open-source para embeddings |

**Nenhuma outra alteração no código é necessária** — o Ollama provider já suporta qualquer modelo que corra no Ollama, e o routing/governance já está preparado.

---

## 11. Alternativa Cloud (pay-as-you-go)

Para quem não deseja investir em hardware:

| Provider | Instance | GPU | Custo/hora |
|----------|----------|-----|-----------|
| **RunPod** | 1x A6000 (48 GB) | A6000 | ~$0.79/hr |
| **Vast.ai** | 1x RTX 4090 | 4090 | ~$0.40–0.60/hr |
| **AWS** | g5.2xlarge (A10G 24GB) | A10G | ~$1.21/hr |
| **Azure** | NC24ads A100 v4 | A100 | ~$3.67/hr |
| **Lambda Labs** | 1x A6000 | A6000 | ~$0.80/hr |

---

## 12. Setup Recomendado Final

### Para Produção Enterprise (Self-hosted)

| Função | Modelo | Licença | VRAM | Comercial |
|--------|--------|---------|------|-----------|
| **Chat + Análise + Agentes + Tool Calling** | `qwen2.5-coder:32b-instruct-q4_K_M` | Apache 2.0 | 24 GB | ✅ Livre |
| **Embeddings / Pesquisa** | `nomic-embed-text` | Apache 2.0 | ~0.5 GB | ✅ Livre |
| **Fallback pago (opcional)** | GPT-4o-mini via API | Proprietária | N/A | ✅ Via API |

### Servidor Mínimo

| Componente | Especificação |
|-----------|---------------|
| **GPU** | 1x NVIDIA RTX 4090 (24 GB) — ~$1,600 |
| **CPU** | AMD Ryzen 9 7950X ou Intel Core i9-14900K |
| **RAM** | 64 GB DDR5 |
| **Storage** | 500 GB NVMe Gen4 |
| **OS** | Ubuntu 22.04 LTS |
| **Software** | Ollama + Docker |
| **Custo total estimado** | ~$3,500–5,000 (servidor completo) |

---

## 13. Justificativa: Porquê Qwen e Não Outro?

| Alternativa | Motivo de Descarte |
|------------|-------------------|
| **Llama 3.1/3.3** | Licença "Community" requer atribuição; cláusula de 700M MAU é risco jurídico |
| **Mistral Small/Large** | Context window limitado (32K); NexTraceOne precisa de 128K+ |
| **DeepSeek-V3** | MIT (excelente) mas requer 320+ GB VRAM; impraticável |
| **Phi-3/Phi-4** | MIT (excelente) mas tool calling limitado; qualidade inferior |
| **Gemma 2** | Licença restritiva; sem tool calling nativo |
| **StarCoder2** | Apenas código; sem chat/análise/raciocínio |

**Qwen 2.5 vence** em todos os critérios simultaneamente: Apache 2.0 + qualidade enterprise + tool calling + code + análise + context window grande + hardware acessível.

---

## 14. Próximos Passos

1. **Decisão:** Confirmar Qwen 2.5 Coder 32B como modelo principal
2. **Teste:** Instalar Ollama + modelo num ambiente de teste
3. **Validação:** Testar cada agente do NexTraceOne com o novo modelo
4. **Atualizar:** `DefaultModelCatalog.cs` com novo modelo de produção
5. **Documentar:** Requisitos de hardware no guia de instalação
6. **Pricing:** Definir tiers de licenciamento com base nos cenários A/B/C

---

> **Nota:** Este documento é um draft para decisão. O brainstorming de ideias inovadoras ainda não está fechado e será revisitado.
