# Relatório de Lacunas de Onboarding — NexTraceOne

> **Tipo:** Relatório de auditoria — perspetiva de programador júnior  
> **Escopo:** Experiência de onboarding completa (backend, frontend, infraestrutura, documentação)  
> **Data de referência:** Junho 2025  
> **Classificação:** Interno — Governança de Equipas e Onboarding

---

## 1. Resumo Executivo

| Métrica | Valor | Avaliação |
|---|---|---|
| Guia "Getting Started" centralizado | ❌ Inexistente | 🔴 Crítico |
| README raiz do repositório | ❌ Inexistente | 🔴 Crítico |
| READMEs nos módulos backend | 0/9 | 🔴 Crítico |
| Documentação de API (OpenAPI/Swagger) | ❌ Inexistente | 🔴 Crítico |
| Referência de schema de BD | ❌ Inexistente | 🟡 Importante |
| ADR (Architecture Decision Records) | ❌ Inexistente | 🟡 Importante |
| Guia de contribuição (CONTRIBUTING.md) | ❌ Inexistente | 🟡 Importante |
| Tempo estimado para produtividade (júnior) | 6–8 semanas | ⚠️ Elevado |
| Tempo estimado para produtividade (mid-level) | 3–4 semanas | ⚠️ Aceitável |
| Tempo estimado para produtividade (sénior) | 1–2 semanas | ✅ Bom |

O NexTraceOne possui uma codebase tecnicamente sólida com convenções excelentes, mas a **ausência quase total de documentação orientada ao onboarding** cria uma barreira significativa para novos membros da equipa, especialmente júniores.

---

## 2. Simulação: Dia 1 de um Programador Júnior

### 2.1 O que o júnior encontra

| Passo | Ação | Resultado | Sentimento |
|---|---|---|---|
| 1 | Clona o repositório | Vê `Directory.Build.props`, `docker-compose.yml`, `src/`, `docs/`, `tests/` | 😐 Ok, estrutura normal |
| 2 | Procura README raiz | ❌ Não existe | 😕 Confuso — por onde começo? |
| 3 | Abre `docs/` | 564 ficheiros .md | 😰 Overwhelmed — qual leio primeiro? |
| 4 | Tenta `docs/GUIDELINE.md` | 553 linhas de standards | 😐 Útil mas não diz como executar |
| 5 | Procura "getting started" | ❌ Não existe | 😟 Preciso de perguntar a alguém |
| 6 | Tenta executar o projeto | Sem instruções claras | 😤 Frustrado — trial and error |
| 7 | Explora `src/modules/` | 9 pastas sem README | 😕 O que faz cada módulo? |
| 8 | Abre uma página complexa | AiAssistantPage.tsx — 1.216 LOC, 19 useState | 😱 Impossível entender sozinho |
| 9 | Procura documentação de API | ❌ Não existe | 😤 Como integro frontend com backend? |
| 10 | Procura schema de BD | ❌ Não existe | 😤 Que tabelas existem? |

### 2.2 O que o júnior precisaria

| Necessidade | Existe? | Prioridade |
|---|---|---|
| README raiz com visão geral e instruções de execução | ❌ | 🔴 P0 |
| Guia "Getting Started" passo-a-passo | ❌ | 🔴 P0 |
| Mapa da codebase (o que está onde) | ❌ | 🔴 P0 |
| Explicação de cada módulo (README) | ❌ | 🔴 P0 |
| Documentação de API (endpoints, payloads) | ❌ | 🔴 P1 |
| Guia de contribuição | ❌ | 🟡 P2 |
| Schema de BD documentado | ❌ | 🟡 P2 |
| Glossário de termos do produto | ❌ | 🟡 P2 |

---

## 3. Obstáculos Identificados por Categoria

### 3.1 Documentação de navegação

| Obstáculo | Impacto | Complexidade de resolução |
|---|---|---|
| Sem README raiz | 🔴 Alto — nenhum ponto de entrada | 🟢 Baixa (2–4h) |
| 564 ficheiros em docs/ sem índice navegável | 🟡 Médio — information overload | 🟡 Média (4h) |
| Sem mapa da codebase | 🟡 Médio — não sabe o que está onde | 🟢 Baixa (2h) |
| Sem separação clara entre docs de auditoria e docs de orientação | 🟡 Médio | 🟡 Média (reorganização) |

### 3.2 Documentação técnica

| Obstáculo | Impacto | Complexidade de resolução |
|---|---|---|
| 9 módulos sem README | 🔴 Alto — não entende responsabilidades | 🟡 Média (9–18h) |
| Sem documentação de API | 🔴 Alto — integração impossível sem mentor | 🟡 Média (ativar Swagger + config) |
| Sem schema de BD | 🟡 Médio — não sabe que dados existem | 🟡 Média (gerar diagramas) |
| Frontend com 0.95% de comentários | 🟡 Médio — código difícil de ler | 🟡 Média (esforço contínuo) |

### 3.3 Documentação de processo

