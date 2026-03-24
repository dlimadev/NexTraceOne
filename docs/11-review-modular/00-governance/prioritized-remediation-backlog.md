# Backlog de Remediação Priorizado — NexTraceOne

> **Classificação:** BACKLOG DE REMEDIAÇÃO  
> **Data de referência:** Julho 2025  
> **Escopo:** Todas as correções identificadas em auditorias modulares  
> **Metodologia:** Priorização P0-P4, classificação por natureza, estimativa de esforço

---

## 1. Legenda de Classificação

### Prioridade
| Nível | Significado | SLA sugerido |
|-------|------------|-------------|
| **P0_BLOCKER** | Impede uso em produção | ≤ 24h |
| **P1_CRITICAL** | Impacto grave na experiência | ≤ 1 semana |
| **P2_HIGH** | Funcionalidade comprometida | ≤ 2-4 semanas |
| **P3_MEDIUM** | Dívida técnica relevante | ≤ 2-3 meses |
| **P4_LOW** | Melhoria futura | Próximo trimestre |

### Natureza
| Tipo | Descrição | Risco |
|------|-----------|-------|
| **QUICK_WIN** | Correção simples, localizada, sem risco | Baixo |
| **LOCAL_FIX** | Correção num módulo, sem dependências externas | Baixo-Médio |
| **CROSS_CUTTING_FIX** | Correção que afeta múltiplos módulos | Médio |
| **STRUCTURAL_REFACTOR** | Reestruturação significativa | Médio-Alto |
| **FOUNDATIONAL_WORK** | Trabalho de base/infraestrutura | Alto |

---

## 2. P0_BLOCKER — Impede Uso em Produção

| ID | Problema | Módulo | Natureza | Estimativa | Dependências | Wave |
|----|----------|--------|----------|------------|-------------|------|
| P0-1 | 3 rotas partidas em Contracts (governance, spectral, canonical) — páginas existem mas não importadas/roteadas em App.tsx | Contracts | **QUICK_WIN** | 2h | Nenhuma | 0 |

### Detalhes P0-1
**Sintoma:** Utilizador clica em menu Contracts → sub-itens governance, spectral, canonical → página em branco ou erro 404.  
**Causa:** Componentes de página existem no filesystem mas não foram adicionados ao router em App.tsx.  
**Correção:** Adicionar imports e rotas em App.tsx para os 3 componentes.  
**Validação:** Navegar para cada uma das 3 rotas e confirmar renderização.

---

## 3. P1_CRITICAL — Impacto Grave na Experiência

| ID | Problema | Módulo | Natureza | Estimativa | Dependências | Wave |
|----|----------|--------|----------|------------|-------------|------|
| P1-1 | README raiz inexistente | Transversal | **QUICK_WIN** | 4h | Nenhuma | 1 |
| P1-2 | ProductAnalyticsOverviewPage.tsx com 0 bytes | Product Analytics | **QUICK_WIN** | 1h | Nenhuma | 0 |
| P1-3 | READMEs inexistentes para 9 módulos | Transversal | **LOCAL_FIX** | 2 dias | P1-1 | 1 |
| P1-4 | Documentação AI excessivamente otimista vs estado real (~25% backend) | AI Knowledge | **LOCAL_FIX** | 1 dia | Nenhuma | 1 |
| P1-5 | Zero documentação em Integrations (0%) | Integrations | **LOCAL_FIX** | 1 dia | Nenhuma | 1 |
| P1-6 | Zero documentação em Product Analytics (0%) | Product Analytics | **LOCAL_FIX** | 1 dia | Nenhuma | 1 |
| P1-7 | 7 páginas órfãs sem acesso via menu ou rota | Vários | **LOCAL_FIX** | 2-3 dias | P0-1 | 1 |

### Detalhes P1-1: README Raiz
**Conteúdo mínimo:**
- Visão geral do NexTraceOne
- Arquitectura high-level
- Setup local (pré-requisitos, build, run)
- Estrutura do repositório
- Módulos e links para READMEs
- Contribuição
- Licença

### Detalhes P1-2: Página 0 Bytes
**Correção:** Implementar componente React mínimo com título, breadcrumb e estado vazio i18n.

### Detalhes P1-4: Documentação AI Otimista
**Acção:** Rever todos os ficheiros de documentação do módulo AI Knowledge e alinhar descrições com o estado real de implementação. Marcar claramente features como "Implementado", "Parcial" ou "Planeado".

---

## 4. P2_HIGH — Funcionalidade Comprometida

