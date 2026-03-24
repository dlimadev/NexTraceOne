# Recomendações de Melhoria da Documentação — NexTraceOne

> **Tipo:** Plano de ação priorizado  
> **Escopo:** Documentação completa do repositório (backend, frontend, docs/, onboarding)  
> **Data de referência:** Junho 2025  
> **Classificação:** Interno — Governança de Produto

---

## 1. Resumo

Este documento apresenta um **plano de melhoria priorizado** para a documentação do NexTraceOne, organizado em quatro horizontes temporais. Cada ação inclui localização concreta dos ficheiros, estimativa de esforço e impacto esperado.

### Princípios orientadores

1. **Impacto no onboarding primeiro** — priorizar o que ajuda novos membros da equipa.
2. **Refatoração incremental** — não reescrever tudo; melhorar gradualmente.
3. **Documentação como código** — ficheiros versionados, revistos em PR, localizados junto ao código.
4. **Manter coerência** — preservar a qualidade existente (97.5% XML docs backend, convenções A+).

---

## 2. Imediato — Semana 1

### 2.1 Criar README raiz do repositório

| Campo | Detalhe |
|---|---|
| **Ficheiro** | `/README.md` |
| **Estado atual** | ❌ Inexistente |
| **Conteúdo** | Visão geral do produto, setup rápido, links para docs principais |
| **Template** | Ver `readme-coverage-report.md` — secção 5.2 |
| **Esforço** | 2–4 horas |
| **Impacto** | 🔴 Máximo — primeiro ficheiro visto por qualquer pessoa |
| **Responsável sugerido** | Tech Lead |

### 2.2 Criar guia "Getting Started"

| Campo | Detalhe |
|---|---|
| **Ficheiro** | `docs/getting-started/README.md` |
| **Estado atual** | ❌ Inexistente |
| **Conteúdo** | Pré-requisitos, instalação, execução (backend, frontend, Docker), primeiro contributo |
| **Esforço** | 4–8 horas |
| **Impacto** | 🔴 Máximo — reduz tempo de setup de horas para minutos |
| **Responsável sugerido** | Tech Lead + DevOps |

**Secções mínimas:**
1. Pré-requisitos (.NET 8, Node 20+, Docker, PostgreSQL)
2. Clonar e configurar
3. Executar backend localmente
4. Executar frontend localmente
5. Executar tudo com Docker Compose
6. Verificar que funciona (smoke test)
7. Estrutura do repositório (mapa)
8. Próximos passos (links para docs de módulos)

### 2.3 Criar mapa da codebase

| Campo | Detalhe |
|---|---|
| **Ficheiro** | `docs/getting-started/codebase-map.md` |
| **Estado atual** | ❌ Inexistente |
| **Conteúdo** | Explicação visual de cada diretório principal |
| **Esforço** | 2 horas |
| **Impacto** | 🔴 Alto — orientação espacial |

**Conteúdo sugerido:**
```
NexTraceOne/
├── src/
│   ├── modules/                 # 9 módulos de domínio (DDD)
│   │   ├── audit/               # Auditoria e rastreabilidade
│   │   ├── catalog/             # Catálogo de serviços
│   │   ├── change-intelligence/ # Inteligência de mudanças
│   │   ├── contract-governance/ # Governança de contratos
│   │   ├── foundation/          # Identidade, organizações, equipas
│   │   ├── notifications/       # Sistema de notificações
│   │   ├── observability/       # Observabilidade e telemetria
│   │   ├── operations/          # Operações e incidentes
│   │   └── ai-agents/           # Agentes de IA
│   ├── platform/                # Infraestrutura partilhada
│   │   └── NexTraceOne.ApiHost/ # Ponto de entrada da API
│   ├── building-blocks/         # Componentes base reutilizáveis
│   └── frontend/                # Aplicação React/TypeScript
├── tests/                       # Testes
├── docs/                        # 564 ficheiros de documentação
├── infra/                       # Infraestrutura (IaC)
├── scripts/                     # Scripts auxiliares
└── tools/                       # Ferramentas de desenvolvimento
```

