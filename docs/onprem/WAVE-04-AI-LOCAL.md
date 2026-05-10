# Wave 4 — IA Local & Gestão de Modelos LLM

> **Prioridade:** Alta
> **Esforço estimado:** M (Medium)
> **Módulos impactados:** `aiknowledge`, `configuration`, frontend `ai-hub`
> **Referência:** [INDEX.md](./INDEX.md)
> **Estado (Maio 2026):** W4-01 IMPLEMENTADO | W4-02 IMPLEMENTADO | W4-03 PARCIAL | W4-04 NAO IMPLEMENTADO | W4-05 NAO IMPLEMENTADO

---

## Contexto

O NexTraceOne já integra Ollama para LLM local. O Model Registry existe no backend.
O que falta é a **camada de gestão e governança** que permite a uma equipa de infra
operar a IA sem SSH, sem linha de comando e sem conhecimento de LLMs.

Benchmark de mercado (2026):
- Modelos 7B em Q4_K_M precisam de 4-6 GB RAM e correm em CPU a 15-20 tokens/s
- Modelos 13B precisam de 8-10 GB RAM; requerem pelo menos 32 GB RAM no servidor
- NVMe SSD é obrigatório — ficheiros de modelo excedem frequentemente 4 GB
- vLLM e Ollama são os standards de facto para inference self-hosted
- AI governance reduz riscos de alucinação em ~40% (benchmarks 2026)

---

## W4-01 — Model Manager UI

### Problema
Gerir modelos Ollama requer acesso SSH ao servidor e conhecimento de linha de comando.
A equipa de infra não tem como adicionar, remover ou monitorizar modelos via UI.

### Solução
Página `/admin/ai/models` com:

```
┌────────────────────────────────────────────────────────────────┐
│                    AI Model Manager                            │
├──────────────────┬───────────┬────────────┬────────────────────┤
│  Modelo          │  Tamanho  │  RAM req.  │  Estado            │
├──────────────────┼───────────┼────────────┼────────────────────┤
│  deepseek-r1:1.5b│  1.1 GB   │  2.0 GB    │  ✅ Activo (padrão)│
│  llama3.2:3b     │  2.0 GB   │  3.5 GB    │  ✅ Instalado      │
│  qwen2.5:7b      │  4.7 GB   │  7.0 GB    │  ⬇ A descarregar  │
│                  │           │            │  [████░░░░] 62%    │
└──────────────────┴───────────┴────────────┴────────────────────┘

[ + Instalar modelo ]  [ ⬆ Importar ficheiro ]  [ Hardware Advisor ]
```

**Funcionalidades:**
- Listar modelos instalados com tamanho, RAM estimada e estado
- Instalar modelo por nome (`qwen2.5:7b`) com progress bar em tempo real
- Importar modelo a partir de ficheiro local (GGUF/safetensors) — **sem internet**
- Remover modelo não utilizado
- Definir modelo padrão por contexto (AI Assistant, Agentes, etc.)
- Testar modelo com prompt simples e medir latência

### Estado de Implementação (Maio 2026): IMPLEMENTADO
Page `AiModelManagerPage.tsx` em `src/frontend/src/features/platform-admin/pages/`.
Rota `/admin/ai/models` com sidebar link presente. Download com progress bar via SSE,
importação de ficheiro local e gestão de modelos implementados.

### Critério de aceite
- [x] Download de modelo com progress bar via SSE
- [x] Importação de ficheiro local funcional
- [x] Confirmação antes de remover modelo em uso
- [x] Disponível apenas para `PlatformAdmin`
- [x] i18n completo

---

## W4-02 — LLM Hardware Advisor

### Problema
Administradores não sabem se o servidor tem hardware adequado para o modelo
que querem instalar. Podem instalar um modelo que não consegue correr.

### Solução
Endpoint `GET /api/v1/admin/ai/hardware-assessment` + widget na UI:

```
┌─────────────────────────────────────────────────────┐
│              Hardware Assessment                    │
├─────────────────────────────────────────────────────┤
│  CPU: Intel Xeon E5-2680 v4 (28 cores)              │
│  RAM total: 64 GB | RAM disponível: 42 GB           │
│  GPU: Não detectada                                 │
│  Disco (modelo path): 380 GB livres                 │
├─────────────────────────────────────────────────────┤
│  Recomendações para este hardware:                  │
│                                                     │
│  ✅  deepseek-r1:1.5b  (1.1 GB)  ~20 tok/s  CPU    │
│  ✅  llama3.2:3b       (2.0 GB)  ~15 tok/s  CPU    │
│  ✅  qwen2.5:7b        (4.7 GB)  ~8 tok/s   CPU    │
│  ⚠️  llama3.1:13b      (8.0 GB)  ~4 tok/s   CPU    │
│       → Funciona mas será lento para uso concurrent │
│  ❌  llama3.1:70b      (40 GB)   RAM insuficiente   │
└─────────────────────────────────────────────────────┘
```

**Regras de estimativa (baseadas em benchmarks 2026):**
- RAM estimada ≈ `parâmetros_B × 0.6 GB` em Q4_K_M
- Velocidade CPU ≈ `15 / (parâmetros_B / 7)` tokens/s (linear aproximado)
- GPU requer VRAM ≥ RAM estimada do modelo

### Estado de Implementação (Maio 2026): IMPLEMENTADO
`HardwareAssessmentService` em `src/platform/NexTraceOne.ApiHost/OnPrem/HardwareAssessmentService.cs`.
Endpoint `GET /ai/hardware-assessment`. Feature `GetHardwareAssessment` com detecção de CPU, RAM, disco e GPU.
Lista de modelos compatíveis com estimativas de performance.