| ID | Problema | Módulo | Natureza | Estimativa | Dependências | Wave |
|----|----------|--------|----------|------------|-------------|------|
| P2-1 | MFA modelado mas não enforced em runtime | Identity | **STRUCTURAL_REFACTOR** | 2-3 semanas | Nenhuma | 1 |
| P2-2 | Suporte SAML não implementado (apenas OIDC) | Identity | **STRUCTURAL_REFACTOR** | 3-4 semanas | P2-1 | 2-3 |
| P2-3 | Gaps i18n: pt-BR -11 ns, es -8 ns, pt-PT -1 ns | Frontend | **LOCAL_FIX** | 2-3 dias | Nenhuma | 3 |
| P2-4 | API key armazenada em memória (appsettings) | Identity | **LOCAL_FIX** | 1 semana | Nenhuma | 1 |
| P2-5 | Guia de onboarding inexistente | Transversal | **LOCAL_FIX** | 3-5 dias | P1-1, P1-3 | 1-2 |
| P2-6 | Módulo Governance como catch-all (25 páginas, 19 endpoints) | Governance | **STRUCTURAL_REFACTOR** | 2-3 semanas | Nenhuma | 3-4 |
| P2-7 | 7 páginas órfãs sem acesso | Vários | **LOCAL_FIX** | 2-3 dias | P0-1 | 1 |
| P2-8 | Frontend Audit & Compliance a 40% | Audit | **STRUCTURAL_REFACTOR** | 2-3 semanas | Nenhuma | 3 |
| P2-9 | Documentation Configuration fragmentada (35 ficheiros) | Configuration | **LOCAL_FIX** | 3-5 dias | Nenhuma | 2 |
| P2-10 | Testes Integrations a 20% | Integrations | **LOCAL_FIX** | 1-2 semanas | Nenhuma | 3 |
| P2-11 | Testes Product Analytics a 10% | Product Analytics | **LOCAL_FIX** | 1-2 semanas | P1-2 | 4 |

---

## 5. P3_MEDIUM — Dívida Técnica Relevante

| ID | Problema | Módulo | Natureza | Estimativa | Dependências | Wave |
|----|----------|--------|----------|------------|-------------|------|
| P3-1 | AI Tools execução COSMETIC_ONLY — não conectados em runtime | AI Knowledge | **STRUCTURAL_REFACTOR** | 2-3 semanas | Nenhuma | 4 |
| P3-2 | RowVersion/ConcurrencyToken ausente em todas as entidades | Transversal | **CROSS_CUTTING_FIX** | 1-2 semanas | Nenhuma | 2 |
| P3-3 | Configuration sem migrações | Configuration | **LOCAL_FIX** | 1-2 dias | Nenhuma | 2 |
| P3-4 | Notifications sem migrações | Notifications | **LOCAL_FIX** | 1-2 dias | Nenhuma | 2 |
| P3-5 | Implementar streaming para AI chat | AI Knowledge | **STRUCTURAL_REFACTOR** | 2-3 semanas | P3-1 | 4 |
| P3-6 | 3/4 AI providers inativos (OpenAI, Anthropic, Azure AI) | AI Knowledge | **LOCAL_FIX** | 1 semana | Nenhuma | 4 |
| P3-7 | Backend AI Knowledge a 25% | AI Knowledge | **STRUCTURAL_REFACTOR** | 3-4 semanas | Nenhuma | 4 |
| P3-8 | RAG/Retrieval não implementado | AI Knowledge | **STRUCTURAL_REFACTOR** | 3-4 semanas | P3-1, P3-7 | 4-5 |
| P3-9 | Testes AI Knowledge a 10% | AI Knowledge | **LOCAL_FIX** | 2-3 semanas | P3-7 | 4 |
| P3-10 | Testes Governance a 55% (insuficiente para 25 páginas) | Governance | **LOCAL_FIX** | 1-2 semanas | Nenhuma | 3 |
| P3-11 | Frontend comments a 0,95% | Frontend | **CROSS_CUTTING_FIX** | Contínuo | Nenhuma | 3+ |
| P3-12 | Versionamento API parcial | Backend | **CROSS_CUTTING_FIX** | 1-2 semanas | Nenhuma | 3 |

---

## 6. P4_LOW — Melhoria Futura