| Obstáculo | Impacto | Complexidade de resolução |
|---|---|---|
| Sem guia de contribuição | 🟡 Médio — não sabe processo de PR/review | 🟢 Baixa (2h) |
| Sem guia de i18n | 🟡 Médio — erros de localização | 🟢 Baixa (2h) |
| Sem ADRs | 🟢 Baixo — não entende decisões passadas | 🟡 Média (criação retroativa) |
| Sem guia de testes | 🟡 Médio — não sabe como testar | 🟢 Baixa (2h) |

---

## 4. Dificuldade de Onboarding por Módulo

### 4.1 Classificação de dificuldade

| Nível | Definição |
|---|---|
| 🟢 FÁCIL | Código auto-explicativo, padrões claros, poucas entidades |
| 🟡 MODERADO | Requer alguma familiaridade com DDD e padrões do projeto |
| 🔴 DIFÍCIL | Código complexo, múltiplas integrações, lógica densa |
| ⚫ MUITO DIFÍCIL | Requer conhecimento profundo do domínio e múltiplas tecnologias |

### 4.2 Avaliação por módulo

| Módulo | Dificuldade Backend | Dificuldade Frontend | Razões | O que ajudaria |
|---|---|---|---|---|
| **Foundation** | 🟡 Moderado | 🟡 Moderado | Identidade, multitenancy, RLS — conceitos não triviais | README + explicação de RLS |
| **Catalog** | 🟡 Moderado | 🟡 Moderado | Muitas entidades, relações complexas entre serviços | README + diagrama ER |
| **ContractGovernance** | 🔴 Difícil | 🔴 Difícil | Pillar central do produto; versionamento, diff, validação, workflows | README + guia de domínio + exemplos |
| **ChangeIntelligence** | 🔴 Difícil | 🔴 Difícil | Correlação de mudanças, blast radius, análise temporal | README + explicação de conceitos |
| **Operations** | 🟡 Moderado | 🟡 Moderado | Incidentes, runbooks, mitigação | README + glossário |
| **Observability** | 🟡 Moderado | 🟡 Moderado | Telemetria, métricas — conceitos especializados | ✅ Já tem boa doc em `docs/observability/` |
| **Notifications** | 🟢 Fácil | 🟢 Fácil | Domínio simples e bem delimitado | README básico suficiente |
| **Audit** | 🟢 Fácil | 🟢 Fácil | Registo de eventos — padrão bem conhecido | README básico suficiente |
| **AIAgents** | ⚫ Muito difícil | ⚫ Muito difícil | IA, model registry, tokens, orquestração, governança | README + guia de arquitetura de IA + exemplos |

### 4.3 Módulos onde o júnior pode começar

| Módulo | Razão |
|---|---|
| **Notifications** | Domínio simples, poucas entidades, fluxo linear |
| **Audit** | Padrão event sourcing/logging bem documentado na indústria |
| **Catalog** (leitura) | Muitas entidades mas CRUD familiar |

### 4.4 Módulos que o júnior deve evitar inicialmente

| Módulo | Razão |
|---|---|
| **AIAgents** | Requer conhecimento de IA, orquestração, governança de modelos |
| **ChangeIntelligence** | Conceitos especializados de change management e blast radius |
| **ContractGovernance** | Complexidade de versionamento, diff e workflow de aprovação |

---

## 5. O que o Júnior Pode vs Não Pode Entender

### 5.1 Entende sem ajuda

| Aspeto | Porquê |
|---|---|
| Estrutura de pastas DDD | Padrão bem documentado na indústria |
| Convenções de nomeação backend | 100% consistentes e previsíveis |
| Organização feature-based do frontend | Padrão React comum |
| CRUD simples | Fluxo Command → Handler → Entity → Repository é standard |
| Design system | README excelente em `src/frontend/src/shared/design-system/` |

### 5.2 Entende com esforço moderado

| Aspeto | O que dificulta | O que ajudaria |
|---|---|---|
| Multitenancy / RLS | Conceito avançado, implementação distribuída | Guia explicativo + diagrama |
| CQRS pattern | Separação Command/Query não é intuitiva para júniores | Documentação com exemplos |
| i18n no frontend | Sistema maduro mas sem documentação | Guia de i18n |
| Docker Compose setup | Múltiplos serviços, configuração específica | Guia de setup local |

### 5.3 Não entende sem mentoria

| Aspeto | Porquê | Solução |
|---|---|---|
| Blast radius / Change Intelligence | Conceito de domínio especializado | Documentação de domínio obrigatória |
| AI Agent orchestration | Arquitetura complexa, múltiplas integrações | Guia arquitetural + diagramas |
| Contract versioning / diff | Lógica de compatibilidade não trivial | Exemplos concretos + diagramas |
| Interceptors (RLS, audit, etc.) | Cross-cutting concerns avançados | Comentários no código (parcialmente feito) |
| 570 ficheiros de docs | Information overload, sem navegação | Índice + categorização |

---

## 6. Comparação com Benchmarks

### 6.1 Onboarding em projetos de referência

