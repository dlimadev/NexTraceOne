# Critérios de Fecho do Produto — NexTraceOne

> **Classificação:** CRITÉRIOS DE ACEITAÇÃO  
> **Data de referência:** Julho 2025  
> **Escopo:** Definição do que constitui "fechado" para cada nível de maturidade  
> **Objetivo:** Eliminar ambiguidade sobre quando o produto está pronto

---

## 1. Filosofia de Fecho

O NexTraceOne não precisa de ser perfeito para estar production-ready. Precisa de ser **sólido, seguro, utilizável e manutenível**. Os critérios de fecho são organizados em três categorias:

| Categoria | Significado | Obrigatório para produção? |
|-----------|------------|---------------------------|
| **MANDATORY** | Sem isto, o produto não vai para produção | Sim |
| **IMPORTANT** | Melhora significativamente o produto, mas pode ser diferido | Não, mas planeado |
| **FUTURE** | Evolução natural, sem impacto na entrega actual | Não |

---

## 2. Critérios MANDATORY — Obrigatórios para Produção

### 2.1 Funcionalidade Base

| # | Critério | Validação | Estado Actual |
|---|---------|-----------|---------------|
| M-1 | Todos os P0 blockers corrigidos | Zero P0 no backlog | ❌ 1 P0 (rotas partidas) |
| M-2 | Todos os P1 critical corrigidos | Zero P1 no backlog | ❌ 7 P1 activos |
| M-3 | ≥80% dos P2 high corrigidos | ≤2 P2 em aberto | ❌ 11 P2 activos |
| M-4 | Menu sidebar coerente com rotas existentes | Navegar por todos os itens sem erro | ❌ 3 rotas partidas |
| M-5 | Zero rotas partidas (404) a partir do menu | Teste manual ou E2E | ❌ 3 rotas partidas |
| M-6 | Zero páginas com 0 bytes | Verificação automática de tamanho | ❌ 1 página (Analytics) |
| M-7 | Todos os módulos acima de 50% maturidade | Avaliação por módulo | ❌ 2 abaixo (Analytics 30%, Integrations 41%) |

### 2.2 Segurança

| # | Critério | Validação | Estado Actual |
|---|---------|-----------|---------------|
| M-8 | Autenticação funcional (JWT + OIDC) | Teste E2E de login | ✅ |
| M-9 | Autorização funcional (73 permissões) | Teste por role | ✅ |
| M-10 | Multi-tenancy isolado (RLS) | Teste cross-tenant | ✅ |
| M-11 | MFA enforcement activo | Teste com utilizador MFA-enabled | ❌ Modelado, não enforced |
| M-12 | API keys em BD encriptada | Verificar storage location | ❌ Em memória |
| M-13 | CSRF protection activa | Teste de cross-site request | ✅ |
| M-14 | Rate limiting funcional | Teste de load | ✅ |
| M-15 | Encriptação AES-256-GCM em dados sensíveis | Verificar BD directamente | ✅ |
| M-16 | Audit trail funcional | Verificar logs de acções críticas | ✅ |

### 2.3 Tenant e Environment

| # | Critério | Validação | Estado Actual |
|---|---------|-----------|---------------|
| M-17 | Tenant isolation coerente | Query cross-tenant retorna zero | ✅ |
| M-18 | Environment como first-class entity | Verificar modelo | ✅ |
| M-19 | Dados não vazam entre tenants | Penetration test básico | ✅ |
| M-20 | Dados não vazam entre environments | Teste manual | ✅ |

### 2.4 Dados e Persistência

| # | Critério | Validação | Estado Actual |
|---|---------|-----------|---------------|
| M-21 | Todos os módulos com migrações | Verificar EF Core migrations | ❌ Config + Notifications sem migrações |
| M-22 | Soft delete transversal | Verificar IsDeleted | ✅ |
| M-23 | Outbox pattern funcional | Verificar event dispatch | ✅ |
| M-24 | Seeds de produção aplicáveis | Executar seed em BD limpa | ✅ (parcial) |

### 2.5 Documentação

| # | Critério | Validação | Estado Actual |
|---|---------|-----------|---------------|
| M-25 | README raiz existente | Verificar ficheiro | ❌ Inexistente |
| M-26 | Setup local documentado e funcional | Seguir instruções em máquina limpa | ❌ Sem README |
| M-27 | Arquitectura high-level documentada | Diagrama + texto | ❌ Fragmentado |
| M-28 | READMEs modulares existentes para módulos core | Verificar ≥6 módulos | ❌ 0/9 |

### 2.6 Qualidade

| # | Critério | Validação | Estado Actual |
|---|---------|-----------|---------------|
| M-29 | 1709+ testes backend a passar | CI/CD verde | ✅ |
| M-30 | Build sem erros | `dotnet build` limpo | ✅ |
| M-31 | Zero warnings críticos | Verificar build output | ✅ (assumido) |
| M-32 | i18n funcional em en, pt-BR | Verificar UI nos 2 locales primários | ⚠️ pt-BR -11 namespaces |

### Resumo MANDATORY

| Categoria | Total | ✅ Cumprido | ❌ Por cumprir |
|-----------|-------|-------------|---------------|
| Funcionalidade | 7 | 0 | 7 |
| Segurança | 9 | 7 | 2 |
| Tenant/Env | 4 | 4 | 0 |
| Dados | 4 | 3 | 1 |
| Documentação | 4 | 0 | 4 |
| Qualidade | 4 | 3 | 1 |
| **Total** | **32** | **17 (53%)** | **15 (47%)** |

---