| ID | Problema | Módulo | Natureza | Estimativa | Dependências | Wave |
|----|----------|--------|----------|------------|-------------|------|
| P4-1 | Zero check constraints na BD | Database | **CROSS_CUTTING_FIX** | 1-2 semanas | Nenhuma | 5 |
| P4-2 | nextraceone_operations com 12 DbContexts | Database | **FOUNDATIONAL_WORK** | 3-4 semanas | P3-2, P3-3, P3-4 | 5+ |
| P4-3 | Responsividade mobile básica | Frontend | **CROSS_CUTTING_FIX** | 2-3 semanas | Nenhuma | 5 |
| P4-4 | IDE extensions (VS Code, Visual Studio) | AI Knowledge | **FOUNDATIONAL_WORK** | 4-6 semanas | P3-1, P3-7 | Futuro |
| P4-5 | Semantic Kernel integration | AI Knowledge | **FOUNDATIONAL_WORK** | 3-4 semanas | P3-7 | Futuro |
| P4-6 | FinOps avançado contextualizado | Governance | **STRUCTURAL_REFACTOR** | 3-4 semanas | P2-6 | Futuro |
| P4-7 | Consolidação de 29 migrações activas | Database | **FOUNDATIONAL_WORK** | 1-2 semanas | Todas P3 DB | 5+ |

---

## 7. Resumo Estatístico

### Por prioridade
| Prioridade | Quantidade | Quick Wins | Local Fix | Cross-Cutting | Structural | Foundational |
|-----------|-----------|------------|-----------|---------------|------------|-------------|
| P0_BLOCKER | 1 | 1 | — | — | — | — |
| P1_CRITICAL | 7 | 2 | 5 | — | — | — |
| P2_HIGH | 11 | — | 7 | — | 4 | — |
| P3_MEDIUM | 12 | — | 5 | 3 | 4 | — |
| P4_LOW | 7 | — | — | 2 | 1 | 4 |
| **Total** | **38** | **3** | **17** | **5** | **9** | **4** |

### Por natureza
| Natureza | Quantidade | % | Esforço Médio |
|----------|-----------|---|---------------|
| QUICK_WIN | 3 | 8% | 2-4h |
| LOCAL_FIX | 17 | 45% | 2-5 dias |
| CROSS_CUTTING_FIX | 5 | 13% | 1-2 semanas |
| STRUCTURAL_REFACTOR | 9 | 24% | 2-4 semanas |
| FOUNDATIONAL_WORK | 4 | 10% | 3-6 semanas |

### Por wave
| Wave | Itens | Foco |
|------|-------|------|
| Wave 0 | 2 | Blockers imediatos |
| Wave 1 | 8 | Segurança + Docs base |
| Wave 2 | 6 | Backend/DB |
| Wave 3 | 8 | Frontend funcional |
| Wave 4 | 7 | AI/Agents real |
| Wave 5 | 4 | Consolidação |
| Futuro | 3 | Evolução |

---

## 8. Dependências Críticas

```
P0-1 (Rotas partidas)
  └── P1-7 (Páginas órfãs) — depende de rotas corrigidas
  └── P2-7 (Páginas órfãs) — mesmo

P1-1 (README raiz)
  └── P1-3 (READMEs modulares) — depende de template
  └── P2-5 (Onboarding guide) — depende de README

P2-1 (MFA enforcement)
  └── P2-2 (SAML) — MFA deve estar enforced antes

P3-1 (AI Tools runtime)
  └── P3-5 (Streaming) — tools devem funcionar antes
  └── P3-8 (RAG) — tools devem funcionar antes

P3-3 + P3-4 (Migrações)
  └── P3-2 (RowVersion) — migrações necessárias primeiro
```

---

## 9. Critérios de Aceitação por Prioridade

### P0 fechado quando:
- Todas as 3 rotas de Contracts navegáveis e renderizam correctamente

### P1 fechado quando:
- README raiz existe com setup instructions
- 9 READMEs modulares criados
- Página 0 bytes implementada com conteúdo
- Documentação AI alinhada com realidade
- Integrations e Analytics com docs mínimas
- Páginas órfãs resolvidas (conectadas ou removidas)

### P2 fechado quando:
- MFA enforced em runtime para utilizadores configurados
- API key armazenada em BD encriptada
- Gaps i18n reduzidos a <3 namespaces por locale
- Onboarding guide funcional
- Audit & Compliance frontend acima de 65%

### P3 fechado quando:
- AI Tools executam ações reais
- RowVersion em entidades críticas (>50% do total)
- Migrações existem para Configuration e Notifications
- Streaming funcional para chat
- Pelo menos 2 providers AI activos

### P4 fechado quando:
- Check constraints em colunas críticas
- nextraceone_operations avaliado e plano documentado