| Projeto/Framework | Tempo onboarding júnior | O que oferecem |
|---|---|---|
| Projeto open-source típico | 2–4 semanas | README, CONTRIBUTING, getting started, API docs |
| Enterprise monorepo maduro | 3–5 semanas | Onboarding guide, module docs, buddy system |
| NexTraceOne (atual) | **6–8 semanas** | 570 docs de auditoria, sem orientação prática |
| NexTraceOne (com melhorias) | **3–4 semanas** (estimativa) | Com README, getting started, module docs |

### 6.2 Documentação mínima para onboarding eficaz

| Artefacto | NexTraceOne tem? | Importância |
|---|---|---|
| README raiz | ❌ | Obrigatório |
| Getting started guide | ❌ | Obrigatório |
| Architecture overview | ⚠️ Parcial (src/frontend/ARCHITECTURE.md) | Obrigatório |
| Module documentation | ❌ | Obrigatório |
| API documentation | ❌ | Muito importante |
| Contributing guide | ❌ | Importante |
| Code style guide | ✅ (docs/GUIDELINE.md) | Importante |
| Database schema | ❌ | Importante |
| Glossary | ❌ | Útil |
| FAQ / Troubleshooting | ⚠️ Parcial (docs/observability/troubleshooting.md) | Útil |

---

## 7. Plano de Onboarding Proposto

### 7.1 Semana 1 — Orientação

| Dia | Atividade | Documentação necessária |
|---|---|---|
| 1 | Configuração do ambiente local | Getting Started guide |
| 1 | Visão geral do produto | README raiz |
| 2 | Arquitetura do backend | Module overview + architecture doc |
| 2 | Arquitetura do frontend | `src/frontend/ARCHITECTURE.md` ✅ |
| 3 | Primeiro módulo simples (Notifications ou Audit) | Module README |
| 4 | Fazer uma tarefa guiada (bug fix simples) | Contributing guide |
| 5 | Review da semana + dúvidas | — |

### 7.2 Semana 2 — Aprofundamento

| Dia | Atividade | Documentação necessária |
|---|---|---|
| 1–2 | Entender Catalog (entidades, relações) | Module README + schema |
| 3–4 | Entender um fluxo end-to-end | API docs + cross-layer guide |
| 5 | Fazer uma tarefa real (feature pequena) | Contributing guide |

### 7.3 Semanas 3–4 — Autonomia crescente

| Atividade | Documentação necessária |
|---|---|
| Trabalhar em módulos moderados (Operations, Foundation) | Module READMEs |
| Entender i18n e design system | i18n guide + design system README ✅ |
| Primeiro PR com review completo | Contributing guide |

### 7.4 Semanas 5–8 — Produtividade

| Atividade | Notas |
|---|---|
| Trabalhar em módulos complexos com supervisão | ContractGovernance, ChangeIntelligence |
| Contribuir para documentação | Documentar o que aprendeu |
| Autonomia em tarefas do sprint | — |

---

## 8. Recomendações Priorizadas

### Fase 1 — Impacto máximo, esforço mínimo (Semana 1)

| # | Ação | Esforço | Impacto no onboarding |
|---|---|---|---|
| 1 | Criar README.md raiz | 2–4h | 🔴 Máximo — primeiro contacto |
| 2 | Criar guia "Getting Started" | 4–8h | 🔴 Máximo — setup e primeiros passos |
| 3 | Criar mapa da codebase (what's where) | 2h | 🔴 Alto — orientação espacial |

### Fase 2 — Construir base documental (Sprint 2)

| # | Ação | Esforço | Impacto |
|---|---|---|---|
| 4 | Criar README para os 9 módulos | 9–18h | 🔴 Alto |
| 5 | Criar CONTRIBUTING.md | 2h | 🟡 Médio |
| 6 | Ativar Swagger para documentação de API | 2–4h | 🟡 Médio |
| 7 | Criar guia de i18n | 2h | 🟡 Médio |

### Fase 3 — Completar ecossistema documental (1–2 meses)

| # | Ação | Esforço | Impacto |
|---|---|---|---|
| 8 | Criar ADRs retroativos para decisões-chave | 8–16h | 🟡 Médio |
| 9 | Gerar diagramas de schema de BD | 4–8h | 🟡 Médio |
| 10 | Criar glossário de termos do produto | 4h | 🟢 Baixo |

### Redução estimada no tempo de onboarding

| Fase | Tempo onboarding júnior |
|---|---|
| Atual | 6–8 semanas |
| Após Fase 1 | 5–6 semanas |
| Após Fase 2 | 4–5 semanas |
| Após Fase 3 | 3–4 semanas |

---

## 9. Métricas de Sucesso

| Métrica | Atual | Objetivo |
|---|---|---|
| Tempo onboarding júnior | 6–8 semanas | 3–4 semanas |
| Docs orientação vs docs auditoria | ~5% / ~95% | ~30% / ~70% |
| Módulos com README | 0/9 | 9/9 |
| Perguntas recorrentes em onboarding | Não medido | Reduzir 50% |
| Satisfação novos membros (survey) | Não medido | ≥ 7/10 |

---

> **Nota:** Este relatório complementa o relatório principal `documentation-and-onboarding-audit.md`.
