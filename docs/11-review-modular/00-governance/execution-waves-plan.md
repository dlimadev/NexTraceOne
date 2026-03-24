# Plano de Ondas de Execução — NexTraceOne

> **Classificação:** PLANO DE EXECUÇÃO  
> **Data de referência:** Julho 2025  
> **Escopo:** 6 ondas de execução para atingir production-ready  
> **Duração total estimada:** 10-14 semanas

---

## 1. Visão Geral das Ondas

```
Wave 0 ─── Correções Imediatas ──────── 1-2 dias
Wave 1 ─── Segurança + Docs Base ────── 1-2 semanas
Wave 2 ─── Backend/DB Críticos ──────── 2-3 semanas
Wave 3 ─── Frontend Funcional ───────── 2-3 semanas
Wave 4 ─── AI/Agents Real ──────────── 3-4 semanas
Wave 5 ─── Consolidação Final ───────── 1-2 semanas
```

| Wave | Duração | Foco Principal | Itens | Pré-requisitos |
|------|---------|---------------|-------|---------------|
| 0 | 1-2 dias | Blockers | 3 | Nenhum |
| 1 | 1-2 semanas | Segurança + Documentação | 8 | Wave 0 |
| 2 | 2-3 semanas | Backend/DB | 7 | Wave 1 (parcial) |
| 3 | 2-3 semanas | Frontend | 8 | Wave 0, Wave 2 (parcial) |
| 4 | 3-4 semanas | AI/Agents | 7 | Wave 2, Wave 3 (parcial) |
| 5 | 1-2 semanas | Consolidação | 5 | Waves 0-4 |

---

## 2. Wave 0 — Correções Imediatas (1-2 dias)

### Objetivo
Eliminar o único P0 BLOCKER e a página vazia. Restaurar a coerência mínima entre menu e rotas.

### Itens

| # | Tarefa | Prioridade | Estimativa | Responsável Sugerido |
|---|--------|-----------|------------|---------------------|
| W0-1 | Corrigir 3 rotas partidas em Contracts (governance, spectral, canonical) — adicionar imports e rotas em App.tsx | P0 | 2h | Frontend dev |
| W0-2 | Implementar ProductAnalyticsOverviewPage.tsx (actualmente 0 bytes) com componente mínimo funcional | P1 | 1h | Frontend dev |
| W0-3 | Validar coerência menu sidebar ↔ rotas após correcções | P1 | 1h | Frontend dev |

### Critérios de Conclusão Wave 0
- [ ] Navegar para `/contracts/governance` renderiza página correctamente
- [ ] Navegar para `/contracts/spectral` renderiza página correctamente
- [ ] Navegar para `/contracts/canonical` renderiza página correctamente
- [ ] ProductAnalyticsOverviewPage.tsx tem conteúdo e renderiza
- [ ] Todos os itens de menu sidebar apontam para rotas funcionais
- [ ] Zero erros 404 ao navegar pelo menu principal

### Riscos Wave 0
| Risco | Probabilidade | Mitigação |
|-------|--------------|-----------|
| Componentes de página dependem de APIs inexistentes | Baixa | Usar estados de loading/empty |
| Imports circulares | Baixa | Verificar com build após alteração |

---

## 3. Wave 1 — Segurança e Documentação Base (1-2 semanas)

### Objetivo
Fortalecer segurança (MFA, API key) e criar a base documental necessária para onboarding e manutenção.

### Itens

| # | Tarefa | Prioridade | Estimativa | Dependências |
|---|--------|-----------|------------|-------------|
| W1-1 | Implementar enforcement de MFA em runtime | P2 | 3-5 dias | Nenhuma |
| W1-2 | Migrar API key storage de appsettings para BD encriptada | P2 | 3-5 dias | Nenhuma |
| W1-3 | Criar README raiz do repositório | P1 | 4h | Nenhuma |
| W1-4 | Criar READMEs para os 9 módulos principais | P1 | 2 dias | W1-3 |
| W1-5 | Corrigir documentação AI Knowledge para refletir estado real | P1 | 1 dia | Nenhuma |
| W1-6 | Criar documentação mínima para Integrations (0% → 30%+) | P1 | 1 dia | Nenhuma |
| W1-7 | Criar documentação mínima para Product Analytics (0% → 30%+) | P1 | 1 dia | Nenhuma |
| W1-8 | Resolver 7 páginas órfãs (conectar a rotas ou remover) | P2 | 2-3 dias | Wave 0 |

### Critérios de Conclusão Wave 1
- [ ] MFA enforcement activo para utilizadores com MFA configurado
- [ ] API keys armazenadas em BD com encriptação AES-256-GCM
- [ ] README raiz existe com setup instructions funcionais
- [ ] 9/9 módulos com README
- [ ] Documentação AI Knowledge alinhada com realidade (features marcadas como "Implementado"/"Planeado")
- [ ] Integrations com docs mínimas (README + overview)
- [ ] Product Analytics com docs mínimas (README + overview)
- [ ] Zero páginas órfãs — todas conectadas ou explicitamente removidas