---

## 3. Curto Prazo — Sprint 2

### 3.1 READMEs para os 9 módulos backend

| Campo | Detalhe |
|---|---|
| **Ficheiros** | `src/modules/{module}/README.md` (×9) |
| **Estado atual** | ❌ 0/9 existem |
| **Template** | Ver `readme-coverage-report.md` — secção 5.1 |
| **Esforço** | 1–2 horas por módulo (total: 9–18h) |
| **Impacto** | 🔴 Alto — permite entender cada módulo sem ler código |
| **Responsável sugerido** | Dono de cada módulo |

**Conteúdo mínimo por módulo:**
- Visão geral e propósito
- Entidades principais
- Endpoints expostos
- Dependências (outros módulos)
- Padrões específicos
- Como testar

### 3.2 Documentar API services do frontend sem JSDoc

| Campo | Detalhe |
|---|---|
| **Ficheiros** | 15 ficheiros de API service em `src/frontend/src/features/*/services/` |
| **Estado atual** | 15 de 34 ficheiros sem JSDoc (55.9% cobertura) |
| **Esforço** | 4–6 horas |
| **Impacto** | 🟡 Médio — ponto de integração backend↔frontend |
| **Responsável sugerido** | Programadores frontend |

**Template JSDoc para API service:**
```typescript
/**
 * Serviço de integração com a API de {módulo}.
 * 
 * Endpoints consumidos:
 * - GET /api/{module}/{resource} — {descrição}
 * - POST /api/{module}/{resource} — {descrição}
 * 
 * @module {Module}ApiService
 */
```

### 3.3 Documentar hooks de contratos sem JSDoc

| Campo | Detalhe |
|---|---|
| **Ficheiros** | `useContractDiff`, `useContractExport`, `useContractTransition`, `useSpectralRulesets` |
| **Estado atual** | 4 hooks sem JSDoc |
| **Esforço** | 1–2 horas |
| **Impacto** | 🟡 Médio — contratos são pilar central do produto |

### 3.4 Documentar 32 páginas sem JSDoc

| Campo | Detalhe |
|---|---|
| **Ficheiros** | 32 ficheiros de página em `src/frontend/src/features/*/pages/` |
| **Estado atual** | 32 de 96 páginas sem JSDoc |
| **Esforço** | 8–12 horas |
| **Impacto** | 🟡 Médio |

### 3.5 Reescrever README do frontend

| Campo | Detalhe |
|---|---|
| **Ficheiro** | `src/frontend/README.md` |
| **Estado atual** | Template genérico Vite — não reflete o NexTraceOne |
| **Esforço** | 2 horas |
| **Impacto** | 🟡 Médio |

**Conteúdo sugerido:**
- Visão geral da aplicação frontend
- Link para `ARCHITECTURE.md` (que já é excelente)
- Setup local (npm install, npm run dev)
- Estrutura de features
- Padrões de desenvolvimento
- Link para design system

### 3.6 Criar CONTRIBUTING.md

| Campo | Detalhe |
|---|---|
| **Ficheiro** | `/CONTRIBUTING.md` |
| **Estado atual** | ❌ Inexistente |
| **Esforço** | 2 horas |
| **Impacto** | 🟡 Médio |

**Secções:**
- Processo de branch e PR
- Convenções de commit
- Code review checklist
- Padrões de código (link para `docs/GUIDELINE.md`)
- Como adicionar testes

### 3.7 Documentar entity configurations em falta

| Campo | Detalhe |
|---|---|
| **Ficheiros** | 6 entity configs (Catalog) + 4 AI Knowledge configs |
| **Estado atual** | 10 ficheiros sem XML docs |
| **Esforço** | 2–3 horas |
| **Impacto** | 🟢 Baixo — completar 97.5% → ~99.5% |

---