### Critério de aceite
- [x] Detecção automática de CPU, RAM total/disponível e GPU (se existir)
- [x] Lista de modelos compatíveis ordenada por desempenho esperado
- [x] Aviso claro para modelos que requerem GPU quando GPU não existe
- [x] Actualiza em tempo real (RAM disponível muda com carga)

---

## W4-03 — AI Resource Governor

### Problema
Em servidores partilhados, o Ollama pode monopolizar CPU e RAM durante inference,
degradando a performance de toda a plataforma.

### Solução
Camada de controlo entre o NexTraceOne e o Ollama:

```
Configuração de AI Resource Governor
├── Max concorrência de requests: 3 (configurável)
├── Timeout de inference: 120s (configurável)
├── Prioridade: Normal (Background jobs < Interactive < Admin)
└── Circuit Breaker: suspender AI se error rate > 50% em 5min
```

**Implementação:**
- `SemaphoreSlim` no `IChatCompletionProvider` para limitar concorrência
- Queue com prioridade (Interactive > Background)
- Circuit breaker com reset automático após 2 minutos
- Métricas de AI no health dashboard (queue depth, latência p95, error rate)

### Estado de Implementação (Maio 2026): PARCIAL
Page UI `AiResourceGovernorPage.tsx` implementada com configuração de `maxConcurrency`, timeouts,
circuit breaker e priority queue. API `getAiGovernorStatus` e `updateAiGovernorConfig` presentes.
O `SemaphoreSlim` no backend `IChatCompletionProvider` não foi encontrado — a limitação de concorrência
é configurável via UI mas o enforcement no provider pode estar incompleto.

### Critério de aceite
- [x] Limite de concorrência configurável via UI admin
- [x] Requests em fila com timeout configurável
- [x] Circuit breaker automático com notificação ao admin
- [ ] Métricas de AI visíveis no Health Dashboard (W2-01) — enforcement no provider a verificar

---

## W4-04 — AI Governance com Avaliação de Qualidade

### Problema
Em LLMs locais, a taxa de alucinação pode ser de 15-52% dependendo do modelo.
O NexTraceOne não tem mecanismo para detectar e mitigar respostas incorrectas.

### Solução
Camada de avaliação pós-inference:

```
Para cada resposta de AI:
1. Confidence Score: quão confiante o modelo parece (heurística baseada em tokens)
2. Grounding Check: verificar se factos citados existem nos dados do sistema
3. Hallucination Flag: sinalizar respostas que citam serviços/contratos inexistentes
4. Feedback Loop: utilizador pode marcar resposta como incorrecta
```

**Implementação sugerida:**
- `IAiResponseEvaluator` — interface para avaliadores plugáveis
- `GroundingEvaluator` — verifica se entidades citadas existem no catalog/changes
- `FeedbackStore` — persiste avaliações dos utilizadores para melhoria contínua
- Dashboard de qualidade AI: accuracy rate, feedback negativo por modelo, top hallucinations

### Estado de Implementação (Maio 2026): NAO IMPLEMENTADO
`IAiResponseEvaluator`, `GroundingEvaluator` e `FeedbackStore` não encontrados no codebase.
O grounding cross-módulo existe (4 readers), mas a camada de avaliação pós-inference com
confidence score, hallucination detection e feedback loop não está implementada.

### Critério de aceite
- [ ] Grounding check contra entidades do catalog e mudanças
- [ ] Flag visual na UI quando resposta tem baixa confiança
- [ ] Utilizador pode marcar resposta como incorrecta com 1 clique
- [ ] Dashboard de qualidade por modelo no Model Registry
- [ ] Métricas auditadas por modelo, utilizador e contexto

---

## W4-05 — Offline Model Bundle

### Problema
Instalar modelos via `ollama pull` requer internet. Em ambiente air-gapped,
não é possível actualizar ou instalar novos modelos.

### Solução
Suporte a importação de modelos via ficheiro local:

```
Fontes suportadas:
├── Ficheiro GGUF local (caminho no servidor)
├── Volume montado / NFS share
├── Bundle pré-embalado do NexTraceOne (modelos curados + testados)
└── Manifesto de modelos aprovados (lista de modelos verificados)
```

**Bundle de modelos curados NexTraceOne:**
```
nextraceone-models-bundle-v1.zip
├── deepseek-r1-1.5b-q4_k_m.gguf     (1.1 GB) — padrão recomendado
├── llama3.2-3b-q4_k_m.gguf          (2.0 GB) — para contextos maiores
├── nomic-embed-text-v1.5.gguf        (0.3 GB) — embeddings para search
└── models-manifest.json              — checksums e metadata
```

### Estado de Implementação (Maio 2026): NAO IMPLEMENTADO
Não existe bundle de modelos curados nem manifesto de modelos aprovados. O import de ficheiro GGUF
local via UI está disponível no `AiModelManagerPage.tsx` mas o bundle pré-empacotado e a validação
de checksum automatizada não estão implementados. Item pendente para iteração futura.

### Critério de aceite
- [ ] Import de GGUF local via caminho no servidor
- [ ] Import via upload de ficheiro (< 10 GB) com progress
- [ ] Validação de checksum SHA-256 antes de registar modelo
- [ ] Manifesto de modelos aprovados configurável por admin

---

## Referências de Mercado

- Ollama: standard de facto para LLM local em 2026
- vLLM: inference engine enterprise para GPU com multi-user support
- Langfuse (self-hosted): observabilidade de LLM com feedback loop
- HalluLens (2026): benchmark de alucinação para avaliação de modelos
- Tabnine: air-gapped AI com zero telemetria como referência de produto
