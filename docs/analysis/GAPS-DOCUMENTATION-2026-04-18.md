# NexTraceOne — Gaps e Problemas: Documentação
**Data:** 2026-04-18  
**Modo:** Analysis realista — sem minimizar problemas  
**Referência:** [STATE-OF-PRODUCT-2026-04-18.md](./STATE-OF-PRODUCT-2026-04-18.md)

---

## 1. Resumo

O NexTraceOne tem **97 ficheiros de documentação** — um investimento sério que poucos produtos em fase equivalente podem apresentar. A cobertura é ampla: arquitectura, segurança, deployment, runbooks, ADRs, guias de utilizador, personas.

**O problema não é a quantidade. É o desalinhamento entre documentação e código real.**

Parte da documentação descreve um produto ideal que ainda não existe. Outra parte foi escrita antes de decisões técnicas que a tornaram desactualizada. E há áreas críticas do produto sem documentação adequada.

**Veredicto:** Documentação de intenção ≠ documentação de estado real.

---

## 2. Documentação que está bem

Para contexto justo:

| Documento | Qualidade | Nota |
|-----------|-----------|------|
| CLAUDE.md | Excelente | Guia de produto e arquitectura extenso e coerente |
| AUDIT-MASTER-2026-04-10.md | Excelente | Auditoria honesta com problemas identificados |
| docs/adr/*.md | Muito boa | 6 ADRs que documentam decisões reais |
| docs/runbooks/*.md | Boa | Runbooks operacionais coerentes |
| docs/security/*.md | Boa | Guias de segurança detalhados |
| docs/onprem/*.md | Boa | Guias de instalação on-premises por waves |
| LOCAL-SETUP.md | Boa | Setup de desenvolvimento funcional |
| ENVIRONMENT-VARIABLES.md | Boa | Referência completa de variáveis |

---

## 3. Problemas de Alta Prioridade

### [D-01] IMPLEMENTATION-STATUS.md desalinhado com estado real

**Problema:** O `IMPLEMENTATION-STATUS.md` reporta a maioria dos módulos como "READY". Mas a análise actual mostra:

- Identity Access: "READY" → **na realidade PARCIAL** (6 endpoints críticos em falta)
- Change Governance: "READY" → **na realidade BLOQUEADO** (colisão de tabela C-01)
- OnCall Intelligence: "READY" → **na realidade DADOS FALSOS** (pseudo-random)
- Export feature: implicitamente "READY" → **na realidade STUB**

**Impacto:** Qualquer pessoa que leia este documento para avaliar o estado do produto fica com uma imagem incorrecta. Pode levar a decisões de produção baseadas em informação falsa.

**Remediação:** Actualizar `IMPLEMENTATION-STATUS.md` com os estados reais, usando a legenda existente correctamente:
- `PARTIAL` para módulos com implementação incompleta
- `INCOMPLETE` para features stub
- Notas explícitas sobre problemas críticos activos

---

### [D-02] MODULES-AND-PAGES.md não reflecte páginas com problemas

**Problema:** O inventário de páginas lista todas as 113 rotas mas não indica:
- Quais páginas têm problemas de UX críticos (GUID inputs)
- Quais páginas dependem de endpoints inexistentes
- Quais páginas são placeholders sem implementação real

**Impacto:** Um developer novo ao projecto não consegue saber rapidamente o que está funcional versus o que está quebrado.

---

### [D-03] ADR-003 desactualizado — Elasticsearch vs ClickHouse

**Ficheiro:** `docs/adr/003-elasticsearch-observability.md`  
**Problema:** Este ADR regista a decisão de usar Elasticsearch para observabilidade. Mas o CLAUDE.md (§10.3) e as conversas do projecto indicam que a **direcção evoluiu para ClickHouse** como base principal para workloads analíticos.

A mudança de direcção não está documentada como:
- Revisão ao ADR-003
- Novo ADR-007 que revê a decisão anterior
- Nota de obsolescência no ADR-003

**Consequência:** Um developer que leia ADR-003 implementará com Elasticsearch sem saber que a direcção mudou.

---

### [D-04] Ausência de documentação do fluxo de onboarding

**Problema:** O produto tem um fluxo de onboarding complexo:
1. Criação de tenant
2. Configuração inicial (environments, SSO/OIDC)
3. Convite de utilizadores
4. Activação de conta
5. Primeira configuração de serviço

Este fluxo **não está documentado em nenhum guia de utilizador**. O `LOCAL-SETUP.md` documenta o setup de desenvolvimento, não o onboarding do utilizador final.

**Impacto:** Um novo cliente enterprise a fazer self-hosted não consegue perceber como começar a usar o produto.

---

### [D-05] Documentação de API inexistente

**Problema:** O produto tem centenas de endpoints REST distribuídos por 12 módulos. Não há:
- Swagger/OpenAPI spec consolidada (irónico para um produto de governança de contratos)
- Documentação de API pública
- Referência de DTOs e contratos de API

**Nota:** O Swagger UI está provavelmente configurado via ASP.NET Core (`/swagger`), mas não há documentação navegável consolidada dos endpoints e suas semânticas.

---

### [D-06] Guias de utilizador incompletos para personas

**Ficheiro:** `docs/user-guide/`  
**Problema:** Existem ficheiros na pasta `user-guide/` mas a cobertura por persona está incompleta:
- **Engineer**: Parcialmente documentado
- **Tech Lead**: Parcialmente documentado
- **Architect**: Pouco documentado
- **Platform Admin**: Pouco documentado
- **Executive**: Praticamente sem documentação
- **Auditor**: Praticamente sem documentação

**Impacto:** O produto tem uma proposta de valor forte por persona (CLAUDE.md §6), mas a documentação não reflecte isso.

---

## 4. Problemas de Média Prioridade

### [D-07] FUTURE-ROADMAP.md desactualizado

**Problema:** O roadmap inclui features que já foram implementadas (marcadas como futuras), e não inclui as lacunas identificadas na auditoria actual. O documento não tem data de última actualização clara.

---

### [D-08] Documentação de segurança incompleta para Break Glass

**Ficheiro:** `docs/security/`  
**Problema:** O `SECURITY-ARCHITECTURE.md` descreve Break Glass como capacidade implementada, mas não documenta:
- Quem pode solicitar Break Glass
- Que dados ficam registados no audit trail
- Qual é o processo de revisão pós-uso
- Como revogar um acesso Break Glass activo

Dado que A-02 (Break Glass sem aprovação) está activo, a documentação descreve uma capacidade que está implementada de forma incompleta e potencialmente insegura.

---

### [D-09] Ausência de guia de contribuição e convenções

**Problema:** Não existe um `CONTRIBUTING.md` que documente:
- Como adicionar um novo módulo
- Como criar migrations de forma correcta
- Como escrever testes de integração com Testcontainers
- Convenções de nomes de tabelas (o problema C-01 poderia ter sido evitado com convenção documentada)
- Como adicionar chaves i18n

O CLAUDE.md tem orientações gerais, mas um developer novo precisa de um guia prático de "como fazer X".

---

### [D-10] Documentação de ClickHouse sem guia de setup

**Ficheiro:** `docs/observability/`  
**Problema:** Os documentos mencionam ClickHouse como direcção para observabilidade analítica, mas não existe:
- Guia de instalação de ClickHouse
- Schema de tabelas para ingestão de telemetria
- Configuração do provider ClickHouse no produto
- Migration de dados de Elasticsearch para ClickHouse

A documentação descreve a intenção. Não descreve como implementar.

---

### [D-11] PLATFORM-CAPABILITIES.md com features não implementadas sem marcação

**Problema:** O documento lista capacidades do produto (Feature Matrix) incluindo itens que não existem:
- IDE Extensions (VS Code / Visual Studio) — não implementado
- SAML 2.0 — não implementado  
- GraphQL Federation — não implementado
- Vector Search / RAG — não implementado

Sem marcação de status (Disponível / Em desenvolvimento / Planeado), o documento é uma lista de aspirações, não de capacidades reais.

---

### [D-12] Runbooks sem validação recente

**Ficheiro:** `docs/runbooks/`  
**Problema:** Os runbooks (PRODUCTION-DEPLOY-RUNBOOK, INCIDENT-RESPONSE-PLAYBOOK, etc.) são bem escritos, mas:
- Não têm data de última validação
- Não indicam com que versão do produto foram testados
- Alguns passos podem estar desactualizados com as mudanças de stack (ex: ainda referem passos de Elasticsearch que podem não se aplicar se ClickHouse for adoptado)

---

### [D-13] Ausência de diagrama de arquitectura actualizado

**Problema:** Os documentos de arquitectura descrevem a estrutura textualmente. Não há:
- Diagrama C4 (Context, Container, Component) actualizado
- Diagrama de sequência para fluxos críticos (login, change promotion, incident correlation)
- Diagrama de deployment para Docker Compose e produção
- Diagrama entidade-relação do schema de base de dados

Texto sem diagrama é mais difícil de consumir por stakeholders não-técnicos.

---

## 5. Problemas de Baixa Prioridade

### [D-14] Inconsistência de idioma na documentação

**Convenção definida:** Documentação narrativa em português, código em inglês.

**Encontrado:** Alguns documentos em `docs/security/` estão parcialmente em inglês, outros em português. O README.md principal está em inglês. Inconsistência que pode confundir um leitor que espera português.

---

### [D-15] Ausência de changelog de produto

**Problema:** Não existe um `CHANGELOG.md` (ou similar) que documente o que mudou entre versões/sprints. Isto dificulta:
- Comunicação de progresso a stakeholders
- Identificação de quando um bug foi introduzido
- Rastreabilidade de decisões ao longo do tempo

---

### [D-16] BRAND-IDENTITY.md sem referência a componentes UI reais

**Problema:** O `BRAND-IDENTITY.md` descreve a identidade visual com referências a cores, tipografia e tom. Mas não é referenciado pelos tokens de design (`src/frontend/src/shared/tokens/`) nem pelo Tailwind config. É um documento desconexo da implementação real.

---

### [D-17] Documentação de testes sem guia de escrita de testes

**Problema:** Não existe um guia de "como escrever testes neste projecto" que documente:
- Quando usar mock vs Testcontainers
- Como estruturar testes de handler
- Como usar Bogus para dados de teste
- Como testar multitenancy
- Como escrever testes E2E Playwright

Um developer novo vai replicar os padrões existentes, incluindo os anti-padrões identificados.

---

## 6. Documentação crítica em falta

Documentos que deveriam existir e não existem:

| Documento em falta | Prioridade | Impacto se ausente |
|-------------------|------------|-------------------|
| Guia de onboarding do utilizador final | Alta | Cliente não consegue começar a usar o produto |
| API Reference consolidada | Alta | Integrações externas impossíveis sem reverse engineering |
| Guia de contribuição (CONTRIBUTING.md) | Alta | Novos developers replicam anti-padrões |
| ADR sobre mudança Elasticsearch→ClickHouse | Alta | Confusão sobre stack de observabilidade |
| Diagrama C4 actualizado | Média | Stakeholders não técnicos sem visão de arquitectura |
| Guia de escrita de testes | Média | Anti-padrões de teste continuam a ser replicados |
| Changelog de produto | Baixa | Dificuldade de rastrear progresso |
| Guia de setup ClickHouse | Alta (se adoptado) | Observabilidade analítica inacessível |

---

## 7. Documentação que pode ser arquivada ou removida

Documentos que são redundantes, obsoletos ou que criam confusão:

| Documento | Problema | Acção sugerida |
|-----------|----------|----------------|
| AI-MODULE-EXECUTION-PLAN-V2.md | Plano de execução executado — histórico | Arquivar em `docs/archive/` |
| AI-MODULE-ACTION-PLAN.md | Duplica conteúdo do V2 | Consolidar ou arquivar |
| DEVELOPMENT-PLAN-INNOVATIVE-IDEAS.md | Ideas genéricas sem tracção | Mover para FUTURE-ROADMAP.md |
| BRAINSTORMING-INNOVATIVE-IDEAS.md | Brainstorming não organizado | Incorporar no FUTURE-ROADMAP.md ou arquivar |

---

## 8. Priorização de remediação documental

```
SPRINT 1 (correcções críticas):
  [D-01] Actualizar IMPLEMENTATION-STATUS.md          → 4h
  [D-03] Criar ADR-007 sobre ClickHouse               → 2h
  [D-05] Activar e documentar Swagger consolidado     → 1 dia

SPRINT 2 (completude):
  [D-04] Criar guia de onboarding do utilizador final → 2 dias
  [D-09] Criar CONTRIBUTING.md                        → 1 dia
  [D-11] Actualizar PLATFORM-CAPABILITIES.md com status real → 4h

SPRINT 3 (qualidade):
  [D-06] Completar guias por persona                  → 3 dias
  [D-07] Actualizar FUTURE-ROADMAP.md                 → 4h
  [D-13] Criar diagramas C4 e sequência               → 2 dias
```

---

*Para análise de inovação ver [INNOVATION-ROADMAP-2026-04-18.md](./INNOVATION-ROADMAP-2026-04-18.md)*  
*Para estado geral do produto ver [STATE-OF-PRODUCT-2026-04-18.md](./STATE-OF-PRODUCT-2026-04-18.md)*