### Riscos Wave 1
| Risco | Probabilidade | Mitigação |
|-------|--------------|-----------|
| MFA enforcement quebra login de utilizadores existentes | Média | Implementar com período de graça (14 dias) |
| Migração API key causa downtime | Baixa | Migração com dual-read (appsettings + BD) temporário |
| READMEs ficam desactualizados rapidamente | Média | Incluir data de revisão e link para CI |

---

## 4. Wave 2 — Backend/DB por Módulos Críticos (2-3 semanas)

### Objetivo
Corrigir dívida técnica em base de dados, criar migrações em falta, adicionar RowVersion, e consolidar backend dos módulos mais críticos.

### Itens

| # | Tarefa | Prioridade | Estimativa | Dependências |
|---|--------|-----------|------------|-------------|
| W2-1 | Criar migrações para Configuration module | P3 | 1-2 dias | Nenhuma |
| W2-2 | Criar migrações para Notifications module | P3 | 1-2 dias | Nenhuma |
| W2-3 | Adicionar RowVersion/ConcurrencyToken em entidades críticas | P3 | 1-2 semanas | W2-1, W2-2 |
| W2-4 | Seed production roles e permissões completas | P2 | 2-3 dias | Nenhuma |
| W2-5 | Consolidar documentação Configuration (35 ficheiros fragmentados) | P2 | 3-5 dias | Nenhuma |
| W2-6 | Completar backend Contracts (75% → 85%+) | P2 | 1-2 semanas | Nenhuma |
| W2-7 | Completar versionamento API (endpoints sem versão) | P3 | 1-2 semanas | Nenhuma |

### Critérios de Conclusão Wave 2
- [ ] Configuration tem migrações aplicáveis com `dotnet ef database update`
- [ ] Notifications tem migrações aplicáveis
- [ ] RowVersion presente em ≥50% das entidades críticas (Aggregates roots)
- [ ] Roles de produção seed funcional
- [ ] Configuration docs consolidados num único README + sub-docs
- [ ] Contracts backend ≥85%
- [ ] Endpoints críticos versionados

### Riscos Wave 2
| Risco | Probabilidade | Mitigação |
|-------|--------------|-----------|
| RowVersion causa breaking changes em handlers existentes | Média | Adicionar de forma progressiva, testar cada módulo |
| Migrações conflitam com schema existente | Baixa | Testar em BD limpa e em BD com dados |
| Versionamento API quebra clientes existentes | Média | Manter versão antiga temporariamente |

---

## 5. Wave 3 — Frontend Funcional (2-3 semanas)

### Objetivo
Resolver todos os gaps de frontend: i18n, integração API, UX/empty states, e elevar módulos fracos.

### Itens

| # | Tarefa | Prioridade | Estimativa | Dependências |
|---|--------|-----------|------------|-------------|
| W3-1 | Completar i18n pt-BR (+11 namespaces) | P2 | 1-2 dias | Nenhuma |
| W3-2 | Completar i18n es (+8 namespaces) | P2 | 1-2 dias | Nenhuma |
| W3-3 | Completar i18n pt-PT (+1 namespace) | P2 | 2-4h | Nenhuma |
| W3-4 | Elevar frontend Audit & Compliance (40% → 70%+) | P2 | 2-3 semanas | Wave 2 backend |
| W3-5 | Completar integração API em Contracts frontend (60% → 80%+) | P2 | 1-2 semanas | W2-6 |
| W3-6 | Melhorar error states e empty states uniformemente | P3 | 1 semana | Nenhuma |
| W3-7 | Aumentar testes Integrations (20% → 50%+) | P2 | 1-2 semanas | Nenhuma |
| W3-8 | Aumentar testes Governance (55% → 70%+) | P3 | 1-2 semanas | Nenhuma |

### Critérios de Conclusão Wave 3
- [ ] pt-BR com ≤2 namespaces em falta
- [ ] es com ≤2 namespaces em falta
- [ ] pt-PT completo
- [ ] Audit & Compliance frontend ≥70%
- [ ] Contracts frontend ≥80%
- [ ] Todos os módulos com error/empty states i18n
- [ ] Integrations testes ≥50%
- [ ] Governance testes ≥70%

### Riscos Wave 3
| Risco | Probabilidade | Mitigação |
|-------|--------------|-----------|
| Traduções incorrectas em es e pt-BR | Média | Revisão por falante nativo |
| Frontend Audit depende de endpoints inexistentes | Média | Criar stubs/mocks temporários |
| Testes flaky em frontend | Média | Usar testing-library com waits adequados |

---

## 6. Wave 4 — AI/Agents Real (3-4 semanas)

### Objetivo
Transformar módulo AI de cosmético para funcional: ferramentas executáveis, streaming, providers activos, RAG básico.

### Itens