## 4. Médio Prazo — 1–2 Meses

### 4.1 Ativar `<GenerateDocumentationFile>` nos .csproj

| Campo | Detalhe |
|---|---|
| **Ficheiro** | `Directory.Build.props` |
| **Estado atual** | 0/53 .csproj com geração ativada |
| **Esforço** | 1 hora (configuração) + 2–4 horas (resolver warnings) |
| **Impacto** | 🟡 Médio — habilita Swagger XML docs e DocFX |

**Implementação sugerida:**
```xml
<!-- Em Directory.Build.props -->
<PropertyGroup>
  <GenerateDocumentationFile>true</GenerateDocumentationFile>
  <NoWarn>$(NoWarn);CS1591</NoWarn>
</PropertyGroup>
```

### 4.2 Configurar Swagger para usar XML docs

| Campo | Detalhe |
|---|---|
| **Ficheiro** | `src/platform/NexTraceOne.ApiHost/Program.cs` ou equivalente |
| **Estado atual** | XML docs não incluídos no Swagger |
| **Esforço** | 2–4 horas |
| **Impacto** | 🟡 Médio — documentação de API automática |

### 4.3 Criar ADRs retroativos

| Campo | Detalhe |
|---|---|
| **Diretório** | `docs/adr/` |
| **Estado atual** | ❌ Inexistente |
| **Esforço** | 8–16 horas (5–10 ADRs iniciais) |
| **Impacto** | 🟡 Médio — preserva conhecimento de decisões |

**ADRs sugeridos para criação retroativa:**

| # | Título | Importância |
|---|---|---|
| ADR-001 | Arquitetura modular DDD com módulos verticais | Alta |
| ADR-002 | Escolha de PostgreSQL com RLS para multitenancy | Alta |
| ADR-003 | CQRS sem Event Sourcing completo | Média |
| ADR-004 | Feature-based frontend architecture | Média |
| ADR-005 | i18n como requisito obrigatório desde o início | Média |
| ADR-006 | IA interna como padrão, externa como opcional governada | Alta |
| ADR-007 | Prefixos de tabela por módulo na BD | Baixa |
| ADR-008 | Building blocks como componentes partilhados | Média |

### 4.4 Normalizar idioma dos comentários frontend

| Campo | Detalhe |
|---|---|
| **Ficheiros** | ~432 ficheiros frontend |
| **Estado atual** | ~60% inglês, ~40% português |
| **Esforço** | 10–15 horas (gradual) |
| **Impacto** | 🟢 Baixo — consistência |

**Decisão necessária:** Definir regra oficial (sugestão: JSDoc em inglês, inline em português).

### 4.5 Adicionar comentários a páginas complexas

| Campo | Detalhe |
|---|---|
| **Ficheiros prioritários** | `AiAssistantPage.tsx`, `ServiceCatalogPage.tsx` |
| **Estado atual** | Mínimo de comentários em páginas com 1.000+ LOC |
| **Esforço** | 4–8 horas |
| **Impacto** | 🟡 Médio — legibilidade para júniores |

### 4.6 Criar índice principal de docs/

| Campo | Detalhe |
|---|---|
| **Ficheiro** | `docs/README.md` |
| **Estado atual** | ❌ Inexistente ou pouco útil |
| **Esforço** | 2 horas |
| **Impacto** | 🟡 Médio — navegação entre 564 ficheiros |

---

## 5. Contínuo — Práticas Permanentes

### 5.1 Code review — verificações de documentação

| Verificação | Obrigatória para | Frequência |
|---|---|---|
| JSDoc presente em novos API services | Todos os PRs | Cada PR |
| JSDoc presente em novos hooks | Todos os PRs | Cada PR |
| JSDoc presente em novas páginas | Todos os PRs | Cada PR |
| XML docs em novos ficheiros C# públicos | Todos os PRs | Cada PR |
| ESLint disable com justificação | Todos os PRs | Cada PR |
| i18n para todo texto visível | Todos os PRs frontend | Cada PR |