## 3. Critérios IMPORTANT — Diferíveis mas Planeados

| # | Critério | Impacto se Diferido | Prazo Sugerido |
|---|---------|-------------------|---------------|
| I-1 | SAML federation completa | Organizações com IdP SAML não conseguem federar | 3 meses |
| I-2 | AI streaming completo | UX de chat degradada | 3 meses |
| I-3 | AI tools funcionais (não cosmético) | Diferenciação competitiva reduzida | 3 meses |
| I-4 | RAG/Retrieval funcional | AI sem acesso a knowledge base | 4 meses |
| I-5 | RowVersion/ConcurrencyToken em todas as entidades | Conflitos silenciosos (last-write-wins) | 2 meses |
| I-6 | Check constraints na BD | Validação apenas no aplicativo | 4 meses |
| I-7 | Todos os módulos acima de 70% | Experiência desigual entre módulos | 3 meses |
| I-8 | Guia de onboarding completo | Onboarding mais lento | 2 meses |
| I-9 | i18n completo em es (espanhol) | Mercado hispanófono limitado | 3 meses |
| I-10 | Frontend comments ≥10% | Manutenibilidade reduzida | Contínuo |
| I-11 | Testes AI Knowledge ≥40% | Risco de regressão | 3 meses |
| I-12 | Divisão do módulo Governance | Manutenibilidade comprometida | 4 meses |
| I-13 | Consolidação de nextraceone_operations | Performance potencialmente afectada | 6 meses |

---

## 4. Critérios FUTURE — Evolução Natural

| # | Critério | Horizonte | Categoria |
|---|---------|----------|-----------|
| F-1 | IDE extensions (VS Code, Visual Studio) | 6-9 meses | AI Governance |
| F-2 | Semantic Kernel integration | 6-9 meses | AI |
| F-3 | FinOps avançado por serviço/equipa/operação | 6-12 meses | Governance |
| F-4 | Responsividade mobile completa | 6-12 meses | Frontend |
| F-5 | Anthropic provider activo | 3-6 meses | AI |
| F-6 | Contract Studio com geração IA completa | 6-9 meses | Contracts |
| F-7 | Blast radius visual interactivo | 6-9 meses | Change Intelligence |
| F-8 | Dashboard de topology em tempo real | 6-12 meses | Catalog |
| F-9 | Webhook e SMS channels completos | 3-6 meses | Notifications |
| F-10 | Multi-region deployment support | 9-12 meses | Infrastructure |

---

## 5. Definição de "Fechado" por Nível

### Nível 1: MVP Production-Ready
**Quando:** Todos os MANDATORY cumpridos  
**Maturidade global:** ≥80%  
**Prazo estimado:** 8-10 semanas a partir de agora

| Aspeto | Requisito |
|--------|----------|
| P0 | Zero |
| P1 | Zero |
| P2 | ≤2 em aberto |
| Módulos ≥50% | 12/12 |
| Módulos ≥70% | ≥8/12 |
| Segurança | MFA enforced, API key em BD |
| Docs | README raiz + 6 READMEs modulares |
| Testes | 1709+ a passar |

### Nível 2: Enterprise-Ready
**Quando:** Todos os MANDATORY + ≥80% IMPORTANT cumpridos  
**Maturidade global:** ≥88%  
**Prazo estimado:** 14-16 semanas a partir de agora

| Aspeto | Requisito |
|--------|----------|
| P0-P2 | Zero |
| P3 | ≤3 em aberto |
| Módulos ≥70% | 12/12 |
| AI | Tools + streaming funcionais |
| SAML | Implementado |
| RowVersion | ≥80% entidades |
| Docs | Completos + onboarding guide |

### Nível 3: Platform-Complete
**Quando:** Todos os MANDATORY + IMPORTANT + ≥50% FUTURE cumpridos  
**Maturidade global:** ≥93%  
**Prazo estimado:** 6-9 meses a partir de agora

| Aspeto | Requisito |
|--------|----------|
| Todos os P | Zero P0-P3 |
| Módulos ≥80% | 12/12 |
| AI | RAG + IDE extensions |
| FinOps | Contextualizado |
| Mobile | Responsivo |

---

## 6. Processo de Validação de Fecho

### Para cada módulo
1. Executar checklist de 7 etapas (ver module-closure-plan.md)
2. Validar com build limpo
3. Executar testes do módulo
4. Verificar rotas e UI manualmente
5. Revisão de docs por developer diferente
6. Marcar como "Fechado" no dashboard de progresso

### Para o produto
1. Executar todos os testes (1709+)
2. Navegar por todo o menu sidebar sem erros
3. Testar login com JWT, OIDC, API Key
4. Verificar isolamento de tenant
5. Verificar i18n em en + pt-BR
6. Executar setup local a partir do README
7. Produzir relatório final de maturidade

---

## 7. Métricas de Fecho

| Métrica | Valor Actual | Alvo MVP | Alvo Enterprise |
|---------|-------------|----------|----------------|
| P0 blockers | 1 | 0 | 0 |
| P1 critical | 7 | 0 | 0 |
| P2 high | 11 | ≤2 | 0 |
| Módulos ≥50% | 10/12 | 12/12 | 12/12 |
| Módulos ≥70% | 5/12 | ≥8/12 | 12/12 |
| Maturidade global | ~75% | ≥80% | ≥88% |
| Testes backend | 1709 | 1709+ | 2000+ |
| READMEs | 0 | 7+ | 12+ |
| i18n completo | en | en + pt-BR | en + pt-BR + es + pt-PT |
| Segurança | 85% | 92% | 95% |