| # | Tarefa | Prioridade | Estimativa | Dependências |
|---|--------|-----------|------------|-------------|
| W4-1 | Conectar AI Tools execution em runtime (de COSMETIC_ONLY para funcional) | P3 | 2-3 semanas | Nenhuma |
| W4-2 | Implementar streaming para AI chat | P3 | 2-3 semanas | W4-1 |
| W4-3 | Ativar OpenAI provider (requer configuração API key) | P3 | 2-3 dias | Nenhuma |
| W4-4 | Ativar Azure AI provider | P3 | 2-3 dias | Nenhuma |
| W4-5 | Elevar backend AI Knowledge (25% → 50%+) | P3 | 3-4 semanas | W4-1 |
| W4-6 | Implementar RAG/Retrieval básico | P3 | 2-3 semanas | W4-1, W4-5 |
| W4-7 | Aumentar testes AI Knowledge (10% → 40%+) | P3 | 2-3 semanas | W4-5 |

### Critérios de Conclusão Wave 4
- [ ] Pelo menos 3 tools executam acções reais e retornam dados
- [ ] Streaming funcional — tokens aparecem incrementalmente no chat
- [ ] Pelo menos 2 providers activos (Ollama + 1 externo)
- [ ] Backend AI Knowledge ≥50%
- [ ] RAG básico funcional (queries sobre documentação interna)
- [ ] Testes AI Knowledge ≥40%

### Riscos Wave 4
| Risco | Probabilidade | Mitigação |
|-------|--------------|-----------|
| Tools execution causa side-effects não previstos | Alta | Sandbox environment, dry-run mode |
| Streaming incompatível com proxy/CDN | Média | Testar com e sem proxy, SSE fallback |
| API keys de providers têm custos | Média | Definir budgets e quotas por tenant |
| RAG requer vector database | Alta | Avaliar pgvector (já PostgreSQL) vs serviço externo |

---

## 7. Wave 5 — Consolidação Final (1-2 semanas)

### Objetivo
Documentação final, guia de onboarding, testes de aceitação, validação de todos os critérios de fecho.

### Itens

| # | Tarefa | Prioridade | Estimativa | Dependências |
|---|--------|-----------|------------|-------------|
| W5-1 | Criar guia de onboarding completo | P2 | 3-5 dias | Waves 1-4 |
| W5-2 | Testes de aceitação end-to-end | P2 | 1 semana | Waves 0-4 |
| W5-3 | Documentação final alinhada com código | P2 | 3-5 dias | Waves 0-4 |
| W5-4 | Validação completa de todos os critérios de fecho | P2 | 2-3 dias | W5-1, W5-2, W5-3 |
| W5-5 | Avaliação final de maturidade por módulo | P2 | 1-2 dias | W5-4 |

### Critérios de Conclusão Wave 5
- [ ] Guia de onboarding testado por developer novo (ou simulação)
- [ ] Testes E2E cobrem fluxos críticos de todos os módulos
- [ ] Documentação final revista e aprovada
- [ ] Todos os módulos acima de 60% maturidade
- [ ] Relatório final de maturidade produzido

### Riscos Wave 5
| Risco | Probabilidade | Mitigação |
|-------|--------------|-----------|
| Testes E2E flaky | Alta | Retry logic, waits adequados, ambiente dedicado |
| Onboarding guide desactualiza rapidamente | Média | Automatizar validação de links e commands |
| Módulos que regrediram durante waves anteriores | Baixa | Regressão tests em CI/CD |

---

## 8. Timeline Visual

```
Semana:  1  2  3  4  5  6  7  8  9  10  11  12  13  14
W0:     ██
W1:     ████████
W2:           ████████████
W3:                 ████████████
W4:                       ████████████████
W5:                                    ████████
```

**Nota:** Waves 2 e 3 podem executar parcialmente em paralelo se houver equipas separadas para backend e frontend.

---

## 9. Recursos Estimados

| Wave | Backend Devs | Frontend Devs | DevOps | Doc Writer | Total |
|------|-------------|---------------|--------|-----------|-------|
| 0 | 0 | 1 | 0 | 0 | 1 |
| 1 | 1 | 1 | 0 | 1 | 3 |
| 2 | 2 | 0 | 1 | 1 | 4 |
| 3 | 0 | 2 | 0 | 0 | 2 |
| 4 | 2 | 1 | 0 | 0 | 3 |
| 5 | 1 | 1 | 1 | 1 | 4 |

**Total estimado:** 3-4 developers a tempo inteiro durante 14 semanas, ou equivalente.

---

## 10. Checkpoints de Go/No-Go

| Checkpoint | Quando | Critério de Go |
|-----------|--------|---------------|
| Post-Wave 0 | Dia 2 | Zero P0, menu coerente |
| Post-Wave 1 | Semana 2 | Segurança hardened, docs base existente |
| Post-Wave 2 | Semana 5 | DB saudável, migrações OK, RowVersion em progresso |
| Post-Wave 3 | Semana 8 | Frontend funcional, i18n completo, testes melhorados |
| Post-Wave 4 | Semana 12 | AI funcional (não cosmético), streaming OK |
| Post-Wave 5 | Semana 14 | Production-ready — todos os critérios de fecho validados |