### 5.2 Manutenção de documentação

| Ação | Frequência | Responsável |
|---|---|---|
| Atualizar READMEs de módulo quando estrutura muda | Cada sprint | Dono do módulo |
| Verificar que Getting Started funciona | Mensal | Rotativo |
| Atualizar ADRs quando decisões mudam | Quando aplicável | Arquiteto |
| Review de documentação de auditoria | Trimestral | Tech Lead |

### 5.3 Métricas a monitorar

| Métrica | Ferramenta sugerida | Frequência |
|---|---|---|
| Cobertura JSDoc frontend | Script personalizado ou ESLint plugin | Cada PR |
| Cobertura XML docs backend | Build warnings (após ativar GenerateDocumentationFile) | Cada build |
| Density de comentários frontend | Script de contagem | Mensal |
| Módulos com README | Verificação manual ou script | Mensal |

---

## 6. Resumo do Plano — Timeline

### Visualização temporal

```
Semana 1          Sprint 2              Mês 1-2               Contínuo
─────────────── ───────────────────── ───────────────────── ────────────
README raiz      9 READMEs módulos     GenerateDocFile        Code review
Getting Started  15 API services JSDoc Swagger + XML docs    Manutenção
Codebase map     32 páginas JSDoc      ADRs retroativos      Métricas
                 4 hooks contratos     Normalizar idioma
                 Rewrite frontend README Páginas complexas
                 CONTRIBUTING.md       Índice docs/
                 10 entity configs
```

### Esforço total estimado

| Fase | Horas estimadas | Pessoas | Semanas |
|---|---|---|---|
| Semana 1 | 8–14h | 1–2 | 1 |
| Sprint 2 | 28–45h | 3–4 | 2 |
| Médio prazo | 30–50h | 2–3 | 4–6 |
| **Total (sem contínuo)** | **66–109h** | — | **7–9 semanas** |

### Impacto esperado

| Métrica | Antes | Depois |
|---|---|---|
| Tempo onboarding júnior | 6–8 semanas | 3–4 semanas |
| Pontuação legibilidade frontend | 61/100 | ~75/100 |
| Cobertura XML docs backend | 97.5% | ~100% |
| Cobertura JSDoc frontend | 55–67% | ~85% |
| READMEs em módulos | 0/9 | 9/9 |
| Documentação orientação vs auditoria | ~5% / ~95% | ~30% / ~70% |

---

## 7. Priorização por ROI

| # | Ação | Esforço | Impacto | ROI |
|---|---|---|---|---|
| 1 | README raiz | 2–4h | 🔴 Máximo | ⭐⭐⭐⭐⭐ |
| 2 | Getting Started guide | 4–8h | 🔴 Máximo | ⭐⭐⭐⭐⭐ |
| 3 | Codebase map | 2h | 🔴 Alto | ⭐⭐⭐⭐⭐ |
| 4 | 9 READMEs de módulos | 9–18h | 🔴 Alto | ⭐⭐⭐⭐ |
| 5 | 15 API services JSDoc | 4–6h | 🟡 Médio | ⭐⭐⭐⭐ |
| 6 | CONTRIBUTING.md | 2h | 🟡 Médio | ⭐⭐⭐⭐ |
| 7 | 4 hooks contratos JSDoc | 1–2h | 🟡 Médio | ⭐⭐⭐⭐ |
| 8 | Swagger + XML docs | 4–8h | 🟡 Médio | ⭐⭐⭐ |
| 9 | 10 entity configs | 2–3h | 🟢 Baixo | ⭐⭐⭐ |
| 10 | ADRs retroativos | 8–16h | 🟡 Médio | ⭐⭐ |
| 11 | Normalizar idioma | 10–15h | 🟢 Baixo | ⭐⭐ |

---

> **Nota:** Este relatório complementa o relatório principal `documentation-and-onboarding-audit.md`.
